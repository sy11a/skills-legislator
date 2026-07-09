#!/usr/bin/env python3
"""Behavior tests for the legislator-hooks plugin — the "unit test" layer.

No agent involved; runs in seconds. Pipes crafted Claude Code hook JSON into
each hook script via subprocess and asserts exit codes (and, where the spec
calls for it, stderr content). Mirrors evals/check_static.py in spirit and
style: small check functions, readable pass/fail output, exit 1 on failure.

Usage: python3 evals/check_hooks.py
Exit code 0 = all checks pass; 1 = at least one failure (printed).
"""
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
PLUGIN = REPO / "plugin"
HOOKS = PLUGIN / "hooks"

failures: list[str] = []


def check(ok: bool, label: str, detail: str = "") -> None:
    if ok:
        print(f"  ok    {label}")
    else:
        print(f"  FAIL  {label}" + (f" — {detail}" if detail else ""))
        failures.append(label)


def run_hook(script: Path, payload: dict, cwd: Path | None = None):
    """Pipe `payload` as JSON on stdin into `script`; return CompletedProcess."""
    return subprocess.run(
        [sys.executable, str(script)],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        cwd=str(cwd) if cwd else None,
        timeout=15,
    )


def edit_payload(file_path: str) -> dict:
    return {
        "hook_event_name": "PreToolUse",
        "tool_name": "Edit",
        "tool_input": {
            "file_path": file_path,
            "old_string": "a",
            "new_string": "b",
        },
        "cwd": str(Path(file_path).parent),
    }


# =====================================================================
# Guard (guard_owned_files.py)
# =====================================================================
print("== guard_owned_files.py ==")
GUARD = HOOKS / "guard_owned_files.py"

with tempfile.TemporaryDirectory() as tmp:
    repo = Path(tmp) / "legislated-repo"
    rules_dir = repo / "docs" / "ai" / "rules" / "core"
    rules_dir.mkdir(parents=True)
    (repo / "docs" / "ai" / "manifest.json").write_text("{}")
    rule_file = rules_dir / "x.md"
    rule_file.write_text("## X\n")

    # Case 1: editing an owned rule file in a legislated repo → blocked.
    proc = run_hook(GUARD, edit_payload(str(rule_file)))
    check(proc.returncode == 2, "owned rule file in legislated repo blocked (exit 2)",
          f"got exit {proc.returncode}, stderr={proc.stderr!r}")
    check("machine-managed law" in proc.stderr,
          "block message mentions machine-managed law", f"stderr={proc.stderr!r}")

    # Case 2: editing a non-rules file in the same legislated repo → allowed.
    src_file = repo / "src" / "a.cs"
    proc = run_hook(GUARD, edit_payload(str(src_file)))
    check(proc.returncode == 0, "non-rules file in legislated repo allowed (exit 0)",
          f"got exit {proc.returncode}, stderr={proc.stderr!r}")

with tempfile.TemporaryDirectory() as tmp:
    # Case 3: same rules-shaped path, but no manifest.json anywhere upward
    # → not a legislated repo → allowed.
    unlegislated = Path(tmp) / "plain-repo"
    rules_dir = unlegislated / "docs" / "ai" / "rules" / "core"
    rules_dir.mkdir(parents=True)
    rule_file = rules_dir / "x.md"
    rule_file.write_text("## X\n")
    proc = run_hook(GUARD, edit_payload(str(rule_file)))
    check(proc.returncode == 0, "rules-shaped path with no manifest allowed (exit 0)",
          f"got exit {proc.returncode}, stderr={proc.stderr!r}")

# Defensive: malformed stdin never blocks.
proc = subprocess.run([sys.executable, str(GUARD)], input="not json",
                       capture_output=True, text=True, timeout=15)
check(proc.returncode == 0, "malformed stdin allowed (exit 0)", f"got {proc.returncode}")
proc = subprocess.run([sys.executable, str(GUARD)], input="",
                       capture_output=True, text=True, timeout=15)
check(proc.returncode == 0, "empty stdin allowed (exit 0)", f"got {proc.returncode}")


# =====================================================================
# Format (format_on_edit.py)
# =====================================================================
print("== format_on_edit.py ==")
FORMAT = HOOKS / "format_on_edit.py"

with tempfile.TemporaryDirectory() as tmp:
    # Case 1: .cs file with no dotnet project anywhere upward → exit 0, silent.
    cs_file = Path(tmp) / "foo.cs"
    cs_file.write_text("class Foo {}\n")
    proc = run_hook(FORMAT, edit_payload(str(cs_file)))
    check(proc.returncode == 0, ".cs file with no dotnet project: exit 0",
          f"got {proc.returncode}, stderr={proc.stderr!r}")
    check(proc.stdout == "" and proc.stderr == "",
          ".cs file with no dotnet project: no output",
          f"stdout={proc.stdout!r} stderr={proc.stderr!r}")

    # Case 2: non-code file → exit 0.
    txt_file = Path(tmp) / "notes.txt"
    txt_file.write_text("hello\n")
    proc = run_hook(FORMAT, edit_payload(str(txt_file)))
    check(proc.returncode == 0, "non-code file: exit 0", f"got {proc.returncode}")

# Defensive: malformed stdin never blocks.
proc = subprocess.run([sys.executable, str(FORMAT)], input="not json",
                       capture_output=True, text=True, timeout=15)
check(proc.returncode == 0, "malformed stdin allowed (exit 0)", f"got {proc.returncode}")


# =====================================================================
# OKF-sync (okf_sync_check.py)
# =====================================================================
print("== okf_sync_check.py ==")
OKF = HOOKS / "okf_sync_check.py"


def stop_payload(cwd: str, stop_hook_active: bool = False) -> dict:
    return {
        "hook_event_name": "Stop",
        "stop_reason": "end_turn",
        "stop_hook_active": stop_hook_active,
        "cwd": cwd,
    }


def make_legislated_git_repo(root: Path) -> None:
    (root / "docs" / "ai").mkdir(parents=True)
    (root / "docs" / "ai" / "manifest.json").write_text("{}")
    (root / "docs" / "okf").mkdir(parents=True)
    (root / "docs" / "okf" / "index.md").write_text("# OKF\n")
    (root / "src").mkdir(parents=True)
    (root / "src" / "a.txt").write_text("original\n")
    subprocess.run(["git", "init", "-q"], cwd=str(root), check=True)
    subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=str(root), check=True)
    subprocess.run(["git", "config", "user.name", "Test"], cwd=str(root), check=True)
    subprocess.run(["git", "add", "-A"], cwd=str(root), check=True)
    subprocess.run(["git", "commit", "-q", "-m", "init"], cwd=str(root), check=True)


have_git = shutil.which("git") is not None
if not have_git:
    check(False, "git available for okf_sync_check.py tests", "git not found on PATH")
else:
    with tempfile.TemporaryDirectory() as tmp:
        # Case 1: dirty src/ only → exit 2.
        repo = Path(tmp) / "repo1"
        repo.mkdir()
        make_legislated_git_repo(repo)
        (repo / "src" / "a.txt").write_text("changed\n")
        proc = run_hook(OKF, stop_payload(str(repo)))
        check(proc.returncode == 2, "dirty src/ only: exit 2",
              f"got {proc.returncode}, stderr={proc.stderr!r}")
        check("docs/okf" in proc.stderr, "reminder mentions docs/okf",
              f"stderr={proc.stderr!r}")

    with tempfile.TemporaryDirectory() as tmp:
        # Case 2: dirty src/ + docs/okf/ → exit 0.
        repo = Path(tmp) / "repo2"
        repo.mkdir()
        make_legislated_git_repo(repo)
        (repo / "src" / "a.txt").write_text("changed\n")
        (repo / "docs" / "okf" / "log.md").write_text("entry\n")
        proc = run_hook(OKF, stop_payload(str(repo)))
        check(proc.returncode == 0, "dirty src/ + docs/okf/: exit 0",
              f"got {proc.returncode}, stderr={proc.stderr!r}")

    with tempfile.TemporaryDirectory() as tmp:
        # Case 3: stop_hook_active True → exit 0 even though src/ alone is dirty.
        repo = Path(tmp) / "repo3"
        repo.mkdir()
        make_legislated_git_repo(repo)
        (repo / "src" / "a.txt").write_text("changed\n")
        proc = run_hook(OKF, stop_payload(str(repo), stop_hook_active=True))
        check(proc.returncode == 0, "stop_hook_active true: exit 0 (loop-safe)",
              f"got {proc.returncode}, stderr={proc.stderr!r}")

    with tempfile.TemporaryDirectory() as tmp:
        # Case 4: clean tree → exit 0.
        repo = Path(tmp) / "repo4"
        repo.mkdir()
        make_legislated_git_repo(repo)
        proc = run_hook(OKF, stop_payload(str(repo)))
        check(proc.returncode == 0, "clean tree: exit 0",
              f"got {proc.returncode}, stderr={proc.stderr!r}")

    with tempfile.TemporaryDirectory() as tmp:
        # Not a legislated repo (no manifest) → exit 0 regardless of dirt.
        repo = Path(tmp) / "repo5"
        repo.mkdir()
        (repo / "src").mkdir()
        (repo / "src" / "a.txt").write_text("x\n")
        subprocess.run(["git", "init", "-q"], cwd=str(repo), check=True)
        proc = run_hook(OKF, stop_payload(str(repo)))
        check(proc.returncode == 0, "non-legislated git repo: exit 0",
              f"got {proc.returncode}, stderr={proc.stderr!r}")

    with tempfile.TemporaryDirectory() as tmp:
        # Not a git repo at all → exit 0.
        plain = Path(tmp) / "not-a-repo"
        plain.mkdir()
        proc = run_hook(OKF, stop_payload(str(plain)))
        check(proc.returncode == 0, "outside any git repo: exit 0",
              f"got {proc.returncode}, stderr={proc.stderr!r}")

# Defensive: malformed stdin never blocks.
proc = subprocess.run([sys.executable, str(OKF)], input="not json",
                       capture_output=True, text=True, timeout=15)
check(proc.returncode == 0, "malformed stdin allowed (exit 0)", f"got {proc.returncode}")


# =====================================================================
# hooks.json well-formedness
# =====================================================================
print("== hooks.json well-formed ==")
hooks_json_path = HOOKS / "hooks.json"
KNOWN_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit", "Bash", "Read",
               "Glob", "Grep", "WebFetch", "WebSearch", "Task", "NotebookRead"}

try:
    hooks_data = json.loads(hooks_json_path.read_text())
    check(True, "hooks.json parses as JSON")
except Exception as e:
    hooks_data = {}
    check(False, "hooks.json parses as JSON", str(e))

events = hooks_data.get("hooks", {}) if isinstance(hooks_data, dict) else {}
check(bool(events), "hooks.json has at least one event", f"events={list(events)}")

for event_name, entries in events.items():
    if not isinstance(entries, list):
        check(False, f"{event_name} entries form a list")
        continue
    for entry in entries:
        matcher = entry.get("matcher")
        if matcher is not None:
            names = matcher.split("|")
            unknown = [n for n in names if n not in KNOWN_TOOLS]
            check(not unknown, f"{event_name} matcher names real tools ({matcher!r})",
                  f"unknown: {unknown}")
        for hook in entry.get("hooks", []):
            command = hook.get("command", "")
            # Extract the script path between the last '/hooks/' and the
            # trailing quote — matches the ${CLAUDE_PLUGIN_ROOT}/hooks/<x>.py form.
            found = None
            for part in command.replace('"', " ").split():
                if part.endswith(".py") and "/hooks/" in part:
                    found = part.rsplit("/hooks/", 1)[1]
                    break
            check(found is not None, f"{event_name} command references a hooks/*.py script",
                  f"command={command!r}")
            if found:
                script_path = HOOKS / found
                check(script_path.is_file(), f"{event_name} script exists: {found}",
                      f"expected at {script_path}")


if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall hook checks passed")
