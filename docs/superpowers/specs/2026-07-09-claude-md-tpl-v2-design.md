# CLAUDE.md.tpl v2 — Design (BL-006)

**Date:** 2026-07-09
**Status:** Approved by user
**Backlog:** BL-006 (docs/backlog.md)
**Origin:** Q1 of the large-codebases best-practices review (Anthropic article),
decisions settled in discussion: the codebase map is OKF knowledge made ambient
via @import (single source of truth, no duplication); boundaries split by
stratum (law vs. project-owned); glossary is a pointer, not an import.

## Goal

Close three gaps in the scaffolded constitution: agents get no ambient picture
of the repo's terrain (navigation wandering), no in-repo signal that
`docs/ai/rules/**` is machine-managed (hand-edit risk), and no home for domain
jargon (term→code mapping).

## Decisions (settled with the user)

1. **Law placement:** the hand-edit ban is appended to the existing
   `pair-development.md` core rule — NOT a new rule file. Reason: existing
   legislated repos already import that file, so the law lands ambiently
   fleet-wide on the next re-run with zero CLAUDE.md edits; a new file would
   sit dormant until each repo owner hand-adds its @import (Step 7 only
   proposes). If meta-law grows past 2–3 lines later, extract a
   `constitution.md` then (deletion propagation handles the move).
2. **Map depth:** top-level directories only, one line each. Deeper structure
   stays in OKF architecture docs loaded on demand.
3. **Boundaries fill:** detect + confirm (same pattern as
   `{{BUILD_TEST_COMMANDS}}`).

## Changes

### 1. Two new templates, two new Step 4 rows

| Target (create-if-missing) | Template | Content |
|---|---|---|
| `docs/okf/codebase-map.md` | `codebase-map.md.tpl` | OKF frontmatter (type: System, title "{{PROJECT_NAME}} — Codebase Map", timestamp {{TODAY_ISO}}); one table: each top-level directory, one-line description ({{CODEBASE_MAP_TABLE}}) |
| `docs/okf/glossary.md` | `glossary.md.tpl` | OKF frontmatter; one-line purpose statement; empty two-column table `| Term | Meaning in this codebase |` — seeded empty, grows under the okf.md sync law |

New placeholder derivation rules (added to SKILL.md Step 4's list):

- `{{CODEBASE_MAP_TABLE}}` — list the repo's actual top-level directories at
  scaffold time, draft a one-line description of each from its contents,
  confirm the table with the user before writing. Rows:
  `| `dir/` | description |`.
- `{{BOUNDARIES}}` — detect no-touch candidates from the repo (`bin/`, `obj/`,
  `node_modules/`, `dist/`, `*.Designer.cs`, migrations output, vendored
  code), present them and ask the user for repo-specific additions (legacy
  areas, generated projects). If nothing beyond generated output, write:
  "Generated build output only (`bin/`, `obj/`, `node_modules/`) — do not
  edit generated files."

Both new placeholders are mode-independent in mechanics (derived from the
repo tree + user confirmation, not from an existing CLAUDE.md), so migration
mode uses the same derivation as fresh scaffold.

### 2. CLAUDE.md.tpl additions

- Import block gains one line, after the stack imports:
  `@docs/okf/codebase-map.md` (map is ambient in every session).
- New `## Boundaries` section containing `{{BOUNDARIES}}`.
- One pointer line (NOT an @import — jargon lookup is on-demand):
  `Domain glossary: docs/okf/glossary.md — check it when a term is unclear;
  add terms as they emerge.`

### 3. Law line appended to `assets/rules/core/pair-development.md`

> - **Never hand-edit `docs/ai/rules/**` or `docs/ai/manifest.json`** — they
>   are machine-managed by the Legislator and overwritten on every run; change
>   rules centrally in the Legislator's repo, then re-run `/legislator`.

Rule content changes → **VERSION 6 → 7** in the same commit.

### 4. Mode behavior

- Step 4 runs in every mode (existing behavior): upgrade and migration runs
  scaffold the map + glossary into already-legislated repos too
  (create-if-missing protects any same-named existing file). The map's
  confirm-with-user step therefore occurs once per repo on its next run.
- Existing repos' CLAUDE.mds are project-owned: the map @import, Boundaries
  section, and glossary pointer reach them only as Step 7 "Needs your review"
  proposals (exact lines quoted for the user to paste). The law line, by
  contrast, is ambient everywhere immediately, since `pair-development.md` is
  already imported fleet-wide.
- Fresh scaffolds get everything from the template directly.

### 5. Verification

- `check_static.py`: no changes needed — new templates are covered by the
  existing referenced↔present and placeholders-documented checks
  automatically.
- `evals/grade.py`: `SCAFFOLD_ARTIFACTS` += `docs/okf/codebase-map.md`,
  `docs/okf/glossary.md`. The existing no-unresolved-tokens check covers the
  new placeholders for free.
- Full e2e benchmark per `evals/README.md`; record `evals/benchmarks/v7.md`
  compared against v6. Expected deltas: fresh scaffold gains the two
  artifacts; upgrade run additionally scaffolds them into the upgrade fixture
  (grader must accept new untracked files there — they are Step 4 artifacts,
  not project-owned modifications); idempotency must stay zero-diff.

## Out of scope

- Linking map/glossary from `okf-index.md.tpl`'s category table — the table
  maps *changes* to files; audit (BL-001) will verify link integrity instead.
- Subdirectory CLAUDE.md scaffolding (deferred; threshold rule noted in the
  best-practices review).
- Any hook enforcement of the hand-edit ban (BL-007's write-guard).
