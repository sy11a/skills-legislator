#!/usr/bin/env bash
# evals-bg.sh — background staged eval runner (BL-037, productized from
# the v17 benchmark's live iteration).
#
# Stages, in order, each only on the previous green:
#   1. gates    — workspace precondition (every scenario dir this invocation
#                 will use holds a repo/ at the `eval-base` tag), writable
#                 headroom (BL-059: a quota-bounded tmpfs fails as fake
#                 stalls, not as a full disk), then
#                 evals/check_static.py and evals/check_engine.py (seconds)
#   2. smoke    — the upgrade scenario (most change-sensitive surface)
#   3. corpus   — every scenario in evals.json except idempotency
#   4. idem     — commit each run-1 result, second pass, zero-diff grade
#
# Per scenario: full fixture reset on EVERY invocation, confirmation
# waiver (headless — nobody answers), stall detection, resume ladder,
# auto-grade into <ws>/<scenario>/outputs/, queue.json + status.md as the
# machine-readable contract, notify-send on scenario/run boundaries.
#
# Runner profiles — the stages, contracts and grading are engine-agnostic;
# only how a scenario agent is spawned differs:
#   opencode (default) — `opencode run`, prose straight into run.log
#   claude             — `claude -p --safe-mode`, stream-json rendered into
#                        run.log by evals/streamfmt.py, raw events kept in
#                        run.jsonl (the stall oracle)
# Every run records its profile in run.json, so the dashboard and the grade
# history can tell two runs apart. Pass rates across profiles are NOT
# comparable — a different engine is a bigger confound than a different model.
#
# Usage:
#   tools/evals-bg.sh <workspace> [--skip-smoke] [--only SCEN [SCEN..]]
#                                 [--runner opencode|claude] [--model NAME]
#   RUNNER=claude MODEL=haiku  tools/evals-bg.sh <ws>
#   NO_BROWSER=1 ...   # don't launch the dashboard/browser (service runs)
# Env: RUNNER (default opencode), MODEL (per-profile default below),
# KBO_EVALS_NO_BROWSER same as NO_BROWSER.
set -u
WS="${1:?usage: evals-bg.sh <workspace> [--skip-smoke|--only SCEN..]}"
shift || true

REPO="$(cd "$(dirname "$0")/.." && pwd)"
SKILL="$REPO/skill"
RUNNER="${RUNNER:-opencode}"
MODEL="${MODEL:-}"           # profile default applied after flag parsing
STALL_SECS=180
MAX_ATTEMPTS=3
MAX_RESUMES=4

SKIP_SMOKE=0
ONLY=()
IDEM=()
while [ $# -gt 0 ]; do
  case "$1" in
    --skip-smoke) SKIP_SMOKE=1 ;;
    --runner) shift; RUNNER="${1:?--runner needs a profile name}" ;;
    --model) shift; MODEL="${1:?--model needs a model name}" ;;
    --only) shift; while [ $# -gt 0 ] && [[ "$1" != --* ]]; do ONLY+=("$1"); shift; done; continue ;;
    --idem) shift; while [ $# -gt 0 ] && [[ "$1" != --* ]]; do IDEM+=("$1"); shift; done; continue ;;
    *) echo "unknown flag: $1" >&2; exit 2 ;;
  esac
  shift
done

# ---- runner profile ------------------------------------------------------
case "$RUNNER" in
  opencode)
    MODEL="${MODEL:-zai-coding-plan/glm-5-turbo}"
    # scoped to THIS workspace (--dir is always $WS/<scenario>): an
    # unscoped "opencode run" pattern sweeps unrelated agents off the whole
    # machine — there are usually some. The concat keeps this script's own
    # cmdline from matching.
    PMATCH="opencode"" run --dir $WS"
    ACTIVITY="run.log"       # opencode streams prose straight into the log
    ;;
  claude)
    MODEL="${MODEL:-haiku}"
    # scoped to THIS workspace on purpose: a bare "claude -p" pattern would
    # match the operator's own sessions and kill them on a stall sweep
    PMATCH="claude"" -p .*$WS"
    ACTIVITY="run.jsonl"     # the rendered log grows in bursts; raw does not
    ;;
  *) echo "unknown runner profile: $RUNNER (expected: opencode | claude)" >&2; exit 2 ;;
esac

mkdir -p "$WS"
TL="$WS/orchestrate.log"

# --- run provenance (A1/A3): every result knows its run -------------------
RUN_ID="$(date +%Y%m%d-%H%M)"
NEW_RUN=1
if [ ${#ONLY[@]} -gt 0 ] || [ ${#IDEM[@]} -gt 0 ]; then
  # targeted retest: the SAME run keeps its id; other scenarios untouched
  NEW_RUN=0
  [ -s "$WS/run.json" ] || RUN_ID="orphan-$(date +%Y%m%d-%H%M)"
fi
python3 - "$WS" "$RUN_ID" "$MODEL" "$REPO" "$RUNNER" <<'PYEOF'
import json, subprocess, sys, pathlib
ws, run_id, model, repo, runner = sys.argv[1:6]
f = pathlib.Path(ws) / "run.json"
prev = {}
try:
    prev = json.loads(f.read_text())
except Exception:
    pass
head = subprocess.run(["git", "-C", repo, "rev-parse", "--short", "HEAD"],
                      capture_output=True, text=True).stdout.strip()
entry = {"run_id": run_id, "started": run_id, "runner": runner,
         "model": model, "law_commit": head}
runs = prev.get("runs", [])
if run_id not in [r["run_id"] for r in runs]:
    runs.append(entry)
f.write_text(json.dumps({"current": entry, "runs": runs}, indent=1) + "\n")
PYEOF

if [ $NEW_RUN -eq 1 ]; then
  # a fresh full run: every scenario starts as not-started; grades from
  # prior runs stay on disk as history but stop posing as current
  echo '{"order": [], "statuses": {}}' > "$WS/queue.json"
  rm -f "$WS"/*/outputs/grading.json "$WS"/*/outputs/grading_idempotency.json \
        "$WS"/*/outputs/grade.txt 2>/dev/null
  : > "$TL"
else
  # targeted retest: reset only the targeted scenario's queue row
  python3 - "$WS" "${ONLY[@]}" <<'PYEOF'
import json, sys, pathlib
ws = pathlib.Path(sys.argv[1])
f = ws / "queue.json"
try: q = json.loads(f.read_text())
except Exception: q = {"order": [], "statuses": {}}
# queued, NOT running: run_scenario flips each row to running when it
# actually starts. Marking the whole batch running up front made every
# waiting scenario look live, so the dashboard measured its (stale) log age
# and cried stall — while showing the PREVIOUS run's output underneath
# (found 2026-08-22).
for sc in sys.argv[2:]:
    q["statuses"][sc] = "queued"
    if sc not in q["order"]: q["order"].append(sc)
f.write_text(json.dumps(q, indent=1) + "\n")
for sc in sys.argv[2:]:
    outputs = ws / sc / "outputs"
    for junk in ("grading.json", "grade.txt"):
        (outputs / junk).unlink(missing_ok=True)
    # a queued scenario must show nothing, never the last run's errors
    for log in ("run.log", "run.jsonl"):
        if (outputs / log).exists(): (outputs / log).write_text("")
PYEOF
fi

# ---- prompts: evals.json is the single source (no runner-side dups) ----
prompt_of() { python3 -c "
import json,sys
name=sys.argv[1]
d=json.load(open('$REPO/evals/evals.json'))
e=next((e for e in d['evals'] if e['name']==name), None)
print(e['prompt'] if e else '')" "$1"; }

REPORT_OF() { # <scenario-dir-name> -> relative expected deliverable
  case "$1" in
    legacy-migration|legacy-migration-agents-first) echo "outputs/migration-report.md";;
    upgrade|upgrade-drop-stack) echo "outputs/upgrade-report.md";;
    rotted-layer|audit-engine-absent) echo "outputs/audit-report.md";;
    restructure) echo "outputs/restructure-report.md";;
    *) echo "";;
  esac
}
SC_OF() { # dir-name -> evals.json scenario name
  case "$1" in rotted-layer) echo audit;; *) echo "$1";; esac
}
DIR_OF() { # evals.json scenario name -> dir-name
  case "$1" in audit) echo rotted-layer;; *) echo "$1";; esac
}

status() { # freeform line -> status.md tail + timeline
  echo "[$(date +%H:%M:%S)] $*" >> "$WS/status.md"
}
tl() { # <scenario> <event> -> dashboard-parseable timeline line
  echo "[$1] $2 $(date +%H:%M:%S)" >> "$TL"
}
notify() { command -v notify-send >/dev/null 2>&1 && notify-send -a "legislator evals" "$*" || true; }

upd_queue() { # <dir> <status>
  python3 - "$WS" "$1" "$2" <<'PYEOF'
import json, sys, pathlib
ws, sc, st = sys.argv[1], sys.argv[2], sys.argv[3]
f = pathlib.Path(ws) / "queue.json"
try: q = json.loads(f.read_text())
except Exception: q = {"order": [], "statuses": {}}
q["statuses"][sc] = st
if sc not in q["order"]: q["order"].append(sc)
f.write_text(json.dumps(q, indent=1) + "\n")
PYEOF
}

runner_kill() { # <pid> — the stalled agent's own process group first, then a
                # profile-scoped sweep for anything it detached
  [ -n "${1:-}" ] && kill -TERM -"$1" 2>/dev/null
  pkill -f "$PMATCH" 2>/dev/null
  return 0
}

reset_repo() { # restore the fixture EXACTLY as materialized — the commit
               # graph included. The idempotency stage commits "run 1" into
               # the fixture on purpose; a later --only rerun used to inherit
               # that commit and fail nothing_committed on the next agent's
               # behalf (found 2026-08-22). "Full reset on every invocation"
               # has to mean the whole repo, not just the working tree. The
               # anchor is setup_workspace's `eval-base` tag, never the root
               # commit — two fixtures carry a planted second commit.
  if git -C "$1" rev-parse -q --verify eval-base >/dev/null 2>&1; then
    git -C "$1" reset --hard --quiet eval-base >/dev/null 2>&1 || true
  else
    git -C "$1" reset --quiet >/dev/null 2>&1 || true
    git -C "$1" checkout -- . >/dev/null 2>&1 || true
  fi
  git -C "$1" clean -fdq >/dev/null 2>&1 || true
}

expected_ok() {
  local sc="$1" ws="$2" report
  report="$(REPORT_OF "$sc")"
  if [ -n "$report" ]; then [ -s "$ws/$sc/$report" ]
  elif [ "$sc" = case-practice ]; then ls "$ws/$sc/repo/docs/cases/" 2>/dev/null | grep -q "^BL-"
  else [ -f "$ws/$sc/repo/docs/ai/manifest.json" ] && [ -f "$ws/$sc/repo/docs/cases/README.md" ]
  fi
}

msg_block() { # <dir>
  # Ground rules state HARNESS constraints only — never law. A previous
  # version restated SKILL.md's entry-document clause ("AGENTS.md is
  # project-owned, never edit it directly"); the law's own phrasing was
  # overstated, the prompt inherited the overstatement, and the harness ended
  # up forbidding exactly what it was testing: four asserts across
  # legacy-migration-agents-first and restructure went red for obeying the
  # prompt. Two model families had hidden it by ignoring the ground rule.
  # Same discipline as the trigger rules in evals/README.md: the prompt must
  # not answer questions the skill is supposed to resolve, and must not
  # repeat rules the skill already carries.
  local sc="$1" p report out
  p="$(prompt_of "$(SC_OF "$sc")")"
  out="$WS/$sc/outputs"
  case "$sc" in
    legacy-migration|legacy-migration-agents-first|upgrade|upgrade-drop-stack) report="Write your full Step 7 report (all sections, including Health and any Constitution candidates) to $out/$(basename "$(REPORT_OF "$sc")") — overwrite if it exists.";;
    case-practice) report="";;
    rotted-layer|audit-engine-absent) report="Save your full audit report to $out/audit-report.md — outside the target repo (which you must not touch: zero writes).";;
    restructure) report="Write your final restructure report to $out/restructure-report.md — overwrite if it exists.";;
    *) report="";;
  esac
  cat <<MSG
Read $SKILL/SKILL.md and follow it EXACTLY, including referenced files under $SKILL/. Work in $WS/$sc/repo. $p

Ground rules: NEVER run git commit — leave all changes uncommitted for review. Byte-for-byte copies happen via Bash cp exactly as the skill instructs. The skill's own law governs what each mode may write — do not infer extra prohibitions from these ground rules. The scenario folder $WS/$sc is the working directory (the repo and $out are writable siblings).

CONFIRMATION WAIVER (scripted run — nobody will answer): every confirmation the skill asks for is pre-approved. Answer with the skill's own default plus what the repo evidences; the skill knows what to deploy. Never end your turn on a question. $report
MSG
}

spawn() { # <dir> <fresh|resume> — self-contained: no /tmp helper
  local sc="$1" log="$WS/$1/outputs/run.log" msg
  mkdir -p "$WS/$sc/outputs"
  : >> "$log"
  [ "$2" = fresh ] && msg_block "$sc" > "$WS/$sc/outputs/prompt.txt"
  if [ "$2" = resume ]; then
    msg="Continue exactly where you left off (the previous stream dropped). Re-check current file state on disk before acting — you may have already completed some steps. CONFIRMATION WAIVER still applies: never end your turn on a question."
  else
    msg="$(msg_block "$sc")"
  fi
  case "$RUNNER" in
    opencode)
      if [ "$2" = resume ]; then
        setsid opencode run --dir "$WS/$sc" -m "$MODEL" --continue "$msg" \
          </dev/null >> "$log" 2>&1 &
      else
        setsid opencode run --dir "$WS/$sc" -m "$MODEL" "$msg" \
          </dev/null >> "$log" 2>&1 &
      fi
      ;;
    claude)
      # --safe-mode is what makes this a fair harness: no user CLAUDE.md, no
      # auto-memory, no hooks, no plugins, no MCP, no installed skills — the
      # agent gets the prompt and the skill path, nothing else. (--bare would
      # go further but demands an API key and never reads OAuth.)
      local flags=(-p --model "$MODEL" --safe-mode
                   --permission-mode bypassPermissions
                   --add-dir "$SKILL"   # the skill lives outside the scenario cwd
                   --output-format stream-json --verbose
                   --include-partial-messages)
      [ "$2" = resume ] && flags+=(-c)
      setsid env SC_DIR="$WS/$sc" RAW="$WS/$sc/outputs/run.jsonl" \
                 FMT="$REPO/evals/streamfmt.py" \
        bash -c 'cd "$SC_DIR" && claude "$@" | python3 "$FMT" "$RAW"' \
             _ "${flags[@]}" "$msg" \
        </dev/null >> "$log" 2>&1 &
      ;;
  esac
  echo $!
}

wait_or_stall() { # <pid> <dir>
  local pid=$1 sc="$2" last=-1 last_dirty=-1 last_change size dirty now
  last_change=$(date +%s)
  while kill -0 "$pid" 2>/dev/null; do
    sleep 15
    size=$(stat -c%s "$WS/$sc/outputs/$ACTIVITY" 2>/dev/null || echo 0)
    dirty=$(git -C "$WS/$sc/repo" status --porcelain 2>/dev/null | wc -l)
    now=$(date +%s)
    if [ "$size" != "$last" ] || [ "$dirty" != "$last_dirty" ]; then
      last=$size; last_dirty=$dirty; last_change=$now
    elif [ $((now - last_change)) -ge $STALL_SECS ]; then
      return 1
    fi
  done
  return 0
}

auto_grade() { # <dir>
  local sc="$1"
  ( cd "$REPO" && python3 evals/grade.py "$WS" "$(SC_OF "$sc")" \
      > "$WS/$sc/outputs/grade.txt" 2>&1 ) || true
}

run_scenario() { # <dir> — attempts + resume ladder; returns 0 on green run
  local sc="$1"
  upd_queue "$sc" running
  rm -f "$WS/$sc/outputs/grading.json" "$WS/$sc/outputs/grade.txt" \
        "$WS/$sc/outputs/$(basename "$(REPORT_OF "$sc")")" 2>/dev/null
  reset_repo "$WS/$sc/repo"
  mkdir -p "$WS/$sc/outputs"   # a virgin workspace has no outputs/ yet
  : > "$WS/$sc/outputs/run.log"; : > "$WS/$sc/outputs/run.jsonl"

  for attempt in $(seq 1 $MAX_ATTEMPTS); do
    tl "$sc" "attempt $attempt (fresh) start"
    [ "$attempt" -gt 1 ] && reset_repo "$WS/$sc/repo"
    local pid; pid=$(spawn "$sc" fresh)
    if wait_or_stall "$pid" "$sc"; then
      if expected_ok "$sc" "$WS"; then tl "$sc" "DONE attempt $attempt"; return 0; fi
      tl "$sc" "attempt $attempt exited without expected output"
      continue
    fi
    tl "$sc" "attempt $attempt stalled — resume ladder"
    runner_kill "$pid"; sleep 2
    for r in $(seq 1 $MAX_RESUMES); do
      tl "$sc" "resume $r start"
      pid=$(spawn "$sc" resume)
      if wait_or_stall "$pid" "$sc"; then
        if expected_ok "$sc" "$WS"; then tl "$sc" "DONE resume $r"; return 0; fi
        tl "$sc" "resume $r exited without expected output"; break
      fi
      tl "$sc" "resume $r stalled again"
      runner_kill "$pid"; sleep 2
    done
  done
  tl "$sc" "FAILED after $MAX_ATTEMPTS attempts"
  return 1
}

idem_scenario() { # <dir> — commit run 1, second pass, zero-diff grade.
                  # NOTE: never reset_repo here — the whole point is to run
                  # against run 1's committed result. That is also why
                  # --only cannot serve this: it resets the fixture to
                  # eval-base and would destroy exactly what is under test.
  local sc="$1" pid ok attempt
  # BL-063: run 2 overwrites the scenario's report, and the corpus report
  # is the mutation pass's substrate (POLICY §1c). Snapshot it first —
  # losing it cost restructure's 38 mutations a fresh agent run on
  # 2026-08-26. .corpus.md files are ignored by every reader (globs match
  # *-report.md); mutate.py restores them on validation.
  for rep in "$WS/$sc/outputs/"*-report.md; do
    [ -f "$rep" ] && cp "$rep" "${rep%.md}.corpus.md"
  done
  git -C "$WS/$sc/repo" add -A >/dev/null 2>&1
  git -C "$WS/$sc/repo" commit -q -m "run 1" >/dev/null 2>&1 || true
  : > "$WS/$sc/outputs/idem-run.log"
  tl "$sc" "idem second pass start"
  ok=""
  for attempt in 1 2 3; do
    pid=$(spawn "$sc" fresh)
    if wait_or_stall "$pid" "$sc"; then ok=1; break; fi
    runner_kill "$pid"; sleep 2
  done
  if [ -n "$ok" ]; then
    ( cd "$REPO" && python3 evals/grade.py "$WS" "idempotency:$sc" \
        > "$WS/$sc/outputs/idem-grade.txt" 2>&1 ) || true
    if grade_clean "$WS/$sc/outputs/grading_idempotency.json"; then
      tl "$sc" "idem ZERO DIFF"; notify "eval idem $sc: zero diff"
    else
      tl "$sc" "idem DIFF FOUND"; notify "eval idem $sc: DIFF"; exit 1
    fi
  else
    tl "$sc" "idem FAILED"; notify "eval idem $sc: FAILED"; exit 1
  fi
}

grade_clean() { # <grading.json> -> 0 when the grade is green, 1 otherwise
  # POLICY §1b: not measured is not passed. A scenario is green only when
  # nothing failed AND nothing went unmeasured — an assert whose artifact was
  # absent did not score, it failed to measure, and an exit code that cannot
  # tell the two apart is the same false green one level up (BL-058, BL-062).
  # One definition, four callers: the previous shape restated
  # `summary.failed == 0` at each site, which is how stage 3 came to disagree
  # with stage 2 about what green means.
  python3 -c "
import json,sys
s = json.load(open('$1'))['summary']
sys.exit(1 if (s.get('failed', 0) or s.get('unmeasured', 0)) else 0)" 2>/dev/null
}

finish_scenario() { # <dir> — grade + queue + notify (after a DONE run)
  local sc="$1"
  auto_grade "$sc"
  local g="$WS/$sc/outputs/grading.json" verdict="?"
  [ -s "$g" ] && verdict=$(python3 -c "
import json;d=json.load(open('$g'));s=d['summary']
m, u = s.get('measured', s['total']), s.get('unmeasured', 0)
tag = ' CLEAN' if not (s['failed'] or u) else (
      ' WITH FAILURES' if s['failed'] else '') + (f' WITH {u} UNMEASURED' if u else '')
print(f\"{m}/{s['total']} measured, {s['passed']} passed\" + tag)")
  tl "$sc" "graded: $verdict"
  local failed=0
  [ -s "$g" ] && grade_clean "$g" || failed=1
  if [ $failed -eq 0 ]; then upd_queue "$sc" done; notify "eval $sc: $verdict"
  else upd_queue "$sc" partial; notify "eval $sc: $verdict (w/ errors)"; fi
  # The GRADE is the verdict, and it has to reach the caller. Stage 2 checked
  # it and stage 3 did not, so a scenario that ran to completion and graded
  # 43/44 left the corpus "green" and the run exited 0 — measured on the v20
  # baseline of 2026-08-24. POLICY.md §1 makes 100% the release bar; the
  # instrument that decides releasability could not see a 99.5%.
  return $failed
}

# ---- workspace precondition (BL-050) ------------------------------------
# The runner never materialized the workspace and never checked that anyone
# else had — `evals/README.md` step 1 is a separate command. On 2026-08-23 a
# full corpus attempt ran for an hour against fixtures that did not exist:
# agents improvised in absent repositories, two scenarios had no repo/ at all,
# and two others still graded CLEAN. `eval-base` is the right witness because
# it is the same tag reset_repo trusts — this checks exactly what the run will
# later use, not a weaker proxy for it.
scenario_dirs() { # -> the dir names THIS invocation will touch, one per line
  if [ ${#IDEM[@]} -gt 0 ]; then
    printf '%s\n' "${IDEM[@]}"
  elif [ ${#ONLY[@]} -gt 0 ]; then
    printf '%s\n' "${ONLY[@]}"
  else
    # stages 2-4: every scenario in evals.json except idempotency, which is a
    # second pass over other scenarios and owns no directory of its own
    python3 -c "
import json
d=json.load(open('$REPO/evals/evals.json'))
print('\n'.join(e['name'] for e in d['evals'] if e['name'] != 'idempotency'))"
  fi | while read -r n; do [ -n "$n" ] && DIR_OF "$n"; done
}

require_workspace() {
  local sc line
  local -a bad=()
  while read -r sc; do
    [ -n "$sc" ] || continue
    if [ ! -d "$WS/$sc/repo" ]; then
      bad+=("$sc — no repo/ directory")
    elif ! git -C "$WS/$sc/repo" rev-parse -q --verify eval-base >/dev/null 2>&1; then
      bad+=("$sc — repo/ carries no eval-base tag")
    fi
  done < <(scenario_dirs)
  [ ${#bad[@]} -eq 0 ] && return 0
  status "WORKSPACE NOT MATERIALIZED — $WS"
  for line in "${bad[@]}"; do status "  $line"; done
  status "  fix: python3 evals/setup_workspace.py $WS"
  {
    printf 'WORKSPACE NOT MATERIALIZED — %s\n' "$WS"
    printf '  %s\n' "${bad[@]}"
    printf '  fix: python3 evals/setup_workspace.py %s\n' "$WS"
  } >&2
  notify "evals: workspace not materialized — nothing ran"
  return 1
}

# ============================= STAGES =============================

status "profile: runner=$RUNNER model=$MODEL (run $RUN_ID)"
status "=== stage 1: workspace precondition ==="
require_workspace || exit 1
status "workspace green"

# ---- dashboard + browser ---- (only once the run can actually proceed)
if [ -z "${NO_BROWSER:-}${KBO_EVALS_NO_BROWSER:-}" ]; then
  setsid nohup python3 "$REPO/evals/dashboard.py" "$WS" --interval 3 --open \
    > "$WS/dashboard.log" 2>&1 &
fi

status "=== stage 1: reclaim dotnet map files ==="
# BL-059 prevention: the leak the probe below only detects. The .NET host
# maps assembly images as /tmp/.<16hex>-<n>.so and deletes them on a clean
# exit; a killed process (the stall ladder's own job) abandons them, and
# they accumulate until some LATER run's writes start failing mid-corpus in
# a shape that reads as a stalled agent. Reclaim what is PROVABLY unowned —
# ours by owner, open in no process (fuser silent). Proof, not age: a file
# some process still maps is never touched, however old. If fuser is
# missing or a removal fails, fall through to the probe unchanged — the
# probe stays the authority on room; this only makes room.
reclaimed_n=0 reclaimed_kb=0
if command -v fuser >/dev/null 2>&1; then
  for f in /tmp/.[0-9a-f]*-*.so; do
    [ -e "$f" ] || continue
    [ -O "$f" ] || continue
    fuser -s "$f" 2>/dev/null && continue
    sz=$(du -k "$f" 2>/dev/null | cut -f1 || echo 0)
    rm -f "$f" 2>/dev/null || continue
    reclaimed_n=$((reclaimed_n + 1)); reclaimed_kb=$((reclaimed_kb + sz))
  done
  status "reclaimed $reclaimed_n unowned dotnet map file(s), $((reclaimed_kb / 1024)) MB"
else
  status "fuser unavailable — reclaim skipped, probe will judge headroom"
fi

status "=== stage 1: writable headroom ==="
# BL-059: /tmp here is a quota-bounded tmpfs, and the dotnet fixtures leak
# runtime .so images into it that nothing removes. When the quota runs out the
# failure does not look like a full disk: streamfmt.py dies mid-scenario,
# run.jsonl stops growing, and the stall oracle reads a perfectly healthy agent
# as stalled — three attempts and four resumes per scenario, producing nothing.
# Three v21 corpus runs were lost to it before anyone looked at `quota -s`.
# `df` is the wrong instrument (it reports the filesystem, not the quota), so
# this does not query anything: it WRITES, which is the thing that actually
# fails.
if ! dd if=/dev/zero of="$WS/.headroom-probe" bs=1M count=512 \
     >/dev/null 2>"$WS/.headroom-probe.err"; then
  rm -f "$WS/.headroom-probe"
  status "NO WRITABLE HEADROOM — could not allocate 512 MB under $WS"
  {
    printf 'NO WRITABLE HEADROOM — could not allocate 512 MB under %s\n' "$WS"
    printf '  %s\n' "$(tail -1 "$WS/.headroom-probe.err" 2>/dev/null)"
    printf '  a corpus run needs room for agent transcripts; check `quota -s`, not `df`\n'
    printf '  stale .NET runtime images are the usual culprit:\n'
    printf "    find /tmp -maxdepth 1 -name '.*.so' -mtime +0 -delete\n"
  } >&2
  rm -f "$WS/.headroom-probe.err"
  notify "evals: no writable headroom — nothing ran"
  exit 1
fi
rm -f "$WS/.headroom-probe" "$WS/.headroom-probe.err"
status "headroom green (512 MB writable)"

status "=== stage 1: static checks ==="
if ! ( cd "$REPO" && python3 evals/check_static.py > "$WS/static.log" 2>&1 ); then
  status "STATIC FAILED — see $WS/static.log"; notify "evals: static checks FAILED"
  exit 1
fi
status "static green"

status "=== stage 1: engine checks ==="
if ! ( cd "$REPO" && python3 evals/check_engine.py > "$WS/engine.log" 2>&1 ); then
  status "ENGINE FAILED — see $WS/engine.log"; notify "evals: engine checks FAILED"
  exit 1
fi
status "engine green"

if [ ${#IDEM[@]} -gt 0 ]; then
  # Targeted idempotency: re-measure the zero-diff promise for one scenario
  # after a law fix, without paying for the whole corpus again.
  for name in "${IDEM[@]}"; do
    idem_scenario "$(DIR_OF "$name")"
  done
  status "=== targeted idempotency complete ==="; notify "evals: targeted idempotency complete"
  exit 0
fi

if [ ${#ONLY[@]} -gt 0 ]; then
  ONLY_FAILED=0
  for name in "${ONLY[@]}"; do
    sc="$(DIR_OF "$name")"
    if run_scenario "$sc"; then
      finish_scenario "$sc" || ONLY_FAILED=1
    else upd_queue "$sc" failed; ONLY_FAILED=1
    fi
  done
  status "=== targeted run complete ==="; notify "evals: targeted run complete"
  exit $ONLY_FAILED
fi

if [ $SKIP_SMOKE -eq 0 ]; then
  status "=== stage 2: smoke (upgrade) ==="
  if run_scenario upgrade; then
    finish_scenario upgrade
    if ! grade_clean "$WS/upgrade/outputs/grading.json"; then
      status "SMOKE GRADED WITH FAILURES OR UNMEASURED ASSERTS — stopping before the full corpus (staged execution: don't burn five runs off a dead change)"
      notify "evals: SMOKE FAILED — corpus not started"; exit 1
    fi
  else
    status "SMOKE FAILED — stopping"; notify "evals: SMOKE FAILED"; exit 1
  fi
fi

status "=== stage 3: corpus ==="
CORPUS_FAILED=0
for name in $(python3 -c "
import json
d=json.load(open('$REPO/evals/evals.json'))
print(' '.join(e['name'] for e in d['evals'] if e['name'] not in ('idempotency','upgrade')))"); do
  sc="$(DIR_OF "$name")"
  if run_scenario "$sc"; then
    finish_scenario "$sc" || { CORPUS_FAILED=1; status "$sc graded with failures"; }
  else upd_queue "$sc" failed; CORPUS_FAILED=1; notify "eval $sc: FAILED (3 attempts)"
  fi
done
[ $CORPUS_FAILED -eq 0 ] || { status "corpus had failures (a scenario failed to run, or graded below 100%) — idempotency stage skipped"; notify "evals: corpus had failures"; exit 1; }

status "=== stage 4: idempotency ==="
for sc in fresh-scaffold-dotnet upgrade restructure; do
  idem_scenario "$sc"
done

status "=== ALL STAGES GREEN ==="
notify "evals: FULL RUN GREEN (corpus + idempotency)"
