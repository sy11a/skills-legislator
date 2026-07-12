# Glossary vitality — design (BL-020, constitution v11)

**Status:** approved 2026-07-12 (plan-mode approval: all three levers +
live backfill of CareerPlatform and RKruiterApi). Historical record — do
not rewrite.

## Problem

The OKF glossary is dead in code-heavy repos. CareerPlatform and
RKruiterApi carry byte-empty glossary tables untouched since legislation;
RKruiterAgent's is populated only because design sessions were actively
coining terms ("Judge", "Action tier") — the advisory "add terms as they
emerge" fires only when terms are being invented, never for terms that
already live in code. Three structural gaps: the template seeds an
intentionally empty table even when the repo is full of derivable terms
(the codebase map, by contrast, is derived at setup); `core/okf.md`'s
MANDATORY completion checklist — the instrument that keeps journal,
changelog, and OKF docs alive — has no glossary item; and no audit check
measures glossary vitality, so audits pass forever on an empty glossary.

## Decisions

1. **Seed at legislation.** `glossary.md.tpl`'s empty table becomes
   `{{GLOSSARY_TABLE}}`; Step 4 gains a mode-independent derivation rule
   (like `{{CODEBASE_MAP_TABLE}}`): derive 5–15 initial terms from the
   repo — domain entity/aggregate/service names in source, terms already
   defined in README/existing CLAUDE.md/overview — one row per term,
   confirmed with the user before writing. Nothing derivable → header-only
   table, noted in the Step 7 report. Migration §1 carves definition-like
   legacy content into glossary rows.
2. **Law.** `core/okf.md`'s completion checklist gains:
   `- [ ] New or renamed domain terms have a row in docs/okf/glossary.md;
   meanings of changed terms updated`.
3. **Detection.** Audit check 13, slug `glossary-vitality` (Warning):
   `docs/okf/glossary.md` exists with zero table body rows while at least
   one non-docs, non-hidden top-level source directory exists → finding
   with remedy pointing at restructure's derivation heal. Skip when the
   file is absent or the repo has no source directories.
4. **Heal.** restructure.md §2 `fix` gains "an empty glossary seeded per
   SKILL.md Step 4's derivation rules" — same precedent as `fix` filling
   unresolved `{{TOKEN}}`s. Approval-gated like every item.
5. **BL-019 riders ship in this cycle:** (1) service-locator bullet
   carve-out for scopes created via `IServiceScopeFactory`; (2)
   pair-development "never cut a new task branch *from main*…" + "parked"
   defined as an explicit recorded user decision; (3) import-line grader
   scoped to the "Needs your review" block; (4) check 9 elevates to
   Warning when a foreign config is predominantly law-shaped; (5)
   conventional-doc exemption extended to `docs/` and `.github/` variants.

## Eval plan

Rotted fixture's glossary is already empty and `src/` exists — defect 13
costs one marker: `glossary-vitality] docs/okf/glossary.md` (both
variants; counts twelve→thirteen). Fresh + migration graders assert ≥1
glossary body row (OrderService / InvoiceApi domains are derivable);
restructure grader asserts the glossary gains rows; idempotency unchanged
(seeding is create-once; a populated glossary re-runs clean). Full
benchmark → `evals/benchmarks/v11.md` vs v10's 98.

## Out of scope

- Glossary *staleness* detection (terms referencing removed code) — not
  deterministically checkable; revisit if empty-glossary enforcement
  proves insufficient.
- Any glossary write outside legislation/restructure — sessions maintain
  it under the okf.md checklist, not the skill.

## Downstream (phase 2)

CareerPlatform + RKruiterApi: v11 upgrade, then restructure applying only
the glossary heal (real domain terms, user-approved), each on a feature
branch per the branch-discipline law; the CareerPlatform branch also
re-lands the orphaned `branching.md` commit (0dd0ebd). RKruiterAgent:
upgrade only.
