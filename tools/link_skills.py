#!/usr/bin/env python3
"""link_skills.py — curated skill linker for ~/.claude/skills (BL-070 port
of link-skills.sh; same contract, portable — per R-703 a failed symlink
fails loud with the remedy named, never a silent copy).

Links the KEEP list below from a source skill dump (default:
~/.agents/skills) into ~/.claude/skills as symlinks, so one `git pull` /
re-install of the source updates every linked skill. Pairs with legislator
audit check 14 (skill-bindings).

Usage:
  tools/link_skills.py              # link anything missing (idempotent)
  tools/link_skills.py --check      # print drift, change nothing, exit 1 on drift
  tools/link_skills.py --prune      # also remove ~/.claude/skills symlinks NOT
                                    # on the keep list that point into SRC
                                    # (never touches real directories or
                                    # links into other sources)
  SRC=/path/to/pack tools/link_skills.py    # alternate source
  DST=/path/to/skills tools/link_skills.py  # alternate destination (tests)
"""
from __future__ import annotations

import os
import sys
from pathlib import Path

# Curated keep list (BL-022 sweep verdicts; see
# docs/superpowers/specs/2026-07-12-skill-governance-design.md):
KEEP = [
    "grilling", "grill-me", "grill-with-docs",
    "tdd", "diagnosing-bugs",
    "design-an-interface", "codebase-design", "improve-codebase-architecture",
    "prototype", "handoff", "claude-handoff", "research",
    "resolving-merge-conflicts",
    "domain-modeling", "ubiquitous-language",
    "qa", "to-tickets", "triage", "request-refactor-plan", "wayfinder",
    "git-guardrails-claude-code",
    "code-review",
    "angular-developer", "angular-new-app", "find-skills",
    # stack + authored skills — repo-backed via the ~/.agents canonical
    # library (cross-harness parity with Claude Code; see software-dev KB
    # Home.md)
    "dotnet-refactoring", "aws-serverless-patterns", "aurelia-developer",
    "legislator",
]

SYMLINK_REMEDY = (
    "creating symlinks failed — on Windows enable Developer Mode (or run "
    "elevated); a copied directory would go stale silently and is not an "
    "acceptable fallback"
)


def make_link(src: Path, dst: Path, replace: bool) -> None:
    try:
        if replace and (dst.is_symlink() or dst.exists()):
            dst.unlink()
        os.symlink(src, dst, target_is_directory=True)
    except OSError as e:
        print(f"ERROR: {SYMLINK_REMEDY} ({dst}: {e})", file=sys.stderr)
        sys.exit(1)


def main(argv: list[str]) -> int:
    mode = "link"
    if argv[1:] == ["--check"]:
        mode = "check"
    elif argv[1:] == ["--prune"]:
        mode = "prune"
    elif argv[1:]:
        print(f"usage: {argv[0]} [--check|--prune]", file=sys.stderr)
        return 2

    src_root = Path(os.environ.get("SRC", Path.home() / ".agents" / "skills"))
    dst_root = Path(os.environ.get("DST", Path.home() / ".claude" / "skills"))

    drift = 0
    unresolved = 0  # conditions a run cannot fix (missing source, real dir)
    dst_root.mkdir(parents=True, exist_ok=True)

    for name in KEEP:
        src, dst = src_root / name, dst_root / name
        if not src.is_dir():
            print(f"MISSING SOURCE  {name}  (not in {src_root} — skill not installed there)")
            drift = 1
            unresolved = 1
            continue
        if dst.is_symlink():
            if dst.resolve() == src.resolve():
                continue  # correctly linked
            print(f"WRONG TARGET    {name}  ({os.readlink(dst)})")
            drift = 1
            if mode != "check":
                make_link(src, dst, replace=True)
                print(f"  relinked -> {src}")
        elif dst.exists():
            print(f"REAL DIR        {name}  (exists as a non-link at {dst} — left untouched)")
            drift = 1
            unresolved = 1
        else:
            print(f"NOT LINKED      {name}")
            drift = 1
            if mode != "check":
                make_link(src, dst, replace=False)
                print(f"  linked -> {src}")

    if mode == "prune":
        for dst in sorted(dst_root.iterdir()):
            if not dst.is_symlink() or dst.name in KEEP:
                continue
            # only prune links that point into SRC (never other sources)
            try:
                inside = dst.resolve().is_relative_to(src_root.resolve())
            except OSError:
                inside = False
            if inside:
                dst.unlink()
                print(f"PRUNED          {dst.name}  (off-list link into {src_root})")

    if mode == "check":
        if drift == 0:
            print("clean: all keep-list skills correctly linked")
        return drift
    # link/prune modes: fixable drift was fixed above; what remains
    # unresolved (MISSING SOURCE, REAL DIR) is an error, not a silent
    # partial run
    if unresolved:
        print("unresolved drift remains after run (see above)", file=sys.stderr)
        return 1
    print("done")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
