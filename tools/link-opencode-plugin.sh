#!/usr/bin/env bash
# link-opencode-plugin.sh — deploy the legislator opencode enforcement plugin.
#
# Symlinks plugin/opencode/legislator-guard.ts into ~/.config/opencode/plugins/
# (global plugin dir, auto-loaded by opencode at startup). The plugin is a
# no-op outside legislated repos, so loading it globally is safe. Idempotent.
#
# Pairs with the Claude Code hook plugin at plugin/hooks/ (deployed by
# Claude's own plugin loader); this script is the opencode-side equivalent.
#
# Usage:
#   tools/link-opencode-plugin.sh            # link (idempotent)
#   tools/link-opencode-plugin.sh --check    # print drift, change nothing, exit 1 on drift
#   tools/link-opencode-plugin.sh --unlink   # remove the symlink
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="$HERE/../plugin/opencode/legislator-guard.ts"
DST_DIR="$HOME/.config/opencode/plugins"
DST="$DST_DIR/legislator-guard.ts"

MODE="link"
case "${1:-}" in
  --check)  MODE="check" ;;
  --unlink) MODE="unlink" ;;
  "")       ;;
  *) echo "usage: $0 [--check|--unlink]" >&2; exit 2 ;;
esac

mkdir -p "$DST_DIR"

if [[ ! -f "$SRC" ]]; then
  echo "MISSING SOURCE  $SRC" >&2
  exit 1
fi

drift=0
if [[ -L "$DST" ]]; then
  if [[ "$(readlink -f "$DST")" == "$(readlink -f "$SRC")" ]]; then
    if [[ "$MODE" == "unlink" ]]; then
      rm "$DST"; echo "UNLINKED  $DST"
    else
      echo "clean: $DST -> $SRC"
    fi
  else
    echo "WRONG TARGET  $DST ($(readlink "$DST"))"
    drift=1
    [[ "$MODE" == "link" ]] && { ln -sfn "$SRC" "$DST"; echo "  relinked -> $SRC"; }
  fi
elif [[ -e "$DST" ]]; then
  echo "REAL FILE  $DST (exists as a non-link — left untouched)" >&2
  exit 1
else
  if [[ "$MODE" == "unlink" ]]; then
    echo "nothing to unlink ($DST absent)"
  else
    echo "NOT LINKED  legislator-guard.ts"
    drift=1
    [[ "$MODE" == "link" ]] && { ln -s "$SRC" "$DST"; echo "  linked -> $SRC"; }
  fi
fi

[[ "$MODE" == "check" ]] && exit $drift
[[ "$MODE" == "link" ]] && echo "done"
true
