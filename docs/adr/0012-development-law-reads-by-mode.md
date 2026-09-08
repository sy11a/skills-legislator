# 0012. The development law reads by mode: `pair` and `waterflow`

## Status

accepted

## Context

Architector's Release 0 kernel (its `docs/cases/BL-008-release-cycle/`, epic
E-5, story S-5.2, scenarios A-5.2.1/A-5.2.2) hands this repository the second
half of the waterflow decision: "the law states both modes and how the three
cornerstone rules read under each; a repository's entry document declares its
mode; a waterflow repository is checked for naming its release-branch
convention; the integration-branch merge stays the operator's." The backlog row
is `docs/backlog.md` `## BL-088` (PROPOSED, hold discharged 2026-09-08); the
cross-repo half is Architector ADR 0007 (status proposed there — "the ruling
and the edition are legislator's"). The concept was swept in stage 2
(`research.md`): settled, greenfield — no code anywhere implements it.

The decision-gate question the mode raises — does a repository's own entry
document get to change how its development law reads — is answered in advance
by the frozen contract: yes, per repository, `pair` the default and inert
wherever a repository declares nothing, exactly the "name it, default stays
innocuous" pattern every other named default in the law already governs. The
detection must stay closed-token substring matching (the convention checks 18
`tracker-drift` and 14 `skill-bindings` hold), so the engine executes the
check and the fixture corpus can plant a deterministic defect for it.

## Decision

1. **The law states both modes** (`core/pair-development.md`): a Mode preamble
   names `pair` (the default, reading exactly as today's text) and `waterflow`;
   the three cornerstone rules — one task at a time, never merge yourself, no
   next task without approval — each carry their `waterflow` reading; the
   integration-branch merge stays the operator's in both modes. The `pair`
   reading is today's text, never reworded.
2. **The entry document declares the mode**: `AGENTS.md.tpl` gains the line
   `- Development law mode: pair` beside the `Task tracker:` line; a
   `waterflow` repository changes the value and names its release branch on a
   `- Release branch: <name>` line. Absent a declaration the law reads as
   `pair` — silent (R-005).
3. **Audit check 21, slug `waterflow-mode`** (Warning): a repository whose
   entry declares `waterflow` but names no release branch is reported; one that
   names a convention is silent (R-003/R-004). The check reads the entry text
   through the existing `EntryDocument.Of` + `Read` path and matches two closed
   marker substrings — `WaterflowModeMarker` (`Development law mode:
   waterflow`) and `ReleaseBranchMarker` (`Release branch:`) — both new
   `OptionValue<string>` members of `LegislatorOptions` (R-8209: no literal in
   the check). Registration is `AuditChecks.Order` plus the identical slug in
   `skill/SKILL.md` § Audit; the check is mechanical, never a model check,
   never in Health.
4. **The fixture proves it**: the `rotted-layer` entry (`CLAUDE.md`) plants the
   omission; `report_markers` and `check_slugs_covered` carry the check; the
   quiet cases (with convention; pair or nothing) live at the twin boundary —
   three ruler labels with their `[Parity]` twins, the label ledger ratcheting
   at 0. No `ENVIRONMENTAL` cover: a repository-fact check is not
   machine-subject (spec clarification).
5. **The "and its kernel" half of BL-088 is not this case**: the frozen
   contract names only the release-branch convention; the divergence is
   recorded as a finding against the kernel, carried to the release ledger.
6. **No `skill/VERSION` bump in this task** — the edition number is the
   operator's; the pull request that carries the change proposes v27, never
   decides it, per DP-1 and the ADR-0009 precedent.

## Feature boundaries

**Not done here:** the kernel reading (decision 5); the edition bump
(decision 6); delivering `docs/ai/rules/**` into this or any governed
repository (the release runbook's job, after the operator rules on the
edition); dev-flow's own cornerstone amendment and ADR-0011 widening (dev-flow
BL-079); touching the Architector checkout (read-only, DP-3); any change to
hooks, `settings.json` or the plugin arms.

**Follow-up plan:** the kernel finding lands in the release ledger at merge;
the edition delivery sweeps the fleet with the new law and check 21; `foundry`
and `dev-flow` declare `waterflow` on their own word; later editions may extend
the mode table.

**Blast radius (stage 7 refactors inside, never beyond):**
`skill/assets/rules/core/pair-development.md`, `skill/assets/templates/AGENTS.md.tpl`,
`src/Legislator.Engine/Audit/AuditChecks.cs`, `src/Legislator.Core/Options/LegislatorOptions.cs`,
`skill/SKILL.md` § Audit, `evals/check_engine.py`, `evals/setup_workspace.py`,
`tests/Legislator.Parity.Tests/Engine/AuditTwins.cs`, `docs/okf/*`,
`CHANGELOG.md`, the journal, and this case's own documents.

## Consumer census

The audit's readers (one new Warning only for `waterflow` repositories that
omit the convention); this repository, member #0, pair-default and silent;
the eval corpus (fixture plant, graders, .NET twins); the fleet at the next
edition sweep (delivered law + check 21; undecorated repos unchanged);
`foundry` and `dev-flow`, which declare `waterflow` on their own word and whose
audits then require the convention line; Architector, whose acceptance
scenarios A-5.2.1/A-5.2.2 are this case's own authority. Full detail in
`docs/cases/L-1-law-reads-by-mode/design.md` §6.

## Consequences

- Easier: any agent opening a task in a `waterflow` repository reads which
  reading its law takes, and check 21 keeps the mode honest — a mode declared
  but not honored was exactly the silent false green the release's auditor
  lessons named (B-004/B-006/B-007).
- Harder: nothing structural — the declaring line, the check and both markers
  are additive; `pair` and undecorated repositories read byte-for-byte as
  today, and the check reports nothing for them.
- Deferred: the edition number (the operator's), the fleet sweep (the release
  runbook), and the kernel half (decision 5, carried as a finding).