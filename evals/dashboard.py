#!/usr/bin/env python3
"""Live eval dashboard — renders the state of a background eval run.

Usage:
  python3 evals/dashboard.py <workspace> [--interval 3]   # render loop
  python3 evals/dashboard.py <workspace> --once            # single render

Reads (never writes) the orchestrator's artifacts:
  <ws>/<scenario>/outputs/run.log        agent stream (size/mtime = liveness)
  <ws>/<scenario>/outputs/*.md           expected deliverables
  <ws>/<scenario>/repo/                  fixture (dirty count, artifacts)
  /tmp/opencode/orchestrate-all.log      orchestrator timeline (optional)

Writes <ws>/dashboard.html — static file, meta-refresh, no server, nothing
leaves the machine (the kbo dashboard pattern). A stale render must look
stale: the generated-at stamp is the first thing on the page.
"""
from __future__ import annotations

import argparse
import html
import json
import re
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path

EXPECTED = {
    "fresh-scaffold-dotnet": ["repo/docs/ai/manifest.json", "repo/docs/cases/README.md"],
    "legacy-migration": ["outputs/migration-report.md"],
    "upgrade": ["outputs/upgrade-report.md"],
    "rotted-layer": ["outputs/audit-report.md"],
    "restructure": ["outputs/restructure-report.md"],
}
# Display names: the mode each fixture exercises (the rotted-layer dir IS
# the audit scenario — the dashboard speaks in modes, not raw dir names).
DISPLAY = {
    "fresh-scaffold-dotnet": "scaffold",
    "legacy-migration": "migration",
    "upgrade": "upgrade",
    "rotted-layer": "audit",
    "restructure": "restructure",
}
SCENARIOS = list(EXPECTED)
STALL_AFTER_S = 180

EVENT_RE = re.compile(r"^\[(?P<sc>[^]]+)\] (?P<ev>.*)$")
TIME_RE = re.compile(r"(\d{2}:\d{2}:\d{2})")


def esc(s: str) -> str:
    return html.escape(s, quote=True)


def strip_ansi(s: str) -> str:
    return re.sub(r"\x1b\[[0-9;]*[A-Za-z]", "", s)


def parse_timeline(log: Path) -> dict[str, list[str]]:
    events: dict[str, list[str]] = {sc: [] for sc in SCENARIOS}
    if not log.exists():
        return events
    for line in log.read_text(errors="ignore").splitlines():
        m = EVENT_RE.match(line)
        if not m:
            continue
        sc, ev = m.group("sc"), m.group("ev").strip()
        if sc in events:
            events[sc].append(ev)
    return events


def state_of(events: list[str]) -> tuple[str, str]:
    """(state, detail) from the event tail; state in
    pending|running|stalled|retrying|done|failed."""
    if not events:
        return "pending", ""
    last = events[-1]
    t = TIME_RE.search(last)
    stamp = t.group(1) if t else ""
    if "FAILED after" in last:
        return "failed", f"final — {stamp}"
    if " DONE " in f" {last} " or last.split(" ", 1)[0] in {"attempt", "resume"} and " DONE " in last:
        return "done", last
    if "stalled" in last:
        return "stalled", f"{last} {stamp}".strip()
    if "without expected output" in last:
        return "retrying", f"{last} {stamp}".strip()
    if last.endswith("start") or " start " in last or "start" in last:
        return "running", f"{last} {stamp}".strip()
    return "running", last


def count_opencode() -> int:
    r = subprocess.run(["ps", "-eo", "cmd"], capture_output=True, text=True)
    return sum(1 for l in r.stdout.splitlines()
               if "opencode" in l and " run" in l and "dashboard" not in l)


def git_dirty(repo: Path) -> tuple[int, int]:
    r = subprocess.run(["git", "-C", str(repo), "status", "--porcelain"],
                       capture_output=True, text=True)
    lines = r.stdout.splitlines()
    non_obj = [l for l in lines if "/obj/" not in l]
    return len(lines), len(non_obj)


def log_errors(log: Path, tail_chars: int = 6000) -> list[str]:
    if not log.exists():
        return []
    text = strip_ansi(log.read_text(errors="ignore")[-tail_chars:])
    hits = []
    for line in text.splitlines():
        if re.search(r"(?i)(stream error|permission denied|aborted|fatal|"
                     r"Cannot connect|API.?error|exit code [1-9])", line):
            line = line.strip()
            if line and line not in hits:
                hits.append(line[:200])
    return hits[-2:]


def log_tail(log: Path, n: int = 4) -> str:
    if not log.exists():
        return ""
    text = strip_ansi(log.read_text(errors="ignore"))
    lines = [l.rstrip() for l in text.splitlines() if l.strip()]
    return "\n".join(l[-160:] for l in lines[-n:])


def grading(d: Path) -> dict | None:
    f = d / "grading.json"
    if not f.exists():
        return None
    try:
        return json.loads(f.read_text())
    except (json.JSONDecodeError, OSError):
        return None


def load_queue(ws: Path) -> dict | None:
    f = ws / "queue.json"
    try:
        return json.loads(f.read_text())
    except (OSError, json.JSONDecodeError):
        return None


def history(d: Path) -> list[dict]:
    f = d / "outputs" / "grade-history.jsonl"
    if not f.exists():
        return []
    out = []
    for line in f.read_text(errors="ignore").splitlines():
        try:
            out.append(json.loads(line))
        except json.JSONDecodeError:
            continue
    return out


def flaky_panel(d: Path) -> str:
    """From ≥2 graded runs: which asserts fail in EVERY run (persistent —
    a real defect or a grader bug) vs SOME runs (flaky — nondeterminism
    to keep under control)."""
    runs = history(d)
    if len(runs) < 2:
        return ""
    counts: dict[str, int] = {}
    for r in runs:
        for name in r.get("fails", []):
            counts[name] = counts.get(name, 0) + 1
    n = len(runs)
    persistent = [(k, v) for k, v in counts.items() if v == n]
    flaky = [(k, v) for k, v in counts.items() if 0 < v < n]
    rows = []
    for k, v in sorted(persistent, key=lambda x: -x[1])[:3]:
        rows.append(f'<div class="flaky persist">persistent ({v}/{n}): {esc(k[:70])}</div>')
    for k, v in sorted(flaky, key=lambda x: -x[1])[:4]:
        rows.append(f'<div class="flaky">flaky ({v}/{n}): {esc(k[:70])}</div>')
    if not rows:
        return f'<div class="dim" style="margin-top:4px">history: {n} runs, no repeats</div>'
    return (f'<div class="dim" style="margin-top:4px">grade history: {n} runs</div>'
            + "".join(rows))


def render(ws: Path, timeline_log: Path) -> str:
    now = datetime.now(timezone.utc)
    events = parse_timeline(timeline_log)
    alive = count_opencode()
    queue = load_queue(ws) or {}
    q_statuses = queue.get("statuses", {})
    q_order = queue.get("order", [])

    def final_state(sc: str) -> str:
        """Effective status: queue view when present (it is current), timeline
        otherwise. Three terminal states, three different questions:
        done — completed AND graded clean; partial ('w/ errors') — completed
        but graded with failures; failed — the run itself crashed or never
        produced its expected output."""
        q_state = q_statuses.get(sc)
        if q_state == "running":
            return "running"
        if q_state in ("done", "failed", "queued"):
            state = "pending" if q_state == "queued" else q_state
            if state == "done":
                gr = grading(ws / sc)
                if gr is not None and gr["summary"]["failed"] > 0:
                    return "partial"
            return state
        return state_of(events[sc])[0]

    total_started = sum(1 for sc in SCENARIOS if events[sc] or sc in q_statuses)
    states = {sc: final_state(sc) for sc in SCENARIOS}
    done = [sc for sc in SCENARIOS if states[sc] == "done"]
    failed = [sc for sc in SCENARIOS if states[sc] == "failed"]
    running = [sc for sc in SCENARIOS if states[sc] in ("running", "retrying")]
    partial = [sc for sc in SCENARIOS if states[sc] == "partial"]

    cards = []
    for sc in SCENARIOS:
        d = ws / sc
        log = d / "outputs" / "run.log"
        repo = d / "repo"
        state, detail = state_of(events[sc])
        expected = EXPECTED[sc]
        artifacts = [(Path(d) / p, p) for p in expected]
        art_html = "".join(
            f'<span class="tag {"ok" if p.exists() else "miss"}">'
            f'{esc(p2.split("/")[-1])}</span>'
            for p, p2 in artifacts)
        size = log.stat().st_size if log.exists() else 0
        age = (time.time() - log.stat().st_mtime) if log.exists() else None
        raw_dirty, dirty = git_dirty(repo) if repo.exists() else (0, 0)
        gr = grading(d)
        grade_html = ""
        if gr:
            sm = gr["summary"]
            rate = int(sm["pass_rate"] * 100)
            if state in ("running", "pending", "stalled", "retrying"):
                # a grade from a previous run must not pose as current
                grade_html = (f'<div class="dim prevgrade">prev run: {sm["passed"]}/{sm["total"]}'
                              f' ({rate}%) — stale while {state}</div>')
            else:
                cls = "gok" if sm["failed"] == 0 else ("gsome" if rate >= 80 else "gbad")
                fails = [e for e in gr["expectations"] if not e["passed"]]
                fail_rows = "".join(
                    f'<div class="gfail">✗ {esc(e["text"])} — {esc(e["evidence"][:120])}</div>'
                    for e in fails[:5])
                more = f'<div class="dim">… +{len(fails) - 5} more</div>' if len(fails) > 5 else ""
                grade_html = (f'<div class="grade {cls}">graded: {sm["passed"]}/{sm["total"]}'
                              f' ({rate}%)</div>{fail_rows}{more}')
        elif state == "done":
            grade_html = '<div class="dim">grading pending…</div>'
        flaky_html = flaky_panel(d) if state in ("done", "partial", "failed") else ""
        errs = log_errors(log)
        err_html = "".join(f'<div class="err">{esc(e)}</div>' for e in errs)
        tail = esc(log_tail(log))
        # newest first: the modal opens on the latest activity, no need to
        # scroll to the bottom of a 128 KB wall
        if log.exists():
            lines = strip_ansi(log.read_text(errors="ignore"))[-131072:].splitlines()
            full = esc("\n".join(reversed(lines)))
        else:
            full = "(no log yet)"
        attempts = sum(1 for e in events[sc] if "attempt" in e and "start" in e)
        resumes = sum(1 for e in events[sc] if "resume" in e and "start" in e)
        mid = sc.replace("-", "_")
        # Queue status outranks the timeline: an active orchestration chain
        # writes queue.json, and its view is current (the timeline log only
        # ever grows and its tail may describe a finished chain).
        q_state = q_statuses.get(sc)
        if q_state == "running":
            state, detail = "running", "orchestrator chain: active"
        elif q_state in ("done", "failed") and grading(d) is not None:
            gr_state = grading(d)
            if q_state == "done" and gr_state is not None and gr_state["summary"]["failed"] > 0:
                state, detail = "partial", "completed — graded with failures"
            else:
                state = "done" if q_state == "done" else "failed"
                detail = "queue: " + q_state
        elif q_state == "queued":
            pos = q_order.index(sc) + 1 if sc in q_order else "?"
            state, detail = "pending", f"queued (#{pos} in chain)"
        elif q_state in ("done", "failed"):
            state = "done" if q_state == "done" else "failed"
            detail = "queue: " + q_state
        else:
            state, detail = state_of(events[sc])
        # stall hint derives from the FINAL state (queue may have just flipped
        # running->done; a frozen log under a green card is noise, not a stall)
        stall_hint = ""
        if state in ("running", "stalled", "retrying") and age is not None and age > STALL_AFTER_S:
            stall_hint = f'<div class="warn">log frozen {int(age)}s — stall suspected</div>'
        elif state in ("running",):
            stall_hint = f'<div class="dim">stream age {int(age)}s</div>' if age is not None else ""
        cards.append(f"""
<div class="card {state}">
  <div class="head"><span class="name">{esc(DISPLAY.get(sc, sc))}</span>
    <span class="dim" style="font-weight:normal">{esc(sc)}/</span>
    <span class="state {state}">{"w/ errors" if state == "partial" else state}</span></div>
  <div class="dim">{esc(detail)}</div>
  <div>attempts: {attempts} · resumes: {resumes} · log: {size//1024} KB ·
    dirty: {dirty} (+{raw_dirty-dirty} obj)</div>
  <div class="tags">{art_html}</div>
  {stall_hint}{grade_html}{flaky_html}{err_html}
  <pre class="tailopen" onclick="openLog('{mid}')">{tail}</pre>
  <button class="logbtn" onclick="openLog('{mid}')">log \u29e2</button>
  <div class="mback" id="m-{mid}" onclick="closeLog(event)">
    <div class="mwin" onclick="event.stopPropagation()">
      <div class="mhead"><span>{esc(DISPLAY.get(sc, sc))} — full log (newest first)</span>
        <button onclick="closeLogX()">close \u00d7</button></div>
      <pre class="mlog">{full}</pre>
    </div>
  </div>
</div>""")

    tl_tail = esc("\n".join(timeline_log.read_text(errors="ignore")
                            .splitlines()[-8:])) if timeline_log.exists() else "(no orchestrator log)"
    return f"""<!doctype html><html lang="en"><head><meta charset="utf-8">
<title>legislator eval — live</title>
<script>
let paused = false;
function openLog(id) {{ paused = true;
  document.getElementById("pausetag").style.display = "inline";
  document.getElementById("m-" + id).style.display = "flex"; }}
function closeLog(ev) {{ if (ev.target === ev.currentTarget) closeLogX(); }}
function closeLogX() {{
  document.querySelectorAll(".mback").forEach(m => m.style.display = "none");
  paused = false;
  document.getElementById("pausetag").style.display = "none"; }}
document.addEventListener("keydown", e => {{ if (e.key === "Escape") closeLogX(); }});
setInterval(() => {{ if (!paused) location.reload(); }}, 3000);
</script>
<style>
 body{{background:#111;color:#ddd;font:13px/1.45 ui-monospace,monospace;
      margin:16px;}}
 h1{{font-size:16px;margin:0 0 4px}}
 .grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(420px,1fr));
       gap:12px;margin-top:12px}}
 .card{{border:1px solid #333;border-radius:8px;padding:10px;background:#181818}}
 .card.running{{border-color:#3a6ea5}} .card.done{{border-color:#2e7d32}}
 .card.failed{{border-color:#c62828}} .card.partial{{border-color:#b8860b}} .card.stalled{{border-color:#e65100}}
 .card.retrying{{border-color:#8d6e63}} .card.pending{{opacity:.55}}
 .head{{display:flex;justify-content:space-between;font-weight:bold}}
 .state{{padding:0 8px;border-radius:4px}}
 .state.running{{background:#1c3a5e}} .state.done{{background:#1b3a1f}}
 .state.failed{{background:#4a1515}} .state.partial{{background:#4a3a00;color:#fc6}} .state.stalled{{background:#4a2c00}}
 .state.retrying{{background:#3a2c24}} .state.pending{{background:#222}}
 .tag{{display:inline-block;margin:2px 4px 2px 0;padding:0 6px;
      border-radius:4px;background:#222}}
 .tag.ok{{background:#1b3a1f;color:#8f8}} .tag.miss{{color:#777}}
 .err{{color:#f66;margin-top:4px;white-space:nowrap;overflow:hidden;
      text-overflow:ellipsis}}
 .warn{{color:#fb0;margin-top:4px}} .dim{{color:#888}}
 .tailopen{{cursor:pointer}} .tailopen:hover{{outline:1px solid #3a6ea5}}
 .logbtn{{background:#222;color:#8ab;border:1px solid #333;border-radius:4px;
         padding:0 6px;margin-top:4px;cursor:pointer;font:inherit}}
 .mback{{display:none;position:fixed;inset:0;background:rgba(0,0,0,.7);
        z-index:10;align-items:center;justify-content:center}}
 .mwin{{background:#141414;border:1px solid #444;border-radius:8px;
       width:min(90vw,1100px);display:flex;flex-direction:column}}
 .mhead{{display:flex;justify-content:space-between;align-items:center;
        padding:8px 12px;border-bottom:1px solid #333}}
 .mhead button{{background:#333;color:#ddd;border:0;border-radius:4px;
               padding:2px 8px;cursor:pointer;font:inherit}}
 .mlog{{margin:0;padding:10px;overflow:auto;max-height:80vh;
       white-space:pre-wrap;color:#bbb}}
 .grade{{margin-top:6px;font-weight:bold;border-radius:4px;padding:2px 6px;display:inline-block}}
 .grade.gok{{background:#1b3a1f;color:#8f8}} .grade.gsome{{background:#3a3000;color:#fc6}}
 .grade.gbad{{background:#4a1515;color:#f88}}
 .prevgrade{{margin-top:6px;font-size:12px}}
 .flaky{{color:#e9a23b;font-size:12px;margin-top:2px}}
 .flaky.persist{{color:#f66}}
 .gfail{{color:#f88;margin-top:3px;font-size:12px;white-space:nowrap;overflow:hidden;
        text-overflow:ellipsis}}
 pre{{white-space:pre-wrap;background:#0c0c0c;border-radius:6px;
     padding:6px;margin:8px 0 0;color:#aaa;max-height:120px;overflow:hidden}}
 .summary span{{margin-right:14px}}
</style></head><body>
<h1>legislator eval — live</h1>
<div class="dim">generated {now.strftime("%Y-%m-%d %H:%M:%S")} UTC ·
 refresh 3s <span id="pausetag" class="warn" style="display:none">— PAUSED (log open)</span> · opencode runs alive: {alive}</div>
<div class="summary" style="margin-top:8px">
 <span>scenarios: {total_started}/{len(SCENARIOS)} started</span>
 <span style="color:#6bf">running: {len(running)}</span>
 <span style="color:#8f8">done: {len(done)}</span>
 <span style="color:#fc6">w/ errors: {len(partial)}</span>
 <span style="color:#f88">failed: {len(failed)}</span>
</div>
<div class="grid">{''.join(cards)}</div>
<h2 style="font-size:13px;margin-top:16px">orchestrator tail</h2>
<pre>{tl_tail}</pre>
</body></html>"""


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("workspace", type=Path)
    ap.add_argument("--interval", type=float, default=3.0)
    ap.add_argument("--once", action="store_true")
    ap.add_argument("--timeline", type=Path,
                    default=Path("/tmp/opencode/orchestrate-all.log"))
    a = ap.parse_args()
    out = a.workspace / "dashboard.html"
    while True:
        out.write_text(render(a.workspace, a.timeline))
        if a.once:
            print(out)
            return
        time.sleep(a.interval)


if __name__ == "__main__":
    main()
