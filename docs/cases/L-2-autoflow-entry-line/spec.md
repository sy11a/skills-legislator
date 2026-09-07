# L-2 — Fleet law: task entry defaults to the unattended route (`/autoflow`)

**Tier: 0 (direct).** Blast radius: one law line in
`skill/assets/rules/core/pair-development.md` and this repository's own
`.claude/rules/skills.md` instance data. Novelty: low — the "name it, but
make it inert where the skill is absent" pattern already governs every
other concrete skill name in `core/skills.md` and `.claude/rules/skills.md`.

**Spec type: feature.** Branch `bl/l2-autoflow-entry-line`, cut from
`master`. Not a legislator `BL-NNN` case: `L-2` is a Release-0 key of
Architector's ledger (`docs/cases/BL-008-release-cycle/`, epic E-5, story
S-5.1), carried here as its own key per that kernel's instruction — no
`BL-NNN` is minted for it. Pays half of dev-flow BL-051's ruling 11; the
skill half shipped in dev-flow's own repo and has been running daily since
2026-09-05.

## Route deviations

Stages 0–1 collapsed into the opening touchpoint (autoflow, ADR-0011 of
dev-flow's own repo). This being a tier-0 direct case, `core/sdd.md`'s
requirements/plan/converge machinery is skipped — only the header and the
decision policy below are recorded; ADR-0009 carries the reasoning.

## Decision policy

Session 2026-09-06 (opening touchpoint, mode autoflow, tier 0). Every
clause below is the operator's, given in advance in the Release 0 kernel
launch line (Architector `docs/cases/BL-008-release-cycle/release-0-protocol.md`
§3, amendment A-12) rather than opened as a live window, since this run is
driven unattended.

- **DP-1 — risk appetite**: on its own authority, this run may touch the
  law text under `skill/assets/rules/**` and whatever this repository's own
  rules require to accompany a law change, this repository's
  `.claude/rules/skills.md` `flow-sessions` class, `docs/backlog.md`'s
  `L-2` row, and this repository's documentation homes (`docs/adr/`,
  `docs/journal/`, `docs/okf/`, `CHANGELOG.md`, `docs/cases/`). Never on its
  own: bumping `skill/VERSION` beyond what `constitution-source.md`
  requires for this change (an edition's number is the operator's plan —
  proposed in the pull request, not decided here); re-delivering the
  constitution into any sibling repository; any change under another
  repository's `docs/ai/**`.
- **DP-2 — tie-break rule**: the frozen contract (Architector's
  `contract/intent.md`, `contract/acceptance.md`) beats the task text; this
  repository's own laws beat the kernel's phrasing; where both are silent,
  the option that changes the least law text.
- **DP-3 — no-go zones**: the parked main checkout of this skill's
  development tree; every sibling repository; the Architector checkout
  (read-only); this repository's `docs/ai/**` (a delivered copy, never
  edited in place — see `constitution-source.md`); hooks and
  `settings.json`; the operator's knowledge base except through `/capture`.
- **DP-4 — expected shape of the result**: a draft pull request into
  `master` with this repository's own gates green (`evals/check_static.py`,
  `evals/check_engine.py`, the enforcement-arm checks), OKF/changelog/
  journal updated per this repository's own law, and a PR body stating
  what law line changed, where it lands in a governed repository once
  delivered, and the gate output — no AI attribution of any kind in
  commits or the PR body.
- **DP-5 — loop caps**: dev-flow's `autoflow` defaults (its skill's own §9).

## Acceptance (carried, not re-derived)

Architector's `contract/acceptance.md` scenario A-5.1.1 is the authority
over what "done" means for this story, and it is a structural exit read at
the *delivery target*, not this source repository: "GIVEN a governed
repository re-legislated after the edition lands, WHEN its delivered law is
read, THEN one line states that a task is entered by the unattended route
unless the operator names the attended one." This case's own exit is
narrower and is what it can actually close: the line exists, correctly
worded, in `skill/assets/rules/core/pair-development.md`, with the edition
question left open for the operator per DP-1. Delivering it into
`docs/ai/rules/**` — here and across the fleet — is the release runbook's
job, after the operator rules on the edition.
