# 0009. Task entry defaults to the unattended route (`/autoflow`)

## Status

accepted

## Context

dev-flow BL-051's ruling 11 asked for one thing, paid in two halves: the
`/autoflow` skill itself (dev-flow's own repo, shipped and running daily
since 2026-09-05), and one line in this constitution naming it the default
way a task is opened. Case `docs/cases/L-2-autoflow-entry-line/`; the
cross-repo initiator is Architector's Release 0 kernel launch (its
`docs/cases/BL-008-release-cycle/` epic E-5, story S-5.1, scenario
A-5.1.1): "as any agent opening a task, read one fleet-law line naming the
default entry route."

The decision-gate question this would ordinarily raise — does the
constitution get to name a specific skill's command names, and what happens
in a repository that never installed that skill pair — is answered in
advance by the operator's ruling on this task: name `/autoflow` and `/flow`
directly, exactly as `.claude/rules/skills.md` already names concrete
installed skills per repo, and make the line inert (not an error) wherever
the pair is absent.

## Decision

1. **One line in `core/pair-development.md`**: task entry defaults to the
   unattended route (`/autoflow`); the operator names `/flow` to run a task
   attended instead. The line is conditioned on the skill pair being
   installed — silent, not a violation, in a repository without it — the
   same posture `core/skills.md`'s stage routing already takes toward an
   unmapped or uninstalled skill.
2. **This repository's own `.claude/rules/skills.md` gains a `flow-sessions`
   class, `autoflow` first** — the concrete instance of the law line for
   fleet member #0, since the skill is in fact installed here. Per
   `core/project-rules.md`, `.claude/rules/skills.md` is instance data
   (created once, edited directly per repository, never machine-regenerated
   on every run) — this is a direct edit of that home, not a change to the
   delivered law stratum. Ruling 11's "generated stage map" half is paid
   here too: the `{{SANCTIONED_SKILLS_BY_STAGE}}` derivation bullet in
   `skill/SKILL.md` pins `flow-sessions` — `autoflow` (task entry;
   unattended default per `core/pair-development.md`) — as the first stage
   affinity, so a fresh legislated repository's generated
   `.claude/rules/skills.md` emits the `flow-sessions` class wherever
   `autoflow` is installed (include-only-when-installed, like every pinned
   affinity). The committed decision covered only the instance half; this
   sentence completes the pair.
3. **No `skill/VERSION` bump in this task.** `constitution-source.md` ties a
   `skill/assets/rules/**` edit to a version bump, but naming an edition is
   the operator's plan (editions are assigned at merge, never reserved —
   `docs/backlog.md`'s note on v25). The bump is proposed in the pull
   request that carries this ADR, not decided here.

## Consequences

- Easier: any agent opening a task in a legislated repository that has
  dev-flow's `/autoflow`/`/flow` pair installed reads one line settling
  which route is the default, instead of the convention living only in
  dev-flow's own documentation.
- Harder: nothing structural — the line is additive and degrades to silence
  where the skill pair is absent.
- Deferred: delivering this line into `docs/ai/rules/**` (here and across
  the fleet) waits on the operator's edition decision and the ordinary
  release runbook (bump → benchmark → deliver → byte-verify → sweep); this
  task changes only the source (`skill/assets/rules/**`) and this
  repository's own instance data.
