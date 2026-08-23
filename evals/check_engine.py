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
