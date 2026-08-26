# BL-049 — Spike: how much of the Step 7 report is machine-derivable?

**Tier: 1 (light).** Blast radius: none in code — no `skill/` change, no
VERSION, no benchmark. Novelty: the report has never been examined as a
data artifact; every shape property it has is re-derived by a model on
every run.

**Spec type: exploration.** Branch `bl/049-report-derivability`. Backlog
entry of 2026-08-23; gate for the report-emitter slice of BL-066 in the
v23 composition.

## The question

What fraction of a report could the engine print from the run's own facts,
and where exactly is the seam with the parts that need a model?

## Scope

**In:** the live v22 corpus reports on disk (`/tmp/legislator-eval-v22`) —
scenario report artifacts as produced by real runs; the SKILL.md report
specifications they instantiate; the grader asserts over them in
`evals/evals.json`; the defect chronicles `evals/benchmarks/v17…v22` for
the historical report-shape defect count. Report families per the clarify
decision below.

**Out:** writing any emitter (the stop condition); regrading or re-running
anything (the artifacts on disk are the evidence — zero agent cost, per
the owner's verification economics); the audit *checks* themselves
(BL-066's territory — this spike sizes only the printing seam).

## Method

- **Line classification:** every line of each report artifact is classified
  **derivable** — printable from facts the run already holds (files
  created/overwritten/deleted, keep-list delta, manifest fields, check
  results, pinned skeleton text) — or **prose** — requiring model
  judgement (constitution candidates, conflict descriptions, free-text
  explanations). A line that is skeleton-plus-fact-slot (a heading, a
  formatted list row) is derivable; a line whose *content* is judgement is
  prose even when its position is pinned.
- **Assert attribution:** each grader assert over a report artifact is
  classified **shape-only** — it exists only because a model composes the
  report (pinned heading levels, byte-for-byte section names,
  order-independence workarounds, presence-of-section) — or
  **substance** — it would exist even with a printed skeleton (does the
  content state the right facts). Counted per scenario from
  `evals/evals.json`.
- **Historical defects:** each report-shape defect in the v17–v22
  chronicles counted, with its class, against the total defect count — the
  measure of what a skeleton would have prevented.
- Totals are script-derived from the classification tables (the BL-047
  recount discipline).

## Acceptance (the case it would hurt to get wrong)

GIVEN the v22 artifacts on disk, WHEN the classification is complete, THEN
every line of every in-scope report artifact is in exactly one class and
every report assert in exactly one class, with totals a reader can recount
from the tables alone; AND the historical count names each defect it
counts (chronicle file + entry); AND the answer states the seam as a
concrete emitter contract — which sections print, which slots stay model —
so BL-066 can be sized from it without re-reading the corpus. The case
that hurts most: counting a judgement-bearing line as derivable — an
emitter built to that count would silently drop model content.

## Deliverable

`docs/cases/BL-049-report-derivability/derivability.md` — the per-report
tables, the assert attribution, the historical count, the seam statement;
summary to the backlog (status flip) and the day's journal.

## Stop condition

The classification, the assert count and the seam statement are the
deliverable. No emitter is written; sizing BL-066's slice from it is the
composition decision for v23, taken with the owner.

## Clarifications

### Session 2026-08-26

- **Q: which report families?** → **All three** — Step-7 run reports
  (scaffold, both migrations, upgrade, stack drop), the audit report
  (both scenarios), and the restructure report. The audit seam feeds
  BL-066 directly (the engine audit job prints exactly that format); the
  extra cost is classification only, zero agents.

## Converge — 2026-08-26

Judged against the spec and the backlog entry: every line of all seven
in-scope artifacts is in exactly one class, defaults-plus-exceptions, with
totals script-derived and cross-checked against the files' line counts;
every report assert is in exactly one class with the J list named; the
historical count names each of the eight defects with its chronicle; the
seam is stated as a concrete emitter contract sized for BL-066. The
cheap-error rule was applied at the boundary: rationale-bearing lines went
to P/Dm, never to D. Zero agents were run (the live workspace was the
material); no emitter was written. Backlog, changelog, journal, glossary
and OKF log updated. Verification: check_static, check_engine, engine
anchors, sdd-lint all clean. Gaps: none (missing / partial / contradicts
/ unrequested: none).

✅ Converged
