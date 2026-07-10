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
import json
import re
import subprocess
import sys
from pathlib import Path

EVALS = Path(__file__).resolve().parent
SKILL = EVALS.parent / "skill"

PROFILES = ["dotnet"]  # all fixtures are dotnet-only

# Mirrors SKILL.md Step 4's table (static-checked against assets/templates/).
SCAFFOLD_ARTIFACTS = [
    "docs/okf/index.md",
    "docs/okf/log.md",
    "docs/okf/codebase-map.md",
    "docs/okf/glossary.md",
    "docs/backlog.md",
    "docs/adr/0001-record-architecture-decisions.md",
    "docs/adr/template.md",
    "docs/journal/README.md",
    "CHANGELOG.md",
]

# Migration fixture content that must never be silently dropped.
MIGRATION_PRESERVED = [
    "Money values are always",
    "bl/NNN-short-description",
]


def git(repo: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(repo), *args],
                          capture_output=True, text=True).stdout


def expected_owned() -> dict[str, Path]:
    """repo-relative owned path -> source file, derived from skill source."""
    owned: dict[str, Path] = {}
    for f in sorted((SKILL / "assets/rules/core").glob("*.md")):
        owned[f"docs/ai/rules/core/{f.name}"] = f
    for profile in PROFILES:
        for f in sorted((SKILL / "assets/rules/stacks" / profile).glob("*.md")):
            owned[f"docs/ai/rules/stacks/{profile}/{f.name}"] = f
    return owned


class Grader:
    def __init__(self) -> None:
        self.exps: list[dict] = []

    def check(self, name: str, passed: bool, evidence: str) -> None:
        self.exps.append({"text": name, "passed": bool(passed), "evidence": evidence})

    def common_checks(self, repo: Path, expected_keep: list | None = None) -> None:
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
        self.check("manifest_profiles_correct",
                   bool(manifest and manifest.get("profiles") == PROFILES),
                   f"profiles={manifest.get('profiles') if manifest else None}")
        self.check("manifest_ownedFiles_exact_sorted",
                   bool(manifest and manifest.get("ownedFiles") == sorted(owned)),
                   "matches files derived from skill source" if manifest and manifest.get("ownedFiles") == sorted(owned)
                   else f"expected {sorted(owned)}, got {manifest.get('ownedFiles') if manifest else None}")
        inline = bool(re.search(r'^  "profiles": \[[^\n\]]*\],$', raw, re.M))
        self.check("manifest_profiles_single_line_inline", inline,
                   "profiles array on one line per Step 3.7" if inline else "profiles array expanded across lines")

        expected_keep = expected_keep or []
        keep = manifest.get("keep") if manifest else None
        self.check("manifest_keep_matches_expected", keep == expected_keep,
                   f"expected {expected_keep}, got {keep}")

        idx = [raw.find(f'"{k}"') for k in
               ("legislatorVersion", "profiles", "keep", "ownedFiles")]
        order_ok = all(i >= 0 for i in idx) and idx == sorted(idx)
        self.check("manifest_key_order", order_ok,
                   "legislatorVersion, profiles, keep, ownedFiles" if order_ok
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
        claude = (repo / "CLAUDE.md").read_text() if (repo / "CLAUDE.md").exists() else ""
        self.check("claude_md_imports_rules", "@docs/ai/rules/core/" in claude,
                   "@import block present" if "@docs/ai/rules/core/" in claude else "no @import lines in CLAUDE.md")
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
    claude = (repo / "CLAUDE.md").read_text() if (repo / "CLAUDE.md").exists() else ""
    v2_wired = "@docs/okf/codebase-map.md" in claude and "## Boundaries" in claude
    g.check("claude_md_v2_wiring_written_directly", v2_wired,
            "map import + Boundaries section present in rewritten CLAUDE.md" if v2_wired
            else "migration left v2 wiring as Step 7 proposals instead of writing it")
    report_path = ws / "legacy-migration" / "outputs" / "step7-report.md"
    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("step7_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")
    m = re.search(r"### Constitution candidates\n(.*?)(?=\n#|\Z)", report, re.S)
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
            "branch convention correctly stayed in CLAUDE.md" if pr_dir.is_dir() and not conv_hits
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
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []))

    withheld = repo / "docs/ai/rules/core" / meta["withheld_core_rule"]
    g.check("newly_added_rule_present", withheld.exists(),
            f"{meta['withheld_core_rule']} copied in by the upgrade" if withheld.exists() else f"{meta['withheld_core_rule']} still missing")

    retired = repo / "docs/ai/rules/core" / meta["retired_rule"]
    g.check("retired_rule_deleted", not retired.exists(),
            "deletion propagation removed it" if not retired.exists() else "retired rule still on disk")

    # Project-owned files must be untouched: tracked-file diff limited to them
    # must be empty. CLAUDE.md included — the skill only PROPOSES import-line
    # changes in its report; it never edits the file (Step 7).
    protected = ["CLAUDE.md", "CHANGELOG.md", "docs/backlog.md",
                 "docs/okf/index.md", "docs/okf/log.md", "docs/journal/README.md"]
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

    for marker in meta.get("absent_markers", []):
        g.check(f"report does NOT contain {marker!r}", marker not in report,
                "correctly absent" if marker not in report else "false-positive finding present")

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
        hits = subprocess.run(
            ["grep", "-rl", "--exclude-dir=.git", s, str(repo)],
            capture_output=True, text=True).stdout.strip()
        g.check(f"fidelity: {s[:44]!r}", bool(hits),
                f"survives in {hits.splitlines()}" if hits
                else "lost — appears nowhere in the repo")

    kept = repo / meta["kept_path"]
    kept_ok = kept.exists() and kept.read_text() == meta["kept_content"]
    g.check("kept_file_untouched_in_place", kept_ok,
            "byte-identical at original path" if kept_ok
            else "kept file moved, edited, or deleted")

    claude = (repo / "CLAUDE.md").read_text() if (repo / "CLAUDE.md").exists() else ""
    g.check("conflict_not_auto_resolved", meta["conflict_marker"] in claude,
            "conflicting line still in CLAUDE.md" if meta["conflict_marker"] in claude
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
            else "ghost-rule import still in CLAUDE.md")

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
    g.check("orphan_linked_not_deleted", linked,
            "orphan still exists and is now referenced" if linked
            else "orphan deleted or still unreferenced")

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

        print(f"\n== {name}: {passed}/{total} ==")
        for e in g.exps:
            mark = "ok  " if e["passed"] else "FAIL"
            print(f"  {mark}  {e['text']}" + ("" if e["passed"] else f" — {e['evidence']}"))

    sys.exit(1 if any_failed else 0)


if __name__ == "__main__":
    main()
