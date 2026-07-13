#!/usr/bin/env bash
# link-skills.sh — curated skill linker for ~/.claude/skills
#
# Links the KEEP list below from a source skill dump (default:
# ~/.agents/skills) into ~/.claude/skills as symlinks, so one `git pull` /
# re-install of the source updates every linked skill. Pairs with
# legislator audit check 14 (skill-bindings).
#
# Usage:
#   tools/link-skills.sh              # link anything missing (idempotent)
#   tools/link-skills.sh --check     # print drift, change nothing, exit 1 on drift
#   tools/link-skills.sh --prune     # also remove ~/.claude/skills symlinks NOT
#                                    # on the keep list that point into SRC
#                                    # (never touches real directories or
#                                    # links into other sources)
#   SRC=/path/to/pack tools/link-skills.sh   # alternate source
set -euo pipefail

SRC="${SRC:-$HOME/.agents/skills}"
DST="$HOME/.claude/skills"

# Curated keep list (BL-022 sweep verdicts; see
# docs/superpowers/specs/2026-07-12-skill-governance-design.md):
KEEP=(
  grilling grill-me grill-with-docs
  tdd diagnosing-bugs
  design-an-interface codebase-design improve-codebase-architecture
  prototype handoff claude-handoff research resolving-merge-conflicts
  domain-modeling ubiquitous-language
  qa to-tickets triage request-refactor-plan wayfinder
  git-guardrails-claude-code
  code-review
)

MODE="link"
case "${1:-}" in
  --check) MODE="check" ;;
  --prune) MODE="prune" ;;
  "") ;;
  *) echo "usage: $0 [--check|--prune]" >&2; exit 2 ;;
esac

drift=0

for name in "${KEEP[@]}"; do
  src="$SRC/$name" dst="$DST/$name"
  if [[ ! -d "$src" ]]; then
    echo "MISSING SOURCE  $name  (not in $SRC — skill not installed there)"
    drift=1
    continue
  fi
  if [[ -L "$dst" ]]; then
    if [[ "$(readlink -f "$dst")" == "$(readlink -f "$src")" ]]; then
      continue  # correctly linked
    fi
    echo "WRONG TARGET    $name  ($(readlink "$dst"))"
    drift=1
    [[ "$MODE" != "check" ]] && { ln -sfn "$src" "$dst"; echo "  relinked -> $src"; }
  elif [[ -e "$dst" ]]; then
    echo "REAL DIR        $name  (exists as a non-link at $dst — left untouched)"
    drift=1
  else
    echo "NOT LINKED      $name"
    drift=1
    [[ "$MODE" != "check" ]] && { ln -s "$src" "$dst"; echo "  linked -> $src"; }
  fi
done

if [[ "$MODE" == "prune" ]]; then
  for dst in "$DST"/*; do
    [[ -L "$dst" ]] || continue
    name="$(basename "$dst")"
    case " ${KEEP[*]} " in *" $name "*) continue ;; esac
    # only prune links that point into SRC (never other sources like legislator)
    if [[ "$(readlink -f "$dst")" == "$(readlink -f "$SRC")"/* ]]; then
      rm "$dst"
      echo "PRUNED          $name  (off-list link into $SRC)"
    fi
  done
fi

if [[ "$MODE" == "check" ]]; then
  [[ $drift -eq 0 ]] && echo "clean: all keep-list skills correctly linked"
  exit $drift
fi
echo "done"
