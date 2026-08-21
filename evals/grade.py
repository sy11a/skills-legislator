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
import subprocess
import sys
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


def protected_project_files() -> list[str]:
    """Tracked project-owned files upgrade must not touch: the scaffold's
    create-once artifacts minus the entry-document pair (AGENTS.md is
    never edited — only proposed to — and CLAUDE.md is a managed
    symlink; both legitimately change across a file-model upgrade)."""
    return [a for a in SCAFFOLD_ARTIFACTS
            if a not in ("AGENTS.md", "CLAUDE.md")]


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


def restructure_actions() -> set[str]:
    """The closed action set, parsed from restructure.md §2's bold
    definitions."""
    text = (SKILL / "references/restructure.md").read_text()
    section2 = text.split("## 2.", 1)[1].split("## 3.", 1)[0]
    return set(re.findall(r"^- \*\*(\w+)\*\*", section2, re.M))


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
    g.scaffold_checks(repo)
    g.no_unresolved_tokens(repo)
    return g


def grade_migration(ws: Path) -> Grader:
    repo = ws / "legacy-migration" / "repo"
    g = Grader()
    g.common_checks(repo)
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
    m = re.search(r"### Constitution candidates\n(.*?)(?=\nClean checks:|\n#|\Z)", report, re.S)
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


def grade_upgrade(ws: Path) -> Grader:
    repo = ws / "upgrade" / "repo"
    meta = json.loads((ws / "upgrade" / "fixture_meta.json").read_text())
    g = Grader()
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []), fixture_meta=meta)

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

    # Project-owned files must be untouched: tracked-file diff limited to them
    # must be empty. Derived from the scaffold table (BL-036 Wave A): the
    # entry-document pair is excluded — AGENTS.md only ever receives
    # proposals and CLAUDE.md is a managed symlink, both legitimately
    # change across a file-model upgrade.
    protected = protected_project_files()
    touched = [p for p in git(repo, "diff", "HEAD", "--name-only").splitlines() if p in protected]
    g.check("project_owned_files_untouched", not touched,
            "no tracked project-owned file modified" if not touched else f"modified: {touched}")
    return g


def grade_audit(ws: Path) -> Grader:
    repo = ws / "rotted-layer" / "repo"
    meta = json.loads((ws / "rotted-layer" / "fixture_meta.json").read_text())
    report_path = ws / "rotted-layer" / "outputs" / "audit-report.md"
    g = Grader()

    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("audit_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")

    for marker in meta["report_markers"]:
        g.check(f"report names {marker!r}", marker in report,
                "named in report" if marker in report else "absent from report")

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
    m = re.search(r"### Constitution candidates\n(.*?)(?=\nClean checks:|\n#|\Z)", report, re.S)
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
    g.check("restructure_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")

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

    claude = (repo / "AGENTS.md").read_text() if (repo / "AGENTS.md").exists() else ""
    g.check("conflict_not_auto_resolved", meta["conflict_marker"] in claude,
            "conflicting line still in AGENTS.md" if meta["conflict_marker"] in claude
            else "conflict line gone — auto-resolved without the user")
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
    # "### For the team:" section; a plan line explicitly marked
    # "— skipped (For the team)" satisfies the same value (observed
    # stable across the final-law series) and is accepted.
    ftt = re.search(r"^#{1,3}\s*For the team:\s*\n(.*?)(?=^Kept \(immovable\)|^#{1,2} |\Z)", report,
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
            ".claude/plans/ gone, file at docs/superpowers/plans/" if moved_ok
            else "plan file not moved (or old dir left behind)")
    g.check("cursorrules_merged_away", not (repo / ".cursorrules").exists(),
            ".cursorrules removed after merge" if not (repo / ".cursorrules").exists()
            else ".cursorrules still present")
    g.check("ghost_import_fixed", "ghost-rule.md" not in claude,
            "dangling import gone" if "ghost-rule.md" not in claude
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
    # Law B (2026-08-21): a deletion proposal is lawful ONLY as an open
    # [decision] item the owner executes — so the accepted outcomes are
    # (a) the orphan was linked, or (b) it still exists and the report
    # carries an open [decision] proposing its deletion. Vanishing without
    # a decision item, or an unreferenced survivor with no decision, fail.
    orphan_decision = orphan.exists() and re.search(
        r"\[decision\][^\n]*orphan-notes\.md", report) is not None
    g.check("orphan_linked_not_deleted", linked or orphan_decision,
            "orphan linked into the layer" if linked
            else "orphan survives with an open deletion [decision]" if orphan_decision
            else "orphan deleted without a decision item, or unreferenced with no decision")

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
    g.check("protected_excludes_entry_document_pair",
            "AGENTS.md" not in protected_project_files()
            and "CLAUDE.md" not in protected_project_files(),
            "entry-document pair excluded from the protected set")
    wiring = migration_wiring()
    g.check("migration_wiring_derived_from_template",
            "@docs/okf/codebase-map.md" in wiring and "## Boundaries" in wiring,
            f"{len(wiring)} wiring strings parsed from AGENTS.md.tpl")
    sev = audit_check_severities()
    g.check("audit_severities_derived",
            len(sev) >= 14 and sev.get("Owned-layer integrity") == "Critical",
            f"{len(sev)} checks with parsed severities")
    actions = restructure_actions()
    g.check("restructure_actions_derived",
            actions == {"move", "merge", "link", "fix", "heal", "decision"},
            f"closed action set parsed: {sorted(actions)}")
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
            g, outdir = grade_derivation_selftest(), EVALS
        else:
            sys.exit(f"unknown scenario: {name}")

        passed = sum(1 for e in g.exps if e["passed"])
        total = len(g.exps)
        any_failed |= passed < total
        out = {"expectations": g.exps,
               "summary": {"passed": passed, "failed": total - passed,
                           "total": total, "pass_rate": round(passed / total, 3)}}
        fname = "grading_idempotency.json" if name.startswith("idempotency:") else "grading.json"
        (outdir / fname).write_text(json.dumps(out, indent=2) + "\n")

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
