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


# ---------------------------------------------------------------------------
# BL-043 (edition v22): the sdd-lint and baseline jobs. Written and shown
# RED against the v21 engine (which has neither job) before either exists —
# POLICY §3's red-before-green, at the engine rung where it costs seconds.
# ---------------------------------------------------------------------------

def make_case_repo() -> Path:
    """A legislated-repo shape with a case tree and an annotated test:
    spec defines R-001..R-003; plan traces R-001 and R-002; the test tree
    carries R-001's marker only. The lint truth: R-003 uncovered... but the
    case HAS a plan, so coverage applies; `per R-999` in the plan dangles;
    a bare {{TOKEN}} in notes.md is reported while a backticked one in the
    spec is quotation and is not."""
    root = make_repo({}, {})
    case = root / "docs" / "cases" / "BL-001-widget-flow"
    case.mkdir(parents=True)
    (case / "spec.md").write_text(
        "# BL-001 — widget flow\n\n"
        "Prose may quote a template token like `{{PROJECT_NAME}}` safely.\n\n"
        "### R-001 — widgets persist\n\nWHEN a widget is saved THEN it SHALL persist.\n\n"
        "### R-002 — widgets list\n\nWHEN listed THEN widgets SHALL appear.\n\n"
        "### R-003 — widgets delete\n\nWHEN deleted THEN widgets SHALL disappear.\n")
    (case / "plan.md").write_text(
        "# plan\n\n1. store layer, per R-001\n2. list endpoint, per R-002\n"
        "3. cleanup, per R-999\n")
    (case / "notes.md").write_text("# notes\n\nLeft behind: {{PROJECT_OVERVIEW}}\n")
    (root / "src" / "App.Tests").mkdir(parents=True)
    (root / "src" / "App.Tests" / "WidgetStoreTests.cs").write_text(
        "// per R-001\npublic class WidgetStoreTests { }\n")
    return root


print("== BL-043 sdd-lint: dangling, uncovered and bare tokens are findings ==")
root = make_case_repo()
code, out = run(root, "sdd-lint")
check(code == 1, "sdd_lint_findings_exit_1", f"exit={code} out={out!r}")
check("R-999" in out and "dangling" in out,
      "sdd_lint_dangling_reference_named", f"out={out!r}")
check("R-003" in out and "uncovered" in out,
      "sdd_lint_uncovered_requirement_named", f"out={out!r}")
check("notes.md" in out and "{{PROJECT_OVERVIEW}}" in out,
      "sdd_lint_bare_token_reported", f"out={out!r}")
check(code == 1 and "{{PROJECT_NAME}}" not in out,
      "sdd_lint_quoted_token_exempt: a backticked token is quotation, not a placeholder",
      f"exit={code} out={out!r}")
check(code == 1 and "uncovered: R-001" not in out and "dangling: R-001" not in out,
      "sdd_lint_covered_requirement_silent", f"exit={code} out={out!r}")

print("== BL-043 sdd-lint: a case without a plan is not lint ==")
root = make_case_repo()
(root / "docs" / "cases" / "BL-001-widget-flow" / "plan.md").unlink()
(root / "docs" / "cases" / "BL-001-widget-flow" / "notes.md").unlink()
code, out = run(root, "sdd-lint")
check(code == 0 and out == "",
      "sdd_lint_planless_case_clean: tier 0/1 is lawful — no plan, no coverage findings",
      f"exit={code} out={out!r}")

print("== BL-043 sdd-lint: a clean case tree is silent ==")
root = make_case_repo()
plan = root / "docs" / "cases" / "BL-001-widget-flow" / "plan.md"
plan.write_text("# plan\n\n1. store, per R-001\n2. list, per R-002\n3. delete, per R-003\n")
(root / "docs" / "cases" / "BL-001-widget-flow" / "notes.md").unlink()
code, out = run(root, "sdd-lint")
check(code == 0 and out == "", "sdd_lint_clean_exit_0", f"exit={code} out={out!r}")

print("== BL-043 baseline: rows for covered, an explicit uncovered list ==")
root = make_case_repo()
code, out = run(root, "baseline")
bl = root / "docs" / "ai" / "baseline.md"
check(code == 0 and bl.exists(), "baseline_writes_declared_target", f"exit={code}")
text = bl.read_text() if bl.exists() else ""
check("R-001" in text and "WidgetStoreTests.cs" in text,
      "baseline_maps_requirement_to_test", f"text={text[:200]!r}")
check("R-002" in text and "R-003" in text,
      "baseline_lists_uncovered_requirements", f"text={text[:200]!r}")
check("do not edit" in text.lower() or "do-not-edit" in text.lower(),
      "baseline_declares_itself_generated", f"head={text[:120]!r}")

print("== BL-043 baseline: deterministic, and a hand edit is destroyed ==")
root = make_case_repo()
run(root, "baseline")
bl = root / "docs" / "ai" / "baseline.md"
first = bl.read_bytes() if bl.exists() else None
run(root, "baseline")
second = bl.read_bytes() if bl.exists() else None
check(first is not None and first == second, "baseline_bytes_deterministic",
      "no baseline written" if first is None else "second run changed bytes")
bl.parent.mkdir(parents=True, exist_ok=True)
bl.write_text("hand edit\n")
run(root, "baseline")
third = bl.read_bytes() if bl.exists() else None
check(first is not None and third == first,
      "baseline_destroys_hand_edits: regeneration is the class's defining property",
      "no baseline to compare" if first is None else "hand edit survived")

print("== BL-043 baseline: writes exactly its declared target (ADR-0003) ==")
root = make_case_repo()
before = {str(q) for q in root.rglob("*") if q.is_file()}
run(root, "baseline")
after = {str(q) for q in root.rglob("*") if q.is_file()}
created = {q.replace(str(root) + "/", "") for q in after - before}
check(created == {"docs/ai/baseline.md"},
      "baseline_writes_nothing_else", f"created={sorted(created)}")

print("== BL-043: check jobs still write nothing ==")
root = make_case_repo()
before = {str(q) for q in root.rglob("*") if q.is_file()}
run(root, "sdd-lint")
after = {str(q) for q in root.rglob("*") if q.is_file()}
check(before == after, "sdd_lint_writes_nothing", f"delta={after ^ before}")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall engine checks passed")
