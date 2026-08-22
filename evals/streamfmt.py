#!/usr/bin/env python3
"""stream-json → readable transcript (the claude runner profile).

Claude Code's headless mode streams JSONL events rather than prose, and two
consumers want different things from that stream: the orchestrator's stall
detector wants a file that keeps growing while the agent works, and the
dashboard's log modal wants something a human can read. Serving both from one
file fails at both, so this filter splits them — raw events are appended to
<raw-path> (the stall oracle, grows on every partial message), a rendered
transcript goes to stdout, which the runner appends to run.log (the dashboard
oracle, one line per turn or tool call).

Line-buffered on purpose: a buffered pipe would read as a stall.
"""
import json
import sys

MAX_ARG = 160
MAX_RESULT = 400


def emit(s: str) -> None:
    sys.stdout.write(s.rstrip("\n") + "\n")
    sys.stdout.flush()


def brief(inp: dict) -> str:
    """One-line rendering of a tool input — the fields a reader scans for."""
    for key in ("command", "file_path", "path", "pattern", "url"):
        if key in inp:
            return str(inp[key])[:MAX_ARG]
    return json.dumps(inp)[:MAX_ARG] if inp else ""


def result_text(block: dict) -> str:
    content = block.get("content")
    if isinstance(content, list):
        content = " ".join(c.get("text", "") for c in content
                           if isinstance(c, dict))
    text = str(content or "").strip().replace("\n", " ⏎ ")
    tag = "ERR" if block.get("is_error") else "ok"
    return f"  [{tag}] {text[:MAX_RESULT]}"


def main() -> None:
    raw = open(sys.argv[1], "a", buffering=1) if len(sys.argv) > 1 else None
    for line in sys.stdin:
        if raw:
            raw.write(line)
        line = line.strip()
        if not line:
            continue
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            emit(line)          # not our stream — pass it through verbatim
            continue
        kind = ev.get("type")
        if kind == "system" and ev.get("subtype") == "init":
            emit(f"[session {ev.get('session_id', '?')}] "
                 f"model={ev.get('model', '?')} cwd={ev.get('cwd', '?')}")
        elif kind == "assistant":
            for b in ev.get("message", {}).get("content", []):
                if b.get("type") == "text" and b.get("text", "").strip():
                    emit(b["text"])
                elif b.get("type") == "tool_use":
                    emit(f"$ {b.get('name')} {brief(b.get('input') or {})}")
        elif kind == "user":
            for b in ev.get("message", {}).get("content", []):
                if b.get("type") == "tool_result":
                    emit(result_text(b))
        elif kind == "result":
            emit(f"[result] {ev.get('subtype', '?')} "
                 f"turns={ev.get('num_turns', '?')} "
                 f"duration={ev.get('duration_ms', '?')}ms "
                 f"cost_usd={ev.get('total_cost_usd', '?')}")


if __name__ == "__main__":
    main()
