#!/usr/bin/env python3
"""Stop hook — reminds the session to sync the OKF when src/ changed.

If the working tree has uncommitted changes under src/** but none under
docs/okf/**, exit 2 with a reminder: the enforcement arm of okf.md's sync
law. Loop-safe: exits 0 immediately when the hook input carries
stop_hook_active: true (Claude Code sets this when a Stop hook already
fired for this stop), so the reminder fires at most once per stop.

Scope, by design (see the spec): uncommitted working-tree state only, via
`git status --porcelain`. Only active in legislated repos (a
docs/ai/manifest.json found at the git repo's toplevel); exits 0 outside a
legislated repo and exits 0 outside a git repo entirely.
"""
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

REMINDER = (
    "src/ changed but docs/okf/ didn't — update the OKF (map/log/glossary) "
    "or state why no update is needed."
)

GIT_TIMEOUT_S = 5


def git_toplevel(cwd: str) -> Path | None:
    try:
        result = subprocess.run(
            ["git", "rev-parse", "--show-toplevel"],
            cwd=cwd,
            capture_output=True,
            text=True,
            timeout=GIT_TIMEOUT_S,
        )
    except Exception:
        return None
    if result.returncode != 0:
        return None
    out = result.stdout.strip()
    if not out:
        return None
    return Path(out)


def changed_paths(repo_root: Path) -> list[str] | None:
    try:
        result = subprocess.run(
            ["git", "status", "--porcelain"],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            timeout=GIT_TIMEOUT_S,
        )
    except Exception:
        return None
    if result.returncode != 0:
        return None
    paths = []
    for line in result.stdout.splitlines():
        if len(line) < 4:
            continue
        rest = line[3:]
        if " -> " in rest:
            rest = rest.split(" -> ", 1)[1]
        rest = rest.strip().strip('"')
        if rest:
            paths.append(rest)
    return paths


def main() -> int:
    try:
        raw = sys.stdin.read()
        if not raw.strip():
            return 0
        data = json.loads(raw)
        if not isinstance(data, dict):
            return 0

        if data.get("stop_hook_active"):
            return 0

        cwd = data.get("cwd") or "."
        repo_root = git_toplevel(cwd)
        if repo_root is None:
            return 0

        if not (repo_root / "docs" / "ai" / "manifest.json").is_file():
            return 0

        paths = changed_paths(repo_root)
        if not paths:
            return 0

        src_changed = any(p == "src" or p.startswith("src/") for p in paths)
        okf_changed = any(p == "docs/okf" or p.startswith("docs/okf/") for p in paths)

        if src_changed and not okf_changed:
            sys.stderr.write(REMINDER)
            return 2

        return 0
    except Exception:
        return 0


if __name__ == "__main__":
    sys.exit(main())
