# BL-487 — edition 29: verification back to always, dev-journal pointer merged, skills defer to on-demand law, verify stops lying about the journal entry

**Tier: 2 (full).** Blast radius: the law delivered to every legislated repository
(`skill/assets/templates/AGENTS.md.tpl`, `skill/assets/templates/opencode.json.tpl`,
`skill/assets/rules/core/skills.md`) and the engine that reads and proposes against it
(`src/Legislator.Engine/Jobs/ReportJob.cs`, `src/Legislator.Engine/Jobs/VerifyJob.cs`,
`src/Legislator.Engine/Runs/Step4Targets.cs`, `skill/SKILL.md`'s Step 5 migration wiring and
Step 7 report text, `skill/references/migration.md`) plus this repository's own delivered copy
(BL-484's D-3 gap). Novelty: low — this case reverses one half of BL-484's tier split
(verification.md back to always-tier), merges two on-demand pointers into one, extends an
existing "law beats skills" sentence, and fixes a mode-blind artifact check the engine already
has a working pattern for elsewhere (`evals/grade.py`'s `scaffold_artifacts`, BL-406). Branch
`bl/487-edition-29`; closes `sy11a/skills-legislator#75`.

**Spec type: feature**, with BL-484's follow-ups F-3, F-6, F-7 bundled in as completions.

## Boundary

**In scope** — the operator's five rulings (`issue-75.md`) and three BL-484 follow-ups still
open: the always/on-demand tier flip for `core/verification.md` (template, templates' opencode
mirror, `SKILL.md`, `references/migration.md`, the upgrade report's stale-pointer cleanup); the
changelog/dev-journal pointer merge (template, `SKILL.md`, `references/migration.md`, and the
report's template-line replace for a changed on-demand pointer); the `core/skills.md` "Law beats
skills" wording; `VerifyJob`'s mode-blindness over `Step4Targets`; `F-3` (a `ReviewLines`-level
non-default-layout test through the report); `F-6` (tightening the `check_static.py` R-009 check
past a loose substring match); `F-7`/BL-484's D-3 gap (this repository is missing
`.claude/rules/verification.md` and `docs/changes/README.md`, scaffolded here by hand from their
templates); the release mechanics for edition 29 (`skill/VERSION`, `Version.props`,
`release.json`, the change fragment, the e2e benchmark, self-delivery to this repo, and this
repository's own `AGENTS.md` reaching the edition-29 shape afterward, R-022).

**Out of scope** — `F-1` (stage skills in other repositories naming their trigger file) and `F-2`
(a tier split for stack rules and `.claude/rules/*.md`) stay open follow-ups; this case does not
touch them. `F-4` and `F-5` are already closed (`93e88ec`, `89ccee6`) and are not reopened here.
No general "stale pointer line" cleanup mechanism is introduced: the removal proposal this case
adds (R-005) is scoped to a rule whose own `@import` line in the template makes it always-tier —
never to a stack rule (always-tier only by the template's silence) and never to a pointer whose
wording merely differs from the template's for a rule that stays on-demand (that case is R-009's
narrower replace, not a removal, and BL-484 R-025/Q12's "an owner's unrelated rewording is left
alone" design otherwise stands). `Step4Targets.Snapshot` itself stays unfiltered for every
caller — `ApplyJob`, `ReportJob`, and `VerifyJob`'s own record persistence alike — because both
`ApplyJob` and `ReportJob` must still account for a row a lawful non-default-mode run created (a
migration day file, `AGENTS.md` renamed under Step 5) as `Created`/`Overwritten`, and `VerifyJob`'s
persisted snapshot feeds that same accounting; the mode filter this case adds applies only to the
"missing" failure list `VerifyJob` derives from the unfiltered snapshot, never to the snapshot
itself (round-1 refutation finding 6). Resolving `Step4Targets`' `<today>`
placeholder (unresolved in every mode, not just the wrong-mode case — `evals/grade.py:204-218`
already fixes this for the grader alone) is out of scope: this case only stops the engine from
checking a fresh-scaffold-only row in a mode where the row was never meant to exist. Generalizing
`apply` into a Step-4 scaffolder is out of scope: `apply`'s footprint is pinned to the owned set,
the manifest and the wiring (`evals/check_engine.py:1040-1045`); it does not and will not perform
Step 4's human-guided scaffolding, which is why R-021 below is written by hand, not delivered by
`apply`.

## Current behavior

- `skill/assets/templates/AGENTS.md.tpl:16-17` holds exactly two `@` lines (always tier):
  `pair-development.md`, `decision-gate.md`. `:24` carries `core/verification.md`'s on-demand
  pointer bullet ("before writing tests or implementation code, and before reporting done").
  `:25` carries `core/changelog.md`'s pointer bullet alone. `:28` carries a separate
  `core/dev-journal.md` pointer bullet ("before writing a fragment's `## journal` section").
  `skill/assets/templates/opencode.json.tpl:4-5` lists the same two always-tier paths.
- `src/Legislator.Engine/Runs/TemplateTiers.cs:69-104` derives the always/on-demand split from
  the template alone: an `@<core-prefix><name>` line is always-tier; any other line naming a
  rule's path is on-demand, and `FoldToOwned` (`:113-136`) already folds every path run on a
  pointer line, so one bullet may legally name more than one rule's path (verified: it maps each
  matched path to the *same* line text, `:95-103`) — nothing in the parser blocks merging the
  changelog and dev-journal bullets into one line.
- `src/Legislator.Engine/Jobs/ReportJob.cs:260-287`'s `ReviewLines` loop has two branches per
  owned rule: the on-demand branch (`:262-280`) proposes removing a stale `@import` and adding
  the template's current pointer line, guarded by `EntryNamesRuleWithoutImport` (`:314-322`) so
  an owner's reworded pointer is never re-proposed (BL-484 R-025); the always-tier branch
  (`:283-286`) only proposes adding a missing `@import` — it never inspects whether the entry
  document still carries the rule's *old* on-demand pointer line. For a repository at edition 28
  with `AGENTS.md`'s `core/verification.md` pointer bullet and no `@import`, upgrading to a
  template where `verification.md` is now always-tier proposes `- add ... @docs/ai/rules/core/
  verification.md` and nothing else: the stale pointer line is never flagged for removal. A
  second, separate gap: for the merged changelog/dev-journal bullet, `TemplateTiers` maps both
  rules' paths to the *same* merged line (`:95-103`), but `ReviewLines` calls `review.Add` once
  per rule in its `foreach` over `ownedRules` (`:276-280`); an edition-28 entry importing
  everything with `@` names both rules and, because neither is yet imported, both iterations
  propose the identical add-line text — the review section would carry it twice.
- `skill/SKILL.md:147`'s Step 5 migration summary ("the two `@docs/ai/rules/core/...` always-tier
  import lines (`pair-development.md`, `decision-gate.md`), one pointer line per on-demand core
  rule") and `skill/SKILL.md:168`'s Step 7 "Needs your review" law text (describing the
  always-tier branch as only ever proposing an `@import`-add, never a removal) both go stale the
  moment `verification.md` moves tier and R-005 gives the always-tier branch a removal proposal.
  `skill/references/migration.md:22,25,27` describes the same stale counts. The Step 1 edge case
  at `SKILL.md:26` checks only the `pair-development.md` import line and needs no change.
- `skill/SKILL.md:147`'s Step 5 wiring is derived by `evals/grade.py`'s `migration_wiring()`
  (`:523-527`: every always-tier `@import` line plus every on-demand pointer line plus
  `## Boundaries`) and pinned at `evals/grade.py:1870-1874` to `len(wiring) == 12` (today: 2
  imports + 9 pointers + `## Boundaries`). Moving `verification.md` to an `@` line and merging the
  dev-journal bullet into the changelog bullet changes this to 3 imports + 7 pointers +
  `## Boundaries` = **11**, not 12.
- A repository's `AGENTS.md` naming a now-merged on-demand rule's path on its *old*, pre-merge
  pointer line (e.g. the edition-28 standalone `core/dev-journal.md` bullet) is, today, simply
  left alone by the on-demand branch's `EntryNamesRuleWithoutImport` guard (BL-484 R-025) — the
  report proposes nothing, even though the template's current line for that rule now reads
  differently and also names a second rule's path.
- `skill/assets/rules/core/skills.md:5`'s "Law beats skills" bullet names only
  `docs/ai/rules/**` and `.claude/rules/**`. `skill/assets/rules/core/project-rules.md:4` already
  allows a project rule to live outside `.claude/rules/`, reached only by a pointer line in the
  entry document — a rule in that shape is not covered by the literal wording of the "Law beats
  skills" bullet, so a skill contradicting such a rule is not told the rule wins.
- `skill/SKILL.md:116`'s Step 4 table row for `docs/journal/<today>.md` carries the note
  "**Fresh-scaffold mode only** — an upgrade writes no entry". `src/Legislator.Engine/Runs/
  Step4Targets.cs:27-57`'s `Of`/`Snapshot` reads every row in the table (skipping only the
  `(empty` directory marker, `:22,54`) regardless of which mode the current run is in, and takes
  no `mode` parameter at all. `src/Legislator.Engine/Jobs/VerifyJob.cs:68-71` checks every
  returned target for existence and reports any absent one as "Step 4 artifact missing →
  scaffold it". `VerifyJob` never calls `Detection.Of` and reads no run mode at all today. Run on
  a repository last brought current by an upgrade, `verify` always reports
  `docs/journal/<today>.md` missing, because the row is never excluded for that mode — this is
  true regardless of whether verify runs standalone or, as Step 6 always does (every mode: fresh,
  migration, upgrade), as part of a full run. `evals/grade.py:81-110`'s `scaffold_artifacts(mode)`
  already filters exactly this row by its notes column for the grader (fixed by BL-406, `docs/
  cases/BL-406-the-grader-reads-the-mode/`); the engine's own `Step4Targets` has no equivalent
  filter. `src/Legislator.Engine/Jobs/ReportJob.cs:85` already reads the run's mode back off the
  `--record` file's `RunRecord.ModeKey`, falling back to the literal string `"fresh"` only when
  there is no record to read.
- `evals/check_static.py:116-119`'s check for `core/project-rules.md` asserts only that the
  literal substrings `"pointer"` and `".claude/rules/"` both appear in the file — a weak test
  that would pass against unrelated prose containing both words (BL-484 F-6).
- `docs/cases/BL-484-core-rules-on-demand/plan.md:426,442` (F-3): the non-default `RulesDir`/
  `OkfDir` case is pinned only at the `TemplateTiers` helper level (`TemplateTiersTests.cs:192`),
  never end to end through `ReportJob.ReviewLines`.
- BL-484's D-3 gap (F-7): this repository's own `.claude/rules/` and `docs/changes/` are missing
  `verification.md` and `README.md`. Both have create-if-absent Step 4 rows (`SKILL.md:117,123`,
  templates `verification-rules.md.tpl` and `changes-README.md.tpl`, both used verbatim), but
  `apply`'s footprint is pinned to the owned set, the manifest and the wiring
  (`evals/check_engine.py:1040-1045`) — it performs none of Step 4's scaffolding. Nothing in this
  repository has written either file by hand since BL-484 introduced the rows; `legislator
  verify` reports both missing today.
- `skill/VERSION` reads `28`; `src/Legislator.Cli/Version.props:9` reads `28.0.0`; `skill/assets/
  release/release.json:2,4` reads edition `28.0.0` with a filled `linux-x64` digest.

## Requirements

- **R-001** — `AGENTS.md.tpl` SHALL import `core/verification.md` on an `@` line alongside
  `pair-development.md` and `decision-gate.md` (always tier).
- **R-002** — `AGENTS.md.tpl`'s `### Read on demand` block SHALL drop the standalone
  `core/verification.md` pointer bullet.
- **R-003** — `opencode.json.tpl`'s `instructions` array SHALL list `core/verification.md` among
  the always-tier paths it already lists `pair-development.md` and `decision-gate.md` under.
- **R-004** — `skill/SKILL.md:147`'s Step 5 migration-wiring prose and `skill/references/
  migration.md`'s rewrite-order and "new sections to add" prose SHALL describe three always-tier
  `@import` lines, naming `verification.md` alongside the other two.
- **R-005** — WHEN a rule is always-tier in the running template (named on its own `@import`
  line) AND the entry document still carries a line naming that rule's path outside any `@import`
  line, AND that line is in the pointer grammar (`- Before <trigger>, read `<path>` — it is law,
  not a reference.`) naming that rule alone — the same grammar gate R-010 uses — THEN the upgrade
  report's "Needs your review" section SHALL propose removing that stale pointer line —
  independently of whether the add-`@import` proposal also fires for the same rule (e.g. because
  the import is already present), using the same text-presence test the on-demand branch already
  uses, never a hardcoded rule name. A stack rule (always-tier only by the template's silence,
  never named on its own `@import` line in the template) is not subject to this proposal. A line
  naming the rule's path outside this grammar (e.g. the owner's own prose mentioning the path) is
  left alone.
- **R-006** — `AGENTS.md.tpl`'s changelog pointer bullet SHALL name both `core/changelog.md` and
  `core/dev-journal.md` as the files its trigger governs.
- **R-007** — `AGENTS.md.tpl`'s `### Read on demand` block SHALL drop the standalone
  `core/dev-journal.md` pointer bullet.
- **R-008** — `skill/SKILL.md:147`'s Step 5 wiring prose and `skill/references/migration.md`
  SHALL reflect the merged changelog/dev-journal bullet (one fewer on-demand pointer line than
  before, and the resulting wiring-string count of 11: 3 imports + 7 pointers + `## Boundaries`,
  replacing 2+9+1=12).
- **R-009** — `skill/SKILL.md:168`'s Step 7 "Needs your review" law text SHALL describe R-005's
  removal proposal for an always-tier rule and R-010's replace proposal (remove-old-line plus
  add-template's-line, deduplicated per R-014) for an on-demand rule whose entry pointer is in the
  grammar but differs from the template's current line — not only the original add-`@import`-only
  and never-re-proposed-reworded-pointer description.
- **R-010** — WHEN an on-demand rule's template pointer line differs from the entry document's
  existing line naming that rule, AND the entry's line is in the pointer grammar (`- Before
  <trigger>, read `<path>` — it is law, not a reference.`) naming that rule alone, THEN the report
  SHALL propose replacing it with the template's current line (one remove-line proposal plus one
  add-line proposal, the add deduplicated per R-014). A line outside this grammar is the owner's
  own wording and is left alone (BL-484 R-025 still holds).
- **R-011** — `core/skills.md`'s "Law beats skills" bullet SHALL state that the rule also wins
  over a contradicting skill when the rule is a project rule reached only by its own pointer line
  outside `.claude/rules/**` (per `core/project-rules.md`), not only a rule under
  `docs/ai/rules/**` or literally under `.claude/rules/**`.
- **R-012** — WHEN `VerifyJob` derives its list of "Step 4 artifact missing" failures from
  `Step4Targets.Snapshot`'s result, THEN the `docs/journal/<today>.md` target alone (identified by
  path, not by its notes text — the `AGENTS.md` row's own note also reads "Fresh-scaffold mode
  only" but stays in the failure list in every mode, per ruling 4, which is about the journal row
  alone) SHALL be excluded from that failure list when the run's mode differs from fresh-scaffold.
- **R-013** — `VerifyJob` SHALL take its mode only from `RunRecord.ModeKey` in the `--record`
  file (the field `ReportJob` reads); with no record, or a record without that key, the journal row
  stays in the failure list (no `Detection.Of` fallback: after apply has written the manifest it
  can never return fresh; code review round 1, ruled by the orchestrator).
- **R-014** — WHEN `ReviewLines` proposes adding the same merged on-demand pointer line for more
  than one rule the line names (e.g. the merged changelog/dev-journal bullet), THEN the "Needs
  your review" section SHALL propose that add-line text once, not once per rule.
- **R-015** — `evals/check_static.py`'s check for `core/project-rules.md`'s on-demand note SHALL
  assert the bullet's own sentence text, not merely the co-occurrence of the substrings
  `"pointer"` and `".claude/rules/"` (completes BL-484 F-6).
- **R-016** — `ReportJob.ReviewLines`'s tier-aware proposals SHALL be covered by an end-to-end
  test using a non-default `RulesDir`/`OkfDir` layout, not only at the `TemplateTiers` helper
  level (completes BL-484 F-3/T-09).
- **R-017** — `skill/VERSION`, `src/Legislator.Cli/Version.props`'s major version, and
  `skill/assets/release/release.json`'s edition SHALL together read 29.
- **R-018** — `release.json`'s per-platform digests SHALL be emptied (`digests: {}`) until tag
  time, as every prior edition bump does.
- **R-019** — A change fragment SHALL record this case under `docs/changes/BL-487.md`.
- **R-020** — The full e2e benchmark SHALL be recorded at `evals/benchmarks/v29.md`, run before
  the delivery commit to this repository, exercising at least one fixture shaped like an
  edition-28 repository (both tier-flip pointer bullets present, `verification.md` and the
  standalone `dev-journal.md` bullet, no `@import` for `verification.md`) upgrading to edition 29,
  so R-005's removal and R-010's replace are both exercised by the benchmark, not only by unit
  tests.
- **R-021** — This repository's `.claude/rules/verification.md` and `docs/changes/README.md`
  SHALL exist, written by hand from `verification-rules.md.tpl` and `changes-README.md.tpl`
  (verbatim, no placeholders, per `SKILL.md:117,123`) — a production task, not an `apply`
  side-effect (`apply` does not scaffold Step 4 artifacts, per the Boundary). Done when
  `legislator verify` run against this repository no longer reports either file missing.
- **R-022** — This repository's own `AGENTS.md` SHALL be brought to the edition-29 shape (the
  `verification.md` `@import` line added, its old on-demand bullet and the standalone
  `dev-journal.md` bullet removed, the changelog bullet merged) — a production task done after
  self-delivery. Done when `legislator report` run against this repository proposes no tier
  lines (no R-005/R-010/R-014 "Needs your review" proposal under this case's rules).
- **R-023** — `skill/SKILL.md:147` and `skill/references/migration.md:25,27` SHALL reword "one
  pointer line per on-demand core rule" to "one per line in the template's `### Read on demand`
  block" — true after the merge, where one line now names two rules.
- **R-024** — `skill/SKILL.md:157`'s Step 6 verify law text ("confirms every artifact from Step
  4's table exists") SHALL be amended to "…except the `docs/journal/<today>.md` row outside
  fresh-scaffold mode", so R-012's narrower filter is not contradicted by Step 6's own prose.
- **R-025** — `Step4Targets.Snapshot` SHALL stay unfiltered for every caller, including
  `VerifyJob`'s own record persistence (R-012's filter applies only to `VerifyJob`'s failure
  list), so `ApplyJob`'s and `ReportJob`'s `Created`/`Overwritten` accounting (and `VerifyJob`'s
  own persisted snapshot, which `ReportJob` reads back) never loses a row a lawful
  non-default-mode run created, per the Boundary.

## Hurting case

**An edition-28 repository upgrades to edition 29.** `AGENTS.md` carries `@pair-development.md`,
`@decision-gate.md`, a `core/verification.md` pointer bullet under `### Read on demand`, a
`core/changelog.md` pointer bullet, and a separate `core/dev-journal.md` pointer bullet — the
edition-28 shape exactly. GIVEN this repository, WHEN `legislator report` runs against the
edition-29 package, THEN the "Needs your review" section proposes `- add ... @docs/ai/rules/core/
verification.md` AND `- remove from AGENTS.md: the pointer line naming core/verification.md
(now always-tier: imported, not read on trigger)` (R-005), both appearing regardless of order;
AND, because both the `core/changelog.md` bullet and the `core/dev-journal.md` bullet are in the
pointer grammar, each naming one rule alone, and each differs from the template's merged line, it
proposes `- remove from AGENTS.md: the pointer line naming core/changelog.md` AND `- remove from
AGENTS.md: the pointer line naming core/dev-journal.md` AND `- add ... <the merged
changelog/dev-journal pointer line>` once, not twice (R-010, R-014) — two removes, one add. GIVEN
the same repository has just been brought
current by that upgrade, WHEN `legislator verify` runs, THEN it does NOT report
`docs/journal/<today>.md` missing (R-012, R-013) — before this case, it always did, because
`Step4Targets` read the Step 4 table's "Fresh-scaffold mode only" row in every mode alike.

## Clarifications

- **Q1 — generalize R-005, or hardcode to `verification.md`?** Closed: generalize. Gate it on
  "the rule is always-tier in the running template (named on its own `@import` line) AND the
  entry's matching line is in the pointer grammar R-010 also uses" — not `EntryNamesRuleWithoutImport`
  alone, which also matches owner prose merely mentioning a rule's path (rulings2 #3) — rather than
  naming `verification.md` specifically. This is the same no-closed-list constraint BL-484 already
  put on the engine (R-015/R-016), and it means the next rule the operator moves back to
  always-tier gets the same cleanup for free.
- **Q2 — does R-005's removal proposal also apply inside Step 5 migration mode's direct
  rewrite?** Settled: `ReviewLines` runs in every mode alike (`ReportJob.cs:156`), not only
  upgrade — migration mode finds nothing to propose only because Step 5 rewrites `AGENTS.md`
  wholesale from the template first (`references/migration.md:27`), leaving no surviving stale
  line by the time `ReviewLines` runs. Closed.
- **Q3 — why does R-012's filter not port `grade.py`'s `SCAFFOLD_ONLY_RE` (`:164-165`)
  verbatim as a notes-column regex?** Closed: that pattern matches on notes *text*
  ("fresh-scaffold mode only", case insensitive), and the `AGENTS.md` row's own note ("Only in
  fresh-scaffold mode… legacy migration mode handles this file per Step 5 instead") matches the
  identical phrase. `grade.py` can afford that — nothing downstream of `scaffold_artifacts(mode)`
  needs the `AGENTS.md` row reported missing. `VerifyJob`'s failure list cannot: the `AGENTS.md`
  row must stay reported missing in every mode (R-012). So the row this case narrows the filter
  to is identified by its **path** (`docs/journal/<today>.md`, the one row the ruling names), not
  by matching the shared notes phrase — a path match cannot also catch `AGENTS.md`'s row, which
  has a different path, by construction.
