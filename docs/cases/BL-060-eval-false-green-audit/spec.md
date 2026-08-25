# BL-060 — The eval suite's false green, and how to prune what measures nothing

**Tier: 2 (full).** Blast radius: every assert in the corpus and the arithmetic
that turns them into a release verdict. Novelty: the suite has never been
measured *as a suite*.

**Spec type: exploration.** The deliverable is a measurement and a design, not
code. Nothing here is implemented.

**Status:** analysis complete 2026-08-25, measured against the v21 runs and the
v17–v21 chronicles. Findings recorded in `evals/POLICY.md` §§1b, 1c.

## The question

Two questions, from the owner, on the same day the v21 corpus produced its
fifth law defect:

1. Where do eval failures actually come from, and what should change so the
   suite stops producing false green?
2. Which asserts are useless — measuring trivia, or passing unconditionally —
   so they can be deleted instead of run forever?

## Method

Three sources, no guesswork:

- **The chronicles.** `evals/benchmarks/v17…v21`, the editions written under
  `POLICY.md`'s classification requirement. Thirty-six defects with a class.
- **The run corpus.** Every `grading.json` surviving on disk (v20 baseline,
  v21d, v21e) — 200 distinct asserts.
- **Mutation.** Copy a graded scenario, corrupt the artifact under test,
  re-grade, count survivors. No agent, seconds per experiment.

## Finding 1 — run history cannot find a useless assert, and this is structural

Across every surviving run, **199 of 200 asserts were green in every one**.
That statistic contains no information: a healthy corpus is green by design, so
a perfect assert and a dead assert produce identical history. Any method built
on "which asserts never fail" will return the whole suite.

This is why §3's red-before-green rule, though right, is not enough. It binds
the instant an assert is authored and nothing revisits it. Two of v17's asserts
were *green and empty in every run of every version* and were found by
accident, months late — exactly the outcome this finding predicts.

## Finding 2 — a third of the audit scenario survives having no report

Measured by blanking the artifact and re-grading:

| Scenario | Survives an empty report | Of which legitimate |
|---|---|---|
| `audit` (`rotted-layer`) | **14 / 44 — 32%** | 3 |
| `restructure` | 30 / 38 — 79% | ~29 |

The asymmetry is the finding, not the percentages. **`restructure` writes to
the repository, so most of its evidence is the tree**; its 8 report-reading
asserts correctly went red, and its survivors are honest. **`audit` is a
zero-writes mode: the report is its entire output.** A run that produced
nothing at all still scored 32%.

Two mechanisms, both mechanical:

1. **Negative asserts are vacuously true against an absent artifact.** Nine
   `does NOT contain` asserts pass on an empty report. This is v17's
   `ghost_import_fixed` — "compared against an empty string whenever
   `AGENTS.md` was absent" — which was fixed as *one assert* and never as a
   *class*, so the class quietly regrew.
2. **Existence is tested where substance is meant.** `step7_report_saved` and
   its siblings call `path.exists()`; a zero-byte file passes. The runner
   creates the file before spawning the agent, so this assert can only fail if
   the filesystem does.

## Finding 3 — where defects actually come from

Thirty-six defects, v17–v21:

| Class | n | Dominant shape |
|---|---|---|
| law | 13 | **9 of 13 are scope or completion.** The rule is right and says neither *where* it applies nor *when you are done*. |
| harness | 12 | **Over half are false green.** A stage computes a verdict and does not propagate it, or an environment failure wears a model's clothes. |
| grader | 8 | Typography graded instead of value. **2 were green and empty in every version.** |
| model | 3 | **0 survived scrutiny** — see `POLICY.md` §1. |

The cross-cutting theme is **silent non-measurement**. A red announces itself.
A thing that was never measured does not, and every expensive incident in this
repo's history — v20's unmaterialized workspace, v21's `ALL STAGES GREEN` at
43/44, v21's quota exhaustion masquerading as stalled agents — is that one
disease.

## The design

### D1 — `unmeasured` becomes a third verdict

Today an assert passes or fails. Add a third state and make it fatal:

- Every assert declares **the artifact it reads** (report / repo tree /
  manifest / git state). The declaration is data, not a comment.
- When that artifact is absent or empty, the assert is **`unmeasured`** — not
  passed, not failed.
- **Any unmeasured assert makes its scenario red.** A run that produced no
  report is not 32%, it is a failure to measure.
- A negative assert may never take its truth from an absent artifact. This is
  enforced by construction rather than by review: `does NOT contain` on a
  missing artifact yields `unmeasured`.

This is BL-058's lesson applied one level down. There, a stage computed a
verdict and dropped it; here, an assert reports a verdict it never earned.

### D2 — a scenario reports two numbers

`44/44 measured, 44 passed` and `14/44 measured, 14 passed` must not both
render as a percentage that reads like progress. The denominator of the pass
rate is **what was measured**, and what was measured is itself reported.

### D3 — the mutation manifest

Every assert carries a named, minimal corruption of its artifact that **must**
turn it red. The pass runs against recorded artifacts: no agent, no tokens,
seconds.

This generalizes §3 from "the moment an assert is written" to "every commit",
and it is the only mechanism that can answer the owner's second question,
because — Finding 1 — history cannot.

### D4 — mechanical pruning criteria

An assert is deleted, not weakened, when any of these holds:

- **It has no mutation.** Nobody can say what would break it.
- **Its mutation leaves it green.** It measures nothing. An assert that cannot
  fail is worse than none: it consumes a run and buys false confidence.
- **Its mutation is identical to another assert's.** Two asserts with the same
  failure condition are one assert; the duplicate inflates the denominator and
  makes a pass rate look like breadth.

Deliberately mechanical. The judgement call "is this assert important?" is
precisely the one that keeps dead asserts alive for months.

### D5 — the immediate repairs, which do not wait for D1–D4

- `step7_report_saved` and every sibling: test substance, not existence.
- The nine negative asserts in the audit fixture: gate on a substantive report.

## What this does not claim

- **No assert has been deleted, and none is named for deletion.** D3 must run
  before anything is pruned; naming candidates from inspection would repeat the
  judgement-call failure D4 exists to prevent.
- **The 79% survival of `restructure` is not a defect.** Its evidence is the
  tree. Reading that number as a problem would be the same error as reading
  `df` when the quota is the constraint.
- **The mutation pass is not a substitute for the corpus.** It proves an assert
  can fail; only an agent run proves the law can be followed.

## Cases this sizes

Recommended, not filed — order is the owner's:

1. **D1 + D2** — the `unmeasured` verdict and honest arithmetic. Grader change
   only; no `skill/` change, no VERSION.
2. **D3** — the mutation manifest and its runner. The expensive one, and the
   one that makes D4 possible.
3. **D4** — the pruning pass, once D3 can name candidates by measurement.
4. **D5** — the two immediate repairs, small enough to ride any cycle.
