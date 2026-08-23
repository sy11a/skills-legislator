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
prints a pass/fail table. Exit code 1 if any assertion failed.
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


def _authority_rows() -> list[list[str]]:
    """The pipe-table rows of SKILL.md's `## File authority` section, each
    as a list of stripped cells (outer pipes removed)."""
    text = _skill_md()
    if "\n## File authority\n" not in text:
        raise ValueError("File authority: no `## File authority` section in SKILL.md")
    section = text.split("\n## File authority\n", 1)[1].split("\n## ", 1)[0]
    rows = []
    for line in section.splitlines():
        if line.startswith("|") and not re.match(r"^\|[\s:|-]+\|$", line):
            rows.append([c.strip() for c in line.strip().strip("|").split("|")])
    return rows


def authority_matrix() -> dict[tuple[str, str], str]:
    """(class, mode) -> right, parsed from the pinned two-header table."""
    rows = _authority_rows()
    if len(rows) < 2 + len(AUTHORITY_CLASSES):
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
                         delegated: dict[str, str] | None = None) -> None:
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

    `delegated` maps a class to the mode whose column governs it for THIS
    run, for the one case the law defines: restructure's `heal` action
    "runs SKILL.md Steps 2-3 as-is", so the owned law and manifest it
    rewrites are written under the upgrade column, not under
    restructure's own `never-touch`. The caller derives the map from the
    law (`restructure_heal_delegates`) and passes it only when the run
    actually healed — a restructure that did not heal is still held to
    its own column."""
    try:
        m = authority_matrix()
    except ValueError as e:
        g.check("mode_respects_authority", False, str(e))
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
        g.check("mode_respects_authority", False, str(e))
        return
    deleg_note = ("" if not delegated else
                  " (" + ", ".join(f"{c} delegated to {mo}"
                                   for c, mo in sorted(delegated.items())) + ")")
    g.check("mode_respects_authority", not violations,
            f"diff shape lawful for all {len(AUTHORITY_CLASSES)} classes in {mode} mode{deleg_note}"
            if not violations else "; ".join(violations[:4]))


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
    return owned


class Grader:
    def __init__(self) -> None:
        self.exps: list[dict] = []

    def check(self, name: str, passed: bool, evidence: str) -> None:
        self.exps.append({"text": name, "passed": bool(passed), "evidence": evidence})

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
                   "parsed OK" if manifest else "missing or invalid JSON")
        self.check("manifest_version_matches_skill_VERSION",
                   bool(manifest and manifest.get("legislatorVersion") == version),
                   f"expected {version}, got {manifest.get('legislatorVersion') if manifest else None}")
        self.check("manifest_stacks_correct",
                   bool(manifest and manifest.get("stacks") == expected_stacks(fixture_meta)),
                   f"stacks={manifest.get('stacks') if manifest else None}")
        self.check("manifest_ownedFiles_exact_sorted",
                   bool(manifest and manifest.get("ownedFiles") == sorted(owned)),
                   "matches files derived from skill source" if manifest and manifest.get("ownedFiles") == sorted(owned)
                   else f"expected {sorted(owned)}, got {manifest.get('ownedFiles') if manifest else None}")
        inline = bool(re.search(r'^  "stacks": \[[^\n\]]*\],$', raw, re.M))
        self.check("manifest_stacks_single_line_inline", inline,
                   "stacks array on one line per Step 3.7" if inline else "stacks array expanded across lines")

        expected_keep = expected_keep or []
        keep = manifest.get("keep") if manifest else None
        self.check("manifest_keep_matches_expected", keep == expected_keep,
                   f"expected {expected_keep}, got {keep}")

        idx = [raw.find(f'"{k}"') for k in
               ("legislatorVersion", "stacks", "keep", "ownedFiles")]
        order_ok = all(i >= 0 for i in idx) and idx == sorted(idx)
        self.check("manifest_key_order", order_ok,
                   "legislatorVersion, stacks, keep, ownedFiles" if order_ok
                   else "keys missing or out of order")

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
        self.check("manifest_keep_pinned_serialization", pinned, evidence)

        bad = [p for p, src in owned.items()
               if not (repo / p).exists() or (repo / p).read_bytes() != src.read_bytes()]
        self.check("owned_files_verbatim", not bad,
                   f"all {len(owned)} owned files byte-identical to source" if not bad else f"differ/missing: {bad}")

        # v14 model: AGENTS.md is the canonical constitution; CLAUDE.md is a symlink to it.
        agents_md = repo / "AGENTS.md"
        claude_link = repo / "CLAUDE.md"
        is_link = claude_link.is_symlink()
        points_to_agents = is_link and os.path.realpath(claude_link) == os.path.realpath(agents_md)
        self.check("v14_model_agents_canonical_claude_symlink",
                   agents_md.is_file() and is_link and points_to_agents,
                   f"AGENTS.md={agents_md.is_file()}, CLAUDE.md symlink={is_link}, ->AGENTS.md={points_to_agents}")

        status = git(repo, "status", "--porcelain").strip()
        commits = len(git(repo, "log", "--oneline").strip().splitlines())
        self.check("nothing_committed", bool(status) and commits == 1,
                   f"{len(status.splitlines()) if status else 0} changed paths in working tree, {commits} commit(s)")

    def no_unresolved_tokens(self, repo: Path) -> None:
        # -uall lists every untracked file individually; without it, a wholly
        # untracked docs/ tree collapses to one "?? docs/" entry and nothing
        # under it gets scanned.
        offenders = []
        changed = git(repo, "status", "--porcelain", "-uall").splitlines()
        for line in changed:
            rel = line[3:].strip()
            path = repo / rel
            if rel == "docs/adr/template.md" or not path.is_file():
                continue
            if path.suffix == ".md" and re.search(r"\{\{[A-Z_]+\}\}", path.read_text(errors="ignore")):
                offenders.append(rel)
        self.check("no_unresolved_placeholders", not offenders,
                   "adr template carve-out respected, no stray {{TOKEN}}s" if not offenders else f"unfilled tokens in: {offenders}")

    def scaffold_checks(self, repo: Path) -> None:
        missing = [a for a in SCAFFOLD_ARTIFACTS if not (repo / a).exists()]
        self.check("scaffold_artifacts_present", not missing,
                   "all Step 4 artifacts exist" if not missing else f"missing: {missing}")
        sk = repo / ".claude/rules/skills.md"
        sk_text = sk.read_text() if sk.exists() else ""
        stages = [w for w in ("pre-plan", "implement", "debug", "review") if w in sk_text.lower()]
        sk_ok = sk.exists() and len(stages) >= 1
        self.check("skills_stage_map_scaffolded", sk_ok,
                   f".claude/rules/skills.md exists with stage(s) {stages}" if sk_ok
                   else ".claude/rules/skills.md missing or has no stage headings")
        rows = glossary_rows(repo)
        self.check("glossary_seeded_with_terms", rows >= 1,
                   f"{rows} term row(s) derived from the repo's domain" if rows >= 1
                   else "glossary table has no body rows — {{GLOSSARY_TABLE}} derivation produced nothing")
        agents = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
        missing_imports = [p for p in expected_owned()
                           if p.startswith("docs/ai/rules/core/") and f"@{p}" not in agents]
        self.check("agents_md_imports_all_core", not missing_imports,
                   "every core rule imported" if not missing_imports
                   else f"core rules on disk but not imported: {missing_imports}")
        self.check("agents_md_imports_rules", "@docs/ai/rules/core/" in agents,
                   "@import block present" if "@docs/ai/rules/core/" in agents else "no @import lines in AGENTS.md")
        rules_dir = repo / ".claude/rules"
        self.check("project_rules_dir_scaffolded", rules_dir.is_dir(),
                   ".claude/rules/ exists" if rules_dir.is_dir()
                   else ".claude/rules/ directory not scaffolded")


def grade_fresh(ws: Path) -> Grader:
    repo = ws / "fresh-scaffold-dotnet" / "repo"
    g = Grader()
    g.common_checks(repo)
    check_mode_authority(g, repo, "scaffold")
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    return g


def grade_migration(ws: Path) -> Grader:
    repo = ws / "legacy-migration" / "repo"
    g = Grader()
    g.common_checks(repo)
    check_mode_authority(g, repo, "migrate")
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    agents = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
    v2_wired = all(w in agents for w in migration_wiring())
    g.check("agents_md_v2_wiring_written_directly", v2_wired,
            f"all {len(migration_wiring())} template wiring strings present in rewritten AGENTS.md (derived from AGENTS.md.tpl)" if v2_wired
            else "migration left v2 wiring as Step 7 proposals instead of writing it")
    report_path = ws / "legacy-migration" / "outputs" / "migration-report.md"
    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("step7_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")
    m = re.search(r"## Constitution candidates\n(.*?)(?=\nClean checks:|\n#|\Z)", report, re.S)
    section = m.group(1) if m else ""
    # Coupled to the constitution's CURRENT content: if a decimal-for-money
    # rule is ever promoted into assets/rules/**, criterion 2 flips and this
    # fixture line stops being a valid candidate — update the fixture then.
    money = "Money values are always" in section
    g.check("harvest_lists_decimal_money_rule", money,
            "decimal-money constraint quoted as a candidate" if money
            else "candidates section missing or does not quote the money rule")
    no_leak = bool(m) and "bl/NNN-short-description" not in section
    g.check("harvest_excludes_instance_convention", no_leak,
            "branch convention correctly not proposed" if no_leak
            else "instance data leaked into candidates (or section missing)")
    pr_dir = repo / ".claude/rules"
    law_hits = subprocess.run(
        ["grep", "-rl", "Money values are always", str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("law_carved_to_project_rules", bool(law_hits),
            f"decimal-money law lives in {law_hits.splitlines()}" if law_hits
            else "law-shaped constraint not carved into .claude/rules/")
    conv_hits = subprocess.run(
        ["grep", "-rl", "bl/NNN-short-description", str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("instance_data_not_in_project_rules", pr_dir.is_dir() and not conv_hits,
            "branch convention correctly stayed in AGENTS.md" if pr_dir.is_dir() and not conv_hits
            else f"instance data leaked into .claude/rules/ (or dir missing): {conv_hits.splitlines() if conv_hits else 'dir missing'}")
    for needle in MIGRATION_PRESERVED:
        hits = subprocess.run(
            ["grep", "-rl", "--exclude-dir=.git", needle, str(repo)],
            capture_output=True, text=True).stdout.strip()
        g.check(f"preserved: {needle!r}", bool(hits),
                f"found in {hits.splitlines()}" if hits else "silently dropped — appears nowhere in the result")
    return g


def grade_migration_agents_first(ws: Path) -> Grader:
    """The AGENTS-native migration branch: hand-written AGENTS.md, no
    CLAUDE.md. Same migration contract minus rename expectations, plus
    'CLAUDE.md created fresh as symlink'. The law branch ('If AGENTS.md
    already exists, it stays canonical') was specified but never
    exercised before BL-036."""
    repo = ws / "legacy-migration-agents-first" / "repo"
    g = Grader()
    g.common_checks(repo)
    check_mode_authority(g, repo, "migrate")
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    agents = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
    v2_wired = all(w in agents for w in migration_wiring())
    g.check("agents_md_v2_wiring_written_directly", v2_wired,
            f"all {len(migration_wiring())} template wiring strings present (derived from AGENTS.md.tpl)" if v2_wired
            else "migration left v2 wiring as proposals instead of writing it")
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
            else f"law preserved={bool(law_hits)}, instance data in AGENTS.md={instance_kept}")
    report_path = ws / "legacy-migration-agents-first" / "outputs" / "migration-report.md"
    has_report = report_path.exists()
    g.check("migration_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")
    return g


def grade_upgrade(ws: Path) -> Grader:
    repo = ws / "upgrade" / "repo"
    meta = json.loads((ws / "upgrade" / "fixture_meta.json").read_text())
    g = Grader()
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []), fixture_meta=meta)
    check_mode_authority(g, repo, "upgrade", meta)

    withheld = repo / "docs/ai/rules/core" / meta["withheld_core_rule"]
    g.check("newly_added_rule_present", withheld.exists(),
            f"{meta['withheld_core_rule']} copied in by the upgrade" if withheld.exists() else f"{meta['withheld_core_rule']} still missing")

    withheld_stack = repo / "docs/ai/rules/stacks/dotnet" / meta["withheld_stack_rule"]
    g.check("newly_added_stack_rule_present", withheld_stack.exists(),
            f"{meta['withheld_stack_rule']} copied in by the upgrade" if withheld_stack.exists()
            else f"{meta['withheld_stack_rule']} still missing")

    report_path = ws / "upgrade" / "outputs" / "upgrade-report.md"
    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("step7_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")
    core_import = f"@docs/ai/rules/core/{meta['withheld_core_rule']}"
    core_review_idx = report.find("eeds your review")
    core_proposed = core_review_idx >= 0 and core_import in report[core_review_idx:]
    g.check("report_proposes_core_import_line", core_proposed,
            "core-rule import proposed in Needs-your-review" if core_proposed
            else f"no proposal for {core_import}")
    import_line = f"@docs/ai/rules/stacks/dotnet/{meta['withheld_stack_rule']}"
    # Scoped to the "Needs your review" section (BL-019 R3): the line counts
    # only as a PROPOSAL — its appearance in Deleted/Overwritten would not.
    review_idx = report.find("eeds your review")
    proposed = review_idx >= 0 and import_line in report[review_idx:]
    g.check("report_proposes_stack_import_line", proposed,
            "proposed in the Needs-your-review section" if proposed
            else f"no 'Needs your review' section proposing {import_line}")

    retired = repo / "docs/ai/rules/core" / meta["retired_rule"]
    g.check("retired_rule_deleted", not retired.exists(),
            "deletion propagation removed it" if not retired.exists() else "retired rule still on disk")

    # BL-036 Wave B: upgrade is also a scaffold for artifacts the repo
    # never had — the v17 fixture predates docs/cases/, so the upgrade run
    # must create the case home (found unasserted by review 2026-08-21).
    missing_artifacts = [a for a in SCAFFOLD_ARTIFACTS if not (repo / a).exists()]
    g.check("upgrade_creates_missing_artifacts", not missing_artifacts,
            "all Step 4 artifacts exist after upgrade (derived list)" if not missing_artifacts
            else f"upgrade failed to scaffold: {missing_artifacts}")

    # BL-036 Wave B: the keep-refusal branch — when the run's prompt (saved
    # by the runner to outputs/prompt.txt) asks to protect an OWNED path,
    # the skill must refuse with a reason under ## Keep list.
    prompt_file = ws / "upgrade" / "outputs" / "prompt.txt"
    if prompt_file.exists() and "protect docs/ai/rules/core/okf.md" in prompt_file.read_text():
        refusal = re.search(r"## Keep list\n(.*?)(?=\n#|\Z)", report, re.S | re.M)
        seg = refusal.group(1) if refusal else ""
        refused = "okf.md" in seg and "owned" in seg.lower()
        g.check("keep_refusal_for_owned_path", refused,
                "owned-path keep request refused with a reason" if refused
                else "no refusal recorded for the owned-path keep request")

    # Project-owned files must be untouched AT THEIR PATH — the set is
    # derived from the file-authority matrix (BL-038): every class whose
    # upgrade cell is a path-protecting right. AGENTS.md is absent because
    # its cell is content-protected (propose-only); its content, not its
    # path, is checked by mode_respects_authority above.
    try:
        protected = protected_project_files(repo, fixture_meta=meta)
        touched = [p for p in git(repo, "diff", "HEAD", "--name-only").splitlines() if p in protected]
        g.check("project_owned_files_untouched", not touched,
                "no tracked project-owned file modified" if not touched else f"modified: {touched}")
    except ValueError as e:
        g.check("project_owned_files_untouched", False, str(e))
    return g


def grade_audit(ws: Path) -> Grader:
    repo = ws / "rotted-layer" / "repo"
    meta = json.loads((ws / "rotted-layer" / "fixture_meta.json").read_text())
    report_path = ws / "rotted-layer" / "outputs" / "audit-report.md"
    g = Grader()
    check_mode_authority(g, repo, "audit", meta)

    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("audit_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")

    # BL-036 Wave B: the report must live OUTSIDE the repo — the audit's
    # zero-writes contract means even its own output may not land in the
    # tree (the zero_writes check below would catch a written file, but
    # this names the intent explicitly).
    inside = [p.name for p in (repo / "docs").rglob("*report*")]
    g.check("audit_report_outside_repo", not inside,
            "no report artifacts inside the repo" if not inside
            else f"report written into the repo: {inside}")

    for marker in meta["report_markers"]:
        g.check(f"report names {marker!r}", marker in report,
                "named in report" if marker in report else "absent from report")

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
                 f"markers for no law check: {sorted(orphaned)}")

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
                else f"not under ## {severity} (heading found={bool(m)})")

    for marker in meta.get("absent_markers", []):
        g.check(f"report does NOT contain {marker!r}", marker not in report,
                "correctly absent" if marker not in report else "false-positive finding present")

    # Scoped to the candidates section: findings may name these statements
    # legitimately, but proposing them as fleet candidates is a failure.
    m = re.search(r"## Constitution candidates\n(.*?)(?=\nClean checks:|\n#|\Z)", report, re.S)
    section = m.group(1) if m else ""
    for marker in meta.get("candidate_absent_markers", []):
        g.check(f"candidates section does NOT contain {marker!r}",
                marker not in section,
                "correctly not proposed" if marker not in section
                else "non-candidate statement proposed as fleet law")

    status = git(repo, "status", "--porcelain").strip()
    head = git(repo, "rev-parse", "HEAD").strip()
    clean = not status and head == meta["fixture_head"]
    g.check("zero_writes", clean,
            "working tree untouched, HEAD identical to fixture" if clean
            else f"status={status[:200]!r}, HEAD={head} (expected {meta['fixture_head']})")
    return g


def grade_restructure(ws: Path) -> Grader:
    repo = ws / "restructure" / "repo"
    meta = json.loads((ws / "restructure" / "fixture_meta.json").read_text())
    report_path = ws / "restructure" / "outputs" / "restructure-report.md"
    g = Grader()
    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""

    # A `[heal]` item is the law's own delegation of the owned layer to the
    # upgrade column (`references/restructure.md` §2) — those writes are
    # not restructure's, so they are judged by the column heal invokes.
    # No heal in the plan, no delegation: never-touch keeps its teeth.
    check_mode_authority(g, repo, "restructure", meta,
                         delegated=restructure_heal_delegates()
                         if "[heal]" in report else None)

    g.check("restructure_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")

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
                else "lost — appears nowhere in the repo")

    kept = repo / meta["kept_path"]
    kept_ok = kept.exists() and kept.read_text() == meta["kept_content"]
    g.check("kept_file_untouched_in_place", kept_ok,
            "byte-identical at original path" if kept_ok
            else "kept file moved, edited, or deleted")

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
                  else "no entry document on disk at all"))

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
                 f"CLAUDE.md is symlink={claude_f.is_symlink()}")
    decision_open = "[decision]" in report and "We do not maintain CHANGELOG.md" in report
    g.check("conflict_surfaced_as_decision", decision_open,
            "[decision] item names the conflict" if decision_open
            else "report lacks a [decision] item naming the conflict")

    pr_path = repo / meta["project_rule_conflict_path"]
    pr_ok = pr_path.exists() and pr_path.read_text() == meta["project_rule_conflict_content"]
    pr_named = meta["project_rule_conflict_path"] in report
    g.check("project_rule_conflict_decision_gated", pr_ok and pr_named,
            "conflicting project rule byte-unchanged and named in the report"
            if pr_ok and pr_named
            else f"file untouched={pr_ok}, named in report={pr_named}")

    rows = glossary_rows(repo)
    g.check("glossary_healed_with_terms", rows >= 1,
            f"glossary seeded with {rows} term row(s) by the fix item" if rows >= 1
            else "glossary still has zero body rows after restructure")
    gl_named = "glossary" in report.lower()
    g.check("glossary_heal_in_plan", gl_named,
            "plan/report names the glossary item" if gl_named
            else "report never mentions the glossary")

    fg = repo / meta["foreign_glossary_path"]
    g.check("foreign_glossary_merged_away", not fg.exists(),
            f"{meta['foreign_glossary_path']} removed after merge" if not fg.exists()
            else f"{meta['foreign_glossary_path']} still on disk")
    gl_text = (repo / "docs/okf/glossary.md").read_text() if (repo / "docs/okf/glossary.md").exists() else ""
    def_in_gl = meta["foreign_glossary_definition"].lower() in gl_text.lower()
    g.check("foreign_definition_in_okf_glossary", def_in_gl,
            "instance definition lives in docs/okf/glossary.md" if def_in_gl
            else "definition not merged into the OKF glossary")

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
            else f"file untouched={skf_ok}, routed to team={named}")

    stray = repo / meta["stray_rulebook_path"]
    g.check("stray_rulebook_merged_away", not stray.exists(),
            "stray rulebook removed after merge" if not stray.exists()
            else f"{meta['stray_rulebook_path']} still on disk")
    pr_dir = repo / ".claude/rules"
    law_hits = subprocess.run(
        ["grep", "-rl", meta["stray_project_law"], str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("stray_law_merged_to_project_rules", bool(law_hits),
            f"stray rulebook law lives in {law_hits.splitlines()}" if law_hits
            else "stray rulebook law not merged into .claude/rules/")

    moved_ok = (not (repo / ".claude/plans").exists()
                and (repo / "docs/superpowers/plans/2026-01-importer-plan.md").exists())
    g.check("plans_relocated_to_standard_home", moved_ok,
            ".claude/plans/ gone, file at docs/superpowers/plans/ (legacy home — stray non-case plans stay there)" if moved_ok
            else "plan file not moved (or old dir left behind)")

    # BL-036 Wave B: the misplaced case directory (docs/superpowers/BL-0007)
    # must reach the case home per §1's cases row — content preserved.
    case_src = repo / "docs/superpowers/BL-0007/plan.md"
    case_dst = repo / "docs/cases/BL-0007/plan.md"
    case_moved = (not case_src.exists() and case_dst.exists()
                  and "sequential per tenant" in case_dst.read_text())
    g.check("misplaced_case_relocated_to_case_home", case_moved,
            "BL-0007 lives in docs/cases/ with content intact" if case_moved
            else f"src gone={not case_src.exists()}, dst ok={case_dst.exists()}")
    g.check("cursorrules_merged_away", not (repo / ".cursorrules").exists(),
            ".cursorrules removed after merge" if not (repo / ".cursorrules").exists()
            else ".cursorrules still present")
    g.check("ghost_import_fixed", "ghost-rule.md" not in claude,            "dangling import gone" if "ghost-rule.md" not in claude
            else "ghost-rule import still in AGENTS.md")

    src = SKILL / "assets/rules/core/okf.md"
    okf = repo / "docs/ai/rules/core/okf.md"
    healed = okf.exists() and okf.read_bytes() == src.read_bytes()
    g.check("owned_drift_healed", healed,
            "okf.md byte-identical to skill source" if healed
            else "owned drift not healed via Steps 2-3")

    version = int((SKILL / "VERSION").read_text().strip())
    mpath = repo / "docs/ai/manifest.json"
    manifest = json.loads(mpath.read_text()) if mpath.exists() else None
    heal_ok = bool(manifest and manifest.get("legislatorVersion") == version
                   and {"path": meta["kept_path"], "reason": "works as-is"}
                   in (manifest.get("keep") or []))
    g.check("manifest_healed_keep_carried", heal_ok,
            f"manifest at v{version}, keep entry carried" if heal_ok
            else "manifest missing, stale, or keep entry dropped")

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
                  else "orphan deleted by the run"))

    # BL-036 Wave B: post-state asserts — the [link] outcome must be visible
    # in the index (not only named in the plan), and the stale map row must
    # be gone from the map (post-state, not report-only).
    if linked:
        idx_text = (repo / "docs/okf/index.md").read_text() if (repo / "docs/okf/index.md").exists() else ""
        g.check("link_post_state_in_index", "orphan-notes.md" in idx_text,
                "index.md references the linked orphan" if "orphan-notes.md" in idx_text
                else "[link] applied but the index does not reference the file")
    map_text = (repo / "docs/okf/codebase-map.md").read_text() if (repo / "docs/okf/codebase-map.md").exists() else ""
    g.check("stale_map_row_gone", "legacy/" not in map_text,
            "stale legacy/ row removed from codebase-map" if "legacy/" not in map_text
            else "stale row still in codebase-map.md")

    fid = "Fidelity: verified" in report
    g.check("fidelity_line_reported", fid,
            "report carries the pinned fidelity line" if fid
            else "no 'Fidelity: verified' line in the report")
    return g


def grade_idempotency(ws: Path, scenario: str) -> Grader:
    repo = ws / scenario / "repo"
    g = Grader()
    status = git(repo, "status", "--porcelain").strip()
    diff = git(repo, "diff", "HEAD", "--stat").strip()
    clean = not status and not diff
    g.check("second_run_zero_diff", clean,
            "re-run produced no spurious diff" if clean else f"status: {status[:300]!r} diff: {diff[:300]!r}")
    return g


def grade_derivation_selftest() -> Grader:
    """BL-036 Wave A: prove the derived contracts track the skill source.
    If someone hand-edits a stale list back in or the source moves, these
    invariants go red — divergence becomes impossible to miss. No agent
    run: pure derivation checks."""
    g = Grader()
    g.check("scaffold_artifacts_derived_nonempty", len(SCAFFOLD_ARTIFACTS) >= 10,
            f"{len(SCAFFOLD_ARTIFACTS)} targets parsed from Step 4's table")
    g.check("scaffold_artifacts_include_cases_home",
            "docs/cases/README.md" in SCAFFOLD_ARTIFACTS,
            "the v17 case home is in the derived list")
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
            f"{len(matrix)} cells, all in the closed vocabulary" if not shape_err else shape_err)
    g.check("authority_states_pinned",
            states == {"scaffold": "installing", "migrate": "installing",
                       "upgrade": "maintaining", "restructure": "maintaining",
                       "audit": "inspecting"},
            f"states: {states}" if states else shape_err)
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
                else f"no canonical file for: {uncovered_content_cells}")
    else:
        g.check("content_protected_rights_have_canonical_file", False, shape_err)
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
                f"real={'AGENTS.md' in real}, flipped={'AGENTS.md' in patched}")
    else:
        g.check("protected_set_derived_from_cells", False, shape_err)
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

        g_untouched = Grader()
        check_mode_authority(g_untouched, sym_repo, "upgrade")
        untouched_exp = g_untouched.exps[0]
        untouched_ok = untouched_exp["passed"]

        (sym_repo / "AGENTS.md").write_text("# A\nextra\n")
        g_edited = Grader()
        check_mode_authority(g_edited, sym_repo, "upgrade")
        edited_exp = g_edited.exps[0]
        edited_flagged = (not edited_exp["passed"]
                          and "AGENTS.md content changed" in edited_exp["evidence"])

        symlink_ok = untouched_ok and edited_flagged
        g.check("content_check_ignores_symlink_at_head", symlink_ok,
                f"untouched: {'ok' if untouched_ok else 'FAIL ' + untouched_exp['evidence']}; "
                f"content-changed: {'flagged' if edited_flagged else 'FAIL ' + edited_exp['evidence']}")
    wiring = migration_wiring()
    g.check("migration_wiring_derived_from_template",
            "@docs/okf/codebase-map.md" in wiring and "## Boundaries" in wiring,
            f"{len(wiring)} wiring strings parsed from AGENTS.md.tpl")
    sev = audit_check_severities()
    g.check("audit_severities_derived",
            len(sev) >= 14 and sev.get("Owned-layer integrity") == "Critical",
            f"{len(sev)} checks with parsed severities")
    slugs = audit_check_slugs()
    g.check("audit_slugs_derived",
            len(slugs) == len(sev) and "imports-resolve" in slugs,
            f"{len(slugs)} pinned slugs parsed, one per severity-carrying check")
    actions = restructure_actions()
    g.check("restructure_actions_derived",
            actions == {"move", "merge", "link", "fix", "heal", "decision"},
            f"closed action set parsed: {sorted(actions)}")
    # §2's heal bullet is the only place the law delegates a class to
    # another mode's column; the grader reads the delegation there instead
    # of restating it. Every class Steps 2-3 write must carry its cell
    # reference in that bullet, or mode_respects_authority will judge a
    # lawful delegated write against restructure's own column.
    heal = restructure_heal_delegates()
    heal_ok = heal == {"owned law": "upgrade", "manifest": "upgrade"}
    g.check("heal_delegation_derived", heal_ok,
            f"heal delegates {heal} to the upgrade column" if heal_ok
            else f"heal bullet delegates {heal} — expected owned law and manifest to the upgrade column")
    # §1 lock (BL-036 Wave B): the standard-layout table must carry the
    # cases row and the legacy qualifiers — the grader's relocation
    # expectations are only lawful while the table says so.
    layout = (SKILL / "references/restructure.md").read_text().split("## 1.", 1)[1].split("## 2.", 1)[0]
    g.check("layout_table_has_cases_row",
            "| Case files (any `BL-NNN` directory) | `docs/cases/BL-NNN/`" in layout,
            "§1 maps case directories to docs/cases/")
    g.check("layout_table_marks_legacy_homes",
            "**legacy home**" in layout,
            "§1 qualifies superpowers rows as legacy homes")
    return g


def grade_upgrade_drop_stack(ws: Path) -> Grader:
    """BL-036 Wave B: the only deletion-semantics-by-stack branch — the
    prompt drops aurelia from a dotnet+aurelia subscription."""
    repo = ws / "upgrade-drop-stack" / "repo"
    meta = json.loads((ws / "upgrade-drop-stack" / "fixture_meta.json").read_text())
    g = Grader()
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []), fixture_meta=meta)
    check_mode_authority(g, repo, "upgrade", meta)

    dropped_left = [p for p in meta["dropped_stack_files"] if (repo / p).exists()]
    g.check("dropped_stack_files_deleted", not dropped_left,
            f"{meta['dropped_stack']} owned files removed" if not dropped_left
            else f"still on disk: {dropped_left}")

    dotnet_dir = repo / "docs/ai/rules/stacks/dotnet"
    dotnet_left = sorted(p.name for p in dotnet_dir.glob("*.md")) if dotnet_dir.is_dir() else []
    expected_dotnet = sorted((SKILL / "assets/rules/stacks/dotnet").glob("*.md"))
    g.check("kept_stack_untouched_and_refreshed",
            dotnet_left == sorted(f.name for f in expected_dotnet),
            f"dotnet rules present and refreshed ({len(dotnet_left)})" if
            dotnet_left == sorted(f.name for f in expected_dotnet)
            else f"dotnet mismatch: {dotnet_left}")

    report_path = ws / "upgrade-drop-stack" / "outputs" / "upgrade-report.md"
    has_report = report_path.exists()
    g.check("upgrade_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")
    if has_report:
        report = report_path.read_text()
        review_idx = report.find("eeds your review")
        proposed = review_idx >= 0 and "stacks/aurelia" in report[review_idx:]
        g.check("report_proposes_aurelia_import_removal", proposed,
                "aurelia import removal proposed under Needs your review" if proposed
                else "no proposal to remove the aurelia import line")
    return g


def grade_case_practice(ws: Path) -> Grader:
    """BL-036 Wave C — the acceptance test: a fresh agent EXECUTES
    core/sdd.md on ordinary feature work. Graded: the practice (case
    home, tier header, EARS ids, hurting case, task traceability,
    converge trail). Not graded: the code itself."""
    repo = ws / "case-practice" / "repo"
    g = Grader()

    cases = sorted((repo / "docs/cases").glob("BL-*")) if (repo / "docs/cases").is_dir() else []
    case_dir = cases[0] if cases else None
    g.check("case_born_in_case_home", case_dir is not None and case_dir.is_dir(),
            f"new case at {case_dir}" if case_dir else "no BL-* directory under docs/cases/")

    if case_dir is None:
        return g

    all_text = "\n".join(p.read_text(errors="ignore") for p in case_dir.rglob("*.md"))

    tier = re.search(r"Tier:\s*([012])", all_text)
    g.check("tier_declared_in_case_header", tier is not None,
            f"tier {tier.group(1)} declared" if tier else "no 'Tier: N' line anywhere in the case")

    ears = re.findall(r"\bR-\d{3}\b", all_text)
    g.check("ears_lines_with_ids", len(set(ears)) >= 2,
            f"{len(set(ears))} distinct R-NNN ids" if len(set(ears)) >= 2
            else f"only {len(set(ears))} R-NNN id(s) — need >=2 EARS lines")

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
            else "no GIVEN/WHEN/THEN scenario in the case")

    per_trace = re.search(r"per\s+R-\d{3}", all_text)
    g.check("tasks_trace_per_rnnn", per_trace is not None,
            "at least one task traces 'per R-NNN'" if per_trace
            else "no task carries per R-NNN traceability")

    converged = ("Converged" in all_text) or re.search(r"\((?:missing|partial|contradicts|unrequested)\)", all_text)
    g.check("converge_trail_present", converged is not None,
            "converge statement or append-only gap findings present" if converged
            else "no converge trail — the case cannot lawfully close")

    # the pre-existing README must survive untouched (create-once discipline)
    readme = repo / "docs/cases/README.md"
    g.check("case_home_readmark_untouched",
            readme.exists() and readme.read_text() == (SKILL / "assets/templates/cases-README.md.tpl").read_text(),
            "docs/cases/README.md byte-identical to the template" if readme.exists() else "README missing")
    return g


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    ws = Path(sys.argv[1]).resolve()
    names = sys.argv[2:] or ["fresh-scaffold-dotnet", "legacy-migration", "upgrade", "audit", "restructure"]

    any_failed = False
    for name in names:
        if name == "fresh-scaffold-dotnet":
            g, outdir = grade_fresh(ws), ws / name
        elif name == "legacy-migration":
            g, outdir = grade_migration(ws), ws / name
        elif name == "legacy-migration-agents-first":
            g, outdir = grade_migration_agents_first(ws), ws / name
        elif name == "upgrade-drop-stack":
            g, outdir = grade_upgrade_drop_stack(ws), ws / name
        elif name == "case-practice":
            g, outdir = grade_case_practice(ws), ws / name
        elif name == "upgrade":
            g, outdir = grade_upgrade(ws), ws / name
        elif name == "audit":
            g, outdir = grade_audit(ws), ws / "rotted-layer"
        elif name == "restructure":
            g, outdir = grade_restructure(ws), ws / "restructure"
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

        passed = sum(1 for e in g.exps if e["passed"])
        total = len(g.exps)
        any_failed |= passed < total
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
        out = {"expectations": g.exps,
               "ts": datetime.now(timezone.utc).isoformat(timespec="seconds"),
               "run_id": run_id, "runner": runner, "model": model,
               "summary": {"passed": passed, "failed": total - passed,
                           "total": total, "pass_rate": round(passed / total, 3)}}
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
                     "passed": passed, "failed": total - passed, "total": total,
                     "fails": [e["text"] for e in g.exps if not e["passed"]]}
            with hist.open("a") as fh:
                fh.write(json.dumps(entry) + "\n")

        print(f"\n== {name}: {passed}/{total} ==")
        for e in g.exps:
            mark = "ok  " if e["passed"] else "FAIL"
            print(f"  {mark}  {e['text']}" + ("" if e["passed"] else f" — {e['evidence']}"))

    sys.exit(1 if any_failed else 0)


if __name__ == "__main__":
    main()
