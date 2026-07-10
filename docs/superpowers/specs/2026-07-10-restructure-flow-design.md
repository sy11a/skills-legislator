# Restructure Flow — Design (BL-004 + BL-005b, one combined cycle)

**Date:** 2026-07-10
**Status:** Draft — awaiting user review (Wave 3 gate)
**Backlog:** BL-004, BL-005b (docs/backlog.md); rides: the BL-012 Wave 3
rider (candidates-section placement anchors; Keep list heading level).

## Goal

The repair capability: a repo whose AI layer (legislator-built or foreign)
has gone chaotic gets diagnosed, a restructuring plan proposed, and — only
after explicit approval — applied, with absolute content fidelity. This is
deliberately the last feature: it is the most destructive if wrong, so it
stands on audit (visibility), keep-markers (protection), and its own eval
scenario landing in the same cycle (fidelity net first, repair second).

## The protocol: diagnose → propose → approve → apply

A new top-level SKILL.md section, run when the user asks to
restructure/repair/reorganize the AI layer (not on any normal run — never a
side effect of an upgrade).

1. **Diagnose.** Run the full Audit section first (all 10 checks +
   candidates appendix), zero writes. The plan consumes its findings.
2. **Propose.** Print a plan — still zero writes — with the pinned format
   below. Every item is one of a closed action set:
   - `move` — relocate a file to its standard-layout home (e.g.
     `.claude/plans/x.md` → `docs/superpowers/plans/x.md`), fixing
     references to the old path.
   - `merge` — fold a foreign config's content into its standard home
     (e.g. `.cursorrules` prose → CLAUDE.md project sections), then remove
     the emptied source file.
   - `link` — wire an orphan into the layer (index/CLAUDE.md reference);
     orphans are linked by default, never deleted (deleting content is
     never proposed — the user can ask for it, at which point it is its own
     explicit decision item).
   - `fix` — repair a dangling import/link or fill an unresolved
     `{{TOKEN}}` (value derived per Step 4's rules or asked).
   - `heal` — owned-layer drift/staleness: run Steps 2–3 (the normal
     upgrade path) as one plan item; never hand-edit owned files.
   - `decision` — anything conflicting with an owned rule or otherwise
     unresolvable without the user (the decision-gate rule). **Never
     applied, even under blanket approval** — each decision item is asked
     and answered individually or stays open, re-surfaced by every future
     run.
   **Kept paths are immovable:** every manifest `keep` entry is listed in
   the plan under "Kept (immovable)" and no item may move, merge, or edit
   it (linking TO a kept file is allowed — that's how an unlinked kept file
   gets fixed).
3. **Approve.** Hard stop. Nothing is applied until the user approves —
   item-by-item or "apply all" (which still excludes `decision` items).
   Unapproved items are simply skipped and reappear in future runs.
4. **Apply + verify.** Execute approved items. Then the **fidelity pass**:
   every content line of every file this run moved, merged, or edited must
   be greppable in the post-state (moves/merges must land byte-preserving
   prose; a line that would be lost blocks that item — skip it, report it,
   never drop content silently). The final report carries the plan with
   per-item outcomes (`applied` / `skipped` / `blocked: <why>` /
   `decision: open`) and ends with `Fidelity: verified (<N> lines tracked)`
   or an explicit failure. Second invocation on a fully-restructured repo
   proposes nothing (except still-open decision items) and writes nothing.

## Plan / report format (pinned)

```
# AI-Layer Restructure Plan — <repo name>, <ISO date>

1. [move] .claude/plans/importer-plan.md → docs/superpowers/plans/importer-plan.md: non-standard plans location
2. [merge] .cursorrules → CLAUDE.md ## Project notes: foreign AI-layer config
3. [decision] CLAUDE.md "…" conflicts with docs/ai/rules/core/changelog.md: <one-line nature of conflict>
...

Kept (immovable): docs/notes/special-sauce.md — works as-is
```

The apply-phase report repeats the items with outcomes appended after each
(`— applied`, `— decision: open`, …). Headings and the `[action]` bracket
slugs are byte-pinned (the eval greps them), same discipline as the audit
report.

## Where the how-to lives

SKILL.md's new section carries the protocol, action set, plan format, and
the fidelity/keep/decision laws — nothing else. The mechanics (how to split
foreign config content, reference-fixing after moves, what "standard
layout" maps each artifact type to) go in a new **`references/
restructure.md`**, which extends `references/migration.md`'s carve-out
discipline from "CLAUDE.md only" to the whole layer and cross-links it
rather than duplicating it. (Mirrors the existing migration pattern: law in
SKILL.md, how-to by pointer.)

## BL-012 Wave 3 rider (rides along, needs this cycle's benchmark)

1. Step 7's candidates-section placement gains "(when those sections are
   absent, make it the report's last section)".
2. The Keep list section's heading is pinned to `### Keep list` (the only
   Step 7 section without a pinned level — same drift class the v7.3
   migration run exhibited).

## BL-005b — eval scenario (lands in the same cycle, graded before BL-004 is declared done)

- **New fixture** `restructure/repo`: the rotted-layer generator,
  parameterized (`materialize_rotted(dest, restructure_extras=True)`), so
  the audit fixture stays byte-comparable across versions. Extras:
  1. `.claude/plans/2026-01-importer-plan.md` with fidelity sentence
     "Planned: split the importer into reader and writer stages." (a
     `move` target).
  2. A CLAUDE.md line "We do not maintain CHANGELOG.md; release notes are
     written in the wiki at release time." — deliberate conflict with the
     owned changelog rule (the `decision` bait). Planted in the
     restructure fixture only, so the audit scenario's marker set and
     harvest behavior stay untouched.
  3. `fixture_meta.json` gains `fidelity_sentences` — one distinctive
     sentence per relocatable/mergeable plant (the plan file's sentence,
     `.cursorrules`' "Always write tests first.", orphan-notes' content
     line, special-sauce's content line) — and `conflict_marker` (the
     wiki-changelog line).
- **New scenario `restructure`:** the eval agent is prompted to restructure
  with blanket approval pre-granted ("apply everything you propose except
  items needing my decision — leave those open"). It saves the final report
  to `<ws>/restructure/outputs/restructure-report.md`. Grader asserts:
  1. every `fidelity_sentences` entry greppable somewhere in the repo
     (zero content loss);
  2. the kept file byte-identical at its original path;
  3. the conflict NOT auto-resolved: `conflict_marker` still present in
     CLAUDE.md **and** the report names it in a `[decision]` item;
  4. standard layout reached: `.claude/plans/` gone with its file at
     `docs/superpowers/plans/`, `.cursorrules` gone with its sentence
     surviving (via the merge), the ghost-rule import line gone from
     CLAUDE.md, `docs/ai/rules/core/okf.md` byte-identical to source
     (heal), manifest at current VERSION;
  5. orphan-notes linked (referenced per the audit's definition), not
     deleted.
- **`idempotency:restructure`:** commit run 1, re-run with the same prompt
  — zero diff (the open decision item alone may be re-reported; it changes
  no bytes).
- evals.json + evals/README.md rows/prompts accordingly.

## Out of scope

- Deleting any project content (only `merge` removes files, and only after
  their content verifiably lives elsewhere).
- Auto-resolving conflicts with owned law (permanent design, not deferred).
- Restructuring source code or anything outside the AI layer
  (CLAUDE.md, `docs/**`, foreign AI-config files at the root).
- Persisting the plan anywhere in the target repo (chat-only, recomputable
  — same rationale as harvest).

## Verification

`check_static.py`; full e2e benchmark per `evals/README.md` — now five
scenarios + three idempotency runs. VERSION stays 7 (procedure + references
+ evals only); record as `evals/benchmarks/v7.4.md` against v7.3. Expected:
all v7.3 assertions hold unchanged (audit fixture untouched by the extras
flag); restructure scenario green incl. the open decision item; all three
idempotency runs zero-diff.