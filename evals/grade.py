#!/usr/bin/env python3
"""Deterministic grader for legislator e2e eval runs.

Grades the file tree an agent produced by running the skill against a
workspace repo (see setup_workspace.py). All expectations are DERIVED from
the current skill source (VERSION, assets/rules/**, template list) at grade
time — nothing is hardcoded to a specific constitution version, so this
grader does not rot when rules are added, removed, or renamed.

Usage:
  python3 evals/grade.py <workspace> [scenario ...]

Scenarios (default: the first five):
  fresh-scaffold-dotnet    grade <ws>/fresh-scaffold-dotnet/repo
  legacy-migration         grade <ws>/legacy-migration/repo (+ the Step 7
                           report saved at legacy-migration/outputs/)
  upgrade                  grade <ws>/upgrade/repo (needs fixture_meta.json)
  audit                    grade the audit report saved by the eval agent at
                           <ws>/rotted-layer/outputs/audit-report.md against
                           the fixture's planted defects; asserts zero writes.
  restructure              grade <ws>/restructure/repo + the report saved at
                           restructure/outputs/
  idempotency:<scenario>   grade a SECOND skill run on <scenario>'s repo:
                           requires that run 1's result was committed before
                           run 2; passes iff run 2 left a zero diff.
                           (benchmarks run it for fresh-scaffold-dotnet and upgrade)

Writes grading.json into <ws>/<scenario>/ (viewer-compatible schema) and
prints a verdict table. Every assert declares the artifact it reads; when
that artifact is absent or empty the assert is `unmeasured` — neither passed
nor failed — and a scenario carrying one is red (POLICY §1b). A scenario
reports two numbers: how many of its asserts were measured, and how many of
those passed. Exit code 1 if any assert failed OR went unmeasured.
"""
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

EVALS = Path(__file__).resolve().parent
SKILL = EVALS.parent / "skill"

# ---------------------------------------------------------------------------
# Contract derivation (BL-036 Wave A): every place the grader used to
# hand-duplicate a skill contract is derived from the skill source at
# grade time. A divergence between law and grader must be impossible,
# not merely noticed. Deliberately manual: fixture content markers
# (decimal-money, bl/NNN) — intentional test-data oracles, not contract.
# ---------------------------------------------------------------------------

def _skill_md() -> str:
    return (SKILL / "SKILL.md").read_text()


def scaffold_artifacts() -> list[str]:
    """File targets parsed from SKILL.md Step 4's table — the table is the
    only source of what a scaffold must create (README's 'maintain by
    hand' note is dead). Rows: `| <target> | <template> | ...`; empty-dir
    rows (template column '(empty directory)') are skipped: no file to
    assert, the scaffold_checks directory assertions cover them."""
    text = _skill_md()
    step4 = text.split("## Step 4", 1)[1].split("## Step 5", 1)[0]
    out = []
    for m in re.finditer(r"^\| `([^`]+)` \| ([^|]+) \|", step4, re.M):
        target, template = m.group(1), m.group(2).strip()
        if template.startswith("(empty"):
            continue
        out.append(target)
    return sorted(out)


SCAFFOLD_ARTIFACTS = scaffold_artifacts()

# File authority (BL-038, edition v18): the ONE table in SKILL.md that
# states what each invocation mode may do to each artifact class. The
# grader derives its protected/writable expectations from it and never
# restates a right by hand. A malformed table raises — grading leniently
# against a broken matrix would be the v17 incident in reverse.
AUTHORITY_VALUES = frozenset({
    "replace", "create-if-absent", "lossless-write", "propose-only",
    "move-or-merge", "link-only", "read-only", "never-touch",
})
AUTHORITY_MODES = ("scaffold", "migrate", "upgrade", "restructure", "audit")
AUTHORITY_CLASSES = (
    "entry document", "owned law", "manifest", "project rules",
    "scaffolded artifacts", "relocated owner content",
    "foreign structures", "kept paths",
)


def _authority_rows(text: str | None = None) -> list[list[str]]:
    """The pipe-table rows of SKILL.md's `## File authority` section, each
    as a list of stripped cells (outer pipes removed). `text` overrides the
    skill source so the selftest can feed a malformed table."""
    text = _skill_md() if text is None else text
    if "\n## File authority\n" not in text:
        raise ValueError("File authority: no `## File authority` section in SKILL.md")
    section = text.split("\n## File authority\n", 1)[1].split("\n## ", 1)[0]
    rows = []
    for line in section.splitlines():
        if line.startswith("|") and not re.match(r"^\|[\s:|-]+\|$", line):
            rows.append([c.strip() for c in line.strip().strip("|").split("|")])
    return rows


def authority_matrix(text: str | None = None) -> dict[tuple[str, str], str]:
    """(class, mode) -> right, parsed from the pinned two-header table."""
    rows = _authority_rows(text)
    if len(rows) != 2 + len(AUTHORITY_CLASSES):
        raise ValueError(f"File authority: expected 2 header rows + {len(AUTHORITY_CLASSES)} body rows, got {len(rows)}")
    modes = tuple(c for c in rows[1][1:])
    if modes != AUTHORITY_MODES:
        raise ValueError(f"File authority: mode row must be {AUTHORITY_MODES}, got {modes}")
    out: dict[tuple[str, str], str] = {}
    for row in rows[2:2 + len(AUTHORITY_CLASSES)]:
        cls = row[0].split(" (", 1)[0].strip().lower()
        if cls not in AUTHORITY_CLASSES:
            raise ValueError(f"File authority: unknown artifact class {cls!r}")
        cells = row[1:1 + len(AUTHORITY_MODES)]
        if len(cells) != len(AUTHORITY_MODES):
            raise ValueError(f"File authority: row {cls!r} has {len(cells)} cells, expected {len(AUTHORITY_MODES)}")
        for mode, cell in zip(AUTHORITY_MODES, cells):
            if cell not in AUTHORITY_VALUES:
                raise ValueError(f"File authority: cell ({cls} × {mode}) = {cell!r} is not one of {sorted(AUTHORITY_VALUES)}")
            out[(cls, mode)] = cell
    missing = [(c, m) for c in AUTHORITY_CLASSES for m in AUTHORITY_MODES if (c, m) not in out]
    if missing:
        raise ValueError(f"File authority: cells missing for {missing[:5]}")
    return out


def authority_states() -> dict[str, str]:
    """mode -> state (installing / maintaining / inspecting), read from the
    state header row directly above each mode."""
    rows = _authority_rows()
    if len(rows) < 2:
        raise ValueError("File authority: no table rows under the heading")
    states = rows[0][1:1 + len(AUTHORITY_MODES)]
    if len(states) != len(AUTHORITY_MODES):
        raise ValueError(f"File authority: state row has {len(states)} cells, expected {len(AUTHORITY_MODES)}")
    return dict(zip(AUTHORITY_MODES, states))


def class_paths(repo: Path, cls: str, fixture_meta: dict | None = None) -> list[str]:
    """Concrete repo-relative paths of one artifact class. Skill-derived
    where the skill defines the class; fixture-declared for the two
    classes only a fixture can know (what is foreign, what was relocated)."""
    meta = fixture_meta or {}
    if cls == "entry document":
        return ["AGENTS.md", "CLAUDE.md"]
    if cls == "owned law":
        return sorted(expected_owned())
    if cls == "manifest":
        return ["docs/ai/manifest.json"]
    if cls == "project rules":
        tracked = git(repo, "ls-files", ".claude/rules").split()
        return sorted(set(tracked) | {p for p in SCAFFOLD_ARTIFACTS if p.startswith(".claude/rules/")})
    if cls == "scaffolded artifacts":
        return [p for p in SCAFFOLD_ARTIFACTS if not p.startswith(".claude/rules/")
                and p not in ("AGENTS.md", "CLAUDE.md")]
    if cls == "relocated owner content":
        return sorted(meta.get("authority_relocated_owner_content", []))
    if cls == "foreign structures":
        return sorted(meta.get("authority_foreign_structures", []))
    if cls == "kept paths":
        return sorted(k["path"] for k in meta.get("expected_keep", []))
    raise ValueError(f"File authority: unknown class {cls!r}")


# File authority (ruling 2026-08-22, spec §3): `propose-only` protects the
# document's CONTENT, not its path — upgrade on a pre-v14 legislated repo
# lawfully renames CLAUDE.md -> AGENTS.md (SKILL.md Step 3), so a path-based
# "no change" reading of the cell fails lawful behaviour. Path-protecting
# rights still forbid any change to the path; content-protecting rights are
# instead enforced by check_mode_authority's canonical-document identity
# check (below), never by protected_project_files.
PATH_PROTECTING_RIGHTS = frozenset({"create-if-absent", "read-only",
                                    "link-only", "never-touch"})
CONTENT_PROTECTING_RIGHTS = frozenset({"propose-only"})

# The canonical file whose content stands in for a content-protected class.
# A content-protecting cell on a class absent from this map is a malformed
# matrix (check_mode_authority raises).
CANONICAL_FILE = {"entry document": "AGENTS.md"}


def protected_project_files(repo: Path, fixture_meta: dict | None = None,
                            matrix: dict | None = None) -> list[str]:
    """Tracked, PATH-protected files an upgrade run must leave byte-unchanged
    at their path, derived from the matrix: every path of every class whose
    `upgrade` cell is in PATH_PROTECTING_RIGHTS, restricted to what existed
    at HEAD. No hand-written exclusions — AGENTS.md drops out because its
    cell says propose-only, not because someone listed it. Content-protected
    classes (propose-only) are NOT in this set — their file may lawfully
    change path (the v14 rename); they are instead checked for content
    identity by check_mode_authority."""
    m = matrix if matrix is not None else authority_matrix()
    tracked = set(git(repo, "ls-files").split())
    out: set[str] = set()
    for cls in AUTHORITY_CLASSES:
        if m[(cls, "upgrade")] in PATH_PROTECTING_RIGHTS:
            out |= {p for p in class_paths(repo, cls, fixture_meta) if p in tracked}
    return sorted(out)


def _head_real_file_content(repo: Path, path: str) -> bytes | None:
    """HEAD's content for `path` if it was a REAL (non-symlink) file at
    HEAD, else None (absent at HEAD, or a symlink — a symlinked entry
    document has no content of its own to protect)."""
    exists = subprocess.run(
        ["git", "-C", str(repo), "cat-file", "-e", f"HEAD:{path}"],
        capture_output=True).returncode == 0
    if not exists:
        return None
    ls = subprocess.run(["git", "-C", str(repo), "ls-tree", "HEAD", path],
                        capture_output=True, text=True).stdout
    if not ls.strip() or ls.split()[0] != "100644":
        return None
    return subprocess.run(["git", "-C", str(repo), "show", f"HEAD:{path}"],
                          capture_output=True).stdout


def check_mode_authority(g: "Grader", repo: Path, mode: str,
                         fixture_meta: dict | None = None,
                         delegated: dict[str, str] | None = None,
                         art: "Artifact | None" = None) -> None:
    """One assert per scenario: the run's tracked-file diff, restricted to
    each artifact class, satisfies that class's cell for this mode.
    Content-level proof for lossless-write / move-or-merge stays with the
    scenario's fidelity asserts; this checks the SHAPE of the diff.
      replace, lossless-write, move-or-merge -> any change
      create-if-absent                       -> additions only
      read-only, never-touch                 -> no change
      link-only                              -> no change to the path itself
    A content-protecting right (propose-only) is not a "no porcelain
    change" rule — the v14 file-model rename (CLAUDE.md -> AGENTS.md,
    symlink back) is lawful wiring under this right even though it is a
    path change. Instead: for every path of the class that was a REAL file
    at HEAD, its HEAD content must equal the post-run content of the
    class's canonical file (symlinks resolved); the porcelain status of
    the pair is not consulted. A content-protecting cell on a class absent
    from CANONICAL_FILE (a malformed matrix) is reported as this same
    assert's FAIL, not raised — a bad matrix must fail the grade, never
    crash the grade run.

    `art` is the repository artifact this assert reads — the caller's
    scenario tree, or the synthetic repo the derivation selftest builds. It
    is optional only so that the one caller with no scenario (that selftest)
    can pass its own; every scenario passes its `g.repo_art`.

    `delegated` maps a class to the mode whose column governs it for THIS
    run, for the one case the law defines: restructure's `heal` action
    "runs SKILL.md Steps 2-3 as-is", so the owned law and manifest it
    rewrites are written under the upgrade column, not under
    restructure's own `never-touch`. The caller derives the map from the
    law (`restructure_heal_delegates`) and passes it only when the run
    actually healed — a restructure that did not heal is still held to
    its own column."""
    art = art if art is not None else g.repo_art
    try:
        m = authority_matrix()
    except ValueError as e:
        g.check("mode_respects_authority", False, str(e), artifact=art)
        return
    status = {}
    for line in git(repo, "status", "--porcelain", "--untracked-files=all").splitlines():
        code, path = line[:2].strip(), line[3:]
        if " -> " in path:                      # rename: both sides count
            old, new = path.split(" -> ", 1)
            status[old] = "D"; status[new] = "A"
        else:
            status[path] = "A" if code in ("??", "A") else ("D" if code == "D" else "M")
    violations = []
    try:
        deleg = delegated or {}
        for cls in AUTHORITY_CLASSES:
            eff_mode = deleg.get(cls, mode)
            right = m[(cls, eff_mode)]
            if right in CONTENT_PROTECTING_RIGHTS:
                canonical = CANONICAL_FILE.get(cls)
                if canonical is None:
                    raise ValueError(f"File authority: no canonical file for class {cls!r}")
                resolved = (repo / canonical).resolve()
                post_content = resolved.read_bytes() if resolved.exists() else None
                for p in class_paths(repo, cls, fixture_meta):
                    head_content = _head_real_file_content(repo, p)
                    if head_content is None:
                        continue
                    if post_content is None or head_content != post_content:
                        violations.append(
                            f"{cls} × {eff_mode} = {right}, but {canonical} content changed (was HEAD:{p})")
                continue
            for p in class_paths(repo, cls, fixture_meta):
                change = status.get(p)
                if change is None:
                    continue
                if right in ("replace", "lossless-write", "move-or-merge"):
                    continue
                if right == "create-if-absent" and change == "A":
                    continue
                violations.append(f"{cls} × {eff_mode} = {right}, but {p} {change}")
    except ValueError as e:
        g.check("mode_respects_authority", False, str(e), artifact=art)
        return
    deleg_note = ("" if not delegated else
                  " (" + ", ".join(f"{c} delegated to {mo}"
                                   for c, mo in sorted(delegated.items())) + ")")
    g.check("mode_respects_authority", not violations,
            f"diff shape lawful for all {len(AUTHORITY_CLASSES)} classes in {mode} mode{deleg_note}"
            if not violations else "; ".join(violations[:4]), artifact=art)


def expected_stacks(fixture_meta: dict | None = None) -> list[str]:
    """The manifest's stack subscription comes from the fixture's own
    meta (per-fixture, not a global hardcode): upgrade/drop-stack
    fixtures carry theirs; everything else is dotnet-only."""
    if fixture_meta and "stacks" in fixture_meta:
        return list(fixture_meta["stacks"])
    return ["dotnet"]


def migration_wiring() -> list[str]:
    """Strings migration must write directly into AGENTS.md (the v2
    wiring), derived from AGENTS.md.tpl: every import line the template
    carries plus the section headings it pins."""
    tpl = (SKILL / "assets/templates/AGENTS.md.tpl").read_text()
    imports = ["@" + i for i in re.findall(r"^@(docs/[^\s]+)$", tpl, re.M)]
    return imports + ["## Boundaries"]


def audit_check_severities() -> dict[str, str]:
    """Pinned check slug -> severity, parsed from SKILL.md's Audit list
    ('N. **<name> (<severity>):**'). The severity-anchored markers and
    the parity law derive from this map."""
    text = _skill_md()
    out = {}
    audit = text.split("Perform these checks", 1)[1]
    for m in re.finditer(r"\d+\.\s+\*\*([^*]+?)\s*\((Critical|Warning|Info)\):\*\*", audit):
        out[m.group(1)] = m.group(2)
    return out


def audit_check_slugs() -> set[str]:
    """The pinned finding slugs, parsed from SKILL.md's "In findings,
    `[check-name]` is the check's pinned slug — use exactly these: ..."
    line. Parity is measured in THIS namespace: the earlier version
    compared check titles against slug markers — two disjoint namespaces,
    so the assert was red by construction and never passed a live grade
    (found 2026-08-22)."""
    m = re.search(r"pinned slug — use exactly these:(.+)", _skill_md())
    return set(re.findall(r"`([a-z][a-z0-9-]+)`", m.group(1))) if m else set()


def restructure_actions() -> set[str]:
    """The closed action set, parsed from restructure.md §2's bold
    definitions."""
    text = (SKILL / "references/restructure.md").read_text()
    section2 = text.split("## 2.", 1)[1].split("## 3.", 1)[0]
    return set(re.findall(r"^- \*\*(\w+)\*\*", section2, re.M))


def restructure_heal_delegates() -> dict[str, str]:
    """class -> the mode whose column governs that class during a
    restructure run that heals, parsed from restructure.md §2's `heal`
    bullet.

    `heal` is the one restructure action that does not write under
    restructure's own authority: it "runs SKILL.md Steps 2-3 as-is", i.e.
    it invokes another column. The bullet says which one, per class, in
    its `(authority: <class> x <mode>)` references — so the delegation is
    derived from the law rather than restated here (POLICY §8).
    """
    text = (SKILL / "references/restructure.md").read_text()
    section2 = text.split("## 2.", 1)[1].split("## 3.", 1)[0]
    bullet = next((l for l in section2.splitlines()
                   if l.startswith("- **heal**")), "")
    return {cls.strip(): mode
            for cls, mode in re.findall(r"\(authority: ([a-z ]+?) × ([a-z]+)", bullet)}


# Migration fixture content that must never be silently dropped.
MIGRATION_PRESERVED = [
    "Money values are always",
    "bl/NNN-short-description",
]


def git(repo: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(repo), *args],
                          capture_output=True, text=True).stdout


def law_stamp() -> str:
    """The generation this grading ran against: skill VERSION + repo HEAD
    + a hash of THIS grader. Runs graded under different stamps are
    different populations — the dashboard's flaky counter never mixes
    them. The grader hash matters as much as the law hash: a grader fix
    (observed 2026-08-21, twice) changes verdicts without touching the
    law, and working-tree grader edits precede their commit."""
    version = (SKILL / "VERSION").read_text().strip()
    try:
        head = git(SKILL.parent, "rev-parse", "--short", "HEAD").strip() or "?"
    except Exception:
        head = "?"
    try:
        grader = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()[:7]
    except Exception:
        grader = "?"
    return f"v{version}-{head}-g{grader}"


def glossary_rows(repo: Path) -> int:
    """Body rows of the glossary's term table specifically: consecutive pipe
    lines following the '| Term |' header, minus header and separator."""
    f = repo / "docs/okf/glossary.md"
    if not f.exists():
        return 0
    lines = f.read_text().splitlines()
    start = next((i for i, l in enumerate(lines)
                  if l.lstrip().startswith("|") and "Term" in l), None)
    if start is None:
        return 0
    n = 0
    for l in lines[start:]:
        if not l.lstrip().startswith("|"):
            break
        n += 1
    return max(0, n - 2)


def expected_owned() -> dict[str, Path]:
    """repo-relative owned path -> source file, derived from skill source."""
    owned: dict[str, Path] = {}
    for f in sorted((SKILL / "assets/rules/core").glob("*.md")):
        owned[f"docs/ai/rules/core/{f.name}"] = f
    for profile in expected_stacks():
        for f in sorted((SKILL / "assets/rules/stacks" / profile).glob("*.md")):
            owned[f"docs/ai/rules/stacks/{profile}/{f.name}"] = f
    # v14: the root owned wiring file opencode.json (no placeholders; byte-copied
    # from its template source).
    oc_src = SKILL / "assets" / "templates" / "opencode.json.tpl"
    if oc_src.exists():
        owned["opencode.json"] = oc_src
    # v20: the constitution's engine, an owned executable delivered like law.
    eng_src = SKILL / "assets" / "engine" / "engine.py"
    if eng_src.exists():
        owned["docs/ai/engine.py"] = eng_src
    return owned


# ---------------------------------------------------------------------------
# Artifacts (BL-062, POLICY §1b): every assert declares the artifact it reads.
#
# Measured on 2026-08-25 against the v21 corpus, by blanking a report and
# re-grading: `audit` still scored 14/44 and `legacy-migration-agents-first`
# a perfect 22/22 with a report containing nothing. Two mechanisms produced
# that — a `does NOT contain` assert is vacuously true against an absent
# artifact, and `*_report_saved` tested `path.exists()` where substance was
# meant. Both are arithmetic bugs, not judgement calls, so the repair is
# arithmetic: an assert whose artifact cannot be read is `unmeasured` — not
# passed, not failed — and any unmeasured assert makes its scenario red.
#
# The declaration is DATA, not a comment: `Grader.check` takes the artifact
# as a required keyword, so an assert that names no source cannot be written
# at all. That is the whole enforcement mechanism, and it costs one argument.
# ---------------------------------------------------------------------------

class Artifact:
    """A named thing an assert reads, and whether it can be read at all.

    Measurability is computed once and cached: nothing this grader does
    changes an artifact, so a second look would only cost time and risk
    two asserts disagreeing about the same file."""

    def __init__(self, name: str, path: Path | None, probe) -> None:
        self.name = name
        self.path = path
        self._probe = probe
        self._state: tuple[bool, str] | None = None

    def _measure(self) -> tuple[bool, str]:
        if self._state is None:
            try:
                self._state = self._probe(self.path)
            except OSError as e:
                self._state = (False, f"unreadable ({e})")
        return self._state

    @property
    def measurable(self) -> bool:
        return self._measure()[0]

    @property
    def reason(self) -> str:
        """Human-readable state — the evidence line of the artifact's probe
        assert when present, and of every unmeasured assert when absent."""
        return self._measure()[1]

    @staticmethod
    def file(name: str, path: Path | None) -> "Artifact":
        """Exists and holds at least one non-whitespace character. Emptiness
        counts as absence: a zero-byte report is exactly what the old
        `path.exists()` probes scored as a pass."""
        def probe(p):
            if p is None or not p.is_file():
                return False, f"absent ({p})"
            if not p.read_bytes().strip():
                return False, f"empty ({p})"
            return True, f"{p} ({p.stat().st_size} bytes)"
        return Artifact(name, path, probe)

    @staticmethod
    def json(name: str, path: Path | None) -> "Artifact":
        """A JSON file that parses. Unparseable is unreadable: the asserts
        that would read its keys have nothing to read."""
        def probe(p):
            if p is None or not p.is_file():
                return False, f"absent ({p})"
            try:
                json.loads(p.read_text())
            except (json.JSONDecodeError, UnicodeDecodeError) as e:
                return False, f"invalid JSON ({e})"
            return True, f"{p} parses"
        return Artifact(name, path, probe)

    @staticmethod
    def repo(name: str, path: Path | None) -> "Artifact":
        """A materialized git working tree. This is the declaration that
        catches v20's incident, where an unmaterialized workspace graded two
        scenarios CLEAN: with no tree there is nothing to measure, and the
        scenario says so instead of scoring it."""
        def probe(p):
            if p is None or not p.is_dir():
                return False, f"absent ({p})"
            if not (p / ".git").exists():
                return False, f"not a git repository ({p})"
            return True, f"{p}"
        return Artifact(name, path, probe)

    @staticmethod
    def dir(name: str, path: Path | None) -> "Artifact":
        """A directory that exists. Containers only — the home a probe
        assert is measured against."""
        def probe(p):
            if p is None or not p.is_dir():
                return False, f"absent ({p})"
            return True, f"{p}"
        return Artifact(name, path, probe)

    @staticmethod
    def docs(name: str, path: Path | None) -> "Artifact":
        """A directory holding at least one non-empty Markdown file. An
        agent that creates the directory and writes nothing into it has
        produced no artifact to read."""
        def probe(p):
            if p is None or not p.is_dir():
                return False, f"absent ({p})"
            written = [f for f in p.rglob("*.md") if f.read_bytes().strip()]
            if not written:
                return False, f"no non-empty .md under {p}"
            return True, f"{p} ({len(written)} document(s))"
        return Artifact(name, path, probe)


# The law and the grader itself: what the derivation selftest reads. Named
# rather than assumed — a moved rule file must report as unmeasured, not as
# a derivation that suddenly returns nothing.
LAW_SKILL = Artifact.file("law:SKILL.md", SKILL / "SKILL.md")
LAW_RESTRUCTURE = Artifact.file("law:references/restructure.md",
                                SKILL / "references/restructure.md")
LAW_AGENTS_TPL = Artifact.file("law:assets/templates/AGENTS.md.tpl",
                               SKILL / "assets/templates/AGENTS.md.tpl")
GRADER_SELF = Artifact.file("grader:evals/grade.py", Path(__file__))
FIXTURE_UPGRADE_BASE = Artifact.dir("fixture:upgrade-base",
                                    EVALS / "fixtures" / "upgrade-base")

VERDICTS = ("passed", "failed", "unmeasured")


class Grader:
    """One scenario's asserts, and the artifacts they are entitled to read.

    A scenario declares its artifacts once, here, and every assert points at
    one of them. `home` is the scenario directory — the container a report's
    own existence is measured against, so that an unmaterialized workspace
    cannot even produce a red probe and claim to have looked."""

    def __init__(self, repo: Path | None = None, report: Path | None = None,
                 home: Path | None = None, label: str = "") -> None:
        self.exps: list[dict] = []
        self.label = label
        self.home_art = Artifact.dir(f"home:{label}", home) if home is not None else None
        self.repo_art = Artifact.repo(f"repo:{label}", repo) if repo is not None else None
        self.manifest_art = (Artifact.json(f"manifest:{label}", repo / "docs/ai/manifest.json")
                             if repo is not None else None)
        self.report_art = (Artifact.file(f"report:{label}", report)
                           if report is not None else None)

    def check(self, name: str, passed: bool, evidence: str, *,
              artifact: Artifact) -> None:
        """Record one assert against the artifact it declares.

        When that artifact cannot be read the assert's own expression is
        discarded, evidence included: a `does NOT contain` that ran against
        an empty string produced the string "correctly absent", and printing
        it would be the false green in words."""
        if not isinstance(artifact, Artifact):
            raise TypeError(
                f"assert {name!r} declares no artifact — POLICY §1b requires one")
        if artifact.measurable:
            verdict = "passed" if passed else "failed"
        else:
            verdict = "unmeasured"
            evidence = f"not measured — {artifact.name}: {artifact.reason}"
        self.exps.append({"text": name, "passed": verdict == "passed",
                          "verdict": verdict, "artifact": artifact.name,
                          "evidence": evidence})

    def probe(self, name: str, artifact: Artifact, *, container: Artifact) -> None:
        """The one assert per artifact whose subject IS its readability, and
        therefore the only one entitled to go RED when it is absent. It is
        measured against the artifact's container: if the container is gone
        too, nothing here was measured either, and saying so is the point."""
        self.check(name, artifact.measurable, artifact.reason, artifact=container)

    def tally(self) -> dict:
        """The two numbers POLICY §1b requires, plus what they are made of.
        `measured` is the denominator of the pass rate; `total` is not."""
        counts = {v: sum(1 for e in self.exps if e["verdict"] == v) for v in VERDICTS}
        measured = counts["passed"] + counts["failed"]
        return {"passed": counts["passed"], "failed": counts["failed"],
                "unmeasured": counts["unmeasured"], "measured": measured,
                "total": len(self.exps),
                "pass_rate": round(counts["passed"] / measured, 3) if measured else 0.0}

    def common_checks(self, repo: Path, expected_keep: list | None = None,
                      fixture_meta: dict | None = None) -> None:
        owned = expected_owned()
        version = int((SKILL / "VERSION").read_text().strip())

        mpath = repo / "docs/ai/manifest.json"
        manifest, raw = None, ""
        if mpath.exists():
            raw = mpath.read_text()
            try:
                manifest = json.loads(raw)
            except json.JSONDecodeError:
                pass
        self.check("manifest_valid_json", manifest is not None,
                   "parsed OK" if manifest else "missing or invalid JSON", artifact=self.repo_art)
        self.check("manifest_version_matches_skill_VERSION",
                   bool(manifest and manifest.get("legislatorVersion") == version),
                   f"expected {version}, got {manifest.get('legislatorVersion') if manifest else None}", artifact=self.manifest_art)
        self.check("manifest_stacks_correct",
                   bool(manifest and manifest.get("stacks") == expected_stacks(fixture_meta)),
                   f"stacks={manifest.get('stacks') if manifest else None}", artifact=self.manifest_art)
        self.check("manifest_ownedFiles_exact_sorted",
                   bool(manifest and manifest.get("ownedFiles") == sorted(owned)),
                   "matches files derived from skill source" if manifest and manifest.get("ownedFiles") == sorted(owned)
                   else f"expected {sorted(owned)}, got {manifest.get('ownedFiles') if manifest else None}", artifact=self.manifest_art)
        inline = bool(re.search(r'^  "stacks": \[[^\n\]]*\],$', raw, re.M))
        self.check("manifest_stacks_single_line_inline", inline,
                   "stacks array on one line per Step 3.7" if inline else "stacks array expanded across lines", artifact=self.manifest_art)

        expected_keep = expected_keep or []
        keep = manifest.get("keep") if manifest else None
        self.check("manifest_keep_matches_expected", keep == expected_keep,
                   f"expected {expected_keep}, got {keep}", artifact=self.manifest_art)

        idx = [raw.find(f'"{k}"') for k in
               ("legislatorVersion", "stacks", "keep", "ownedFiles")]
        order_ok = all(i >= 0 for i in idx) and idx == sorted(idx)
        self.check("manifest_key_order", order_ok,
                   "legislatorVersion, stacks, keep, ownedFiles" if order_ok
                   else "keys missing or out of order", artifact=self.manifest_art)

        if isinstance(keep, list) and keep:
            block = re.search(
                r'^  "keep": \[\n((?:    \{"path": "[^"]*", "reason": "[^"]*"\},?\n)+)  \],$',
                raw, re.M)
            pinned = bool(block) and [e["path"] for e in keep] == sorted(e["path"] for e in keep)
            evidence = ("one entry per line, single-line objects, sorted by path" if pinned
                        else "keep block not in pinned form (expanded objects, unsorted, or wrong indent)")
        else:
            pinned = bool(re.search(r'^  "keep": \[\],$', raw, re.M))
            evidence = ('empty keep inline as \'"keep": [],\'' if pinned
                        else "empty keep not serialized inline on one line")
        self.check("manifest_keep_pinned_serialization", pinned, evidence, artifact=self.manifest_art)

        bad = [p for p, src in owned.items()
               if not (repo / p).exists() or (repo / p).read_bytes() != src.read_bytes()]
        self.check("owned_files_verbatim", not bad,
                   f"all {len(owned)} owned files byte-identical to source" if not bad else f"differ/missing: {bad}", artifact=self.repo_art)

        # v14 model: AGENTS.md is the canonical constitution; CLAUDE.md is a symlink to it.
        agents_md = repo / "AGENTS.md"
        claude_link = repo / "CLAUDE.md"
        is_link = claude_link.is_symlink()
        points_to_agents = is_link and os.path.realpath(claude_link) == os.path.realpath(agents_md)
        self.check("v14_model_agents_canonical_claude_symlink",
                   agents_md.is_file() and is_link and points_to_agents,
                   f"AGENTS.md={agents_md.is_file()}, CLAUDE.md symlink={is_link}, ->AGENTS.md={points_to_agents}", artifact=self.repo_art)

        status = git(repo, "status", "--porcelain").strip()
        commits = len(git(repo, "log", "--oneline").strip().splitlines())
        self.check("nothing_committed", bool(status) and commits == 1,
                   f"{len(status.splitlines()) if status else 0} changed paths in working tree, {commits} commit(s)", artifact=self.repo_art)

    def no_unresolved_tokens(self, repo: Path) -> None:
        # The WHOLE tree — tracked files united with the porcelain set —
        # never the diff alone. The porcelain-only form was blind to a
        # planted token in a file the run left untouched: on the v22
        # baseline (2026-08-25) restructure's run 1 skipped filling
        # {{PROJECT_OVERVIEW}}, this assert reported "no stray {{TOKEN}}s"
        # over a tree that carried one, run 2 filled it, and only the
        # idempotency diff exposed the pair — v17's invisible-token defect
        # alive again one layer up. (-uall so a wholly untracked docs/
        # tree lists file by file instead of collapsing to one entry.)
        offenders = []
        seen = set(git(repo, "ls-files").splitlines())
        seen.update(line[3:].strip()
                    for line in git(repo, "status", "--porcelain", "-uall").splitlines())
        for rel in sorted(seen):
            path = repo / rel
            if rel == "docs/adr/template.md" or not path.is_file():
                continue
            if path.suffix != ".md":
                continue
            # The quotation rule (BL-057), applied identically here, in
            # audit check 2 and in the engine's sdd-lint: a token inside
            # backticks or a fenced block is prose about templating.
            prose_lines, fenced = [], False
            for line in path.read_text(errors="ignore").splitlines():
                if line.lstrip().startswith("```"):
                    fenced = not fenced
                elif not fenced:
                    prose_lines.append(re.sub(r"`[^`\n]*`", "", line))
            if re.search(r"\{\{[A-Z_]+\}\}", "\n".join(prose_lines)):
                offenders.append(rel)
        self.check("no_unresolved_placeholders", not offenders,
                   "adr template carve-out respected, no stray {{TOKEN}}s" if not offenders else f"unfilled tokens in: {offenders}", artifact=self.repo_art)

    def scaffold_checks(self, repo: Path) -> None:
        missing = [a for a in SCAFFOLD_ARTIFACTS if not (repo / a).exists()]
        self.check("scaffold_artifacts_present", not missing,
                   "all Step 4 artifacts exist" if not missing else f"missing: {missing}", artifact=self.repo_art)
        sk = repo / ".claude/rules/skills.md"
        sk_text = sk.read_text() if sk.exists() else ""
        stages = [w for w in ("pre-plan", "implement", "debug", "review") if w in sk_text.lower()]
        sk_ok = sk.exists() and len(stages) >= 1
        self.check("skills_stage_map_scaffolded", sk_ok,
                   f".claude/rules/skills.md exists with stage(s) {stages}" if sk_ok
                   else ".claude/rules/skills.md missing or has no stage headings", artifact=self.repo_art)
        rows = glossary_rows(repo)
        self.check("glossary_seeded_with_terms", rows >= 1,
                   f"{rows} term row(s) derived from the repo's domain" if rows >= 1
                   else "glossary table has no body rows — {{GLOSSARY_TABLE}} derivation produced nothing", artifact=self.repo_art)
        agents = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
        missing_imports = [p for p in expected_owned()
                           if p.startswith("docs/ai/rules/core/") and f"@{p}" not in agents]
        self.check("agents_md_imports_all_core", not missing_imports,
                   "every core rule imported" if not missing_imports
                   else f"core rules on disk but not imported: {missing_imports}", artifact=self.repo_art)
        self.check("agents_md_imports_rules", "@docs/ai/rules/core/" in agents,
                   "@import block present" if "@docs/ai/rules/core/" in agents else "no @import lines in AGENTS.md", artifact=self.repo_art)
        rules_dir = repo / ".claude/rules"
        self.check("project_rules_dir_scaffolded", rules_dir.is_dir(),
                   ".claude/rules/ exists" if rules_dir.is_dir()
                   else ".claude/rules/ directory not scaffolded", artifact=self.repo_art)


def report_text(g: Grader) -> str:
    """The report's text, or "" when no readable report exists. Every assert
    that reads it declares `g.report_art`, so "" can never become a pass —
    which is the only reason returning "" is safe here at all."""
    art = g.report_art
    return art.path.read_text() if art is not None and art.measurable else ""


def grade_fresh(ws: Path) -> Grader:
    home = ws / "fresh-scaffold-dotnet"
    repo = home / "repo"
    g = Grader(repo=repo, home=home, label="fresh-scaffold-dotnet")
    g.common_checks(repo)
    check_mode_authority(g, repo, "scaffold")
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    return g


def grade_migration(ws: Path) -> Grader:
    home = ws / "legacy-migration"
    repo = home / "repo"
    g = Grader(repo=repo, report=home / "outputs" / "migration-report.md",
               home=home, label="legacy-migration")
    g.common_checks(repo)
    check_mode_authority(g, repo, "migrate")
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    agents = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
    v2_wired = all(w in agents for w in migration_wiring())
    g.check("agents_md_v2_wiring_written_directly", v2_wired,
            f"all {len(migration_wiring())} template wiring strings present in rewritten AGENTS.md (derived from AGENTS.md.tpl)" if v2_wired
            else "migration left v2 wiring as Step 7 proposals instead of writing it", artifact=g.repo_art)
    report = report_text(g)
    g.probe("step7_report_saved", g.report_art, container=g.home_art)
    m = re.search(r"## Constitution candidates\n(.*?)(?=\nClean checks:|\n#|\Z)", report, re.S)
    section = m.group(1) if m else ""
    # Coupled to the constitution's CURRENT content: if a decimal-for-money
    # rule is ever promoted into assets/rules/**, criterion 2 flips and this
    # fixture line stops being a valid candidate — update the fixture then.
    money = "Money values are always" in section
    g.check("harvest_lists_decimal_money_rule", money,
            "decimal-money constraint quoted as a candidate" if money
            else "candidates section missing or does not quote the money rule", artifact=g.report_art)
    no_leak = bool(m) and "bl/NNN-short-description" not in section
    g.check("harvest_excludes_instance_convention", no_leak,
            "branch convention correctly not proposed" if no_leak
            else "instance data leaked into candidates (or section missing)", artifact=g.report_art)
    pr_dir = repo / ".claude/rules"
    law_hits = subprocess.run(
        ["grep", "-rl", "Money values are always", str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("law_carved_to_project_rules", bool(law_hits),
            f"decimal-money law lives in {law_hits.splitlines()}" if law_hits
            else "law-shaped constraint not carved into .claude/rules/", artifact=g.repo_art)
    conv_hits = subprocess.run(
        ["grep", "-rl", "bl/NNN-short-description", str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("instance_data_not_in_project_rules", pr_dir.is_dir() and not conv_hits,
            "branch convention correctly stayed in AGENTS.md" if pr_dir.is_dir() and not conv_hits
            else f"instance data leaked into .claude/rules/ (or dir missing): {conv_hits.splitlines() if conv_hits else 'dir missing'}", artifact=g.repo_art)
    for needle in MIGRATION_PRESERVED:
        hits = subprocess.run(
            ["grep", "-rl", "--exclude-dir=.git", needle, str(repo)],
            capture_output=True, text=True).stdout.strip()
        g.check(f"preserved: {needle!r}", bool(hits),
                f"found in {hits.splitlines()}" if hits else "silently dropped — appears nowhere in the result", artifact=g.repo_art)
    return g


def grade_migration_agents_first(ws: Path) -> Grader:
    """The AGENTS-native migration branch: hand-written AGENTS.md, no
    CLAUDE.md. Same migration contract minus rename expectations, plus
    'CLAUDE.md created fresh as symlink'. The law branch ('If AGENTS.md
    already exists, it stays canonical') was specified but never
    exercised before BL-036."""
    home = ws / "legacy-migration-agents-first"
    repo = home / "repo"
    g = Grader(repo=repo, report=home / "outputs" / "migration-report.md",
               home=home, label="legacy-migration-agents-first")
    g.common_checks(repo)
    check_mode_authority(g, repo, "migrate")
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    agents = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
    v2_wired = all(w in agents for w in migration_wiring())
    g.check("agents_md_v2_wiring_written_directly", v2_wired,
            f"all {len(migration_wiring())} template wiring strings present (derived from AGENTS.md.tpl)" if v2_wired
            else "migration left v2 wiring as proposals instead of writing it", artifact=g.repo_art)
    # The three-way split, not a location pin: law-shaped constraints are
    # carved into .claude/rules/ (grade_migration asserts exactly that for
    # the same fixture line), and only instance data stays in the canonical
    # entry document. The earlier form demanded the money rule stay inside
    # AGENTS.md and so contradicted the law it was testing (found
    # 2026-08-22 — the agent was right, the assert was wrong).
    law_hits = subprocess.run(
        ["grep", "-rl", "--exclude-dir=.git", "Money values are always", str(repo)],
        capture_output=True, text=True).stdout.strip()
    instance_kept = "bl/NNN-short-description" in agents
    agents_preserved = bool(law_hits) and instance_kept
    g.check("agents_md_content_preserved", agents_preserved,
            f"law carved to {law_hits.splitlines()}, instance data kept in AGENTS.md"
            if agents_preserved
            else f"law preserved={bool(law_hits)}, instance data in AGENTS.md={instance_kept}", artifact=g.repo_art)
    g.probe("migration_report_saved", g.report_art, container=g.home_art)
    return g


def grade_upgrade(ws: Path) -> Grader:
    home = ws / "upgrade"
    repo = home / "repo"
    meta = json.loads((home / "fixture_meta.json").read_text())
    g = Grader(repo=repo, report=home / "outputs" / "upgrade-report.md",
               home=home, label="upgrade")
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []), fixture_meta=meta)
    check_mode_authority(g, repo, "upgrade", meta)

    withheld = repo / "docs/ai/rules/core" / meta["withheld_core_rule"]
    g.check("newly_added_rule_present", withheld.exists(),
            f"{meta['withheld_core_rule']} copied in by the upgrade" if withheld.exists() else f"{meta['withheld_core_rule']} still missing", artifact=g.repo_art)

    withheld_stack = repo / "docs/ai/rules/stacks/dotnet" / meta["withheld_stack_rule"]
    g.check("newly_added_stack_rule_present", withheld_stack.exists(),
            f"{meta['withheld_stack_rule']} copied in by the upgrade" if withheld_stack.exists()
            else f"{meta['withheld_stack_rule']} still missing", artifact=g.repo_art)

    report = report_text(g)
    g.probe("step7_report_saved", g.report_art, container=g.home_art)
    core_import = f"@docs/ai/rules/core/{meta['withheld_core_rule']}"
    core_review_idx = report.find("eeds your review")
    core_proposed = core_review_idx >= 0 and core_import in report[core_review_idx:]
    g.check("report_proposes_core_import_line", core_proposed,
            "core-rule import proposed in Needs-your-review" if core_proposed
            else f"no proposal for {core_import}", artifact=g.report_art)
    import_line = f"@docs/ai/rules/stacks/dotnet/{meta['withheld_stack_rule']}"
    # Scoped to the "Needs your review" section (BL-019 R3): the line counts
    # only as a PROPOSAL — its appearance in Deleted/Overwritten would not.
    review_idx = report.find("eeds your review")
    proposed = review_idx >= 0 and import_line in report[review_idx:]
    g.check("report_proposes_stack_import_line", proposed,
            "proposed in the Needs-your-review section" if proposed
            else f"no 'Needs your review' section proposing {import_line}", artifact=g.report_art)

    retired = repo / "docs/ai/rules/core" / meta["retired_rule"]
    g.check("retired_rule_deleted", not retired.exists(),
            "deletion propagation removed it" if not retired.exists() else "retired rule still on disk", artifact=g.repo_art)

    # BL-036 Wave B: upgrade is also a scaffold for artifacts the repo
    # never had — the v17 fixture predates docs/cases/, so the upgrade run
    # must create the case home (found unasserted by review 2026-08-21).
    missing_artifacts = [a for a in SCAFFOLD_ARTIFACTS if not (repo / a).exists()]
    g.check("upgrade_creates_missing_artifacts", not missing_artifacts,
            "all Step 4 artifacts exist after upgrade (derived list)" if not missing_artifacts
            else f"upgrade failed to scaffold: {missing_artifacts}", artifact=g.repo_art)

    # BL-036 Wave B: the keep-refusal branch — when the run's prompt (saved
    # by the runner to outputs/prompt.txt) asks to protect an OWNED path,
    # the skill must refuse with a reason under ## Keep list.
    prompt_art = Artifact.file("prompt:upgrade", home / "outputs" / "prompt.txt")
    if not prompt_art.measurable:
        # The gate's own input is gone. That is not "the branch does not
        # apply" — it is unmeasured, and dropping the assert here is exactly
        # how a whole branch leaves the corpus with nobody the wiser.
        g.check("keep_refusal_for_owned_path", False, "", artifact=prompt_art)
    elif "protect docs/ai/rules/core/okf.md" in prompt_art.path.read_text():
        refusal = re.search(r"## Keep list\n(.*?)(?=\n#|\Z)", report, re.S | re.M)
        seg = refusal.group(1) if refusal else ""
        refused = "okf.md" in seg and "owned" in seg.lower()
        g.check("keep_refusal_for_owned_path", refused,
                "owned-path keep request refused with a reason" if refused
                else "no refusal recorded for the owned-path keep request", artifact=g.report_art)

    # Project-owned files must be untouched AT THEIR PATH — the set is
    # derived from the file-authority matrix (BL-038): every class whose
    # upgrade cell is a path-protecting right. AGENTS.md is absent because
    # its cell is content-protected (propose-only); its content, not its
    # path, is checked by mode_respects_authority above.
    try:
        protected = protected_project_files(repo, fixture_meta=meta)
        touched = [p for p in git(repo, "diff", "HEAD", "--name-only").splitlines() if p in protected]
        g.check("project_owned_files_untouched", not touched,
                "no tracked project-owned file modified" if not touched else f"modified: {touched}", artifact=g.repo_art)
    except ValueError as e:
        g.check("project_owned_files_untouched", False, str(e), artifact=g.repo_art)
    return g



def engine_audit_findings(repo: Path) -> list[str]:
    """The engine's own mechanical finding lines for `repo`, re-run at grade
    time on the same tree (audit is zero-writes, so run-time and grade-time
    trees are identical). v23 BL-066: the report must carry every one."""
    r = subprocess.run(
        [sys.executable, str(SKILL / "assets/engine/engine.py"), "audit",
         "--root", str(repo), "--skill", str(SKILL)],
        capture_output=True, text=True)
    if r.returncode not in (0, 1):
        raise RuntimeError(f"engine audit re-run failed ({r.returncode}): "
                           f"{r.stderr[:300]}")
    return [l for l in r.stdout.splitlines() if l.startswith("- [")]


AUDIT_STAMP = "Emitted by docs/ai/engine.py audit — constitution v"


def check_engine_backed_report(g: "Grader", repo: Path, report: str) -> None:
    """v23 BL-066 (R-661..R-663): the audit report is the engine's print."""
    g.check("audit_report_carries_engine_stamp", AUDIT_STAMP in report,
            "engine stamp present" if AUDIT_STAMP in report
            else "no emitter stamp — the report was not engine-printed",
            artifact=g.report_art)
    engine_lines = engine_audit_findings(repo)
    missing = [l for l in engine_lines if l not in report]
    g.check("audit_mechanical_findings_match_engine", not missing,
            f"all {len(engine_lines)} engine finding lines present verbatim"
            if not missing else f"engine lines absent from the report: "
            f"{missing[:3]!r} (+{max(0, len(missing) - 3)} more)",
            artifact=g.report_art)
    m = re.search(r"^## Warning\s*\n(.*?)(?=^## |\Z)", report, re.S | re.M)
    warn = m.group(1) if m else ""
    displaced = [slug for slug in ("project-rules", "stray-rulebooks")
                 if f"[{slug}]" in report and f"[{slug}]" not in warn]
    g.check("model_findings_in_pinned_sections", not displaced,
            "model findings sit inside ## Warning" if not displaced
            else f"model findings outside their section: {displaced}",
            artifact=g.report_art)

def grade_audit(ws: Path) -> Grader:
    home = ws / "rotted-layer"
    repo = home / "repo"
    meta = json.loads((home / "fixture_meta.json").read_text())
    g = Grader(repo=repo, report=home / "outputs" / "audit-report.md",
               home=home, label="audit")
    check_mode_authority(g, repo, "audit", meta)

    report = report_text(g)
    g.probe("audit_report_saved", g.report_art, container=g.home_art)

    # BL-036 Wave B: the report must live OUTSIDE the repo — the audit's
    # zero-writes contract means even its own output may not land in the
    # tree (the zero_writes check below would catch a written file, but
    # this names the intent explicitly).
    inside = [p.name for p in (repo / "docs").rglob("*report*")]
    g.check("audit_report_outside_repo", not inside,
            "no report artifacts inside the repo" if not inside
            else f"report written into the repo: {inside}", artifact=g.repo_art)

    for marker in meta["report_markers"]:
        g.check(f"report names {marker!r}", marker in report,
                "named in report" if marker in report else "absent from report", artifact=g.report_art)

    # Parity law (BL-036 Wave B): every audit check the law pins must have
    # a planted defect exercising it, and vice versa. Derived check slugs
    # vs slug-markers in the fixture — a new check without its defect (or
    # an orphaned marker) is red at grade time, not discovered by rot.
    law_slugs = audit_check_slugs()
    covered = set(meta.get("check_slugs_covered", []))
    uncovered = law_slugs - covered
    orphaned = covered - law_slugs
    parity_ok = bool(law_slugs) and not uncovered and not orphaned
    g.check("parity_every_check_has_a_defect", parity_ok,
            f"all {len(law_slugs)} law checks exercised by a planted defect" if parity_ok
            else f"checks with no planted defect: {sorted(uncovered)}; "
                 f"markers for no law check: {sorted(orphaned)}", artifact=LAW_SKILL)

    # BL-025 item 2: severity-anchored presence — the marker must appear
    # inside the section under its pinned severity heading (## <Severity>
    # up to the next heading), not merely anywhere in the report.
    for marker, severity in meta.get("severity_anchored_markers", []):
        m = re.search(rf"^## {re.escape(severity)}\s*\n(.*?)(?=^## |\Z)", report,
                      re.S | re.M)
        section = m.group(1) if m else ""
        anchored = marker in section
        g.check(f"report anchors {marker!r} under ## {severity}", anchored,
                f"present in the {severity} section" if anchored
                else f"not under ## {severity} (heading found={bool(m)})", artifact=g.report_art)

    for marker in meta.get("absent_markers", []):
        g.check(f"report does NOT contain {marker!r}", marker not in report,
                "correctly absent" if marker not in report else "false-positive finding present", artifact=g.report_art)

    # Scoped to the candidates section: findings may name these statements
    # legitimately, but proposing them as fleet candidates is a failure.
    m = re.search(r"## Constitution candidates\n(.*?)(?=\nClean checks:|\n#|\Z)", report, re.S)
    section = m.group(1) if m else ""
    for marker in meta.get("candidate_absent_markers", []):
        g.check(f"candidates section does NOT contain {marker!r}",
                marker not in section,
                "correctly not proposed" if marker not in section
                else "non-candidate statement proposed as fleet law", artifact=g.report_art)

    check_engine_backed_report(g, repo, report)

    status = git(repo, "status", "--porcelain").strip()
    head = git(repo, "rev-parse", "HEAD").strip()
    clean = not status and head == meta["fixture_head"]
    g.check("zero_writes", clean,
            "working tree untouched, HEAD identical to fixture" if clean
            else f"status={status[:200]!r}, HEAD={head} (expected {meta['fixture_head']})", artifact=g.repo_art)
    return g


def report_has_heal_item(report: str) -> bool:
    """True when the restructure report carries a `[heal]` plan item — the
    law's delegation of the owned layer to the upgrade column. Anchored
    to a numbered item line: a prose mention ("no [heal] needed") does not
    unlock the upgrade column."""
    return re.search(r"^\s*\d+\. \[heal\]", report, re.M) is not None


def grade_audit_engine_absent(ws: Path) -> Grader:
    """BL-051 item 5b. Check 15 carries a branch for "bundle present, engine
    absent → Info" that no fixture reached, so it was law nobody could
    falsify. This scenario is the smallest thing that can: a repo legislated
    at v19, OKF bundle on disk, no docs/ai/engine.py.

    The assert reads the AUDIT REPORT, not the repo — naming the artifact is
    what keeps it from reporting a confident wrong diagnosis (POLICY §3)."""
    sc = ws / "audit-engine-absent"
    repo = sc / "repo"
    g = Grader(repo=repo, report=sc / "outputs" / "audit-report.md",
               home=sc, label="audit-engine-absent")

    report = report_text(g)
    g.probe("audit_report_saved", g.report_art, container=g.home_art)

    # v23 BL-066: this repo has no delivered engine, so the stamp also
    # proves the model reached for the skill package's own copy.
    g.check("audit_report_carries_engine_stamp", AUDIT_STAMP in report,
            "engine stamp present" if AUDIT_STAMP in report
            else "no emitter stamp — the report was not engine-printed",
            artifact=g.report_art)

    # The fixture's premise, asserted rather than assumed: if a future edit
    # ships the engine here, every check below would pass for the wrong
    # reason and the branch would silently stop being measured.
    engine_absent = not (repo / "docs/ai/engine.py").exists()
    bundle_present = (repo / "docs/okf").is_dir()
    g.check("fixture_state_is_bundle_without_engine", engine_absent and bundle_present,
            "bundle present, engine absent"
            if engine_absent and bundle_present
            else f"engine_absent={engine_absent} bundle_present={bundle_present}", artifact=g.repo_art)

    info = re.search(r"^## Info\s*\n(.*?)(?=^## |\Z)", report, re.S | re.M)
    info_section = info.group(1) if info else ""
    named = "okf-anchors" in info_section and "docs/ai/engine.py" in info_section
    g.check("check15_engine_absent_info", named,
            "check 15's engine-absent Info line present under ## Info"
            if named else f"not under ## Info (heading found={bool(info)})", artifact=g.report_art)

    # Negative control: an absent engine is Info, never an anchors Warning.
    # An assert that only checks presence is passed by an agent that reports
    # the Info line AND invents anchor findings it cannot have measured.
    warn = re.search(r"^## Warning\s*\n(.*?)(?=^## |\Z)", report, re.S | re.M)
    warn_section = warn.group(1) if warn else ""
    no_false_warning = "okf-anchors" not in warn_section and "okf-sync-debt" not in warn_section
    g.check("no_anchor_warning_without_an_engine", no_false_warning,
            "no anchors/debt Warning — nothing was measured, so nothing is claimed"
            if no_false_warning else "reported anchor findings with no engine to produce them", artifact=g.report_art)

    g.check("zero_writes", not git(repo, "status", "--porcelain").strip(),
            "git status clean after a read-only audit", artifact=g.repo_art)
    return g


def grade_restructure(ws: Path) -> Grader:
    home = ws / "restructure"
    repo = home / "repo"
    meta = json.loads((home / "fixture_meta.json").read_text())
    g = Grader(repo=repo, report=home / "outputs" / "restructure-report.md",
               home=home, label="restructure")
    report = report_text(g)

    # A `[heal]` item is the law's own delegation of the owned layer to the
    # upgrade column (`references/restructure.md` §2) — those writes are
    # not restructure's, so they are judged by the column heal invokes.
    # No heal in the plan, no delegation: never-touch keeps its teeth.
    # Declared against the REPORT, not the tree, although it reads both:
    # the delegation gate above is derived from the report, so with no
    # readable report this assert would judge a lawful healed tree against
    # restructure's own never-touch column and report a violation it never
    # measured. Where an assert reads two artifacts, it declares the one
    # whose absence would make its verdict wrong.
    check_mode_authority(g, repo, "restructure", meta,
                         delegated=restructure_heal_delegates()
                         if report_has_heal_item(report) else None,
                         art=g.report_art)

    g.probe("restructure_report_saved", g.report_art, container=g.home_art)

    # Audit check 2 is Critical and filling `{{TOKEN}}`s is inside
    # restructure's closed `fix` scope, but only fresh/migration graded it —
    # so a run could leave the planted {{PROJECT_OVERVIEW}} unresolved and
    # still score 100%. The idempotency pass is what exposed it: run 1 left
    # the token, run 2 filled it, and the second run wrote (found
    # 2026-08-22). Third time this cycle that idempotency caught what the
    # corpus asserts missed.
    g.no_unresolved_tokens(repo)

    for s in meta["fidelity_sentences"]:
        # -i: the law's carve-outs lawfully REFORMAT lines while carrying
        # them ("definitions become glossary rows") — a sentence-initial
        # lowercase word becomes table-capitalized. Fidelity means the
        # concept survived, not the casing (observed 2026-08-21).
        hits = subprocess.run(
            ["grep", "-rli", "--exclude-dir=.git", s, str(repo)],
            capture_output=True, text=True).stdout.strip()
        g.check(f"fidelity: {s[:44]!r}", bool(hits),
                f"survives in {hits.splitlines()}" if hits
                else "lost — appears nowhere in the repo", artifact=g.repo_art)

    kept = repo / meta["kept_path"]
    kept_ok = kept.exists() and kept.read_text() == meta["kept_content"]
    g.check("kept_file_untouched_in_place", kept_ok,
            "byte-identical at original path" if kept_ok
            else "kept file moved, edited, or deleted", artifact=g.repo_art)

    # The entry document under whichever name it currently carries. Reading
    # only AGENTS.md reported a *missing* file as "the conflict line was
    # auto-resolved" — a confident wrong diagnosis for a real but entirely
    # different defect, and it also made ghost_import_fixed pass trivially
    # whenever AGENTS.md was absent (found 2026-08-22).
    agents_f, claude_f = repo / "AGENTS.md", repo / "CLAUDE.md"
    entry_f = agents_f if agents_f.exists() else claude_f
    claude = entry_f.read_text() if entry_f.exists() else ""
    conflict_kept = meta["conflict_marker"] in claude
    g.check("conflict_not_auto_resolved", conflict_kept,
            f"conflicting line still in {entry_f.name}" if conflict_kept
            else (f"conflict line gone from {entry_f.name} — auto-resolved "
                  "without the user" if entry_f.exists()
                  else "no entry document on disk at all"), artifact=g.repo_art)

    # v14 file model: a real CLAUDE.md with no AGENTS.md is renamed, with
    # CLAUDE.md left as a symlink — the law pins this inside restructure's
    # closed `fix` scope, so it is applied, never proposed. Nothing asserted
    # it, so a run that skipped the canonicalization stayed green here and
    # surfaced only as the misleading failure above.
    v14_ok = (agents_f.exists() and not agents_f.is_symlink()
              and claude_f.is_symlink())
    g.check("v14_model_canonicalized", v14_ok,
            "AGENTS.md canonical, CLAUDE.md a symlink to it" if v14_ok
            else f"AGENTS.md exists={agents_f.exists()}, "
                 f"CLAUDE.md is symlink={claude_f.is_symlink()}", artifact=g.repo_art)
    decision_open = "[decision]" in report and "We do not maintain CHANGELOG.md" in report
    g.check("conflict_surfaced_as_decision", decision_open,
            "[decision] item names the conflict" if decision_open
            else "report lacks a [decision] item naming the conflict", artifact=g.report_art)

    pr_path = repo / meta["project_rule_conflict_path"]
    pr_ok = pr_path.exists() and pr_path.read_text() == meta["project_rule_conflict_content"]
    pr_named = meta["project_rule_conflict_path"] in report
    g.check("project_rule_conflict_decision_gated", pr_ok and pr_named,
            "conflicting project rule byte-unchanged and named in the report"
            if pr_ok and pr_named
            else f"file untouched={pr_ok}, named in report={pr_named}", artifact=g.report_art)

    rows = glossary_rows(repo)
    g.check("glossary_healed_with_terms", rows >= 1,
            f"glossary seeded with {rows} term row(s) by the fix item" if rows >= 1
            else "glossary still has zero body rows after restructure", artifact=g.repo_art)
    gl_named = "glossary" in report.lower()
    g.check("glossary_heal_in_plan", gl_named,
            "plan/report names the glossary item" if gl_named
            else "report never mentions the glossary", artifact=g.report_art)

    fg = repo / meta["foreign_glossary_path"]
    g.check("foreign_glossary_merged_away", not fg.exists(),
            f"{meta['foreign_glossary_path']} removed after merge" if not fg.exists()
            else f"{meta['foreign_glossary_path']} still on disk", artifact=g.repo_art)
    gl_text = (repo / "docs/okf/glossary.md").read_text() if (repo / "docs/okf/glossary.md").exists() else ""
    def_in_gl = meta["foreign_glossary_definition"].lower() in gl_text.lower()
    g.check("foreign_definition_in_okf_glossary", def_in_gl,
            "instance definition lives in docs/okf/glossary.md" if def_in_gl
            else "definition not merged into the OKF glossary", artifact=g.repo_art)

    skf = repo / meta["skills_rules_path"]
    skf_ok = skf.exists() and skf.read_text() == meta["skills_rules_content"]
    # BL-025 item 6 + 2026-08-21 equivalence: the protected value is
    # routing-to-the-owner — skills.md byte-unchanged AND the finding
    # surfaced as not-applied. The law's canonical form is the
    # "## For the team:" section; a plan line explicitly marked
    # "— skipped (For the team)" satisfies the same value (observed
    # stable across the final-law series) and is accepted.
    ftt = re.search(r"^## For the team:\s*\n(.*?)(?=^Kept \(immovable\)|^#{1,2} |\Z)", report,
                    re.S | re.M)
    ftt_section = ftt.group(1) if ftt else ""
    routed_in_section = "made-up-skill" in ftt_section
    routed_as_skipped = re.search(
        r"made-up-skill[^\n]*— skipped \(For the team\)", report) is not None
    named = routed_in_section or routed_as_skipped
    g.check("skill_binding_for_the_team_not_a_plan_item", skf_ok and named,
            "skills.md byte-unchanged, finding routed to the team"
            if skf_ok and named
            else f"file untouched={skf_ok}, routed to team={named}", artifact=g.report_art)

    stray = repo / meta["stray_rulebook_path"]
    g.check("stray_rulebook_merged_away", not stray.exists(),
            "stray rulebook removed after merge" if not stray.exists()
            else f"{meta['stray_rulebook_path']} still on disk", artifact=g.repo_art)
    pr_dir = repo / ".claude/rules"
    law_hits = subprocess.run(
        ["grep", "-rl", meta["stray_project_law"], str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("stray_law_merged_to_project_rules", bool(law_hits),
            f"stray rulebook law lives in {law_hits.splitlines()}" if law_hits
            else "stray rulebook law not merged into .claude/rules/", artifact=g.repo_art)

    moved_ok = (not (repo / ".claude/plans").exists()
                and (repo / "docs/superpowers/plans/2026-01-importer-plan.md").exists())
    g.check("plans_relocated_to_standard_home", moved_ok,
            ".claude/plans/ gone, file at docs/superpowers/plans/ (legacy home — stray non-case plans stay there)" if moved_ok
            else "plan file not moved (or old dir left behind)", artifact=g.repo_art)

    # BL-036 Wave B: the misplaced case directory (docs/superpowers/BL-0007)
    # must reach the case home per §1's cases row — content preserved.
    case_src = repo / "docs/superpowers/BL-0007/plan.md"
    case_dst = repo / "docs/cases/BL-0007/plan.md"
    case_moved = (not case_src.exists() and case_dst.exists()
                  and "sequential per tenant" in case_dst.read_text())
    g.check("misplaced_case_relocated_to_case_home", case_moved,
            "BL-0007 lives in docs/cases/ with content intact" if case_moved
            else f"src gone={not case_src.exists()}, dst ok={case_dst.exists()}", artifact=g.repo_art)
    g.check("cursorrules_merged_away", not (repo / ".cursorrules").exists(),
            ".cursorrules removed after merge" if not (repo / ".cursorrules").exists()
            else ".cursorrules still present", artifact=g.repo_art)
    g.check("ghost_import_fixed", "ghost-rule.md" not in claude,            "dangling import gone" if "ghost-rule.md" not in claude
            else "ghost-rule import still in AGENTS.md", artifact=g.repo_art)

    # v20: anchor and debt findings are owner prose, not wiring. The closed
    # `fix` scope forbids touching them, so they route to "For the team"
    # and both documents stay byte-identical. Reuses ftt_section (computed
    # above for the skill-bindings check) rather than recomputing an
    # unbounded slice of `report` — an unbounded split would also match a
    # slug appearing after a later heading or the `Kept (immovable):` block.
    for marker in ("okf-anchors", "okf-sync-debt"):
        g.check(f"{marker.replace('-', '_')}_routed_to_team", marker in ftt_section,
                f"{marker} listed under For the team" if marker in ftt_section
                else f"{marker} missing from the For the team section", artifact=g.report_art)
    for path, expected in meta.get("okf_untouched", {}).items():
        doc = repo / path
        intact = doc.exists() and doc.read_text() == expected
        g.check(f"okf_{Path(path).stem}_unedited", intact,
                f"{path} byte-identical to its planted content" if intact
                else f"{path} was rewritten or removed", artifact=g.repo_art)

    src = SKILL / "assets/rules/core/okf.md"
    okf = repo / "docs/ai/rules/core/okf.md"
    healed = okf.exists() and okf.read_bytes() == src.read_bytes()
    g.check("owned_drift_healed", healed,
            "okf.md byte-identical to skill source" if healed
            else "owned drift not healed via Steps 2-3", artifact=g.repo_art)

    version = int((SKILL / "VERSION").read_text().strip())
    mpath = repo / "docs/ai/manifest.json"
    manifest = json.loads(mpath.read_text()) if mpath.exists() else None
    heal_ok = bool(manifest and manifest.get("legislatorVersion") == version
                   and {"path": meta["kept_path"], "reason": "works as-is"}
                   in (manifest.get("keep") or []))
    g.check("manifest_healed_keep_carried", heal_ok,
            f"manifest at v{version}, keep entry carried" if heal_ok
            else "manifest missing, stale, or keep entry dropped", artifact=g.manifest_art)

    orphan = repo / "docs/okf/orphan-notes.md"
    refs = subprocess.run(
        ["grep", "-rl", "--exclude-dir=.git", "--include=*.md",
         "orphan-notes.md", str(repo)],
        capture_output=True, text=True).stdout.strip().splitlines()
    linked = orphan.exists() and any(Path(r) != orphan for r in map(Path, refs))
    # Law B (2026-08-21) also accepted a second lawful outcome: the orphan
    # left unlinked with an open [decision] proposing its deletion. The
    # idempotency pass showed the price of two lawful outcomes — run 1 filed
    # the decision, run 2 linked the file, so the second run wrote and the
    # zero-diff promise broke; worse, a decision a previous run had left OPEN
    # was silently reclassified into an auto-applied item under a blanket
    # approval, because reports do not live in the repo and the later run had
    # no trace of the earlier ruling (found 2026-08-22). The law now pins
    # `link` as unconditional, so exactly one outcome is lawful: the orphan
    # survives AND something references it.
    g.check("orphan_linked_not_deleted", linked,
            "orphan linked into the layer" if linked
            else ("orphan still unreferenced — link is unconditional for a "
                  "check-7 orphan" if orphan.exists()
                  else "orphan deleted by the run"), artifact=g.repo_art)

    # BL-036 Wave B: post-state asserts — the [link] outcome must be visible
    # in the index (not only named in the plan), and the stale map row must
    # be gone from the map (post-state, not report-only).
    if linked:
        idx_text = (repo / "docs/okf/index.md").read_text() if (repo / "docs/okf/index.md").exists() else ""
        g.check("link_post_state_in_index", "orphan-notes.md" in idx_text,
                "index.md references the linked orphan" if "orphan-notes.md" in idx_text
                else "[link] applied but the index does not reference the file", artifact=g.repo_art)
    map_text = (repo / "docs/okf/codebase-map.md").read_text() if (repo / "docs/okf/codebase-map.md").exists() else ""
    g.check("stale_map_row_gone", "legacy/" not in map_text,
            "stale legacy/ row removed from codebase-map" if "legacy/" not in map_text
            else "stale row still in codebase-map.md", artifact=g.repo_art)

    fid = "Fidelity: verified" in report
    g.check("fidelity_line_reported", fid,
            "report carries the pinned fidelity line" if fid
            else "no 'Fidelity: verified' line in the report", artifact=g.report_art)
    return g


def grade_idempotency(ws: Path, scenario: str) -> Grader:
    repo = ws / scenario / "repo"
    g = Grader(repo=repo, home=ws / scenario, label=f"idempotency:{scenario}")
    status = git(repo, "status", "--porcelain").strip()
    diff = git(repo, "diff", "HEAD", "--stat").strip()
    clean = not status and not diff
    g.check("second_run_zero_diff", clean,
            "re-run produced no spurious diff" if clean else f"status: {status[:300]!r} diff: {diff[:300]!r}", artifact=g.repo_art)
    return g


def grade_derivation_selftest() -> Grader:
    """BL-036 Wave A: prove the derived contracts track the skill source.
    If someone hand-edits a stale list back in or the source moves, these
    invariants go red — divergence becomes impossible to miss. No agent
    run: pure derivation checks."""
    g = Grader(label="selftest:derivation")
    g.check("scaffold_artifacts_derived_nonempty", len(SCAFFOLD_ARTIFACTS) >= 10,
            f"{len(SCAFFOLD_ARTIFACTS)} targets parsed from Step 4's table", artifact=LAW_SKILL)
    g.check("scaffold_artifacts_include_cases_home",
            "docs/cases/README.md" in SCAFFOLD_ARTIFACTS,
            "the v17 case home is in the derived list", artifact=LAW_SKILL)
    # File authority (BL-038): the table parses to the pinned shape, and
    # the state header says which repo state each mode assumes.
    try:
        matrix = authority_matrix()
        states = authority_states()
        shape_err = ""
    except ValueError as e:
        matrix, states, shape_err = {}, {}, str(e)
    g.check("authority_matrix_shape",
            not shape_err and len(matrix) == len(AUTHORITY_CLASSES) * len(AUTHORITY_MODES),
            f"{len(matrix)} cells, all in the closed vocabulary" if not shape_err else shape_err, artifact=LAW_SKILL)
    g.check("authority_states_pinned",
            states == {"scaffold": "installing", "migrate": "installing",
                       "upgrade": "maintaining", "restructure": "maintaining",
                       "audit": "inspecting"},
            f"states: {states}" if states else shape_err, artifact=LAW_SKILL)
    # Every content-protecting cell must name a class with a canonical
    # file, or check_mode_authority has nothing to compare content
    # against and would raise at grade time instead of at this selftest.
    if matrix:
        uncovered_content_cells = [
            (cls, mo) for (cls, mo), right in matrix.items()
            if right in CONTENT_PROTECTING_RIGHTS and cls not in CANONICAL_FILE
        ]
        g.check("content_protected_rights_have_canonical_file",
                not uncovered_content_cells,
                "every content-protecting cell's class has a canonical file"
                if not uncovered_content_cells
                else f"no canonical file for: {uncovered_content_cells}", artifact=LAW_SKILL)
    else:
        g.check("content_protected_rights_have_canonical_file", False, shape_err, artifact=LAW_SKILL)
    # Two directions: AGENTS.md is absent from the PATH-protected set
    # because its cell is content-protected (propose-only is in
    # CONTENT_PROTECTING_RIGHTS, not PATH_PROTECTING_RIGHTS) — its content,
    # not its path, is what check_mode_authority guards. Flip the cell to
    # read-only (a path-protecting right) on a patched copy of the matrix
    # and AGENTS.md must come back into the path-protected set. If either
    # direction fails, the set is not being derived from the table.
    if matrix:
        flipped = dict(matrix)
        flipped[("entry document", "upgrade")] = "read-only"
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td) / "repo"
            shutil.copytree(EVALS / "fixtures" / "upgrade-base", scratch)
            (scratch / "AGENTS.md").write_text("# stub\n")
            subprocess.run(["git", "-C", str(scratch), "init", "-q"], check=True)
            subprocess.run(["git", "-C", str(scratch), "add", "-A"], check=True)
            real = protected_project_files(scratch, None, matrix)
            patched = protected_project_files(scratch, None, flipped)
        derived = ("AGENTS.md" not in real
                   and matrix[("entry document", "upgrade")] == "propose-only"
                   and "AGENTS.md" in patched)
        g.check("protected_set_derived_from_cells", derived,
                f"real={'AGENTS.md' in real}, flipped={'AGENTS.md' in patched}", artifact=FIXTURE_UPGRADE_BASE)
    else:
        g.check("protected_set_derived_from_cells", False, shape_err, artifact=FIXTURE_UPGRADE_BASE)
    # Symlink-at-HEAD regression coverage (Important finding #2, Task 5 fix
    # round 2): the 100644-vs-120000 filter in _head_real_file_content has
    # no automated coverage without this. Build a tiny repo whose HEAD is
    # already the v14 steady state — AGENTS.md a real file, CLAUDE.md its
    # symlink — and prove check_mode_authority behaves correctly in both
    # directions: untouched passes; an edit to AGENTS.md is flagged by name
    # (not by CLAUDE.md's symlink entry, which the filter must ignore).
    with tempfile.TemporaryDirectory() as td:
        sym_repo = Path(td) / "repo"
        sym_repo.mkdir()
        subprocess.run(["git", "-C", str(sym_repo), "init", "-q"], check=True)
        (sym_repo / "AGENTS.md").write_text("# A\n")
        (sym_repo / "CLAUDE.md").symlink_to("AGENTS.md")
        subprocess.run(["git", "-C", str(sym_repo), "add", "-A"], check=True)
        subprocess.run(["git", "-C", str(sym_repo),
                        "-c", "user.email=eval@local", "-c", "user.name=eval",
                        "commit", "-q", "-m", "seed: v14 steady state"], check=True)

        sym_art = Artifact.repo("synthetic:v14-steady-state", sym_repo)
        g_untouched = Grader(label="selftest:synthetic")
        check_mode_authority(g_untouched, sym_repo, "upgrade", art=sym_art)
        untouched_exp = g_untouched.exps[0]
        untouched_ok = untouched_exp["passed"]

        (sym_repo / "AGENTS.md").write_text("# A\nextra\n")
        g_edited = Grader(label="selftest:synthetic")
        check_mode_authority(g_edited, sym_repo, "upgrade", art=sym_art)
        edited_exp = g_edited.exps[0]
        edited_flagged = (not edited_exp["passed"]
                          and "AGENTS.md content changed" in edited_exp["evidence"])

        symlink_ok = untouched_ok and edited_flagged
        g.check("content_check_ignores_symlink_at_head", symlink_ok,
                f"untouched: {'ok' if untouched_ok else 'FAIL ' + untouched_exp['evidence']}; "
                f"content-changed: {'flagged' if edited_flagged else 'FAIL ' + edited_exp['evidence']}", artifact=GRADER_SELF)
    wiring = migration_wiring()
    g.check("migration_wiring_derived_from_template",
            "@docs/okf/codebase-map.md" in wiring and "## Boundaries" in wiring,
            f"{len(wiring)} wiring strings parsed from AGENTS.md.tpl", artifact=LAW_AGENTS_TPL)
    sev = audit_check_severities()
    g.check("audit_severities_derived",
            len(sev) >= 14 and sev.get("Owned-layer integrity") == "Critical",
            f"{len(sev)} checks with parsed severities", artifact=LAW_SKILL)
    slugs = audit_check_slugs()
    g.check("audit_slugs_derived",
            len(slugs) == len(sev) and "imports-resolve" in slugs,
            f"{len(slugs)} pinned slugs parsed, one per severity-carrying check", artifact=LAW_SKILL)
    actions = restructure_actions()
    g.check("restructure_actions_derived",
            actions == {"move", "merge", "link", "fix", "heal", "decision"},
            f"closed action set parsed: {sorted(actions)}", artifact=LAW_RESTRUCTURE)
    # BL-041 hardenings. A ninth body row must be rejected, not parsed past:
    # the table is exact-shape, and an extra class with no derived rights
    # is the silent failure the matrix exists to prevent.
    nine = _skill_md().replace(
        "| kept paths (manifest `keep`) | link-only | link-only | link-only | link-only | read-only |\n",
        "| kept paths (manifest `keep`) | link-only | link-only | link-only | link-only | read-only |\n"
        "| ninth row (unreviewed) | replace | replace | replace | replace | read-only |\n")
    try:
        authority_matrix(nine)
        ninth_rejected = False
    except ValueError:
        ninth_rejected = True
    g.check("authority_matrix_rejects_ninth_row", ninth_rejected,
            "a ninth body row raises" if ninth_rejected else "a ninth body row parsed silently", artifact=LAW_SKILL)
    # The heal delegation gate reads a plan item, not a bare substring: a
    # report that merely *mentions* `[heal]` in prose ("no [heal] needed")
    # must not unlock the upgrade column for restructure's writes.
    prose_only = "## Plan\n\n1. [fix] fill token\n\nNote: no [heal] item was needed.\n"
    item = "## Plan\n\n1. [fix] fill token\n2. [heal] refresh owned law\n"
    gate_ok = not report_has_heal_item(prose_only) and report_has_heal_item(item)
    g.check("heal_gate_anchored_to_item_line", gate_ok,
            "prose mention ignored, plan item honoured" if gate_ok
            else f"prose-only={report_has_heal_item(prose_only)}, item={report_has_heal_item(item)}", artifact=GRADER_SELF)
    # §2's heal bullet is the only place the law delegates a class to
    # another mode's column; the grader reads the delegation there instead
    # of restating it. Every class Steps 2-3 write must carry its cell
    # reference in that bullet, or mode_respects_authority will judge a
    # lawful delegated write against restructure's own column.
    heal = restructure_heal_delegates()
    heal_ok = heal == {"owned law": "upgrade", "manifest": "upgrade"}
    g.check("heal_delegation_derived", heal_ok,
            f"heal delegates {heal} to the upgrade column" if heal_ok
            else f"heal bullet delegates {heal} — expected owned law and manifest to the upgrade column", artifact=LAW_RESTRUCTURE)
    # §1 lock (BL-036 Wave B): the standard-layout table must carry the
    # cases row and the legacy qualifiers — the grader's relocation
    # expectations are only lawful while the table says so.
    layout = (SKILL / "references/restructure.md").read_text().split("## 1.", 1)[1].split("## 2.", 1)[0]
    g.check("layout_table_has_cases_row",
            "| Case files (any `BL-NNN` directory) | `docs/cases/BL-NNN/`" in layout,
            "§1 maps case directories to docs/cases/", artifact=LAW_RESTRUCTURE)
    g.check("layout_table_marks_legacy_homes",
            "**legacy home**" in layout,
            "§1 qualifies superpowers rows as legacy homes", artifact=LAW_RESTRUCTURE)
    return g


def grade_upgrade_drop_stack(ws: Path) -> Grader:
    """BL-036 Wave B: the only deletion-semantics-by-stack branch — the
    prompt drops aurelia from a dotnet+aurelia subscription."""
    home = ws / "upgrade-drop-stack"
    repo = home / "repo"
    meta = json.loads((home / "fixture_meta.json").read_text())
    g = Grader(repo=repo, report=home / "outputs" / "upgrade-report.md",
               home=home, label="upgrade-drop-stack")
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []), fixture_meta=meta)
    check_mode_authority(g, repo, "upgrade", meta)

    dropped_left = [p for p in meta["dropped_stack_files"] if (repo / p).exists()]
    g.check("dropped_stack_files_deleted", not dropped_left,
            f"{meta['dropped_stack']} owned files removed" if not dropped_left
            else f"still on disk: {dropped_left}", artifact=g.repo_art)

    dotnet_dir = repo / "docs/ai/rules/stacks/dotnet"
    dotnet_left = sorted(p.name for p in dotnet_dir.glob("*.md")) if dotnet_dir.is_dir() else []
    expected_dotnet = sorted((SKILL / "assets/rules/stacks/dotnet").glob("*.md"))
    g.check("kept_stack_untouched_and_refreshed",
            dotnet_left == sorted(f.name for f in expected_dotnet),
            f"dotnet rules present and refreshed ({len(dotnet_left)})" if
            dotnet_left == sorted(f.name for f in expected_dotnet)
            else f"dotnet mismatch: {dotnet_left}", artifact=g.repo_art)

    g.probe("upgrade_report_saved", g.report_art, container=g.home_art)
    # No `if has_report:` guard any more. An assert that is not emitted is an
    # assert nobody can see was not measured — the same silence POLICY §1b
    # exists to end. It is emitted, it declares the report, and it comes out
    # `unmeasured` when there is no report to read.
    report = report_text(g)
    review_idx = report.find("eeds your review")
    proposed = review_idx >= 0 and "stacks/aurelia" in report[review_idx:]
    g.check("report_proposes_aurelia_import_removal", proposed,
            "aurelia import removal proposed under Needs your review" if proposed
            else "no proposal to remove the aurelia import line", artifact=g.report_art)
    return g


def grade_case_practice(ws: Path) -> Grader:
    """BL-036 Wave C — the acceptance test: a fresh agent EXECUTES
    core/sdd.md on ordinary feature work. Graded: the practice (case
    home, tier header, EARS ids, hurting case, task traceability,
    converge trail). Not graded: the code itself."""
    home = ws / "case-practice"
    repo = home / "repo"
    g = Grader(repo=repo, home=home, label="case-practice")

    cases = sorted((repo / "docs/cases").glob("BL-*")) if (repo / "docs/cases").is_dir() else []
    case_dir = cases[0] if cases else None
    # The case the run was asked to write is itself an artifact, and the five
    # practice asserts below read that and nothing else. The early return
    # that used to stand here dropped them from the corpus without a trace —
    # a scenario reporting 1/1 where 7 asserts were due.
    case_art = Artifact.docs("case:case-practice", case_dir)
    g.probe("case_born_in_case_home", case_art, container=g.repo_art)

    all_text = ("\n".join(p.read_text(errors="ignore") for p in case_dir.rglob("*.md"))
                if case_art.measurable else "")

    tier = re.search(r"Tier:\s*([012])", all_text)
    g.check("tier_declared_in_case_header", tier is not None,
            f"tier {tier.group(1)} declared" if tier else "no 'Tier: N' line anywhere in the case", artifact=case_art)

    ears = re.findall(r"\bR-\d{3}\b", all_text)
    g.check("ears_lines_with_ids", len(set(ears)) >= 2,
            f"{len(set(ears))} distinct R-NNN ids" if len(set(ears)) >= 2
            else f"only {len(set(ears))} R-NNN id(s) — need >=2 EARS lines", artifact=case_art)

    # Order and proximity, not line layout: the law asks for "at least one
    # named GIVEN/WHEN/THEN scenario" and says nothing about where the line
    # breaks fall. The three-consecutive-lines form missed a correct hurting
    # case written as a wrapped paragraph (found 2026-08-22) — same class as
    # the "## For the team:" heading regex and the fidelity case-sensitivity
    # fix: measuring typography instead of the value. The bounded gaps keep
    # the match inside one scenario, so a stray GIVEN cannot pair with an
    # EARS line's WHEN/THEN elsewhere in the document.
    hurting = re.search(r"(?is)\bGIVEN\b.{0,600}?\bWHEN\b.{0,400}?\bTHEN\b",
                        all_text)
    g.check("gherkin_hurting_case_present", hurting is not None,
            "GIVEN/WHEN/THEN scenario present" if hurting
            else "no GIVEN/WHEN/THEN scenario in the case", artifact=case_art)

    per_trace = re.search(r"per\s+R-\d{3}", all_text)
    g.check("tasks_trace_per_rnnn", per_trace is not None,
            "at least one task traces 'per R-NNN'" if per_trace
            else "no task carries per R-NNN traceability", artifact=case_art)

    converged = ("Converged" in all_text) or re.search(r"\((?:missing|partial|contradicts|unrequested)\)", all_text)
    g.check("converge_trail_present", converged is not None,
            "converge statement or append-only gap findings present" if converged
            else "no converge trail — the case cannot lawfully close", artifact=case_art)

    # BL-043 (v22): the delivered engine's sdd-lint judges the agent's case
    # practice — the analyze gate's mechanical passes, run by the exact
    # artifact the constitution installed. Exit 0 is the contract: 2 means
    # the delivered engine predates the job (v21), 1 means the case tree
    # fails its own law's lint. Red against the v21 law by construction.
    eng = repo / "docs/ai/engine.py"
    if eng.exists():
        r = subprocess.run([sys.executable, "docs/ai/engine.py", "sdd-lint"],
                           cwd=repo, capture_output=True, text=True, timeout=60)
        lint_ok, lint_ev = r.returncode == 0, (
            "delivered engine lints the case tree clean" if r.returncode == 0
            else f"exit={r.returncode}: {(r.stdout or r.stderr)[:160]}")
    else:
        lint_ok, lint_ev = False, "docs/ai/engine.py not delivered"
    g.check("delivered_engine_sdd_lint_clean", lint_ok, lint_ev,
            artifact=g.repo_art)

    # the pre-existing README must survive untouched (create-once discipline)
    readme = repo / "docs/cases/README.md"
    g.check("case_home_readmark_untouched",
            readme.exists() and readme.read_text() == (SKILL / "assets/templates/cases-README.md.tpl").read_text(),
            "docs/cases/README.md byte-identical to the template" if readme.exists() else "README missing", artifact=g.repo_art)
    return g


# BL-042. The idempotency stage commits "run 1" into the fixture on purpose
# (`tools/evals-bg.sh:idem_scenario`), so a fixture that has moved off its
# `eval-base` tag is no longer the repo the corpus measured. Grading it again
# writes a verdict for a DIFFERENT state over the corpus verdict — how three
# v19 scenarios came to read 20/21 and 18/19 on the dashboard while
# `grade-history.jsonl` held the true 21/21 and 19/19 (2026-08-23).
# `eval-base` already exists for exactly this hazard (`setup_workspace.py`);
# this is the check that consults it.
SCENARIO_DIRS = {
    "fresh-scaffold-dotnet": "fresh-scaffold-dotnet",
    "legacy-migration": "legacy-migration",
    "legacy-migration-agents-first": "legacy-migration-agents-first",
    "upgrade-drop-stack": "upgrade-drop-stack",
    "case-practice": "case-practice",
    "upgrade": "upgrade",
    "audit": "rotted-layer",
    "audit-engine-absent": "audit-engine-absent",
    "restructure": "restructure",
}


def fixture_off_base(repo: Path) -> str | None:
    """None when the fixture sits at its `eval-base` tag (or the tag/repo is
    absent — an older workspace degrades to no check, visibly). Otherwise a
    one-line description of how far it has moved."""
    if not (repo / ".git").exists():
        return None
    try:
        base = git(repo, "rev-parse", "eval-base").strip()
    except Exception:
        return None
    if not base:
        return None
    head = git(repo, "rev-parse", "HEAD").strip()
    if head == base:
        return None
    ahead = git(repo, "rev-list", "--count", f"{base}..HEAD").strip() or "?"
    return f"HEAD {head[:7]} is {ahead} commit(s) past eval-base {base[:7]}"


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    ws = Path(sys.argv[1]).resolve()
    names = sys.argv[2:] or ["fresh-scaffold-dotnet", "legacy-migration", "upgrade", "audit", "restructure"]

    any_failed = False
    for name in names:
        # The guard runs BEFORE the grader reads the tree: a corpus verdict
        # for a moved fixture must not be computed, let alone written.
        # `idempotency:` is exempt — grading the committed run-1 state is
        # precisely its job.
        if name in SCENARIO_DIRS:
            moved = fixture_off_base(ws / SCENARIO_DIRS[name] / "repo")
            if moved:
                print(f"\n== {name}: REFUSED ==")
                print(f"  fixture has moved past its eval-base — {moved}")
                print("  The idempotency stage commits run 1 into the fixture by design,")
                print("  so a corpus grade taken now measures a different repo state and")
                print("  would overwrite the corpus verdict in grading.json.")
                print("  Read the authoritative verdict in")
                print(f"    {ws / SCENARIO_DIRS[name] / 'outputs' / 'grade-history.jsonl'}")
                print("  or restore the fixture with"
                      f" `git -C {ws / SCENARIO_DIRS[name] / 'repo'} reset --mixed eval-base`.")
                any_failed = True
                continue
        if name == "fresh-scaffold-dotnet":
            g, outdir = grade_fresh(ws), ws / SCENARIO_DIRS[name]
        elif name == "legacy-migration":
            g, outdir = grade_migration(ws), ws / SCENARIO_DIRS[name]
        elif name == "legacy-migration-agents-first":
            g, outdir = grade_migration_agents_first(ws), ws / SCENARIO_DIRS[name]
        elif name == "upgrade-drop-stack":
            g, outdir = grade_upgrade_drop_stack(ws), ws / SCENARIO_DIRS[name]
        elif name == "case-practice":
            g, outdir = grade_case_practice(ws), ws / SCENARIO_DIRS[name]
        elif name == "upgrade":
            g, outdir = grade_upgrade(ws), ws / SCENARIO_DIRS[name]
        elif name == "audit":
            g, outdir = grade_audit(ws), ws / SCENARIO_DIRS[name]
        elif name == "audit-engine-absent":
            g, outdir = grade_audit_engine_absent(ws), ws / SCENARIO_DIRS[name]
        elif name == "restructure":
            g, outdir = grade_restructure(ws), ws / SCENARIO_DIRS[name]
        elif name.startswith("idempotency:"):
            target = name.split(":", 1)[1]
            g, outdir = grade_idempotency(ws, target), ws / target
        elif name == "selftest:derivation":
            # ws, never EVALS: writing a run artifact into the repo tree
            # is how evals/grading.json ended up committed in the first
            # place. Graded output belongs in the throwaway workspace.
            g, outdir = grade_derivation_selftest(), ws
        else:
            sys.exit(f"unknown scenario: {name}")

        t = g.tally()
        # POLICY §1b: an unmeasured assert makes its scenario red. A run that
        # produced no artifact did not score partial credit — it failed to
        # measure, and the exit code says so exactly like a failure does.
        any_failed |= bool(t["failed"] or t["unmeasured"])
        # run.json's shape is {"current": {...}, "runs": [...]} — reading
        # .get("run_id") off the TOP level always returned None, so every
        # grade landed unattributed and the dashboard's per-run rows could
        # never match a run (found 2026-08-22). Provenance travels with the
        # grade: which run, which runner profile, which model.
        run = {}
        run_file = ws / "run.json"
        if run_file.exists():
            try:
                run = json.loads(run_file.read_text()).get("current", {})
            except json.JSONDecodeError:
                pass
        run_id, runner, model = (run.get("run_id"), run.get("runner"),
                                 run.get("model"))
        # BL-042 item 2: the verdict carries the generation that produced it
        # — skill VERSION + repo HEAD + grader hash, the same stamp
        # grade-history.jsonl records. Without it a grading.json from another
        # law or grader generation is indistinguishable from this run's.
        out = {"expectations": g.exps,
               "ts": datetime.now(timezone.utc).isoformat(timespec="seconds"),
               "law": law_stamp(),
               "run_id": run_id, "runner": runner, "model": model,
               "summary": t}
        fname = "grading_idempotency.json" if name.startswith("idempotency:") else "grading.json"
        (outdir / "outputs").mkdir(exist_ok=True)
        (outdir / "outputs" / fname).write_text(json.dumps(out, indent=2) + "\n")

        # Append-only grade history: the flaky-vs-persistent oracle. Every
        # grading of this scenario lands here; the dashboard (and humans)
        # read which asserts fail in SOME runs (flaky) vs EVERY run
        # (persistent, a real defect or a grader bug). The "law" stamp is
        # the law GENERATION (skill VERSION + repo commit): flaky counting
        # is only meaningful within one generation — a fix changes the
        # population, and pre-fix runs must not vote on post-fix stability.
        if not name.startswith(("idempotency:", "selftest:")):
            hist = outdir / "outputs" / "grade-history.jsonl"
            entry = {"ts": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                     "law": law_stamp(),
                     "run_id": run_id, "runner": runner, "model": model,
                     "passed": t["passed"], "failed": t["failed"],
                     "unmeasured": t["unmeasured"], "measured": t["measured"],
                     "total": t["total"],
                     # Kept disjoint on purpose: the flaky-vs-persistent
                     # oracle must never read a non-measurement as a failure
                     # of the thing the assert was about.
                     "fails": [e["text"] for e in g.exps if e["verdict"] == "failed"],
                     "unmeasured_asserts": [e["text"] for e in g.exps
                                            if e["verdict"] == "unmeasured"]}
            with hist.open("a") as fh:
                fh.write(json.dumps(entry) + "\n")

        print(f"\n== {name}: {t['measured']}/{t['total']} measured, "
              f"{t['passed']} passed ==")
        for e in g.exps:
            mark = {"passed": "ok  ", "failed": "FAIL", "unmeasured": "UNMS"}[e["verdict"]]
            print(f"  {mark}  {e['text']}"
                  + ("" if e["verdict"] == "passed" else f" — {e['evidence']}"))

    sys.exit(1 if any_failed else 0)


if __name__ == "__main__":
    main()
