# Harvest Report — Design (BL-003, with BL-012 riding)

**Date:** 2026-07-09
**Status:** Draft — awaiting user review
**Backlog:** BL-003, BL-012 (docs/backlog.md)
**User-settled decisions (2026-07-09, Wave 2 gate):** candidates are
report-only and re-derived every run (no persistence anywhere); the human
loop is the user pasting survivors into this repo's backlog; rejected
candidates are suppressed in-place with a `<!-- legislator: not-law -->`
marker; harvest runs in migration, upgrade, and audit modes — never fresh
scaffold.

## Goal

The upward feedback loop: what one repo learns, the constitution can adopt —
deliberately, centrally, once. During runs that read project-owned content
anyway, statements phrased as enforceable law are quoted back to the user as
**Constitution candidates**. Proposals only; the skill never writes them
anywhere. Law stays one-way: down via `assets/rules/**` + VERSION, up only
through the user's hands.

## What counts as a candidate

A statement in project-owned prose that satisfies all three:

1. **Law-shaped:** imperative and diff-checkable — an agent reviewing a diff
   could verify compliance ("always …", "never …", "must …", "… before every
   commit"). Descriptions, overviews, and how-to narration don't qualify.
2. **Not already covered:** no owned rule under `docs/ai/rules/**` already
   states it (the run has those files on hand; judge by meaning, not
   wording).
3. **Generalizable:** it would make sense verbatim in another repo of the
   same stack. Project-instance data stays put — "CareerPlatform.Domain has
   zero NuGet dependencies" is an instance of an existing architecture rule,
   not a candidate; "Domain projects must have zero NuGet dependencies"
   phrased as a general law would be.

**Scanned sources:** CLAUDE.md's project-specific sections (everything
except the `@import` block and the tpl-v2 wiring lines) and `docs/okf/**`
prose. Not scanned: `docs/ai/rules/**` (already law), `docs/adr/**`
(decisions, not rules), `docs/journal/**`, `docs/backlog.md`,
`docs/superpowers/**`.

**Suppression:** any line carrying `<!-- legislator: not-law -->` (or whose
immediately preceding line is exactly that marker) is skipped. The marker
lives in project-owned content the user edits anyway — no manifest schema,
no new state. Rewording the project rule so it stops being law-shaped works
too.

## Where candidates appear

A **`### Constitution candidates`** section, one bullet per candidate:

```
### Constitution candidates
- "<verbatim quote>" — <repo-relative path>
```

- **Migration / upgrade mode:** appended to the Step 7 report (after the
  Keep list section, before Health).
- **Audit mode:** appended to the audit report after the Info section,
  before the clean-checks line. Harvest is not a numbered audit check and
  produces no severity findings — it is a proposals appendix; the zero-writes
  contract covers it unchanged.
- **When none qualify:** omit the section entirely (silence, not "none").
- Fresh scaffold: no harvest — everything was just written by the skill.

The operating loop is documented once in this repo's README (steward
section): review candidates → add worthy ones to `assets/rules/**`
centrally → bump VERSION → re-run `/legislator` across repos → the source
repos' now-covered statements become removable on their next migration-style
cleanup or get suppressed.

## SKILL.md changes

- New subsection under Step 7 (migration/upgrade) defining the candidate
  test (three criteria + suppression marker + scanned sources) and the
  section format; one sentence in the Audit section appending the same
  (by reference, not duplicated — the definition lives once in Step 7 and
  the audit section points to it).
- Intro sentence in Step 7: proposals only, never written to any file.

## BL-012 riding items (from the v7.2 final review)

1. Step 7's Keep list trigger becomes "only when this run changed the `keep`
   list **or refused a keep request**", parenthetical widened to both
   refusal reasons (path missing; path is an owned file).
2. Step 3 item 1's copy instruction becomes "using a byte-for-byte **Bash**
   copy (`cp`) — never the Write/Edit tools" so the legislator-hooks
   write-guard's Bash exemption is guaranteed to cover legislator's own
   runs.
3. (Bookkeeping item — already recorded in BL-012; no file change.)

## Eval changes

- **Migration fixture** already contains hand-written law-shaped content
  (architecture constraints, `bl/NNN-short-description` branch convention —
  the latter is *instance data* per criterion 3 and must NOT be listed; one
  of the architecture constraints, e.g. the decimal-money rule, is a
  legitimate candidate). The migration eval agent now also saves its Step 7
  report to `<ws>/legacy-migration/outputs/step7-report.md` (outside the
  repo, mirroring the audit scenario). Grader: report contains a
  `Constitution candidates` section quoting the decimal-money constraint
  with source path.
- **Rotted fixture** gains two planted lines in CLAUDE.md's Project notes:
  one harvestable law-shaped statement ("Always run the archive importer in
  dry-run mode before a real import.") and one suppressed law-shaped
  statement preceded by `<!-- legislator: not-law -->`. Meta: a report
  marker for the first, an absent-marker for the second's quote.
- **Zero writes attributable to harvest:** already enforced by the audit
  scenario's `zero_writes` check and migration's `project_owned_files
  _untouched`-style checks; no new mechanism needed.
- Idempotency scenarios unchanged (harvest writes nothing by construction).

## Out of scope

- Any persistence of candidates (user decision: report-only).
- Auto-promotion into `assets/rules/**` or automated VERSION bumps.
- Cross-repo candidate aggregation tooling.
- Removing now-covered statements from source repos (that's BL-004's
  restructure territory, or manual).

## Verification

`check_static.py` (no template changes). Full e2e benchmark per
`evals/README.md`; VERSION stays 7 (procedure-only cycle); record as
`evals/benchmarks/v7.3.md` against v7.2. Expected: migration gains the
harvest assertion; audit gains one marker + one absent-marker; all v7.2
assertions hold; both idempotency scenarios stay zero-diff.
