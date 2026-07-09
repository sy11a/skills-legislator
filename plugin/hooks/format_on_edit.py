#!/usr/bin/env python3
"""PostToolUse hook — best-effort formatting of a just-edited file.

Matcher (see ../hooks.json): Edit|Write|MultiEdit. Timeout 10s set there.

.cs files: run `dotnet format <nearest .sln/.csproj upward> --include
<file>` if `dotnet` is on PATH and a project file is found.
.ts/.js/.html/.css files: run `npx prettier --write <file>` if a prettier
config is found upward.

This hook NEVER blocks and NEVER fails: every path — missing toolchain,
formatter error, malformed input — ends in exit 0. It is pure best-effort
polish, not a gate. Deleting the corresponding mechanically-enforced style
rules from coding-standards.md is an explicit follow-up (see the design
spec), not done here.
"""
from __future__ import annotations

import json
import shutil
import subprocess
import sys
from pathlib import Path

# Hard ceiling per formatter invocation — belt-and-suspenders alongside the
# 10s timeout hooks.json sets on the whole hook process.
SUBPROCESS_TIMEOUT_S = 8

PRETTIER_EXTS = {".ts", ".tsx", ".js", ".jsx", ".html", ".css"}
PRETTIER_CONFIG_NAMES = (
    ".prettierrc",
    ".prettierrc.json",
    ".prettierrc.yml",
    ".prettierrc.yaml",
    ".prettierrc.js",
    ".prettierrc.cjs",
    "prettier.config.js",
    "prettier.config.cjs",
)


def resolve_file_path(tool_input: dict, cwd: str) -> Path | None:
    file_path = tool_input.get("file_path") or tool_input.get("notebook_path")
    if not file_path or not isinstance(file_path, str):
        return None
    p = Path(file_path)
    if not p.is_absolute():
        base = Path(cwd) if cwd else Path.cwd()
        p = base / p
    return p


def find_upward(start_dir: Path, matches) -> Path | None:
    """Return the first ancestor directory (inclusive) where `matches(dir)`
    finds something, or None."""
    for ancestor in (start_dir, *start_dir.parents):
        found = matches(ancestor)
        if found is not None:
            return found
    return None


def find_dotnet_project(start_dir: Path) -> Path | None:
    def look(d: Path):
        entries = [*d.glob("*.sln"), *d.glob("*.csproj")]
        return entries[0] if entries else None

    return find_upward(start_dir, look)


def find_prettier_config(start_dir: Path) -> Path | None:
    def look(d: Path):
        for name in PRETTIER_CONFIG_NAMES:
            candidate = d / name
            if candidate.is_file():
                return candidate
        pkg = d / "package.json"
        if pkg.is_file():
            try:
                data = json.loads(pkg.read_text())
                if isinstance(data, dict) and "prettier" in data:
                    return pkg
            except Exception:
                pass
        return None

    return find_upward(start_dir, look)


def run_quiet(cmd) -> None:
    try:
        subprocess.run(
            cmd,
            capture_output=True,
            timeout=SUBPROCESS_TIMEOUT_S,
            check=False,
        )
    except Exception:
        pass


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

        ext = file_path.suffix.lower()

        if ext == ".cs":
            dotnet = shutil.which("dotnet")
            if not dotnet:
                return 0
            project = find_dotnet_project(file_path.parent)
            if not project:
                return 0
            run_quiet([dotnet, "format", str(project), "--include", str(file_path)])
            return 0

        if ext in PRETTIER_EXTS:
            config = find_prettier_config(file_path.parent)
            if not config:
                return 0
            npx = shutil.which("npx")
            if not npx:
                return 0
            run_quiet([npx, "prettier", "--write", str(file_path)])
            return 0

        return 0
    except Exception:
        return 0


if __name__ == "__main__":
    sys.exit(main())
