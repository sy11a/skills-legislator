# Keep-Markers — Design (BL-002, with BL-011 riding)

**Date:** 2026-07-09
**Status:** Draft — awaiting Gate 1 review
**Backlog:** BL-002, BL-011 (docs/backlog.md)
**Gate 0 decision (user-settled):** keep entries serialize as an array of
single-line inline objects, sorted by path.

## Goal

Ultra-specific project artifacts that work as-is can be marked untouchable
*and* must stay wired into the layer — protection without orphaning. The
manifest gains a `keep` list; audit check 10 goes from forward-compat stub to
real enforcement; restructure (BL-004, later) will treat kept paths as
immovable.

## Decisions

1. **Schema & serialization (Gate 0).** `docs/ai/manifest.json` key order
   becomes `legislatorVersion`, `profiles`, `keep`, `ownedFiles`. `keep` is
   always present: `[]` inline on one line when empty; otherwise one entry
   per line, sorted by `path`, each a single-line inline object with keys in
   the order `path`, `reason`:

   ```json
   {
     "legislatorVersion": 7,
     "profiles": ["dotnet"],
     "keep": [
       {"path": "docs/notes/deploy-runbook.md", "reason": "battle-tested, do not restructure"}
     ],
     "ownedFiles": [
       ...
     ]
   }
   ```

   Two runs with no keep change produce a byte-identical manifest.

2. **Entries are user-driven only.** The skill never invents keep entries.
   They are added when the user asks — during any run ("keep
   `docs/notes/x.md` as-is: <reason>") or as a standalone request. Adding:
   validate the path exists in the repo (refuse + report if not), dedupe by
   path (re-marking replaces the reason). Removing: by user request only.
   Every run reports keep additions/removals in Step 7's "Applied" section;
   proposing keep *candidates* is BL-004's decision-gate territory, not
   BL-002's.

3. **Carry-forward is deterministic.** Upgrade runs read the old manifest's
   `keep` (defaulting to `[]` when the key is absent — pre-keep manifests),
   apply any user add/remove requests from this run, and re-serialize in the
   pinned form. No user request → entries survive byte-identically.

4. **Kept ≠ owned.** Kept files are project-owned content: never copied,
   never overwritten, never deleted by the skill. The keep list is metadata
   about them, living in the machine-managed manifest.

## Audit changes (SKILL.md audit section)

**Check 10 (Warning) becomes real enforcement:** if the manifest has a
`keep` key, every kept path must (a) exist on disk — finding: "kept path
missing from disk → restore it or remove the keep entry", and (b) be
*referenced* from somewhere in the layer — finding: "kept but referenced
from nowhere → link it from `docs/okf/index.md` or CLAUDE.md". If the key is
absent, the note goes to the **Info** section (resolves BL-011 item 2):
`- [keep-list] docs/ai/manifest.json: no keep key (pre-keep-schema manifest)
→ re-run /legislator to refresh`.

**Shared "referenced" definition (resolves BL-011 item 1):** for both check
7 (orphan docs) and check 10, a file counts as referenced when any other
`.md` under the repo (or CLAUDE.md) mentions it by markdown link, `@import`,
**or inline-code path mention** (the backticked relative path). This fixes
the false-positive orphan flags on the constitution's own hub files
(`docs/okf/index.md` is named in okf.md's rule text, `docs/okf/glossary.md`
in CLAUDE.md's pointer bullet) generically instead of by-name exemption, and
gives kept files a natural cheap way to be "wired in".

## BL-011 riding items

1. Orphan-check hub false positives — resolved by the shared referenced
   definition above, locked in by new negative eval markers (below).
2. Check 10's "not present" note placement — resolved: Info section, exact
   line format above.
3. `references/migration.md` §1's glossary pointer quote is aligned to
   CLAUDE.md.tpl's exact text:
   `- Domain glossary: \`docs/okf/glossary.md\` — check it when a term is
   unclear; add terms as they emerge` (leading bullet + backticks included).

## SKILL.md changes

- Step 3.6: serialization spec updated to the four-key order with the `keep`
  rules above (empty inline `[]`, populated one-per-line sorted).
- New short paragraph in Step 3 (before 3.6): collect keep add/remove
  requests from the user's prompt; validate; merge into the carried-forward
  list.
- Audit section: check 10 rewritten; the "referenced" definition stated once
  and shared by checks 7 and 10.
- Step 7: keep changes listed under "Applied".

## Eval changes

- **Upgrade fixture** gains a healthy keep entry: file exists and is linked
  from `docs/okf/index.md`; manifest carries it in pinned form. Grader:
  after the upgrade run the entry survives byte-identically; manifest key
  order and keep line format asserted by regex (extending the existing
  `manifest_profiles_single_line_inline` style).
- **Fresh/migration grader:** manifest has `"keep": []` inline.
- **Rotted fixture, 10th planted defect:** manifest keep-lists
  `docs/notes/special-sauce.md` — the file exists but nothing references it.
  Audit must flag it (new report marker in `fixture_meta.json`).
- **Negative markers (new grader concept):** the audit report must NOT flag
  `docs/okf/index.md` or `docs/okf/glossary.md` in any finding line — locks
  in the BL-011 item-1 fix without a separate clean-audit scenario.
- **Populated-keep idempotency:** this benchmark adds `idempotency:upgrade`
  (second run on the upgrade repo after committing run 1) to prove zero-diff
  with a populated keep list. Kept as a permanent scenario.

## Out of scope

- Keep-candidate proposals by the skill (BL-004's decision gate).
- Restructure honoring kept paths (BL-004).
- Harvest (BL-003).

## Verification

`check_static.py` (no new templates — no change expected). Full e2e
benchmark per `evals/README.md` with the audit scenario's new marker set and
the new `idempotency:upgrade` scenario. VERSION stays 7 — no
`assets/rules/**` content changes; record as `evals/benchmarks/v7.2.md`
against v7.1. Expected: all prior assertions hold; new keep assertions
green; both idempotency scenarios zero-diff.
