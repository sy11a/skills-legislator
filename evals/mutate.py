#!/usr/bin/env python3
"""The mutation pass — every assert proves it can fail (BL-063, POLICY §1c).

Run history cannot find a useless assert: a healthy corpus is green by
definition, so a perfect assert and a dead one produce identical history.
This pass executes falsifiability instead of remembering it: every corpus
assert carries a named, minimal corruption of the artifact it declares
(evals/mutations.py) that MUST flip it to failed. The pass runs against the
RECORDED artifacts of a finished benchmark run — no agent, no tokens.

Usage:
  python3 evals/mutate.py <workspace> [scenario ...]

Per scenario:
  1. validate the substrate — reconstruct run-1 state if the idempotency
     stage moved the fixture, re-grade, and compare assert-by-assert with
     the recorded corpus grading.json; a scenario that disagrees is
     UNUSABLE and makes the pass red (a mutation verdict measured on a
     different state than the corpus graded is not a verdict);
  2. for each assert: apply its mutation in place, re-grade, require the
     target assert failed, restore every touched byte.

Output: one summary — killed / survived / uncovered / duplicate groups /
unusable — every non-killed item by name (no silent caps). Exit 0 only when
every assert was killed and no scenario was unusable. The obligation covers
every FUTURE assert by construction: an assert the manifest does not cover
is `uncovered`, and uncovered is red.
"""
from __future__ import annotations

import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

EVALS = Path(__file__).resolve().parent

sys.path.insert(0, str(EVALS))
import grade as grade_mod                              # noqa: E402
from mutations import mutations_for                    # noqa: E402

SCENARIO_DIRS = grade_mod.SCENARIO_DIRS
CORPUS = ["fresh-scaffold-dotnet", "legacy-migration",
          "legacy-migration-agents-first", "upgrade", "upgrade-drop-stack",
          "case-practice", "audit", "audit-engine-absent", "restructure"]


def git(repo: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(repo), *args],
                          capture_output=True, text=True).stdout


def grade_scenario(ws: Path, name: str) -> list[dict]:
    """Grade one scenario in-process; expectations list. The grader builds
    everything per call, so repeated grading is safe."""
    fn = {"fresh-scaffold-dotnet": grade_mod.grade_fresh,
          "legacy-migration": grade_mod.grade_migration,
          "legacy-migration-agents-first": grade_mod.grade_migration_agents_first,
          "upgrade": grade_mod.grade_upgrade,
          "upgrade-drop-stack": grade_mod.grade_upgrade_drop_stack,
          "case-practice": grade_mod.grade_case_practice,
          "audit": grade_mod.grade_audit,
          "audit-engine-absent": grade_mod.grade_audit_engine_absent,
          "restructure": grade_mod.grade_restructure}[name]
    return fn(ws).exps


def reconstruct_run1(repo: Path) -> str | None:
    """Reset a fixture the idempotency stage moved back to its corpus
    (run-1) state: tree = the 'run 1' commit's content, HEAD = eval-base.
    Returns an error string, or None."""
    log = git(repo, "log", "--format=%H %s").splitlines()
    run1 = next((l.split()[0] for l in log if l.endswith(" run 1")), None)
    if run1 is None:
        return "fixture moved but no 'run 1' commit found"
    base = git(repo, "rev-parse", "eval-base").strip()
    if not base:
        return "no eval-base tag"
    subprocess.run(["git", "-C", str(repo), "reset", "--hard", run1],
                   capture_output=True, check=True)
    subprocess.run(["git", "-C", str(repo), "clean", "-fdq"],
                   capture_output=True, check=True)
    subprocess.run(["git", "-C", str(repo), "reset", "--mixed", base],
                   capture_output=True, check=True)
    return None


def validate_substrate(ws: Path, name: str) -> tuple[list[dict] | None, str]:
    """(recorded expectations, "") when the live re-grade reproduces the
    recorded corpus verdict assert-by-assert; (None, reason) otherwise."""
    outdir = ws / SCENARIO_DIRS[name] / "outputs"
    recorded_file = outdir / "grading.json"
    if not recorded_file.exists():
        return None, "no recorded grading.json"
    recorded = json.loads(recorded_file.read_text())["expectations"]
    repo = ws / SCENARIO_DIRS[name] / "repo"
    if repo.exists() and grade_mod.fixture_off_base(repo):
        err = reconstruct_run1(repo)
        if err:
            return None, err
    live = grade_scenario(ws, name)
    rec_map = {e["text"]: e.get("verdict", "passed" if e["passed"] else "failed")
               for e in recorded}
    live_map = {e["text"]: e["verdict"] for e in live}
    if rec_map != live_map:
        diffs = sorted(set(rec_map) ^ set(live_map)) or sorted(
            k for k in rec_map if rec_map[k] != live_map.get(k))
        return None, f"re-grade disagrees with the recorded verdict: {diffs[:4]}"
    return recorded, ""


class Reverter:
    """Byte-restore for every path a mutation touches, plus git-HEAD
    restore for commit mutations. One substrate serves every mutation."""

    def __init__(self) -> None:
        self.saved: dict[Path, bytes | None] = {}
        self.git_heads: dict[Path, str] = {}

    def touch(self, path: Path) -> None:
        if path not in self.saved:
            self.saved[path] = path.read_bytes() if path.is_file() else None

    def touch_git(self, repo: Path) -> None:
        if repo not in self.git_heads:
            self.git_heads[repo] = git(repo, "rev-parse", "HEAD").strip()

    def restore(self) -> None:
        for repo, head in self.git_heads.items():
            subprocess.run(["git", "-C", str(repo), "reset", "--mixed", head],
                           capture_output=True, check=True)
        for path, content in self.saved.items():
            if content is None:
                if path.is_file():
                    path.unlink()
            else:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(content)
        self.saved.clear()
        self.git_heads.clear()


def run_pass(ws: Path, names: list[str]) -> int:
    killed: list[str] = []
    survived: list[tuple[str, str]] = []
    uncovered: list[str] = []
    unusable: list[tuple[str, str]] = []
    dup_groups: list[list[str]] = []

    for name in names:
        recorded, reason = validate_substrate(ws, name)
        if recorded is None:
            print(f"== {name}: UNUSABLE — {reason}")
            unusable.append((name, reason))
            continue
        muts = mutations_for(ws, name)
        # duplicates: identical canonical operations within one scenario
        by_key: dict[tuple, list[str]] = {}
        for a, m in muts.items():
            by_key.setdefault(m.key(), []).append(a)
        dup_groups += [sorted(v) for v in by_key.values() if len(v) > 1]

        n_killed = 0
        for exp in recorded:
            assert_name = exp["text"]
            mut = muts.get(assert_name)
            if mut is None:
                uncovered.append(f"{name}: {assert_name}")
                continue
            rev = Reverter()
            try:
                mut.apply(ws, rev)
                live = grade_scenario(ws, name)
            finally:
                rev.restore()
            verdict = next((e["verdict"] for e in live
                            if e["text"] == assert_name), "absent")
            ok = verdict == "failed" or (mut.probe and verdict != "passed")
            if ok:
                killed.append(f"{name}: {assert_name}")
                n_killed += 1
            else:
                survived.append((f"{name}: {assert_name}",
                                 f"{mut.describe()} -> {verdict}"))
        print(f"== {name}: {n_killed}/{len(recorded)} killed ==")

    total = len(killed) + len(survived) + len(uncovered)
    print(f"\n=== mutation pass: {total} asserts — {len(killed)} killed, "
          f"{len(survived)} survived, {len(uncovered)} uncovered, "
          f"{len(dup_groups)} duplicate group(s), "
          f"{len(unusable)} unusable scenario(s) ===")
    for a, why in survived:
        print(f"  SURVIVED   {a} — {why}")
    for a in uncovered:
        print(f"  UNCOVERED  {a}")
    for grp in dup_groups:
        print(f"  DUPLICATE  {', '.join(grp)}")
    for name, reason in unusable:
        print(f"  UNUSABLE   {name} — {reason}")
    return 1 if (survived or uncovered or unusable) else 0


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    ws = Path(sys.argv[1]).resolve()
    names = sys.argv[2:] or CORPUS
    sys.exit(run_pass(ws, names))


if __name__ == "__main__":
    main()
