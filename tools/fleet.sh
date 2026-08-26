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
#     --runner opencode|claude            #   which engine drives the upgrade
#     --model NAME                        #   pass through to the runner
#
# `upgrade` exits non-zero when any repository did not reach the current
# version: a run that failed, a run that left the repo behind, or a repo
# skipped for a dirty tree. `--exclude` is a deliberate operator choice and
# does not fail the sweep. A sweep is normally read from a scrollback, so the
# exit code is the only signal a wrapper or a cron line can trust.
#
# Runner profiles — delivery is engine-agnostic (the skill's owned-file work
# is `cp` plus a regenerated manifest, and any harness that can follow
# SKILL.md performs it identically), so only the invocation differs:
#   opencode (default) — `opencode run --agent service-fleet`
#   claude             — `claude -p`, prompt on stdin, cwd = the repo
# The second profile exists so that one vendor's credential cannot stop the
# fleet: on 2026-08-23 the opencode agent's key was rejected and not one
# repository could be upgraded. It carries a known asymmetry — the
# `service-fleet` agent name is what makes fleet-obs exclude a sweep from
# practice metrics (its ADR-0039), and its Claude Code adapter records no
# agent identity at all, so a sweep run under `claude` counts as practice and
# inflates that week's lenses. Use it when opencode is unavailable, and read
# the next report knowing this.
#
# Env:
#   SCAN_ROOTS     dirs to scan (default: "$HOME/Repository $HOME/Agent")
#   PROPOSALS_DIR  where Step 7 reports land
#                  (default: "$HOME/Knowledge/_generated/legislator-proposals")
#   RUNNER         default runner profile (default: opencode)
#   MODEL          default model handed to the runner
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
RUNNER="${RUNNER:-opencode}"
MODEL="${MODEL:-}"

# the whole header block, however long it grows — a line range here went
# stale the moment the header was edited
usage() { awk 'NR>1 { if (!/^#/) exit; sub(/^# ?/, ""); print }' "${BASH_SOURCE[0]}"; exit 2; }

# discover: print "repo_path<TAB>version" per legislated repo, sorted by path.
discover() {
  # shellcheck disable=SC2086 — SCAN_ROOTS is intentionally word-split
  find $SCAN_ROOTS -maxdepth 4 -path '*/docs/ai/manifest.json' 2>/dev/null | sort | while read -r manifest; do
    repo="${manifest%/docs/ai/manifest.json}"
    version="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1])).get('legislatorVersion','?'))" "$manifest" 2>/dev/null || echo '?')"
    printf '%s\t%s\n' "$repo" "$version"
  done
}

manifest_version_at_head() { # <repo> -> committed version, or "" (no git / no commit / not tracked)
  git -C "$1" show HEAD:docs/ai/manifest.json 2>/dev/null \
    | python3 -c 'import json,sys;print(json.load(sys.stdin).get("legislatorVersion","?"))' 2>/dev/null || true
}

cmd_status() {
  # BL-056: the version column is the COMMITTED one. On 2026-08-24 status
  # read the working tree and reported three repositories delivered whose
  # HEAD said otherwise — an unreviewed sweep diff counted as delivery, and
  # the dirty-tree skip then parked them there indefinitely. Law that is
  # not committed is not there; a worktree that differs from HEAD is
  # "pending review", visibly, and never ok.
  behind=0 total=0
  printf '%-55s %-8s %s\n' "repo" "version" "state"
  while IFS=$'\t' read -r repo wt_version; do
    total=$((total + 1))
    committed="$(manifest_version_at_head "$repo")"
    if [ -z "$committed" ]; then
      version="$wt_version"
      if [ "$wt_version" = "$CURRENT_VERSION" ]; then
        state="ok (no-git — nothing committed to trust)"
      else
        state="behind (skill: v$CURRENT_VERSION; no-git)"
        behind=$((behind + 1))
      fi
    elif [ "$committed" != "$wt_version" ]; then
      version="$committed"
      state="pending review (worktree v$wt_version, uncommitted)"
      behind=$((behind + 1))
    elif [ "$committed" = "$CURRENT_VERSION" ]; then
      version="$committed"
      state="ok"
    else
      version="$committed"
      state="behind (skill: v$CURRENT_VERSION)"
      behind=$((behind + 1))
    fi
    printf '%-55s %-8s %s\n' "$repo" "v$version" "$state"
  done < <(discover)
  echo
  echo "$total legislated repo(s), $behind not delivered at skill v$CURRENT_VERSION (committed)"
  [ "$behind" -eq 0 ]
}

upgrade_prompt() {
  local repo_name="$1"
  cat <<EOF
Read $SKILL_MD and follow it EXACTLY, including any referenced files under $SKILL_DIR/. This repo is already legislated (docs/ai/manifest.json exists) — upgrade mode is expected; take profiles from the manifest without re-asking. Re-run the legislator so the repo picks up the current constitution.

Ground rules: NEVER run git commit — leave all changes uncommitted for review. The skill's own law governs what each mode may write to the entry document — do not infer extra prohibitions from these ground rules. When the skill requires byte-for-byte copies via Bash cp, use Bash cp exactly as instructed. Write your full Step 7 report (all sections, including Health and any Constitution candidates) to $PROPOSALS_DIR/$repo_name.md — overwrite it if it exists.
EOF
}

run_upgrade_agent() { # <repo-path> <repo-name> — 0 iff the runner exited clean
  local repo="$1" name="$2" prompt
  prompt="$(upgrade_prompt "$name")"
  case "$RUNNER" in
    opencode)
      # </dev/null: opencode reads stdin and would swallow the rest of the
      # repo list. --agent service-fleet marks the session as a service run so
      # fleet-obs excludes it from practice metrics (its ADR-0039); the agent
      # must exist in the machine's opencode config (~/.config/opencode/agents/).
      # shellcheck disable=SC2086 — ${MODEL:+...} is meant to word-split
      opencode run --dir "$repo" --agent service-fleet ${MODEL:+--model "$MODEL"} \
        "$prompt" </dev/null
      ;;
    claude)
      # The prompt goes on STDIN on purpose. --add-dir is variadic, so
      # `claude -p --add-dir "$DIR" "$prompt"` reads the prompt as a second
      # directory and the agent starts with no task at all — one wasted pass
      # on 2026-08-23. With no positional argument there is nothing to
      # swallow, whatever flag order a later edit introduces.
      # shellcheck disable=SC2086 — ${MODEL:+...} is meant to word-split
      ( cd "$repo" && printf '%s' "$prompt" | claude -p \
          --permission-mode bypassPermissions \
          --add-dir "$SKILL_DIR" --add-dir "$PROPOSALS_DIR" \
          ${MODEL:+--model "$MODEL"} )
      ;;
    *) echo "unknown runner profile: $RUNNER" >&2; return 2 ;;
  esac
}

cmd_upgrade() {
  local dry_run=0
  local -a only=() exclude=()
  while [ $# -gt 0 ]; do
    case "$1" in
      --dry-run) dry_run=1 ;;
      --only)    only+=("$2"); shift ;;
      --exclude) exclude+=("$2"); shift ;;
      --runner)  RUNNER="$2"; shift ;;
      --model)   MODEL="$2"; shift ;;
      *) usage ;;
    esac
    shift
  done
  # a typo must cost nothing, not one failed run per repository
  case "$RUNNER" in
    opencode|claude) ;;
    *) echo "unknown runner profile: $RUNNER (expected: opencode | claude)" >&2; exit 2 ;;
  esac

  mkdir -p "$PROPOSALS_DIR"
  # every repository ends in exactly one of these; the last three are the
  # ones that mean "did not reach v$CURRENT_VERSION"
  local ok=0 planned=0 excluded=0 failed=0 behind=0 dirty=0
  while IFS=$'\t' read -r repo version; do
    name="$(basename "$repo")"
    [ "$version" = "$CURRENT_VERSION" ] && continue
    if [ ${#only[@]} -gt 0 ] && ! printf '%s\n' "${only[@]}" | grep -qxF "$name"; then continue; fi
    if [ ${#exclude[@]} -gt 0 ] && printf '%s\n' "${exclude[@]}" | grep -qxF "$name"; then
      echo "skip  $name — excluded"; excluded=$((excluded + 1)); continue
    fi
    if [ -n "$(git -C "$repo" status --porcelain 2>/dev/null)" ]; then
      echo "skip  $name — dirty working tree (commit or stash first)"; dirty=$((dirty + 1)); continue
    fi
    if [ "$dry_run" -eq 1 ]; then
      echo "would upgrade  $name (v$version → v$CURRENT_VERSION)"; planned=$((planned + 1)); continue
    fi

    echo "== $name: v$version → v$CURRENT_VERSION ($RUNNER) =="
    # BL-061: the exit code is evidence, never the verdict. The success
    # branch already re-read the manifest (a runner can exit 0 having
    # achieved nothing — BL-053's WARN); the failure branch trusted the
    # code, and on the 2026-08-25 sweep reported FAIL over a completed
    # delivery — the session limit killed the CLI after the byte-copies
    # were done. Both branches now re-read and decide on the version.
    rc=0
    run_upgrade_agent "$repo" "$name" || rc=$?
    after="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1])).get('legislatorVersion','?'))" "$repo/docs/ai/manifest.json" 2>/dev/null || echo '?')"
    if [ "$after" = "$CURRENT_VERSION" ]; then
      if [ "$rc" -eq 0 ]; then
        echo "ok    $name at v$after — review the diff, then apply $PROPOSALS_DIR/$name.md and commit"
      else
        echo "ok    $name at v$after (runner exited $rc after delivery — the version is the verdict; the Step 7 report at $PROPOSALS_DIR/$name.md may be truncated)"
      fi
      ok=$((ok + 1))
    elif [ "$rc" -eq 0 ]; then
      echo "WARN  $name still at v$after — read the run output above"
      behind=$((behind + 1))
    else
      echo "FAIL  $name still at v$after — $RUNNER exited $rc"
      failed=$((failed + 1))
    fi
  done < <(discover)

  echo
  if [ "$dry_run" -eq 1 ]; then
    echo "$planned repo(s) would be upgraded, $excluded excluded."
    return 0
  fi

  echo "$((ok + behind + failed)) repo(s) processed: $ok ok, $behind still behind, $failed failed;" \
       "$dirty skipped (dirty tree), $excluded excluded. Proposals: $PROPOSALS_DIR/"

  # The sweep is the moment law reaches other people's repositories. Anything
  # short of "at v$CURRENT_VERSION" has to reach the exit status: on
  # 2026-08-23 three consecutive failures were printed and the tool exited 0.
  # An exclusion is the operator's own decision and is not a miss.
  local missed=$((failed + behind + dirty))
  if [ "$missed" -gt 0 ]; then
    echo "$missed repo(s) did not reach v$CURRENT_VERSION" >&2
    return 1
  fi
  return 0
}

case "${1:-}" in
  status)  cmd_status ;;
  upgrade) shift; cmd_upgrade "$@" ;;
  *) usage ;;
esac
