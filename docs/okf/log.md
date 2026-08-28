---
type: Changelog
title: OKF Bundle Changelog
description: Chronological record of significant changes to the OKF knowledge bundle.
tags: [changelog, okf]
timestamp: 2026-08-24T00:00:00Z
---

# OKF Bundle Changelog

## 2026-08-28 — BL-048: the per-job model floor

The glossary gains `per-job model floor`. No concept document changed:
the probe and its results are lifecycle artifacts in the case home, and
no code changed.

## 2026-08-27 — edition v23: the audit engine and the case-shape lints

The glossary gains `audit job` and `model-findings channel`. The map's
rows still hold (the engine grew in place; `tools/` gained `proc.py`
under the existing description). Benchmark and defect chronicle:
`evals/benchmarks/v23.md`.

## 2026-08-26 — BL-049: report derivability

The glossary gains `report emitter` — the concept v23's composition will
build against. No concept document changed: the classification is a
lifecycle artifact in its case home, and no code changed in this spike.

## 2026-08-26 — BL-069: the dependency register

The glossary gains `dependency register` (with its absence-behavior
taxonomy — fail-open / fail-loud / crash / silent false green). No concept
document changed: the register is a lifecycle artifact in its case home,
and no code changed in this spike.

## 2026-08-26 — BL-064: the git conduct guard

The glossary gains `git conduct guard`. The codebase map's `plugin/` row
already covers the new hook ("the deterministic enforcement arms"), so no
map change; the plugin's own README carries the concept in depth. The
regenerated `docs/ai/baseline.md` picks up R-641–R-649 alongside the
R-3xx/4xx/5xx/6xx rows the earlier cases had defined since its last
regeneration.

## 2026-08-26 — BL-047: the decision inventory

The glossary gains `decision inventory` — the bucket vocabulary (a/b/c) the
repo will now reason in when sizing engine growth. No concept document
changed: the inventory is a lifecycle artifact living in its case home
(`docs/cases/BL-047-decision-inventory/`), and no code changed in this
spike.

## 2026-08-25 — edition v22: baseline, sdd-lint, and the quotation rule

BL-043 populated the `generated` class: `docs/ai/baseline.md` exists, written
by the engine's third job. The glossary's `baseline` and `generated` rows
drop their "arrives with BL-043" tense; a new `annotated test` row pins the
marker form. BL-057's quotation rule (a backticked token is quotation, not a
placeholder) now governs audit check 2 and sdd-lint's placeholder pass alike.

## 2026-08-25 — `unmeasured` and `declared artifact` enter the glossary

BL-062 gave the eval grader a third verdict. Two terms were minted with it:
**unmeasured**, the verdict an assert carries when the artifact it declared
cannot be read, and **declared artifact**, the source an assert names as data.
Both are `coin` — the field has assert/pass/fail and no word for "this assert
scored a point against nothing", which is precisely the failure BL-060
measured at 32% of one scenario. No concept document changed: the bundle has
none for the eval suite, and `codebase-map.md`'s `evals/` row still describes
it correctly.

## 2026-08-24 — Bundle initialized

Initial OKF bundle scaffolded by the Legislator, during BL-034
(self-legislation). The glossary was not seeded from scratch: the repo's
existing 48-term register at `docs/glossary.md` was migrated forward into
`glossary.md`, keeping its `Status` and `Lives` columns, and the old path was
removed.
