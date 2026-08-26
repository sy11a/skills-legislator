# BL-063 — The mutation manifest: every assert proves it can fail

**Tier: 2 (full).** Blast radius: all 201 corpus asserts and the release
ritual (POLICY gains a mandatory stage). Novelty: the runner and the
manifest form are new; the design is BL-060's D3, already law-shaped in
`evals/POLICY.md` §1c.

**Spec type: feature.** Branch `bl/063-mutation-manifest`. Implements D3;
D4 (pruning) is deliberately a separate, later case — the owner reviews the
candidate list before anything is deleted.

## Why (the measured premise)

Run history cannot find a useless assert even in principle: a healthy
corpus is green by definition (BL-060: 199/200 asserts green in every
surviving run). BL-062 made an *absent* artifact unable to score; nothing
yet proves an assert with a *present* artifact measures anything —
`no_unresolved_placeholders` was exactly that for five editions, and only a
lucky idempotency diff exposed it. Falsifiability must be executed, not
remembered (§1c).

## Clarifications

### Session 2026-08-26

- **Q: scope?** → All 201 corpus asserts, **and every future corpus assert**:
  an assert without a mutation is a red finding of the pass itself, never a
  silent gap. `selftest:derivation` (16) is out of scope — its asserts
  already falsify themselves with synthetic both-ways cases, and pruning
  does not apply to them.
- **Q: where does the obligation bind?** → POLICY §1c names the concrete
  command; the full pass is mandatory in every edition cycle — after the
  green corpus, before `evals/benchmarks/v<N>.md` is written — and the
  pass's summary (killed / survived / uncovered / duplicates) is recorded in
  the benchmark file. The substrate is the benchmark run's own workspace,
  so the pass runs while that workspace is alive; it is a separate command,
  not an evals-bg stage (by stage 4 the idempotency runs have already
  mutated the fixtures — the exact substrate corruption this cycle met
  twice).
- **Q: does this case delete anything?** → No. The runner names candidates
  by the three mechanical criteria; deletion is D4, a separate case, after
  the owner reviews the list.

## Boundary

**In scope:** `evals/mutations.py` (the manifest), `evals/mutate.py` (the
runner), POLICY §1c's binding, the OKF/changelog trail, and the first full
pass against the recorded v22 workspace with its findings recorded in the
case. **Out of scope:** deleting or weakening any assert (D4); the
selftest scenario; any `skill/` change — no VERSION, no agent benchmark.

## Requirements

R-601 — Every corpus assert SHALL carry exactly one mutation: a named,
minimal corruption of the artifact it declares that must flip the assert to
`failed`. WHERE assert names are data-derived (fixture markers), the
mutations SHALL be derived from the same source, never restated by hand
(POLICY §8).

R-602 — WHEN the runner grades a mutated copy, THEN the target assert SHALL
be `failed` — `unmeasured` does not count as a kill except for the probe
asserts, whose subject is the artifact's existence and whose mutation is
its removal.

R-603 — WHEN an assert has no mutation in the manifest, THEN the pass SHALL
report it `uncovered` and exit red — the obligation extends to every future
assert by construction.

R-604 — WHEN an assert survives its mutation, THEN the pass SHALL report it
`survived` and exit red; survived and uncovered asserts are the D4
candidate list, together with duplicate groups (asserts whose canonical
mutation is identical).

R-605 — Before mutating anything, the runner SHALL validate the substrate:
re-grade each scenario and compare with the recorded corpus verdict;
scenarios moved by the idempotency stage are reconstructed to their run-1
state first, and a scenario whose re-grade still disagrees is reported
unusable and makes the pass red — a mutation verdict measured on a
different state than the corpus graded is not a verdict (this cycle's
recorded lesson: `outputs/` is not immutable after grading).

R-606 — Mutations SHALL be applied and reverted in place (byte-restore of
every touched path), so one substrate serves all mutations of a scenario
and the runner needs no agent, no tokens, and no per-mutation copies.

R-607 — The pass SHALL print and record one summary — total, killed,
survived, uncovered, duplicate groups, unusable scenarios — with every
non-killed item listed by name; exit 0 only when every assert was killed
by its own mutation and no scenario was unusable.

R-608 — `evals/POLICY.md` §1c SHALL name the command and the cadence
(every edition cycle, after the green corpus, before the benchmark file);
the benchmark template gains the mutation summary as a required section.

## Hurting case

**GIVEN** the recorded v22 workspace and a manifest covering all 201
asserts,
**WHEN** `python3 evals/mutate.py /tmp/legislator-eval-v22` runs,
**THEN** it reports 201/201 killed in minutes with no agent — and WHEN one
mutation is deleted from the manifest, THEN the pass exits red naming that
assert `uncovered`; and WHEN a deliberately vacuous assert is planted
(one that cannot fail), THEN its mutation leaves it green and the pass
exits red naming it `survived`.

## Eval design (POLICY §3)

The runner is itself the eval; its red-before-green is structural:

1. **Red 1 (uncovered):** run the runner with an empty manifest against the
   v22 workspace — 201 uncovered, exit 1. Committed before the manifest.
2. **Red 2 (survived):** a planted always-green assert (test-only, in the
   runner's selftest) whose mutation cannot flip it — the pass must name
   it survived.
3. **Green:** the full manifest kills 201/201.
4. **A red would mean:** uncovered = the manifest lags the grader (file it
   with the assert); survived = the assert measures nothing (D4 candidate)
   or the mutation is wrong (fix the mutation, shown red first); unusable =
   the substrate moved (restore it, or re-run the corpus).
