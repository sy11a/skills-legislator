#!/usr/bin/env bash
# fleet.sh — constitution fleet control: discover legislated repos, deliver updates.
#
# There is NO fleet registry to maintain: discovery is a filesystem scan for
# docs/ai/manifest.json (the manifests ARE the database — derived, rebuildable,
# cannot drift). Delivery is one headless `opencode run` per stale repo: the
# agent follows SKILL.md upgrade mode (owned-file refresh is deterministic,
# write-guarded, idempotent), and every project-owned proposal (AGENTS.md
# import lines) lands in a per-repo file under the proposals dir for one
# human review session. Nothing here ever commits.
#
# Usage:
#   tools/fleet.sh status                 # table: repo → version vs skill VERSION; exit 1 if any behind
#   tools/fleet.sh upgrade [options]      # headless-upgrade every stale repo, sequentially
#     --dry-run                           #   print what would run, run nothing
#     --only NAME                         #   upgrade just this repo (pilot); repeatable
#     --exclude NAME                      #   skip this repo (e.g. an archive); repeatable
#     --model provider/model              #   pass through to `opencode run`
#
# Env:
#   SCAN_ROOTS     dirs to scan (default: "$HOME/Repository $HOME/Agent")
#   PROPOSALS_DIR  where Step 7 reports land
#                  (default: "$HOME/Knowledge/_generated/legislator-proposals")
#
# A repo with a dirty working tree is skipped (the upgrade leaves uncommitted
# changes for review — they must not mix with unrelated edits). Proposal files
# are derived artifacts: overwritten on every run, deleted by you once applied.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="$(cd "$HERE/../skill" && pwd)"
SKILL_MD="$SKILL_DIR/SKILL.md"
CURRENT_VERSION="$(cat "$SKILL_DIR/VERSION")"
SCAN_ROOTS="${SCAN_ROOTS:-$HOME/Repository $HOME/Agent}"
PROPOSALS_DIR="${PROPOSALS_DIR:-$HOME/Knowledge/_generated/legislator-proposals}"

usage() { sed -n '2,26p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2; }

# discover: print "repo_path<TAB>version" per legislated repo, sorted by path.
discover() {
  # shellcheck disable=SC2086 — SCAN_ROOTS is intentionally word-split
  find $SCAN_ROOTS -maxdepth 4 -path '*/docs/ai/manifest.json' 2>/dev/null | sort | while read -r manifest; do
    repo="${manifest%/docs/ai/manifest.json}"
    version="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1])).get('legislatorVersion','?'))" "$manifest" 2>/dev/null || echo '?')"
    printf '%s\t%s\n' "$repo" "$version"
  done
}

cmd_status() {
  behind=0 total=0
  printf '%-55s %-8s %s\n' "repo" "version" "state"
  while IFS=$'\t' read -r repo version; do
    total=$((total + 1))
    if [ "$version" = "$CURRENT_VERSION" ]; then
      state="ok"
    else
      state="behind (skill: v$CURRENT_VERSION)"
      behind=$((behind + 1))
    fi
    printf '%-55s %-8s %s\n' "$repo" "v$version" "$state"
  done < <(discover)
  echo
  echo "$total legislated repo(s), $behind behind skill v$CURRENT_VERSION"
  [ "$behind" -eq 0 ]
}

upgrade_prompt() {
  local repo_name="$1"
  cat <<EOF
Read $SKILL_MD and follow it EXACTLY, including any referenced files under $SKILL_DIR/. This repo is already legislated (docs/ai/manifest.json exists) — upgrade mode is expected; take profiles from the manifest without re-asking. Re-run the legislator so the repo picks up the current constitution.

Ground rules: NEVER run git commit — leave all changes uncommitted for review. AGENTS.md is project-owned — never edit it; every proposed AGENTS.md change (@import lines to add or remove, wiring) goes in the Step 7 report only. When the skill requires byte-for-byte copies via Bash cp, use Bash cp exactly as instructed. Write your full Step 7 report (all sections, including Health and any Constitution candidates) to $PROPOSALS_DIR/$repo_name.md — overwrite it if it exists.
EOF
}

cmd_upgrade() {
  local dry_run=0 model=""
  local -a only=() exclude=()
  while [ $# -gt 0 ]; do
    case "$1" in
      --dry-run) dry_run=1 ;;
      --only)    only+=("$2"); shift ;;
      --exclude) exclude+=("$2"); shift ;;
      --model)   model="$2"; shift ;;
      *) usage ;;
    esac
    shift
  done

  mkdir -p "$PROPOSALS_DIR"
  local ran=0 skipped=0
  while IFS=$'\t' read -r repo version; do
    name="$(basename "$repo")"
    [ "$version" = "$CURRENT_VERSION" ] && continue
    if [ ${#only[@]} -gt 0 ] && ! printf '%s\n' "${only[@]}" | grep -qxF "$name"; then continue; fi
    if [ ${#exclude[@]} -gt 0 ] && printf '%s\n' "${exclude[@]}" | grep -qxF "$name"; then
      echo "skip  $name — excluded"; skipped=$((skipped + 1)); continue
    fi
    if [ -n "$(git -C "$repo" status --porcelain 2>/dev/null)" ]; then
      echo "skip  $name — dirty working tree (commit or stash first)"; skipped=$((skipped + 1)); continue
    fi
    if [ "$dry_run" -eq 1 ]; then
      echo "would upgrade  $name (v$version → v$CURRENT_VERSION)"; ran=$((ran + 1)); continue
    fi

    echo "== $name: v$version → v$CURRENT_VERSION =="
    if opencode run --dir "$repo" ${model:+--model "$model"} "$(upgrade_prompt "$name")"; then
      after="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1])).get('legislatorVersion','?'))" "$repo/docs/ai/manifest.json" 2>/dev/null || echo '?')"
      if [ "$after" = "$CURRENT_VERSION" ]; then
        echo "ok    $name at v$after — review the diff, then apply $PROPOSALS_DIR/$name.md and commit"
      else
        echo "WARN  $name still at v$after — read the run output above"
      fi
    else
      echo "FAIL  $name — opencode run exited non-zero"
    fi
    ran=$((ran + 1))
  done < <(discover)

  echo
  echo "$ran repo(s) processed, $skipped skipped. Proposals: $PROPOSALS_DIR/"
}

case "${1:-}" in
  status)  cmd_status ;;
  upgrade) shift; cmd_upgrade "$@" ;;
  *) usage ;;
esac
