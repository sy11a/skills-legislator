#!/usr/bin/env bash
# evals-bg.sh — background staged eval runner (BL-037, productized from
# the v17 benchmark's live iteration).
#
# Stages, in order, each only on the previous green:
#   1. static   — evals/check_static.py (seconds)
#   2. smoke    — the upgrade scenario (most change-sensitive surface)
#   3. corpus   — every scenario in evals.json except idempotency
#   4. idem     — commit each run-1 result, second pass, zero-diff grade
#
# Per scenario: full fixture reset on EVERY invocation, confirmation
# waiver (headless — nobody answers), stall detection, resume ladder,
# auto-grade into <ws>/<scenario>/outputs/, queue.json + status.md as the
# machine-readable contract, notify-send on scenario/run boundaries.
#
# Usage:
#   tools/evals-bg.sh <workspace> [--skip-smoke] [--only SCEN [SCEN..]]
#   MODEL=provider/zai-coding-plan/glm-5-turbo  tools/evals-bg.sh <ws>
#   NO_BROWSER=1 ...   # don't launch the dashboard/browser (service runs)
# Env: MODEL (default zai-coding-plan/glm-5-turbo — glm-5.3 drops long
# streams on this endpoint), KBO_EVALS_NO_BROWSER same as NO_BROWSER.
set -u
WS="${1:?usage: evals-bg.sh <workspace> [--skip-smoke|--only SCEN..]}"
shift || true

REPO="$(cd "$(dirname "$0")/.." && pwd)"
SKILL="$REPO/skill"
MODEL="${MODEL:-zai-coding-plan/glm-5-turbo}"
STALL_SECS=180
MAX_ATTEMPTS=3
MAX_RESUMES=4
P="opencode"" run"   # concat: this script's own cmdline must never match pkill

SKIP_SMOKE=0
ONLY=()
while [ $# -gt 0 ]; do
  case "$1" in
    --skip-smoke) SKIP_SMOKE=1 ;;
    --only) shift; while [ $# -gt 0 ] && [[ "$1" != --* ]]; do ONLY+=("$1"); shift; done; continue ;;
    *) echo "unknown flag: $1" >&2; exit 2 ;;
  esac
  shift
done

mkdir -p "$WS"
TL="$WS/orchestrate.log"

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
    rotted-layer) echo "outputs/audit-report.md";;
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
  echo "[$(date +%H:%M:%S)] $*" | tee -a "$TL" >> "$WS/status.md"
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

reset_repo() {
  git -C "$1" reset --quiet >/dev/null 2>&1 || true
  git -C "$1" checkout -- . >/dev/null 2>&1 || true
  git -C "$1" clean -fdq >/dev/null 2>&1 || true
}

expected_ok() {
  local sc="$1" ws="$2" report
  report="$(REPORT_OF "$sc")"
  if [ -n "$report" ]; then [ -s "$ws/$sc/$report" ]
  else [ -f "$ws/$sc/repo/docs/ai/manifest.json" ] && [ -f "$ws/$sc/repo/docs/cases/README.md" ]
  fi
}

msg_block() { # <dir>
  local sc="$1" p report out
  p="$(prompt_of "$(SC_OF "$sc")")"
  out="$WS/$sc/outputs"
  case "$sc" in
    legacy-migration|legacy-migration-agents-first|upgrade|upgrade-drop-stack) report="Write your full Step 7 report (all sections, including Health and any Constitution candidates) to $out/$(basename "$(REPORT_OF "$sc")") — overwrite if it exists.";;
    rotted-layer) report="Save your full audit report to $out/audit-report.md — outside the target repo (which you must not touch: zero writes).";;
    restructure) report="Write your final restructure report to $out/restructure-report.md — overwrite if it exists.";;
    *) report="";;
  esac
  cat <<MSG
Read $SKILL/SKILL.md and follow it EXACTLY, including referenced files under $SKILL/. Work in $WS/$sc/repo. $p

Ground rules: NEVER run git commit — leave all changes uncommitted for review. AGENTS.md is project-owned — never edit it directly; every proposed AGENTS.md change goes in the report only. Byte-for-byte copies happen via Bash cp exactly as the skill instructs. The scenario folder $WS/$sc is the working directory (the repo and $out are writable siblings).

CONFIRMATION WAIVER (scripted run — nobody will answer): every confirmation the skill asks for is pre-approved. Answer with the skill's own default plus what the repo evidences; the skill knows what to deploy. Never end your turn on a question. $report
MSG
}

spawn() { # <dir> <fresh|resume> — self-contained: no /tmp helper
  local sc="$1" log="$WS/$1/outputs/run.log" msg
  : >> "$log"
  [ "$2" = fresh ] && msg_block "$sc" > "$WS/$sc/outputs/prompt.txt"
  if [ "$2" = resume ]; then
    msg="Continue exactly where you left off (the previous stream dropped). Re-check current file state on disk before acting — you may have already completed some steps. CONFIRMATION WAIVER still applies: never end your turn on a question."
    setsid opencode run --dir "$WS/$sc" -m "$MODEL" --continue "$msg" \
      </dev/null >> "$log" 2>&1 &
  else
    msg="$(msg_block "$sc")"
    setsid opencode run --dir "$WS/$sc" -m "$MODEL" "$msg" \
      </dev/null >> "$log" 2>&1 &
  fi
  echo $!
}

wait_or_stall() { # <pid> <dir>
  local pid=$1 sc="$2" last=-1 last_dirty=-1 last_change size dirty now
  last_change=$(date +%s)
  while kill -0 "$pid" 2>/dev/null; do
    sleep 15
    size=$(stat -c%s "$WS/$sc/outputs/run.log" 2>/dev/null || echo 0)
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
  reset_repo "$WS/$sc/repo"; : > "$WS/$sc/outputs/run.log"

  for attempt in $(seq 1 $MAX_ATTEMPTS); do
    status "$sc attempt $attempt (fresh) start"
    [ "$attempt" -gt 1 ] && reset_repo "$WS/$sc/repo"
    local pid; pid=$(spawn "$sc" fresh)
    if wait_or_stall "$pid" "$sc"; then
      if expected_ok "$sc" "$WS"; then status "$sc DONE (attempt $attempt)"; return 0; fi
      status "$sc attempt $attempt exited without expected output"
      continue
    fi
    status "$sc attempt $attempt stalled — resume ladder"
    pkill -f "$P" 2>/dev/null; sleep 2
    for r in $(seq 1 $MAX_RESUMES); do
      status "$sc resume $r start"
      pid=$(spawn "$sc" resume)
      if wait_or_stall "$pid" "$sc"; then
        if expected_ok "$sc" "$WS"; then status "$sc DONE (resume $r)"; return 0; fi
        status "$sc resume $r exited without expected output"; break
      fi
      status "$sc resume $r stalled again"
      pkill -f "$P" 2>/dev/null; sleep 2
    done
  done
  status "$sc FAILED after $MAX_ATTEMPTS attempts"
  return 1
}

finish_scenario() { # <dir> — grade + queue + notify (after a DONE run)
  local sc="$1"
  auto_grade "$sc"
  local g="$WS/$sc/outputs/grading.json" verdict="?"
  [ -s "$g" ] && verdict=$(python3 -c "
import json;d=json.load(open('$g'));s=d['summary']
print(f\"{s['passed']}/{s['total']}\" + (' CLEAN' if s['failed']==0 else ' WITH FAILURES'))")
  status "$sc graded: $verdict"
  local failed=0
  [ -s "$g" ] && python3 -c "
import json,sys;sys.exit(0 if json.load(open('$g'))['summary']['failed']==0 else 1)" || failed=1
  if [ $failed -eq 0 ]; then upd_queue "$sc" done; notify "eval $sc: $verdict"
  else upd_queue "$sc" partial; notify "eval $sc: $verdict (w/ errors)"; fi
}

# ---- dashboard + browser ----
if [ -z "${NO_BROWSER}${KBO_EVALS_NO_BROWSER}" ]; then
  setsid nohup python3 "$REPO/evals/dashboard.py" "$WS" --interval 3 --open \
    > "$WS/dashboard.log" 2>&1 &
fi

# ============================= STAGES =============================

status "=== stage 1: static checks ==="
if ! ( cd "$REPO" && python3 evals/check_static.py > "$WS/static.log" 2>&1 ); then
  status "STATIC FAILED — see $WS/static.log"; notify "evals: static checks FAILED"
  exit 1
fi
status "static green"

if [ ${#ONLY[@]} -gt 0 ]; then
  for name in "${ONLY[@]}"; do
    sc="$(DIR_OF "$name")"
    run_scenario "$sc" && finish_scenario "$sc" || upd_queue "$sc" failed
  done
  status "=== targeted run complete ==="; notify "evals: targeted run complete"
  exit 0
fi

if [ $SKIP_SMOKE -eq 0 ]; then
  status "=== stage 2: smoke (upgrade) ==="
  if run_scenario upgrade; then
    finish_scenario upgrade
    if ! python3 -c "
import json,sys;sys.exit(0 if json.load(open('$WS/upgrade/outputs/grading.json'))['summary']['failed']==0 else 1)"; then
      status "SMOKE GRADED WITH FAILURES — stopping before the full corpus (staged execution: don't burn five runs off a dead change)"
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
  if run_scenario "$sc"; then finish_scenario "$sc"
  else upd_queue "$sc" failed; CORPUS_FAILED=1; notify "eval $sc: FAILED (3 attempts)"
  fi
done
[ $CORPUS_FAILED -eq 0 ] || { status "corpus had failures — idempotency stage skipped"; notify "evals: corpus had failures"; exit 1; }

status "=== stage 4: idempotency ==="
for sc in fresh-scaffold-dotnet upgrade restructure; do
  git -C "$WS/$sc/repo" add -A >/dev/null 2>&1
  git -C "$WS/$sc/repo" commit -q -m "run 1" >/dev/null 2>&1 || true
  : > "$WS/$sc/outputs/idem-run.log"
  status "idem:$sc second pass start"
  ok=""
  for attempt in 1 2 3; do
    pid=$(spawn "$sc" fresh)
    if wait_or_stall "$pid" "$sc"; then ok=1; break; fi
    pkill -f "$P" 2>/dev/null; sleep 2
  done
  if [ -n "$ok" ]; then
    ( cd "$REPO" && python3 evals/grade.py "$WS" "idempotency:$sc" \
        > "$WS/$sc/outputs/idem-grade.txt" 2>&1 ) || true
    if python3 -c "
import json,sys;d=json.load(open('$WS/$sc/outputs/grading_idempotency.json'));sys.exit(0 if d['summary']['failed']==0 else 1)" 2>/dev/null; then
      status "idem:$sc ZERO DIFF"; notify "eval idem $sc: zero diff"
    else
      status "idem:$sc DIFF FOUND — see $WS/$sc/outputs/idem-grade.txt"; notify "eval idem $sc: DIFF"; exit 1
    fi
  else
    status "idem:$sc second pass failed to complete"; notify "eval idem $sc: FAILED"; exit 1
  fi
done

status "=== ALL STAGES GREEN ==="
notify "evals: FULL RUN GREEN (corpus + idempotency)"
