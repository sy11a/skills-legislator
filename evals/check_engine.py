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

print("== okf-debt: a directory anchor is never a debt source ==")
root = make_repo(
    {"dirdebt.md": "# Dir debt\n\nCode lives under `src/`.\n"},
    {"src/App/Widget.cs": "public class Widget { }\n"})
git(root, "init", "-q")
git(root, "add", "-A", date="2026-01-01T12:00:00")
git(root, "commit", "-q", "-m", "doc and code", date="2026-01-01T12:00:00")
(root / "src/App/Widget.cs").write_text("public class Widget { void Flush() {} }\n")
git(root, "add", "-A", date="2026-03-01T12:00:00")
git(root, "commit", "-q", "-m", "a file under the anchored directory moves on",
    date="2026-03-01T12:00:00")
code, out = run(root, "okf-debt")
check(code == 0 and out == "",
      "a directory anchor's history (the union of everything beneath it) never produces debt",
      f"exit={code} out={out!r}")

print("== BL-051 item 1: a status: removed document is outside the anchored class ==")
REMOVED = ("---\ntype: Concept\nstatus: removed\n---\n\n"
           "# Payments\n\nThis concept was removed. It named `src/App/Gone.cs`.\n")
LIVE = ("---\ntype: Concept\nstatus: implemented\n---\n\n"
        "# Billing\n\nStill names `src/App/Gone.cs`.\n")
root = make_repo({"payments.md": REMOVED, "billing.md": LIVE},
                 {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check("payments.md" not in out,
      "removed_doc_not_anchored: a status: removed document produces no anchor finding",
      f"exit={code} out={out!r}")
check("billing.md:8: path-anchor: src/App/Gone.cs" in out,
      "removed_doc_not_anchored (control): a live sibling with the same dead path still reports",
      f"exit={code} out={out!r}")

print("== BL-051 item 1: nor does it accrue sync debt ==")
root = make_repo({"payments.md": REMOVED, "billing.md": LIVE},
                 {"src/App/Gone.cs": "public class Gone { }\n"})
git(root, "init", "-q")
git(root, "add", "-A", date="2026-01-01T12:00:00")
git(root, "commit", "-q", "-m", "docs and code", date="2026-01-01T12:00:00")
(root / "src/App/Gone.cs").write_text("public class Gone { void Later() {} }\n")
git(root, "add", "-A", date="2026-04-01T12:00:00")
git(root, "commit", "-q", "-m", "source moves on", date="2026-04-01T12:00:00")
code, out = run(root, "okf-debt")
check("payments.md" not in out,
      "removed_doc_no_debt: a status: removed document accrues no okf-sync-debt",
      f"exit={code} out={out!r}")
check("billing.md" in out,
      "removed_doc_no_debt (control): the live sibling still accrues debt",
      f"exit={code} out={out!r}")

print("== BL-051 item 2: build output is excluded at any depth, not just top level ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nHandled by `PaymentProcessor`.\n"},
    {"src/App/obj/Debug/App.js": "var PaymentProcessor = 1;\n",
     "src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, out = run(root, "anchors")
check("symbol-anchor: PaymentProcessor" in out,
      "nested_build_output_ignored: a symbol living only in nested build output stays unresolved",
      f"exit={code} out={out!r}")

print("== BL-051 item 2 (control): a symbol in real source still resolves ==")
root = make_repo(
    {"widgets.md": "# Widgets\n\nHandled by `PaymentProcessor`.\n"},
    {"src/App/PaymentProcessor.cs": "public class PaymentProcessor { }\n"})
code, out = run(root, "anchors")
check(code == 0 and out == "",
      "real_source_still_resolves: a symbol outside build output resolves as before",
      f"exit={code} out={out!r}")

print("== BL-051 item 3: an unhandled exception must not read as a clean audit ==")
root = make_repo({"widgets.md": "# Widgets\n\nSee `src/App/WidgetStore.cs`.\n"},
                 {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
unreadable = root / "docs" / "okf" / "locked.md"
unreadable.write_text("# Locked\n\nNames `src/App/WidgetStore.cs`.\n")
unreadable.chmod(0o000)
code, out = run(root, "anchors")
unreadable.chmod(0o644)
check(code not in (0, 1, 2),
      "crash_exits_distinctly: an unhandled exception exits with a code distinct from clean/findings/usage",
      f"exit={code} out={out!r}")
check(out == "",
      "crash_exits_distinctly (control): a crash prints nothing to stdout, so a stdout-only reader sees no findings",
      f"out={out!r}")

print("== BL-051 item 3 (regression contract): the three documented exit codes ==")
root = make_repo({"widgets.md": "# W\n\nSee `src/App/WidgetStore.cs`.\n"},
                 {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, _ = run(root, "anchors")
check(code == 0, "exit_codes_unchanged: clean is 0", f"exit={code}")
root = make_repo({"widgets.md": "# W\n\nSee `src/App/Gone.cs`.\n"},
                 {"src/App/WidgetStore.cs": "public class WidgetStore { }\n"})
code, _ = run(root, "anchors")
check(code == 1, "exit_codes_unchanged: findings is 1", f"exit={code}")
code, _ = run(root, "no-such-job")
check(code == 2, "exit_codes_unchanged: usage error is 2", f"exit={code}")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall engine checks passed")
