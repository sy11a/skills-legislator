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
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
PLUGIN = REPO / "plugin"
HOOKS = PLUGIN / "hooks"

# The arm under test. Unset, this ruler measures the Python hook scripts; set, it
# measures the command it names — the .NET binary, addressed as `<cmd> hook <name>`
# — on the same payloads (BL-082, R-8205).
# Out of the LEGISLATOR_* namespace for the reason check_engine.py records: the binary
# reads that prefix as option keys and refuses an unknown one (BL-082 T-07).
# Required since T-13: the Python hooks are gone, so an unset variable no longer
# means "measure the other arm" — it means measure nothing while printing green.
HOOK_CMD = os.environ.get("PARITY_HOOK_CMD")
if not HOOK_CMD:
    sys.exit("set PARITY_HOOK_CMD to the legislator binary — the Python hooks are retired")

failures: list[str] = []


def check(ok: bool, label: str, detail: str = "") -> None:
    if ok:
        print(f"  ok    {label}")
    else:
        print(f"  FAIL  {label}" + (f" — {detail}" if detail else ""))
        failures.append(label)


def _hook_argv(script: Path) -> list[str]:
    """Argv for one hook under test: the binary carries it under the script's own stem."""
    return [HOOK_CMD, "hook", script.stem]


def run_hook_raw(script: Path, text: str):
    """Pipe `text` verbatim on stdin — the defensive cases, whose whole point is stdin
    that is not JSON. They go through the same argv as the rest: a call site that built
    its own would keep measuring Python after the arm was switched."""
    return subprocess.run(_hook_argv(script), input=text,
                          capture_output=True, text=True, timeout=15)


def run_hook(script: Path, payload: dict, cwd: Path | None = None):
    """Pipe `payload` as JSON on stdin into `script`; return CompletedProcess.

    The binary carries the same hook under the script's own stem, so both arms
    answer the identical payload.
    """
    return subprocess.run(
        _hook_argv(script),
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
print(f"arm: {HOOK_CMD or 'python3 plugin/hooks/<name>.py'}")
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

    # Case 3: editing the owned root wiring file opencode.json → blocked.
    oc_file = repo / "opencode.json"
    oc_file.write_text("{}")
    proc = run_hook(GUARD, edit_payload(str(oc_file)))
    check(proc.returncode == 2, "owned root opencode.json blocked (exit 2)",
          f"got exit {proc.returncode}, stderr={proc.stderr!r}")

    # Case 4: a different root config (package.json) is NOT guarded → allowed.
    pkg_file = repo / "package.json"
    pkg_file.write_text("{}")
    proc = run_hook(GUARD, edit_payload(str(pkg_file)))
    check(proc.returncode == 0, "non-owned root package.json allowed (exit 0)",
          f"got exit {proc.returncode}, stderr={proc.stderr!r}")

    # Case 5: docs/ai/engine.py is an ordinary file from v26 → allowed. The
    # engine left ownedFiles when the Python arm was retired (R-8207), so the
    # guard has no branch for it: a repo that keeps a file at that path keeps
    # its own file, and blocking it would guard law that no longer exists.
    engine_file = repo / "docs" / "ai" / "engine.py"
    engine_file.write_text("# engine\n")
    proc = run_hook(GUARD, edit_payload(str(engine_file)))
    check(proc.returncode == 0, "retired docs/ai/engine.py is an ordinary file (exit 0)",
          f"got exit {proc.returncode}, stderr={proc.stderr!r}")

    # Case 6: editing an unowned file under docs/ai/ → allowed.
    notes_file = repo / "docs" / "ai" / "notes.md"
    notes_file.write_text("# notes\n")
    proc = run_hook(GUARD, edit_payload(str(notes_file)))
    check(proc.returncode == 0, "unowned docs/ai/notes.md allowed (exit 0)",
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
proc = run_hook_raw(GUARD, "not json")
check(proc.returncode == 0, "guard_owned_files: malformed stdin allowed (exit 0)", f"got {proc.returncode}")
proc = run_hook_raw(GUARD, "")
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
proc = run_hook_raw(FORMAT, "not json")
check(proc.returncode == 0, "format_on_edit: malformed stdin allowed (exit 0)", f"got {proc.returncode}")


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
proc = run_hook_raw(OKF, "not json")
check(proc.returncode == 0, "okf_sync_check: malformed stdin allowed (exit 0)", f"got {proc.returncode}")



# =====================================================================
# Git-conduct guard (guard_git_conduct.py)
# =====================================================================
print("== guard_git_conduct.py ==")
CONDUCT = HOOKS / "guard_git_conduct.py"


def bash_payload(command: str, cwd: str) -> dict:
    return {
        "hook_event_name": "PreToolUse",
        "tool_name": "Bash",
        "tool_input": {"command": command},
        "cwd": cwd,
    }


def make_conduct_repo(root: Path, legislated: bool = True,
                      default: str = "master") -> None:
    root.mkdir(parents=True, exist_ok=True)
    if legislated:
        (root / "docs" / "ai").mkdir(parents=True)
        (root / "docs" / "ai" / "manifest.json").write_text("{}")
    subprocess.run(["git", "init", "-q", "-b", default], cwd=str(root), check=True)
    subprocess.run(["git", "config", "user.email", "t@example.com"], cwd=str(root), check=True)
    subprocess.run(["git", "config", "user.name", "T"], cwd=str(root), check=True)
    (root / "README.md").write_text("x\n")
    subprocess.run(["git", "add", "-A"], cwd=str(root), check=True)
    subprocess.run(["git", "commit", "-q", "-m", "init"], cwd=str(root), check=True)


def conduct(command: str, cwd: Path):
    return run_hook(CONDUCT, bash_payload(command, str(cwd)))


if not have_git:
    check(False, "git available for guard_git_conduct.py tests", "git not found on PATH")
else:
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "fleet-repo"
        make_conduct_repo(repo)
        subprocess.run(["git", "branch", "bl/064-x"], cwd=str(repo), check=True)

        # per R-641: merging while ON the default branch is blocked.
        proc = conduct("git merge bl/064-x", repo)
        check(proc.returncode == 2, "merge into default branch blocked (exit 2) per R-641",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")
        check("pair-development" in proc.stderr,
              "merge block message names the rule per R-641", f"stderr={proc.stderr!r}")

        # per R-641: the same merge from a feature branch is allowed.
        subprocess.run(["git", "checkout", "-q", "bl/064-x"], cwd=str(repo), check=True)
        proc = conduct("git merge master", repo)
        check(proc.returncode == 0, "merge INTO a feature branch allowed per R-641",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-642: pushing the default branch explicitly is blocked, from any branch.
        proc = conduct("git push origin master", repo)
        check(proc.returncode == 2, "push origin master blocked per R-642",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")
        proc = conduct("git push origin HEAD:master", repo)
        check(proc.returncode == 2, "push HEAD:master blocked per R-642",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-642: pushing a task branch is allowed.
        proc = conduct("git push -u origin bl/064-x", repo)
        check(proc.returncode == 0, "push of a task branch allowed per R-642",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-642: a bare push while ON the default branch is blocked.
        subprocess.run(["git", "checkout", "-q", "master"], cwd=str(repo), check=True)
        proc = conduct("git push", repo)
        check(proc.returncode == 2, "bare push on default branch blocked per R-642",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-641: a compound command hides no merge.
        proc = conduct("git fetch && git merge origin/master", repo)
        check(proc.returncode == 2, "merge inside a compound command blocked per R-641",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-641: aborting a merge is cleanup, not merging.
        proc = conduct("git merge --abort", repo)
        check(proc.returncode == 0, "merge --abort allowed per R-641",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-643: an AI-attribution trailer in a commit message is blocked.
        proc = conduct(
            'git commit -m "fix: x\n\nCo-Authored-By: Claude <noreply@anthropic.com>"', repo)
        check(proc.returncode == 2, "Claude co-author trailer blocked per R-643",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")
        check("attribution" in proc.stderr.lower(),
              "attribution block message says why per R-643", f"stderr={proc.stderr!r}")
        proc = conduct(
            'git commit -m "docs: y" -m "Generated with [Claude Code](https://claude.com/claude-code)"',
            repo)
        check(proc.returncode == 2, "Generated-with footer blocked per R-643",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-643: a human co-author and an ordinary message pass.
        proc = conduct(
            'git commit -m "fix: x\n\nCo-Authored-By: Jane Doe <jane@example.com>"', repo)
        check(proc.returncode == 0, "human co-author trailer allowed per R-643",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")
        proc = conduct('git commit -m "fix: ordinary message"', repo)
        check(proc.returncode == 0, "ordinary commit message allowed per R-643",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-643: attribution outside a commit/pr command is not the guard's business.
        proc = conduct('grep "Co-Authored-By: Claude" README.md', repo)
        check(proc.returncode == 0, "grep for the pattern allowed per R-643",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-644: attribution in a PR body is blocked; a clean body passes.
        proc = conduct(
            'gh pr create --title "x" --body "🤖 Generated with [Claude Code](https://claude.com/claude-code)"',
            repo)
        check(proc.returncode == 2, "attribution in gh pr body blocked per R-644",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")
        proc = conduct('gh pr create --title "x" --body "plain delivery notes"', repo)
        check(proc.returncode == 0, "clean gh pr body allowed per R-644",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-645: merging the PR is the user's act.
        proc = conduct("gh pr merge 23 --squash", repo)
        check(proc.returncode == 2, "gh pr merge blocked per R-645",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-646: a detached HEAD is undecidable — allow.
        subprocess.run(["git", "checkout", "-q", "--detach"], cwd=str(repo), check=True)
        proc = conduct("git merge bl/064-x", repo)
        check(proc.returncode == 0, "detached HEAD merge allowed (can't tell) per R-646",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")
        subprocess.run(["git", "checkout", "-q", "master"], cwd=str(repo), check=True)

    with tempfile.TemporaryDirectory() as tmp:
        # per R-647: outside a legislated repo the guard is a silent no-op.
        plain = Path(tmp) / "plain-repo"
        make_conduct_repo(plain, legislated=False)
        subprocess.run(["git", "branch", "topic"], cwd=str(plain), check=True)
        proc = conduct("git merge topic", plain)
        check(proc.returncode == 0, "non-legislated repo: merge on default allowed per R-647",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

    # per R-646: malformed stdin and non-git commands never block.
    proc = run_hook_raw(CONDUCT, "not json")
    check(proc.returncode == 0, "malformed stdin allowed (exit 0) per R-646",
          f"got {proc.returncode}")
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "fleet-repo2"
        make_conduct_repo(repo)
        proc = conduct("ls -la && echo done", repo)
        check(proc.returncode == 0, "non-git command allowed per R-646",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")



# --- BL-070 R-701: Windows-style command heads -------------------------
if have_git:
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "fleet-repo3"
        make_conduct_repo(repo)
        subprocess.run(["git", "branch", "topic"], cwd=str(repo), check=True)

        # per R-701: git.exe is git.
        proc = conduct("git.exe merge topic", repo)
        check(proc.returncode == 2, "git.exe merge on default branch blocked per R-701",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-701: a backslashed absolute Windows path is git.
        proc = conduct('"C:\\Program Files\\Git\\bin\\git.exe" push origin master', repo)
        check(proc.returncode == 2, "backslashed git.exe path push blocked per R-701",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-701: upper-case head is git.
        proc = conduct("GIT.EXE merge topic", repo)
        check(proc.returncode == 2, "GIT.EXE merge blocked per R-701",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")

        # per R-701 (control): a github-named binary is NOT git.
        proc = conduct("github merge topic", repo)
        check(proc.returncode == 0, "github-named binary not treated as git per R-701",
              f"got exit {proc.returncode}, stderr={proc.stderr!r}")


# =====================================================================
# hooks.json well-formedness
# =====================================================================
print("== hooks.json well-formed ==")
hooks_json_path = HOOKS / "hooks.json"
KNOWN_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit", "Bash", "Read",
               "Glob", "Grep", "WebFetch", "WebSearch", "Task", "NotebookRead"}
# The four names the binary answers to (R-8208, C-11). A fifth would be a hook
# nothing implements; a misspelling of one of these disables a guard silently,
# which is why the binary itself exits 2 on an unknown name.
KNOWN_HOOKS = {"guard_owned_files", "guard_git_conduct",
               "format_on_edit", "okf_sync_check"}

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
            # per R-8208: the command IS the binary and its hook name — three
            # words, no interpreter, no shim, no path into the package. The
            # shape is asserted before the name because a command that is not
            # this form carries no name to ask about.
            # per R-8208 (amended 2026-09-08): the command runs the BINARY -
            # no interpreter carries the hook - behind a PATH guard that exits
            # 0 when the arm is not installed, so a machine without it loses
            # neither the turn nor the quiet (R-8215). The shape is asserted
            # before the name because a command that is not this form carries
            # no name to ask about.
            named = ("legislator hook " in command
                     and ".py" not in command
                     and "exec legislator hook " in command
                     and "command -v legislator" in command)
            check(named, f"{event_name} command runs the binary behind a PATH guard per R-8208",
                  f"command={command!r}")
            if named:
                hook_name = command.split("exec legislator hook ", 1)[1].split()[0]
                check(hook_name in KNOWN_HOOKS,
                      f"{event_name} command names a known hook per C-11",
                      f"{hook_name!r} is not one of {sorted(KNOWN_HOOKS)}")



# --- R-8208: hooks.json's own command line, resolved through PATH -------
# R-702 pinned a launcher that resolved python3 -> py -> python. With the
# interpreter gone there is nothing left to resolve but the binary, so the
# requirement's TEXT dies here while its coverage does not: what still needs
# proving is that the command line hooks.json actually carries blocks an owned
# edit and lets an ordinary one through, found on PATH and nowhere else.
#
# The two halves are ONE assertion on purpose. The allow half alone is
# satisfied by any command that exits 0 — an absent script, a shim that gives
# up, a guard that was never wired — so a green there proves nothing unless the
# same command line is also shown to block. Only the pair catches removal.
print("== hooks.json command line through PATH (R-8208) ==")
with tempfile.TemporaryDirectory() as tmp:
    binroot = Path(tmp) / "bin"
    binroot.mkdir()
    import os as _os
    _os.symlink(shutil.which("sh"), binroot / "sh")
    _os.symlink(HOOK_CMD, binroot / "legislator")

    repo = Path(tmp) / "legislated"
    rules = repo / "docs" / "ai" / "rules" / "core"
    rules.mkdir(parents=True)
    (repo / "docs" / "ai" / "manifest.json").write_text("{}")
    rule = rules / "x.md"
    rule.write_text("## X\n")

    guard_entry = next(
        (h["command"]
         for e in events.get("PreToolUse", [])
         for h in e.get("hooks", [])
         if "guard_owned_files" in h.get("command", "")),
        None)

    def through_path(target: Path) -> int:
        """The exit code hooks.json's guard command gives for an edit of `target`."""
        return subprocess.run(
            ["sh", "-c", guard_entry],
            input=json.dumps(edit_payload(str(target))),
            capture_output=True, text=True, timeout=15,
            env={"PATH": str(binroot)}).returncode

    # R-8215: on a machine without the arm the hook gives up quietly - exit 0,
    # nothing on stderr - so an uninstalled fleet member loses neither its turn
    # nor its output. Measured through a PATH that holds `sh` and no arm, which
    # is the state of every repository the edition reaches before install.
    if guard_entry is not None:
        shonly = Path(tmp) / "shonly"
        shonly.mkdir()
        _os.symlink(shutil.which("sh"), shonly / "sh")
        absent = subprocess.run(
            ["sh", "-c", guard_entry],
            input=json.dumps(edit_payload(str(rule))),
            capture_output=True, text=True, timeout=15,
            env={"PATH": str(shonly)})
        check(absent.returncode == 0 and absent.stderr == "",
              "hooks.json's guard fails open and silent with no arm on PATH per R-8215",
              f"exit={absent.returncode} stderr={absent.stderr[:120]!r}")

    if guard_entry is None:
        # A missing entry is a finding, never a crash: the ruler that dies here
        # exits 1 exactly as a FAIL does, and every check below it silently
        # never runs (the fault this ruler carried until T-13).
        check(False, "hooks.json's PreToolUse guard blocks owned and allows ordinary per R-8208",
              "no PreToolUse hook command mentions guard_owned_files")
    else:
        blocked = through_path(rule)
        allowed = through_path(repo / "src" / "a.cs")
        check(blocked == 2 and allowed == 0,
              "hooks.json's PreToolUse guard blocks owned and allows ordinary per R-8208",
              f"owned edit exited {blocked} (want 2), ordinary edit exited {allowed} (want 0)")


# per R-641: the Bash matcher entry registering the git-conduct guard exists.
bash_entries = [e for e in events.get("PreToolUse", [])
                if e.get("matcher") == "Bash"]
check(any("hook guard_git_conduct" in h.get("command", "")
          for e in bash_entries for h in e.get("hooks", [])),
      "PreToolUse has a Bash entry running the git-conduct guard per R-641")


if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall hook checks passed")
