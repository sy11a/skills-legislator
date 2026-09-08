# L-1 — The development law reads by mode: `pair` and `waterflow` — Plan

Tier 1 (light), stage 4 of the feature flow. Cuts the design (`design.md` §4,
contracts C-1…C-9) into one-session tasks traceable per R-NNN and passes the
analyze gate — reuse-first, over-engineering, mechanical — before any code
exists. Implements nothing; every task is class **[D]** (deterministic — a
builder session executes the deliverable; no model judgement is asked of any
task's output beyond the e2e benchmark's scenario agents, which the harness
grades deterministically).

**Spec:** `spec.md` (R-001–R-006, DP-1…DP-5, the hurting case). **Design:**
`design.md` (C-1…C-9, feature boundaries, consumer census) and `docs/adr/0012`
(accepted). **Decision policy:** `spec.md` `## Decision policy` — read, never
edited (stage-subagent brief §3); every `[operator]` step below was pre-resolved
by the launch line's rulings (DP-1…DP-5), mode autoflow.

## Task cut

One task = one session (BL-082's practice: each task closes under an owner
review; the autoflow run closes them under the launch line's pre-written
policy). Commit mechanics per `legislator - Sources` § Commit mechanics: one
commit per task, subject `L-1: <what changed>`, the task's journal section in
the same commit, no attribution trailers. File-disjoint tasks are marked `[P]`
and may run in parallel sessions or any order.

### T-01 — The markers become options members (C-2, C-3) `[P]`

- **Files:** `src/Legislator.Core/Options/LegislatorOptions.cs` (two new
  `OptionValue<string>` members — `WaterflowModeMarker` = `Development law
  mode: waterflow`, `ReleaseBranchMarker` = `Release branch:` — plus their
  `KeyMap` rows `waterflow_mode_marker` / `release_branch_marker` and their
  `Enumerate` rows); `src/Legislator.Core/Options/OptionsComposer.cs` (the two
  `Apply` branches — the settability census refuses a member "in KeyMap but not
  applied", C-04).
- **per R-003, R-004.** The two closed substrings whose presence in the entry
  text means the facts the check judges (design C-2/C-3); R-8209's rule — no
  literal in the check — is why the markers are options members at all.
- **Consumes:** the `OptionValue<string>` member pattern and the
  reflection-completeness tests (C-03/C-04). **Produces:** the two markers,
  consumed by T-05.
- **Verification:** `sh evals/check_dotnet.sh` (`LegislatorOptionsTests`
  completeness, `OptionsComposerTests` EveryKey theory); `python3
  evals/check_static.py` (R-8209: no marker literal outside the options
  model); the remaining every-commit gates as the repo's set.

### T-02 — The template declares the mode (C-1) `[P]`

- **Files:** `skill/assets/templates/AGENTS.md.tpl` — insert
  `- Development law mode: pair` immediately after the `Task tracker:` line
  (tpl line 13). No other template change; a `waterflow` repository changes the
  value and adds `- Release branch: <name>` on its own word.
- **per R-001.** The default mode is named by the declaration the law reads —
  "one default declared" and the pair value is the template's default, so a
  repository that declares nothing reads as `pair` (R-005's quiet half starts
  here).
- **Consumes:** nothing new. **Produces:** the declaring line the scaffold and
  migration paths render; the tpl-driven derivations grade against it
  (`grade.py` migration wiring, `check_static.py`'s tpl import-wiring assert).
- **Verification:** `python3 evals/check_static.py` (tpl assert holds);
  `python3 evals/check_engine.py`; the fresh-scaffold proof lands in T-08.

### T-03 — The law states both modes (C-8) `[P]`

- **Files:** `skill/assets/rules/core/pair-development.md` **only**. Never
  `docs/ai/rules/core/pair-development.md` — the delivered copy is the release
  runbook's job and the branch skew is branch-normal (R-006).
- **per R-001, R-002, R-006.**
- **Shape (C-8):** a Mode preamble — the law reads by the mode a repository's
  entry document declares, `pair` (the default, reading exactly as the bullets
  below) or `waterflow`; absent a declaration it reads as `pair`. The three
  cornerstone rules (one task at a time; never merge yourself; no next task
  without approval) each carry their `waterflow` reading **beside** today's
  text — the `pair` reading is never reworded (R-001). A closing line states
  the integration-branch merge stays the operator's in both modes (R-002).
- **Verification:** the five every-commit gates; a grep that
  `docs/ai/rules/core/pair-development.md` is byte-untouched (R-006); the
  check-11 interplay — a `waterflow` declaration contradicts nothing once the
  law states both modes (research §3) — proven in T-06/T-08.

### T-04 — The test quartet arrives red (C-6)

- **Files:** `evals/check_engine.py` (three ruler labels,
  `audit_check21_waterflow_without_convention` / `audit_check21_quiet_with_convention`
  / `audit_check21_quiet_pair`, in the check-18 region ≈689–709, plus the
  `audit_repo` entry files the labels need); `tests/Legislator.Parity.Tests/Engine/AuditTwins.cs`
  (the three `[Parity("engine", label)]` twins after check-18's triple ≈377–422,
  plus the `AuditRepo` entry files per design C-6).
- **per R-003, R-004, R-005.**
- **Red-first, the mandated demonstration** (`.claude/rules/evals.md`: a new
  assert must be shown RED against the unchanged law before it is shown green).
  Prep: `sh tools/publish-legislator.sh` — a fresh binary of the **current**
  source (check 21 does not exist in it; `artifacts/` is git-ignored, no
  commit). Run `PARITY_ENGINE_CMD=artifacts/linux-x64/legislator python3
  evals/check_engine.py` and `sh evals/check_dotnet.sh`: both FAIL because
  `[waterflow-mode]` appears in no output — record the failing check lines
  verbatim in the task's journal section. `LabelCoverageTests` stays green
  (debt 0 — labels arrive with their twins; the ratchet's contract).
- **Ordering note:** this is the one expected-red commit of the plan; the
  red-first requirement forces an assert into the tree before its check, and
  the label ledger's declared-debt design exists exactly to carry it. A red
  that survives T-05 (a check exists and still fails) is a real failure with
  the finding (DP-5 loop cap).
- **Verification:** the observed red (this task), then green after T-05.

### T-05 — Check 21 lands: the engine and its registration (C-4, C-5)

- **Files:** `src/Legislator.Engine/Audit/AuditChecks.cs` (`Order` gains
  `"waterflow-mode"` at position 21, after `"arm-integrity"`; `Run()` gains the
  call; `Check21WaterflowMode` — entry = `EntryDocument.Of(fs, layout,
  options)`, null entry → no finding; `text = Read($"{layout.Root}/{entry}")`,
  check 18's tolerant read (IO failure → empty text, no false finding); matrix:
  waterflow marker present and convention marker absent → finding (C-5),
  waterflow and convention present → silent (R-004), pair or nothing → silent
  (R-005)); `skill/SKILL.md` § Audit — the pinned-slug line (≈201) gains
  `21 `waterflow-mode`` and a numbered item 21 after check 20 (≈227) spelling
  the behaviour and the C-5 finding template. `AuditChecks.Order` and
  `SKILL.md` hold the same slug by construction of the parity law.
- **per R-003, R-004, R-005.** Position 21 (never in Health — `Order.Take(6)`
  derives it; never a model check — mechanical repo-fact), severity Warning,
  the finding names the entry verbatim (C-5, D-3).
- **Consumes:** T-01's markers (the method contains no marker literal —
  R-8209); T-04's quartet (this task greens it). **Produces:** the check 21 the
  fixture (T-06) and the benchmark (T-08) measure.
- **Verification:** `sh tools/publish-legislator.sh` (rebuild the arm with
  check 21; `artifacts/` is git-ignored; `release.json` is NOT touched — the
  edition's digests are the release runbook's, and `"digests": {}` keeps check
  20 at its Info line for a fresh binary); then `PARITY_ENGINE_CMD=artifacts/linux-x64/legislator
  python3 evals/check_engine.py` (3 labels green), `sh evals/check_dotnet.sh`
  (3 twins green, `LabelCoverageTests` 0), `python3 evals/check_static.py`,
  `python3 evals/check_hooks.py`, `node evals/check_opencode_plugin.mjs`; the
  anchors/sdd-lint rung through the published binary before done.

### T-06 — The fixture triplet proves it (C-7) `[P]`

- **Files:** `evals/setup_workspace.py` — the rotted-layer entry (`CLAUDE.md`,
  ≈299) gains `- Development law mode: waterflow` with **no**
  `- Release branch:` line (the plant); `report_markers` (≈501) gains
  `"waterflow-mode]"` and a verbatim finding token (the entry name — two
  order-independent markers per the skill-bindings precedent ≈516–525);
  `check_slugs_covered` (≈546) gains `"waterflow-mode"`. `grade.py` needs no
  edit — it derives `law_slugs` from SKILL.md's pinned-slug line and reddens on
  any one missing. No `ENVIRONMENTAL` cover (closed for repo-fact checks).
- **per R-003, R-004** (the quiet-with-convention half stays at the twin
  boundary — D-5); the e2e proof is T-08's audit scenario.
- **Requires:** T-03 (the law states both modes, so the declaration is not a
  check-11 contradiction — verify at build, research §3) and T-05 (the slug in
  SKILL.md, so grade.py's parity derivation stays alive).
- **Verification:** `python3 evals/setup_workspace.py /tmp/legislator-eval-L1`
  materializes clean; `python3 evals/grade.py /tmp/legislator-eval-L1
  selftest:derivation` green (`waterflow-mode` on both sides of the parity
  equation); the audit-scenario report lands the plant in T-08.

### T-07 — The documentation homes (C-9) `[P]`

- **Files:** `docs/okf/glossary.md` (rows `mode`, `waterflow`, `release branch`
  — definitions drafted in `design.md` §4); `docs/okf/index.md` (bundle
  mapping — verify and update as the rows need); `docs/okf/log.md` (appended
  entry); `CHANGELOG.md` (`[Unreleased]` Added — both modes, the template
  line, check 21, the fixture); `docs/journal/2026-09-08.md` (the day's
  journal — each build task journals its own section in its own commit; this
  task closes the case's documentation set).
- **per R-001** (the mode concept named in the glossary), **R-006** (no
  delivered-copy edit in any of these homes), and the okf.md MUSTs (new
  concepts documented, index + log entries, glossary rows).
- **Records discipline:** no fleet repo names and no absolute local paths in
  tracked files (`.claude/rules/records.md`; `check_static.py` enforces); the
  glossary's "Lives" cells name this repo's own files and Architector documents
  by name, consistent with the existing glossary.
- **Verification:** the five gates; the `legislator anchors` and the
  sdd-lint rung through the published binary (OKF front-matter statuses, the
  changelog's `[Unreleased]`, the journal's day-names) before done.

### T-08 — The full e2e benchmark is the warrant

- **Files (deliverable):** `evals/benchmarks/v27.md` — the proposed-edition
  record (VERSION reaches v27 only at the operator's ruling — DP-1; this
  record is the PR's proposal). The workspace itself is ephemeral, under
  scratch.
- **per R-003, R-004** (the hurting case end-to-end: the audit scenario's
  report names `[waterflow-mode]` and the entry, and the quiet-with-convention
  half holds at the twin boundary), **R-005** (the corpus's other repositories
  — pair and undecorated — stay silent), and the evals.md warrant (a behavioral
  `skill/` change: law text + tpl + SKILL.md all move).
- **Procedure:** `sh tools/publish-legislator.sh` (the arm with check 21) →
  `python3 evals/setup_workspace.py /tmp/legislator-eval-v27` →
  `tools/evals-bg.sh /tmp/legislator-eval-v27` (staged: static → smoke
  (upgrade) → corpus → idempotency ×3) → `python3 evals/mutate.py
  /tmp/legislator-eval-v27` (the mutations pass — every assert killed,
  POLICY §1c) → record pass-rate, tokens, wall time and idempotency in v27.md
  against v26's record (212/212, ≈65 min, ≈9.7 USD, zero ×3, sonnet profile).
- **The plan-size claim, re-confirmed here** (the handoff's open question): the
  v26 record prices the corpus at ≈65 min wall on the claude profile plus the
  mutation pass at seconds; the audit scenario now carries the check-21 plant
  — its report must show the finding AND no project-rules finding naming the
  mode line (the check-11 interplay, research §3).
- **Verification:** the record file itself — 100% on the corpus, idempotency
  zero ×3, every assert killed — plus the both-arms-identical check
  (`AuditChecks.Order` vs the SKILL.md pinned-slug line, which grade.py's
  parity already proves).

## Coverage — every R-line has a task (exit criterion 1)

| R-line | Tasks |
|---|---|
| R-001 | T-02, T-03, T-07 |
| R-002 | T-03 |
| R-003 | T-01, T-04, T-05, T-06, T-08 |
| R-004 | T-01, T-04, T-05, T-08 |
| R-005 | T-04, T-05, T-08 |
| R-006 | T-03, T-07 |

## Ordering

- **Dependencies first, hurting path early.** T-01 → T-05 consumes its
  markers; T-04 (the quartet — the hurting case's first gate) must precede
  T-05 so the red is demonstrated against the unchanged engine; T-06 requires
  T-03 (law states both modes) and T-05 (slug in SKILL.md); T-08 runs last,
  measuring everything.
- `[P]` groups (file-disjoint, parallel-capable): **T-01, T-02, T-03** (one
  session each; src/Core, tpl, law files respectively — no overlap), then
  **T-06, T-07** (evals fixture vs docs). Strict sequence: T-04 → T-05 (red →
  green, one unit of evidence), T-05 → T-06, T-06/T-07 → T-08.
- First task: **T-01**.

## Analyze gate — reuse-first (step 3)

Judged against research §2's census: **pass.** Every mechanism has an in-tree
precedent exercised in stage 2 — the entry read and the matrix are check 18's
(`AuditChecks.cs` ≈452–494), the registration is one `Order` row plus `Run()`
(≈26–33, ≈108–110), the markers copy the options-member pattern
(`LegislatorOptionsTests` completeness, `OptionsComposer` branches), the tests
copy check-18's ruler triple (≈689–709) and twin triple (≈377–422), the
fixture extends the existing meta lists (≈299, ≈501, ≈546). Nothing new is
invented; T-05 is a ~15-line method by design (design §2, V-A cost).
**Finding: none.**

## Analyze gate — over-engineering (step 4)

Judged against what the spec actually asked for: **pass.** The plan delivers
exactly the in-scope five: law text (T-03), template line (T-02), check 21
(T-01/T-04/T-05), the fixture (T-06), documentation homes (T-07), plus the
warrant the law itself requires (T-08). Explicitly not built, each by a
recorded ruling: the kernel half (D-4 — carried to the release ledger, never
implemented), a parser for the mode line (V-D rejected), the manifest as the
declaration home (V-B rejected), a model-judged check (V-C rejected), a
`skill/VERSION` bump (DP-1 — proposed in the PR), delivery into
`docs/ai/rules/**` (DP-1/DP-3 — the release runbook's job).
**Finding: none.**

## Analyze gate — mechanical (step 5)

The project's lint is the sdd-lint pass of the published binary; the binary is
absent on this machine at plan time (`artifacts/` empty, no `legislator` on
PATH), so per `core/sdd.md` the pass is walked here as an explicit checklist
and recorded — a gap to close, never a licence to skip: T-05's publish closes
it, and the binary's rung runs before done.

- **Coverage R↔task** — the table above traces all of R-001…R-006; every task
  cites `per R-NNN` and no R-line is unassigned. ✔
- **Dangling per-R-NNN** — the plan cites only R-001…R-006, all defined in this
  case's spec; no dangling reference. ✔
- **Placeholders** — no `{{TOKEN}}` pattern in this file. ✔
- **Shape lints** — case header and sections in the spec (tier, type, boundary,
  requirements, hurting case, clarifications, decision policy): present since
  stage 1; ADR-0012 numbered/sectioned/`accepted` with Consequences (written
  stage 3); journal day-names (`2026-09-08 — Tuesday`); the changelog's
  `[Unreleased]` present; OKF front-matter statuses valid. ✔
- **Both arms identical** — `skill/SKILL.md` § Audit and `AuditChecks.Order`
  carry the same slug once T-05 lands; grade.py's parity proves it at T-06/T-08
  (no edit to grade.py). ✔

**Findings: none.**

## Approval (step 6) — decision

Prepared: the plan above is the build order, gates passed with no findings,
the one open question (e2e cost) answered by T-08's size. No option falls in a
no-go zone (DP-3: the plan writes only the law source, the engine, the
fixture, the docs homes, the case home — the sanctioned list of DP-1).
Options: (a) approve the plan as the build order; (b) split T-08 further;
(c) merge T-04 into T-05. Rejected: (b) — the benchmark is one coherent
instrument run with a single recorded artifact; (c) — keeping the quartet
before the check keeps the red-first demonstration observable and gives the
review two gates (DP-2: leave the contract's red-first reading intact).
**Taken: (a) — approve as the build order.**