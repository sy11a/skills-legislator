---
task: L-1
acceptance_set_version: cycle-1 (frozen 2026-09-08, stage 8; no amendments — stage 7 changed
  no code, so nothing tagged a candidate)
stand: no live stand for this project (`legislator - Sources.md` § Flow methods, row
  "instances": "No live stands"); the acceptance run's environment is this branch's own
  checkout, `l/1-law-reads-by-mode` at `98b5aff`, worktree `legislator-E`, `git status --short`
  clean throughout
date: 2026-09-08
---

# Acceptance protocol — L-1 — the law reads by mode

Stage 8 of the feature flow, executing the contract this case froze at stage 1 and auditing
the diff against every promise it made. This flow has no Gherkin document and no reproduction
canon (no `L-1 Reproduction.md` in the task folder): the frozen set is `spec.md`'s R-001–R-006
plus the hurting case, and each scenario's own steps are its execution steps.

## Frozen acceptance set (cycle 1)

No amendment candidates were tagged at any earlier stage (stage 7's refactor pass found nothing
to change — `L-1 Stage 7 Summary`), so the set adopted here is `spec.md`'s R-001–R-006 and the
hurting case, byte-identical to how stage 1 wrote them. Frozen 2026-09-08; a future cycle (a
ticket returned after delivery) would open cycle 2 with its own version.

**[operator] — freeze.** Options: (a) freeze R-001–R-006 + the hurting case as spec.md states
them, no amendment to adopt; (b) — none: no amendment candidate exists to weigh against (a).
Option-set status: exhaustive (there is nothing to adopt or decline). Taken: (a). Ledgered
below.

## Stand preparation

**[link] — deploy-to-stand playbook.** The registry row (`Tech/Dev-Flow Project Links.md`,
"Stand deploy playbook", stage 8) carried no method for `legislator` specifically (TBD for
this project; only dev-flow, foundry and clerk had rows). Discover-and-own: this project's own
sources note already answers it one row up — "instances": "No live stands. Fleet sweep is OUT
of the flows for BL-082 — release runbook only." There is no stand to deploy to; the acceptance
run's environment is the branch checkout itself. Recorded in `legislator - Sources.md` § Flow
methods (new bullet) and the registry row re-pointed at it — see this stage's KB summary,
§ Members used / discover-and-own.

Precondition checked against the checkout (pattern anchor A-010): `git status --short` clean,
branch `l/1-law-reads-by-mode` at `98b5aff` (stage 7's journal-only commit), no stray untracked
paths (the `./--help` directory stage 7 removed stays gone). No temporary data or flips were
needed — nothing this case's scenarios require is a stateful fixture beyond the eval corpus,
which each scenario materializes fresh into its own scratch workspace.

## Protocol — R-NNN → scenario → run mode → verdict → evidence

| R-line | Scenario | Run mode | Verdict | Evidence |
|---|---|---|---|---|
| R-001 | The delivered law states both modes, one default declared | scripted (direct read) | PASS | `skill/assets/rules/core/pair-development.md` lines 1–3: "The development law reads by the mode a repository's entry document declares: `pair` (the default, reading exactly as the bullets below) or `waterflow` … Absent a declaration the law reads as `pair`." `skill/assets/templates/AGENTS.md.tpl` diff (`cf84e19..HEAD`): `+ - Development law mode: pair` after the `Task tracker:` line. |
| R-002 | Each of the three cornerstone rules reads under the declared mode; the integration-branch merge stays the operator's in both | scripted (direct read) | PASS | Same file: each of "Work one task at a time", "Never merge to the main branch yourself", "Do not start the next task without explicit user approval" carries a `Under waterflow:` sub-bullet (per-track parallelism; never merge `master`, the release kernel merges into the release branch; approvals per release). Closing line: "The integration-branch merge stays the operator's in both modes." |
| R-003 | `waterflow` declared, no release branch named → audit reports the omission | scripted (ruler + .NET twin, this session, fresh) | PASS | `PARITY_ENGINE_CMD=artifacts/linux-x64/legislator python3 evals/check_engine.py` this session: `ok audit_check21_waterflow_without_convention`. `bash evals/check_dotnet.sh` this session: 662/662, including `Audit_check21_waterflow_without_convention`. Corpus-level (T-08, same code, unchanged since — see Run-mode note below): `evals/benchmarks/v27.md` `audit` scenario report carries verbatim `- [waterflow-mode] CLAUDE.md: declares the waterflow mode but names no release branch → add a line naming it, e.g. "- Release branch: release/0"`. |
| R-004 | `waterflow` declared with a release branch named → audit stays silent | scripted (ruler + .NET twin, fresh) | PASS | This session: `ok audit_check21_quiet_with_convention`; twin green in the same 662/662 run. |
| R-005 | `pair` (or no declaration) → audit stays silent | scripted (ruler + .NET twin, fresh; plus a live sibling check) | PASS | This session: `ok audit_check21_quiet_pair`; the "declares nothing" half rides the existing clean-audit labels (`audit_clean_repo_clean_report`), also green this run. Sibling check: `grep -n "Development law mode" CLAUDE.md AGENTS.md` in this repo's own working tree → no match — this repository (member #0) declares nothing and reads `pair`; check 21 is silent for it (consumer census item 2, `design.md` §6). |
| R-006 | The law source stays one-way: `docs/ai/rules/**` is never edited in place | scripted (direct read) | PASS | `git diff HEAD -- docs/ai/rules/core/pair-development.md` on this branch: empty. `git log --oneline -1 -- docs/ai/rules/core/pair-development.md` → `ed5c069` (v25, predates this case entirely). The delivered copy is untouched; only `skill/assets/rules/core/pair-development.md` moved. |
| Hurting case | A `waterflow` repo naming no convention is caught; one naming a convention is quiet | scripted (fixture probes A/B, T-06; corpus `audit` scenario, T-08) | PASS | T-06's journal record: probe A (plant in) → the finding line verbatim; probe B (plant removed on a mutated copy) → the report-marker assert reddens, proving the marker is load-bearing, not vacuously true. T-08's `audit` scenario: the finding line lands, and the report's one `[project-rules]` finding names a *different* planted defect (`.claude/rules/journal.md` vs `core/dev-journal.md`) — no `[project-rules]` finding anywhere names the mode line, confirming research §3's check-11 prediction (stating both modes removes the contradiction a bare declaration would otherwise read as). |

**Run-mode note (cost/repeatability, step 3).** R-003–R-005 and the hurting case each have both
a cheap scripted form (the ruler labels + .NET twins, re-run fresh this session, seconds) and an
expensive agent-driven form (the eval corpus's `audit` scenario inside the full benchmark, ≈66
min / ≈$9.90 total across 8 scenarios). Stage 7 made zero production-code changes (confirmed:
`git diff cf84e19..HEAD --stat` shows no file touched after T-08's `58c756c` except the
journal-only `98b5aff`), so the corpus evidence T-08 recorded in `evals/benchmarks/v27.md` this
same cycle is still current — re-running the full corpus here would spend real cost measuring
code that has not moved since it was last measured. The cheap scripted checks were re-run fresh
in this stage's own session (not carried by claim, session rule 9); the corpus's `audit` scenario
report is cited from `v27.md` as unchanged evidence, named explicitly rather than silently
assumed.

## Sibling regression (from the consumer census, `design.md` §6)

| # | Neighbor (census item) | Verdict | Evidence |
|---|---|---|---|
| 1 | This repository (item 2) — pair-default, no mode line | PASS (silent) | `grep` above; no `[waterflow-mode]` possible with no marker in the text. |
| 2 | The eval corpus's other seven scenarios (item 3) — `upgrade`, `legacy-migration-agents-first`, `fresh-scaffold-dotnet`, `legacy-migration`, `restructure`, `upgrade-drop-stack`, `case-practice` | PASS, with two pre-existing exceptions named below (not check-21 regressions) | `evals/benchmarks/v27.md` corpus table: every scenario's count matches or exceeds its v26 count; `audit` moved 52→54 by the plan's own arithmetic (one new finding line, one new meta-assert), the rest unchanged. |
| 3 | Every other audit check (checks 1–20) | PASS (unmoved) | `python3 evals/check_engine.py` this session: all engine checks passed, including every pre-existing check-18 and earlier label; `bash evals/check_dotnet.sh`: 662/662 (659 baseline + the 3 new twins), `LabelCoverageTests` debt 0. |
| 4 | `foundry` / `dev-flow` (item 5) and the wider fleet (item 4) | N/A this stage | Out of this case's boundary — no delivery into `docs/ai/rules/**` happens here (DP-1); the fleet sweep is the release runbook's job, unchanged by this case's own gates. |
| 5 | Architector (item 6) | N/A this stage | Read-only checkout (DP-3); its acceptance scenarios A-5.2.1/A-5.2.2 are addressed under "Carried acceptance" below, not independently re-run (no access). |

**Two pre-existing findings — not check-21 regressions, carried unfixed (per the resume
session's DP-2 ruling, unchanged since stage 6/7):**

| Assert | Verdict | Disposition |
|---|---|---|
| `legacy-migration` — `preserved: 'bl/NNN-short-description'` | FAIL, identical on a fresh `origin/master` checkout | Same-tree, pre-existing — not this case's gap |
| `restructure` — `fidelity: 'We do not maintain CHANGELOG.md'` | FAIL, identical on a fresh `origin/master` checkout | Same-tree, pre-existing — not this case's gap |
| `restructure` — `conflict_not_auto_resolved` | FAIL, identical on a fresh `origin/master` checkout | Same-tree, pre-existing — not this case's gap |

Full derivation of "same-tree" (the `origin/master` re-grade, the direct `grep` confirming the
dropped line) is `evals/benchmarks/v27.md` § "Two pre-existing findings". Surfaced again here,
per `core/sdd.md`'s converge obligation, as a finding **for the operator, not for this case** —
see Converge record below.

## Stand state

- Preconditions verified per scenario: `git status --short` clean before and after; each
  scenario ran either the branch's own artifacts (ruler/twin runs) or a scratch workspace the
  scenario itself materializes and discards (corpus scenarios, already run and recorded at T-08).
- Receipts worked off: none were outstanding at this stage's start (`L-1 Handoff` stand_receipts:
  "None outstanding"); none were opened this stage.
- Deployed fix fate **[operator]**: no stand exists to deploy to or roll back (see Stand
  preparation above). Options: (a) treat the republished `artifacts/linux-x64/legislator` as
  ordinary regenerable, git-ignored build output — kept on disk, no action; (b) delete it after
  this session. Option-set status: exhaustive. Rejected: (b) — nothing in this project's law
  treats `artifacts/` as a stand or a deployed instance; deleting it would only force the next
  session to republish work this session already did, for no safety gain (DP-1: nothing here
  falls outside the sanctioned engine/fixture/docs area; the artifact itself is ADR-0008's own
  named generated output). Taken: (a) — kept, git-ignored, regenerated on demand.

## Converge record — promise inventory (`[law: sdd/gates]`)

Judged against the code on this branch (`98b5aff`), never against the git diff alone — every
file named below was opened and read at this stage, not taken from a summary (session rule 9).

### Spec R-lines

| R-line | Verdict |
|---|---|
| R-001 | ✅ held — see Protocol table |
| R-002 | ✅ held |
| R-003 | ✅ held |
| R-004 | ✅ held |
| R-005 | ✅ held |
| R-006 | ✅ held |

### Plan decisions (T-01–T-08)

All eight tasks owned with a commit, each verified against its own "Files" and "Verification"
lines this stage (not merely cited): T-01 (`8f808a9`, options members + `OptionsComposer`
branches — read, present), T-02 (`dd73585`, tpl line — read, present), T-03 (`52ccdeb`, law
text — read, matches C-8 exactly, `docs/ai/rules` untouched), T-04 (`0fa2ff2`, the one
expected-red commit — red observed and recorded in the journal, not just claimed), T-05
(`2181ff3`, `Check21WaterflowMode` — read, matches C-4/C-5 exactly, no marker literal in the
method), T-06 (`1cdd232`, fixture triplet — read, matches C-7), T-07 (`88a5514`, docs homes —
read, glossary/log/changelog all present and worded per C-9), T-08 (`58c756c` + `ed63e5c`,
benchmark — `v27.md` read in full this stage). **Verdict: ✅ held**, no gap.

### ADR-0012 decisions

Decisions 1–4 (law states both modes; entry declares it; check 21; the fixture) are implemented
and verified above. Decisions 5 (the kernel half) and 6 (no VERSION bump) are **declared not
done here** by the ADR's own Feature boundaries — verified as promised-absent, not missing:
`skill/VERSION` still reads `26`; no write touched the Architector checkout or
`docs/ai/rules/**`. **Verdict: ✅ held** (both the done and the deliberately-not-done halves).

### Decision policy (DP-1…DP-5)

DP-1 (risk appetite): every touched file is inside the sanctioned area (law source, `src/`
engine+options, the docs homes, the case home) — confirmed by the `--stat` diff read at this
stage's start; no `skill/VERSION` bump, no `docs/ai/**` delivery, no other repository touched.
DP-2 (tie-break): the T-08 pre-existing-findings disposition and the stage-5 live-remote-wins
handling both cite it explicitly in the journal. DP-3 (no-go zones): none entered. DP-4 (result
shape): all five gates green fresh this session (see below); OKF/changelog/journal all present;
commit subjects all `L-1: …`; no attribution trailer anywhere in `git log --format=%B` for this
branch's own commits. DP-5 (loop caps): no loop exceeded a cap; T-04's one expected red is
exactly the plan's own prediction, not a cap event. **Verdict: ✅ held.**

### Constitutional MUSTs (`docs/ai/rules/**`, this repo's own delivered copy)

| Rule | Verdict | Note |
|---|---|---|
| `core/okf.md` | ✅ held | Glossary rows `mode`/`waterflow`/`release branch` present and worded per design §4; `docs/okf/log.md` carries the 2026-09-08 L-1 entry; `docs/okf/index.md` verified to already route new terms — no edit needed, confirmed by reading the row, not assumed. |
| `core/sdd.md` | ✅ held | Spec/plan/ADR shapes present; analyze gate passed with no findings (plan.md); this converge is that law's own gate, run fresh. |
| `.claude/rules/dotnet-substrate.md` | ✅ held, one item forwarded | No literal outside the options model (`python3 evals/check_static.py`, fresh, all "carries no path/name literal" lines green); no new statics introduced. **Forwarded to stage 9**: "every PR touching `src/` or `tests/` is reviewed with the owner before merge" — a person's review, not yet run; this stage cannot close it, delivery opens the PR that carries it. |
| `.claude/rules/evals.md` | ✅ held | Red-first shown at T-04 (verbatim red quoted in the journal, re-read at this stage, not re-derived); the e2e benchmark ran at T-08 for this behavioral change. |
| `.claude/rules/records.md` | ✅ held | `check_static.py` (fresh, this stage) is the mechanical enforcer and passed; the glossary's "Lives" cells name only this repo's own files and Architector's ADR by its real name (consistent with this repo's existing convention of naming Architector directly, never aliasing it — the alias list is for sibling *governed fleet* repos, and Architector is the kernel, not a fleet member under this repo's own convention). |
| `core/changelog.md` | ✅ held | `[Unreleased]` carries the Added line (checked directly, `CHANGELOG.md`). |
| `core/dev-journal.md` | ✅ held | One section per task, every session, in `docs/journal/2026-09-08.md` (read in full this stage). |
| `core/adr.md` | ✅ held | `docs/adr/0012-development-law-reads-by-mode.md`, numbered, sectioned, `accepted`, written in the same task that made the decision. |
| `core/decision-gate.md` | ✅ held | No trade-off, simplification, or architecture concern was resolved silently; every judgement this run made under the autoflow reading is ledgered with its `DP-N`, per stage-subagent brief §2 — checked against every stage's ledger/decisions this stage read. |
| `core/verification.md` | ✅ held | The static rung (`legislator anchors`) exits 0 through the rebuilt published arm, this session, fresh — not carried by claim. |
| `docs/ai/rules/core/artifact-lifecycle.md` | ✅ held | This case's own artifacts (spec/research/design/plan/acceptance) are lifecycle, correctly placed in `docs/cases/`; the glossary rows are reference, correctly placed in `docs/okf/`. |

**Findings against this case's own promises: none.**

### Unrequested finding — carried, not this case's (per `core/sdd.md`'s converge obligation)

The two pre-existing eval-corpus reds (`legacy-migration`'s dropped
`bl/NNN-short-description` line; `restructure`'s lost CHANGELOG-fidelity line and
auto-resolved conflict) are **surfaced here as a finding for the operator**, distinct from a
gap in this case: confirmed same-tree against a fresh `origin/master` checkout (no L-1 content),
so no code this case wrote caused them. Not appended as a plan task — they trace to no R-line of
this case's spec, and forcing them into the R-line coverage table would misrepresent what L-1
promised. **Disposition is the operator's**: whether they warrant a separate bugfix case, and
which POLICY §2 class (law / grader / harness / model) each is. Recorded again at delivery
(stage 9's PR body) per the handoff's carried open question.

### Verdict

**✅ Converged.** Every R-line holds, every plan task is owned and verified against its own
file, the ADR's done/not-done halves both match its own text, the decision policy was honored,
every constitutional MUST this case's diff touches holds (one item — the `src`/`tests` PR
review — forwarded to stage 9 as a to-do, not a gap), and the one pre-existing finding this
stage surfaces is explicitly not this case's own gap. No loop back to stage 6 or 7 is needed.

## Carried acceptance (Architector's authority, `spec.md` § Acceptance)

- **A-5.2.1**: held by R-001/R-002 above.
- **A-5.2.2**: held by R-003/R-004 above.
- This case's own exit is the narrower, source-side half per `spec.md`'s own closing paragraph:
  satisfied. The kernel half and the fleet sweep remain, by design, outside this case.
