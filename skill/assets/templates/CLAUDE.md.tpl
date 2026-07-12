# {{PROJECT_NAME}} — Project Instructions for Claude Code

## Project Overview

{{PROJECT_OVERVIEW}}

Stack: {{STACK_SUMMARY}}

- OKF bundle: `docs/okf/` (knowledge documentation — must stay in sync with code)
- Domain glossary: `docs/okf/glossary.md` — check it when a term is unclear; add terms as they emerge
- Project-specific rules: `.claude/rules/` — one law file per topic, auto-loaded every session; read `docs/ai/rules/core/project-rules.md` before adding one
- Specs and plans: `docs/superpowers/` (committed)

@docs/ai/rules/core/okf.md
@docs/ai/rules/core/pair-development.md
@docs/ai/rules/core/decision-gate.md
@docs/ai/rules/core/adr.md
@docs/ai/rules/core/dev-journal.md
@docs/ai/rules/core/changelog.md
@docs/ai/rules/core/project-rules.md
@docs/ai/rules/core/skills.md
@docs/ai/rules/core/verification.md
{{STACK_IMPORTS}}
@docs/okf/codebase-map.md

## Architecture Constraints

{{PROJECT_ARCHITECTURE_NOTES}}

## Boundaries

{{BOUNDARIES}}

## Build & Test

{{BUILD_TEST_COMMANDS}}
