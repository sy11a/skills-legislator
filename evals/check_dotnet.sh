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

dotnet build src/Legislator.slnx -warnaserror --nologo

total=0
failed_projects=()
for proj in tests/*.Tests; do
  [ -d "$proj" ] || continue
  name="$(basename "$proj")"
  dll="$proj/bin/Debug/net10.0/$name.dll"
  [ -f "$dll" ] || { echo "FAIL  $name: no test module at $dll — it did not build"; exit 1; }

  out="$(dotnet exec "$dll" 2>&1)" || true
  echo "$out" | tail -6
  count="$(printf '%s\n' "$out" | sed -n 's/^ *total: \([0-9]\+\)$/\1/p' | tail -1)"
  fails="$(printf '%s\n' "$out" | sed -n 's/^ *failed: \([0-9]\+\)$/\1/p' | tail -1)"

  # A run that names no count, or names zero, is an instrument fault — never a
  # pass. This is the whole reason the discovery layer was taken out.
  if [ -z "$count" ] || [ "$count" -eq 0 ]; then
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

dotnet publish src/Legislator.Cli -c Release -r linux-x64 -o artifacts/linux-x64 --nologo
