#!/usr/bin/env bash
# BL-082 (C-12): put the published binary on this machine's PATH.
#
# The operator-side arm, POSIX only - the Windows install is a Copy-Item documented in the
# README, per BL-068's declaration rule. Copies rather than links: a symlink into a build
# output directory is a binary that changes under the hooks whenever someone rebuilds.
#
# Usage: tools/install-legislator.sh [--from artifacts/<rid>]
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
from=""
while [ $# -gt 0 ]; do
  case "$1" in
    --from) from="${2:-}"; shift 2 ;;
    *)      printf 'install-legislator: unknown argument %s (usage: install-legislator.sh [--from artifacts/<rid>])\n' "$1" >&2; exit 2 ;;
  esac
done

if [ -z "$from" ]; then
  case "$(uname -s)" in
    Linux)  host_os=linux ;;
    Darwin) host_os=osx ;;
    *)      printf 'install-legislator: unsupported host %s\n' "$(uname -s)" >&2; exit 2 ;;
  esac
  case "$(uname -m)" in
    x86_64|amd64)  host_arch=x64 ;;
    arm64|aarch64) host_arch=arm64 ;;
    *)             printf 'install-legislator: unsupported architecture %s\n' "$(uname -m)" >&2; exit 2 ;;
  esac
  from="artifacts/$host_os-$host_arch"
fi

source_binary="$repo/$from/legislator"
if [ ! -x "$source_binary" ]; then
  printf 'install-legislator: no executable at %s - run tools/publish-legislator.sh first\n' "$from" >&2
  exit 2
fi

target_dir="$HOME/.local/bin"
mkdir -p "$target_dir"
cp "$source_binary" "$target_dir/legislator"
chmod +x "$target_dir/legislator"
printf 'installed %s -> %s/legislator\n' "$from" "$target_dir"
case ":$PATH:" in
  *":$target_dir:"*) ;;
  *) printf 'note: %s is not on PATH - the hooks resolve `legislator` through it\n' "$target_dir" >&2 ;;
esac
