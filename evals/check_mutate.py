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

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall mutate checks passed")
