#!/usr/bin/env python3
"""Component tests for the mutation runner (BL-063) — no agent, no live
workspace, seconds. The end-to-end pass runs against a recorded benchmark
workspace and is exercised per edition (POLICY §1c); these pin the
machinery that must not regress between editions: the kill criterion, the
byte-exact revert, and duplicate detection."""
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from mutate import Reverter, is_kill              # noqa: E402
from mutations import Mutation                    # noqa: E402

failures: list[str] = []


def check(ok: bool, label: str, detail: str = "") -> None:
    print(("  ok    " if ok else "  FAIL  ") + label
          + ("" if ok or not detail else f" — {detail}"))
    if not ok:
        failures.append(label)


print("== kill criterion (R-602) ==")
check(is_kill("failed", probe=False), "failed_is_a_kill")
check(not is_kill("passed", probe=False), "passed_is_survival")
check(not is_kill("unmeasured", probe=False),
      "unmeasured_is_not_a_kill_for_content_asserts: too-coarse mutation proves nothing")
check(is_kill("unmeasured", probe=True),
      "unmeasured_kills_a_probe: removal is the probe's lawful mutation")
check(not is_kill("absent", probe=False), "a_vanished_assert_is_not_a_kill")

print("== revert fidelity (R-606): every touched byte restored ==")
with tempfile.TemporaryDirectory() as td:
    root = Path(td)
    subprocess.run(["git", "-C", td, "init", "-q"], check=True)
    (root / "a.md").write_text("original a\n")
    (root / "sub").mkdir()
    (root / "sub" / "b.md").write_text("original b\n")
    subprocess.run(["git", "-C", td, "-c", "user.email=t@t", "-c",
                    "user.name=t", "add", "-A"], check=True)
    subprocess.run(["git", "-C", td, "-c", "user.email=t@t", "-c",
                    "user.name=t", "commit", "-qm", "seed"], check=True)
    head = subprocess.run(["git", "-C", td, "rev-parse", "HEAD"],
                          capture_output=True, text=True).stdout.strip()

    rev = Reverter()
    # edit, delete, create, and a git commit — all in one round
    rev.touch(root / "a.md"); (root / "a.md").write_text("mutated\n")
    rev.touch(root / "sub" / "b.md"); (root / "sub" / "b.md").unlink()
    rev.touch(root / "new.md"); (root / "new.md").write_text("planted\n")
    rev.touch_git(root)
    subprocess.run(["git", "-C", td, "-c", "user.email=t@t", "-c",
                    "user.name=t", "commit", "-aqm", "mutation commit"],
                   check=True)
    rev.restore()

    check((root / "a.md").read_text() == "original a\n", "edited_file_restored")
    check((root / "sub" / "b.md").read_text() == "original b\n", "deleted_file_restored")
    check(not (root / "new.md").exists(), "created_file_removed")
    now = subprocess.run(["git", "-C", td, "rev-parse", "HEAD"],
                         capture_output=True, text=True).stdout.strip()
    check(now == head, "git_head_restored", f"{now[:7]} != {head[:7]}")
    dirty = subprocess.run(["git", "-C", td, "status", "--porcelain"],
                           capture_output=True, text=True).stdout.strip()
    check(dirty == "", "worktree_clean_after_round_trip", dirty[:80])

print("== revert fidelity: a deleted symlink comes back as a symlink ==")
with tempfile.TemporaryDirectory() as td:
    root = Path(td)
    (root / "AGENTS.md").write_text("canonical\n")
    (root / "CLAUDE.md").symlink_to("AGENTS.md")
    rev = Reverter()
    rev.touch(root / "CLAUDE.md")
    (root / "CLAUDE.md").unlink()
    rev.restore()
    check((root / "CLAUDE.md").is_symlink(),
          "symlink_restored_as_symlink: bytes-only restore silently converts "
          "it to a regular file and poisons the substrate",
          "restored as a regular file")

print("== duplicate detection (R-604): identity is the canonical operation ==")
m1 = Mutation("remove-line", "report.md", "marker X", fn=lambda ws, r: None)
m2 = Mutation("remove-line", "report.md", "marker X", fn=lambda ws, r: None)
m3 = Mutation("remove-line", "report.md", "marker Y", fn=lambda ws, r: None)
check(m1.key() == m2.key(), "same_operation_same_key")
check(m1.key() != m3.key(), "different_args_different_key")

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO / "tools"))
try:
    from proc import acquire_lock, release_lock, LOCK_NAME   # noqa: E402
    HAVE_LOCK = True
except ImportError:
    HAVE_LOCK = False


def _held_by_live_pid(ws: Path, instrument: str = "evals-bg") -> subprocess.Popen:
    """Plant a lock whose holder is a live throwaway process."""
    holder = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(60)"])
    import json
    (ws / ".lock").write_text(json.dumps({
        "instrument": instrument, "pid": holder.pid,
        "started": "2026-08-28T00:00:00", "argv": ["x"]}) + "\n")
    return holder


def _dead_pid() -> int:
    p = subprocess.Popen([sys.executable, "-c", "pass"])
    p.wait()
    return p.pid


print("== workspace lock primitive (R-731, R-732, R-733) ==")
check(HAVE_LOCK, "proc.py_exports_acquire_lock_release_lock_LOCK_NAME")
if HAVE_LOCK:
    with tempfile.TemporaryDirectory() as td:
        ws = Path(td)
        ok, msg = acquire_lock(ws, "test-a", ["a"])
        check(ok and (ws / LOCK_NAME).exists(), "first_acquire_creates_lock", msg)
        import json
        rec = json.loads((ws / LOCK_NAME).read_text())
        check(rec.get("instrument") == "test-a" and rec.get("pid") == __import__("os").getpid()
              and rec.get("started") and rec.get("argv") == ["a"],
              "lock_records_instrument_pid_started_argv", str(rec))
        release_lock(ws)
        check(not (ws / LOCK_NAME).exists(), "release_removes_lock")

        holder = _held_by_live_pid(ws, "evals-bg")
        try:
            ok, msg = acquire_lock(ws, "mutate", ["m"])
            check(not ok, "live_holder_refuses_second_instrument")
            check("evals-bg" in msg and str(holder.pid) in msg and "2026-08-28" in msg,
                  "refusal_names_holder_instrument_pid_started", msg)
            still = json.loads((ws / LOCK_NAME).read_text())
            check(still.get("pid") == holder.pid, "refusal_leaves_holder_lock_intact")
        finally:
            holder.kill(); holder.wait()

        dead = _dead_pid()
        (ws / LOCK_NAME).write_text(json.dumps({
            "instrument": "evals-bg", "pid": dead, "started": "t0", "argv": []}) + "\n")
        ok, msg = acquire_lock(ws, "mutate", ["m"])
        check(ok, "dead_holder_is_taken_over", msg)
        check("stale" in msg.lower() and str(dead) in msg,
              "takeover_is_loud_and_names_the_dead_holder", msg)
        check(json.loads((ws / LOCK_NAME).read_text()).get("instrument") == "mutate",
              "takeover_rewrites_lock_to_new_holder")
        release_lock(ws)

print("== mutate.py refuses a locked workspace before writing (R-732, R-735) ==")
with tempfile.TemporaryDirectory() as td:
    ws = Path(td)
    holder = _held_by_live_pid(ws, "evals-bg")
    try:
        r = subprocess.run([sys.executable, str(REPO / "evals" / "mutate.py"), td,
                            "upgrade"], capture_output=True, text=True)
    finally:
        holder.kill(); holder.wait()
    out = r.stdout + r.stderr
    check(r.returncode != 0, "mutate_exits_nonzero_when_locked", f"rc={r.returncode}")
    check("evals-bg" in out and str(holder.pid) in out,
          "mutate_refusal_names_holder", out[-200:])
    check(sorted(p.name for p in ws.iterdir()) == [".lock"],
          "mutate_wrote_nothing_while_locked", str(sorted(p.name for p in ws.iterdir())))

print("== evals-bg.sh refuses a locked workspace before writing (R-732, R-734) ==")
import shutil
if shutil.which("bash") is None:
    print("  SKIPPED — bash not on PATH (runner is bash; nothing to drive)")
else:
    env = {**__import__("os").environ, "NO_BROWSER": "1"}
    with tempfile.TemporaryDirectory() as td:
        ws = Path(td)
        holder = _held_by_live_pid(ws, "mutate")
        try:
            r = subprocess.run(["bash", str(REPO / "tools" / "evals-bg.sh"), td],
                               capture_output=True, text=True, env=env)
        finally:
            holder.kill(); holder.wait()
        out = r.stdout + r.stderr
        check(r.returncode != 0, "runner_exits_nonzero_when_locked", f"rc={r.returncode}")
        check("mutate" in out and str(holder.pid) in out,
              "runner_refusal_names_holder", out[-200:])
        check(sorted(p.name for p in ws.iterdir()) == [".lock"],
              "runner_wrote_nothing_while_locked",
              str(sorted(p.name for p in ws.iterdir())))

    print("== evals-bg.sh releases the lock on a failed exit path (R-734) ==")
    print("== and wipes only the scenarios it will run (R-736) ==")
    with tempfile.TemporaryDirectory() as td:
        ws = Path(td)
        for sc in ("upgrade", "restructure"):
            (ws / sc / "outputs").mkdir(parents=True)
            (ws / sc / "outputs" / "grading.json").write_text("{}")
        # not materialized: the run dies at the stage-1 workspace gate,
        # which is AFTER the invocation-start cleanup — the cleanup's
        # footprint is observable without running an agent
        r = subprocess.run(["bash", str(REPO / "tools" / "evals-bg.sh"), td,
                            "--skip-smoke"], capture_output=True, text=True, env=env)
        check(r.returncode != 0, "unmaterialized_ws_still_fails_the_gate")
        check(not (ws / ".lock").exists(), "lock_released_after_gate_failure")
        check((ws / "upgrade" / "outputs" / "grading.json").exists(),
              "skip_smoke_leaves_upgrade_grading_in_place")
        check(not (ws / "restructure" / "outputs" / "grading.json").exists(),
              "scenario_this_run_touches_is_wiped")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall mutate checks passed")
