#!/usr/bin/env python3
"""Deterministic grader for legislator e2e eval runs.

Grades the file tree an agent produced by running the skill against a
workspace repo (see setup_workspace.py). All expectations are DERIVED from
the current skill source (VERSION, assets/rules/**, template list) at grade
time — nothing is hardcoded to a specific constitution version, so this
grader does not rot when rules are added, removed, or renamed.

Usage:
  python3 evals/grade.py <workspace> [scenario ...]

Scenarios (default: the first four):
  fresh-scaffold-dotnet    grade <ws>/fresh-scaffold-dotnet/repo
  legacy-migration         grade <ws>/legacy-migration/repo
  upgrade                  grade <ws>/upgrade/repo (needs fixture_meta.json)
  audit                    grade the audit report saved by the eval agent at
                           <ws>/rotted-layer/outputs/audit-report.md against
                           the fixture's planted defects; asserts zero writes.
  idempotency:<scenario>   grade a SECOND skill run on <scenario>'s repo:
                           requires that run 1's result was committed before
                           run 2; passes iff run 2 left a zero diff.

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

    def common_checks(self, repo: Path) -> None:
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
                   "profiles array on one line per Step 3.6" if inline else "profiles array expanded across lines")

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
    g.common_checks(repo)

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

    status = git(repo, "status", "--porcelain").strip()
    commits = len(git(repo, "log", "--oneline").strip().splitlines())
    clean = not status and commits == meta["fixture_commit_count"]
    g.check("zero_writes", clean,
            "working tree untouched, no new commits" if clean
            else f"status={status[:200]!r}, commits={commits} (expected {meta['fixture_commit_count']})")
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
    names = sys.argv[2:] or ["fresh-scaffold-dotnet", "legacy-migration", "upgrade", "audit"]

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
