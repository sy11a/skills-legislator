# BL-062 — `unmeasured` as a third verdict, and honest scenario arithmetic

**Tier: 1 (light).** Blast radius is high — every assert in the corpus and the
number an edition ships on. Novelty is near zero: the research and contract
halves already exist as BL-060's designs D1 and D2 and as law in
`evals/POLICY.md` §1b. Inflating this to tier 2 would re-derive what is
already written.

**Spec type: bugfix.** The suite awards points for artifacts that do not
exist. Current / expected / unchanged behavior are stated below; the
unchanged list is the regression contract.

**Implements:** BL-060 designs D1 (`unmeasured` verdict) and D2 (two numbers),
which `POLICY.md` §1b already carries as law. This case makes the law
executable.

## Boundary

**In scope:** `evals/grade.py`, `evals/dashboard.py`, `tools/evals-bg.sh`,
`evals/POLICY.md` §5, and the OKF/changelog trail.

**Out of scope**, deliberately:

- Anything under `skill/`. No law change, no VERSION bump, no benchmark run —
  the whole point of taking this case first is to repair the instrument before
  the next law edition is measured with it.
- **D3 (the mutation manifest) and D4 (mechanical pruning).** No assert is
  named for deletion here. D1 makes an absent artifact fatal; it does not
  prove that a present artifact is actually being read.
- **The substance half of D5.** "Empty" here means the literal thing D1 says:
  absent, or containing nothing but whitespace. A report that exists and has
  content but is structurally junk is still `measured` after this case. That
  is D5's territory and it stays open.

## Current behavior — measured, not argued

Method: the surviving eval workspaces of the v21 cycle
(`/tmp/legislator-eval-v21f`, `/tmp/legislator-eval-v21e`) replayed through the
unchanged grader, then re-graded with one report truncated to zero bytes. No
agent, no tokens, seconds per scenario. Every scenario's replay was first shown
to reproduce its recorded `grading.json` verdict exactly, so a delta below is
the mutation's doing and not the replay's.

| Scenario | Asserts | With its report blanked | Survives |
|---|---|---|---|
| `legacy-migration-agents-first` | 22 | **22** | **100%** |
| `upgrade-drop-stack` | 16 | 15 | 94% |
| `legacy-migration` | 27 | 25 | 93% |
| `upgrade` | 21 | 18 | 86% |
| `audit-engine-absent` | 5 | 4 | 80% |
| `restructure` | 38 | 30 | 79% |
| `audit` (`rotted-layer`) | 44 | **14** | 32% |

`audit` and `restructure` reproduce BL-060's numbers exactly. The five
scenarios BL-060 never measured extend the finding rather than soften it:
**`legacy-migration-agents-first` scores a perfect 22/22 against a report that
contains nothing.** Not a partial credit — a full green.

Two mechanisms, both mechanical, both already named in `POLICY.md` §1b:

1. **A negative assert is vacuously true against an absent artifact.** Nine
   `does NOT contain` asserts in `audit` pass on an empty report.
2. **Existence is asserted where substance is meant.** Every report-reading
   scenario has a `*_report_saved` assert testing `path.exists()`, so a
   zero-byte file passes it. This is why the blanked runs above stay green on
   their own report-existence assert — which is the single reason
   `legacy-migration-agents-first` reaches 100%.

Of `audit`'s 14 survivors, four read something other than the report and are
legitimate — `mode_respects_authority` and `audit_report_outside_repo` (the
repo tree), `parity_every_check_has_a_defect` (the law and the fixture meta),
`zero_writes` (git state). The other ten are free points. (§1b records three
legitimate; the difference is `audit_report_outside_repo`, which reads the
tree and therefore stays measured under this design. The count of free points
is what the design turns on, and it is 10 either way.)

## Expected behavior

### R-001 — a declaration is data, not a comment

The grader SHALL require every assert to name the artifact it reads, as a
mandatory argument. An assert that does not declare its artifact SHALL be
impossible to write.

### R-002 — not measured is not passed

WHILE the artifact an assert declares is absent or empty, the grader SHALL
record that assert as `unmeasured`, and SHALL NOT record it as passed or as
failed, whatever the assert's own expression evaluates to.

### R-003 — unmeasured is fatal

WHEN a scenario carries one or more `unmeasured` asserts, THEN the grader SHALL
report that scenario as red and SHALL exit non-zero.

### R-004 — two numbers, always

The grader SHALL report, per scenario, how many of its asserts were measured
and how many of those passed. A pass rate SHALL be computed over the measured
count, never over the total.

### R-005 — the probe is the one assert allowed to be red

WHERE an assert's subject is the existence of an artifact rather than its
content, THAT assert SHALL declare the artifact's container, and SHALL report
`failed` — not `unmeasured` — when the artifact is absent or empty. An
artifact SHALL carry at most one such assert. Where an artifact carries none,
its absence makes every assert that declares it `unmeasured`, which is already
fatal by R-003; inventing a probe to produce one extra red line would inflate
the corpus denominator and measure nothing new.

### R-006 — what "measurable" means

A file artifact SHALL count as measurable only when it exists and contains at
least one non-whitespace character. A repository-tree artifact SHALL count as
measurable only when the directory exists and is a git repository.

### R-007 — the verdict is recorded, not just printed

WHEN the grader writes `grading.json`, THEN every expectation SHALL carry its
verdict and its declared artifact, and the summary SHALL carry the measured and
unmeasured counts alongside the existing keys.

### R-008 — the verdict propagates

WHEN `tools/evals-bg.sh` computes a stage verdict or a gate from a scenario's
`grading.json`, THEN it SHALL treat a non-zero unmeasured count as a failure.
This is BL-058's lesson at one level down: a verdict that is not propagated is
a verdict that was not reached.

### R-009 — the dashboard shows what was measured

WHEN the dashboard renders a graded scenario, THEN it SHALL show the measured
count and mark a scenario with unmeasured asserts as failing.

### R-010 — history distinguishes the two

WHEN a grade is appended to `grade-history.jsonl`, THEN unmeasured asserts
SHALL be recorded separately from failed ones, so the flaky-vs-persistent
oracle cannot read a non-measurement as a failure.

### R-011 — the baseline recipe survives a grader change

`POLICY.md` §5 SHALL state that when the grader changes without a law change,
the edition's baseline is taken from the last commit carrying the previous
law with the current grader — not from the previous edition's tag, whose
grader is a different instrument.

## Unchanged behavior — the regression contract

- Every scenario's replay verdict on its **unmutated** recorded artifacts
  stays what it is today: `fresh-scaffold-dotnet` 19/19, `legacy-migration`
  27/27, `legacy-migration-agents-first` 22/22, `upgrade` 21/21,
  `upgrade-drop-stack` 16/16, `case-practice` 7/7, `audit` 44/44,
  `audit-engine-absent` 5/5, `restructure` 38/38 (v21e), `selftest:derivation`
  16/16. An assert that becomes `unmeasured` on a healthy run is a mislabeled
  artifact, not a finding.
- Assert names do not change. `grade-history.jsonl` continuity depends on
  them, and a rename would silently restart every flaky count.
- `grading.json` keeps `passed`, `failed`, `total`, `pass_rate`. An
  `unmeasured` expectation keeps `passed: false`, so a reader that has not
  been updated under-reports rather than over-reports.
- No fixture changes. No `skill/` changes. VERSION stays 21.
- `check_static.py`, `check_engine.py`, `check_hooks.py` and
  `check_opencode_plugin.mjs` stay green.

## The hurting case

**GIVEN** the recorded artifacts of the v21f corpus run, in which
`legacy-migration-agents-first` scored 22/22,
**WHEN** its migration report is truncated to zero bytes and the scenario is
re-graded,
**THEN** the scenario is red and the grader exits non-zero, where today it
prints `22/22` and exits 0. That scenario declares the report exactly once —
in its probe — so the observable outcome is `22/22 measured, 21 passed` with
`migration_report_saved` FAILED, not a wall of unmeasured lines. The second
case below is where the unmeasured verdict itself is observable.

Second, from BL-060's own measurement:

**GIVEN** the same run's `audit` scenario at 44/44,
**WHEN** its report is truncated to zero bytes and it is re-graded,
**THEN** `audit` reports 5 of 44 measured — the four tree-and-git asserts plus
the failed probe — and 39 unmeasured, instead of today's `14/44` that reads
like a third of a pass.

Both are observable by a tester who never read the grader: truncate one file,
run one command, read the printed line.

## What a red would mean

Per `POLICY.md` §1's four classes, a red in this case's own verification is
**grader** class by construction — no law, no agent and no harness stage is
involved. That is exactly why this case can be verified for free, and exactly
why it must land before an edition is measured against the instrument.

## Clarifications

### Session 2026-08-25

- **Q:** PR #16 is open against `master` and touches `docs/backlog.md`, which
  this case must also touch. Merge first, or stack the branch on it?
  **A:** Merged first (#16 merged as `2111235`); `bl/062-unmeasured-verdict`
  is cut from the updated `master`.
- **Q:** After this case, `POLICY.md` §5's baseline recipe (`git worktree add
  v<N-1>`) resurrects the previous edition's grader together with its law, so
  the v22 baseline would be measured on a different instrument than v22
  itself. Amend §5 in this case, or file it separately?
  **A:** Amend it here (R-011).

## Converge — 2026-08-25

Judged against every promise above, not against the diff. Three findings, all
closed in this case.

1. **per R-005 (contradicts).** The requirement said "Each artifact has exactly
   one such assert." The implementation gives probes to `report`, `manifest`
   and `case` and none to `repo`, `prompt`, `law`, `grader` or the scenario
   home. Judged and resolved as a spec defect, not a code gap: an artifact with
   no probe is already fatal through R-003, and adding a probe per artifact
   would add one red line and five asserts to the corpus denominator without
   measuring anything the scenario does not already report. R-005 now says
   "at most one".
2. **per the hurting case (partial).** The first scenario's THEN was not
   observable as written — `legacy-migration-agents-first` declares the report
   in exactly one assert, so it goes red through the probe and reports
   `22/22 measured, 21 passed`, not a set of unmeasured lines. Wording
   corrected to what was measured; the case still hurts, and harder: **the old
   grader scored it 22/22 and exited 0 against a report containing nothing.**
3. **per R-004 (partial).** The CLI prints no percentage at all, but the
   dashboard printed `5/44 measured, 4 passed (80%)` — a rate over five
   measured asserts reading like progress, which is the sentence §1b forbids.
   The percentage is now suppressed whenever anything is unmeasured.

Two things this case deliberately did not do, recorded so a later reader does
not mistake them for oversights:

- **Law-branch conditionals were left alone.** `restructure`'s
  `link_post_state_in_index` is emitted only when the orphan was linked, and
  `upgrade`'s keep-refusal branch only when the prompt asked for it. Both are
  conditional on a *law branch*, not on an artifact's absence, so they are out
  of D1's scope — but they do make a scenario's denominator vary between runs,
  which D3 will have to reckon with when it builds the mutation manifest.
- **No assert was deleted, weakened, or added.** Totals per scenario are
  unchanged, which is what makes the replay a regression contract at all.

✅ Converged.
