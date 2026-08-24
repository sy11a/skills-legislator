# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

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
