#!/usr/bin/env python3
"""PreToolUse hook — blocks hand-edits to legislator-owned rule files.

Matcher (see ../hooks.json): Edit|Write|MultiEdit|NotebookEdit.

Reads the tool call's file path, walks up the directory tree looking for
docs/ai/manifest.json (the "is this a legislated repo?" test), and — if
found — blocks (exit 2) when the file lies under that repo's
docs/ai/rules/**. docs/ai/manifest.json itself is deliberately NOT guarded:
SKILL.md Step 3.7 rewrites it with the Write tool on every run, and that
rewrite already heals hand-edits; guarding it would block legislator's own
runs. See docs/superpowers/specs/2026-07-09-hooks-plugin-design.md.

Contract: reads one JSON object from stdin (the Claude Code hook payload).
Exit 0 = allow (including every "can't tell" / malformed-input case).
Exit 2 = block; stderr text is fed back to the model. This script must
never raise or block for any reason other than the one intentional guard
below — a hook crash must not stop the user's work.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

BLOCK_MESSAGE = (
    "docs/ai/rules/** is machine-managed law — edit the legislator skill "
    "source and re-run /legislator instead."
)


def find_repo_root(start: Path) -> Path | None:
    """Walk up from `start` (inclusive) looking for docs/ai/manifest.json."""
    for ancestor in (start, *start.parents):
        if (ancestor / "docs" / "ai" / "manifest.json").is_file():
            return ancestor
    return None


def resolve_file_path(tool_input: dict, cwd: str) -> Path | None:
    file_path = tool_input.get("file_path") or tool_input.get("notebook_path")
    if not file_path or not isinstance(file_path, str):
        return None
    p = Path(file_path)
    if not p.is_absolute():
        base = Path(cwd) if cwd else Path.cwd()
        p = base / p
    try:
        return p.resolve()
    except (OSError, RuntimeError):
        return None


def main() -> int:
    try:
        raw = sys.stdin.read()
        if not raw.strip():
            return 0
        data = json.loads(raw)
        if not isinstance(data, dict):
            return 0

        tool_input = data.get("tool_input")
        if not isinstance(tool_input, dict):
            return 0

        file_path = resolve_file_path(tool_input, data.get("cwd", ""))
        if file_path is None:
            return 0

        repo_root = find_repo_root(file_path.parent)
        if repo_root is None:
            return 0

        rules_dir = repo_root / "docs" / "ai" / "rules"
        try:
            file_path.relative_to(rules_dir)
        except ValueError:
            return 0

        sys.stderr.write(BLOCK_MESSAGE)
        return 2
    except Exception:
        # Never let a bug in this hook block the user's work.
        return 0


if __name__ == "__main__":
    sys.exit(main())
