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


print("== v23 R-665: okf-debt without git fails loud, never clean ==")
root = make_repo(
    {"mod.md": "---\ntype: Concept\nstatus: implemented\n---\n\nDescribes `src/mod.py`.\n"},
    {"src/mod.py": "x = 1\n"})
git(root, "init", "-q")
git(root, "add", "docs/okf/mod.md", date="2026-06-01T00:00:00Z")
git(root, "commit", "-q", "-m", "doc", "docs/okf/mod.md", date="2026-06-01T00:00:00Z")
git(root, "add", "src/mod.py", date="2026-08-25T00:00:00Z")
git(root, "commit", "-q", "-m", "src", "src/mod.py", date="2026-08-25T00:00:00Z")
code, out = run(root, "okf-debt")
check(code == 1 and "okf-sync-debt" in out,
      "okf_debt_git_absent (control): with git the 85-day debt is a finding",
      f"exit={code} out={out!r}")
with tempfile.TemporaryDirectory() as shim:
    for tool in ("sh",):
        real = shutil.which(tool)
        if real:
            os.symlink(real, Path(shim) / tool)
    os.symlink(sys.executable, Path(shim) / "python3")
    r = subprocess.run([sys.executable, "docs/ai/engine.py", "okf-debt"],
                       cwd=root, capture_output=True, text=True,
                       env={"PATH": shim})
    check(r.returncode not in (0, 1, 2) and "git" in r.stderr.lower(),
          "okf_debt_git_absent: without git the job exits as a check failure, never clean",
          f"exit={r.returncode} stderr={r.stderr!r} stdout={r.stdout!r}")
    check(r.stdout == "",
          "okf_debt_git_absent (stdout control): no findings text a stdout-reader could mistake",
          f"stdout={r.stdout!r}")

print("== v23 R-665 boundary: no anchored docs needs no git ==")
root = make_repo({}, {"src/a.py": "x\n"})
with tempfile.TemporaryDirectory() as shim:
    os.symlink(sys.executable, Path(shim) / "python3")
    r = subprocess.run([sys.executable, "docs/ai/engine.py", "okf-debt"],
                       cwd=root, capture_output=True, text=True,
                       env={"PATH": shim})
    check(r.returncode == 0,
          "okf_debt_git_absent (boundary): nothing to measure is clean even without git",
          f"exit={r.returncode} stderr={r.stderr!r}")

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

if os.name == "nt":
    # chmod 0o000 does not revoke read permission on Windows; the
    # unreadable-file fixture is not constructible there. Stated per
    # BL-070 R-704 — never a silent skip.
    print("== BL-051 item 3: SKIPPED on Windows — chmod cannot make a file unreadable (R-704) ==")
else:
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
        "**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
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


# =====================================================================
# v23 BL-065: the case-shape lints (R-651..R-659) — red-first pairs
# =====================================================================

def case_repo(spec: str | None = None, name: str = "BL-900-test-case",
              extra: dict[str, str] | None = None) -> Path:
    files = dict(extra or {})
    if spec is not None:
        files[f"docs/cases/{name}/spec.md"] = spec
    return make_repo({}, files)

TIER1_HEAD = "# BL-900 — test\n\n**Tier: 1 (light).** x\n\n**Spec type: feature.** y\n\n"
BOUNDARY = "## Boundary\n\n**In:** a thing.\n\n**Out:** another thing (out of scope).\n\n"
HURT = "## The hurting case\n\nGIVEN a repo, WHEN it runs, THEN it works.\n\n"
CLAR = "## Clarifications\n\n- **Q: x?** -> y.\n\n"
REQ_OK = "## Requirements\n\n- **R-901** — WHEN x happens the tool SHALL do y.\n\n"
GOOD_SPEC = TIER1_HEAD + REQ_OK + BOUNDARY + HURT + CLAR

print("== R-651: tier and spec type declared ==")
code, out = run(case_repo(GOOD_SPEC), "sdd-lint")
check(code == 0, "lint_tier_and_type (control): a headered spec is silent", f"exit={code} out={out!r}")
code, out = run(case_repo(GOOD_SPEC.replace("**Tier: 1 (light).** x\n\n", "")), "sdd-lint")
check(code == 1 and "tier" in out.lower(), "lint_tier_required: missing tier is a finding", f"exit={code} out={out!r}")
code, out = run(case_repo(GOOD_SPEC.replace("**Spec type: feature.** y\n\n", "")), "sdd-lint")
check(code == 1 and "spec type" in out.lower(), "lint_type_required: missing spec type is a finding", f"exit={code} out={out!r}")

print("== R-651 boundary: a case with no spec at all is lawful (tier 0) ==")
code, out = run(case_repo(None, extra={"docs/cases/BL-901-direct/summary.md": "# done\n"}), "sdd-lint")
check(code == 0, "lint_specless_case_clean: tier-0 direct case stays silent", f"exit={code} out={out!r}")

print("== R-652: bugfix spec states current/expected/unchanged ==")
bugfix = GOOD_SPEC.replace("**Spec type: feature.**", "**Spec type: bugfix.**")
bugfix_full = bugfix + "## Behavior\n\nCurrent behavior: a. Expected behavior: b. Unchanged: c.\n"
code, out = run(case_repo(bugfix_full), "sdd-lint")
check(code == 0, "lint_bugfix (control): current/expected/unchanged present is silent", f"exit={code} out={out!r}")
code, out = run(case_repo(bugfix), "sdd-lint")
check(code == 1 and "unchanged" in out.lower(), "lint_bugfix_sections: missing unchanged statement is a finding", f"exit={code} out={out!r}")

print("== R-653: boundary and hurting case for tier >= 1 ==")
code, out = run(case_repo(TIER1_HEAD + REQ_OK + HURT + CLAR), "sdd-lint")
check(code == 1 and "boundary" in out.lower(), "lint_boundary_required: missing boundary is a finding", f"exit={code} out={out!r}")
code, out = run(case_repo(TIER1_HEAD + REQ_OK + BOUNDARY + CLAR), "sdd-lint")
check(code == 1 and ("hurting" in out.lower() or "GIVEN" in out), "lint_hurting_case_required: missing GIVEN/WHEN/THEN is a finding", f"exit={code} out={out!r}")

print("== R-654: one SHALL per requirement line ==")
two = GOOD_SPEC.replace("the tool SHALL do y.", "the tool SHALL do y and SHALL do z.")
code, out = run(case_repo(two), "sdd-lint")
check(code == 1 and "SHALL" in out, "lint_ears_two_shalls: two SHALLs on one R-line is a finding", f"exit={code} out={out!r}")
zero = GOOD_SPEC.replace("the tool SHALL do y.", "the tool does y.")
code, out = run(case_repo(zero), "sdd-lint")
check(code == 1 and "SHALL" in out, "lint_ears_no_shall: an R-line with no SHALL is a finding", f"exit={code} out={out!r}")
quoted = GOOD_SPEC.replace("the tool SHALL do y.", "the tool SHALL do y (never write `SHALL SHALL` bare).")
code, out = run(case_repo(quoted), "sdd-lint")
check(code == 0, "lint_ears_quoted_shall (control): backticked SHALL is quotation", f"exit={code} out={out!r}")

print("== R-655: Clarifications required for tier >= 1 ==")
code, out = run(case_repo(TIER1_HEAD + REQ_OK + BOUNDARY + HURT), "sdd-lint")
check(code == 1 and "clarification" in out.lower(), "lint_clarifications_required: missing session is a finding", f"exit={code} out={out!r}")

print("== R-656: ADR shape ==")
ADR_OK = "# 0001. Record decisions\n\n## Status\n\naccepted\n\n## Context\n\nx\n\n## Decision\n\ny\n\n## Consequences\n\nz\n"
code, out = run(case_repo(GOOD_SPEC, extra={"docs/adr/0001-record.md": ADR_OK, "docs/adr/template.md": "{{TITLE}} skeleton\n"}), "sdd-lint")
check(code == 0, "lint_adr (control): well-shaped ADR + template are silent", f"exit={code} out={out!r}")
code, out = run(case_repo(GOOD_SPEC, extra={"docs/adr/0001-record.md": ADR_OK, "docs/adr/0003-gap.md": ADR_OK.replace("0001.", "0003.")}), "sdd-lint")
check(code == 1 and "sequence" in out.lower(), "lint_adr_sequence_gap: 0002 missing is a finding", f"exit={code} out={out!r}")
bad_status = ADR_OK.replace("accepted", "done-ish")
code, out = run(case_repo(GOOD_SPEC, extra={"docs/adr/0001-record.md": bad_status}), "sdd-lint")
check(code == 1 and "status" in out.lower(), "lint_adr_status_closed_set: unknown status is a finding", f"exit={code} out={out!r}")
no_sect = ADR_OK.replace("## Consequences\n\nz\n", "")
code, out = run(case_repo(GOOD_SPEC, extra={"docs/adr/0001-record.md": no_sect}), "sdd-lint")
check(code == 1 and "consequences" in out.lower(), "lint_adr_missing_section: absent Consequences is a finding", f"exit={code} out={out!r}")

print("== R-657: journal day-file names ==")
code, out = run(case_repo(GOOD_SPEC, extra={"docs/journal/2026-08-26.md": "# day\n", "docs/journal/README.md": "# how\n"}), "sdd-lint")
check(code == 0, "lint_journal (control): dated file + README are silent", f"exit={code} out={out!r}")
code, out = run(case_repo(GOOD_SPEC, extra={"docs/journal/notes.md": "# stray\n"}), "sdd-lint")
check(code == 1 and "journal" in out.lower(), "lint_journal_filename: a stray name is a finding", f"exit={code} out={out!r}")

print("== R-658: CHANGELOG carries [Unreleased] ==")
code, out = run(case_repo(GOOD_SPEC, extra={"CHANGELOG.md": "# Changelog\n\n## [Unreleased]\n"}), "sdd-lint")
check(code == 0, "lint_changelog (control): Unreleased present is silent", f"exit={code} out={out!r}")
code, out = run(case_repo(GOOD_SPEC, extra={"CHANGELOG.md": "# Changelog\n\nstuff\n"}), "sdd-lint")
check(code == 1 and "unreleased" in out.lower(), "lint_changelog_unreleased: missing heading is a finding", f"exit={code} out={out!r}")

print("== R-659: OKF front-matter status closed set ==")
okf_ok = "---\ntype: Concept\nstatus: implemented\n---\n\nx\n"
code, out = run(make_repo({"widgets.md": okf_ok}, {f"docs/cases/BL-900-t/spec.md": GOOD_SPEC}), "sdd-lint")
check(code == 0, "lint_okf_status (control): implemented is silent", f"exit={code} out={out!r}")
okf_bad = okf_ok.replace("implemented", "shipped")
code, out = run(make_repo({"widgets.md": okf_bad}, {f"docs/cases/BL-900-t/spec.md": GOOD_SPEC}), "sdd-lint")
check(code == 1 and "status" in out.lower(), "lint_okf_status_closed_set: 'shipped' is a finding", f"exit={code} out={out!r}")
gloss = "---\ntype: System\nstatus: whatever\n---\n\nterms\n"
code, out = run(make_repo({"glossary.md": gloss}, {f"docs/cases/BL-900-t/spec.md": GOOD_SPEC}), "sdd-lint")
check(code == 0, "lint_okf_status_human_exempt: glossary.md is never linted", f"exit={code} out={out!r}")


print("== v23 defect: a QUOTED converge marker must not converge the case ==")
quoted = TIER1_HEAD + REQ_OK + BOUNDARY + HURT + CLAR + \
    'Note: the "\u2705 Converged" close marker binds a closing act.\n'
code, out = run(case_repo(quoted.replace(CLAR, "")), "sdd-lint")
check(code == 1 and "clarification" in out.lower(),
      "lint_quoted_converge_marker_not_a_closure: an inline mention does not exempt the case",
      f"exit={code} out={out!r}")
code, out = run(case_repo(quoted + "\n\u2705 Converged\n"), "sdd-lint")
check(code == 0,
      "lint_standalone_converge_marker_closes (control): the standalone line still exempts",
      f"exit={code} out={out!r}")

print("== BL-065: a converged case is skipped by every case lint ==")
converged = TIER1_HEAD.replace("**Tier: 1 (light).** x\n\n", "") + "done\n\n\u2705 Converged\n"
code, out = run(case_repo(converged), "sdd-lint")
check(code == 0, "lint_converged_case_exempt: history is never re-linted", f"exit={code} out={out!r}")


# =====================================================================
# v23 BL-066: the engine audit job + emitter (R-661..R-669)
# =====================================================================

SKILL_VERSION = (REPO / "skill" / "VERSION").read_text().strip()


def audit_repo(files: dict[str, str], manifest: str = '{"legislatorVersion": ' + SKILL_VERSION + ', "stacks": [], "keep": [], "ownedFiles": []}') -> Path:
    root = Path(tempfile.mkdtemp(prefix="audit-eval-"))
    (root / "docs" / "ai").mkdir(parents=True)
    if manifest is not None:
        (root / "docs" / "ai" / "manifest.json").write_text(manifest)
    shutil.copy2(ENGINE_SRC, root / "docs" / "ai" / "engine.py")
    (root / "AGENTS.md").write_text("# Repo\n\n@docs/okf/index.md\n")
    (root / "docs" / "okf").mkdir(parents=True)
    (root / "docs" / "okf" / "index.md").write_text("# OKF\n\nSee `docs/okf/codebase-map.md`.\n")
    (root / "docs" / "okf" / "codebase-map.md").write_text("# Map\n\n| Directory | What |\n|---|---|\n| `src/` | code |\n| `docs/` | docs |\n")
    (root / "src").mkdir()
    (root / "src" / "a.py").write_text("x = 1\n")
    for rel, text in files.items():
        f = root / rel
        f.parent.mkdir(parents=True, exist_ok=True)
        f.write_text(text)
    return root


def audit(root: Path, *extra: str) -> tuple[int, str, str]:
    r = subprocess.run([sys.executable, "docs/ai/engine.py", "audit",
                        "--skill", str(REPO / "skill"), *extra],
                       cwd=root, capture_output=True, text=True)
    return r.returncode, r.stdout, r.stderr


print("== R-661: a clean repo prints a clean report, exit 0 ==")
root = audit_repo({})
code, out, err = audit(root)
check(code == 0 and "# AI-Layer Audit" in out and "No findings." in out,
      "audit_clean_repo_clean_report", f"exit={code} out={out[:200]!r} err={err[:200]!r}")
check("Clean checks:" in out, "audit_clean_checks_line_present", f"out={out[-300:]!r}")

print("== R-663: the emitter stamp is printed ==")
check("engine.py audit" in out and "constitution v" in out,
      "audit_report_carries_engine_stamp", f"out={out[-300:]!r}")

print("== R-661: planted defects are found with their pinned slugs ==")
root = audit_repo({
    "AGENTS.md": "# Repo\n\n@docs/ai/rules/core/ghost-rule.md\n@docs/okf/index.md\n",
    "docs/okf/overview-draft.md": "# Draft\n\n{{PROJECT_OVERVIEW}}\n\nSee `docs/okf/index.md`.\n",
    "docs/okf/orphan-notes.md": "# Orphan\n",
    ".cursorrules": "Always write tests first.\n",
})
code, out, err = audit(root)
check(code == 1, "audit_defects_exit_1", f"exit={code} err={err[:200]!r}")
for slug, needle in [("imports-resolve", "ghost-rule.md"),
                     ("unresolved-placeholders", "{{PROJECT_OVERVIEW}}"),
                     ("orphan-docs", "orphan-notes.md"),
                     ("foreign-structures", ".cursorrules")]:
    check(f"[{slug}]" in out and needle in out,
          f"audit_finds_{slug}", f"out={out[:600]!r}")
check("## Critical" in out and "## Warning" in out,
      "audit_severity_sections_present", f"out={out[:400]!r}")

print("== R-669: byte-stable across runs ==")
code2, out2, _ = audit(root)
check(out == out2, "audit_report_byte_stable", "two runs differ")

print("== R-667: the audit job writes nothing ==")
before = sorted(str(p.relative_to(root)) + str(p.stat().st_size)
                for p in root.rglob("*") if p.is_file())
audit(root)
after = sorted(str(p.relative_to(root)) + str(p.stat().st_size)
               for p in root.rglob("*") if p.is_file())
check(before == after, "engine_audit_writes_nothing", "tree changed")

print("== R-662: model findings merge into their sections ==")
mf = root / "mf.json"
mf.write_text('{"findings": [{"check": "project-rules", "severity": "Warning", '
              '"line": "- [project-rules] .claude/rules/x.md: contradicts core/sdd.md -> align it"}], '
              '"candidates": ["- \\"Always deploy on Fridays.\\" - AGENTS.md"]}')
code, out, err = audit(root, "--model-findings", str(mf))
warn = out.split("## Warning", 1)[1].split("##", 1)[0] if "## Warning" in out else ""
check("[project-rules]" in warn, "model_findings_in_pinned_sections", f"warn={warn!r} err={err[:200]!r}")
check("## Constitution candidates" in out and "Always deploy on Fridays" in out,
      "model_candidates_appended", f"out={out[-500:]!r}")

print("== R-662: a malformed model-findings file is a loud exit ==")
bad = root / "bad.json"
bad.write_text("{nope")
code, out, err = audit(root, "--model-findings", str(bad))
check(code not in (0, 1, 2) and out == "",
      "audit_malformed_model_findings_fails_loud", f"exit={code} out={out[:100]!r}")

print("== R-665: audit without git fails loud ==")
with tempfile.TemporaryDirectory() as shim:
    os.symlink(sys.executable, Path(shim) / "python3")
    r = subprocess.run([sys.executable, "docs/ai/engine.py", "audit",
                        "--skill", str(REPO / "skill")],
                       cwd=root, capture_output=True, text=True, env={"PATH": shim})
    check(r.returncode not in (0, 1, 2) and "git" in r.stderr.lower(),
          "engine_audit_fails_loud_without_git",
          f"exit={r.returncode} stderr={r.stderr[:200]!r}")

print("== R-661: staleness and the constitution header ==")
root = audit_repo({}, manifest='{"legislatorVersion": 21, "stacks": [], "keep": [], "ownedFiles": []}')
code, out, err = audit(root)
check(f"(skill source: v{SKILL_VERSION}) — behind" in out, "audit_constitution_header_behind", f"out={out[:300]!r}")
check("[staleness]" in out and "legislatorVersion 21" in out,
      "audit_finds_staleness", f"out={out[:600]!r}")

print("== R-661: keep-list integrity and the no-keep-key Info ==")
root = audit_repo({}, manifest='{"legislatorVersion": ' + SKILL_VERSION + ', "stacks": [], "ownedFiles": []}')
code, out, err = audit(root)
check("[keep-list]" in out and "no keep key" in out,
      "audit_keep_key_missing_info", f"out={out[:600]!r}")
root = audit_repo({}, manifest='{"legislatorVersion": ' + SKILL_VERSION + ', "stacks": [], "keep": [{"path": "docs/notes/gone.md", "reason": "x"}], "ownedFiles": []}')
code, out, err = audit(root)
check("[keep-list]" in out and "gone.md" in out and "missing from disk" in out,
      "audit_keep_path_missing", f"out={out[:600]!r}")

print("== R-661: codebase-map freshness both directions ==")
root = audit_repo({"docs/okf/codebase-map.md": "# Map\n\n| Directory | What |\n|---|---|\n| `legacy/` | gone |\n"})
code, out, err = audit(root)
check("[codebase-map]" in out and "legacy/" in out, "audit_map_stale_row", f"out={out[:700]!r}")
check("src/" in out.split("[codebase-map]", 1)[1] if out.count("[codebase-map]") >= 1 else False,
      "audit_map_missing_row", f"out={out[:700]!r}")


print("== v23 defect fixes: check 14 sees backticked names; journal dates from prefix+content ==")
root = audit_repo({".claude/rules/skills.md": "# Skills\n\n- **implement:** `made-up-skill-zz`\n"})
code, out, err = audit(root)
check("[skill-bindings]" in out and "made-up-skill-zz" in out,
      "audit_check14_sees_backticked_names", f"out={out[:500]!r}")
import subprocess as _sp
root = audit_repo({"docs/journal/2026-01-15-setup.md": "# 2026-01-15 — setup\n",
                   "docs/journal/README.md": "# j\n"})
_sp.run(["git", "init", "-q"], cwd=root, check=True)
_sp.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", "add", "-A"], cwd=root, check=True)
_sp.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", "commit", "-qm", "x"],
        cwd=root, check=True, env={**os.environ,
        "GIT_AUTHOR_DATE": "2026-07-01T00:00:00Z", "GIT_COMMITTER_DATE": "2026-07-01T00:00:00Z"})
code, out, err = audit(root)
check("newest entry is 2026-01-15" in out,
      "audit_journal_date_from_prefixed_filename", f"out={out[:800]!r} err={err[:200]!r}")

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


print("== BL-043 sdd-lint: all three real definition forms parse ==")
root = make_repo({}, {})
case = root / "docs" / "cases" / "BL-002-forms"
case.mkdir(parents=True)
(case / "spec.md").write_text(
    "# BL-002 — forms\n\n"
    "**Tier: 0 (direct).** fixture\n\n**Spec type: exploration.** fixture\n\n"
    "### R-001 — heading form\n\nWHEN a THEN b SHALL c.\n\n"
    "- **R-002** — bullet form: the store SHALL persist `docs/x.md` rows.\n\n"
    "R-003 — bare form SHALL hold.\n")
(case / "plan.md").write_text(
    "# plan\n\n1. all of it, per R-001, R-002, R-003\n")
code, out = run(root, "sdd-lint")
check(code == 0 and out == "",
      "sdd_lint_accepts_three_definition_forms_and_list_refs",
      f"exit={code} out={out!r}")
code, out = run(root, "baseline")
text = (root / "docs" / "ai" / "baseline.md").read_text() \
    if (root / "docs" / "ai" / "baseline.md").exists() else ""
check("R-002" in text and "`docs/x.md`" in text,
      "baseline_definition_keeps_inline_code",
      f"text={text[:300]!r}")

print("== BL-043 sdd-lint: a cross-case reference is not dangling ==")
root = make_repo({}, {})
for name, spec, plan in (
        ("BL-003-owner", "### R-001 — owner req\n\na SHALL b.\n", None),
        ("BL-004-rider", "# rider\n\n(no requirements of its own)\n",
         "# plan\n\n1. fix the sibling too, per R-001\n")):
    d = root / "docs" / "cases" / name
    d.mkdir(parents=True)
    (d / "spec.md").write_text(
        f"# {name}\n\n**Tier: 0 (direct).** fixture\n\n"
        f"**Spec type: exploration.** fixture\n\n{spec}")
    if plan:
        (d / "plan.md").write_text(plan)
code, out = run(root, "sdd-lint")
check(code == 0 and out == "",
      "sdd_lint_cross_case_reference_resolves: a rider may trace a sibling case's requirement",
      f"exit={code} out={out!r}")

print("== BL-043 sdd-lint: a converged case is history, not lint ==")
root = make_case_repo()   # carries dangling + uncovered + bare token
case = root / "docs" / "cases" / "BL-001-widget-flow"
(case / "summary.md").write_text("# done\n\n\u2705 Converged.\n")
code, out = run(root, "sdd-lint")
check(code == 0 and out == "",
      "sdd_lint_converged_case_skipped: completed lifecycle artifacts are never re-judged",
      f"exit={code} out={out!r}")


# =====================================================================
# v24 BL-075: detect / apply / verify / report and the run record
# (R-751..R-772). Shown red against the v23 engine: unknown job, exit 2.
# =====================================================================
import hashlib as _hashlib
import json as _json
import re as _re

SKILL_DIR = REPO / "skill"


def eng(root: Path, *args: str, env: dict | None = None) -> tuple[int, str, str]:
    """Run the SKILL SOURCE engine against `root` (the fresh-scaffold shape:
    no docs/ai/engine.py exists yet, so --root is the contract)."""
    r = subprocess.run([sys.executable, str(ENGINE_SRC), *args, "--root", str(root)],
                       cwd=root, capture_output=True, text=True, env=env)
    return r.returncode, r.stdout, r.stderr


def step4_targets() -> list[str]:
    text = (SKILL_DIR / "SKILL.md").read_text()
    step4 = text.split("## Step 4", 1)[1].split("## Step 5", 1)[0]
    out = []
    for m in _re.finditer(r"^\| `([^`]+)` \| ([^|]+) \|", step4, _re.M):
        if not m.group(2).strip().startswith("(empty"):
            out.append(m.group(1))
    return sorted(out)


def git_repo(files: dict[str, str]) -> Path:
    root = Path(tempfile.mkdtemp(prefix="apply-eval-"))
    subprocess.run(["git", "-C", str(root), "init", "-q"], check=True)
    for rel, text in files.items():
        f = root / rel
        f.parent.mkdir(parents=True, exist_ok=True)
        f.write_text(text)
    subprocess.run(["git", "-C", str(root), "add", "-A"], check=True)
    subprocess.run(["git", "-C", str(root), "-c", "user.email=t@t", "-c", "user.name=t",
                    "commit", "-qm", "seed", "--allow-empty"], check=True)
    return root


def scaffold_all(root: Path) -> None:
    """Every Step-4 file target present (content irrelevant to these tests)."""
    for t in step4_targets():
        if t in ("AGENTS.md", "CLAUDE.md"):
            continue
        f = root / t
        f.parent.mkdir(parents=True, exist_ok=True)
        if not f.exists():
            f.write_text("# x\n")


def record_path(root: Path) -> Path:
    digest = _hashlib.sha1(str(root.resolve()).encode()).hexdigest()[:8]
    return Path(tempfile.gettempdir()) / "legislator-runs" / f"{root.name}-{digest}.json"


def tree(root: Path) -> set[str]:
    return {str(p.relative_to(root)) for p in root.rglob("*")
            if (p.is_file() or p.is_symlink()) and ".git" not in p.parts}


CORE_RULES = sorted(p.name for p in (SKILL_DIR / "assets/rules/core").glob("*.md"))
VERSION = (SKILL_DIR / "VERSION").read_text().strip()

print("== R-753: detect — the three modes and the edge case, zero writes ==")
root = git_repo({"README.md": "hi\n"})
code, out, err = eng(root, "detect", "--skill", str(SKILL_DIR))
d = _json.loads(out) if code == 0 and out.strip().startswith("{") else {}
check(code == 0 and d.get("mode") == "fresh" and d.get("entry") is None,
      "detect_fresh", f"exit={code} out={out[:200]!r} err={err[:200]!r}")
root = git_repo({"AGENTS.md": "# P\n"})
code, out, err = eng(root, "detect", "--skill", str(SKILL_DIR))
d = _json.loads(out) if code == 0 else {}
check(d.get("mode") == "migration" and d.get("entry") == "AGENTS.md",
      "detect_migration_agents", f"out={out[:200]!r}")
root = git_repo({"CLAUDE.md": "# P\n", "Foo.csproj": "<Project/>\n"})
code, out, err = eng(root, "detect", "--skill", str(SKILL_DIR))
d = _json.loads(out) if code == 0 else {}
check(d.get("mode") == "migration" and d.get("entry") == "CLAUDE.md"
      and "dotnet" in d.get("stacks", {}).get("candidates", []),
      "detect_migration_claude_and_stack_candidate", f"out={out[:300]!r}")
root = git_repo({"AGENTS.md": "# P\n",
                 "docs/ai/manifest.json": '{"legislatorVersion": 23, "profiles": ["dotnet"], "ownedFiles": ["docs/ai/rules/core/okf.md"]}'})
code, out, err = eng(root, "detect", "--skill", str(SKILL_DIR))
d = _json.loads(out) if code == 0 else {}
check(d.get("mode") == "upgrade" and d.get("stacks", {}).get("subscribed") == ["dotnet"]
      and d.get("ownedFilesOld") == ["docs/ai/rules/core/okf.md"] and not d.get("reconstructed"),
      "detect_upgrade_reads_legacy_profiles", f"out={out[:300]!r}")
root = git_repo({"AGENTS.md": "# P\n\n@docs/ai/rules/core/okf.md\n",
                 "docs/ai/rules/core/okf.md": "x\n", "docs/ai/rules/stacks/dotnet/a.md": "x\n",
                 "opencode.json": "{}\n"})
before = tree(root)
code, out, err = eng(root, "detect", "--skill", str(SKILL_DIR))
d = _json.loads(out) if code == 0 else {}
check(d.get("mode") == "upgrade" and d.get("reconstructed") is True
      and d.get("stacks", {}).get("subscribed") == ["dotnet"]
      and sorted(d.get("ownedFilesOld", [])) == ["docs/ai/rules/core/okf.md", "docs/ai/rules/stacks/dotnet/a.md", "opencode.json"],
      "detect_edge_case_reconstructs_from_disk", f"out={out[:300]!r}")
check(tree(root) == before, "detect_writes_nothing")

print("== R-754/R-757/R-751/R-752: apply — copies, ownedFiles, the pinned manifest, the record ==")
root = git_repo({"README.md": "hi\n"})
rec = record_path(root)
rec.unlink(missing_ok=True)
code, out, err = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "dotnet")
check(code == 0, "apply_fresh_exit_0", f"exit={code} err={err[:300]!r}")
def _same(a: Path, b: Path) -> bool:
    return a.is_file() and a.read_bytes() == b.read_bytes()
owned_ok = all(_same(root / "docs/ai/rules/core" / n, SKILL_DIR / "assets/rules/core" / n) for n in CORE_RULES)
check(owned_ok and _same(root / "docs/ai/engine.py", ENGINE_SRC)
      and _same(root / "opencode.json", SKILL_DIR / "assets/templates/opencode.json.tpl")
      and (root / "docs/ai/rules/stacks/dotnet").is_dir(),
      "apply_copies_owned_set_byte_for_byte")
expected_owned = sorted(["docs/ai/engine.py", "opencode.json"]
                        + [f"docs/ai/rules/core/{n}" for n in CORE_RULES]
                        + [f"docs/ai/rules/stacks/dotnet/{p.name}" for p in (SKILL_DIR / "assets/rules/stacks/dotnet").glob("*.md")])
expected_manifest = ('{\n  "legislatorVersion": ' + VERSION + ',\n  "stacks": ["dotnet"],\n  "keep": [],\n  "ownedFiles": [\n'
                     + ",\n".join(f'    "{o}"' for o in expected_owned) + "\n  ]\n}\n")
mani = (root / "docs/ai/manifest.json").read_text() if (root / "docs/ai/manifest.json").exists() else ""
check(mani == expected_manifest, "apply_manifest_pinned_serialization", f"got={mani[:200]!r}")
check(rec.exists(), "apply_writes_record_at_derived_tempdir_path", str(rec))
r1 = _json.loads(rec.read_text()) if rec.exists() else {}
check(r1.get("mode") == "fresh" and str(r1.get("version")) == VERSION and r1.get("stacks") == ["dotnet"]
      and sorted(r1.get("owned", {}).get("created", [])) == expected_owned
      and "pre" in r1 and "step4" in r1["pre"] and "imports" in r1["pre"],
      "record_carries_mode_version_events_and_pre_snapshot", str(r1)[:300])
check(not any("legislator-runs" in t or t.endswith(".json") and "run" in t for t in tree(root) - {"docs/ai/manifest.json", "opencode.json"}),
      "record_lives_outside_the_repo")
code2, _, _ = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "dotnet")
r2 = _json.loads(rec.read_text()) if rec.exists() else {"owned": {}}
check(code2 == 0 and rec.exists() and (root / "docs/ai/manifest.json").read_text() == expected_manifest
      and r2["owned"].get("created") == [] and r2["owned"].get("overwritten") == []
      and sorted(r2["owned"].get("unchanged", [])) == expected_owned,
      "apply_second_run_byte_stable_and_records_unchanged", str(r2.get("owned"))[:300])

print("== R-755: deletions and empty stack directories ==")
root = git_repo({"AGENTS.md": "# P\n", "docs/ai/rules/core/retired.md": "old\n",
                 "docs/ai/rules/stacks/aurelia/x.md": "old\n",
                 "docs/ai/manifest.json": '{"legislatorVersion": 23, "stacks": ["dotnet", "aurelia"], "keep": [], "ownedFiles": ["docs/ai/rules/core/retired.md", "docs/ai/rules/stacks/aurelia/x.md"]}'})
code, out, err = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "dotnet")
r = _json.loads(record_path(root).read_text()) if record_path(root).exists() else {}
check(code == 0 and not (root / "docs/ai/rules/core/retired.md").exists()
      and not (root / "docs/ai/rules/stacks/aurelia").exists()
      and sorted(r.get("owned", {}).get("deleted", [])) == ["docs/ai/rules/core/retired.md", "docs/ai/rules/stacks/aurelia/x.md"],
      "apply_deletes_retired_and_removes_emptied_stack_dir", f"exit={code} err={err[:200]!r} rec={str(r.get('owned'))[:200]}")

print("== R-756: the keep rules — carry, add, refuse, dedupe, remove ==")
root = git_repo({"AGENTS.md": "# P\n", "docs/notes/a.md": "a\n", "docs/notes/b.md": "b\n", "docs/ai/baseline.md": "gen\n",
                 "docs/ai/manifest.json": '{"legislatorVersion": 23, "stacks": [], "keep": [{"path": "docs/notes/b.md", "reason": "old"}], "ownedFiles": []}'})
code, out, err = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "",
                     "--keep-add", "docs/notes/a.md::hand-tuned", "--keep-add", "docs/ai/rules/core/okf.md::x",
                     "--keep-add", "docs/nope.md::x", "--keep-add", "docs/ai/baseline.md::x",
                     "--keep-add", "docs/notes/b.md::new reason")
mani = _json.loads((root / "docs/ai/manifest.json").read_text()) if code == 0 else {}
r = _json.loads(record_path(root).read_text()) if record_path(root).exists() else {}
check(code == 0 and mani.get("keep") == [{"path": "docs/notes/a.md", "reason": "hand-tuned"}, {"path": "docs/notes/b.md", "reason": "new reason"}],
      "keep_add_and_dedupe_by_path", f"exit={code} keep={mani.get('keep')} err={err[:200]!r}")
refused = {x["path"]: x["reason"] for x in r.get("keep", {}).get("refused", [])}
check(set(refused) == {"docs/ai/rules/core/okf.md", "docs/nope.md", "docs/ai/baseline.md"}
      and "owned" in refused["docs/ai/rules/core/okf.md"] and "exist" in refused["docs/nope.md"],
      "keep_refusals_recorded_with_reasons", str(refused))
mtext = (root / "docs/ai/manifest.json").read_text()
check('  "keep": [\n    {"path": "docs/notes/a.md", "reason": "hand-tuned"},\n    {"path": "docs/notes/b.md", "reason": "new reason"}\n  ],' in mtext,
      "keep_pinned_one_entry_per_line", mtext[:300])
code, out, err = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "", "--keep-remove", "docs/notes/b.md")
mani = _json.loads((root / "docs/ai/manifest.json").read_text()) if code == 0 else {}
check(mani.get("keep") == [{"path": "docs/notes/a.md", "reason": "hand-tuned"}], "keep_remove_only_on_request", str(mani.get("keep")))

print("== R-758/R-771: the v14 file model ==")
root = git_repo({"CLAUDE.md": "# Real\n"})
code, out, err = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
check(code == 0 and (root / "AGENTS.md").is_file() and not (root / "AGENTS.md").is_symlink()
      and (root / "AGENTS.md").read_text() == "# Real\n" and (root / "CLAUDE.md").is_symlink()
      and os.readlink(root / "CLAUDE.md") == "AGENTS.md",
      "file_model_renames_real_claude_and_links", f"exit={code} err={err[:200]!r}")
tracked = subprocess.run(["git", "-C", str(root), "ls-files", "AGENTS.md"], capture_output=True, text=True).stdout.strip()
check(tracked == "AGENTS.md", "file_model_rename_is_git_mv_when_tracked", tracked)
root = git_repo({"AGENTS.md": "# A\n"})
eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
check((root / "CLAUDE.md").is_symlink(), "file_model_links_when_only_agents_exists")
root = git_repo({"AGENTS.md": "# A\n", "CLAUDE.md": "# C\n"})
before = tree(root)
code, out, err = eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
check(code not in (0, 1) and "AGENTS.md" in err and "CLAUDE.md" in err and tree(root) == before,
      "both_real_entry_documents_is_a_loud_stop_with_zero_writes", f"exit={code} err={err[:200]!r}")

print("== R-759: apply writes nothing but the owned set, the manifest and the wiring ==")
root = git_repo({"AGENTS.md": "# A\n", "README.md": "r\n", "docs/notes/a.md": "a\n"})
before = tree(root)
eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "dotnet")
extra = tree(root) - before - set(expected_owned) - {"docs/ai/manifest.json", "CLAUDE.md"}
check(not extra, "apply_footprint_is_exactly_the_declared_set", str(sorted(extra))[:300])

print("== R-760/R-761: verify — diverged file re-copied once, missing artifacts named, post snapshot ==")
root = git_repo({"AGENTS.md": "# A\n"})
eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
scaffold_all(root)
(root / "docs/ai/rules/core").mkdir(parents=True, exist_ok=True)
(root / "docs/ai/rules/core/okf.md").write_text("corrupted\n")
code, out, err = eng(root, "verify", "--skill", str(SKILL_DIR))
check(code == 0 and (root / "docs/ai/rules/core/okf.md").read_bytes() == (SKILL_DIR / "assets/rules/core/okf.md").read_bytes(),
      "verify_recopies_a_diverged_owned_file_once_and_is_clean", f"exit={code} out={out[:200]!r} err={err[:200]!r}")
(root / "docs/cases/README.md").unlink(missing_ok=True)
code, out, err = eng(root, "verify", "--skill", str(SKILL_DIR))
check(code == 1 and "docs/cases/README.md" in out, "verify_names_a_missing_step4_artifact", f"exit={code} out={out[:200]!r}")
r = _json.loads(record_path(root).read_text()) if record_path(root).exists() else {}
check(r.get("post", {}).get("step4", {}).get("docs/cases/README.md") is False
      and r["post"].get("verify", {}).get("clean") is False,
      "verify_appends_post_snapshot_to_record", str(r.get("post"))[:300])

print("== R-762/R-763/R-765/R-766/R-767: report — skeleton, deltas, Health, Keep gating, stamp, stability ==")
root = git_repo({"README.md": "r\n"})
eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
scaffold_all(root)
(root / "AGENTS.md").write_text("# P\n\n" + "\n".join(f"@docs/ai/rules/core/{n}" for n in CORE_RULES) + "\n@docs/okf/codebase-map.md\n\n## Boundaries\n\nnone\n")
eng(root, "verify", "--skill", str(SKILL_DIR))
code, out, err = eng(root, "report", "--skill", str(SKILL_DIR))
check(code == 0 and out.startswith("# Legislator Scaffold — "), "report_scaffold_title", f"exit={code} out={out[:120]!r} err={err[:200]!r}")
heads = [l for l in out.splitlines() if l.startswith("## ")]
check(heads == ["## Created", "## Overwritten", "## Deleted", "## Needs your review"],
      "report_scaffold_sections_pinned_order_no_health_no_keep", str(heads))
created = out.split("## Created", 1)[1].split("## Overwritten", 1)[0] if "## Created" in out else ""
check(f"- `docs/ai/rules/core/okf.md`" in created and "- `docs/cases/README.md`" in created,
      "report_created_lists_owned_files_and_step4_artifacts_from_snapshots", out[:600])
check(bool(out.strip()) and out.rstrip("\n").splitlines()[-1] == f"Emitted by docs/ai/engine.py report — constitution v{VERSION}.",
      "report_stamp_is_last_line", out[-200:])
code2, out2, _ = eng(root, "report", "--skill", str(SKILL_DIR))
check(out == out2, "report_byte_stable")

root = git_repo({"AGENTS.md": "# P\n\n@docs/ai/rules/core/okf.md\n@docs/ai/rules/core/ghost.md\n", "docs/notes/a.md": "a\n",
                 "docs/ai/rules/core/okf.md": "stale\n",
                 "docs/ai/manifest.json": '{"legislatorVersion": 23, "stacks": [], "keep": [], "ownedFiles": ["docs/ai/rules/core/okf.md"]}'})
eng(root, "apply", "--skill", str(SKILL_DIR), "--stacks", "", "--keep-add", "docs/notes/a.md::notes", "--keep-add", "docs/ai/engine.py::x")
scaffold_all(root)
eng(root, "verify", "--skill", str(SKILL_DIR))
code, out, err = eng(root, "report", "--skill", str(SKILL_DIR))
check(out.startswith("# Legislator Upgrade — "), "report_upgrade_title", out[:100])
heads = [l for l in out.splitlines() if l.startswith("## ")]
check(heads == ["## Created", "## Overwritten", "## Deleted", "## Needs your review", "## Keep list", "## Health"],
      "report_upgrade_sections_with_keep_and_health", str(heads))
def _sec(text, a, b):
    return text.split(a, 1)[1].split(b, 1)[0] if a in text and b in text.split(a, 1)[1] else ""
over = _sec(out, "## Overwritten", "## Deleted")
check("- `docs/ai/rules/core/okf.md`" in over, "report_overwritten_lists_changed_owned_file", over[:300])
review = _sec(out, "## Needs your review", "## Keep list")
check("@docs/ai/rules/core/sdd.md" in review and "remove" in review and "@docs/ai/rules/core/ghost.md" in review
      and "@docs/okf/codebase-map.md" in review and "## Boundaries" in review and "docs/okf/glossary.md" in review,
      "report_review_carries_import_deltas_and_scaffold_wiring", review[:500])
keep = _sec(out, "## Keep list", "## Health")
check("docs/notes/a.md" in keep and "docs/ai/engine.py" in keep and "owned" in keep,
      "report_keep_list_added_and_refused", keep[:300])
health = out.split("## Health", 1)[1] if "## Health" in out else ""
check("[imports-resolve]" in health and "ghost.md" in health, "report_health_runs_audit_checks_1_to_6", health[:300])
root2 = git_repo({"AGENTS.md": "# P\n\n" + "\n".join(f"@docs/ai/rules/core/{n}" for n in CORE_RULES) + "\n@docs/okf/codebase-map.md\n\n## Boundaries\n\nx\n\n- Domain glossary: `docs/okf/glossary.md`\n",
                  "docs/ai/manifest.json": '{"legislatorVersion": 23, "stacks": [], "keep": [], "ownedFiles": []}'})
eng(root2, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
scaffold_all(root2)
(root2 / "docs/okf/index.md").write_text("# OKF\n\nSee `docs/okf/codebase-map.md`.\n")
(root2 / "docs/okf/codebase-map.md").write_text("# Map\n\n| Directory | What |\n|---|---|\n| `docs/` | docs |\n")
eng(root2, "verify", "--skill", str(SKILL_DIR))
code, out, err = eng(root2, "report", "--skill", str(SKILL_DIR))
check("Health: clean" in out and "## Keep list" not in out, "report_health_clean_and_no_keep_section_without_delta", out[-300:])

print("== R-762 defect (2026-08-28 manual run): a renamed entry document is not also 'scaffolded' ==")
root4 = git_repo({"CLAUDE.md": "# Real\n", "docs/ai/manifest.json": '{"legislatorVersion": 23, "stacks": [], "keep": [], "ownedFiles": []}'})
eng(root4, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
scaffold_all(root4)
eng(root4, "verify", "--skill", str(SKILL_DIR))
code, out, err = eng(root4, "report", "--skill", str(SKILL_DIR))
created4 = _sec(out, "## Created", "## Overwritten")
check(created4.count("- `AGENTS.md`") == 1 and "renamed from CLAUDE.md" in created4
      and created4.count("- `CLAUDE.md`") == 1,
      "report_entry_document_listed_once_by_its_file_model_event", created4[:400])

print("== R-764: the Step-7 model-findings channel ==")
mf = root2 / "mf.json"
mf.write_text('{"candidates": ["- \\"Always deploy on Fridays.\\" — AGENTS.md"], "review": ["- remove `docs/superpowers/` from .gitignore"]}')
code, out, err = eng(root2, "report", "--skill", str(SKILL_DIR), "--model-findings", str(mf))
heads = [l for l in out.splitlines() if l.startswith("## ")]
check(heads == ["## Created", "## Overwritten", "## Deleted", "## Needs your review", "## Constitution candidates", "## Health"]
      and "Always deploy on Fridays" in _sec(out, "## Constitution candidates", "## Health")
      and ".gitignore" in _sec(out, "## Needs your review", "## Constitution"),
      "report_merges_candidates_and_review_lines_into_pinned_sections", str(heads) + out[:200])
mf.write_text('{"candidates": "nope"}')
code, out, err = eng(root2, "report", "--skill", str(SKILL_DIR), "--model-findings", str(mf))
check(code not in (0, 1) and err.strip(), "report_malformed_findings_is_a_loud_exit", f"exit={code}")
mf.write_text('{"candidates": ["- x"]}')
code, out, err = eng(root, "report", "--skill", str(SKILL_DIR), "--model-findings", str(mf))
_ = out
root3 = git_repo({"README.md": "r\n"})
eng(root3, "apply", "--skill", str(SKILL_DIR), "--stacks", "")
scaffold_all(root3); (root3 / "AGENTS.md").write_text("# P\n")
eng(root3, "verify", "--skill", str(SKILL_DIR))
code, out, err = eng(root3, "report", "--skill", str(SKILL_DIR), "--model-findings", str(mf))
check("## Constitution candidates" not in out, "report_scaffold_never_prints_candidates", out[-300:])

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall engine checks passed")
