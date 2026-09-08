# L-1 — The development law reads by mode: `pair` and `waterflow`

**Tier: 1 (light).** EARS requirements plus a hurting case in this file; research/design/plan
run with light ceremony; converge audits every tier. Blast radius: one law file
(`skill/assets/rules/core/pair-development.md`), the entry-document template, the .NET audit
check set (a new check 21), the eval fixture, and this repository's docs homes. Novelty: a new
mode is a new contract — but the "name it, default stays innocuous" pattern already governs
every other named default in the law.

**Spec type: feature.** Branch `l/1-law-reads-by-mode`, cut from the live `origin/master` tip
`88205b9` (2026-09-08; the launch line named `1c260b7`, but PR #44 merged first — live override,
recorded in the tracker). Not a legislator `BL-NNN` case: `L-1` is a Release-0 key of
Architector's ledger (`docs/cases/BL-008-release-cycle/`, epic E-5, story S-5.2, scenarios
A-5.2.1/A-5.2.2), carried here as its own key per that kernel's instruction; the register row
this case lands on is `docs/backlog.md` `## BL-088`, held until v26 landed.

## Route deviations

Stages 0–1 collapsed into the opening touchpoint (autoflow, ADR-0011 of dev-flow's own repo).
The operator's rulings were given in advance in the Release 0 kernel launch line
(Architector `docs/cases/BL-008-release-cycle/release-0-protocol.md` §3, amendment A-12) and are
recorded verbatim in `## Decision policy` below. The launch line's gate list names
`python3 docs/ai/engine.py anchors` / `sdd-lint`; v26 retired the Python engine, so those rungs
run through the published .NET binary (`legislator anchors`, `legislator sdd-lint`) — a
substitution the launch line's own ground paragraph names, not a deviation.

## Boundary

**In scope.** (1) `core/pair-development.md` gains the mode: the law states both modes
(`pair` — today's text, the default — and `waterflow`) and how the three cornerstone rules read
under each; the integration-branch merge stays the operator's in both. (2) The entry document
declares its mode: `AGENTS.md.tpl` gains the line, beside the `Task tracker:` line. (3) A new
audit check **21** (pinned slug per the launch line; `.NET` work in
`src/Legislator.Engine/Audit/`, wired into `AuditChecks.Order`, written up in `skill/SKILL.md`
§ Audit): a `waterflow` repository names its release-branch convention — the check reports the
omission, and reports nothing for one that names a convention. (4) The fixture proves it: a real
defect plant in the `rotted-layer` fixture, its slug in `check_slugs_covered`, a marker in
`report_markers`. (5) Documentation homes: glossary rows (`mode`, `waterflow`, `release branch`),
OKF/index/log, changelog `[Unreleased]`, the day's journal, an ADR where a decision outlives the
case.

**Out of scope.** Delivering `docs/ai/**` into this or any governed repository (the release
runbook's job); bumping `skill/VERSION` on this run's authority (the edition number is the
operator's — proposed in the PR, never decided); editing the Architector checkout or any sibling
repository's tree; touching hooks and `settings.json` beyond what the law itself delivers;
dev-flow's own cornerstone amendment and ADR-0011 widening (dev-flow BL-079, their case). All the
"and also" halves of the frozen contract stay their owners'.

## Requirements

- **R-001** — WHEN the delivered development law is read THEN it SHALL state both modes of the
  development law — `pair` (the default, reading exactly as today's text) and `waterflow` —
  each name closed and one default declared.
- **R-002** — WHERE the law states a mode THEN each of the three cornerstone rules (one task at
  a time; the merge remains the operator's; no next task without approval) SHALL read under it,
  spelled for that mode.
- **R-003** — WHEN a repository's entry document declares `waterflow` and names no
  release-branch convention THEN the audit SHALL report the omission.
- **R-004** — WHEN a repository's entry document declares `waterflow` and names a
  release-branch convention THEN the audit SHALL report nothing for the convention.
- **R-005** — WHEN a repository declares `pair` (or nothing, meaning `pair`) THEN the audit
  SHALL report nothing for the mode.
- **R-006** — The law source SHALL stay one-way: every change lands in `skill/assets/rules/**`,
  and no delivered copy (`docs/ai/rules/**`) is edited in place.

## The hurting case

GIVEN a governed repository whose entry document declares `waterflow` but names no
release-branch convention, WHEN the audit runs, THEN it reports the omission — and a repository
that names a convention is silent. The wound it guards: a mode declared but not honored is
exactly the silent false green the release's own auditor lessons named (B-004/B-006/B-007 — a
check that passes its own subject while catching nothing else); a check with no quiet-case
twin, and with no planted defect in the fixture, is that defect in wait.

## Clarifications

Session 2026-09-08 (opening touchpoint, mode autoflow, tier 1). No live grill was run: the
operator's rulings arrived in advance in the launch line, and the answers below are its own
words transcribed, not a substitute for the contract (which stays the authority where they
disagree).

- **Q: base branch before the cut?** A: the live `origin/master` tip — the launch line named
  `1c260b7` but PR #44 merged first; the launch line's own precedence rule (live remote wins)
  resolves it, and the disagreement is recorded as a finding against the kernel.
- **Q: which check number?** A: 21 — 19 `case-collisions` and 20 `arm-integrity` are taken by
  v26, and the check's slug is appended to `AuditChecks.Order` and spelled in `SKILL.md` § Audit,
  which both arms must hold identical.
- **Q: the fixture obligation?** A: three pieces, all mandatory — a real plant in the
  `rotted-layer` fixture (the meta-assert reddens without it), the slug in `check_slugs_covered`,
  a marker in `report_markers`; the `ENVIRONMENTAL = {"arm-integrity"}` exemption is closed and
  does not cover a repository-fact check.

## Decision policy

Session 2026-09-08 (opening touchpoint, mode autoflow, tier 1). Every clause below is the
operator's, given in advance in the Release 0 kernel launch line (Architector
`docs/cases/BL-008-release-cycle/release-0-protocol.md` §3, amendment A-12) rather than opened
as a live window, since this run is driven unattended. The launch line is quoted, not
paraphrased, for the clauses that carry the operator's exact words; where the frozen contract
and the launch line disagree, the contract wins and the disagreement is a finding against the
kernel — twice this release the machinery caught a diverged launch line that way.

- **DP-1 — risk appetite**: the law source (`skill/assets/rules/**`), the generated half
  (`skill/SKILL.md`'s derivations), this repository's own instance data where the law lands,
  `src/Legislator.Engine/Audit/` for the new check with its registration and its fixture plant,
  plus the documentation homes (OKF, changelog, journal), the case home, and an ADR where a
  decision outlives the case. Never on its own: a bump of `skill/VERSION` — the edition number
  is the operator's (the ADR-0009 precedent from L-2); re-delivering `docs/ai/**` into any
  governed repository; another repository's tree.
- **DP-2 — tie-break rule**: the frozen contract beats the task text; the ledger's rulings beat
  the repository's docs; where both are silent, the option that leaves a contract unchanged.
- **DP-3 — no-go zones**: `docs/ai/**` of any governed repository, other repositories' trees,
  the Architector checkout (**read-only** — read the frozen contract, write nothing), the
  ledger, hooks and `settings.json` beyond what the law itself delivers, the vault except
  through `/capture`.
- **DP-4 — expected shape of the result**: a **draft** PR into `master` of the legislator
  repository; the every-commit gates green — `python3 evals/check_static.py`, `python3
  evals/check_engine.py`, `python3 evals/check_hooks.py`, `node evals/check_opencode_plugin.mjs`,
  the anchors/sdd-lint rung through the published binary — **plus `sh evals/check_dotnet.sh`**;
  OKF documents for every new concept with index and log entries; `CHANGELOG.md` `[Unreleased]`;
  the day's journal; the PR body naming the check number, the fixture plant, and what it did
  **not** do (no VERSION bump, no re-delivery). **No attribution trailer, footer, badge or
  session link of any kind.** Commit subjects `L-1: <what changed>`.
- **DP-5 — loop caps**: implement→converge 3, red→green 5, review-rework 3, restarts of one
  stage 2 (dev-flow autoflow §9 defaults, restated here because the launch line names them as
  overrides for this task). A stage that stops twice for a reason that is not a provider window
  is a **park with the finding**, not a third blind relaunch; a kill by the host's low-memory
  guard is a host stop under A-14, costs no restart, and is to be reported, not fought.

## Acceptance (carried, not re-derived)

Architector's `contract/acceptance.md` scenarios are the authority over what "done" means for
this story:

- **A-5.2.1** [AC] GIVEN the delivered law, WHEN the cornerstone rules are read, THEN both modes
  are stated, each of the three cornerstone rules reads under each, and the integration-branch
  merge stays the operator's in both.
- **A-5.2.2** [AC] GIVEN a repository whose entry document declares the waterflow mode but names
  no release-branch convention, WHEN the audit runs, THEN it reports that omission, and reports
  none for a repository that names one.

This case's own exit is the narrower, source-side half it can close: the law text in
`skill/assets/rules/**`, the entry-document mode line in `AGENTS.md.tpl`, audit check 21 in the
.NET engine with `AuditChecks.Order` and `SKILL.md` § Audit identical, the fixture triplet
planted, and the whole delivering through `R-001`–`R-006` with the edition question left open
for the operator per DP-1. Delivering into `docs/ai/rules/**` — here and across the fleet — is
the release runbook's job, after the operator rules on the edition.