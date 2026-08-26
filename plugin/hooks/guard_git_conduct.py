#!/usr/bin/env python3
"""PreToolUse hook — the git conduct guard (BL-064).

Matcher (see ../hooks.json): Bash. Enforces the pair-development git law
where the decision is taken — on the agent's command line — in legislated
repos only (a docs/ai/manifest.json up the tree from cwd):

- never merge into the default branch (`git merge` while ON it, and
  `gh pr merge` — merging is the user's act whatever the channel);
- never push the default branch (explicit refspec targeting it, or a
  bare/remote-only push while ON it);
- no AI attribution in the VCS record (`git commit` messages and
  `gh pr create|edit` title/body carrying a Claude/Anthropic
  Co-Authored-By trailer or a "Generated with" footer).

Contract: reads one JSON object from stdin (the Claude Code hook payload).
Exit 0 = allow — including EVERY "can't tell" case: malformed input, no
git, detached HEAD, unknown default branch, parser surprise. Exit 2 =
block; stderr is fed back to the model. A hook bug must never stop the
user's work, so the whole decision runs inside a blanket try/except that
allows. The human path stays open by construction: hooks fire on the
agent's tool calls only, never on the user's own terminal.
"""
from __future__ import annotations

import json
import re
import shlex
import subprocess
import sys
from pathlib import Path

MERGE_MSG = (
    "merging into the default branch is the user's act — push the task "
    "branch and leave merging to them (core/pair-development.md)."
)
PUSH_MSG = (
    "pushing the default branch is the user's act — push the task branch "
    "and leave integration to them (core/pair-development.md)."
)
PR_MERGE_MSG = (
    "merging the PR is the user's act, whatever the channel — leave it to "
    "them (core/pair-development.md)."
)
ATTRIBUTION_MSG = (
    "AI attribution in the VCS record is forbidden — drop the "
    "Co-Authored-By trailer / Generated-with footer "
    "(core/pair-development.md)."
)

ATTRIBUTION_PATTERNS = (
    re.compile(r"co-authored-by\s*:[^\n]{0,120}\b(claude|anthropic)\b",
               re.IGNORECASE),
    re.compile(r"generated\s+with\b.{0,80}\b(claude|anthropic)\b",
               re.IGNORECASE | re.DOTALL),
)

# Bash control operators shlex keeps as their own tokens; quoted
# occurrences stay inside their word and never split a segment.
SEGMENT_BREAKS = {"&&", "||", ";", "|", "&", ";;", "|&"}

# git global options that consume the following token.
GIT_OPTS_WITH_ARG = {"-C", "-c", "--exec-path", "--git-dir", "--work-tree",
                     "--namespace"}
# git-push options that consume the following token.
PUSH_OPTS_WITH_ARG = {"-o", "--push-option", "--receive-pack", "--exec",
                      "--repo"}


def find_repo_root(start: Path) -> Path | None:
    for ancestor in (start, *start.parents):
        if (ancestor / "docs" / "ai" / "manifest.json").is_file():
            return ancestor
    return None


def git_query(args: list[str], cwd: Path) -> str | None:
    try:
        proc = subprocess.run(["git", *args], cwd=str(cwd),
                              capture_output=True, text=True, timeout=5)
    except Exception:
        return None
    if proc.returncode != 0:
        return None
    return proc.stdout.strip()


def current_branch(cwd: Path) -> str | None:
    return git_query(["symbolic-ref", "--quiet", "--short", "HEAD"], cwd)


def default_branch(cwd: Path) -> str | None:
    head = git_query(["symbolic-ref", "--quiet", "--short",
                      "refs/remotes/origin/HEAD"], cwd)
    if head and "/" in head:
        return head.split("/", 1)[1]
    branches = git_query(["for-each-ref", "--format=%(refname:short)",
                          "refs/heads"], cwd)
    if branches is None:
        return None
    names = set(branches.splitlines())
    candidates = [b for b in ("main", "master") if b in names]
    return candidates[0] if len(candidates) == 1 else None


def segments(command: str) -> list[list[str]]:
    try:
        tokens = shlex.split(command, posix=True)
    except ValueError:
        return []
    out: list[list[str]] = []
    cur: list[str] = []
    for tok in tokens:
        if tok in SEGMENT_BREAKS:
            if cur:
                out.append(cur)
            cur = []
        else:
            cur.append(tok)
    if cur:
        out.append(cur)
    return out


def strip_env_prefix(seg: list[str]) -> list[str]:
    i = 0
    while i < len(seg) and re.match(r"^[A-Za-z_][A-Za-z0-9_]*=", seg[i]):
        i += 1
    return seg[i:]


def git_subcommand(seg: list[str]) -> tuple[str | None, list[str], Path | None]:
    """(subcommand, args after it, -C path if given) for a git segment."""
    c_path: Path | None = None
    i = 1
    while i < len(seg):
        tok = seg[i]
        if tok in GIT_OPTS_WITH_ARG:
            if tok == "-C" and i + 1 < len(seg):
                c_path = Path(seg[i + 1])
            i += 2
            continue
        if tok.startswith("-"):
            i += 1
            continue
        return tok, seg[i + 1:], c_path
    return None, [], c_path


def push_targets_default(args: list[str], default: str,
                         current: str | None) -> bool:
    positional: list[str] = []
    i = 0
    force_all = False
    while i < len(args):
        tok = args[i]
        if tok in PUSH_OPTS_WITH_ARG:
            i += 2
            continue
        if tok in ("--all", "--branches", "--mirror"):
            force_all = True
            i += 1
            continue
        if tok.startswith("-"):
            i += 1
            continue
        positional.append(tok)
        i += 1
    if force_all:
        return True
    refspecs = positional[1:]  # positional[0] is the remote
    if not refspecs:
        return current == default
    for rs in refspecs:
        rs = rs.lstrip("+")
        dst = rs.split(":", 1)[1] if ":" in rs else rs
        if dst.removeprefix("refs/heads/") == default:
            return True
    return False


def has_attribution(text: str) -> bool:
    return any(p.search(text) for p in ATTRIBUTION_PATTERNS)


def judge(command: str, cwd: Path) -> str | None:
    """Return a block message, or None to allow."""
    for raw_seg in segments(command):
        seg = strip_env_prefix(raw_seg)
        if not seg:
            continue
        head = seg[0].rsplit("/", 1)[-1]

        if head == "git":
            sub, args, c_path = git_subcommand(seg)
            gitdir = c_path if c_path is not None else cwd
            if sub == "merge":
                if any(a in ("--abort", "--quit") for a in args):
                    continue
                cur = current_branch(gitdir)
                default = default_branch(gitdir)
                if cur is not None and default is not None and cur == default:
                    return MERGE_MSG
            elif sub == "push":
                default = default_branch(gitdir)
                if default is not None and push_targets_default(
                        args, default, current_branch(gitdir)):
                    return PUSH_MSG
            elif sub == "commit":
                if has_attribution(" ".join(seg)):
                    return ATTRIBUTION_MSG

        elif head == "gh" and len(seg) >= 3 and seg[1] == "pr":
            if seg[2] == "merge":
                return PR_MERGE_MSG
            if seg[2] in ("create", "edit") and has_attribution(" ".join(seg)):
                return ATTRIBUTION_MSG
    return None


def main() -> int:
    try:
        raw = sys.stdin.read()
        if not raw.strip():
            return 0
        payload = json.loads(raw)
        if payload.get("tool_name") != "Bash":
            return 0
        command = (payload.get("tool_input") or {}).get("command")
        if not command or not isinstance(command, str):
            return 0
        if "git" not in command and "gh" not in command:
            return 0
        cwd = Path(payload.get("cwd") or Path.cwd())
        if find_repo_root(cwd) is None:
            return 0
        message = judge(command, cwd)
        if message:
            print(message, file=sys.stderr)
            return 2
        return 0
    except Exception:
        return 0


if __name__ == "__main__":
    sys.exit(main())
