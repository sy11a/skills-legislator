# OKF v2 and the anchor engine — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the knowledge stratum a mechanical bond to the code it
describes — an owned, read-only engine that verifies every path and symbol an
OKF document names, with the law that defines an anchor and the ladder rung
that requires the check.

**Architecture:** One new owned artifact (`skill/assets/engine/engine.py` →
`docs/ai/engine.py`), delivered byte-for-byte by SKILL.md Step 3 exactly like
`opencode.json`. Two read-only jobs: `anchors` (every path/symbol resolves)
and `okf-debt` (an anchored document whose sources moved on without it). The
anchor definition is law in `core/okf.md`; `core/verification.md` carries the
rung; audit checks 15 and 17 run the two jobs; restructure routes both
findings to `## For the team:` because they are owner prose.

**Tech Stack:** Python 3 standard library only (the engine and every eval
harness); Markdown law files; `git` for the debt job's dates.

**Spec:** `docs/superpowers/specs/2026-08-23-okf-v2-anchors-design.md` — read
it before Task 1. Every decision below argues from it.

## Global Constraints

- **VERSION 19 → 20**, bumped once, in Task 8 — `skill/assets/rules/**`
  content changes this cycle (README rule).
- **Every new assert is committed RED before the change that greens it**
  (`evals/POLICY.md`). Tasks below name the red commit explicitly. An assert
  that was never seen red is measuring nothing.
- **`python3 evals/check_static.py` must pass at the end of every task.**
- **No AI co-author trailers in any commit** (CLAUDE.md).
- **No fleet repository names and no absolute local paths in tracked files.**
  Example paths in code, tests and law use neutral names (`src/App/…`,
  `LegacyBilling` is the eval fixture's own name and is fine).
- **Owned files are copied with Bash `cp`, never Write/Edit** — that is the
  law the skill itself states; the engine is now one of them.
- The engine writes nothing this edition. Any step that makes it write is out
  of scope (that is BL-043).

---

## File Structure

| File | Responsibility |
|---|---|
| `skill/assets/engine/engine.py` (create) | The engine. Two jobs, stdlib only, read-only. The only implementation of the anchor definition. |
| `evals/check_engine.py` (create) | Deterministic unit tests of the engine against temp repositories. No agent. |
| `skill/assets/rules/core/okf.md` (modify) | OKF v2: the three link-hardness classes and the closed anchor definition. |
| `skill/assets/rules/core/verification.md` (modify) | The static rung. |
| `skill/SKILL.md` (modify) | Step 3 delivery, File authority row, audit check 3, new checks 15 and 17, restructure routing. |
| `evals/check_static.py` (modify) | Wall: engine present in assets, named in the authority row. |
| `evals/grade.py` (modify) | `expected_owned()` gains the engine; audit/restructure asserts for the two new slugs. |
| `evals/setup_workspace.py` (modify) | Rotted fixture: engine present + three planted defects; `check_slugs_covered` and `report_markers`. |
| `plugin/hooks/guard_owned_files.py` (modify) | Write-guard covers `docs/ai/engine.py`. |
| `evals/check_hooks.py` (modify) | Guard cases for the engine path. |
| `docs/ontology.md`, `docs/glossary.md`, `docs/philosophy.md`, `docs/backlog.md`, `skill/VERSION` (modify) | Bookkeeping, Task 8. |
| `evals/benchmarks/v20.md` (create) | The edition's benchmark record, Task 9. |

---

## Task 1: The engine — the `anchors` job

**Files:**
- Create: `skill/assets/engine/engine.py`
- Create: `evals/check_engine.py`

**Interfaces:**
- Consumes: nothing.
- Produces: `python3 docs/ai/engine.py anchors` — prints one finding per
  line, exit `0` clean / `1` findings / `2` usage. Finding shapes:
  `<doc>:<line>: path-anchor: <token> → no such file` and
  `<doc>:<line>: symbol-anchor: <token> → not found in <roots>`.
  Task 2 adds `okf-debt` to the same `JOBS` dict.

- [ ] **Step 1: Write the failing test harness**

Create `evals/check_engine.py`. It builds a throwaway repository in
`tempfile.mkdtemp()`, copies the engine to `docs/ai/engine.py` (the real
deployment shape — the engine derives the repo root from its own location),
and runs it as a subprocess.

```python
#!/usr/bin/env python3
"""Unit tests for the constitution's engine — no agent, seconds to run.

The engine derives the repository root from its own location
(<repo>/docs/ai/engine.py), so every case materializes that exact shape in a
temp directory. Usage: python3 evals/check_engine.py
"""
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
ENGINE_SRC = REPO / "skill" / "assets" / "engine" / "engine.py"

failures: list[str] = []


def check(ok: bool, label: str, detail: str = "") -> None:
    if ok:
        print(f"  ok    {label}")
    else:
        print(f"  FAIL  {label}" + (f" — {detail}" if detail else ""))
        failures.append(label)


def make_repo(docs: dict[str, str], sources: dict[str, str]) -> Path:
    """A repo with docs/okf/<name> files and source files, engine in place."""
    root = Path(tempfile.mkdtemp(prefix="engine-eval-"))
    (root / "docs" / "ai").mkdir(parents=True)
    shutil.copy2(ENGINE_SRC, root / "docs" / "ai" / "engine.py")
    (root / "docs" / "okf").mkdir(parents=True)
    for name, text in docs.items():
        (root / "docs" / "okf" / name).write_text(text)
    for rel, text in sources.items():
        p = root / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text)
    return root


def run(root: Path, job: str) -> tuple[int, str]:
    r = subprocess.run([sys.executable, "docs/ai/engine.py", job],
                       cwd=root, capture_output=True, text=True)
    return r.returncode, r.stdout


def git(root: Path, *args: str, date: str | None = None) -> None:
    env = {**os.environ}
    if date:
        env["GIT_AUTHOR_DATE"] = env["GIT_COMMITTER_DATE"] = date
    subprocess.run(["git", "-c", "user.email=eval@local", "-c",
                    "user.name=eval", *args], cwd=root, check=True,
                   capture_output=True, env=env)
```

- [ ] **Step 2: Write the anchors test cases**

Append to `evals/check_engine.py`:

```python
print("== anchors: resolving anchors are silent ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nSee `src/App/WidgetStore.cs` and `WidgetStore`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 0 and out == "", "a healthy document produces no findings", f"exit={code} out={out!r}")

print("== anchors: a path that does not exist is a finding ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nSee `src/App/Gone.cs`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 1 and "widgets.md:3: path-anchor: src/App/Gone.cs → no such file" in out,
      "missing path reported with document, line and token", f"exit={code} out={out!r}")

print("== anchors: a symbol absent from the source is a finding ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nHandled by `LegacyProcessor`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 1 and "widgets.md:3: symbol-anchor: LegacyProcessor" in out,
      "missing symbol reported", f"exit={code} out={out!r}")

print("== anchors: the closed definition excludes non-anchors ==")
root = make_repo(
    {"widgets.md": (
        "# Widgets\n\n"
        "Template `schemas/<type>/<version>.json`, glob `src/**/*.cs`, "
        "home `~/.config/app/settings.yaml`, absolute `/etc/hosts`, "
        "command `dotnet build`, field `contenthash`, short `Api`.\n")},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 0 and out == "",
      "templates, globs, ~ and / paths, commands, lowercase and 3-char tokens are not anchors",
      f"exit={code} out={out!r}")

print("== anchors: a member suffix resolves to its file ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\n`src/App/WidgetStore.Flush()` writes.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { void Flush() {} }\n"})
code, out = run(root, "anchors")
check(code == 0 and out == "", "a trailing .Member() is stripped before the file test",
      f"exit={code} out={out!r}")

print("== anchors: human-class documents are exempt ==")
root = make_repo(
    {"log.md": "# Log\n\n2026-01: removed `RetiredJob` and `src/App/Retired.cs`.\n",
     "glossary.md": "# Glossary\n\n| `RetiredJob` | gone |\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 0 and out == "", "log.md and glossary.md are never anchored",
      f"exit={code} out={out!r}")

print("== anchors: front matter and fenced blocks are not scanned ==")
root = make_repo(
    {"widgets.md": (
        "---\ntitle: uses `src/App/Gone.cs` in its description\n---\n\n"
        "# Widgets\n\n```\n`src/App/AlsoGone.cs`\n```\n")},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 0 and out == "", "front matter and fenced code are skipped",
      f"exit={code} out={out!r}")

print("== anchors: docs/ is not a source root ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nHandled by `OnlyInDocs`.\n",
     "other.md": "# Other\n\nThe class `OnlyInDocs` is described here.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check(code == 1 and "symbol-anchor: OnlyInDocs" in out,
      "a symbol that exists only in prose does not resolve", f"exit={code} out={out!r}")

print("== usage ==")
root = make_repo({"widgets.md": "# Widgets\n"}, {"src/App/A.cs": "class A {}\n"})
r = subprocess.run([sys.executable, "docs/ai/engine.py", "nonsense"],
                   cwd=root, capture_output=True, text=True)
check(r.returncode == 2, "an unknown job exits 2", f"exit={r.returncode}")
r = subprocess.run([sys.executable, "docs/ai/engine.py"],
                   cwd=root, capture_output=True, text=True)
check(r.returncode == 2, "no job exits 2", f"exit={r.returncode}")

print("== no OKF bundle ==")
root = Path(tempfile.mkdtemp(prefix="engine-eval-"))
(root / "docs" / "ai").mkdir(parents=True)
shutil.copy2(ENGINE_SRC, root / "docs" / "ai" / "engine.py")
code, out = run(root, "anchors")
check(code == 0 and out == "", "a repo with no docs/okf/ is clean, not an error",
      f"exit={code} out={out!r}")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall engine checks passed")
```

- [ ] **Step 3: Run it to verify it fails**

Run: `python3 evals/check_engine.py`
Expected: an exception or every case FAIL — `skill/assets/engine/engine.py`
does not exist yet.

- [ ] **Step 4: Commit the red tests**

```bash
git add evals/check_engine.py
git commit -m "BL-033 evals: engine unit tests, red — no engine exists yet

Eleven cases pin the closed anchor definition before a line of engine
code exists: resolving anchors silent, a missing path and a missing
symbol reported with document/line/token, the exclusion set (templates,
globs, ~ and absolute paths, commands, lowercase, 3-char), the
.Member() strip, the human-class exemption, front matter and fenced
blocks skipped, docs/ excluded from source roots, the three exit codes,
and an OKF-less repo reading clean rather than erroring."
```

- [ ] **Step 5: Write the engine**

Create `skill/assets/engine/engine.py`:

```python
#!/usr/bin/env python3
"""The constitution's static engine — read-only checks that bond documents to code.

Delivered by the Legislator as an owned file (`docs/ai/engine.py`). Never
hand-edit it: change the skill source and re-run /legislator.

Jobs (both read-only — this engine writes nothing):
  anchors    every path or symbol an anchored OKF document backticks resolves
  okf-debt   anchored documents whose sources moved on without them

Usage: python3 docs/ai/engine.py <job>
Exit:  0 clean, 1 findings printed to stdout, 2 usage error.

The law this executes is `docs/ai/rules/core/okf.md` (link hardness and the
closed anchor definition); the rung that requires it is
`docs/ai/rules/core/verification.md`.
"""
from __future__ import annotations

import re
import subprocess
import sys
from datetime import datetime
from pathlib import Path

# The engine ships at <repo>/docs/ai/engine.py — two parents up is the repo.
ROOT = Path(__file__).resolve().parents[2]
OKF = ROOT / "docs" / "okf"

HUMAN_CLASS = {"glossary.md", "log.md"}   # core/okf.md's human class
IGNORED_DIRS = {"docs", "bin", "obj", "node_modules", "dist"}
SOURCE_EXTS = (".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs",
               ".java", ".kt", ".rb", ".php", ".sql", ".html", ".css")
DEBT_DAYS = 30            # audit check 8's threshold, reused, never restated
MAX_BYTES = 2_000_000     # a file bigger than this is not prose or source

TOKEN = re.compile(r"`([^`\n]+)`")
PASCAL = re.compile(r"[A-Z][A-Za-z0-9]{3,}(\.[A-Z][A-Za-z0-9]+)*$")
FORBIDDEN = set(" <>*?")


def top_level_dirs() -> set[str]:
    return {p.name for p in ROOT.iterdir() if p.is_dir() and not p.name.startswith(".")}


def source_roots() -> list[Path]:
    return sorted(p for p in ROOT.iterdir()
                  if p.is_dir() and not p.name.startswith(".")
                  and p.name not in IGNORED_DIRS)


def anchored_docs() -> list[Path]:
    if not OKF.is_dir():
        return []
    return sorted(p for p in OKF.rglob("*.md") if p.name not in HUMAN_CLASS)


def scannable_lines(text: str):
    """Yield (lineno, line) outside front matter and fenced code blocks."""
    lines = text.splitlines()
    i, n = 0, len(lines)
    if lines and lines[0].strip() == "---":
        i = 1
        while i < n and lines[i].strip() != "---":
            i += 1
        i += 1
    fenced = False
    while i < n:
        line = lines[i]
        if line.lstrip().startswith("```"):
            fenced = not fenced
        elif not fenced:
            yield i + 1, line
        i += 1


def classify(token: str, top: set[str]) -> str | None:
    """'path' | 'symbol' | None — the closed definition in core/okf.md."""
    if any(c in token for c in FORBIDDEN):
        return None
    if token.startswith("~") or token.startswith("/"):
        return None
    if "/" in token:
        return "path" if token.split("/", 1)[0] in top else None
    return "symbol" if PASCAL.match(token) else None


def path_target(token: str) -> Path:
    """The path a path-anchor names; a trailing `.Member()` is stripped."""
    base = token.rstrip(":,.")
    if base.endswith("()"):
        stem = base[:-2].rsplit(".", 1)[0]
        for ext in SOURCE_EXTS:
            if (ROOT / (stem + ext)).exists():
                return ROOT / (stem + ext)
    return ROOT / base


def resolve_symbols(symbols: set[str]) -> set[str]:
    """The subset occurring literally under the source roots — one pass."""
    found: set[str] = set()
    if not symbols:
        return found
    for root in source_roots():
        for p in root.rglob("*"):
            if len(found) == len(symbols):
                return found
            if not p.is_file():
                continue
            if any(part.startswith(".") for part in p.relative_to(ROOT).parts):
                continue
            try:
                if p.stat().st_size > MAX_BYTES:
                    continue
                text = p.read_text(errors="ignore")
            except OSError:
                continue
            for s in symbols - found:
                if s.split(".", 1)[0] in text:
                    found.add(s)
    return found


def job_anchors() -> list[str]:
    top = top_level_dirs()
    roots = ", ".join(p.name + "/" for p in source_roots()) or "(no source roots)"
    findings: list[str] = []
    sites: list[tuple[str, int, str]] = []
    symbols: set[str] = set()
    for doc in anchored_docs():
        rel = doc.relative_to(ROOT).as_posix()
        for lineno, line in scannable_lines(doc.read_text(errors="ignore")):
            for m in TOKEN.finditer(line):
                token = m.group(1).strip()
                kind = classify(token, top)
                if kind == "path":
                    if not path_target(token).exists():
                        findings.append(
                            f"{rel}:{lineno}: path-anchor: {token} → no such file")
                elif kind == "symbol":
                    symbols.add(token)
                    sites.append((rel, lineno, token))
    resolved = resolve_symbols(symbols)
    for rel, lineno, token in sites:
        if token not in resolved:
            findings.append(
                f"{rel}:{lineno}: symbol-anchor: {token} → not found in {roots}")
    return sorted(findings)


JOBS = {"anchors": job_anchors}


def main(argv: list[str]) -> int:
    if len(argv) != 2 or argv[1] not in JOBS:
        print(f"usage: python3 {Path(__file__).name} "
              f"{{{'|'.join(sorted(JOBS))}}}", file=sys.stderr)
        return 2
    findings = JOBS[argv[1]]()
    for f in findings:
        print(f)
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `python3 evals/check_engine.py`
Expected: `all engine checks passed`.

- [ ] **Step 7: Run it against a real bundle as a smoke test**

Run, from a legislated repository with an OKF bundle:
`cp skill/assets/engine/engine.py <repo>/docs/ai/engine.py && (cd <repo> && python3 docs/ai/engine.py anchors); rm <repo>/docs/ai/engine.py`
Expected: findings that a human agrees are real, or none. Record the count —
Task 9's benchmark file cites it. Remove the copy afterwards; the pilot is a
post-merge step, not part of this cycle.

- [ ] **Step 8: Commit**

```bash
git add skill/assets/engine/engine.py
git commit -m "BL-033: the anchor engine, first job

The constitution's first executable artifact. One file, stdlib only,
read-only: it scans anchored OKF documents for backticked paths and
PascalCase symbols and reports every one the repository no longer
contains. Repo root is derived from the engine's own delivered location,
so the law that calls it stays repo-relative — no skill path, no machine
install, works in a fresh clone.

Greens evals/check_engine.py (11 cases, red in the previous commit)."
```

---

## Task 2: The engine — the `okf-debt` job

**Files:**
- Modify: `skill/assets/engine/engine.py` (add `job_okf_debt`, extend `JOBS`)
- Modify: `evals/check_engine.py` (append debt cases)

**Interfaces:**
- Consumes: `classify`, `path_target`, `scannable_lines`, `anchored_docs`,
  `TOKEN`, `DEBT_DAYS` from Task 1.
- Produces: `python3 docs/ai/engine.py okf-debt`, findings shaped
  `<doc>: okf-sync-debt: <source path> changed <N> days after this document`.
  One finding per document (the worst offender), not one per anchor.

- [ ] **Step 1: Write the failing tests**

Append to `evals/check_engine.py`, before the `if failures:` block:

```python
print("== okf-debt: a source that moved on is debt ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
git(root, "init", "-q")
git(root, "add", "-A", date="2026-01-01T12:00:00")
git(root, "commit", "-q", "-m", "docs and code", date="2026-01-01T12:00:00")
(root / "src/App/WidgetStore.cs").write_text("public class WidgetStore { void Flush() {} }\n")
git(root, "add", "-A", date="2026-03-01T12:00:00")
git(root, "commit", "-q", "-m", "code moves on", date="2026-03-01T12:00:00")
code, out = run(root, "okf-debt")
check(code == 1 and "widgets.md: okf-sync-debt: src/App/WidgetStore.cs changed 59 days after this document" in out,
      "a source 59 days newer than its document is debt", f"exit={code} out={out!r}")

print("== okf-debt: inside the threshold is not debt ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
git(root, "init", "-q")
git(root, "add", "-A", date="2026-01-01T12:00:00")
git(root, "commit", "-q", "-m", "docs and code", date="2026-01-01T12:00:00")
(root / "src/App/WidgetStore.cs").write_text("public class WidgetStore { void Flush() {} }\n")
git(root, "add", "-A", date="2026-01-20T12:00:00")
git(root, "commit", "-q", "-m", "small change", date="2026-01-20T12:00:00")
code, out = run(root, "okf-debt")
check(code == 0 and out == "", "19 days is inside the 30-day threshold",
      f"exit={code} out={out!r}")

print("== okf-debt: a document updated with its source is clean ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
git(root, "init", "-q")
git(root, "add", "-A", date="2026-01-01T12:00:00")
git(root, "commit", "-q", "-m", "docs and code", date="2026-01-01T12:00:00")
(root / "src/App/WidgetStore.cs").write_text("public class WidgetStore { void Flush() {} }\n")
(root / "docs/okf/widgets.md").write_text(
    "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`, now with Flush.\n")
git(root, "add", "-A", date="2026-03-01T12:00:00")
git(root, "commit", "-q", "-m", "both move", date="2026-03-01T12:00:00")
code, out = run(root, "okf-debt")
check(code == 0 and out == "", "a document that moved with its source is clean",
      f"exit={code} out={out!r}")

print("== okf-debt: no git history, no findings ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nImplemented in `src/App/WidgetStore.cs`.\n"},
    {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "okf-debt")
check(code == 0 and out == "", "an untracked tree yields no debt findings",
      f"exit={code} out={out!r}")
```

- [ ] **Step 2: Run to verify they fail**

Run: `python3 evals/check_engine.py`
Expected: the four new cases FAIL with exit 2 (`okf-debt` is not a known job).

- [ ] **Step 3: Commit the red tests**

```bash
git add evals/check_engine.py
git commit -m "BL-033 evals: okf-debt cases, red — the job does not exist

Four cases pin the debt rule before it is written: 59 days is debt, 19
is not, a document that moved with its source is clean, and a tree with
no git history yields nothing rather than erroring."
```

- [ ] **Step 4: Implement the job**

In `skill/assets/engine/engine.py`, add after `job_anchors` and extend `JOBS`:

```python
def git_iso(rel: str) -> str | None:
    """Newest commit date for a path, or None (untracked, or no git)."""
    try:
        r = subprocess.run(["git", "log", "-1", "--format=%cI", "--", rel],
                           cwd=ROOT, capture_output=True, text=True, timeout=10)
    except (OSError, subprocess.SubprocessError):
        return None
    return r.stdout.strip() or None


def job_okf_debt() -> list[str]:
    top = top_level_dirs()
    findings: list[str] = []
    for doc in anchored_docs():
        rel = doc.relative_to(ROOT).as_posix()
        doc_iso = git_iso(rel)
        if not doc_iso:
            continue                      # untracked, or no git — nothing to compare
        doc_dt = datetime.fromisoformat(doc_iso)
        worst: tuple[str, int] | None = None
        for _lineno, line in scannable_lines(doc.read_text(errors="ignore")):
            for m in TOKEN.finditer(line):
                token = m.group(1).strip()
                if classify(token, top) != "path":
                    continue
                target = path_target(token)
                if not target.exists():
                    continue              # a broken anchor is the anchors job's finding
                src_rel = target.relative_to(ROOT).as_posix()
                src_iso = git_iso(src_rel)
                if not src_iso:
                    continue
                days = (datetime.fromisoformat(src_iso) - doc_dt).days
                if days > DEBT_DAYS and (worst is None or days > worst[1]):
                    worst = (src_rel, days)
        if worst:
            findings.append(f"{rel}: okf-sync-debt: {worst[0]} changed "
                            f"{worst[1]} days after this document")
    return sorted(findings)


JOBS = {"anchors": job_anchors, "okf-debt": job_okf_debt}
```

Delete the old one-entry `JOBS` line so exactly one definition remains.

- [ ] **Step 5: Run to verify green**

Run: `python3 evals/check_engine.py`
Expected: `all engine checks passed` (15 cases).

- [ ] **Step 6: Commit**

```bash
git add skill/assets/engine/engine.py
git commit -m "BL-033: okf-debt — anchors used twice

A document's path-anchors already say which files it is about, so the
debt job needs no second mechanism: an anchored source with a commit
more than 30 days newer than the document's own newest commit is debt.
The comparison uses git dates, never the hand-written front-matter
timestamp — a hand-maintained timestamp rots exactly like the prose
this case exists to police. One finding per document, the worst
offender, so the worklist stays actionable."
```

---

## Task 3: Delivery — the engine becomes an owned file

**Files:**
- Modify: `evals/grade.py:426-439` (`expected_owned`)
- Modify: `evals/check_static.py` (new section before the file-authority one)
- Modify: `skill/SKILL.md:37` (Step 3.1), `:40` (Step 3.4), `:68` (authority
  row), `:172` (audit check 3)

**Interfaces:**
- Consumes: `skill/assets/engine/engine.py` from Task 1.
- Produces: `docs/ai/engine.py` present, byte-identical and listed in
  `ownedFiles` in every legislated repo — which every scenario's
  `common_checks` then asserts for free.

- [ ] **Step 1: Write the failing asserts**

In `evals/grade.py`, inside `expected_owned()`, after the `opencode.json`
block and before `return owned`:

```python
    # v20: the constitution's engine, an owned executable delivered like law.
    eng_src = SKILL / "assets" / "engine" / "engine.py"
    if eng_src.exists():
        owned["docs/ai/engine.py"] = eng_src
```

In `evals/check_static.py`, insert before the `== file authority ==` section:

```python
print("== engine is an owned, delivered artifact ==")
engine_src = SKILL / "assets" / "engine" / "engine.py"
check(engine_src.exists(), "assets/engine/engine.py exists")
if engine_src.exists():
    eng = engine_src.read_text()
    check(eng.startswith("#!/usr/bin/env python3"), "engine has a python3 shebang")
    STDLIB_OK = {"re", "sys", "subprocess", "pathlib", "datetime", "__future__"}
    imported = set(re.findall(r"^(?:from|import)\s+([a-zA-Z_][\w.]*)", eng, re.M))
    check(imported <= STDLIB_OK, "engine imports only stdlib modules",
          f"unexpected: {sorted(imported - STDLIB_OK)}")
    for job in ("anchors", "okf-debt"):
        check(f'"{job}"' in eng, f"engine declares the {job} job")
check("assets/engine/engine.py" in skill_md,
      "SKILL.md Step 3 names the engine source", "Step 3 does not deliver it")
check("docs/ai/engine.py" in skill_md,
      "SKILL.md names the delivered engine path")
```

- [ ] **Step 2: Run both to verify they fail**

Run: `python3 evals/check_static.py`
Expected: FAIL on `SKILL.md Step 3 names the engine source` and
`SKILL.md names the delivered engine path` (the assets checks already pass —
Task 1 created the file).

Run: `python3 evals/grade.py --help` to confirm it still imports; the
`expected_owned` change is exercised only by a live scenario, and its red
state is the benchmark's first fresh-scaffold run in Task 9. Note that in the
commit message rather than pretending a local red.

- [ ] **Step 3: Commit the red asserts**

```bash
git add evals/check_static.py evals/grade.py
git commit -m "BL-033 evals: delivery asserts, red — SKILL.md does not ship the engine

check_static demands that Step 3 name both the engine source and its
delivered path; both are absent, so the wall is red. grade.py's
expected_owned now derives docs/ai/engine.py, which makes every
scenario's common_checks (existence, byte-identity, ownedFiles
membership) assert the delivery — that half goes red on the first live
run of the benchmark, not locally."
```

- [ ] **Step 4: Deliver the engine from SKILL.md**

`skill/SKILL.md:37` — replace the trailing parenthetical of Step 3.1 so the
sentence reads (change shown from `Also copy` onward):

```
Also copy the owned wiring file `opencode.json.tpl` (from `assets/templates/`) to the target repo root as `opencode.json`, and the engine `assets/engine/engine.py` to `docs/ai/engine.py` (both byte-for-byte `cp`; neither has placeholders — owned, machine-managed files refreshed every run). (The legislator-hooks write-guard blocks Edit/Write on `docs/ai/rules/**`, `docs/ai/engine.py` and `opencode.json` in legislated repos; the Bash copy is the sanctioned path.)
```

`skill/SKILL.md:40` — Step 3.4 becomes:

```
4. Compute the new `ownedFiles` list: every path just copied — the `docs/ai/rules/**` files, `docs/ai/engine.py`, and the root `opencode.json` — expressed relative to the target repo root (e.g. `docs/ai/rules/core/okf.md`, `docs/ai/engine.py`, `opencode.json`).
```

`skill/SKILL.md:68` — the authority row becomes:

```
| owned law (`docs/ai/rules/**`, `docs/ai/engine.py`, `opencode.json`) | replace | replace | replace | never-touch | read-only |
```

`skill/SKILL.md:172` — audit check 3's comparison list becomes:

```
3. **Owned-layer integrity (Critical):** `docs/ai/manifest.json` parses; every `ownedFiles` entry exists on disk; every owned file is byte-identical to its source — `docs/ai/rules/**` files against `<skill-path>/assets/rules/...`, `docs/ai/engine.py` against `<skill-path>/assets/engine/engine.py`, and `opencode.json` against `<skill-path>/assets/templates/opencode.json.tpl` (diff each one).
```

- [ ] **Step 5: Verify green**

Run: `python3 evals/check_static.py`
Expected: `all static checks passed`, including the new engine section and the
unchanged file-authority shape check (the row's class label is still
`owned law`, so the pinned 8 × 5 shape holds).

- [ ] **Step 6: Commit**

```bash
git add skill/SKILL.md
git commit -m "BL-033: Step 3 delivers the engine as owned law

The engine rides the path opencode.json already proved: byte-for-byte
cp, listed in ownedFiles, byte-diffed by audit check 3, refreshed on
every run. The authority row gains it beside the rule files — same
rights, no new class, table shape unchanged."
```

---

## Task 4: The law — OKF v2 and the ladder rung

**Files:**
- Modify: `skill/assets/rules/core/okf.md`
- Modify: `skill/assets/rules/core/verification.md:10` (insert a bullet before it)

**Interfaces:**
- Consumes: the engine's job names and finding semantics (Tasks 1–2).
- Produces: the anchor definition as law — the single place it is stated in
  prose; the engine is its only implementation.

- [ ] **Step 1: Amend `core/okf.md`**

Append to `skill/assets/rules/core/okf.md`, after the `### When to update`
block and before the `## OKF is non-negotiable` section:

```markdown
### Link hardness — what each document is bonded to

Hand-maintained truth rots; the parts of this repo that cannot rot are the
parts a machine writes or checks. The bundle is three classes, and a
document's class decides what can be checked about it:

- **anchored** — `index.md`, `codebase-map.md`, and every concept document.
  Hand-written, and every path or symbol it backticks resolves in this
  repository. A broken anchor is a document describing code that is gone.
- **human** — `glossary.md` and `log.md`. Anchoring does not apply: a glossary
  defines terms and a log records what was true at the time, so naming
  something since removed is correct there, not stale.
- **generated** — written by a machine from a source it mirrors, never
  hand-edited, regenerated on demand. Declared here; this bundle has no
  generated member yet.

**What counts as an anchor (closed).** A backticked token in an anchored
document that carries no space, none of `<`, `>`, `*`, `?`, and does not start
with `~` or `/`. It is a **path-anchor** when it contains `/` and its first
segment is a top-level directory of this repository — it resolves when that
path exists (a trailing `.Member()` is stripped first). It is a
**symbol-anchor** when it is PascalCase of at least four characters — it
resolves when it occurs literally under this repository's source roots (every
non-hidden top-level directory except `docs/`, `bin/`, `obj/`,
`node_modules/`, `dist/`). Everything else a document backticks — commands,
field names, lowercase identifiers, templates — is not an anchor. A
symbol-anchor asks whether the identifier still exists, not whether its
declaration kept its shape.

- **`python3 docs/ai/engine.py anchors` is the executing arm of this rule** —
  it writes nothing and reports every anchor that no longer resolves.
  `core/verification.md` carries the rung that requires it before "done".
- **`python3 docs/ai/engine.py okf-debt`** names anchored documents whose
  sources moved on without them: an anchored source file with a commit more
  than 30 days newer than the document's own newest commit. Repair is an
  ordinary OKF update by the document's owner — never an automatic rewrite.
```

- [ ] **Step 2: Add the rung to `core/verification.md`**

Insert immediately **before** the existing `- **The gate before "done":**`
bullet (line 10):

```markdown
- **The static rung — the constitution's own engine** — `python3 docs/ai/engine.py anchors` exits clean before "done": every path and symbol the knowledge layer names still exists in this repository (`core/okf.md` defines what counts as an anchor). A finding means a document describes code that is gone — repair the document or the reference, never the exit code.
```

- [ ] **Step 3: Verify**

Run: `python3 evals/check_static.py`
Expected: pass. The rule-file checks require each file to start with `## ` and
be non-empty; both still do. `AGENTS.md.tpl` needs no change — no new rule
file was added, so the import block is unchanged.

- [ ] **Step 4: Commit**

```bash
git add skill/assets/rules/core/okf.md skill/assets/rules/core/verification.md
git commit -m "BL-033: OKF v2 — link hardness, the anchor definition, the rung

core/okf.md gains the three classes and the closed definition of an
anchor; core/verification.md gains the static rung that requires the
check before done. The definition lives in exactly one place and has
exactly one implementation.

No 'when available' hedge: the engine is an owned repository file, so
every repo at v20+ has it by construction, and a repo below v20 is
reading the previous verification.md. core/artifact-lifecycle.md is
deliberately untouched — anchoring is not a fourth role; the three roles
answer when an artifact dies, anchoring answers what it is bonded to."
```

---

## Task 5: Audit checks 15 and 17

**Files:**
- Modify: `evals/setup_workspace.py` (`materialize_rotted`, ~line 232–500)
- Modify: `evals/grade.py` (`grade_audit`, ~line 743)
- Modify: `skill/SKILL.md:168` (slug line), `:185` (new checks after 16)

**Interfaces:**
- Consumes: the engine's two jobs and their finding shapes.
- Produces: audit findings under the slugs `okf-anchors` and `okf-sync-debt`;
  the fixture's `check_slugs_covered` gains both, keeping grade.py's parity
  law satisfied.

- [ ] **Step 1: Plant the fixture defects**

In `evals/setup_workspace.py`, inside `materialize_rotted`, after the
`orphan-notes.md` block (the `# Defect 7` write), add:

```python
    # Defect 16 (check 15, okf-anchors) — a concept doc naming code that is
    # gone: one path-anchor to a file that does not exist and one symbol
    # nowhere in the source. Linked from the index, so it is not an orphan.
    (okf / "importer.md").write_text(
        "# Importer\n\n"
        "The archive importer lives in `src/LegacyBilling/Removed/OldImporter.cs` "
        "and its sweep step is `ArchivedInvoiceSweeper`.\n")
    # Defect 17 (check 17, okf-sync-debt) — a doc whose anchored source is
    # touched by the second commit (2026-07-01), 167 days after the doc's
    # own commit (2026-01-15). Isolated from defect 16: its anchor resolves.
    (okf / "endpoints.md").write_text(
        "# Endpoints\n\n"
        "The public surface is `src/LegacyBilling/Endpoints.cs`.\n")
```

`src/LegacyBilling/Endpoints.cs` must exist in the first commit for the debt
comparison to have two dates. Immediately after the `.csproj` write near the
top of `materialize_rotted`, add:

```python
    (dest / "src/LegacyBilling/Endpoints.cs").write_text(
        "// initial endpoint surface\n")
```

Extend the index write (Defect 5 block) so neither new document is an orphan:

```python
    (okf / "index.md").write_text(
        "# OKF Index\n\n- [Log](log.md)\n- [Overview draft](overview-draft.md)\n"
        "- [Old notes](renamed-away.md)\n- [Glossary](glossary.md)\n"
        "- [Importer](importer.md)\n- [Endpoints](endpoints.md)\n")
```

The engine must be present for checks 15 and 17 to have anything to run. In
the owned-file loop, after the stacks copy, add:

```python
    # v20: the engine is an owned file. The fixture carries it (checks 15
    # and 17 need a runnable engine); defect 4 is about the manifest's
    # version field, not about which files were delivered.
    shutil.copy2(SKILL / "assets/engine/engine.py", dest / "docs/ai/engine.py")
    owned.append("docs/ai/engine.py")
```

`(dest / "docs/ai")` is created by the manifest block below this point — move
the `(dest / "docs/ai").mkdir(parents=True, exist_ok=True)` line above the
copy, or add an identical `mkdir` before it.

Add to `meta["report_markers"]` (order-independent markers, per the v18
lesson recorded there):

```python
            "okf-anchors]",                  # defect 16a: pinned slug
            "OldImporter.cs",                # defect 16b: the dead path named
            "ArchivedInvoiceSweeper",        # defect 16c: the dead symbol named
            "okf-sync-debt]",                # defect 17a: pinned slug
            "docs/okf/endpoints.md",         # defect 17b: the document named
```

Add to `meta["check_slugs_covered"]`:

```python
            "okf-anchors",              # importer.md names a dead path and symbol
            "okf-sync-debt",            # endpoints.md's source moved on 167 days later
```

- [ ] **Step 2: Confirm the new documents collide with no other check**

Before running anything, walk the interactions — the fixture's rule is that
defects stay isolated:

- both new documents are linked from `index.md`, so check 7 (orphan-docs)
  stays silent;
- neither is law-shaped, so check 12 (stray-rulebooks) stays silent;
- `codebase-map.md`'s `` `legacy/` `` is not an anchor (its first segment is
  not an existing top-level directory), so check 6's stale row is not
  double-reported by check 15, while its `` `docs/` `` resolves;
- `glossary.md` and `log.md` are human class, so the empty-glossary defect
  (check 13) is untouched by anchoring;
- `importer.md`'s anchors do not resolve, so it produces no debt finding —
  the debt job only compares anchors that exist. The two defects stay one
  check each.

- [ ] **Step 3: Verify the fixture and the parity red**

Run: `python3 evals/setup_workspace.py /tmp/v20-red && ls /tmp/v20-red/rotted-layer/repo/docs/okf/`
Expected: `importer.md` and `endpoints.md` present, `docs/ai/engine.py` present.

Run: `cd /tmp/v20-red/rotted-layer/repo && python3 docs/ai/engine.py anchors; echo "exit=$?"`
Expected: exit 1, two findings naming `OldImporter.cs` and
`ArchivedInvoiceSweeper`.

Run: `cd /tmp/v20-red/rotted-layer/repo && python3 docs/ai/engine.py okf-debt; echo "exit=$?"`
Expected: exit 1, one finding naming `docs/okf/endpoints.md` and 167 days.

The parity assert in `grade_audit` is now red by construction: the fixture
covers two slugs the law does not declare (`orphaned` non-empty). That red is
the point — record it.

- [ ] **Step 4: Commit the red fixture**

```bash
git add evals/setup_workspace.py
git commit -m "BL-033 evals: plant anchor rot and sync debt, red

Two isolated defects in the rotted fixture: importer.md names a path
that does not exist and a symbol nowhere in the source; endpoints.md
anchors a file the second commit touches 167 days later. The fixture now
carries docs/ai/engine.py so the two checks have something to run.

grade.py's slug parity is red on purpose — the fixture covers
okf-anchors and okf-sync-debt, which SKILL.md does not yet declare. The
next commit declares them."
```

- [ ] **Step 5: Declare the checks in SKILL.md**

`skill/SKILL.md:168` — the slug line's tail becomes:

```
… 13 `glossary-vitality`, 14 `skill-bindings`, 15 `okf-anchors`, 16 `legacy-home-violation`, 17 `okf-sync-debt`.
```

Insert check 15 after check 14's paragraph and check 17 after check 16's:

```markdown
15. **OKF anchors (Warning):** skip when `docs/ai/engine.py` or `docs/okf/` is absent — a repo below v20 has no engine, and check 4 already reports the staleness that caused it (add nothing to Info for a missing bundle; add `- [okf-anchors] docs/ai/engine.py: engine absent (repo below v20) → re-run /legislator to upgrade` to **Info** when the bundle exists but the engine does not). Otherwise run `python3 docs/ai/engine.py anchors` from the repo root and report each output line as a finding: `<doc>:<line>: <path|symbol>-anchor: <token> → the repo no longer contains it; update the document or fix the reference`. The engine writes nothing, so this check is inside audit's zero-writes contract.
```

```markdown
17. **OKF-sync debt (Warning):** skip under the same conditions as check 15. Otherwise run `python3 docs/ai/engine.py okf-debt` and report each output line as a finding: `<doc>: its anchored source <path> changed <N> days after it → update the document or state why it still holds`. Out-of-cycle drift only — in-cycle staleness is converge's job (`core/sdd.md`), and repair here is an ordinary owner update, never an automatic rewrite.
```

- [ ] **Step 6: Verify the parity green**

Run: `python3 -c "import sys; sys.path.insert(0,'evals'); import grade; print(sorted(grade.audit_check_slugs())); print(len(grade.audit_check_severities()))"`
Expected: 17 slugs listed including `okf-anchors` and `okf-sync-debt`, and a
severity count equal to the slug count (the `audit_slugs_derived` assert).

Run: `python3 evals/check_static.py`
Expected: pass.

- [ ] **Step 7: Commit**

```bash
git add skill/SKILL.md
git commit -m "BL-033: audit checks 15 (okf-anchors) and 17 (okf-sync-debt)

Both run the engine and report its lines; both skip cleanly on a repo
below v20. 15 fills a number that was never used and 17 follows 16 —
slugs are the identity, the numbers are ordinals.

Greens the slug parity left red by the previous commit."
```

---

## Task 6: Restructure routes the two findings to the team

**Files:**
- Modify: `skill/SKILL.md:213` (the Propose step)
- Modify: `evals/grade.py` (`grade_restructure`, ~line 960–1000)

**Interfaces:**
- Consumes: the slugs from Task 5.
- Produces: both findings appear under `## For the team:` in a restructure
  report, and both documents are byte-unchanged afterwards.

- [ ] **Step 1: Write the failing asserts**

In `evals/grade.py`, inside `grade_restructure`, after the
`ghost_import_fixed` check, add:

```python
    # v20: anchor and debt findings are owner prose, not wiring. The closed
    # `fix` scope forbids touching them, so they route to "For the team"
    # and both documents stay byte-identical.
    report = (ws / "restructure" / "outputs" / "restructure-report.md")
    report_text = report.read_text() if report.exists() else ""
    team = report_text.split("## For the team:", 1)[1] if "## For the team:" in report_text else ""
    for marker in ("okf-anchors", "okf-sync-debt"):
        g.check(f"{marker.replace('-', '_')}_routed_to_team", marker in team,
                f"{marker} listed under For the team" if marker in team
                else f"{marker} missing from the For the team section")
    for name, expected in (("importer.md", "OldImporter.cs"),
                           ("endpoints.md", "Endpoints.cs")):
        doc = repo / "docs/okf" / name
        intact = doc.exists() and expected in doc.read_text()
        g.check(f"okf_{name.split('.')[0]}_unedited", intact,
                f"{name} byte-carrying its planted content" if intact
                else f"{name} was rewritten or removed")
```

- [ ] **Step 2: Confirm the red is a live red**

These asserts can only run inside a benchmark scenario. Do not fake a local
red: state in the commit message that the red is observed on the first
restructure run of Task 9's benchmark, and check there that both were red
before the SKILL.md change of Step 3 below. If the benchmark is run only
once, run the restructure scenario twice — once at this commit, once after
Step 3 — and record both in `evals/benchmarks/v20.md`.

- [ ] **Step 3: Commit the asserts, then pin the routing**

```bash
git add evals/grade.py
git commit -m "BL-033 evals: restructure must route anchor findings to the team

Four asserts: both slugs appear under For the team, and both planted
documents keep their content. Red until SKILL.md says so — the closed
fix scope implies it, but implication is not law."
```

`skill/SKILL.md:213` — after the sentence beginning
`Check-14 (\`skill-bindings\`) findings are likewise never plan items`, insert:

```
**Check-15 (`okf-anchors`) and check-17 (`okf-sync-debt`) findings are likewise never plan items** — a document naming code that is gone is owner prose, and rewriting it is inventing project content, which the closed `fix` scope already forbids; list them under the `## For the team:` section with the engine command that reproduces them, and leave every named document byte-unchanged.
```

- [ ] **Step 4: Verify**

Run: `python3 evals/check_static.py`
Expected: pass — in particular `no authority-shaped prose outside the File
authority table`, since the inserted sentence states a routing duty, not a
right. If it trips `AUTH_PROSE`, reword to avoid "never edit/touch" phrasing
(the sentence above deliberately says "leave … byte-unchanged").

- [ ] **Step 5: Commit**

```bash
git add skill/SKILL.md
git commit -m "BL-033: restructure routes anchor and debt findings to the team

Same shape as check-14: a finding the run has no authority to repair is
listed for the owner with the command that reproduces it, and the files
it names are left alone."
```

---

## Task 7: The write-guard covers the engine

**Files:**
- Modify: `plugin/hooks/guard_owned_files.py`
- Modify: `evals/check_hooks.py`

**Interfaces:**
- Consumes: the delivered path `docs/ai/engine.py`.
- Produces: `Edit`/`Write` on the engine is blocked in a legislated repo.

- [ ] **Step 1: Write the failing cases**

`evals/check_hooks.py` already builds a legislated temp repo and feeds the
guard a JSON payload on stdin. Add two cases beside the existing owned-rule
case, using that file's own helper names:

```python
print("== guard: the engine is owned law ==")
repo = legislated_repo()
code, err = run_guard({"tool_name": "Edit",
                       "tool_input": {"file_path": str(repo / "docs/ai/engine.py")},
                       "cwd": str(repo)})
check(code == 2 and "machine-managed law" in err,
      "editing docs/ai/engine.py is blocked", f"exit={code} err={err!r}")

print("== guard: docs/ai is not blanket-guarded ==")
code, err = run_guard({"tool_name": "Edit",
                       "tool_input": {"file_path": str(repo / "docs/ai/notes.md")},
                       "cwd": str(repo)})
check(code == 0, "an unowned file under docs/ai/ is allowed", f"exit={code}")
```

If the helpers in `check_hooks.py` are named differently, keep its names and
the two payloads above — the payload shape is what matters.

- [ ] **Step 2: Run to verify they fail**

Run: `python3 evals/check_hooks.py`
Expected: the engine-edit case FAILs (currently allowed).

- [ ] **Step 3: Commit the red cases**

```bash
git add evals/check_hooks.py
git commit -m "BL-033 evals: guard cases for the engine, red

The engine is owned law that a stray Edit can drift; the guard does not
know it yet."
```

- [ ] **Step 4: Extend the guard**

In `plugin/hooks/guard_owned_files.py`, the decision is the
`is_owned_root_config` line (around line 82). Replace it and the test below it:

```python
        is_owned_root_config = file_path == repo_root / "opencode.json"
        is_owned_engine = file_path == repo_root / "docs" / "ai" / "engine.py"
        if not (in_rules or is_owned_root_config or is_owned_engine):
            return 0
```

Widen `BLOCK_MESSAGE` (line 27) so it names what it now covers:

```python
BLOCK_MESSAGE = (
    "docs/ai/rules/**, docs/ai/engine.py and opencode.json are "
    "machine-managed law — edit the legislator skill source and re-run "
    "/legislator instead."
)
```

Update the module docstring's line 9 to name the engine alongside
`opencode.json`, so the file's own description stays true.

- [ ] **Step 5: Verify and commit**

Run: `python3 evals/check_hooks.py`
Expected: all pass.

```bash
git add plugin/hooks/guard_owned_files.py
git commit -m "BL-033 plugin: the write-guard covers docs/ai/engine.py

Executable owned law deserves the same block as textual owned law. The
Bash-copy bypass stays by design — that is how the legislator itself
writes the file."
```

---

## Task 8: Bookkeeping and the VERSION bump

**Files:**
- Modify: `skill/VERSION`, `docs/ontology.md`, `docs/glossary.md`,
  `docs/philosophy.md`, `docs/backlog.md`

**Interfaces:**
- Consumes: everything above.
- Produces: a repository whose own records match what shipped;
  `check_static.py`'s Horizon wall stays green.

- [ ] **Step 1: Bump VERSION**

`skill/VERSION`: `19` → `20`.

- [ ] **Step 2: Correct the ontology**

`docs/ontology.md` §2 — replace the `generated` bullet (lines 65–72) with:

```markdown
- **generated** — the third ownership class (decided 2026-08-20, deep-audit
  D2; scoped 2026-08-23, BL-033): artifacts written by a machine **locally
  in the repo**, not delivered from the center and not hand-maintained.
  Properties: do-not-edit, regenerated from their source on demand, die
  together with their source; not listed in `ownedFiles` (nothing is
  byte-copied onto them), not keepable. The class is **declared and
  unpopulated**: `baseline.md` (from annotated tests) is its first member and
  arrives with BL-043. `codebase-map.md` and `index.md` are *not* members —
  D2 assumed they were, and the fleet showed otherwise: their rows carry
  judgment a generator would destroy, while their structure is already
  machine-checked (audit checks 6 and 5). They are anchored instead.
- **anchored** — a reference document bonded to code by its own text: every
  path and PascalCase symbol it backticks resolves in its repository,
  verified by `docs/ai/engine.py anchors`. The OKF bundle's default class;
  `glossary.md` and `log.md` are the human-class exceptions.
```

Same file, two riders while the section is open — both records the editions
already invalidated:

- the `owned vs project-owned` bullet's owned list becomes
  ``docs/ai/rules/**`, `docs/ai/engine.py`, `opencode.json``;
- the `manifest` bullet's "(key currently `profiles` — legacy key name,
  concept is stacks; rename queued, BL-028)" becomes "(`stacks`; a legacy
  manifest may carry it as `profiles`, read as the same field)" — BL-028
  shipped in v17;
- the `constitution` bullet's "prose cleanup is queued (BL-030)" becomes
  "the former loose usage for `AGENTS.md` is retired (swept in v17–v18)".

- [ ] **Step 3: Correct the glossary**

`docs/glossary.md` — replace the `baseline` and `generated` rows and add an
`anchored` row in alphabetical position (after `analyze`):

```markdown
| anchored | A knowledge document bonded to code by its own text: every backticked path and PascalCase symbol resolves in the repository. Checked by `docs/ai/engine.py anchors`; `glossary.md` and `log.md` are the human-class exceptions. | coin | `core/okf.md` (BL-033) |
| baseline | The generated answer to "what must the system do today": `baseline.md`, regenerated from EARS ids (R-NNN) ↔ annotated tests. Do-not-edit; a deleted test is a deleted line, visible in the diff. Rot-proof by construction. | coin | `docs/ai/baseline.md` (generated; BL-043, pilot fleet-obs) |
| generated | Third ownership class: artifacts a machine writes locally — do-not-edit, regenerated from source, die with it; not in `ownedFiles`, not keepable. Declared and unpopulated until `baseline.md` (BL-043). | coin | ontology §2; `core/artifact-lifecycle.md` amendment (BL-043) |
```

- [ ] **Step 4: Update the Horizon**

`docs/philosophy.md` §6 — replace the generation-and-anchoring bullet
(lines 305–311) with:

```markdown
- **Generation at full strength** (BL-043) — anchoring landed in v20: the
  engine verifies every path and symbol a knowledge document names against
  the source, and the verification ladder requires it before "done".
  Generation did not: the third ownership class is declared and still has no
  member, because the baseline it would hold is built from requirement ids
  and annotated tests that no repository carries yet. The linter that catches
  dangling ids and uncovered requirements waits with it. Until they land, the
  truth-bonding principle above is enforced for what documents *name* and not
  yet for what they *promise*.
```

`check_static.py` fails if the Horizon names a case the backlog reports
closed, so this and Step 5 must land in the same commit.

- [ ] **Step 5: Update the backlog**

`docs/backlog.md`:
- BL-033's status becomes GREEN with the v20 numbers (filled in Task 9) and
  its **What** narrowed to what shipped, with a pointer to BL-043 for the
  rest.
- New entry **BL-043 — Generated baseline and the spec/plan linter (edition
  v21)**: the baseline generator (`R-NNN` ↔ annotated tests →
  `docs/ai/baseline.md`), the linter and its binding in `core/sdd.md`'s
  analyze gate, the population of the `generated` role class in
  `core/artifact-lifecycle.md`, and the fleet-obs registry `generated`
  content-type with its gold-panel exclusion. Status: queued → edition v21.
- The edition plan section: v20 = BL-033 (docs half), v21 = BL-043, BL-034
  after v21 (its dependency is OKF v2 *and* the generated class).

- [ ] **Step 6: Verify**

Run: `python3 evals/check_static.py`
Expected: pass, including `Horizon's BL-043 exists in the backlog` and
`Horizon's BL-043 is still open`.

- [ ] **Step 7: Commit**

```bash
git add skill/VERSION docs/ontology.md docs/glossary.md docs/philosophy.md docs/backlog.md
git commit -m "BL-033: VERSION 20, and the records match what shipped

The ontology and glossary drop codebase-map and index from the generated
class — the live fleet showed both carry judgment a generator would
destroy, and both were already machine-checked. anchored enters as a
term. The Horizon hands its generation item to BL-043, which the backlog
now carries as edition v21."
```

---

## Task 9: The v20 benchmark

**Files:**
- Create: `evals/benchmarks/v20.md`

**Interfaces:**
- Consumes: everything above.
- Produces: the edition's evidence — corpus result, idempotency, model floor,
  and the defect chronicle.

- [ ] **Step 1: Read the procedure**

Read `evals/README.md` and `evals/POLICY.md` in full before running anything.
The benchmark is a deliverable, not a check.

- [ ] **Step 2: Materialize a workspace and run every scenario**

Run the nine scenarios per `evals/README.md` (`tools/evals-bg.sh` is the
staged runner). Grade each with `evals/grade.py`.

- [ ] **Step 3: Run the idempotency pass three times**

Fresh, upgrade and restructure repos: second runs must produce zero diff. The
engine writes nothing, so a diff here means a delivery bug — investigate, do
not paper over.

- [ ] **Step 4: Classify every red**

Every failure is classified law / grader / harness / model before it is fixed
(POLICY). Record each in the defect chronicle with its classification and fix.

- [ ] **Step 5: Establish the model floor**

Re-run the corpus at the cheapest model that reaches 100%; record it with the
harness version, as `v19.md` does.

- [ ] **Step 6: Write `evals/benchmarks/v20.md`**

Compare against `v19.md`: corpus size before and after (185 + the new asserts),
idempotency, model floor, and the chronicle. State the smoke-test count from
Task 1 Step 7.

- [ ] **Step 7: Fill BL-033's status line**

Put the real numbers into the backlog entry drafted in Task 8.

- [ ] **Step 8: Commit**

```bash
git add evals/benchmarks/v20.md docs/backlog.md
git commit -m "benchmark v20: <N>/<N> corpus, idempotency x3 zero-diff, model floor <model>

BL-033 green. <one line per classified defect, or 'no law or grader
defect was found by the benchmark'>."
```

---

## After the merge (not part of this plan's tasks)

1. **fleet-obs pilot** — upgrade the repository to v20, run both jobs live,
   record what they found. The measurement in the spec predicts zero anchor
   findings there; a surprise is worth a case.
2. **Fleet sweep 16/18 → 20** — cumulative, one pass, per `tools/fleet.sh`.
   fleet-api is the repository the anchor job was built for; expect findings.
