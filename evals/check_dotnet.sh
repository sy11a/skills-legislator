#!/usr/bin/env bash
# BL-082: one entry point for the .NET substrate — build strict, test, AOT publish smoke.
#
# The suite is run by executing each test project's Microsoft.Testing.Platform
# binary directly, not through `dotnet test`. On SDK 10.0.106 with the
# xunit.v3 MTP-v2 adapter, `dotnet test` discovers nothing — "Zero tests ran",
# exit 5, for every project, with or without the dotnet.config runner opt-in,
# while the same assemblies run their full suite under `dotnet exec`. A gate
# that reports nothing is worse than a gate that fails, so the discovery layer
# is not in the path: the modules are named and each one's count is checked.
# (BL-082 T-13.9.)
set -euo pipefail
cd "$(dirname "$0")/.."

# read_figure <name>: the last `<name>: N` line of a Microsoft.Testing.Platform summary
# on standard input, or nothing.
#
# Colour and line endings are the terminal's, not the summary's: a runner's terminal wraps
# the line in SGR sequences and a Windows one ends it in CR, and a reader anchored to the
# bare line reads neither. Both are removed before the line is read. The expressions are
# the portable ones - BSD sed on a macOS runner has no `\+` and no `\x1b`.
read_figure() {
  local esc; esc="$(printf '\033')"
  tr -d '\r' | sed "s/${esc}\[[0-9;]*[A-Za-z]//g" | sed -n "s/^ *$1: \([0-9][0-9]*\) *\$/\1/p" | tail -1
}

# The reader is checked before it is believed (BL-370). A summary it cannot read is
# reported as "zero tests ran", so a reader that is wrong reddens a green suite - and did,
# on every runner, for every run of the release matrix from the day it was written.
self_test() {
  local esc cr bad=0 got
  esc="$(printf '\033')"; cr="$(printf '\r')"
  expect() { # <label> <want> <summary text>
    got="$(printf '%s\n' "$3" | read_figure total)"
    if [ "$got" = "$2" ]; then echo "ok    reader: $1"; else echo "FAIL  reader: $1 - want '$2', read '$got'"; bad=1; fi
  }
  expect "a plain summary"                          36 "  total: 36"
  expect "a coloured summary (a runner's terminal)" 36 "${esc}[m  total: 36"
  expect "colour on both sides"                     36 "${esc}[32m  total: 36${esc}[m"
  expect "a CRLF summary (a Windows runner)"        36 "  total: 36${cr}"
  expect "the last count wins"                      7  "  total: 36
  total: 7"
  expect "no count is read as no count"             "" "Test run summary: Passed!"
  expect "a count inside prose is not a count"      "" "the total: 36 of them"
  return $bad
}
if [ "${1:-}" = "--self-test" ]; then self_test; exit $?; fi
self_test >/dev/null || { self_test || true; echo "FAIL  the summary reader failed its own controls - nothing it reports can be believed"; exit 1; }

dotnet build src/Legislator.slnx -warnaserror --nologo

total=0
failed_projects=()
for proj in tests/*.Tests; do
  [ -d "$proj" ] || continue
  name="$(basename "$proj")"
  dll="$proj/bin/Debug/net10.0/$name.dll"
  [ -f "$dll" ] || { echo "FAIL  $name: no test module at $dll — it did not build"; exit 1; }

  out="$(dotnet exec "$dll" 2>&1)" || true
  fails="$(printf '%s\n' "$out" | read_figure failed)"
  # A red module prints everything it said: a count of failures with no names is a red
  # nobody can act on, and on a runner the log is the only thing that survives the job.
  if [ "${fails:-0}" -eq 0 ]; then echo "$out" | tail -6; else echo "$out"; fi
  count="$(printf '%s\n' "$out" | read_figure total)"

  # A run that names no count, or names zero, is an instrument fault — never a
  # pass. This is the whole reason the discovery layer was taken out.
  if [ -z "$count" ] || [ "$count" -eq 0 ]; then
    [ "${fails:-0}" -eq 0 ] && echo "$out"
    echo "FAIL  $name: zero tests ran — the module reported no count; the runner is broken, not the suite"
    exit 1
  fi
  total=$((total + count))
  [ "${fails:-0}" -eq 0 ] || failed_projects+=("$name ($fails failed)")
done

echo
if [ ${#failed_projects[@]} -gt 0 ]; then
  echo "FAIL  ${#failed_projects[@]} project(s) red: ${failed_projects[*]}"
  exit 1
fi
echo "all $total .NET tests passed"

# The AOT smoke publishes THIS host's RID, never a named one. Since ADR 0013 the matrix has a
# linux-x64 job alone, so this line is the ONLY thing that ever builds the arm on a macOS or
# Windows machine - which is why `released=(...)` is a release statement and not a build gate
# (BL-408: making it one refused the host's own RID here, after a green test run).
tools/publish-legislator.sh
