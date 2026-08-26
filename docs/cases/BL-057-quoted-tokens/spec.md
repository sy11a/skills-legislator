# BL-057 — Audit check 2 cannot tell a quoted token from an unfilled one

**Tier: 1 (light).** Blast radius: one audit check's false-positive rate, in
every legislated repository whose prose documents a templating system — which
includes every repository that legislates another. Novelty: none; the remedy
was named in the backlog entry and the failing behavior is measured (fourteen
false Criticals in this repository alone).

**Spec type: bugfix.** Rides edition v22 on the branch
`bl/043-baseline-and-linter` (one MR per version; same batching precedent as
BL-051 riding `bl/034-self-legislation`).

## The defect

Check 2 (`unresolved-placeholders`, Critical) flags any `{{TOKEN}}` pattern in
`AGENTS.md`, any `.md` under `docs/`, or any `.md` under `.claude/rules/`,
exempting only `docs/adr/template.md`. A token **quoted inside backticks** in
prose that discusses the templating system matches the same pattern — in this
repository that yields fourteen Critical findings, every one a quotation.
Critical is the severity that means "the layer is broken"; fourteen false ones
train the reader to skim exactly the section that must never be skimmed.

## Boundary

**In scope:** SKILL.md's check 2 text; the rotted-layer fixture and its
grader markers. **Out of scope:** directory-based exemptions
(`docs/superpowers/**`, `docs/cases/**`, `docs/backlog.md`) — the backlog
entry offered them as the alternative remedy, and they are rejected here:
they narrow where the check looks instead of fixing what it tests, and an
unfilled token genuinely landing in a case file would become invisible.

## Requirements

### R-101 — a quotation is not a placeholder

WHEN check 2 scans a file, THEN a `{{TOKEN}}` inside an inline-code span
(backticks) SHALL NOT be reported: a backticked token is prose *about* the
templating system, not an artifact awaiting a fill.

### R-102 — a fenced block is quotation too

WHEN check 2 scans a file, THEN a `{{TOKEN}}` inside a fenced code block
SHALL NOT be reported, for the same reason — and because the engine's anchor
scanner already treats fenced blocks as non-prose, so the two scanners agree
on what counts as text.

### R-103 — a bare token still bites

WHILE a `{{TOKEN}}` stands outside any code span or fence in a scanned file,
check 2 SHALL report it Critical, exactly as today. The `docs/adr/template.md`
exemption stands unchanged.

## The hurting case

**GIVEN** the rotted-layer fixture, which carries both directions — the
planted `{{PROJECT_OVERVIEW}}` standing bare in `overview-draft.md`, and a
planted document whose prose quotes `` `{{PROJECT_NAME}}` `` in backticks
while discussing templates,
**WHEN** the audit runs under the v22 law,
**THEN** the report names `overview-draft.md` under Critical and does not
name the quoting document — and under the v21 law the same fixture makes the
new negative assert red, which is its red-before-green demonstration.

## Eval design (POLICY §3, written before the change)

1. **Scenario:** `audit` (rotted-layer). The existing fixture already covers
   R-103 (defect 2); it gains one planted file for R-101/R-102.
2. **Asserts, by name and artifact:**
   - `report does NOT contain '<quoted-doc marker>'` — reads the **report**;
     joins the existing `absent_markers` machinery, so the unmeasured rules
     of BL-062 apply to it automatically.
   - The existing `report names 'overview-draft.md'` marker keeps the
     positive direction; no new assert is needed for R-103.
3. **Planted defect:** a fixture doc (linked from the OKF index so it is not
   an orphan) whose prose discusses the templating system and quotes tokens
   in backticks and in one fenced block.
4. **Negative control:** that is precisely what the new assert is.
5. **Derived or restated?** Restated in the fixture as a marker string —
   same as every other absent-marker; the fixture is the authoritative copy.
6. **A red would mean:** law class before the fix (the check text commands
   the false positive); grader class after it (the marker drifted).

## Converge — 2026-08-26

Judged against R-101..R-103 and the eval design:

- **per R-101/R-102 (complete).** Check 2 carries the quotation rule; the
  planted `templating-notes.md` (inline code and a fence) audits silent under
  v22 — `report does NOT contain 'templating-notes.md'` green in both v22
  corpus passes, and shown red against the v21 law by a live agent run
  (`red-evidence.md`).
- **per R-103 (complete).** The bare `{{PROJECT_OVERVIEW}}` plant still
  reports Critical — `report names 'overview-draft.md'` and its
  severity-anchored twin green throughout; the `docs/adr/template.md`
  exemption untouched.
- **Beyond the spec, same rule:** the quotation rule now also governs the
  engine's `sdd-lint` placeholder pass and the grader's whole-tree token
  scan — three scanners, one definition of prose. The rule promptly earned
  its keep against its own author: `sdd-lint` flagged a bare token in this
  case's red-evidence file, which quoted the v21 agent's finding *about*
  bare tokens without backticking its own example.

Measured close: `audit` 45/45 in both v22 corpus passes
(`evals/benchmarks/v22.md`).

✅ Converged.
