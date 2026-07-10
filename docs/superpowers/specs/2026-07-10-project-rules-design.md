# Project Rules — Design (BL-014, constitution v8, with BL-013 riding)

**Date:** 2026-07-10
**Status:** Approved by user (plan-mode approval 2026-07-10; decisions:
location `.claude/rules/`, full integration in one cycle)
**Backlog:** BL-014, BL-013 riders (docs/backlog.md)

## Goal

Give project-specific rules ("every feature ships behind a feature toggle")
a home that doesn't bloat CLAUDE.md. Claude Code natively supports
`.claude/rules/` (official memory docs): every `.md` there loads at launch
with CLAUDE.md priority, no `@import` wiring, with optional `paths:`
frontmatter for path-scoped loading. This completes the two-strata design:
`docs/ai/rules/**` = fleet law (machine-managed, write-guarded),
`.claude/rules/**` = project law (human/agent-managed; legislator scaffolds
the directory but never touches its content).

**This is the first constitution-content change: `skill/VERSION` 7 → 8;
benchmark records as `evals/benchmarks/v8.md`.**

## Decisions

1. **New core rule `core/project-rules.md`** — the law every agent reads via
   CLAUDE.md's import block: project rules live in `.claude/rules/<topic>.md`,
   one topic per file, law-shaped, path-scoped via `paths:` frontmatter when
   partial, never in `docs/ai/rules/**`, never inlined into CLAUDE.md's
   body, kept small. This is the workflow the user described: agent consults
   CLAUDE.md → learns the convention → creates the auto-loaded rule file.
2. **Scaffolding:** Step 4 gains a `.claude/rules/` empty-directory row;
   `CLAUDE.md.tpl` gains one pointer bullet next to the glossary pointer.
3. **Migration carves law there:** law-shaped project-specific constraints
   from a legacy CLAUDE.md go to `.claude/rules/<topic>.md` (verbatim,
   grouped by topic); instance data (branch conventions, build commands,
   contacts, environment notes) stays in CLAUDE.md.
4. **Harvest** scans `.claude/rules/**` as a candidate source (prime home
   of promotable law).
5. **Audit:** new check 11, slug `project-rules` (Warning): a
   `.claude/rules/*.md` statement contradicting an owned rule (decision-gate
   material — remedy: align it or record an explicit exception). Check 2's
   `{{TOKEN}}` scope extends to `.claude/rules/**`. Check 9
   (foreign-structures) explicitly excludes `.claude/rules/` — it is
   standard layout now.
6. **Restructure:** the standard-layout table gains the row — law-shaped
   prose (e.g. from `.cursorrules`) merges into `.claude/rules/<topic>.md`;
   narrative prose into CLAUDE.md as before. Conflicting project rules are
   `decision` items like any owned-law conflict.
7. **BL-013 riders ship in this cycle:** (a) fidelity-pass inventory scoped
   to move/merge sources, with deletions that are an approved item's stated
   purpose exempt; (b) Restructure runs Step 0's dirty-tree warning first;
   (c) restructure.md §5's AI-layer parenthetical reworded to a definition;
   (d) the decision-item line shape pinned with its own example
   (`N. [decision] <where> ↔ <owned rule>: <one-line conflict>`).

## Eval changes

- **Fresh/migration:** grader asserts the `.claude/rules/` directory exists
  after scaffold.
- **Migration:** two new checks — the decimal-money law lands under
  `.claude/rules/` (grep scoped there), and the branch convention does NOT
  (instance data stays in CLAUDE.md).
- **Rotted fixture, defect 11:** planted `.claude/rules/journal.md` whose
  rule ("journal entries are optional; skip them for small changes")
  contradicts `core/dev-journal.md`. Audit must flag it under the
  `project-rules` slug (new report marker).
- **Restructure fixture** inherits defect 11 → a second `decision` item.
  Grader asserts the planted file's content is byte-unchanged after the run
  and the report names it (decision-gated, never auto-edited).
- **Upgrade fixture** auto-tracks v8: the generator withholds the
  alphabetically last core rule, which is now `project-rules.md` — the
  upgrade scenario therefore tests delivering exactly this new rule.
- Full benchmark: five scenarios + three idempotency passes → `v8.md` vs
  `v7.4.md`.

## Out of scope

- Any write to `.claude/rules/**` content by legislator runs (scaffold
  creates the empty directory only; migration writes carved files ONCE at
  migration time — after that the directory is project-owned like CLAUDE.md).
- Migrating existing legislated repos' CLAUDE.md sections into
  `.claude/rules/` (restructure can propose it case-by-case; no automatic
  sweep).
- Path-scoping heuristics (agents/users choose `paths:` themselves; the
  rule file documents the option).

## Verification

`check_static.py` (covers the new rule file's well-formedness
automatically) + `check_hooks.py`; full e2e benchmark per `evals/README.md`
recorded as `evals/benchmarks/v8.md` against v7.4; final whole-cycle review
before done. VERSION bumps to 8 in the same commit that adds the rule file
(repo rule).
