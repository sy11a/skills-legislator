# Audit Mode — Design (BL-001 + BL-005a, with BL-010 riding)

**Date:** 2026-07-09
**Status:** Approved by user (trigger question settled: "Both" — explicit full
audit + lightweight Health section in normal runs)
**Backlog:** BL-001, BL-005a, BL-010 (docs/backlog.md)

## Goal

Make rot in the project-owned AI layer visible. The constitution can restore
law byte-for-byte, but until now nothing looks at the knowledge stratum —
broken imports, orphan docs, stale maps, dead journals, foreign AI-layer
structures. Audit mode is read-only diagnosis; repair stays out of scope
(BL-004, approval-gated, later).

## Decisions

1. **Trigger — both paths (user-settled).** An explicit request ("audit /
   check / health-check the AI layer", no scaffolding intent) runs the full
   audit and produces the severity-ranked report. Additionally, every
   **upgrade-mode** run appends a short **Health** section to its Step 7
   report carrying only the cheap checks. Fresh-scaffold and migration runs
   skip Health — everything they just created is definitionally fresh, and
   migration's pending Step-7 wiring proposals would make Health flag noise.
2. **Zero writes, mechanically verified.** The audit performs no writes of
   any kind: `git status --porcelain` and `git rev-parse HEAD` must be
   identical before and after. The report is chat output (plus, in evals, a
   copy saved *outside* the target repo).
3. **Audit includes the owned layer too.** Beyond the backlog's
   project-stratum checks, the audit byte-diffs owned files against the
   skill source and compares manifest version against `VERSION` — drift and
   staleness are rot, and the checks are nearly free.

## The checks

| # | Check | Severity | In Health? |
|---|---|---|---|
| 1 | Every `@import` line in CLAUDE.md resolves to an existing file | Critical | yes |
| 2 | Unresolved `{{TOKEN}}`s in any `.md` under `docs/` or CLAUDE.md, except `docs/adr/template.md` | Critical | yes |
| 3 | Manifest parses; every `ownedFiles` entry exists on disk; every owned file byte-identical to skill source | Critical (missing/drifted) | yes |
| 4 | Manifest `legislatorVersion` behind skill `VERSION` | Info ("re-run to upgrade") | yes |
| 5 | Every markdown link in `docs/okf/index.md` resolves | Warning | yes |
| 6 | Codebase-map freshness: every map row's directory exists; every non-generated top-level directory (exclude `.git`, `bin`, `obj`, `node_modules`, `dist`, hidden dirs) has a map row. Skipped if `docs/okf/codebase-map.md` absent | Warning | yes |
| 7 | Orphan docs: an `.md` under `docs/okf/` or directly in `docs/` that no other `.md` (or CLAUDE.md) references by link or `@import`. Exempt by directory convention: `docs/ai/rules/**` (owned, wired via imports), `docs/adr/**`, `docs/journal/**`, `docs/superpowers/**`, and `docs/backlog.md` | Warning | no |
| 8 | Journal recency: newest entry date in `docs/journal/` (filename or content) vs. date of the last commit touching paths outside `docs/` — Warning if the code commit is newer by more than 30 days | Warning | no |
| 9 | Foreign AI-layer structures present: `.cursorrules`, `.cursor/`, `AGENTS.md`/`agents.md`, `.github/copilot-instructions.md`, `wiki/`, ADR/plans dirs outside the standard layout (`adrs/`, `doc/adr/`, `.claude/plans/`) | Info (inventory list) | no |
| 10 | Keep-list integrity (forward-compat with BL-002): if the manifest has a `keep` key, each kept path exists and is linked from somewhere; if the key is absent, note "keep-list: not present (pre-BL-002 manifest)" and skip | Warning | no |

## Report format

```
# AI-Layer Audit — <repo name>, <ISO date>

Constitution: v<manifest> (skill source: v<VERSION>) — <up to date | behind>

## Critical
- [check-name] <path>: <one-line finding> → <one-line remedy>

## Warning
...

## Info
...

Clean checks: <comma-separated list of check names that passed>
```

Empty severity sections are omitted; a fully clean audit prints the header,
"No findings.", and the clean-checks line. The Health section in upgrade
runs uses the same line format under a `### Health` heading inside Step 7's
report, listing findings only (silent when clean: `Health: clean`).

## SKILL.md changes

- Intro paragraph: one sentence — if the user's request is to audit/check
  the AI layer's health rather than scaffold/upgrade, jump to the Audit
  section.
- New top-level section "## Audit — read-only health check" after Step 7,
  containing the checks table above (as instructions), the report format,
  and the zero-writes constraint with the before/after `git status` /
  `rev-parse` verification.
- Step 7 gains: "In upgrade mode, append a `### Health` subsection" with the
  cheap checks (1–6).

## BL-005a — rotted fixture + eval scenario

`setup_workspace.py` gains a fourth scenario, `rotted-layer`, generated from
the current skill source (like the upgrade fixture) and then deliberately
damaged. Planted defects, each greppable in an audit report:

1. Broken import: CLAUDE.md imports `@docs/ai/rules/core/ghost-rule.md`
   (file does not exist).
2. Unresolved token: `docs/okf/overview-draft.md` containing
   `{{PROJECT_OVERVIEW}}`.
3. Owned-file drift: one byte appended to `docs/ai/rules/core/okf.md`
   (differs from source).
4. Stale manifest: `legislatorVersion` = `VERSION − 1`.
5. Stale index link: `docs/okf/index.md` links `docs/okf/renamed-away.md`
   (does not exist).
6. Stale map: `docs/okf/codebase-map.md` lists `legacy/` (does not exist)
   and omits the real `src/`.
7. Orphan doc: `docs/okf/orphan-notes.md`, linked from nowhere.
8. Dead journal: single journal entry dated 2026-01-15; a later fixture
   commit (committer date forced ≥ 60 days after) touches `src/`.
9. Foreign structure: a `.cursorrules` file with one dummy rule.

`fixture_meta.json` records the planted defects so `grade.py` doesn't
re-derive them. `grade.py` gains scenario `audit`: the eval agent runs the
audit against the rotted repo and saves the report to
`<ws>/rotted-layer/outputs/audit-report.md` (outside the repo). The grader
asserts: (a) the report names each planted defect (one grep marker per
defect, recorded in fixture_meta), (b) `git status --porcelain` in the repo
is empty and HEAD unchanged (zero writes), (c) a clean-repo control — the
same audit prompt against the freshly-generated upgrade fixture repo *after*
a normal upgrade run — is NOT required in this cycle (the migration/upgrade
scenarios plus Health-section behavior cover the clean path implicitly;
a dedicated clean-audit scenario can be added later if it earns its cost).

`evals/evals.json` gains the audit scenario's prompt. `evals/README.md`
gains one row in the layer table and one paragraph describing the scenario.

## BL-010 (riding along)

1. **Migration mode writes the full v2 wiring directly.**
   `references/migration.md` §1's import-block description is updated: the
   rewritten CLAUDE.md carries the complete `CLAUDE.md.tpl` v2 structure —
   rule imports, `@docs/okf/codebase-map.md`, the `## Boundaries` section
   (detect + confirm derivation), and the glossary pointer line — not just
   the rule imports. The "mirroring `CLAUDE.md.tpl`'s import block" sentence
   is replaced by an explicit list (resolving the ambiguity finding — moot
   as predicted, since inlining is now intended). SKILL.md Step 5's summary
   line 1 is updated to match. Step 7's "Needs your review" wiring proposals
   then apply only to upgrade mode.
2. **SKILL.md Step 4 glossary row** gains "`{{PROJECT_NAME}}` and
   `{{TODAY_ISO}}` as usual" for consistency with the map row.
3. **Grader strengthening:** `grade_migration` gains one check — the
   migrated CLAUDE.md contains `@docs/okf/codebase-map.md` and a
   `## Boundaries` section (locks in item 1's behavior).

## Out of scope

- Any repair/restructuring action (BL-004) — audit only reports.
- Keep-list creation (BL-002) — audit only forward-supports reading it.
- Harvest candidates in the report (BL-003).
- A dedicated clean-audit eval scenario (noted above; add later if needed).

## Verification

Static checks unchanged (no new templates). Full e2e benchmark per
`evals/README.md`, now four scenarios + idempotency. VERSION stays 7: this
cycle edits SKILL.md and `references/migration.md` but no `assets/rules/**`
content, and VERSION tracks constitution content only. Record results as
`evals/benchmarks/v7.1.md` (procedure change at constitution v7), compared
against v7. Expected: migration scenario gains the new wiring check; audit
scenario finds all nine planted defects with zero writes; idempotency stays
zero-diff.
