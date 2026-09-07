#!/usr/bin/env bash
# BL-082 (R-8203, C-12): publish the deterministic arm as a NativeAOT binary.
#
# NativeAOT does not cross-compile between operating systems - the toolchain says so in as
# many words ("Cross-OS native compilation is not supported"), so a loop over the edition's
# four RIDs cannot run on one machine. This script publishes what THIS host can publish and
# refuses the rest by name, before reaching the SDK: the other three come from the release
# matrix in .github/workflows/dotnet.yml, which is the only place they can come from
# (operator ruling 2026-09-04).
#
# Usage: tools/publish-legislator.sh [rid ...]   (default: this host's RID)
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
released=(linux-x64 win-x64 osx-x64 osx-arm64)

case "$(uname -s)" in
  Linux)  host_os=linux ;;
  Darwin) host_os=osx ;;
  # Git Bash / MSYS on a Windows runner: one publish path for all four RIDs beats a second
  # one in the workflow that could drift from this script without anyone noticing.
  MINGW*|MSYS*|CYGWIN*) host_os=win ;;
  *)      printf 'publish-legislator: unsupported host %s\n' "$(uname -s)" >&2; exit 2 ;;
esac
case "$(uname -m)" in
  x86_64|amd64)  host_arch=x64 ;;
  arm64|aarch64) host_arch=arm64 ;;
  *)             printf 'publish-legislator: unsupported architecture %s\n' "$(uname -m)" >&2; exit 2 ;;
esac
host_rid="$host_os-$host_arch"

rids=("$@")
[ ${#rids[@]} -eq 0 ] && rids=("$host_rid")

# Refuse everything unbuildable BEFORE any work: a wrong RID costs a second, not a restore.
for rid in "${rids[@]}"; do
  found=no
  for known in "${released[@]}"; do [ "$rid" = "$known" ] && found=yes; done
  if [ "$found" = no ]; then
    printf 'publish-legislator: %s is not a RID this edition releases (%s)\n' "$rid" "${released[*]}" >&2
    exit 2
  fi
  if [ "${rid%-*}" != "$host_os" ]; then
    printf 'publish-legislator: %s cannot be built on a %s host - cross-OS native compilation is not supported by the AOT toolchain; the release matrix builds it\n' "$rid" "$host_os" >&2
    exit 2
  fi
done

sums="$repo/artifacts/SHA256SUMS"
mkdir -p "$repo/artifacts"
touch "$sums"

for rid in "${rids[@]}"; do
  out="$repo/artifacts/$rid"
  dotnet publish "$repo/src/Legislator.Cli" -c Release -r "$rid" -o "$out" --nologo
  exe="legislator"
  [ "${rid%-*}" = "win" ] && exe="legislator.exe"
  # One line per RID, rewritten in place: the digest recorded at tag time is what
  # `legislator audit` later compares the running binary against (R-8214).
  digest="$(cd "$out" && sha256sum "$exe" | cut -d' ' -f1)"
  remaining="$(grep -v "  $rid/$exe\$" "$sums" || true)"
  { [ -n "$remaining" ] && printf '%s\n' "$remaining"; printf '%s  %s/%s\n' "$digest" "$rid" "$exe"; } > "$sums.new"
  mv "$sums.new" "$sums"
  printf 'published %s -> artifacts/%s/%s (%s)\n' "$rid" "$rid" "$exe" "$digest"
done
