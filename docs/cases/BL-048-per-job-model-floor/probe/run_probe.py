#!/usr/bin/env python3
"""BL-048 probe runner: identical prompts to every candidate model,
answers parsed to a forced binary (job A) / term list (job B), accuracy
and latency recorded. Ollama models via the local HTTP API; haiku via the
Claude Code CLI. Deterministic where the backend allows (temperature 0)."""
from __future__ import annotations

import json
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

HERE = Path(__file__).parent
DATA = json.loads((HERE / "dataset.json").read_text())

PROMPT_A = """You audit a project document for "constitution candidates" — statements to promote into fleet-wide law shared by many repositories of the same stack. A statement qualifies ONLY if ALL three tests pass:
1. law-shaped: imperative and checkable against a code diff ("always...", "never...", "must..."), not a description or narration;
2. not already covered by the existing fleet law, which already regulates: tests/verification discipline, dev-journal entries at task boundaries, changelog upkeep, case/spec workflow, git conduct (branching, merging, no AI attribution), OKF documentation sync. A statement CONTRADICTING one of those is covered by it and is never a candidate;
3. generalizable: it would make sense verbatim in another repository of the same stack. A concrete project path, this project's own branch pattern, a named project service or table, an environment/file detail, or a domain fact about this business is project-instance data — never a candidate.
Additionally: if the line immediately before the statement is the marker <!-- legislator: not-law -->, the statement is excluded regardless of the tests.

Preceding line: {prev}
Statement: {text}

Answer with exactly one word: CANDIDATE or NOT."""

PROMPT_B = """From the following project notes, extract the domain terms that belong in a domain glossary (the project's own vocabulary — entities and processes of its business domain). List ONLY the terms, one per line, lowercase, nothing else.

{text}"""


def ask_ollama(model: str, prompt: str) -> tuple[str, float]:
    body = json.dumps({"model": model, "prompt": prompt, "stream": False,
                       "options": {"temperature": 0}}).encode()
    t0 = time.time()
    req = urllib.request.Request("http://localhost:11434/api/generate",
                                 data=body,
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=300) as r:
        out = json.loads(r.read())["response"]
    return out, time.time() - t0


def ask_haiku(prompt: str) -> tuple[str, float]:
    t0 = time.time()
    r = subprocess.run(["claude", "-p", "--model", "haiku", "--safe-mode", prompt],
                       capture_output=True, text=True, timeout=300)
    return r.stdout, time.time() - t0


def ask(model: str, prompt: str) -> tuple[str, float]:
    if model == "haiku":
        return ask_haiku(prompt)
    return ask_ollama(model, prompt)


def parse_binary(out: str) -> str:
    up = out.upper()
    has_c = "CANDIDATE" in up
    # "NOT" alone is too common a word; look for it as a standalone token
    import re
    has_n = bool(re.search(r"\bNOT\b", up))
    if has_c and not has_n:
        return "CANDIDATE"
    if has_n and not has_c:
        return "NOT"
    # both or neither: take the first occurrence
    ic = up.find("CANDIDATE")
    im = up.find("NOT")
    if ic == -1 and im == -1:
        return "UNPARSED"
    if ic == -1:
        return "NOT"
    if im == -1:
        return "CANDIDATE"
    return "CANDIDATE" if ic < im else "NOT"


def main() -> int:
    models = sys.argv[1:] or ["qwen2.5:3b", "llama3.2:3b", "haiku"]
    results: dict = {}
    for model in models:
        rows = []
        for item in DATA["job_a"]:
            prompt = PROMPT_A.format(prev=item["prev"] or "(none)",
                                     text=item["text"])
            out, dt = ask(model, prompt)
            got = parse_binary(out)
            rows.append({"id": item["id"], "label": item["label"],
                         "got": got, "ok": got == item["label"],
                         "secs": round(dt, 1),
                         "raw": out.strip()[:120]})
            print(f"{model} A#{item['id']:02d} label={item['label']:9s} "
                  f"got={got:9s} {'ok' if got == item['label'] else 'MISS'} "
                  f"({dt:.1f}s)", flush=True)
        out, dt = ask(model, PROMPT_B.format(text=DATA["job_b"]["text"]))
        terms = [l.strip().lstrip("-* ").lower() for l in out.splitlines()
                 if l.strip() and len(l.strip()) < 60]
        expected = set(DATA["job_b"]["expected_terms"])
        found = {t for t in expected if any(t in line for line in terms)}
        rows_b = {"terms_returned": terms, "expected": sorted(expected),
                  "recall": len(found) / len(expected),
                  "extras": [t for t in terms
                             if not any(e in t for e in expected)],
                  "secs": round(dt, 1)}
        print(f"{model} B recall={rows_b['recall']:.0%} "
              f"extras={len(rows_b['extras'])} ({dt:.1f}s)", flush=True)
        acc = sum(r["ok"] for r in rows) / len(rows)
        results[model] = {"job_a": rows, "job_a_accuracy": acc,
                          "job_b": rows_b}
        print(f"== {model}: job A {sum(r['ok'] for r in rows)}/{len(rows)} "
              f"({acc:.0%})", flush=True)
    merged = {}
    if (HERE / "results.json").exists():
        merged = json.loads((HERE / "results.json").read_text())
    merged.update(results)
    (HERE / "results.json").write_text(json.dumps(merged, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
