#!/usr/bin/env python3
"""link_opencode_plugin.py — deploy the legislator opencode enforcement
plugin (BL-070 port of link-opencode-plugin.sh; same contract, portable —
per R-703 a failed symlink fails loud with the remedy named, never a
silent copy).

Symlinks plugin/opencode/legislator-guard.ts into
~/.config/opencode/plugins/ (global plugin dir, auto-loaded by opencode at
startup). The plugin is a no-op outside legislated repos, so loading it
globally is safe. Idempotent.

Pairs with the Claude Code hook plugin at plugin/hooks/ (deployed by
Claude's own plugin loader); this script is the opencode-side equivalent.

Usage:
  tools/link_opencode_plugin.py             # link (idempotent)
  tools/link_opencode_plugin.py --check     # print drift, change nothing, exit 1 on drift
  tools/link_opencode_plugin.py --unlink    # remove the symlink
  DST_DIR=/path tools/link_opencode_plugin.py  # alternate destination (tests)
"""
from __future__ import annotations

import os
import sys
from pathlib import Path

SYMLINK_REMEDY = (
    "creating the symlink failed — on Windows enable Developer Mode (or "
    "run elevated); a copied file would go stale silently and is not an "
    "acceptable fallback"
)


def main(argv: list[str]) -> int:
    mode = "link"
    if argv[1:] == ["--check"]:
        mode = "check"
    elif argv[1:] == ["--unlink"]:
        mode = "unlink"
    elif argv[1:]:
        print(f"usage: {argv[0]} [--check|--unlink]", file=sys.stderr)
        return 2

    here = Path(__file__).resolve().parent
    src = (here / ".." / "plugin" / "opencode" / "legislator-guard.ts").resolve()
    dst_dir = Path(os.environ.get("DST_DIR",
                                  Path.home() / ".config" / "opencode" / "plugins"))
    dst = dst_dir / "legislator-guard.ts"

    dst_dir.mkdir(parents=True, exist_ok=True)

    if not src.is_file():
        print(f"MISSING SOURCE  {src}", file=sys.stderr)
        return 1

    def link(replace: bool) -> None:
        try:
            if replace:
                dst.unlink()
            os.symlink(src, dst)
        except OSError as e:
            print(f"ERROR: {SYMLINK_REMEDY} ({dst}: {e})", file=sys.stderr)
            sys.exit(1)

    drift = 0
    if dst.is_symlink():
        if dst.resolve() == src:
            if mode == "unlink":
                dst.unlink()
                print(f"UNLINKED  {dst}")
            else:
                print(f"clean: {dst} -> {src}")
        else:
            print(f"WRONG TARGET  {dst} ({os.readlink(dst)})")
            drift = 1
            if mode == "link":
                link(replace=True)
                print(f"  relinked -> {src}")
    elif dst.exists():
        print(f"REAL FILE  {dst} (exists as a non-link — left untouched)",
              file=sys.stderr)
        return 1
    else:
        if mode == "unlink":
            print(f"nothing to unlink ({dst} absent)")
        else:
            print("NOT LINKED  legislator-guard.ts")
            drift = 1
            if mode == "link":
                link(replace=False)
                print(f"  linked -> {src}")

    if mode == "check":
        return drift
    if mode == "link":
        print("done")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
