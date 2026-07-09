# CLAUDE.md.tpl v2 Implementation Plan (BL-006)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ambient codebase map, a Boundaries section, a domain-glossary pointer, and a hand-edit ban on owned files to the constitution the Legislator scaffolds.

**Architecture:** Two new project-owned templates (`codebase-map.md.tpl`, `glossary.md.tpl`) join Step 4's scaffold table; `CLAUDE.md.tpl` gains one `@import`, one section, one pointer line; one law line lands in the existing `pair-development.md` core rule (so it reaches already-legislated repos ambiently); the eval grader learns the two new artifacts. Spec: `docs/superpowers/specs/2026-07-09-claude-md-tpl-v2-design.md`.

**Tech Stack:** Markdown templates, SKILL.md prompt procedure, Python eval scripts (`evals/check_static.py`, `evals/grade.py`).

## Global Constraints

- Repo root: `/home/admin/Repository/custom_skills/legislator`. All paths below are relative to it.
- `python3 evals/check_static.py` must pass before every commit (repo CLAUDE.md).
- Editing `skill/assets/rules/**` requires bumping `skill/VERSION` in the same commit (repo README). Task 2 does this: VERSION `6` → `7`.
- Never rewrite files under `docs/superpowers/` from earlier dates.
- Commit messages end with: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- The full e2e benchmark (Task 4) is mandatory before this change is reported complete (repo CLAUDE.md: behavioral change).

---

### Task 1: New templates + SKILL.md registration + CLAUDE.md.tpl v2

These move together because `check_static.py` enforces referenced↔present both ways: a template without a SKILL.md mention fails, and vice versa. The placeholders-documented check likewise ties `CLAUDE.md.tpl`'s new `{{BOUNDARIES}}` token to SKILL.md's derivation rules.

**Files:**
- Create: `skill/assets/templates/codebase-map.md.tpl`
- Create: `skill/assets/templates/glossary.md.tpl`
- Modify: `skill/SKILL.md` (Step 4 table, derivation-rules preamble + list)
- Modify: `skill/assets/templates/CLAUDE.md.tpl`
- Test: `evals/check_static.py` (existing, no changes)

**Interfaces:**
- Produces: scaffold targets `docs/okf/codebase-map.md` and `docs/okf/glossary.md` (Task 3 adds them to the grader); placeholder names `{{CODEBASE_MAP_TABLE}}` and `{{BOUNDARIES}}` (exact spelling — Task 3's grader token-scan and future SKILL.md references depend on them).

- [ ] **Step 1: Run static checks to confirm green baseline**

Run: `python3 evals/check_static.py`
Expected: exits 0, ends with `all static checks passed`.

- [ ] **Step 2: Create `skill/assets/templates/codebase-map.md.tpl`** with exactly:

```markdown
---
type: System
title: {{PROJECT_NAME}} — Codebase Map
description: Top-level directory map — where things live in this repo.
tags: [system, architecture, map]
timestamp: {{TODAY_ISO}}
status: implemented
---

# Codebase Map

One line per top-level directory. Keep this table in sync with the actual tree (the okf.md sync rule applies): update it when directories are added, removed, or repurposed.

| Directory | What lives there |
|-----------|------------------|
{{CODEBASE_MAP_TABLE}}
```

- [ ] **Step 3: Create `skill/assets/templates/glossary.md.tpl`** with exactly:

```markdown
---
type: System
title: {{PROJECT_NAME}} — Domain Glossary
description: Domain terms mapped to their meaning in this codebase.
tags: [system, glossary, domain]
timestamp: {{TODAY_ISO}}
status: implemented
---

# Domain Glossary

Map internal jargon to what it means in this codebase, so any session edits the right files. Add terms as they emerge; keep meanings current (the okf.md sync rule applies).

| Term | Meaning in this codebase |
|------|--------------------------|
```

(The table is intentionally empty — the project fills it over time.)

- [ ] **Step 4: Run static checks to see the expected failure**

Run: `python3 evals/check_static.py`
Expected: FAILs with two `template file is referenced in SKILL.md: codebase-map.md.tpl` / `glossary.md.tpl — orphaned in assets/templates/` findings. This confirms the check guards the registration you are about to add.

- [ ] **Step 5: Register both templates in `skill/SKILL.md` Step 4's table**

In the table in Step 4, immediately after the row `| \`docs/okf/log.md\` | \`okf-log.md.tpl\` | |`, insert:

```markdown
| `docs/okf/codebase-map.md` | `codebase-map.md.tpl` | `{{CODEBASE_MAP_TABLE}}` per the derivation rules below; `{{PROJECT_NAME}}` and `{{TODAY_ISO}}` as usual |
| `docs/okf/glossary.md` | `glossary.md.tpl` | Seeded with an intentionally empty table — terms grow under the okf.md sync rule |
```

- [ ] **Step 6: Update the derivation-rules preamble in `skill/SKILL.md`**

Replace the line:

```markdown
Placeholder derivation rules (fresh-scaffold mode only, except `{{TODAY_ISO}}` and `{{TODAY_ISO_DATE}}` which are always mode-independent — legacy migration extracts the rest of these from the existing CLAUDE.md instead, per Step 5 and the `docs/okf/index.md` row above):
```

with:

```markdown
Placeholder derivation rules (fresh-scaffold mode only, except `{{TODAY_ISO}}`, `{{TODAY_ISO_DATE}}`, `{{CODEBASE_MAP_TABLE}}`, and `{{BOUNDARIES}}`, which are mode-independent — legacy migration extracts the rest of these from the existing CLAUDE.md instead, per Step 5 and the `docs/okf/index.md` row above; the mode-independent four are always derived the same way regardless of mode):
```

- [ ] **Step 7: Append the two new derivation rules in `skill/SKILL.md`**

At the end of the derivation-rules bullet list (after the `{{CATEGORY_MAPPING_TABLE}}` bullet), add:

```markdown
- `{{CODEBASE_MAP_TABLE}}` — list the repo's actual top-level directories, draft a one-line description of each from its contents, and confirm the table with the user before writing. One row per directory, formatted `| ` + backtick-quoted `dir/` + ` | description |`. Mode-independent: always derived from the tree, never from an existing CLAUDE.md.
- `{{BOUNDARIES}}` — detect no-touch candidates from the repo (`bin/`, `obj/`, `node_modules/`, `dist/`, `*.Designer.cs`, database-migration output, vendored code), present them and ask the user for repo-specific additions (legacy areas, generated projects). If nothing exists beyond generated output, write exactly: "Generated build output only (`bin/`, `obj/`, `node_modules/`) — do not edit generated files."
```

- [ ] **Step 8: Update `skill/assets/templates/CLAUDE.md.tpl`** to exactly:

```markdown
# {{PROJECT_NAME}} — Project Instructions for Claude Code

## Project Overview

{{PROJECT_OVERVIEW}}

Stack: {{STACK_SUMMARY}}

- OKF bundle: `docs/okf/` (knowledge documentation — must stay in sync with code)
- Domain glossary: `docs/okf/glossary.md` — check it when a term is unclear; add terms as they emerge
- Specs and plans: `docs/superpowers/` (committed)

@docs/ai/rules/core/okf.md
@docs/ai/rules/core/pair-development.md
@docs/ai/rules/core/decision-gate.md
@docs/ai/rules/core/adr.md
@docs/ai/rules/core/dev-journal.md
@docs/ai/rules/core/changelog.md
{{STACK_IMPORTS}}
@docs/okf/codebase-map.md

## Architecture Constraints

{{PROJECT_ARCHITECTURE_NOTES}}

## Boundaries

{{BOUNDARIES}}

## Build & Test

{{BUILD_TEST_COMMANDS}}
```

(Diff vs. v1: glossary pointer bullet added to the Overview list; `@docs/okf/codebase-map.md` added after `{{STACK_IMPORTS}}`; new `## Boundaries` section with `{{BOUNDARIES}}` between Architecture Constraints and Build & Test. Everything else byte-identical to v1.)

- [ ] **Step 9: Extend Step 7's "Needs your review" wording in `skill/SKILL.md`**

The spec promises already-legislated repos receive the new CLAUDE.md wiring as Step 7 proposals; make that explicit so an upgrade-mode agent actually proposes it. In Step 7, replace:

```markdown
**Needs your review** (e.g. a proposed `@import` line to add/remove from CLAUDE.md when a rule file was added/removed — CLAUDE.md is project-owned, so the Legislator never edits it directly; it only proposes the exact line here for the user to add or delete themselves).
```

with:

```markdown
**Needs your review** (CLAUDE.md is project-owned, so the Legislator never edits it directly; it only proposes exact lines here for the user to apply themselves — e.g. an `@import` line to add/remove when a rule file was added/removed, and, when this run scaffolded an artifact an existing CLAUDE.md doesn't reference yet, the wiring for it: the `@docs/okf/codebase-map.md` import, a `## Boundaries` section, the glossary pointer line).
```

- [ ] **Step 10: Run static checks to verify everything reconciles**

Run: `python3 evals/check_static.py`
Expected: exits 0, `all 10 templates referenced and present, no orphans`, and placeholder checks pass for both new templates (their tokens `{{PROJECT_NAME}}`, `{{TODAY_ISO}}`, `{{CODEBASE_MAP_TABLE}}` are all documented in SKILL.md; `CLAUDE.md.tpl`'s `{{BOUNDARIES}}` likewise).

- [ ] **Step 11: Commit**

```bash
git add skill/assets/templates/codebase-map.md.tpl skill/assets/templates/glossary.md.tpl skill/assets/templates/CLAUDE.md.tpl skill/SKILL.md
git commit -m "Scaffold codebase map, glossary, and Boundaries: templates + Step 4 registration + CLAUDE.md.tpl v2

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 2: Hand-edit ban law line + VERSION bump

**Files:**
- Modify: `skill/assets/rules/core/pair-development.md`
- Modify: `skill/VERSION`
- Test: `evals/check_static.py` (existing, no changes)

**Interfaces:**
- Consumes: nothing from Task 1 (independent — but keep task order for clean benchmark history).
- Produces: constitution version `7` (Task 4's grader derives the expected manifest version from this file automatically).

- [ ] **Step 1: Append the law line to `skill/assets/rules/core/pair-development.md`**

Add as the last bullet of the file (after the "Do not start the next task without explicit user approval" line):

```markdown
- **Never hand-edit `docs/ai/rules/**` or `docs/ai/manifest.json`** — they are machine-managed by the Legislator and overwritten on every run; change rules centrally in the Legislator's repo, then re-run `/legislator`
```

- [ ] **Step 2: Bump `skill/VERSION`**

Replace the file content `6` with `7` (single line, trailing newline preserved).

- [ ] **Step 3: Run static checks**

Run: `python3 evals/check_static.py`
Expected: exits 0 (`VERSION is a bare integer`, rule file still starts with `## ` and is non-empty).

- [ ] **Step 4: Commit (rule edit and VERSION bump together — repo rule)**

```bash
git add skill/assets/rules/core/pair-development.md skill/VERSION
git commit -m "Law: never hand-edit owned rule files or the manifest; bump VERSION to 7

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 3: Teach the grader the two new scaffold artifacts

**Files:**
- Modify: `evals/grade.py` (the `SCAFFOLD_ARTIFACTS` list near the top)
- Test: `python3 -m py_compile evals/grade.py` plus the real proof in Task 4

**Interfaces:**
- Consumes: scaffold target paths from Task 1 (`docs/okf/codebase-map.md`, `docs/okf/glossary.md`).
- Produces: a grader whose fresh-scaffold and migration scenarios fail if the new artifacts are missing.

- [ ] **Step 1: Extend `SCAFFOLD_ARTIFACTS` in `evals/grade.py`**

Change:

```python
SCAFFOLD_ARTIFACTS = [
    "docs/okf/index.md",
    "docs/okf/log.md",
    "docs/backlog.md",
    "docs/adr/0001-record-architecture-decisions.md",
    "docs/adr/template.md",
    "docs/journal/README.md",
    "CHANGELOG.md",
]
```

to:

```python
SCAFFOLD_ARTIFACTS = [
    "docs/okf/index.md",
    "docs/okf/log.md",
    "docs/okf/codebase-map.md",
    "docs/okf/glossary.md",
    "docs/backlog.md",
    "docs/adr/0001-record-architecture-decisions.md",
    "docs/adr/template.md",
    "docs/journal/README.md",
    "CHANGELOG.md",
]
```

- [ ] **Step 2: Syntax-check**

Run: `python3 -m py_compile evals/grade.py && echo OK`
Expected: `OK`

- [ ] **Step 3: Commit**

```bash
git add evals/grade.py
git commit -m "Grader: expect codebase-map.md and glossary.md among scaffold artifacts

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 4: Full e2e benchmark (v7) — run by the controller

This task follows `evals/README.md` and dispatches agents; the controller session executes it directly rather than a subagent implementer.

**Files:**
- Create: `evals/benchmarks/v7.md`
- No skill/ changes.

**Interfaces:**
- Consumes: everything from Tasks 1–3.
- Produces: the recorded verdict for this constitution version; regression gate for reporting BL-006 complete.

- [ ] **Step 1: Materialize a fresh workspace**

Run: `python3 evals/setup_workspace.py /tmp/legislator-eval-v7`
Expected: `workspace ready: /tmp/legislator-eval-v7` with three repos listed. (The generated upgrade fixture will now be at manifest version 6 with `pair-development.md` withheld — it sorts last among core rules — plus the planted retired rule.)

- [ ] **Step 2: Dispatch one fresh agent per scenario (parallel)**

Prompts: `evals/evals.json`. Each agent gets the skill path (`<repo>/skill`), its target repo under `/tmp/legislator-eval-v7/<scenario>/repo`, instructions to follow SKILL.md exactly, treat profile confirmation AND the new codebase-map/boundaries confirmations as pre-approved (derive from the repo, note the derivation), and never commit.

- [ ] **Step 3: Grade all three scenarios**

Run: `python3 evals/grade.py /tmp/legislator-eval-v7`
Expected: fresh-scaffold and legacy-migration now report 10/10 including the two new artifacts inside `scaffold_artifacts_present`; upgrade 10/10 (new rule content of `pair-development.md` propagates verbatim — note: in this workspace pair-development.md is the *withheld* rule, so it doubles as the added-rule check). Any failure: stop, investigate per `evals/README.md` (single failures = investigate, not auto-revert).

- [ ] **Step 4: Idempotency pass**

```bash
git -C /tmp/legislator-eval-v7/fresh-scaffold-dotnet/repo add -A
git -C /tmp/legislator-eval-v7/fresh-scaffold-dotnet/repo -c user.email=eval@local -c user.name=eval commit -m "run 1"
```

Dispatch the same fresh-scaffold agent prompt again (second run, nothing changed), then:

Run: `python3 evals/grade.py /tmp/legislator-eval-v7 idempotency:fresh-scaffold-dotnet`
Expected: `1/1`, `second_run_zero_diff`.

- [ ] **Step 5: Record `evals/benchmarks/v7.md`**

Same table format as `evals/benchmarks/v6.md`: per-scenario pass counts, tokens, wall time from the agent-run notifications, compared against v6's numbers, with notes on any deltas (expected: slightly higher tokens on fresh scaffold — two more artifacts and one more confirmation step).

- [ ] **Step 6: Commit**

```bash
git add evals/benchmarks/v7.md
git commit -m "Record v7 benchmark: CLAUDE.md.tpl v2 scenarios all passing

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
