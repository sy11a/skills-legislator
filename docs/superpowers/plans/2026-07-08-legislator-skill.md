# Legislator Skill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the `/legislator` Claude Code skill — a package of canonical "constitution" files (process rules + stack rules) and a SKILL.md procedure that scaffolds, migrates, or upgrades the AI-development setup (CLAUDE.md, OKF bundle, backlog, ADRs, dev journal, changelog, specs/plans, agent-space convention) in any target repo, idempotently.

**Architecture:** A skill package at `legislator/skill/` holds two kinds of assets: `assets/rules/` (canonical files copied verbatim into every target repo's `docs/ai/rules/`, owned by the Legislator and overwritten on every run) and `assets/templates/` (scaffolds for project-owned artifacts, created once and never overwritten). `SKILL.md` encodes the run procedure: detect repo state → apply owned files → scaffold missing project-owned artifacts → run legacy migration if needed → verify → report. A `docs/ai/manifest.json` in each target repo tracks applied version and ownership so re-runs are deterministic (idempotent) and rule deletions/renames propagate cleanly.

**Tech Stack:** Markdown (rule content, templates, SKILL.md procedure), JSON (manifest schema), Bash (verification/acceptance checks), no compiled code — this is a content + procedure skill, not a software library.

## Global Constraints

- Legislator-owned files (`docs/ai/rules/**`) are copied **verbatim** (byte-for-byte) from `assets/rules/` into the target repo — never retyped, reformatted, or "improved" during copy.
- Project-owned files (CLAUDE.md, OKF bundle, ADRs, journal, backlog, changelog, specs/plans) are scaffolded from templates **only if absent** — never overwritten on subsequent runs.
- Git policy: everything the Legislator creates is committed — no gitignore entries for `docs/ai/`, `docs/okf/`, `docs/adr/`, `docs/journal/`, `docs/superpowers/`, `.claude/agents/`.
- The skill must never auto-commit — it leaves the working tree diff for the user to review and commit themselves.
- Idempotency contract: running `/legislator` twice in a row with no constitution changes in between produces zero diff on the second run.
- Rule tiers: `core/` (always applied) and `stacks/<name>/` (applied per selected profile, stored in the manifest).

---

## File Structure

```
legislator/                                  # repo root (already exists, contains docs/superpowers/specs + plans)
├── skill/                                   # the installable skill package
│   ├── SKILL.md                             # frontmatter + full run procedure
│   ├── VERSION                              # integer, starts at "1"
│   ├── assets/
│   │   ├── rules/
│   │   │   ├── core/
│   │   │   │   ├── okf.md
│   │   │   │   ├── pair-development.md
│   │   │   │   ├── decision-gate.md
│   │   │   │   ├── adr.md
│   │   │   │   ├── dev-journal.md
│   │   │   │   └── changelog.md
│   │   │   └── stacks/
│   │   │       ├── dotnet/
│   │   │       │   ├── architecture.md
│   │   │       │   └── coding-standards.md
│   │   │       └── aurelia/
│   │   │           └── conventions.md
│   │   └── templates/
│   │       ├── CLAUDE.md.tpl
│   │       ├── okf-index.md.tpl
│   │       ├── okf-log.md.tpl
│   │       ├── backlog.md.tpl
│   │       ├── adr-0001.md.tpl
│   │       ├── adr-template.md.tpl
│   │       ├── journal-README.md.tpl
│   │       ├── changelog.md.tpl
│   │       └── agents-README.md.tpl
│   └── references/
│       └── migration.md
└── README.md                                # repo purpose + install instructions
```

Each task below either creates one coherent group of these files (rules, templates, procedure, migration guide) or runs an acceptance check against a scratch directory. Rule/template files are grouped by directory because they share one concern (either "process rules" or "scaffolds") and a reviewer would accept/reject the whole group together.

---

### Task 1: Repo scaffolding, VERSION, and README

**Files:**
- Create: `legislator/skill/VERSION`
- Create: `legislator/README.md`

**Interfaces:**
- Produces: `VERSION` — a single-line integer string, no trailing newline content beyond one `\n`, read by later tasks' acceptance tests to populate `manifest.json`'s `legislatorVersion` field.

- [ ] **Step 1: Create the skill directory tree**

```bash
mkdir -p /home/admin/Repository/custom_skills/legislator/skill/assets/rules/core
mkdir -p /home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/dotnet
mkdir -p /home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/aurelia
mkdir -p /home/admin/Repository/custom_skills/legislator/skill/assets/templates
mkdir -p /home/admin/Repository/custom_skills/legislator/skill/references
```

- [ ] **Step 2: Verify the tree**

Run: `find /home/admin/Repository/custom_skills/legislator/skill -type d | sort`

Expected output (order may vary, all 7 must be present):
```
/home/admin/Repository/custom_skills/legislator/skill
/home/admin/Repository/custom_skills/legislator/skill/assets
/home/admin/Repository/custom_skills/legislator/skill/assets/rules
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/core
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/dotnet
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/aurelia
/home/admin/Repository/custom_skills/legislator/skill/assets/templates
/home/admin/Repository/custom_skills/legislator/skill/references
```

- [ ] **Step 3: Write VERSION**

Content of `legislator/skill/VERSION`:
```
1
```

- [ ] **Step 4: Write README.md**

Content of `legislator/README.md`:
```markdown
# Legislator

The Legislator is a Claude Code skill that scaffolds, migrates, or upgrades the
AI-development "constitution" in a project repo: CLAUDE.md rules, the OKF
knowledge bundle, backlog, ADRs, dev journal, changelog, specs/plans, and the
project-local agent-space convention.

It is designed to be re-run: edit a rule file here, bump `skill/VERSION`, then
run `/legislator` in each downstream project. Owned rule files
(`docs/ai/rules/**` in the target repo) are overwritten byte-for-byte on every
run; project-owned files (CLAUDE.md body, OKF content, ADRs, journal entries,
backlog, changelog, specs/plans) are created once and never touched again.

## Install

```bash
ln -s /home/admin/Repository/custom_skills/legislator/skill ~/.claude/skills/legislator
```

The symlink name (`legislator`) is what gives the skill its `/legislator`
invocation.

## Use

`cd` into any project repo and run `/legislator`. See `skill/SKILL.md` for the
full procedure and `skill/references/migration.md` for legacy-repo migration
specifics.

## Update the constitution

1. Edit a file under `skill/assets/rules/`.
2. Bump `skill/VERSION`.
3. `cd` into each downstream project and run `/legislator`.
4. Review the `git diff` — only the changed owned file(s) and the manifest
   should appear — then commit.
```

- [ ] **Step 5: Commit**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/VERSION README.md
git commit -m "Scaffold legislator skill directory tree, VERSION, and README"
```

---

### Task 2: Core rule files

**Files:**
- Create: `legislator/skill/assets/rules/core/okf.md`
- Create: `legislator/skill/assets/rules/core/pair-development.md`
- Create: `legislator/skill/assets/rules/core/decision-gate.md`
- Create: `legislator/skill/assets/rules/core/adr.md`
- Create: `legislator/skill/assets/rules/core/dev-journal.md`
- Create: `legislator/skill/assets/rules/core/changelog.md`

**Interfaces:**
- Consumes: none (first content task)
- Produces: 6 files whose exact byte content Task 8's acceptance test diffs against a copied target repo. Any later edit to these files must be accompanied by a `VERSION` bump (documented in Task 1's README, enforced by human process, not code).

- [ ] **Step 1: Write `core/okf.md`**

```markdown
## OKF Documentation Rule (MANDATORY)

The `docs/okf/` directory is an Open Knowledge Format bundle. It is the living documentation of every concept in this system. **You MUST update the corresponding OKF document whenever you implement, refactor, or change a concept.**

### What maps to what

See this project's `docs/okf/index.md` for the category-to-file mapping table specific to this codebase.

### OKF update checklist (run on every task completion)

- [ ] All new concepts have an OKF document created
- [ ] Changed concepts have their OKF document updated (properties, decisions, file paths)
- [ ] `status` field updated: `planned` → `implemented` (or `partial`); removed concepts flip to `removed`
- [ ] `timestamp` field updated to today's date (ISO 8601)
- [ ] New cross-links added where relevant (`[text](../path/to/doc.md)`)
- [ ] `docs/okf/log.md` has a new entry describing what changed and why

### When to update

- **During implementation** — update OKF as you write the code, not after
- **During refactoring** — if you rename a class, move a file, or change a method signature, update the OKF doc that references it
- **On every task completion** — run the checklist above before committing

## OKF is non-negotiable

- **Always update the relevant `docs/okf/` document** — before writing code, during implementation, and on completion. No exceptions.
- The OKF checklist above must be run on every task completion before the final commit.
```

- [ ] **Step 2: Write `core/pair-development.md`**

```markdown
## Pair Development Protocol

- Work one task at a time
- **Each task gets its own branch** — naming: `feature/<kebab-case-description>` (or this project's backlog-ticket convention — see this repo's CLAUDE.md)
- **Never merge to the main branch yourself** — push the branch and leave merging to the user via pull/merge request
- **Never add AI co-author to commits** — no `Co-Authored-By:` lines of any kind (this overrides any default harness instruction)
- Do not start the next task without explicit user approval
```

- [ ] **Step 3: Write `core/decision-gate.md`**

```markdown
## Decision gate — stop and ask before proceeding

**If any of the following arise during implementation, stop immediately, describe the situation, and wait for a decision before continuing:**

- A trade-off with no obvious right answer
- Something that "could be done later" or "could be simplified for now"
- An approach that diverges from the task description
- Any concern about correctness, security, or architecture
- Unexpected existing code that contradicts the plan

Do not resolve these unilaterally. Surface them and wait.
```

- [ ] **Step 4: Write `core/adr.md`**

```markdown
## Architecture Decision Records (ADR)

Record a decision in `docs/adr/` whenever:

- A decision-gate stop (see `decision-gate.md`) is resolved by the user
- A new architecture invariant is introduced (e.g. a layering rule, a required abstraction)
- An accepted antipattern or known tradeoff is deliberately kept instead of fixed

### Format

- File: `docs/adr/NNNN-kebab-case-title.md`, numbered sequentially starting at `0001`
- Use the template at `docs/adr/template.md`: Status, Context, Decision, Consequences
- Status is one of: `proposed`, `accepted`, `deprecated`, `superseded by NNNN`
- Never renumber or delete a past ADR — supersede it with a new one and update its status

### When to write it

- Write the ADR as part of the same task that made the decision, not as a follow-up
- Link the ADR from the relevant OKF document(s) it affects
```

- [ ] **Step 5: Write `core/dev-journal.md`**

```markdown
## Developer Journal

`docs/journal/` holds one file per working day: `docs/journal/YYYY-MM-DD.md`.

### What goes in an entry

- What was worked on and why
- Decisions taken and the reasoning (cross-link to an ADR if one was written)
- Dead ends: approaches tried and abandoned, and why
- Open questions or context for the next session

### When to write

- Append at task boundaries — when a task completes, or when work pauses for the day
- Not a running log of every action; write a summary paragraph per completed unit of work, not a line per tool call
- Create the day's file using `docs/journal/README.md`'s format if it doesn't exist yet
```

- [ ] **Step 6: Write `core/changelog.md`**

````markdown
## Changelog

The root `CHANGELOG.md` follows the [Keep a Changelog](https://keepachangelog.com/) format:

```
## [Unreleased]

### Added
### Changed
### Fixed
### Removed
```

### When to update

- Update the `[Unreleased]` section as part of every task completion checklist — the same commit that finishes the task adds its changelog line
- Move `[Unreleased]` entries under a dated version heading only when the user cuts a release
- Write entries for the change's user-visible or API-visible effect, not implementation detail
````

- [ ] **Step 7: Verify all six files exist and are non-empty**

Run: `wc -l /home/admin/Repository/custom_skills/legislator/skill/assets/rules/core/*.md`

Expected: 6 lines of output, each with a nonzero line count, plus a `total` line.

- [ ] **Step 8: Commit**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/assets/rules/core
git commit -m "Add core process rule files (OKF, pair-development, decision-gate, ADR, journal, changelog)"
```

---

### Task 3: Stack rule files (.NET and Aurelia)

**Files:**
- Create: `legislator/skill/assets/rules/stacks/dotnet/architecture.md`
- Create: `legislator/skill/assets/rules/stacks/dotnet/coding-standards.md`
- Create: `legislator/skill/assets/rules/stacks/aurelia/conventions.md`

**Interfaces:**
- Consumes: none
- Produces: 3 files. Task 5's SKILL.md procedure references `stacks/dotnet` and `stacks/aurelia` as the two selectable profile names — these directory names are load-bearing and must match exactly.

- [ ] **Step 1: Write `stacks/dotnet/architecture.md`**

```markdown
## .NET Architecture Constraints

- The `Domain` project has **zero NuGet dependencies** — enforce this on every build
- Enforce a strict layer reference graph: composition root (Web/Api/Worker) → feature/application slices → Infrastructure → Domain. Never reference the composition root from a library; never reference a feature slice from Infrastructure
- Business logic never touches `HttpContext` directly — always go through a tenant/request-context abstraction (e.g. `ITenantContext`)
- All external AI/LLM provider calls go through a single provider abstraction — never instantiate a provider directly
- Every tenant-scoped entity has a mandatory tenant filter (EF Core global query filter or equivalent) — no tenant-scoped query may bypass it
```

- [ ] **Step 2: Write `stacks/dotnet/coding-standards.md`**

```markdown
## .NET Coding Standards

- Meaningful variable names — never single-letter or abbreviated names (`p`, `e`, `v`)
- Explicit types over `var` for built-in types; editorconfig enforces this where configured
- Braces `{}` on **all** `if` statements, including single-line guard returns
- No alignment-padding around `=` in initializers or assignments
- No comments unless the WHY is non-obvious
- No empty catch blocks; no swallowed exceptions
- Treat compiler warnings as errors where the project enables it — zero-warnings policy
```

- [ ] **Step 3: Write `stacks/aurelia/conventions.md`**

```markdown
## Aurelia Conventions

For Aurelia-specific component, binding, and template guidance, use the `aurelia-developer` skill — it covers components, dependency injection, the router, validation, and scaffolding in depth.

This file holds only conventions specific to this constitution's projects:

- Follow the project's existing component file layout (`.ts` + `.html` pairs) rather than introducing new patterns
- Keep this file thin — expand it via the normal update loop as Aurelia-specific project conventions emerge
```

- [ ] **Step 4: Verify the stack directory names match what Task 5 will reference**

Run: `find /home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks -name '*.md' | sort`

Expected:
```
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/aurelia/conventions.md
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/dotnet/architecture.md
/home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/dotnet/coding-standards.md
```

- [ ] **Step 5: Commit**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/assets/rules/stacks
git commit -m "Add dotnet and aurelia stack rule files"
```

---

### Task 4: Project-owned templates

**Files:**
- Create: `legislator/skill/assets/templates/CLAUDE.md.tpl`
- Create: `legislator/skill/assets/templates/okf-index.md.tpl`
- Create: `legislator/skill/assets/templates/okf-log.md.tpl`
- Create: `legislator/skill/assets/templates/backlog.md.tpl`
- Create: `legislator/skill/assets/templates/adr-0001.md.tpl`
- Create: `legislator/skill/assets/templates/adr-template.md.tpl`
- Create: `legislator/skill/assets/templates/journal-README.md.tpl`
- Create: `legislator/skill/assets/templates/changelog.md.tpl`
- Create: `legislator/skill/assets/templates/agents-README.md.tpl`

**Interfaces:**
- Consumes: the 6 `@docs/ai/rules/core/*.md` import paths (fixed strings, referenced in `CLAUDE.md.tpl`) and the `stacks/dotnet` / `stacks/aurelia` profile names from Task 3
- Produces: 9 template files with `{{PLACEHOLDER}}` tokens. Task 5's SKILL.md procedure documents exactly which placeholders it fills in and how (asking the user, reading `git remote`/solution files, or using today's date) — the placeholder names here must match the names Task 5 uses verbatim: `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, `{{STACK_SUMMARY}}`, `{{STACK_IMPORTS}}`, `{{PROJECT_ARCHITECTURE_NOTES}}`, `{{BUILD_TEST_COMMANDS}}`, `{{TODAY_ISO}}`, `{{TODAY_ISO_DATE}}`, `{{CATEGORY_MAPPING_TABLE}}`.

- [ ] **Step 1: Write `CLAUDE.md.tpl`**

```markdown
# {{PROJECT_NAME}} — Project Instructions for Claude Code

## Project Overview

{{PROJECT_OVERVIEW}}

Stack: {{STACK_SUMMARY}}

- OKF bundle: `docs/okf/` (knowledge documentation — must stay in sync with code)
- Specs and plans: `docs/superpowers/` (committed)

@docs/ai/rules/core/okf.md
@docs/ai/rules/core/pair-development.md
@docs/ai/rules/core/decision-gate.md
@docs/ai/rules/core/adr.md
@docs/ai/rules/core/dev-journal.md
@docs/ai/rules/core/changelog.md
{{STACK_IMPORTS}}

## Architecture Constraints

{{PROJECT_ARCHITECTURE_NOTES}}

## Build & Test

{{BUILD_TEST_COMMANDS}}
```

- [ ] **Step 2: Write `okf-index.md.tpl`**

```markdown
---
type: System
title: {{PROJECT_NAME}} — System Overview
description: Root of the OKF knowledge bundle — architecture, tech stack, project layout, and links to every category.
tags: [system, architecture, index]
timestamp: {{TODAY_ISO}}
status: implemented
---

# {{PROJECT_NAME}}

{{PROJECT_OVERVIEW}}

## Tech stack

{{STACK_SUMMARY}}

## What maps to what

| Change | OKF file to update |
|--------|---------------------|
{{CATEGORY_MAPPING_TABLE}}

## Changelog

All bundle changes are recorded in [log.md](log.md).
```

- [ ] **Step 3: Write `okf-log.md.tpl`**

```markdown
---
type: Changelog
title: OKF Bundle Changelog
description: Chronological record of significant changes to the OKF knowledge bundle.
tags: [changelog, okf]
timestamp: {{TODAY_ISO}}
---

# OKF Bundle Changelog

## {{TODAY_ISO_DATE}} — Bundle initialized

Initial OKF bundle scaffolded by the Legislator.
```

- [ ] **Step 4: Write `backlog.md.tpl`**

```markdown
# {{PROJECT_NAME}} — Backlog

Tasks pending implementation. **Rule: update the relevant `docs/okf/` document first, before writing any code.** When a task is fully done, remove it from here.

---
```

- [ ] **Step 5: Write `adr-0001.md.tpl`**

```markdown
# 0001. Record architecture decisions

## Status

Accepted

## Context

We need to record the architectural decisions made on this project so that future contributors (human or AI) understand why the codebase looks the way it does, not just what it looks like.

## Decision

We will use Architecture Decision Records, as described by Michael Nygard, and follow the format in `docs/adr/template.md`. Decisions are numbered sequentially and never renumbered or deleted — superseded decisions get a new ADR that references the old one.

## Consequences

Every non-trivial architecture decision gets a permanent, dated record. See `docs/ai/rules/core/adr.md` for when to write one.
```

- [ ] **Step 6: Write `adr-template.md.tpl`**

```markdown
# NNNN. {{Title}}

## Status

{{proposed | accepted | deprecated | superseded by NNNN}}

## Context

{{What is the issue that we're seeing that is motivating this decision or change?}}

## Decision

{{What is the change that we're proposing and/or doing?}}

## Consequences

{{What becomes easier or more difficult to do because of this change?}}
```

- [ ] **Step 7: Write `journal-README.md.tpl`**

```markdown
# Developer Journal

One file per working day: `YYYY-MM-DD.md`.

Each entry: what was worked on, decisions taken (cross-link ADRs), dead ends, and open questions for next time. See `docs/ai/rules/core/dev-journal.md` for the full discipline.
```

- [ ] **Step 8: Write `changelog.md.tpl`**

```markdown
# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

### Changed

### Fixed

### Removed
```

- [ ] **Step 9: Write `agents-README.md.tpl`**

```markdown
# Project-Local Mini-Agents

This folder holds project-specific specialized agents — `.claude/agents/<name>.md` files with frontmatter declaring `description`, `model`, and the tools/MCP servers the agent is allowed to use.

This convention is installed by the Legislator; the routing system that decides when to reuse an existing agent versus create a new one is a separate design (the "master-agent" cycle) and is not part of this skill.
```

- [ ] **Step 10: Verify all nine templates exist**

Run: `ls /home/admin/Repository/custom_skills/legislator/skill/assets/templates/`

Expected (9 files, any order):
```
CLAUDE.md.tpl
adr-0001.md.tpl
adr-template.md.tpl
agents-README.md.tpl
backlog.md.tpl
changelog.md.tpl
journal-README.md.tpl
okf-index.md.tpl
okf-log.md.tpl
```

- [ ] **Step 11: Commit**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/assets/templates
git commit -m "Add project-owned scaffold templates"
```

---

### Task 5: SKILL.md run procedure

**Files:**
- Create: `legislator/skill/SKILL.md`

**Interfaces:**
- Consumes: rule file paths from Task 2/3 (`assets/rules/core/*.md`, `assets/rules/stacks/{dotnet,aurelia}/*.md`), template placeholder names from Task 4, `VERSION`'s content format from Task 1
- Produces: the `docs/ai/manifest.json` schema (`legislatorVersion` int, `profiles` string array, `ownedFiles` string array relative to the target repo root) — Task 6 (migration guide) and Tasks 8–11 (acceptance tests) read this exact schema when writing verification checks.

- [ ] **Step 1: Write `SKILL.md`**

```markdown
---
name: legislator
description: Scaffold or upgrade the AI-development constitution (CLAUDE.md rules, OKF knowledge bundle, backlog, ADRs, dev journal, changelog, specs/plans, agent-space convention) in the current project repo. Use when starting a new AI-first project, migrating an existing repo to the standard layout, or re-applying an updated constitution after editing the Legislator's rule files.
---

# Legislator

Run this procedure against the **current working directory** (the target project repo). Never write outside it. Never commit — leave the diff for the user to review.

## Step 0 — Preconditions

1. Confirm the current directory looks like a project root (has a `.git` directory, or the user has explicitly confirmed this directory is the target). If neither is true, ask the user to confirm before proceeding.
2. Run `git status --porcelain`. If it reports anything, warn the user that uncommitted changes exist and ask whether to proceed (a run's diff would mix with their changes) before continuing.

## Step 1 — Detect state

Check, in this order:

1. Does `docs/ai/manifest.json` exist? → **upgrade** mode. Read it for the previously applied `legislatorVersion`, `profiles`, and `ownedFiles`.
2. Else, does `CLAUDE.md` exist at the repo root? → **legacy migration** mode.
3. Else → **fresh scaffold** mode.

## Step 2 — Determine profiles

- **Upgrade mode:** use `profiles` from the existing manifest. Do not ask again.
- **Fresh scaffold / legacy migration:** inspect the repo for stack signals — any `*.slnx`/`*.sln`/`*.csproj` file → candidate profile `dotnet`; an `aurelia` entry in `package.json` dependencies, or an `aurelia.json`/`au.config` file → candidate profile `aurelia`. Present the detected candidates to the user and ask them to confirm or adjust before proceeding. Store the confirmed list as `profiles`.

## Step 3 — Apply owned files

Owned files live in this skill package at `assets/rules/`. For each file:

1. Always copy every file under `assets/rules/core/` to `docs/ai/rules/core/` in the target repo, preserving relative path, using a byte-for-byte copy operation (e.g. `cp`) — never retype or reformat the content.
2. For each confirmed profile name in `profiles`, copy `assets/rules/stacks/<profile>/` to `docs/ai/rules/stacks/<profile>/` the same way.
3. Read `VERSION` (a single integer) — this is the `legislatorVersion` to write into the manifest.
4. Compute the new `ownedFiles` list: every path just copied, expressed relative to the target repo root (e.g. `docs/ai/rules/core/okf.md`).
5. **Deletions:** if an upgrade-mode manifest's old `ownedFiles` list contains a path not present in the new `ownedFiles` list, delete that file from the target repo (it was removed from the constitution).
6. Write `docs/ai/manifest.json`:

```json
{
  "legislatorVersion": <int from VERSION>,
  "profiles": [<confirmed profile names>],
  "ownedFiles": [<new ownedFiles list, sorted>]
}
```

## Step 4 — Scaffold missing project-owned artifacts

For each of the following, create it **only if it does not already exist** — never overwrite. Use the templates in `assets/templates/`, filling placeholders as described below. Never invent content for a placeholder without either asking the user or deriving it from the repo (see the derivation rules below) — do not leave a placeholder token unfilled in the written file.

| Target path | Template | Notes |
|---|---|---|
| `CLAUDE.md` | `CLAUDE.md.tpl` | Only in fresh-scaffold mode — legacy migration mode handles this file per Step 5 instead |
| `docs/okf/index.md` | `okf-index.md.tpl` | |
| `docs/okf/log.md` | `okf-log.md.tpl` | |
| `docs/backlog.md` | `backlog.md.tpl` | |
| `docs/adr/0001-record-architecture-decisions.md` | `adr-0001.md.tpl` | Used verbatim, no placeholders |
| `docs/adr/template.md` | `adr-template.md.tpl` | Used verbatim, no placeholders |
| `docs/journal/README.md` | `journal-README.md.tpl` | Used verbatim, no placeholders |
| `CHANGELOG.md` | `changelog.md.tpl` | Used verbatim, no placeholders |
| `.claude/agents/README.md` | `agents-README.md.tpl` | Used verbatim, no placeholders |
| `docs/superpowers/specs/` | (empty directory) | Create the directory if absent; no file |
| `docs/superpowers/plans/` | (empty directory) | Create the directory if absent; no file |

Placeholder derivation rules (fresh-scaffold mode only — legacy migration extracts these from the existing CLAUDE.md instead, per Step 5):

- `{{PROJECT_NAME}}` — ask the user, or infer from the repo directory name if unambiguous, and confirm with the user before writing.
- `{{PROJECT_OVERVIEW}}` — ask the user for a one-paragraph description.
- `{{STACK_SUMMARY}}` — derive from `profiles` (e.g. "`.NET`" for `dotnet`, "`.NET, Aurelia`" for both) plus anything the user adds.
- `{{STACK_IMPORTS}}` — one `@docs/ai/rules/stacks/<profile>/<file>.md` line per file under each confirmed profile's rule directory.
- `{{PROJECT_ARCHITECTURE_NOTES}}` — ask the user for any project-specific architecture constraints beyond the stack rules; if none, write "None beyond the stack rules imported above."
- `{{BUILD_TEST_COMMANDS}}` — ask the user for the build/test commands (e.g. `dotnet build`, `dotnet test`), or detect from solution/project files and confirm.
- `{{TODAY_ISO}}` — today's date/time in ISO 8601 (e.g. `2026-07-08T00:00:00Z`).
- `{{TODAY_ISO_DATE}}` — today's date only (e.g. `2026-07-08`).
- `{{CATEGORY_MAPPING_TABLE}}` — ask the user for the project's feature-slice-to-OKF-category mapping (mirroring the pattern: `| Change | OKF file to update |`); if the project has no slices yet, write a single row pointing everything at `docs/okf/index.md`.

## Step 5 — Legacy migration (migration mode only)

Follow `references/migration.md` in full. Summary of what it covers:

1. Split the existing CLAUDE.md into project-specific content (kept) and content now covered by an owned rule (removed, replaced by the `@docs/ai/rules/...` import block from the `CLAUDE.md.tpl` import section).
2. Extract any existing "what maps to what" / category table into `docs/okf/index.md` if `docs/okf/` already exists but lacks this file; otherwise scaffold `docs/okf/index.md` per Step 4 and fold the existing table in as `{{CATEGORY_MAPPING_TABLE}}`.
3. Relocate any plans/specs directory outside the standard location (e.g. `.claude/plans/`) into `docs/superpowers/plans/`, fixing any relative references inside the moved files.
4. Remove `docs/superpowers/` (or equivalent) from `.gitignore` if present.
5. If an existing CLAUDE.md section conflicts with an owned rule (e.g. contradictory branch-naming convention), do not resolve it — surface the conflict and ask, per the decision-gate rule.

## Step 6 — Verify

1. For every file in the new `ownedFiles` list, diff it against the corresponding `assets/rules/...` source file (e.g. `diff docs/ai/rules/core/okf.md <skill-path>/assets/rules/core/okf.md`) — must be byte-identical. If any diff is non-empty, re-copy and check again; if it still fails, stop and report the failure instead of continuing.
2. Confirm every artifact from Step 4's table exists (or was already present).

## Step 7 — Report

Print a summary with four sections: **Created** (new files/directories), **Overwritten** (owned files updated by this run), **Deleted** (owned files removed because they left the constitution), **Needs your review** (e.g. a proposed `@import` line to add/remove from CLAUDE.md when a rule file was added/removed — CLAUDE.md is project-owned, so the Legislator never edits it directly; it only proposes the exact line here for the user to add or delete themselves).

Do not run `git add` or `git commit`. The user reviews and commits.
```

- [ ] **Step 2: Verify the SKILL.md frontmatter is valid**

Run: `head -5 /home/admin/Repository/custom_skills/legislator/skill/SKILL.md`

Expected: starts with `---`, contains a `name: legislator` line and a `description:` line, closes with `---` on line 4 or 5.

- [ ] **Step 3: Commit**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/SKILL.md
git commit -m "Add SKILL.md run procedure"
```

---

### Task 6: Legacy migration reference guide

**Files:**
- Create: `legislator/skill/references/migration.md`

**Interfaces:**
- Consumes: Step 5 of `SKILL.md` (Task 5) references this file by path (`references/migration.md`) — the section headers here must match what Step 5 summarizes.
- Produces: none consumed by later tasks directly; read at run-time by whoever executes the SKILL.md procedure in migration mode (including Tasks 9's acceptance test).

- [ ] **Step 1: Write `references/migration.md`**

```markdown
# Legacy Repo Migration Guide

Detailed guidance for SKILL.md Step 5 (legacy migration mode) — when `CLAUDE.md` exists but `docs/ai/manifest.json` does not.

## 1. Splitting CLAUDE.md

Read the existing `CLAUDE.md` top to bottom and classify each section:

- **Project-specific — keep verbatim in the new CLAUDE.md:** project overview, tech stack description, project-specific architecture instances (e.g. "CareerPlatform.Domain has zero NuGet dependencies" — this is an *instance* of the generic `stacks/dotnet/architecture.md` rule and stays as a concrete callout), build/test commands, CI notes.
- **Now covered by an owned rule — remove and replace with an import:** the OKF Documentation Rule section, the Pair Development Protocol section, the Decision gate section. If the existing text differs from the owned rule's wording, that's expected — the owned rule supersedes it. Do not try to preserve project-specific phrasing of these sections; the import replaces them entirely.
- **New sections to add:** none go in CLAUDE.md itself — ADR, dev journal, and changelog disciplines are covered by their own owned rule files, imported via the same `@docs/ai/rules/core/...` block.

Rewrite CLAUDE.md as: kept project-specific content, followed by the six core `@import` lines plus one import line per file in each confirmed stack profile's rule directory (mirroring `CLAUDE.md.tpl`'s import block).

## 2. OKF "what maps to what" table

If the existing CLAUDE.md has a "What maps to what" or category-mapping table (mapping change types to `docs/okf/<category>/<concept>.md` paths), and `docs/okf/index.md` does not already contain an equivalent table, move that table into `docs/okf/index.md` under a `## What maps to what` heading — this is exactly the `{{CATEGORY_MAPPING_TABLE}}` placeholder from the `okf-index.md.tpl` template. If `docs/okf/index.md` already exists with its own mapping table (common — most legacy repos already have an OKF bundle), leave it as-is; do not duplicate or merge.

## 3. Relocating plans and specs

Some legacy repos keep implementation plans somewhere other than `docs/superpowers/plans/` — for example `.claude/plans/`. If such a directory exists:

1. Create `docs/superpowers/plans/` and `docs/superpowers/specs/` if they don't exist.
2. Move every file from the legacy location into `docs/superpowers/plans/`, preserving filenames.
3. Grep the moved files (and any other docs that reference them) for the old path and update references to the new path.
4. Leave the old directory empty (or remove it if nothing else uses it) — do not leave a stale duplicate.

## 4. Gitignore cleanup

Check `.gitignore` for an entry matching `docs/superpowers/` (or a more specific `docs/superpowers/plans/` / `docs/superpowers/specs/`). If found, remove the entry — this project's git policy commits everything the Legislator manages. Note the removal in the run's report (Step 7 of SKILL.md) since it changes what future commits include.

## 5. Conflicting sections

If a section of the existing CLAUDE.md contradicts an owned rule — for example, a branch-naming convention that differs from `pair-development.md`, or a decision-gate list with fewer/different triggers — this is exactly the kind of trade-off the decision-gate rule exists for. Do not silently pick one side. Stop, describe the conflict (quote both versions), and ask the user which should win before writing anything.

## 6. Known-repo notes

These are examples of what to expect, not an exhaustive list — always re-read the actual repo rather than assuming these apply unchanged.

- A repo with a **gitignored** `docs/superpowers/` (specs/plans kept local-only): this is precisely the pattern Section 4 above corrects — un-gitignore it and note in the report that history for those files starts now (existing local-only content should be committed as part of this migration, not lost).
- A repo with plans under **`.claude/plans/`** instead of `docs/superpowers/plans/`: apply Section 3 above.
- A repo whose CLAUDE.md already has an OKF "what maps to what" table with several project-specific slice categories (tenancy, applications, pipeline, etc.): leave that table in `docs/okf/index.md` untouched per Section 2 — it is project knowledge, not something the Legislator owns or regenerates.
```

- [ ] **Step 2: Verify the file exists and section headers match SKILL.md's summary**

Run: `grep -E '^## ' /home/admin/Repository/custom_skills/legislator/skill/references/migration.md`

Expected:
```
## 1. Splitting CLAUDE.md
## 2. OKF "what maps to what" table
## 3. Relocating plans and specs
## 4. Gitignore cleanup
## 5. Conflicting sections
## 6. Known-repo notes
```

- [ ] **Step 3: Commit**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/references/migration.md
git commit -m "Add legacy repo migration reference guide"
```

---

### Task 7: Local install

**Files:**
- No new files — creates a symlink outside the repo (`~/.claude/skills/legislator`).

**Interfaces:**
- Consumes: `legislator/skill/` (the complete package from Tasks 1–6)
- Produces: an installed, invocable `/legislator` skill, required by Tasks 8–11's acceptance tests.

- [ ] **Step 1: Check for a pre-existing skill at the target path**

Run: `ls -la ~/.claude/skills/legislator 2>&1`

Expected: either "No such file or directory" (proceed to Step 2), or an existing symlink/directory (stop and ask the user how to proceed — do not overwrite without confirmation).

- [ ] **Step 2: Create the symlink**

```bash
ln -s /home/admin/Repository/custom_skills/legislator/skill ~/.claude/skills/legislator
```

- [ ] **Step 3: Verify the symlink resolves correctly**

Run: `readlink -f ~/.claude/skills/legislator`

Expected:
```
/home/admin/Repository/custom_skills/legislator/skill
```

- [ ] **Step 4: Verify SKILL.md is reachable through the symlink**

Run: `head -3 ~/.claude/skills/legislator/SKILL.md`

Expected:
```
---
name: legislator
description: Scaffold or upgrade the AI-development constitution (CLAUDE.md rules, OKF knowledge bundle, backlog, ADRs, dev journal, changelog, specs/plans, agent-space convention) in the current project repo. Use when starting a new AI-first project, migrating an existing repo to the standard layout, or re-applying an updated constitution after editing the Legislator's rule files.
```

No commit needed for this task (nothing changed inside the repo).

---

### Task 8: Acceptance test — fresh scaffold on an empty directory

**Files:**
- Create (in a scratch location, not the skill repo): `/tmp/legislator-accept/fresh/` and its scaffolded contents.

**Interfaces:**
- Consumes: the installed skill from Task 7, `VERSION`'s value from Task 1 (expected `1`)
- Produces: a verified reference example of `docs/ai/manifest.json`'s exact shape, used as the baseline Task 10 diffs against after an upgrade.

- [ ] **Step 1: Create and enter a clean scratch directory**

```bash
rm -rf /tmp/legislator-accept/fresh
mkdir -p /tmp/legislator-accept/fresh
cd /tmp/legislator-accept/fresh
git init -q
```

- [ ] **Step 2: Run the procedure**

As the acting agent, follow the full procedure in `~/.claude/skills/legislator/SKILL.md` against the current directory (`/tmp/legislator-accept/fresh`), invoked via `/legislator`. Since the directory is empty (no `CLAUDE.md`, no manifest), this exercises **fresh scaffold** mode. When asked for profile confirmation, answer `dotnet` only (no aurelia). When asked for `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, `{{BUILD_TEST_COMMANDS}}`, and `{{PROJECT_ARCHITECTURE_NOTES}}`, supply any reasonable placeholder answers (e.g. project name `AcceptanceTestApp`, overview "A test project for verifying the Legislator skill.", build/test `dotnet build` / `dotnet test`, architecture notes "None beyond the stack rules imported above.").

- [ ] **Step 3: Verify every expected artifact exists**

Run:
```bash
cd /tmp/legislator-accept/fresh
find . -type f -not -path './.git/*' | sort
```

Expected (paths, order may vary — must include every one of these):
```
./.claude/agents/README.md
./CHANGELOG.md
./CLAUDE.md
./docs/adr/0001-record-architecture-decisions.md
./docs/adr/template.md
./docs/ai/manifest.json
./docs/ai/rules/core/adr.md
./docs/ai/rules/core/changelog.md
./docs/ai/rules/core/decision-gate.md
./docs/ai/rules/core/dev-journal.md
./docs/ai/rules/core/okf.md
./docs/ai/rules/core/pair-development.md
./docs/ai/rules/stacks/dotnet/architecture.md
./docs/ai/rules/stacks/dotnet/coding-standards.md
./docs/backlog.md
./docs/journal/README.md
./docs/okf/index.md
./docs/okf/log.md
```

Also verify the two empty directories exist: `test -d docs/superpowers/specs && test -d docs/superpowers/plans && echo "both present"` → expected output `both present`.

- [ ] **Step 4: Verify the manifest shape**

Run: `cat /tmp/legislator-accept/fresh/docs/ai/manifest.json`

Expected: valid JSON matching this shape (whitespace may differ):
```json
{
  "legislatorVersion": 1,
  "profiles": ["dotnet"],
  "ownedFiles": [
    "docs/ai/rules/core/adr.md",
    "docs/ai/rules/core/changelog.md",
    "docs/ai/rules/core/decision-gate.md",
    "docs/ai/rules/core/dev-journal.md",
    "docs/ai/rules/core/okf.md",
    "docs/ai/rules/core/pair-development.md",
    "docs/ai/rules/stacks/dotnet/architecture.md",
    "docs/ai/rules/stacks/dotnet/coding-standards.md"
  ]
}
```

- [ ] **Step 5: Verify owned files are byte-identical to the source**

```bash
diff -r /tmp/legislator-accept/fresh/docs/ai/rules/core /home/admin/Repository/custom_skills/legislator/skill/assets/rules/core
diff -r /tmp/legislator-accept/fresh/docs/ai/rules/stacks/dotnet /home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/dotnet
```

Expected: no output from either command (identical directory contents).

- [ ] **Step 6: Verify CLAUDE.md has no unfilled placeholders**

Run: `grep -o '{{[A-Z_]*}}' /tmp/legislator-accept/fresh/CLAUDE.md`

Expected: no output (no matches — every placeholder was filled).

- [ ] **Step 7: Run the procedure a second time and verify zero diff (idempotency)**

```bash
cd /tmp/legislator-accept/fresh
git add -A
git commit -q -m "checkpoint before second run"
```

As the acting agent, follow the full procedure again against `/tmp/legislator-accept/fresh` (now in upgrade mode, since the manifest exists — profiles read from the manifest, no questions asked).

```bash
git status --porcelain
```

Expected: no output (clean — the second run produced zero changes).

- [ ] **Step 8: Record the result — no commit in the skill repo for this task**

This task validates behavior; it does not modify the `legislator` repo, so there is nothing to commit there. If Steps 3–7 all passed, mark this task complete.

---

### Task 9: Acceptance test — legacy migration on a scratch copy of RKruiterApi

**Files:**
- Create (scratch, not the skill repo): `/tmp/legislator-accept/rkruiter/` (a copy of `/home/admin/Repository/RKruiterApi`).

**Interfaces:**
- Consumes: the installed skill from Task 7, `references/migration.md` from Task 6
- Produces: nothing consumed by later tasks — this is a terminal verification.

- [ ] **Step 1: Create a scratch copy of the real repo (never operate on the original)**

```bash
rm -rf /tmp/legislator-accept/rkruiter
cp -r /home/admin/Repository/RKruiterApi /tmp/legislator-accept/rkruiter
cd /tmp/legislator-accept/rkruiter
```

- [ ] **Step 2: Confirm the scratch copy has the expected starting shape**

Run: `test -f CLAUDE.md && test ! -f docs/ai/manifest.json && echo "legacy migration mode expected"`

Expected: `legacy migration mode expected`

- [ ] **Step 3: Run the procedure**

As the acting agent, follow the full procedure in `~/.claude/skills/legislator/SKILL.md` against `/tmp/legislator-accept/rkruiter`. Because `CLAUDE.md` exists and `docs/ai/manifest.json` does not, this exercises **legacy migration** mode — follow `references/migration.md` for Step 5. Detected profile should be `dotnet` (confirm it). Since `docs/okf/index.md` already exists in this repo with its own content, do **not** scaffold it from the template — leave it untouched per migration guide Section 2. Extract project-specific content from the existing `CLAUDE.md` (overview, architecture constraints, coding standards, build/test commands) and rewrite it with the `@docs/ai/rules/...` import block replacing the OKF/pair-development/decision-gate sections.

- [ ] **Step 4: Verify CLAUDE.md retains project-specific content and gains the import block**

```bash
grep -q "RKruiterApi" CLAUDE.md && echo "project content kept"
grep -q "@docs/ai/rules/core/okf.md" CLAUDE.md && echo "core import present"
grep -q "@docs/ai/rules/core/pair-development.md" CLAUDE.md && echo "pair-dev import present"
grep -q "@docs/ai/rules/core/decision-gate.md" CLAUDE.md && echo "decision-gate import present"
```

Expected: all four lines print (`project content kept`, `core import present`, `pair-dev import present`, `decision-gate import present`).

- [ ] **Step 5: Verify the pre-existing OKF bundle was left untouched**

```bash
diff /tmp/legislator-accept/rkruiter/docs/okf/index.md /home/admin/Repository/RKruiterApi/docs/okf/index.md
```

Expected: no output (file unchanged from the original).

- [ ] **Step 6: Verify owned rule files were created and are byte-identical to source**

```bash
diff -r /tmp/legislator-accept/rkruiter/docs/ai/rules/core /home/admin/Repository/custom_skills/legislator/skill/assets/rules/core
diff -r /tmp/legislator-accept/rkruiter/docs/ai/rules/stacks/dotnet /home/admin/Repository/custom_skills/legislator/skill/assets/rules/stacks/dotnet
```

Expected: no output from either command.

- [ ] **Step 7: Verify the manifest was written**

Run: `cat /tmp/legislator-accept/rkruiter/docs/ai/manifest.json`

Expected: valid JSON with `"legislatorVersion": 1`, `"profiles": ["dotnet"]`, and an `ownedFiles` array listing the 8 core+dotnet rule paths (same shape as Task 8 Step 4).

- [ ] **Step 8: Verify newly-scaffolded artifacts that RKruiterApi lacked (ADR, journal, changelog) now exist**

Run:
```bash
test -f /tmp/legislator-accept/rkruiter/docs/adr/0001-record-architecture-decisions.md && echo "ADR present"
test -f /tmp/legislator-accept/rkruiter/docs/journal/README.md && echo "journal present"
test -f /tmp/legislator-accept/rkruiter/CHANGELOG.md && echo "changelog present"
```

Expected: all three lines print.

- [ ] **Step 9: Run the procedure a second time and verify zero diff (idempotency)**

```bash
cd /tmp/legislator-accept/rkruiter
git add -A
git commit -q -m "checkpoint before second run"
```

As the acting agent, follow the procedure again (now upgrade mode). Then:

```bash
git status --porcelain
```

Expected: no output.

- [ ] **Step 10: Record the result — no commit in the skill repo for this task**

Nothing in the `legislator` repo changes here. If Steps 2–9 all passed, mark this task complete. Do not push or commit anything from the scratch copy — it is disposable.

---

### Task 10: Acceptance test — upgrade propagation

**Files:**
- Modify (real, permanent — a genuine rule improvement, not reverted; see Step 7): `legislator/skill/assets/rules/core/decision-gate.md`
- Modify (real, permanent — see Step 7): `legislator/skill/VERSION`

**Interfaces:**
- Consumes: the fresh-scaffold scratch repo from Task 8 (`/tmp/legislator-accept/fresh`), already committed at the end of Task 8 Step 7
- Produces: nothing consumed later — terminal verification. This task proves the "edit rule → bump VERSION → re-run → only that file changes" update loop described in the spec's Adaptability model.

- [ ] **Step 1: Make a real edit to one owned rule file**

Append one line to `legislator/skill/assets/rules/core/decision-gate.md`:

```markdown

- A migration or upgrade step that would delete data without an explicit backup
```

(This appends a 7th bullet to the existing list — a genuine, permanent improvement to the rule, not a throwaway test edit. Keep it.)

- [ ] **Step 2: Bump VERSION**

Change the content of `legislator/skill/VERSION` from `1` to `2`.

- [ ] **Step 3: Re-run the procedure against the Task 8 scratch repo**

```bash
cd /tmp/legislator-accept/fresh
git status --porcelain
```

Expected: no output (clean, from Task 8 Step 7's commit).

As the acting agent, follow the procedure again against `/tmp/legislator-accept/fresh` (upgrade mode — profiles read from its existing manifest).

- [ ] **Step 4: Verify only the changed rule file and the manifest changed**

```bash
cd /tmp/legislator-accept/fresh
git status --porcelain
```

Expected exactly two changed paths (order may vary):
```
 M docs/ai/manifest.json
 M docs/ai/rules/core/decision-gate.md
```

- [ ] **Step 5: Verify the manifest's `legislatorVersion` updated to 2**

Run: `grep legislatorVersion /tmp/legislator-accept/fresh/docs/ai/manifest.json`

Expected: `"legislatorVersion": 2,`

- [ ] **Step 6: Verify the updated rule file matches the new source exactly**

```bash
diff /tmp/legislator-accept/fresh/docs/ai/rules/core/decision-gate.md /home/admin/Repository/custom_skills/legislator/skill/assets/rules/core/decision-gate.md
```

Expected: no output.

- [ ] **Step 7: Commit the rule improvement and VERSION bump in the skill repo (this is a real, permanent change — not reverted)**

```bash
cd /home/admin/Repository/custom_skills/legislator
git add skill/assets/rules/core/decision-gate.md skill/VERSION
git commit -m "Add data-loss-without-backup to the decision gate; bump VERSION to 2"
```

---

### Task 11: Acceptance test — rule removal propagation

**Files:**
- Create then delete (never committed — a throwaway rule used only to prove deletion propagates): `legislator/skill/assets/rules/core/scratch-temp-rule.md`
- Modify (real, permanent — VERSION ends at 4, not reverted; see Step 10): `legislator/skill/VERSION`

**Interfaces:**
- Consumes: the fresh-scaffold scratch repo from Task 10 (`/tmp/legislator-accept/fresh`, now at manifest version 2)
- Produces: nothing — terminal verification of the deletion behavior described in SKILL.md Step 3.5 and the spec's rule-removal test.

- [ ] **Step 1: Add a throwaway rule file and bump VERSION to 3**

Content of `legislator/skill/assets/rules/core/scratch-temp-rule.md`:

```markdown
## Scratch Temporary Rule

This file exists only to verify that the Legislator deletes owned files when they are removed from the constitution. It should never appear in a real downstream project for longer than one acceptance-test run.
```

Change `legislator/skill/VERSION` from `2` to `3`.

- [ ] **Step 2: Run the procedure against the scratch repo to pick up the new rule**

```bash
cd /tmp/legislator-accept/fresh
git add -A
git commit -q -m "checkpoint before add-then-remove rule test"
```

As the acting agent, follow the procedure against `/tmp/legislator-accept/fresh` (upgrade mode).

- [ ] **Step 3: Verify the new rule file was added**

Run: `test -f /tmp/legislator-accept/fresh/docs/ai/rules/core/scratch-temp-rule.md && echo "present"`

Expected: `present`

- [ ] **Step 4: Delete the throwaway rule file from the skill and bump VERSION to 4**

```bash
rm /home/admin/Repository/custom_skills/legislator/skill/assets/rules/core/scratch-temp-rule.md
```

Change `legislator/skill/VERSION` from `3` to `4`.

- [ ] **Step 5: Run the procedure again against the scratch repo**

As the acting agent, follow the procedure against `/tmp/legislator-accept/fresh` (upgrade mode) once more.

- [ ] **Step 6: Verify the file was deleted from the target and the manifest no longer lists it**

```bash
test ! -f /tmp/legislator-accept/fresh/docs/ai/rules/core/scratch-temp-rule.md && echo "deleted as expected"
grep -c scratch-temp-rule /tmp/legislator-accept/fresh/docs/ai/manifest.json
```

Expected: `deleted as expected`, then `0` (grep count of zero matches).

- [ ] **Step 7: Verify the manifest's `legislatorVersion` is now 4**

Run: `grep legislatorVersion /tmp/legislator-accept/fresh/docs/ai/manifest.json`

Expected: `"legislatorVersion": 4,`

- [ ] **Step 8: Verify the run's report flagged nothing to add/remove from CLAUDE.md incorrectly**

Since `scratch-temp-rule.md` was a `core/` file added and removed within the same test and never had a corresponding `@import` line added to any CLAUDE.md (core imports are the fixed 6-line block from `CLAUDE.md.tpl`, not per-file), confirm no import line was added for it:

```bash
grep -c scratch-temp-rule /tmp/legislator-accept/fresh/CLAUDE.md
```

Expected: `0`

- [ ] **Step 9: Clean up the scratch acceptance directories**

```bash
rm -rf /tmp/legislator-accept
```

- [ ] **Step 10: Commit the VERSION bump in the skill repo (the scratch rule file itself was already removed in Step 4 and never committed)**

```bash
cd /home/admin/Repository/custom_skills/legislator
git status --porcelain
```

Confirm `skill/assets/rules/core/scratch-temp-rule.md` never appears (it was created only in Step 1 and deleted in Step 4, both before any commit in the skill repo) — expected: it does not appear in the status output.

```bash
git add skill/VERSION
git commit -m "Bump VERSION to 4 after rule-removal acceptance test"
```

---

## Self-Review Notes

**Spec coverage check** — every section of `docs/superpowers/specs/2026-07-08-legislator-design.md` maps to a task:

- Skill package layout → Task 1
- Rule content sources (core) → Task 2
- Rule content sources (stacks) → Task 3
- Templates → Task 4
- Run procedure (detect/apply/scaffold/migrate/verify/report) → Task 5
- Legacy migration guide → Task 6
- Install → Task 7
- Testing/verification plan (fresh scaffold, legacy migration, upgrade propagation, rule removal) → Tasks 8, 9, 10, 11
- Agent space (cycle-2 hook) → `agents-README.md.tpl` in Task 4, scaffolded in Task 5/8
- Adaptability model (edit → bump VERSION → re-run → review diff) → Tasks 10 and 11 exercise this end-to-end
- Error handling (uncommitted changes warning, conflicting-section decision gate) → SKILL.md Step 0 and Step 5 (Task 5), migration guide Section 5 (Task 6)

**Real-repo migration** (CareerPlatform and RKruiterApi themselves, not scratch copies) is intentionally **out of scope for this plan** — the spec calls for that to happen "after acceptance, one repo at a time, reviewed via git diff," as a follow-up once this plan's tasks are all green.
