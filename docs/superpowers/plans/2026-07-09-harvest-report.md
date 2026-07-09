# Harvest Report (BL-003 + BL-012) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migration/upgrade/audit runs propose "Constitution candidates" (law-shaped, uncovered, generalizable project statements) as report-only output; BL-012's two SKILL.md fixes ride along.

**Architecture:** All harvest behavior is SKILL.md prose (definition lives once in Step 7; the audit section applies it by reference as an appendix). The eval layer plants harvestable + suppressed lines in fixtures and asserts section-scoped: the candidate quoted, the instance data excluded, the suppressed line absent. Spec: `docs/superpowers/specs/2026-07-09-harvest-report-design.md` — binding.

**Tech Stack:** Markdown skill prose; Python 3 stdlib eval scripts.

## Global Constraints

- **NEVER add any `Co-Authored-By` / AI-attribution trailer to any commit.** Absolute repo rule.
- `skill/VERSION` stays `7` — nothing under `skill/assets/rules/` may change.
- Every commit: `python3 evals/check_static.py` must pass first.
- Behavioral `skill/` changes are not "done" until Task 4's e2e benchmark is recorded as `evals/benchmarks/v7.3.md`.
- Harvest is proposals-only: no task may add any write of candidates to any file in a target repo; the audit zero-writes contract is untouched.
- Suppression marker text, exact: `<!-- legislator: not-law -->` (same line, or the immediately preceding line).
- Candidate section heading, exact: `### Constitution candidates`; bullet format: `- "<verbatim quote>" — <repo-relative path>`.

---

### Task 1: SKILL.md — harvest definition, audit appendix, BL-012 fixes

**Files:**
- Modify: `skill/SKILL.md` (Step 3 item 1, Step 7, Audit section)

**Interfaces:**
- Produces: the `### Constitution candidates` section format and the three-part candidate test — Task 2's planted fixture lines and Task 3's grader regex (`r"### Constitution candidates\n(.*?)(?=\n#|\Z)"`) depend on the exact heading.

- [ ] **Step 1: BL-012 item 2 — mandate the Bash copy path**

In Step 3 item 1, replace:

```
using a byte-for-byte copy operation (e.g. `cp`) — never retype or reformat the content.
```

with:

```
using a byte-for-byte Bash copy (`cp`) — never the Write or Edit tools, and never retype or reformat the content. (The legislator-hooks write-guard blocks Edit/Write on `docs/ai/rules/**` in legislated repos; the Bash copy is the sanctioned path.)
```

- [ ] **Step 2: BL-012 item 1 — Keep list section trigger covers refusals**

In Step 7's first paragraph, replace:

```
Additionally, only when this run changed the `keep` list, include a **Keep list** section: each entry added or removed (path + reason) and any refused keep request (the path did not exist).
```

with:

```
Additionally, only when this run changed the `keep` list or refused a keep request, include a **Keep list** section: each entry added or removed (path + reason) and each refused request with why it was refused (the path does not exist, or the path is an owned file under `docs/ai/rules/`).
```

- [ ] **Step 3: Add the harvest subsection to Step 7**

Insert a new paragraph block between Step 7's Health paragraph (`In **upgrade mode only**, append a final section...`) and the `Do not run `git add`...` line:

````markdown
In **migration and upgrade modes** (never fresh scaffold — everything there was just written by this skill), also scan project-owned prose for **constitution candidates** and, when at least one qualifies, append a section (after the Keep list section, before Health):

```
### Constitution candidates
- "<verbatim quote>" — <repo-relative path>
```

A candidate is a statement passing all three tests: (1) **law-shaped** — imperative and diff-checkable ("always …", "never …", "must …", "… before every commit"), not description or narration; (2) **not already covered** — no rule under `docs/ai/rules/**` states it (judge by meaning, not wording); (3) **generalizable** — it would make sense verbatim in another repo of the same stack. Project-instance data is an instantiation of law, not law: a concrete path, this project's branch pattern, a named contact or environment stays put and is never proposed. Scan CLAUDE.md's project-specific sections (skip the `@import` block and the template wiring lines) and prose under `docs/okf/`; do not scan `docs/ai/rules/**`, `docs/adr/**`, `docs/journal/**`, `docs/backlog.md`, or `docs/superpowers/**`. Skip any line carrying `<!-- legislator: not-law -->` on it or as the entire immediately preceding line — the user has already ruled that statement out. Candidates are **proposals only**: never write them to any file; the user promotes worthy ones into the skill's central `assets/rules/**` (bumping VERSION) and re-runs `/legislator` fleet-wide. If nothing qualifies, omit the section entirely.
````

- [ ] **Step 4: Audit section — candidates appendix by reference**

In the Audit section, insert after the report-format fenced block (after the line ending ` ``` ` that closes it):

```markdown
**Constitution candidates appendix:** after the Info section and before the `Clean checks:` line, apply Step 7's constitution-candidates scan — same three tests, same scanned sources, same suppression marker, same section format. It is an appendix of proposals, not a numbered check: no severity, no slug, and it changes nothing about the zero-writes contract. Omit it when nothing qualifies.
```

- [ ] **Step 5: Verify and commit**

```bash
python3 evals/check_static.py
grep -c "Constitution candidates" skill/SKILL.md
```

Expected: static checks all pass; grep prints `3` (Step 7 prose + fenced example + audit appendix paragraph).

```bash
git add skill/SKILL.md
git commit -m "SKILL.md: constitution-candidates harvest (Step 7 + audit appendix); BL-012 fixes (Bash-copy mandate, keep-refusal reporting)"
```

---

### Task 2: Fixtures — planted harvest lines, meta markers, eval docs

**Files:**
- Modify: `evals/setup_workspace.py` (`materialize_rotted`: CLAUDE.md content + meta)
- Modify: `evals/evals.json` (legacy-migration + audit entries)
- Modify: `evals/README.md` (E2E procedure step 2; scenario descriptions)
- Modify: `README.md` (steward section: the harvest operating loop)

**Interfaces:**
- Consumes: exact heading/marker text from Task 1.
- Produces: rotted meta gains one `report_markers` entry (`"dry-run mode before a real import"`) and one `absent_markers` entry (`"Never delete rows from the invoices table"`); migration scenario now requires the eval agent to save its Step 7 report to `<ws>/legacy-migration/outputs/step7-report.md` — Task 3's grader reads exactly that path.

- [ ] **Step 1: Plant one harvestable + one suppressed line in the rotted fixture's CLAUDE.md**

In `evals/setup_workspace.py` `materialize_rotted`, replace the CLAUDE.md write's final segment:

```python
        "## Project notes\n\nLegacyBilling processes archived invoices.\n")
```

with:

```python
        "## Project notes\n\nLegacyBilling processes archived invoices.\n\n"
        # Harvest bait: one law-shaped generalizable line the audit's
        # constitution-candidates appendix must quote...
        "Always run the archive importer in dry-run mode before a real "
        "import.\n"
        # ...and one suppressed by the not-law marker, which it must NOT.
        "<!-- legislator: not-law -->\n"
        "Never delete rows from the invoices table; archive them instead.\n")
```

- [ ] **Step 2: Extend the rotted meta**

In `materialize_rotted`'s `meta`, append to `report_markers`:

```python
            "dry-run mode before a real import",  # harvest: candidate quoted
```

and append to `absent_markers`:

```python
            "Never delete rows from the invoices table",
```

with this comment line above the second addition:

```python
            # not-law suppression: the marked statement must not be proposed
```

- [ ] **Step 3: evals.json updates**

- `legacy-migration` (id 1): append to `expected_output`: ` The Step 7 report (saved by the eval harness to <ws>/legacy-migration/outputs/step7-report.md, outside the repo) carries a "### Constitution candidates" section quoting the decimal-money constraint with its source path, and does NOT propose the bl/NNN branch convention (project-instance data, not generalizable law).`
- `audit` (id 4): append to `expected_output`: ` The report ends with a Constitution candidates appendix quoting the dry-run importer rule; the not-law-marked statement about deleting invoice rows is suppressed and appears nowhere.`

- [ ] **Step 4: evals/README.md updates**

- In "E2E procedure" step 2, after the audit-agent parenthetical, add: `The migration agent must likewise be told to save its Step 7 report to <ws>/legacy-migration/outputs/step7-report.md — the harvest assertions grade that file.`
- In the scenario descriptions: extend **legacy-migration** with "; harvest: the decimal-money constraint is proposed as a constitution candidate, the branch convention (instance data) is not"; extend **audit** with "; harvest appendix: planted law-shaped line quoted, not-law-suppressed line absent".

- [ ] **Step 4b: README.md — the harvest operating loop (steward section)**

In the repo root `README.md`, locate the steward-duties section (added for BL-009; grep for "steward") and append this bullet to its duty list:

```markdown
- **Harvest candidates:** migration/upgrade/audit runs propose "Constitution candidates" in their reports (proposals only — nothing is written anywhere). Review them; promote worthy ones into `assets/rules/**` (bump `VERSION`), then re-run `/legislator` across repos. Reject by adding `<!-- legislator: not-law -->` above (or on) the source line in the target repo, or by rewording it so it stops reading as law. Candidates are re-derived every run, so an unread proposal is never lost — and a rejected one stays silenced.
```

- [ ] **Step 5: Materialize and verify**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/hrtest-ws
python3 - <<'EOF'
import json
from pathlib import Path
ws = Path("/tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/hrtest-ws")
claude = (ws / "rotted-layer/repo/CLAUDE.md").read_text()
assert "dry-run mode before a real import" in claude
assert "<!-- legislator: not-law -->\nNever delete rows" in claude
meta = json.loads((ws / "rotted-layer/fixture_meta.json").read_text())
assert "dry-run mode before a real import" in meta["report_markers"] and len(meta["report_markers"]) == 11
assert "Never delete rows from the invoices table" in meta["absent_markers"] and len(meta["absent_markers"]) == 3
print("harvest fixture assertions OK")
EOF
rm -rf /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/hrtest-ws
```

Expected: `harvest fixture assertions OK`.

- [ ] **Step 6: Commit**

```bash
python3 evals/check_static.py
git add evals/setup_workspace.py evals/evals.json evals/README.md
git commit -m "Fixtures: harvest bait in rotted CLAUDE.md (candidate + not-law-suppressed), meta markers, eval docs"
```

---

### Task 3: grade.py — migration harvest assertions

**Files:**
- Modify: `evals/grade.py` (`grade_migration`, module docstring)

**Interfaces:**
- Consumes: `<ws>/legacy-migration/outputs/step7-report.md` (Task 2's harness contract) and the exact `### Constitution candidates` heading (Task 1). Audit-side harvest needs NO grader change — `report_markers`/`absent_markers` are data-driven from meta.

- [ ] **Step 1: Add section-scoped harvest checks to grade_migration**

In `evals/grade.py` `grade_migration`, insert before the `for needle in MIGRATION_PRESERVED:` loop:

```python
    report_path = ws / "legacy-migration" / "outputs" / "step7-report.md"
    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("step7_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")
    m = re.search(r"### Constitution candidates\n(.*?)(?=\n#|\Z)", report, re.S)
    section = m.group(1) if m else ""
    money = "Money values are always" in section
    g.check("harvest_lists_decimal_money_rule", money,
            "decimal-money constraint quoted as a candidate" if money
            else "candidates section missing or does not quote the money rule")
    no_leak = bool(m) and "bl/NNN-short-description" not in section
    g.check("harvest_excludes_instance_convention", no_leak,
            "branch convention correctly not proposed" if no_leak
            else "instance data leaked into candidates (or section missing)")
```

- [ ] **Step 2: Docstring alignment**

In the module docstring's scenario list, change the `legacy-migration` line to:

```
  legacy-migration         grade <ws>/legacy-migration/repo (+ the Step 7
                           report saved at legacy-migration/outputs/)
```

- [ ] **Step 3: Self-test the section regex both ways**

```bash
python3 - <<'EOF'
import re
RE = r"### Constitution candidates\n(.*?)(?=\n#|\Z)"
rep = ("## Overwritten\n- CLAUDE.md (kept bl/NNN-short-description callout)\n\n"
       "### Constitution candidates\n- \"Money values are always `decimal`\" — CLAUDE.md\n\n"
       "### Health\nHealth: clean\n")
m = re.search(RE, rep, re.S)
assert m and "Money values are always" in m.group(1)
assert "bl/NNN-short-description" not in m.group(1), "section scoping failed"
assert not re.search(RE, "## Overwritten\nno candidates here\n", re.S)
print("section regex self-test OK")
EOF
```

Expected: `section regex self-test OK` — proving the instance-data check can't false-fail on mentions outside the candidates section.

- [ ] **Step 4: Dry-run grader for tracebacks, then commit**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/hrtest-ws2
python3 evals/grade.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/hrtest-ws2 2>&1 | grep -A2 "legacy-migration"
rm -rf /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/hrtest-ws2
python3 evals/check_static.py
git add evals/grade.py
git commit -m "Grader: migration harvest assertions (section-scoped candidate + instance-data exclusion)"
```

Expected: dry-run exits 1 (un-run fixtures — correct) with the three new checks FAILing on readable evidence, no tracebacks; static checks pass.

---

### Task 4: Full e2e benchmark v7.3 + record + backlog

**Files:**
- Create: `evals/benchmarks/v7.3.md`
- Modify: `docs/backlog.md` (BL-003 → done, BL-012 → done, queue Wave 2 note)

**Interfaces:**
- Consumes: everything above. Run by the main session (it dispatches eval agents), not a subagent.

- [ ] **Step 1:** `python3 evals/setup_workspace.py /tmp/legislator-eval-v7.3`
- [ ] **Step 2:** Run the four scenario agents per `evals/README.md` (exact prompts from `evals.json`; the audit agent saves its report to `rotted-layer/outputs/audit-report.md`; the migration agent NOW ALSO saves its Step 7 report to `legacy-migration/outputs/step7-report.md`). Record tokens + wall time.
- [ ] **Step 3:** `python3 evals/grade.py /tmp/legislator-eval-v7.3` — expected exit 0: fresh 13, migration 19 (16 + 3 harvest), upgrade 13, audit 16 (14 + 1 marker + 1 absent-marker).
- [ ] **Step 4:** Commit run-1 in fresh + upgrade repos, run both idempotency agents, then `python3 evals/grade.py /tmp/legislator-eval-v7.3 idempotency:fresh-scaffold-dotnet idempotency:upgrade` — both 1/1.
- [ ] **Step 5:** Record `evals/benchmarks/v7.3.md` vs v7.2 (same table format; note harvest live behavior, any regression stops the cycle — investigate or surface, never commit over it).
- [ ] **Step 6:** Update `docs/backlog.md` (BL-003 done, BL-012 done, Wave 2 complete note); `python3 evals/check_static.py`; commit: `"Record v7.3 benchmark: harvest live; BL-003 + BL-012 done"`.

---

## Deviations from spec (documented)

1. The spec's migration eval names only the decimal-money candidate; the plan adds the *negative* assertion (branch convention must not be proposed) — implied by criterion 3 and the spec's own fixture note, made explicit and section-scoped so narration elsewhere in the report can't false-fail it.
