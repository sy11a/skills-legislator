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
"""
from __future__ import annotations

import os
import signal
import subprocess
import sys


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
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
