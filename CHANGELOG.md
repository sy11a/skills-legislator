# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

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
