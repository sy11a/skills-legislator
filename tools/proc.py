#!/usr/bin/env python3
"""proc.py — portable process control for the eval runner (BL-068 E1/E2,
edition v23). Replaces the `setsid` / process-group `kill` / `stat -c%s`
Linux-isms in tools/evals-bg.sh with python's portable primitives.

Subcommands:
  spawn --log <file> -- <cmd...>   start detached (own session/group on
                                   POSIX, new process group on Windows),
                                   stdin closed, stdout+stderr appended to
                                   the log; prints the pid and returns.
  alive <pid>                      exit 0 if the process exists, 1 if not.
  stop <pid>                       terminate the process's whole group
                                   (POSIX killpg; Windows taskkill /T);
                                   best-effort, always exits 0.
  size <file>                      print the file's size in bytes (0 when
                                   absent) — the `stat -c%s` replacement.
  lock <ws> <instrument> <pid> [argv..]
                                   take the workspace lock (BL-073) on behalf
                                   of <pid> (the caller's own $$ — this
                                   helper exits at once): exit 0 when taken
                                   (a stale lock — dead holder — is taken
                                   over with one line on stdout); exit 1 with
                                   the live holder named on stderr, writing
                                   nothing.
  unlock <ws>                      release the workspace lock; always exit 0.

The workspace lock (BL-073): one instrument at a time. The runner and the
mutation pass both write the same workspace — a second runner invocation
wiped every scenario's grading.json, a mutation pass racing a runner lost
a reverted fixture line silently. `<ws>/.lock` is created exclusively and
records who holds it; a holder whose pid no longer exists is stale and is
taken over, loudly. Liveness is a pid probe, never an age.
"""
from __future__ import annotations

import datetime
import json
import os
import signal
import subprocess
import sys

LOCK_NAME = ".lock"


def _pid_alive(pid: int) -> bool:
    if pid <= 0:
        return False
    if os.name == "nt":
        out = subprocess.run(["tasklist", "/FI", f"PID eq {pid}", "/NH"],
                             capture_output=True, text=True).stdout
        return str(pid) in out
    try:
        os.kill(pid, 0)
        return True
    except ProcessLookupError:
        return False
    except PermissionError:
        return True   # exists, owned by someone else — still a live holder


def _write_lock(path: str, instrument: str, argv: list[str],
                flags: int, pid: int) -> None:
    fd = os.open(path, flags, 0o644)
    with os.fdopen(fd, "w", encoding="utf-8") as f:
        f.write(json.dumps({
            "instrument": instrument, "pid": pid,
            "started": datetime.datetime.now().isoformat(timespec="seconds"),
            "argv": list(argv)}) + "\n")


def acquire_lock(ws, instrument: str, argv: list[str],
                 pid: int | None = None) -> tuple[bool, str]:
    """(True, note) when `pid` (default: this process) now holds
    <ws>/.lock — note is empty, or the one loud line describing a stale
    lock taken over; (False, reason) when a live holder has it, naming
    instrument/pid/started. Nothing under ws is written on refusal."""
    pid = os.getpid() if pid is None else pid
    path = os.path.join(str(ws), LOCK_NAME)
    os.makedirs(str(ws), exist_ok=True)
    try:
        _write_lock(path, instrument, argv,
                    os.O_WRONLY | os.O_CREAT | os.O_EXCL, pid)
        return True, ""
    except FileExistsError:
        pass
    try:
        with open(path, encoding="utf-8") as f:
            rec = json.load(f)
    except (OSError, ValueError):
        rec = {}
    holder = rec.get("instrument", "unknown")
    hpid = int(rec.get("pid", 0) or 0)
    started = rec.get("started", "?")
    if _pid_alive(hpid):
        return False, (f"workspace locked by {holder} (pid {hpid}, started "
                       f"{started}) — one instrument at a time; wait for it "
                       f"or stop it, never run alongside it")
    _write_lock(path, instrument, argv,
                os.O_WRONLY | os.O_CREAT | os.O_TRUNC, pid)
    return True, (f"stale lock from {holder} (pid {hpid}, started {started}) "
                  f"— holder dead, taken over by {instrument}")


def release_lock(ws) -> None:
    try:
        os.unlink(os.path.join(str(ws), LOCK_NAME))
    except OSError:
        pass


def spawn(argv: list[str]) -> int:
    if len(argv) < 3 or argv[0] != "--log" or argv[2] != "--":
        print("usage: proc.py spawn --log <file> -- <cmd...>", file=sys.stderr)
        return 2
    log_path, cmd = argv[1], argv[3:]
    if not cmd:
        print("proc.py spawn: empty command", file=sys.stderr)
        return 2
    log = open(log_path, "ab")
    kwargs: dict = {"stdin": subprocess.DEVNULL, "stdout": log,
                    "stderr": subprocess.STDOUT}
    if os.name == "nt":
        kwargs["creationflags"] = (subprocess.CREATE_NEW_PROCESS_GROUP
                                   | getattr(subprocess, "CREATE_NO_WINDOW", 0))
    else:
        kwargs["start_new_session"] = True
    p = subprocess.Popen(cmd, **kwargs)
    print(p.pid)
    return 0


def alive(pid: int) -> int:
    try:
        os.kill(pid, 0)
        return 0
    except OSError:
        return 1


def stop(pid: int) -> int:
    try:
        if os.name == "nt":
            subprocess.run(["taskkill", "/T", "/F", "/PID", str(pid)],
                           capture_output=True)
        else:
            try:
                os.killpg(os.getpgid(pid), signal.SIGTERM)
            except OSError:
                os.kill(pid, signal.SIGTERM)
    except OSError:
        pass
    return 0


def size(path: str) -> int:
    try:
        print(os.path.getsize(path))
    except OSError:
        print(0)
    return 0


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__, file=sys.stderr)
        return 2
    cmd, rest = argv[1], argv[2:]
    if cmd == "spawn":
        return spawn(rest)
    if cmd == "alive" and len(rest) == 1:
        return alive(int(rest[0]))
    if cmd == "stop" and len(rest) == 1:
        return stop(int(rest[0]))
    if cmd == "size" and len(rest) == 1:
        return size(rest[0])
    if cmd == "lock" and len(rest) >= 3:
        ok, note = acquire_lock(rest[0], rest[1], rest[3:], pid=int(rest[2]))
        if ok:
            if note:
                print(note)
            return 0
        print(note, file=sys.stderr)
        return 1
    if cmd == "unlock" and len(rest) == 1:
        release_lock(rest[0])
        return 0
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
