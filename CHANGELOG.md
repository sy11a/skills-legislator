# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- `tools/fleet.sh upgrade` no longer reports `FAIL` over a completed
  delivery: both branches re-read the manifest and decide on the version —
  the runner's exit code is evidence on the line, never the verdict
  (BL-061). `tools/fleet.sh status` reads the committed manifest; an
  uncommitted upgrade shows as `pending review`, never `ok` (BL-056).
  Verified by the new stub-runner harness `tools/fleet-harness.sh`.

### Added

- Edition v22: the engine gains `sdd-lint` (the analyze gate's mechanical
  passes — dangling per-R-NNN references, uncovered requirements in planned
  cases, unresolved placeholders; converged cases are history and are
  skipped) and `baseline` (writes `docs/ai/baseline.md`, the R-NNN ↔
  annotated-tests register — the `generated` class's first member, per
  ADR-0003). Audit check 2 learns the quotation rule: a `{{TOKEN}}` inside
  backticks or a fence is prose about templating, never a Critical
  (BL-057 — fourteen false Criticals in this repository alone).

- The eval grader carries a third verdict, `unmeasured`: every assert declares
  the artifact it reads, and an assert whose artifact is absent or empty scores
  nothing instead of passing. Any unmeasured assert makes its scenario red and
  the run exit non-zero. Scenarios now report two numbers — how many asserts
  were measured, and how many of those passed — and the pass rate is computed
  over what was measured (BL-062, `evals/POLICY.md` §1b).

- Edition v21: `status: removed` OKF documents leave the anchored class; build
  output is excluded from symbol resolution at any depth; the engine exits 3 on
  an unhandled exception and the audit treats any exit outside `{0,1}` as a
  check failure; audit checks 15 and 17 carry a `python3`-absent branch; no
  owned file can be keep-listed. New corpus scenario `audit-engine-absent`.
- This repository is legislated by its own constitution (fleet member #0):
  manifest, owned law under `docs/ai/`, OKF bundle, case home, project rules
  under `.claude/rules/`. See ADR-0002 and `docs/cases/BL-034-self-legislation/`.
- `tools/fleet.sh` takes a runner profile and exits non-zero when any
  repository did not reach the current version (BL-053).
- `tools/evals-bg.sh` refuses to run against a workspace that was never
  materialized (BL-050).

### Changed

- The entry document is `AGENTS.md`; `CLAUDE.md` is now a symlink to it.
- The domain glossary moved from `docs/glossary.md` to `docs/okf/glossary.md`,
  all 48 terms carried, under `core/okf.md`'s sync checklist.

### Fixed

### Removed
