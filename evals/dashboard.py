#!/usr/bin/env python3
"""Live eval dashboard — renders the state of a background eval run.

Usage:
  python3 evals/dashboard.py <workspace> [--interval 3]   # render loop
  python3 evals/dashboard.py <workspace> --once            # single render

Reads (never writes) the orchestrator's artifacts:
  <ws>/<scenario>/outputs/run.log        agent transcript (shown in the log modal)
  <ws>/<scenario>/outputs/run.jsonl      raw stream (claude profile) — liveness
  <ws>/<scenario>/outputs/*.md           expected deliverables
  <ws>/<scenario>/repo/                  fixture (dirty count, artifacts)
  /tmp/opencode/orchestrate-all.log      orchestrator timeline (optional)

Writes <ws>/dashboard.html — static file, meta-refresh, no server, nothing
leaves the machine. A stale render must look
stale: the generated-at stamp is the first thing on the page.
"""
from __future__ import annotations

import argparse
import html
import json
import os
import re
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path

# Where each scenario's deliverable lands — display labels for the card,
# not existence probes (see the chip rendering below). The key order also
# fixes the card order.
EXPECTED = {
    "fresh-scaffold-dotnet": ["repo/docs/ai/manifest.json", "repo/docs/cases/README.md"],
    "legacy-migration": ["outputs/migration-report.md"],
    "legacy-migration-agents-first": ["outputs/migration-report.md"],
    "upgrade": ["outputs/upgrade-report.md"],
    "upgrade-drop-stack": ["outputs/upgrade-report.md"],
    "rotted-layer": ["outputs/audit-report.md"],
    "restructure": ["outputs/restructure-report.md"],
    # NOT docs/cases/README.md — that ships with the clean legislated
    # fixture. The deliverable is a NEW case directory; the runner's own
    # completion oracle looks for exactly that.
    "case-practice": ["repo/docs/cases/BL-NNN/"],
}
# Display names: the mode each fixture exercises (the rotted-layer dir IS
# the audit scenario — the dashboard speaks in modes, not raw dir names).
DISPLAY = {
    "fresh-scaffold-dotnet": "scaffold",
    "legacy-migration": "migration",
    "legacy-migration-agents-first": "migration (agents-first)",
    "upgrade": "upgrade",
    "upgrade-drop-stack": "upgrade (drop-stack)",
    "rotted-layer": "audit",
    "restructure": "restructure",
    "case-practice": "case-practice",
}
SCENARIOS = list(EXPECTED)
STALL_AFTER_S = 180

EVENT_RE = re.compile(r"^\[(?P<sc>[^]]+)\] (?P<ev>.*)$")
TIME_RE = re.compile(r"(\d{2}:\d{2}:\d{2})")


def idem_html(evs: list[str], d: Path) -> str:
    """The idempotency pass has no queue row — it runs outside the corpus
    chain — so it never showed on the dashboard at all: every measurement in
    the v17 cycle was visible only in the orchestrator log, and a card sat
    reading "done" while a second pass was in flight underneath it (found
    2026-08-22). Derived here from the timeline (a start with no verdict
    after it means in flight) plus grading_idempotency.json."""
    started = verdict = None
    for e in evs:
        if e.startswith("idem second pass start"):
            started, verdict = e, None
        elif e.startswith("idem "):
            verdict = e
    if started is None:
        return ""
    if verdict is None:
        return ('<div class="warn">idempotency: second pass running — '
                'the card\'s grade is the corpus run, not this pass</div>')
    gr = None
    f = d / "outputs" / "grading_idempotency.json"
    if f.exists():
        try:
            gr = json.loads(f.read_text())
        except (OSError, json.JSONDecodeError):
            gr = None
    ok = "ZERO DIFF" in verdict
    detail = ""
    if gr is not None and not ok:
        fails = [e["evidence"] for e in gr["expectations"] if not e["passed"]]
        detail = f' — {esc(fails[0][:110])}' if fails else ""
    cls = "gok" if ok else "gbad"
    label = "zero diff" if ok else "DIFF"
    return f'<div class="grade {cls}">idempotency: {label}</div>{detail}'


def resolve_expected(base: Path, rel: str) -> str:
    """Turn a pattern expectation into the real name once it exists. The
    case scenario's deliverable is a NEW `docs/cases/BL-NNN/` directory whose
    number the agent picks, so the label starts as the pattern and becomes
    the actual directory the moment one is created."""
    if "NNN" not in rel:
        return rel
    parent, name = Path(rel).parent, Path(rel).name
    stem = name.split("NNN")[0]
    d = base / parent
    if d.is_dir():
        hits = sorted(c.name for c in d.iterdir()
                      if c.is_dir() and c.name.startswith(stem))
        if hits:
            return f"{parent / hits[0]}/"
    return rel


def basename(rel: str) -> str:
    """Last path segment, keeping a trailing slash so a directory target
    still reads as one (a bare split() left 'docs/cases/BL-NNN/' blank)."""
    return rel.rstrip("/").split("/")[-1] + ("/" if rel.endswith("/") else "")


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


def count_runner(runner: str, ws: Path) -> int:
    """Live scenario agents of THIS workspace, in the current run's profile.
    Each profile has its own process shape (counting 'opencode' while a
    claude run is live reports zero and reads as a dead run), and the
    workspace scope keeps unrelated agents on the machine out of the count."""
    r = subprocess.run(["ps", "-eo", "cmd"], capture_output=True, text=True)
    lines = [l for l in r.stdout.splitlines()
             if "dashboard" not in l and str(ws) in l]
    if runner == "claude":
        return sum(1 for l in lines if "claude" in l and " -p " in l)
    return sum(1 for l in lines if "opencode" in l and " run" in l)


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
        # A tool call exiting non-zero is routine probing, not a run in
        # trouble: migration mode is *supposed* to test whether CLAUDE.md
        # exists, and `ls` answers "no" by exiting 2. The bare "exit code N"
        # heuristic was written for opencode prose; against the claude
        # profile's transcript it painted every such probe red — and, because
        # only the last 6000 chars are scanned, the banner flickered in and
        # out as probes scrolled through the window (found 2026-08-22).
        routine_tool_result = re.match(r"\s*\[(ERR|ok)\]", line)
        serious = re.search(r"(?i)(stream error|permission denied|aborted|"
                            r"fatal|cannot connect|API.?error|connection lost)",
                            line)
        if serious or (not routine_tool_result
                       and re.search(r"(?i)exit code [1-9]", line)):
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
    f = d / "outputs" / "grading.json"
    if not f.exists():
        f = d / "grading.json"  # pre-relocation layout
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


def load_runs(ws: Path) -> tuple[dict, list[dict]]:
    """The current run's full provenance record + the run history
    (run.json: {current, runs}). The record carries run_id, runner profile,
    model and law commit — everything needed to tell two runs apart."""
    f = ws / "run.json"
    try:
        d = json.loads(f.read_text())
        return d.get("current", {}), d.get("runs", [])
    except (OSError, json.JSONDecodeError):
        return {}, []


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


def current_law(ws: Path) -> str | None:
    """The newest law stamp across all scenarios' histories — the generation
    of the latest graded run. Entries without a stamp predate the mechanism
    (older generations by definition)."""
    newest = None
    for sc in SCENARIOS:
        for r in history(ws / sc):
            law = r.get("law")
            if law:
                if newest is None or r["ts"] > newest[0]:
                    newest = (r["ts"], law)
    return newest[1] if newest else None


def flaky_panel(d: Path, law: str | None = None) -> str:
    """From ≥2 graded runs ON ONE LAW GENERATION: which asserts fail in
    EVERY run (persistent — a real defect or a grader bug) vs SOME runs
    (flaky — nondeterminism to keep under control). Runs from older law
    generations are excluded from counting: a law fix changes the
    population, and pre-fix runs must not vote on post-fix stability."""
    runs = history(d)
    if law:
        excluded = sum(1 for r in runs if r.get("law") != law)
        runs = [r for r in runs if r.get("law") == law]
    else:
        # no stamped run anywhere yet: every existing entry predates
        # generation tracking — its generation is unknowable, so it cannot
        # vote on stability. Show the count, count nothing.
        excluded = len(runs)
        runs = []
    if len(runs) < 2:
        base = '<div class="dim" style="margin-top:4px">flaky analysis: '
        base += f"{len(runs)} run(s) on this law generation"
        if excluded:
            base += f" ({excluded} pre-tracking/older-law runs excluded)"
        base += " — need ≥2</div>"
        return base
    counts: dict[str, int] = {}
    for r in runs:
        for name in r.get("fails", []):
            counts[name] = counts.get(name, 0) + 1
    # Counted separately, never mixed into the flaky tally: an assert whose
    # artifact was absent did not fail at what it measures — it measured
    # nothing, and a repeat of THAT is a different defect (BL-062).
    unmeasured: dict[str, int] = {}
    for r in runs:
        for name in r.get("unmeasured_asserts", []):
            unmeasured[name] = unmeasured.get(name, 0) + 1
    n = len(runs)
    persistent = [(k, v) for k, v in counts.items() if v == n]
    flaky = [(k, v) for k, v in counts.items() if 0 < v < n]
    rows = []
    for k, v in sorted(unmeasured.items(), key=lambda x: -x[1])[:3]:
        rows.append(f'<div class="flaky persist">unmeasured ({v}/{n}): {esc(k[:70])}</div>')
    for k, v in sorted(persistent, key=lambda x: -x[1])[:3]:
        rows.append(f'<div class="flaky persist">persistent ({v}/{n}): {esc(k[:70])}</div>')
    for k, v in sorted(flaky, key=lambda x: -x[1])[:4]:
        rows.append(f'<div class="flaky">flaky ({v}/{n}): {esc(k[:70])}</div>')
    if not rows:
        return (f'<div class="dim" style="margin-top:4px">grade history: {n} runs'
                + (f' + {excluded} older-law excluded' if excluded else '')
                + ', no repeats</div>')
    return (f'<div class="dim" style="margin-top:4px">grade history: {n} runs on this law'
            + (f' ({excluded} older-law excluded)' if excluded else '')
            + f' — {esc(law or "unstamped")}</div>'
            + "".join(rows))


def render(ws: Path, timeline_log: Path) -> str:
    now = datetime.now(timezone.utc)
    events = parse_timeline(timeline_log)
    run_cur, run_history = load_runs(ws)
    run_id = run_cur.get("run_id")
    runner = run_cur.get("runner", "opencode")
    model = run_cur.get("model", "?")
    alive = count_runner(runner, ws)
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
        if q_state in ("done", "failed", "queued", "partial"):
            state = "pending" if q_state == "queued" else q_state
            # queue.json records EXECUTION, grading.json records the VERDICT.
            # Where a grade exists it decides between done and partial — in
            # both directions. The check used to run one way only (done →
            # partial), so a scenario re-graded clean after a grader fix kept
            # showing "w/ errors" against a 100% bar (found 2026-08-22).
            if state in ("done", "partial"):
                gr = grading(ws / sc)
                if gr is not None:
                    return "partial" if gr["summary"]["failed"] > 0 else "done"
            return state
        return state_of(events[sc])[0]

    total_started = sum(1 for sc in SCENARIOS if events[sc] or sc in q_statuses)
    states = {sc: final_state(sc) for sc in SCENARIOS}
    done = [sc for sc in SCENARIOS if states[sc] == "done"]
    failed = [sc for sc in SCENARIOS if states[sc] == "failed"]
    running = [sc for sc in SCENARIOS if states[sc] in ("running", "retrying")]
    partial = [sc for sc in SCENARIOS if states[sc] == "partial"]

    cards = []
    law = current_law(ws)
    for sc in SCENARIOS:
        d = ws / sc
        log = d / "outputs" / "run.log"
        repo = d / "repo"
        # effective state FIRST (queue-merged) — the grade block and every
        # hint below must classify against the final state, not the raw one
        # ONE state oracle: final_state() above. This block used to re-derive
        # it with slightly different rules — and its `partial` branch never
        # consulted the grade, so a scenario re-graded clean kept its
        # "w/ errors" badge next to a 100% bar while the header counter
        # (which does use final_state) already said done (found 2026-08-22).
        # Two implementations of one decision is one too many; only the
        # human-facing detail line is derived here.
        q_state = q_statuses.get(sc)
        state = final_state(sc)
        if state == "running":
            detail = "orchestrator chain: active"
        elif state == "partial":
            detail = "completed — graded with failures"
        elif state == "pending" and q_state == "queued":
            pos = q_order.index(sc) + 1 if sc in q_order else "?"
            detail = f"queued (#{pos} in chain)"
        elif q_state in ("done", "failed", "partial"):
            detail = "queue: " + q_state
        else:
            detail = state_of(events[sc])[1]
        expected = EXPECTED[sc]
        artifacts = [(Path(d) / p, resolve_expected(Path(d), p))
                     for p in expected]
        # Deliberately colour-free: these name WHERE the deliverable lands,
        # they are not a verdict. Colouring them by mere existence read as a
        # green light — a scaffolded README.md that ships with the fixture lit
        # up before the agent had done anything, and a report file lit up the
        # moment it was created, mid-run, saying nothing about its contents.
        # The verdict lives in the state badge and the grade bar.
        art_html = "".join(
            f'<span class="tag">{esc(basename(p2))}</span>'
            for p, p2 in artifacts)
        size = log.stat().st_size if log.exists() else 0
        # Liveness oracle, per runner profile: the claude profile renders
        # run.log once per turn (bursty — a long tool call or a long think
        # reads as frozen), while run.jsonl grows on every partial message.
        # Measure the freshest of the two: a stall is silence in BOTH.
        stamps = [f.stat().st_mtime
                  for f in (log, d / "outputs" / "run.jsonl") if f.exists()]
        age = (time.time() - max(stamps)) if stamps else None
        raw_dirty, dirty = git_dirty(repo) if repo.exists() else (0, 0)
        gr = grading(d)
        grade_html = ""
        if gr and run_id and gr.get("run_id") not in (None, run_id) and state in ("pending",):
            sm = gr["summary"]
            grade_html = (f'<div class="dim prevgrade">run {esc(str(gr.get("run_id"))[:11])}:'
                          f' {sm["passed"]}/{sm["total"]} — from an earlier run</div>')
        elif gr:
            sm = gr["summary"]
            rate = int(sm["pass_rate"] * 100)
            # POLICY §1b rule 2. `.get` because grading.json files written
            # before BL-062 carry neither key; they degrade to "everything
            # was measured", which is what they meant at the time.
            meas, unm = sm.get("measured", sm["total"]), sm.get("unmeasured", 0)
            stamp = (gr.get("ts") or "")[11:16]
            if state in ("running", "pending", "stalled", "retrying"):
                # a grade from a previous run must not pose as current
                grade_html = (f'<div class="dim prevgrade">prev run: {meas}/{sm["total"]}'
                              f' measured, {sm["passed"]} passed ({rate}%)'
                              f' — stale while {state}</div>')
            elif state == "failed" and not (d / "outputs" / "run.log").exists():
                grade_html = ""
            else:
                # terminal states: the orchestrator grades BEFORE flipping the
                # queue to done, so a grade here is this run's grade
                cls = ("gok" if not (sm["failed"] or unm)
                       else ("gsome" if rate >= 80 and not unm else "gbad"))
                # BL-042 item 2: a verdict from another generation must say
                # so. grading.json is overwritten in place, so without the
                # stamp a re-grade under a different law or grader is
                # indistinguishable from this run's own verdict.
                gl = gr.get("law")
                if gl and law and gl != law:
                    stamp_html = (f'<div class="err">verdict stamped {esc(gl)} —'
                                  f' not this generation ({esc(law)}); read'
                                  f' grade-history.jsonl</div>')
                elif not gl:
                    stamp_html = ('<div class="dim">verdict unstamped —'
                                  ' graded before BL-042</div>')
                else:
                    stamp_html = ""
                # Unmeasured first: a failed assert names a defect in the
                # thing it measures, an unmeasured one says the measurement
                # never happened — and the second is the more urgent read.
                bad = sorted((e for e in gr["expectations"] if not e["passed"]),
                             key=lambda e: e.get("verdict") != "unmeasured")
                fail_rows = "".join(
                    f'<div class="gfail">{"?" if e.get("verdict") == "unmeasured" else "✗"}'
                    f' {esc(e["text"])} — {esc(e["evidence"][:120])}</div>'
                    for e in bad[:5])
                more = f'<div class="dim">… +{len(bad) - 5} more</div>' if len(bad) > 5 else ""
                unm_html = (f'<div class="err">{unm} assert(s) UNMEASURED —'
                            f' the artifact was absent or empty</div>' if unm else "")
                # No percentage while anything is unmeasured: a rate taken
                # over five measured asserts out of forty-four reads like
                # progress, which is the exact sentence POLICY §1b forbids.
                rate_html = "" if unm else f' ({rate}%)'
                grade_html = (f'<div class="grade {cls}">graded: {meas}/{sm["total"]} measured,'
                              f' {sm["passed"]} passed{rate_html} · {stamp}</div>'
                              f'{unm_html}{stamp_html}{fail_rows}{more}')
        elif state == "done":
            grade_html = '<div class="dim">grading pending…</div>'
        idem_block = idem_html(events[sc], d)
        flaky_html = flaky_panel(d, law) if state in ("done", "partial", "failed") else ""
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
  {stall_hint}{grade_html}{idem_block}{flaky_html}{err_html}
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

    # run history: every run (run.json) x every scenario's graded runs
    # (grade-history.jsonl carries run ids since the relocation)
    hist_rows = []
    for r in reversed(run_history):
        rid = r.get("run_id", "?")
        cur = " · current" if rid == run_id else ""
        hist_rows.append(
            f'<div class="runrow"><b>{esc(rid)}</b> — {esc(r.get("runner","opencode"))}'
            f' / {esc(r.get("model","?"))}, law {esc(r.get("law_commit","?"))}{cur}</div>')
        for sc in SCENARIOS:
            entries = [h for h in history(ws / sc) if h.get("run_id") == rid]
            if entries:
                e = entries[-1]
                e_unm = e.get("unmeasured", 0)
                ok = not (e["failed"] or e_unm)
                note = "; ".join(f[:40] for f in e["fails"][:3])
                if e_unm:
                    note = f"{e_unm} unmeasured" + (f"; {note}" if note else "")
                hist_rows.append(
                    f'<div class="runsc{" rokken" if ok else " rbad"}">{esc(DISPLAY.get(sc, sc))}:'
                    f' {e.get("measured", e["total"])}/{e["total"]} measured,'
                    f' {e["passed"]} passed'
                    + ("" if ok else f' — {esc(note)}')
                    + '</div>')
    runs_html = "\n".join(hist_rows) or "(no runs recorded yet)"

    tl_tail = esc("\n".join(timeline_log.read_text(errors="ignore")
                            .splitlines()[-8:])) if timeline_log.exists() else "(no orchestrator log)"
    tl_full_lines = (strip_ansi(timeline_log.read_text(errors="ignore"))[-131072:].splitlines()
                     if timeline_log.exists() else ["(no orchestrator log)"])
    tl_full = esc("\n".join(reversed(tl_full_lines)))
    return f"""<!doctype html><html lang="en"><head><meta charset="utf-8">
<title>legislator eval — live</title>
<script>
let paused = false;
let copyPauseUntil = 0;
function openLog(id) {{ paused = true;
  document.getElementById("pausetag").style.display = "inline";
  document.getElementById("m-" + id).style.display = "flex"; }}
function closeLog(ev) {{ if (ev.target === ev.currentTarget) closeLogX(); }}
function closeLogX() {{
  document.querySelectorAll(".mback").forEach(m => m.style.display = "none");
  paused = false;
  document.getElementById("pausetag").style.display = "none"; }}
function openRuns() {{ paused = true;
  document.getElementById("pausetag").style.display = "inline";
  document.getElementById("m-runs").style.display = "flex"; }}
document.addEventListener("keydown", e => {{ if (e.key === "Escape") closeLogX(); }});
// copy protection: an active text selection (or a copy event just fired)
// pauses the refresh so a 3s reload cannot wipe the selection mid-copy.
// Any click clears the selection and the pause lifts; the copy-event grace
// is bounded (10s) so a forgotten selection cannot freeze the page forever.
document.addEventListener("copy", () => {{
  copyPauseUntil = Date.now() + 10000;
  document.getElementById("pausetag").style.display = "inline"; }});
document.addEventListener("selectionchange", () => {{
  if (document.getSelection().toString().length > 0) {{
    document.getElementById("pausetag").style.display = "inline";
  }} else if (!paused && Date.now() >= copyPauseUntil) {{
    document.getElementById("pausetag").style.display = "none"; }} }});
setInterval(() => {{
  const selecting = document.getSelection().toString().length > 0;
  const copyGrace = Date.now() < copyPauseUntil;
  if (!paused && !selecting && !copyGrace) location.reload(); }}, 3000);
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
      border-radius:4px;background:#2b2f36;color:#9db4d0;
      border:1px solid #3a4350}}
 .err{{color:#f66;margin-top:4px;white-space:nowrap;overflow:hidden;
      text-overflow:ellipsis}}
 .warn{{color:#fb0;margin-top:4px}} .dim{{color:#888}}
 .tailopen{{cursor:pointer}} .tailopen:hover{{outline:1px solid #3a6ea5}}
 .orchbox{{border:1px solid #333;border-radius:8px;background:#181818;
          padding:8px;margin-top:6px}}
 .orchbox pre{{margin:0;white-space:pre-wrap;color:#aaa}}
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
 .runbadge{{background:#1c3a5e;color:#6bf;border:0;border-radius:4px;
           padding:0 8px;cursor:pointer;font:inherit}}
 .runrow{{margin:6px 0 2px}} .runsc{{padding-left:14px;color:#aaa}}
 .runsc.rokken{{color:#8f8}} .runsc.rbad{{color:#f88}}
 .flaky{{color:#e9a23b;font-size:12px;margin-top:2px}}
 .flaky.persist{{color:#f66}}
 .gfail{{color:#f88;margin-top:3px;font-size:12px;white-space:nowrap;overflow:hidden;
        text-overflow:ellipsis}}
 pre{{white-space:pre-wrap;background:#0c0c0c;border-radius:6px;
     padding:6px;margin:8px 0 0;color:#aaa;max-height:120px;overflow:hidden}}
 .summary span{{margin-right:14px}}
</style></head><body>
<h1>legislator eval — live</h1>
<div class="dim">run <button class="runbadge" onclick="openRuns()">{esc(run_id or "—")}</button> ·
 generated {now.strftime("%Y-%m-%d %H:%M:%S")} UTC ·
 refresh 3s <span id="pausetag" class="warn" style="display:none">— PAUSED (log open)</span> · {esc(runner)} agents alive: {alive}</div>
<div class="dim" style="margin-top:2px">profile <b>{esc(runner)}</b> · model <b>{esc(model)}</b> · law {esc(run_cur.get("law_commit", "?"))}</div>
<div class="summary" style="margin-top:8px">
 <span>scenarios: {total_started}/{len(SCENARIOS)} started</span>
 <span style="color:#6bf">running: {len(running)}</span>
 <span style="color:#8f8">done: {len(done)}</span>
 <span style="color:#fc6">w/ errors: {len(partial)}</span>
 <span style="color:#f88">failed: {len(failed)}</span>
</div>
<div class="grid">{''.join(cards)}</div>
<h2 style="font-size:13px;margin-top:16px">orchestrator tail
  <button class="logbtn" onclick="openLog('orchestrator')">log \u29e2</button></h2>
<div class="orchbox">
  <pre class="tailopen" onclick="openLog('orchestrator')">{tl_tail}</pre>
</div>
<div class="mback" id="m-runs" onclick="closeLog(event)">
  <div class="mwin" onclick="event.stopPropagation()">
    <div class="mhead"><span>run history</span>
      <button onclick="closeLogX()">close \u00d7</button></div>
    <pre class="mlog">{runs_html}</pre>
  </div>
</div>
<div class="mback" id="m-orchestrator" onclick="closeLog(event)">
  <div class="mwin" onclick="event.stopPropagation()">
    <div class="mhead"><span>orchestrator — full log (newest first)</span>
      <button onclick="closeLogX()">close \u00d7</button></div>
    <pre class="mlog">{tl_full}</pre>
  </div>
</div>
</body></html>"""


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("workspace", type=Path)
    ap.add_argument("--interval", type=float, default=3.0)
    ap.add_argument("--once", action="store_true")
    ap.add_argument("--open", action="store_true",
                    help="open the dashboard in the default browser after the "
                         "first render (skip under NO_BROWSER/KBO_EVALS_NO_BROWSER)")
    ap.add_argument("--timeline", type=Path,
                    default=None,
                    help="orchestrator log (default: <ws>/orchestrate.log, "
                         "falling back to /tmp/opencode/orchestrate-all.log)")
    a = ap.parse_args()
    timeline = a.timeline or a.workspace / "orchestrate.log"
    if not timeline.exists():
        timeline = Path("/tmp/opencode/orchestrate-all.log")
    out = a.workspace / "dashboard.html"
    first = True
    while True:
        out.write_text(render(a.workspace, timeline))
        if first:
            first = False
            print(out)
            if a.open and not os.environ.get("NO_BROWSER") and not os.environ.get("KBO_EVALS_NO_BROWSER"):
                import webbrowser
                webbrowser.open(out.as_uri())
        if a.once:
            return
        time.sleep(a.interval)


if __name__ == "__main__":
    main()
