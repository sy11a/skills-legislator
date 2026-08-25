---
type: Changelog
title: OKF Bundle Changelog
description: Chronological record of significant changes to the OKF knowledge bundle.
tags: [changelog, okf]
timestamp: 2026-08-24T00:00:00Z
---

# OKF Bundle Changelog

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
