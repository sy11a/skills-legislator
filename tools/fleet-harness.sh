#!/usr/bin/env bash
# BL-061/BL-056 harness: fake fleet + stub runner, no agent, no tokens.
# Usage: run_harness.sh <fleet.sh-path>
set -u
FLEET_SH="${1:?usage: run_harness.sh <fleet.sh>}"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/fleet-harness-XXXXXX")"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$WORK/roots" "$WORK/bin" "$WORK/proposals"

REPO_ROOT="$(cd "$(dirname "$FLEET_SH")/.." && pwd)"
CUR="$(cat "$REPO_ROOT/skill/VERSION")"
PREV=$((CUR - 1))

mk_repo() { # <name> <committed-version> [worktree-version]
  local name="$1" cv="$2" wv="${3:-$2}" d="$WORK/roots/$1"
  mkdir -p "$d/docs/ai"
  printf '{\n  "legislatorVersion": %s\n}\n' "$cv" > "$d/docs/ai/manifest.json"
  git -C "$d" init -q
  git -C "$d" -c user.email=h@l -c user.name=h add -A
  git -C "$d" -c user.email=h@l -c user.name=h commit -qm seed
  if [ "$wv" != "$cv" ]; then
    printf '{\n  "legislatorVersion": %s\n}\n' "$wv" > "$d/docs/ai/manifest.json"
  fi
}

# The stub runner: "claude" that optionally delivers, then exits as told.
# Per-repo behavior via marker files the stub reads from the repo it runs in.
cat > "$WORK/bin/claude" <<'STUB'
#!/usr/bin/env bash
# reads cwd/.stub-behavior: "deliver <version> exit <code>" or "noop exit <code>"
cat > /dev/null   # swallow the prompt on stdin
b="$(cat .stub-behavior 2>/dev/null || echo "noop exit 0")"
set -- $b
if [ "$1" = deliver ]; then
  printf '{\n  "legislatorVersion": %s\n}\n' "$2" > docs/ai/manifest.json
  exit "$4"
fi
exit "$3"
STUB
chmod +x "$WORK/bin/claude"

# Four upgrade scenarios (all committed at PREV so the sweep targets them):
mk_repo delivered-clean  "$PREV"; echo "deliver $CUR exit 0" > "$WORK/roots/delivered-clean/.stub-behavior"
mk_repo delivered-crash  "$PREV"; echo "deliver $CUR exit 1" > "$WORK/roots/delivered-crash/.stub-behavior"
mk_repo behind-clean     "$PREV"; echo "noop exit 0"          > "$WORK/roots/behind-clean/.stub-behavior"
mk_repo behind-crash     "$PREV"; echo "noop exit 1"          > "$WORK/roots/behind-crash/.stub-behavior"
# .stub-behavior would make the tree dirty — commit it as part of the seed
for r in delivered-clean delivered-crash behind-clean behind-crash; do
  git -C "$WORK/roots/$r" -c user.email=h@l -c user.name=h add -A
  git -C "$WORK/roots/$r" -c user.email=h@l -c user.name=h commit -qm stub
done

# Status scenarios:
mk_repo status-ok        "$CUR"
mk_repo status-pending   "$PREV" "$CUR"     # committed PREV, worktree CUR — BL-056's case

echo "=== upgrade (stub runner) ==="
PATH="$WORK/bin:$PATH" SCAN_ROOTS="$WORK/roots" PROPOSALS_DIR="$WORK/proposals" \
  RUNNER=claude bash "$FLEET_SH" upgrade --exclude status-ok --exclude status-pending
echo "upgrade exit=$?"
echo
echo "=== status ==="
SCAN_ROOTS="$WORK/roots" bash "$FLEET_SH" status
echo "status exit=$?"
