# L-1 — Design: the mode-reading law, audit check 21, and its fixture

Tier 1 (light), stage 3 of the feature flow. Turns `research.md` into the
decision: the chosen variant recorded as ADR-0012 with its rejected alternatives
preserved, the contracts the plan cites, the feature boundaries and the consumer
census. The slug (21 `waterflow-mode`), the severity (Warning), the check-18
quartet scaffold, the fixture-triplet obligation and the DP-1…DP-5 policy are
already pinned by stages 1–2; nothing below re-opens them. Implements nothing.

## 1. Constraints (elicited before any variant, H-013)

From the change-placement constraints (`legislator - Sources` § Flow methods)
and the constraints census of `research.md` §3:

- **Placement is by law, not by team** (solo project): the law stratum is
  one-way — every change lands in `skill/assets/rules/**`; the delivered copy
  under `docs/ai/rules/**` here and across the fleet is never edited
  (`.claude/rules/constitution-source.md`). The check is engine logic in
  `src/Legislator.Engine/Audit/`; the markers are options members (R-8209 — no
  literal in the check); no statics in core (ADR-0008).
- **Tests-first is required**: a new assert is shown red against the unchanged
  law before green (`.claude/rules/evals.md`); the fixture triplet is mandatory
  (spec clarification); the label ledger ratchets at 0 (red-first by
  construction). The full e2e benchmark is warranted (behavioral `skill/`
  change).
- **Social context**: solo; the review is the operator's own PR review; one MR
  per version (`docs/ai/rules/core/pair-development.md`).
- **Release frame**: Release 0 track E under Architector's kernel; the edition
  number is the operator's — this run proposes v27 in the PR, never decides
  (DP-1); no re-delivery into `docs/ai/**` (DP-1, out of scope).
- **DP-3 no-go zones**: the Architector checkout (read-only), the ledger, other
  repositories' trees, hooks/settings.json beyond the law, `docs/ai/**`.

## 2. Variants (scored against the R-lines)

The R-lines are the criteria (`flows/feature/design.md` § Delta). R-001/R-002
are served by the law-text shape; R-003/R-004/R-005 by the detection mechanism.

### V-A — Closed-token substring markers, the check-18 quartet in full (taken)

- **Components**: one mode line in `AGENTS.md.tpl` beside the `Task tracker:`
  line; check 21 reads the entry text once via `EntryDocument.Of` + `Read` and
  matches two closed marker substrings; both markers are new
  `OptionValue<string>` members (C-2, C-3); the finding is a Warning naming the
  entry verbatim (C-5).
- **R-lines**: R-003 (waterflow without convention → report), R-004 (waterflow
  with convention → silent), R-005 (pair or nothing → silent). R-001/R-002
  served by the law shape (C-8).
- **Blast radius**: the entry-document readers only — the audit's check set
  (one added method + one `Order` row), the options model (two members), the
  fixture (`rotted-layer` entry plant + two meta rows), the ruler twins. No
  other check's behaviour changes; pair repositories are byte-for-byte
  unaffected.
- **Cost**: one ~15-line method mirroring check 18's read; two options members
  with key-map + enumerate rows; three ruler labels + three parity twins; the
  fixture plant. Everything has an in-tree precedent exercised in stage 2.
- **What it closes downstream**: the plan's tasks are the quartet copy; the
  e2e benchmark scope is unchanged.

### V-B — The mode declared in `docs/ai/manifest.json`, not the entry document

- **Components**: manifest gains a `mode` key; the check reads the manifest.
- **R-lines**: contradicts the frozen contract and the spec in-scope (2) — "the
  entry document declares its mode" — and the manifest is machine-managed (a
  `pair`-labelled manifest would be closed when the entry is the real source of
  truth). R-003's "entry document declares" is unambiguous.
- **Blast radius/cost**: a manifest schema change; pre-v14 fixture repos
  (CLAUDE.md entries) would carry no declaration and read as pair regardless of
  their actual mode. Rejected — the frozen contract names the entry document
  (DP-2: the contract beats the task text).

### V-C — The mode judged by the model through the model-findings channel

- **Components**: check 21 becomes a semantic judgement like checks 11/12.
- **R-lines**: R-003 is a repository-fact check; the check-set split (mechanical
  engine vs model judgement) puts repo facts in the engine (`research.md` D-2);
  the fixture triplet and the parity law (a planted defect per law slug) cannot
  bind a model judgement deterministically.
- **Cost**: cannot be e2e-planted; `ENVIRONMENTAL` is closed for a repo-fact
  check (spec clarification). Rejected.

### V-D — An open-ended/parsed line model for the declaration

- **Components**: parse the mode line's value, tolerate spacing and synonyms.
- **R-lines**: none of R-003…R-005 needs it — "what counts is closed, so the
  engine can execute the check" is the convention checks 18 and 14 hold
  (`research.md` D-2); permissiveness reintroduces exactly the ambiguity that
  makes a mechanical check unexecutable.
- **Cost**: a parser where a substring suffices; over-engineering (the analyze
  gate's bar). Rejected.

### Wording variants folded into V-A (mode line, convention line, finding text, member names)

- **tpl mode line**: W-1 (taken) `- Development law mode: pair` — the frozen
  contract's own vocabulary ("the development law"); the pair value is the
  template's default and waterflow repos toggle it. W-2 `- Mode: pair` rejected:
  "Mode:" is a high-collision substring (prose, editors, tooling). W-3
  `- Development-law mode:` (hyphenated) rejected: no advantage over the
  contract's two-word spelling.
- **Convention line**: K-1 (taken) the waterflow repo adds `- Release branch:
  <name>`; the marker is the label+colon `Release branch:`, presence alone
  counts (A-5.2.2's "names" = the line exists), mirroring check 18's
  `Task tracker:` match. K-2 `- Release branch convention: <name>` rejected:
  the extra word is prose, not contract; K-3 (judgement of "names a
  convention") rejected — same split argument as V-C.
- **Finding text**: F-1 (taken) `{entry}: declares the waterflow mode but names
  no release branch → add a line naming it, e.g. "- Release branch: release/0"`.
  F-2 (`waterflow declared with no release-branch convention`) rejected: strays
  from the R-003 wording the hurting case quotes. F-1 names the entry verbatim
  (D-3) and carries a remedy (SKILL.md's finding rule).
- **Options member names**: N-1 (taken) `WaterflowModeMarker` (value
  `Development law mode: waterflow`) and `ReleaseBranchMarker` (value
  `Release branch:`); each is the closed substring whose presence means the
  fact. N-2 (`ModeDeclarationMarker`, `ReleaseBranchConventionMarker`) rejected:
  longer, no added precision (the mode member already carries its value);
  N-3 (`WaterflowMarker`, `ConventionMarker`) rejected: "convention" is not the
  marker's shape.

## 3. The decision (autoflow reading of `#decision`, DP-1…DP-5)

The policy pre-resolved every choice; no option falls in a no-go zone, and the
taken options are stack 4's recommendations: V-A over V-B/V-C/V-D (DP-1: the
R-lines are mechanical repo facts; DP-2: the frozen contract names the entry
document); W-1/K-1/F-1/N-1 as above (DP-2: they leave the check 18 and
`Task tracker:` contracts unchanged); the law shape L-1 → paired bullets under
a Mode preamble (C-8; DP-2: leaves the pair reading exactly as today's text);
ADR written (the decision outlives the case — the ADR-0009 precedent). Each is
ledgered in the case's stage-3 ledger (handoff `decisions`).

## 4. Contracts / data model (cited by plan tasks as `design.md` § Contracts, C-N)

- **C-1 — the template line.** `AGENTS.md.tpl` gains, immediately after the
  `Task tracker:` line (tpl line 13): `- Development law mode: pair`. A
  `waterflow` repository changes the value to `waterflow` and adds a `Release
  branch:` line. No other template change.
- **C-2 — `WaterflowModeMarker`** (new `OptionValue<string>` in
  `LegislatorOptions`): value `Development law mode: waterflow`; key
  `waterflow_mode_marker`; added to `KeyMap` and `Enumerate`. The closed
  substring whose presence in the entry text means the repository declares the
  waterflow mode.
- **C-3 — `ReleaseBranchMarker`** (new `OptionValue<string>`): value
  `Release branch:`; key `release_branch_marker`; added to `KeyMap` and
  `Enumerate`. The closed substring whose presence means the repository names
  its release-branch convention.
- **C-4 — check 21 `waterflow-mode` behaviour.** Entry = `EntryDocument.Of(fs,
  layout, options)`; null entry → no finding. `text = Read($"{layout.Root}/{entry}")`
  (check 18's tolerant read: IO failure → empty string, no false finding). The
  matrix:
  | entry suggests | marker found | outcome |
  |---|---|---|
  | waterflow mode | no convention marker | finding (C-5) |
  | waterflow mode | convention marker | silent (R-004) |
  | pair or absent | — | silent (R-005) |
  Registration: `"waterflow-mode"` appended to `AuditChecks.Order` (position 21;
  `Place`/`HealthChecks`/`ModelChecks` derive — it is mechanical, never in
  Health, never a model check) and to `Run()`; the same slug spelled in
  `skill/SKILL.md` § Audit (the pinned-slug line and a numbered item 21 after
  check 20) — both arms identical.
- **C-5 — the finding line.** `new(Severity.Warning, "waterflow-mode", $"{entry}: declares the waterflow mode but names no release branch → add a line naming it, e.g. \"- Release branch: release/0\"")`. The entry is named verbatim (D-3).
- **C-6 — the test quartet.** Ruler labels in `evals/check_engine.py`:
  `audit_check21_waterflow_without_convention` (reports `[waterflow-mode]` and
  the entry name), `audit_check21_quiet_with_convention` (silent), and
  `audit_check21_quiet_pair` (silent, incl. no mode line). `[Parity("engine",
  label)]` twins in `tests/Legislator.Parity.Tests/Engine/AuditTwins.cs`
  (check-18 triple at ≈383–415 is the shape; `AuditRepo` gains the entry files
  the labels need). `LabelCoverageTests` stays 0 — red-first by the ratchet,
  red while the check does not exist.
- **C-7 — the fixture triplet.** `evals/setup_workspace.py`: the rotted-layer
  entry (`CLAUDE.md`) is written with `- Development law mode: waterflow` and no
  `Release branch:` line (the plant); `report_markers` gains `"waterflow-mode]"`
  and a verbatim finding token; `check_slugs_covered` gains `"waterflow-mode"`.
  `grade.py` parity needs no edit — it derives `law_slugs` from SKILL.md and
  reddens on any one missing. No `ENVIRONMENTAL` cover (closed for repo-fact
  checks).
- **C-8 — the law text shape.** `skill/assets/rules/core/pair-development.md`
  gains a Mode preamble: the law reads by the mode a repository's entry document
  declares — `pair` (the default, reading exactly as the bullets below) or
  `waterflow`; absent a declaration it reads as `pair`. The three cornerstone
  rules (one task at a time; never merge yourself; no next task without
  approval) each carry their `waterflow` reading next to today's text, and a
  closing line states the integration-branch merge stays the operator's in both
  modes. The `pair` reading is today's text, never reworded (R-001).
- **C-9 — documentation homes** (build): glossary rows `mode`, `waterflow`,
  `release branch` (definitions drafted below); `docs/okf/index.md` and
  `docs/okf/log.md` entries; `CHANGELOG.md` `[Unreleased]`; the day's journal;
  ADR-0012. The mode line in the fixture must not trip check 11 — verify at
  build once the law states both modes (research §3). Tracked files carry no
  absolute paths and no fleet names (`.claude/rules/records.md`).

### Glossary definitions (for C-9, drafted here)

- **mode** — home — "the named reading of the development law a repository's
  entry document declares: `pair` (the default) or `waterflow`; absent a
  declaration the law reads as `pair`" — Lives: `core/pair-development.md`,
  `AGENTS.md.tpl`.
- **waterflow** — coin (home coinage minted where the field is silent) — "the
  second mode of the development law: one task per track with tracks in
  parallel, the release kernel merging into the release branch on the
  reviewer's and the gates' green, approvals per release" — Lives: `docs/backlog.md`
  `## BL-088`, Architector ADR 0007, `core/pair-development.md`.
- **release branch** — industry — "the branch a `waterflow` repository names in
  its entry document as its release-branch convention; check 21 requires the
  naming" — Lives: `core/pair-development.md`, audit check 21.

## 5. Feature boundaries

**What this case deliberately does NOT do:** (a) implement BL-088's "and its
kernel" half — the frozen contract names only the release-branch convention;
recorded as a finding against the kernel, carried to the release ledger, never
implemented here (D-4, DP-2); (b) bump `skill/VERSION` — the edition number is
the operator's; the PR proposes v27, this run never decides (DP-1); (c) deliver
`docs/ai/rules/**` into this or any governed repository — the release runbook's
job; (d) dev-flow's own cornerstone amendment and ADR-0011 widening — dev-flow
BL-079, their case; (e) touch the Architector checkout — read-only (DP-3); (f)
any change to hooks, settings.json or the plugin arms — the law and the check
set are the deliverable.

**Follow-up plan:** the kernel finding lands in the release ledger at merge; the
edition delivery (after the operator rules on v27) sweeps the fleet with the new
law and check 21; `foundry` and `dev-flow` declare `waterflow` in their entry
documents on their own word; later editions may extend the mode table.

**Declared blast radius (stage 7 refactors inside, never beyond):**
`skill/assets/rules/core/pair-development.md`, `skill/assets/templates/AGENTS.md.tpl`,
`src/Legislator.Engine/Audit/AuditChecks.cs` (Order + Run + the new method),
`src/Legislator.Core/Options/LegislatorOptions.cs` (C-2, C-3),
`skill/SKILL.md` § Audit, `evals/check_engine.py`, `evals/setup_workspace.py`,
`tests/Legislator.Parity.Tests/Engine/AuditTwins.cs`, `docs/okf/*`,
`CHANGELOG.md`, the journal, and this case home's own documents (spec,
research, design, plan, acceptance).

## 6. Consumer census (stage 8's regression reads this list)

1. **The audit's readers** — the operator and any model auditing a legislated
   repository: one new Warning only for `waterflow` repositories that omit the
   convention; a new clean-checks line entry otherwise.
2. **This repository (member #0)** — pair-default, no mode line in its entry:
   the audit stays silent for check 21 (R-005).
3. **The eval corpus** — the `rotted-layer` fixture (plant + meta rows), the
   graders (`grade.py` parity, `check_engine.py` labels), the .NET twins.
4. **The fleet** — every governed repository at the next edition sweep: the
   delivered law carries the mode, check 21 lands in their audits; repos with
   no declaration are unchanged (pair).
5. **`foundry` and `dev-flow`** — declare `waterflow` on their own word; their
   audits then require the convention line (A-5.2.2's other half of the story).
6. **Architector** — the ledger half (ADR 0007, status proposed; read-only to
   this run); its acceptance scenarios A-5.2.1/A-5.2.2 are the case's own
   authority (spec § Acceptance).

## 7. Context package for stage 4 (plan)

The plan (stage 4) builds tasks from this case file per `core/sdd.md` (one task
= one session, traceable `per R-NNN`), citing contracts C-1…C-9. The ADR-0012
records the decision with its rejected alternatives; `research.md` §6 carries
the reuse map and constraints; the e2e benchmark warrant and the 212/212
runtime are re-confirmed at plan (handoff open question). Nothing is parked:
no prototype was needed (research §5 — every mechanism has an in-tree
precedent) and `git stash` is unused per the project's stash conventions.