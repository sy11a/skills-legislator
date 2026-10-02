# {{PROJECT_NAME}} — Project Instructions

## Project Overview

{{PROJECT_OVERVIEW}}

Stack: {{STACK_SUMMARY}}

- OKF bundle: `docs/okf/` (knowledge documentation — must stay in sync with code)
- Domain glossary: `docs/okf/glossary.md` — check it when a term is unclear; add terms as they emerge
- Project-specific rules: `.claude/rules/` — one law file per topic (auto-loaded by Claude Code; opencode loads them via `opencode.json`'s `instructions`); read `docs/ai/rules/core/project-rules.md` before adding one
- Specs and plans: `docs/superpowers/` (committed)
- Task tracker: the project's sources note records it (kind `tracker`); `docs/backlog.md` is the work-item home only while no tracker is recorded
- Development law mode: pair

@docs/ai/rules/core/pair-development.md
@docs/ai/rules/core/decision-gate.md
{{STACK_IMPORTS}}

### Read on demand

- Before changing code that implements a concept, read `docs/ai/rules/core/okf.md` — it is law, not a reference.
- Before starting any unit of work or merging, read `docs/ai/rules/core/sdd.md` — it is law, not a reference.
- Before writing tests or implementation code, and before reporting done, read `docs/ai/rules/core/verification.md` — it is law, not a reference.
- Before the first commit on a task branch, or touching `CHANGELOG.md`/`docs/changes/`, read `docs/ai/rules/core/changelog.md` — it is law, not a reference.
- When creating, deleting or reporting on an artifact, read `docs/ai/rules/core/artifact-lifecycle.md` — it is law, not a reference.
- At each stage boundary (plan, implement, debug, review), or before invoking a skill, read `docs/ai/rules/core/skills.md` — it is law, not a reference.
- When writing a fragment's `## journal` section, or editing `docs/journal/` directly, read `docs/ai/rules/core/dev-journal.md` — it is law, not a reference.
- When closing a decision-gate stop, introducing a new invariant, or deliberately keeping an accepted antipattern or tradeoff, read `docs/ai/rules/core/adr.md` — it is law, not a reference.
- When finding where something lives, read `docs/okf/codebase-map.md` — it is law, not a reference.

## Architecture Constraints

{{PROJECT_ARCHITECTURE_NOTES}}

## Boundaries

{{BOUNDARIES}}

## Build & Test

{{BUILD_TEST_COMMANDS}}