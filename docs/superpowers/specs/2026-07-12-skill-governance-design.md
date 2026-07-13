# Skill governance + verification law — design (BL-022, constitution v12)

**Status:** approved 2026-07-12 (plan-mode approval: govern + prune, one
combined cycle, fleet backfill). Historical record — do not rewrite.

## Problem

The user installed the mattpocock skill pack (37 skills) and hit
skill-hell risk. A full sweep found the pack is a **parallel
constitution**: its own work intake (GitHub issues / `.scratch/` vs
`docs/backlog.md`), its own knowledge stores (`CONTEXT.md`,
`CONTEXT-MAP.md`, `UBIQUITOUS_LANGUAGE.md` vs `docs/okf/glossary.md`),
inline mid-conversation ADRs (vs the same-task numbered ADR law),
auto-committing skills with no approval gate or co-author guard, and
parallel-agent assumptions that break WIP-1. Root cause is not the pack:
**the constitution had no skill↔law precedence rule** — any installed
skill could out-instruct the law. Second gap surfaced by the same review:
**no verification/testing law** — the build/test gate lived only in the
dotnet-refactoring skill, and CareerPlatform had already grown its own
"e2e coverage" project rule (harvest signal).

## Sweep verdicts (2026-07-12, all 37 SKILL.md files read)

- **CONFLICT — pruned from `~/.claude/skills/`:** `setup-matt-pocock-skills`
  (installs the parallel layout: `docs/agents/*.md`, CLAUDE.md rule block),
  `ask-matt` (router over the competing lifecycle), `implement`
  (auto-commits, no gates), `setup-pre-commit` (npm/Husky on a .NET fleet,
  Prettier fights format-on-edit hook, auto-commits), `scaffold-exercises`
  (TS course tooling, auto-commits), `setup-ts-deep-modules` (TS-only,
  injects CLAUDE.md rule line), `migrate-to-shoehorn` (TS-only), `loop-me`
  (root NOTES.md + workflows/ stray docs), `wizard`, and the
  writing/teaching/personal set (`teach`, `edit-article`, `writing-beats`,
  `writing-fragments`, `writing-great-skills`, `obsidian-vault`).
- **CONFLICT — kept but governed by the new law (outputs redirected):**
  `qa`, `to-tickets`, `triage`, `request-refactor-plan`, `wayfinder`
  (issue-tracker intake → backlog entries; parallel-ticket assumptions
  overridden by WIP-1), `domain-modeling`, `ubiquitous-language`,
  `grill-with-docs`, `improve-codebase-architecture` (CONTEXT.md /
  UBIQUITOUS_LANGUAGE.md / inline-ADR writes → glossary rows + ADR law),
  `git-guardrails-claude-code` (hook install = decision-gate stop;
  settings.json merge risk vs legislator-hooks), `claude-handoff`
  (background agent must still honor gates), `resolving-merge-conflicts`
  ("never abort" posture bends to the decision gate).
- **COMPLEMENT — sanctioned:** `grilling`, `grill-me` (pre-plan
  stress-test), `tdd` (no auto-commit; operates after the plan gate),
  `diagnosing-bugs` (clean debug loop, no repo pollution),
  `design-an-interface`, `codebase-design` (design lenses; do not replace
  stack law), `prototype` (throwaway, out of main), `handoff` (temp-dir
  only), `research` (governed: output lands under `docs/okf/`).

## Decisions

1. **New core rule `skills.md` (skill discipline).** Law beats skills;
   output redirection table (issues→backlog, plans→superpowers/plans,
   mid-skill decisions→decision gate + ADR law, foreign glossaries→
   `docs/okf/glossary.md`, research→`docs/okf/`); no skill may commit/
   push/merge/tag/file external issues on its own authority; hook or
   settings/CLAUDE.md installs are decision-gate stops; per-repo
   sanction/discourage lists in `.claude/rules/skills.md`.
2. **New core rule `verification.md` (verification ladder).** Task done
   only when exercised at the strongest available level; tests at the
   boundary where behavior lives (unit/integration/e2e); UI flows driven
   through the repo's configured browser tooling (MCP) before "done";
   mock externals at the boundary, never types you own; direct DB access
   during verification is read-only; test hygiene (no order dependence,
   no sleeps, deterministic data); zero errors/warnings + green tests
   before completion, failures reported verbatim; concrete bindings
   (MCP servers, URLs, read-only DSNs, commands) are per-repo instance
   data in `.claude/rules/verification.md`.
3. **Check 9 foreign list** gains the parallel-constitution artifacts:
   `CONTEXT.md`, `CONTEXT-MAP.md`, `UBIQUITOUS_LANGUAGE.md`, root
   `NOTES.md`, `docs/agents/`, `.scratch/` (law-shaped ones elevate to
   Warning per the v10 rider). Restructure §1 routes foreign glossary
   stores into `docs/okf/glossary.md` rows.
4. **BL-021 riders ship in this cycle:** pair-development "unmerged,
   unparked"; glossary derivation floor "up to 15, typically 5+ when the
   repo evidences them"; IServiceScopeFactory carve-out conditioned on a
   longer-lived service reaching shorter-lived ones; check 13 borrows
   check 6's pinned ignore list; `glossary_rows()` scoped to the term
   table; check 12 scans `.github/*.md` (resolving the dead-text
   exemption).
5. **Legislator repo self-rule (user item 5):** repo CLAUDE.md now bans AI
   co-author trailers explicitly (fleet law applies to this repo too).

## Eval plan

Upgrade fixture auto-withholds `verification.md` (new alphabetically-last
core rule) — the upgrade scenario delivers exactly this release and
proposes its import line. Rotted fixture plants defect 14: root
`UBIQUITOUS_LANGUAGE.md` with one law-shaped generic line and one
project-instance line → check 9 flags it at Warning (foreign +
law-shaped); restructure merges it to glossary rows and removes it;
candidates section carries the generic line and never the instance line.
Counts 13→14 defects (checks stay 13). Full benchmark →
`evals/benchmarks/v12.md` vs v11's 103.

## Out of scope

- Editing any mattpocock skill in place (they update upstream; law
  governs them instead).
- A skills-audit check (detecting installed conflicting skills) — the
  audit sees repos, not `~/.claude/skills`; revisit if governance proves
  insufficient.
- Stack-specific test-framework law (`stacks/dotnet/testing.md`) — the
  core ladder + per-repo bindings cover it; add only if repos drift.

## Downstream (phase 2)

CareerPlatform, RKruiterApi, RKruiterAgent: v12 upgrade (two new import
lines applied as the user's hand) + drafted `.claude/rules/verification.md`
(bindings derived from each repo's docker-compose/config; DSNs by env-var
name, never literal secrets) + `.claude/rules/skills.md` (sanctioned/
discouraged lists). Worktree + PR pattern; merges stay with the user.
