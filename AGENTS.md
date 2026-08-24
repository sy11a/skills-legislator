# Legislator — Project Instructions

## Project Overview

This repo develops the `legislator` Claude Code skill. `skill/` is the shipped
package (symlinked into `~/.claude/skills/legislator`); `evals/` is its
regression suite; `tools/` holds the fleet and eval runners; `plugin/` holds
the two deterministic enforcement arms; `docs/` holds this repo's own backlog,
cases and historical specs/plans.

Stack: Python, Bash and Markdown — no stack rules apply (`stacks` is empty).

This repository is **fleet member #0**: it is legislated by the very
constitution it produces. The law under `docs/ai/rules/**` is a delivered copy
of `skill/assets/rules/**`, never edited in place.

- OKF bundle: `docs/okf/` (knowledge documentation — must stay in sync with code)
- Domain glossary: `docs/okf/glossary.md` — check it when a term is unclear; add terms as they emerge
- Project-specific rules: `.claude/rules/` — one law file per topic (auto-loaded by Claude Code; opencode loads them via `opencode.json`'s `instructions`); read `docs/ai/rules/core/project-rules.md` before adding one
- Cases: `docs/cases/BL-NNN-short-description/` — every unit of work; `docs/superpowers/` holds pre-v17 specs and plans as legacy history

## Project Conventions

- **Branch naming:** `bl/NNN-short-description`, matching the case directory
  (`docs/cases/BL-NNN-short-description/`). This is the backlog-ticket
  convention `docs/ai/rules/core/pair-development.md` defers to. Branches
  predating v17 use `feature/bl-NNN-...` and are left as they are.
- **One MR per version.** Commits stay small and well described; the merge
  request is a version delivery, not an increment.
- **Release runbook:** bump `skill/VERSION` → benchmark → deliver to this repo
  → byte-verify → sweep the fleet. See `README.md`.

@docs/ai/rules/core/okf.md
@docs/ai/rules/core/pair-development.md
@docs/ai/rules/core/decision-gate.md
@docs/ai/rules/core/adr.md
@docs/ai/rules/core/dev-journal.md
@docs/ai/rules/core/changelog.md
@docs/ai/rules/core/artifact-lifecycle.md
@docs/ai/rules/core/project-rules.md
@docs/ai/rules/core/sdd.md
@docs/ai/rules/core/skills.md
@docs/ai/rules/core/verification.md
@docs/okf/codebase-map.md

## Architecture Constraints

The law stratum is one-way: `skill/assets/rules/**` is the only source, and
every delivered copy — here and across the fleet — is byte-identical to it.
While an edition v(N+1) is under development on a branch, this repo stays
legislated at v(N); that skew is branch-normal. Owned-integrity drift on the
default branch is a finding.

## Boundaries

Generated build output and eval run artifacts only — `evals/fixtures/**/bin/`,
`evals/fixtures/**/obj/`, `evals/__pycache__/`, and everything `.gitignore`
names under eval run output. Do not edit generated files. `docs/superpowers/**`
is not generated but is equally no-touch: it is retired history, redactable but
never rewritten (`.claude/rules/records.md`).

## Build & Test

- Every commit: `python3 evals/check_static.py` and `python3 evals/check_engine.py`
- Enforcement arms: `python3 evals/check_hooks.py` and `node evals/check_opencode_plugin.mjs`
- Behavioral change to `skill/`: the full e2e benchmark per `evals/README.md`
  (`python3 evals/setup_workspace.py <ws>` then `tools/evals-bg.sh <ws>`)
- The bar these serve is `.claude/rules/evals.md` and `evals/POLICY.md`.
