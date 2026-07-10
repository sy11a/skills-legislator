# Project Rules (BL-014 + BL-013 riders) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Project-specific rules get a native home (`.claude/rules/`, auto-loaded by Claude Code) taught by a new core rule — the first constitution-content change (VERSION 7→8) — with migration/harvest/audit/restructure fully integrated and BL-013's wording riders shipped.

**Architecture:** One new law file in `skill/assets/rules/core/`; SKILL.md gains a Step 4 row, audit check 11, harvest source, and the BL-013 fixes; the two reference guides route law-shaped prose to the new home. Evals plant a conflicting project rule and assert carve/flag/decision-gate behavior. Spec: `docs/superpowers/specs/2026-07-10-project-rules-design.md` — binding.

**Tech Stack:** Markdown skill prose; Python 3 stdlib eval scripts.

## Global Constraints

- **NEVER add any `Co-Authored-By` / AI-attribution trailer to any commit.** Absolute repo rule.
- This cycle EDITS `skill/assets/rules/**` (adds `core/project-rules.md`): `skill/VERSION` must become `8` **in the same commit** (repo rule), and the benchmark records as `evals/benchmarks/v8.md`.
- Every commit: `python3 evals/check_static.py` must pass first.
- Pinned strings: audit slug `project-rules` (check 11); decision-item shape `N. [decision] <where> ↔ <owned rule>: <one-line conflict>`.
- Legislator runs never write `.claude/rules/**` content: fresh scaffold creates the empty directory only; migration writes carved files once at migration time; upgrade/audit/restructure treat it as project-owned.
- The rotted/restructure fixture generators share `materialize_rotted`; defect 11 is planted in BOTH variants (no `restructure_extras` gate — audit and restructure both exercise it).

---

### Task 1: Constitution v8 — new core rule + template pointer

**Files:**
- Create: `skill/assets/rules/core/project-rules.md`
- Modify: `skill/VERSION` (7 → 8, same commit)
- Modify: `skill/assets/templates/CLAUDE.md.tpl`

**Interfaces:**
- Produces: rule filename `project-rules.md` — alphabetically last core rule, so the upgrade fixture generator auto-withholds it (upgrade scenario then tests delivering it); the tpl pointer bullet Task 4's grader does NOT assert (kept unasserted — template content is covered by fresh-scaffold's placeholder checks).

- [ ] **Step 1: Create `skill/assets/rules/core/project-rules.md`** with exactly:

````markdown
## Project Rules

- Project-specific rules — law that applies to this repo only ("every feature ships behind a feature toggle") — live in `.claude/rules/`, one topic per file (`feature-toggles.md`, `e2e-tests.md`). Claude Code loads every `.md` there automatically at session start; no `@import` wiring is needed.
- Write them law-shaped: short, imperative, checkable against a diff. How-to guidance is not law — link to where it lives (docs, skills) instead of inlining it.
- When a rule only applies to part of the tree, scope it with `paths:` YAML frontmatter (glob patterns, e.g. `src/api/**/*.ts`) so it loads only when matching files are touched.
- Never put project rules in `docs/ai/rules/**` — that is machine-managed fleet law, overwritten byte-for-byte on every legislator run. Never inline them into CLAUDE.md's body either — CLAUDE.md stays lean; `.claude/rules/` is the project-law home.
- Keep each rule file small (aim under ~30 lines): unscoped rule files enter context every session.
- If a project rule would make sense verbatim in another repo of the same stack, it is a constitution candidate — legislator runs will propose promoting it to fleet law.
````

- [ ] **Step 2: Bump VERSION**

Change `skill/VERSION` content from `7` to `8` (single integer, no trailing content beyond newline).

- [ ] **Step 3: Add the tpl pointer bullet**

In `skill/assets/templates/CLAUDE.md.tpl`, insert directly after the `- Domain glossary: ...` bullet:

```markdown
- Project-specific rules: `.claude/rules/` — one law file per topic, auto-loaded every session; read `docs/ai/rules/core/project-rules.md` before adding one
```

- [ ] **Step 4: Verify and commit**

```bash
python3 evals/check_static.py
```

Expected: all pass, including the new rule file's well-formedness checks (starts with `## `, non-empty).

```bash
git add skill/assets/rules/core/project-rules.md skill/VERSION skill/assets/templates/CLAUDE.md.tpl
git commit -m "Constitution v8: core/project-rules.md (.claude/rules/ project-law home) + CLAUDE.md.tpl pointer"
```

---

### Task 2: SKILL.md + references — scaffold row, audit 11, harvest source, BL-013 riders, carve/merge routing

**Files:**
- Modify: `skill/SKILL.md`
- Modify: `skill/references/migration.md`
- Modify: `skill/references/restructure.md`

**Interfaces:**
- Consumes: the rule file path from Task 1 (named in check 11's remedy).
- Produces: slug `project-rules` and the decision-item shape — Task 3's fixture markers and Task 4's grader depend on them.

- [ ] **Step 1: Step 4 table row**

In `skill/SKILL.md`'s Step 4 table, insert directly after the `docs/superpowers/plans/` row:

```markdown
| `.claude/rules/` | (empty directory) | Create the directory if absent; no file — project-specific rules live here (see `core/project-rules.md`); migration mode may populate it per Step 5 |
```

- [ ] **Step 2: Harvest scans `.claude/rules/`**

In Step 7's candidate-test paragraph, replace:

```
Scan CLAUDE.md's project-specific sections (skip the `@import` block and the template wiring lines) and prose under `docs/okf/`; do not scan
```

with:

```
Scan CLAUDE.md's project-specific sections (skip the `@import` block and the template wiring lines), every `.md` under `.claude/rules/`, and prose under `docs/okf/`; do not scan
```

- [ ] **Step 3: Audit — slug list + check 2 scope + check 11**

Replace the slug-list paragraph line:

```
In findings, `[check-name]` is the check's pinned slug — use exactly these: 1 `imports-resolve`, 2 `unresolved-placeholders`, 3 `owned-integrity`, 4 `staleness`, 5 `okf-index-links`, 6 `codebase-map`, 7 `orphan-docs`, 8 `journal-recency`, 9 `foreign-structures`, 10 `keep-list`.
```

with:

```
In findings, `[check-name]` is the check's pinned slug — use exactly these: 1 `imports-resolve`, 2 `unresolved-placeholders`, 3 `owned-integrity`, 4 `staleness`, 5 `okf-index-links`, 6 `codebase-map`, 7 `orphan-docs`, 8 `journal-recency`, 9 `foreign-structures`, 10 `keep-list`, 11 `project-rules`.
```

Replace check 2:

```
2. **Unresolved placeholders (Critical):** no `{{TOKEN}}` pattern in CLAUDE.md or any `.md` under `docs/`, except `docs/adr/template.md` (its tokens are intentional).
```

with:

```
2. **Unresolved placeholders (Critical):** no `{{TOKEN}}` pattern in CLAUDE.md, any `.md` under `docs/`, or any `.md` under `.claude/rules/`, except `docs/adr/template.md` (its tokens are intentional).
```

Append to check 9's line (before its final period): `; `.claude/rules/` is standard layout, never foreign`. Then add after check 10:

```markdown
11. **Project-rules integrity (Warning):** every `.md` under `.claude/rules/` is project law (see `core/project-rules.md`). Flag any statement there that contradicts an owned rule under `docs/ai/rules/` — finding `<path>: contradicts <owned rule path> → align it with the owned rule, or record an explicit exception in the file (conflicts are decision-gate material; never silently pick a side)`. Skip when the directory is absent.
```

- [ ] **Step 4: BL-013 riders in the Restructure section**

(a) In the section's intro paragraph, replace:

```
Run this section INSTEAD of Steps 0–7 when the user asks to restructure, repair, or reorganize the AI layer.
```

with:

```
Run this section INSTEAD of Steps 1–7 when the user asks to restructure, repair, or reorganize the AI layer — but run Step 0's preconditions first: restructure writes, and a dirty working tree would mix the user's changes into the applied diff.
```

(b) In the Propose step, replace:

```
`N. [action] <current> → <target>: <one-line what/why>`. `[action]` is one of the closed set
```

with:

```
`N. [action] <current> → <target>: <one-line what/why>` — except `decision` items, whose shape is `N. [decision] <where> ↔ <owned rule>: <one-line conflict>`. `[action]` is one of the closed set
```

(c) In the Apply step, replace:

```
then run the **fidelity pass**: every content line of every file this run moved, merged, or edited must still be greppable somewhere in the repo. If applying an item would lose a line, do not apply it.
```

with:

```
then run the **fidelity pass**: every content line of every file this run moved or merged must still be greppable somewhere in the repo (deletions that are an approved `fix` item's stated purpose — a dangling import line, a stale map row, a dead link — are exempt: removing them IS the item). If applying an item would otherwise lose a line, do not apply it.
```

- [ ] **Step 5: references/restructure.md — layout row, merge target, §4 scope, §5 reword**

(a) In the §1 table, replace the foreign-configs row:

```
| Foreign AI configs (`.cursorrules`, `.cursor/`, `AGENTS.md`, `.github/copilot-instructions.md`) | prose merged into CLAUDE.md's project sections; the file removed after the merge |
```

with:

```
| Foreign AI configs (`.cursorrules`, `.cursor/`, `AGENTS.md`, `.github/copilot-instructions.md`) | law-shaped rules merged into `.claude/rules/<topic>.md`; narrative prose into CLAUDE.md's project sections; the file removed after the merge |
| Project-specific rules (law for this repo only) | `.claude/rules/<topic>.md` — see `core/project-rules.md` |
```

(b) In §4, replace:

```
Before applying, inventory every content line of each file a plan item will move, merge, or edit
```

with:

```
Before applying, inventory every content line of each file a plan item will move or merge (edits by `fix` items are exempt — their stated purpose is removing specific dead lines)
```

(c) In §5, replace:

```
- Touch source code or anything outside the AI layer (CLAUDE.md, `docs/**`, root-level foreign AI configs).
```

with:

```
- Touch source code or anything outside the AI layer (the AI layer being: CLAUDE.md, `.claude/rules/**`, `docs/**`, and root-level foreign AI configs).
```

- [ ] **Step 6: references/migration.md §1 — carve law to `.claude/rules/`**

Replace the first classification bullet:

```
- **Project-specific — keep verbatim in the new CLAUDE.md:** project overview, tech stack description, project-specific architecture instances (e.g. "CareerPlatform.Domain has zero NuGet dependencies" — this is an *instance* of the generic `stacks/dotnet/architecture.md` rule and stays as a concrete callout), build/test commands, CI notes.
```

with:

```
- **Project-specific and law-shaped — carve into `.claude/rules/<topic>.md`, verbatim:** imperative, diff-checkable project rules (e.g. "Money values are always `decimal`, never `double` or `float`.") move to one topic file each (or a shared topic file when they clearly group, e.g. `architecture.md`) under `.claude/rules/` — Claude Code auto-loads them, so no import line is added. This keeps CLAUDE.md lean per `core/project-rules.md`.
- **Project-specific instance data — keep verbatim in the new CLAUDE.md:** project overview, tech stack description, architecture *instances* (e.g. "CareerPlatform.Domain has zero NuGet dependencies" — an instance of the generic `stacks/dotnet/architecture.md` rule), branch conventions, build/test commands, CI notes, environment/contact facts. Instance data is not law; it stays in CLAUDE.md's sections.
```

- [ ] **Step 7: Verify and commit**

```bash
python3 evals/check_static.py
grep -c "project-rules" skill/SKILL.md
```

Expected: static all pass; grep ≥ 3 (Step 4 row, slug list, check 11).

```bash
git add skill/SKILL.md skill/references/migration.md skill/references/restructure.md
git commit -m "SKILL.md + references: .claude/rules integration (scaffold row, audit check 11, harvest source, migration carve, restructure routing); BL-013 riders"
```

---

### Task 3: Fixtures, eval docs, README tutorial

**Files:**
- Modify: `evals/setup_workspace.py` (`materialize_rotted`)
- Modify: `evals/evals.json`
- Modify: `evals/README.md`
- Modify: `README.md` (tutorial tree + step 3)

**Interfaces:**
- Consumes: slug `project-rules` from Task 2.
- Produces: rotted meta gains marker `"project-rules] .claude/rules/journal.md"`; restructure-extras meta gains `project_rule_conflict_path` + `project_rule_conflict_content` — Task 4's grader reads exactly these keys.

- [ ] **Step 1: Plant defect 11 in `materialize_rotted`** (BOTH variants — insert after the Defect 9 `.cursorrules` write, before the `if restructure_extras:` block):

```python
    # Defect 11 -- a project rule (in the standard .claude/rules/ home) that
    # contradicts owned law (core/dev-journal.md). Audit check 11 must flag
    # it; restructure must decision-gate it, never edit it.
    (dest / ".claude/rules").mkdir(parents=True)
    (dest / ".claude/rules/journal.md").write_text(
        "# Journal policy\n\n"
        "Dev journal entries are optional; skip them for small changes.\n")
```

(Note: the extras block's `.claude/plans` mkdir must become `mkdir(parents=True, exist_ok=True)` since `.claude/` now already exists.)

- [ ] **Step 2: Meta additions**

In the base `meta`, append to `report_markers`:

```python
            "project-rules] .claude/rules/journal.md",  # defect 11: project rule vs owned law
```

In the `if restructure_extras:` meta block, add:

```python
        meta["project_rule_conflict_path"] = ".claude/rules/journal.md"
        meta["project_rule_conflict_content"] = (
            dest / ".claude/rules/journal.md").read_text()
```

Update both docstrings "ten planted defects" → "eleven planted defects".

- [ ] **Step 3: evals.json**

- `fresh-scaffold-dotnet` (id 0): append to `expected_output`: ` The .claude/rules/ directory is scaffolded (empty).`
- `legacy-migration` (id 1): append to `expected_output`: ` Law-shaped constraints (the decimal-money rule) are carved into .claude/rules/<topic>.md; instance data (the bl/NNN branch convention) stays in CLAUDE.md.`
- `audit` (id 4): change "all ten planted defects" to "all eleven planted defects" and extend the parenthetical with `, a .claude/rules/journal.md project rule contradicting the dev-journal law`.
- `restructure` (id 5): append to `expected_output`: ` The conflicting .claude/rules/journal.md is a second open [decision] item — its content byte-unchanged.`

- [ ] **Step 4: evals/README.md + README.md**

- `evals/README.md` scenario descriptions: extend **audit** with "; project-rule conflicting with owned law flagged under the project-rules slug"; extend **legacy-migration** with "; law carved to .claude/rules/, instance data kept in CLAUDE.md"; extend **restructure** with "; conflicting project rule decision-gated byte-unchanged".
- Root `README.md` tutorial: in the "What you get" tree, insert after the `CLAUDE.md` line: `.claude/rules/             ← YOUR project law — one topic per file, auto-loaded`; and append to tutorial step 3's list: `- **"Add a project rule: every feature ships behind a feature toggle"** — the agent creates \`.claude/rules/feature-toggles.md\` per the project-rules law (auto-loaded, no CLAUDE.md bloat); harvest will later propose promoting it if it generalizes.`

- [ ] **Step 5: Materialize and verify**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/prtest-ws
python3 - <<'EOF'
import json
from pathlib import Path
ws = Path("/tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/prtest-ws")
rot = json.loads((ws / "rotted-layer/fixture_meta.json").read_text())
assert "project-rules] .claude/rules/journal.md" in rot["report_markers"] and len(rot["report_markers"]) == 13
assert (ws / "rotted-layer/repo/.claude/rules/journal.md").exists()
assert "project_rule_conflict_path" not in rot
rst = json.loads((ws / "restructure/fixture_meta.json").read_text())
assert rst["project_rule_conflict_path"] == ".claude/rules/journal.md"
assert rst["project_rule_conflict_content"] == (ws / "restructure/repo/.claude/rules/journal.md").read_text()
assert (ws / "restructure/repo/.claude/plans/2026-01-importer-plan.md").exists()
up = json.loads((ws / "upgrade/fixture_meta.json").read_text())
assert up["withheld_core_rule"] == "project-rules.md", up["withheld_core_rule"]
assert up["fixture_manifest_version"] == 7
print("project-rules fixture assertions OK")
EOF
rm -rf /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/prtest-ws
```

Expected: `project-rules fixture assertions OK` (13 = 12 prior markers + defect 11; upgrade fixture now withholds `project-rules.md` since it sorts last).

- [ ] **Step 6: Commit**

```bash
python3 evals/check_static.py
git add evals/setup_workspace.py evals/evals.json evals/README.md README.md
git commit -m "Fixtures: defect 11 (.claude/rules conflict with owned law), restructure decision-gate meta; eval docs + README tutorial"
```

---

### Task 4: grade.py — scaffold dir, migration carve, restructure decision-gate

**Files:**
- Modify: `evals/grade.py`

**Interfaces:**
- Consumes: Task 3's meta keys; the audit marker is data-driven (no audit code change).

- [ ] **Step 1: Scaffold dir check**

In `scaffold_checks`, append:

```python
        rules_dir = repo / ".claude/rules"
        self.check("project_rules_dir_scaffolded", rules_dir.is_dir(),
                   ".claude/rules/ exists" if rules_dir.is_dir()
                   else ".claude/rules/ directory not scaffolded")
```

- [ ] **Step 2: Migration carve checks**

In `grade_migration`, insert after the `harvest_excludes_instance_convention` check:

```python
    pr_dir = repo / ".claude/rules"
    law_hits = subprocess.run(
        ["grep", "-rl", "Money values are always", str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("law_carved_to_project_rules", bool(law_hits),
            f"decimal-money law lives in {law_hits.splitlines()}" if law_hits
            else "law-shaped constraint not carved into .claude/rules/")
    conv_hits = subprocess.run(
        ["grep", "-rl", "bl/NNN-short-description", str(pr_dir)],
        capture_output=True, text=True).stdout.strip() if pr_dir.is_dir() else ""
    g.check("instance_data_not_in_project_rules", pr_dir.is_dir() and not conv_hits,
            "branch convention correctly stayed in CLAUDE.md" if pr_dir.is_dir() and not conv_hits
            else f"instance data leaked into .claude/rules/ (or dir missing): {conv_hits.splitlines() if conv_hits else 'dir missing'}")
```

- [ ] **Step 3: Restructure decision-gate check**

In `grade_restructure`, insert after the `conflict_surfaced_as_decision` check:

```python
    pr_path = repo / meta["project_rule_conflict_path"]
    pr_ok = pr_path.exists() and pr_path.read_text() == meta["project_rule_conflict_content"]
    pr_named = meta["project_rule_conflict_path"] in report
    g.check("project_rule_conflict_decision_gated", pr_ok and pr_named,
            "conflicting project rule byte-unchanged and named in the report"
            if pr_ok and pr_named
            else f"file untouched={pr_ok}, named in report={pr_named}")
```

- [ ] **Step 4: Dry-run and commit**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/prtest-ws2
python3 evals/grade.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/prtest-ws2 2>&1 | grep -cE "FAIL|Traceback"
rm -rf /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/prtest-ws2
python3 evals/check_static.py
git add evals/grade.py
git commit -m "Grader: project-rules assertions (scaffold dir, migration carve/instance split, restructure decision-gate)"
```

Expected: dry-run shows FAILs (un-run fixtures — correct) and ZERO `Traceback` lines; sanity-read that `project_rule_conflict_decision_gated` fails only on `named in report=False` pre-run (the file itself is untouched in the fixture).

---

### Task 5: Full e2e benchmark v8 + record + backlog

Run by the main session. Same procedure as v7.4 (five scenario agents with prompts from `evals.json` + saved-report paths for migration/audit/restructure; grade; commit run-1 in fresh/upgrade/restructure; three idempotency agents; grade).

- Expected totals: fresh 14, migration 22, upgrade 13, audit 18, restructure 17, idempotency 3×1.
- Record `evals/benchmarks/v8.md` vs `v7.4.md` — note the first VERSION bump, the upgrade scenario now delivering `project-rules.md` itself, and any regression (stop and investigate, never commit over).
- Update `docs/backlog.md`: BL-014 done, BL-013 done; run final whole-cycle review (strongest model) before declaring done.
- Commit: `"Record v8 benchmark: project-rules live; BL-014 + BL-013 done"`.

---

## Deviations from spec (documented)

1. The fresh-scaffold `.claude/rules/` row note says "migration mode may populate it per Step 5" — the spec implies it; made explicit so the create-if-missing rule doesn't read as forbidding migration's carved files.
2. Defect 11 is planted in both fixture variants (audit + restructure) rather than extras-only — the audit needs it for check 11 and restructure needs it for the decision gate; the spec listed it under the rotted fixture without addressing the shared generator.
