# BL-487 — plan

## Research

**D1 — the tier flip and the pointer merge both reduce to one fact already proven generic
(BL-484).** `TemplateTiers.Read` (`src/Legislator.Engine/Runs/TemplateTiers.cs:69-104`) derives
the always/on-demand split from `AGENTS.md.tpl` alone, and `FoldToOwned`
(`:113-136`, comment at `:94`) already folds every `.md` path run on one pointer line into that
same line's text — the parser was already fixed (BL-484 "f3") to accept two paths on one bullet.
So moving `verification.md` to an `@` line and merging the dev-journal bullet into the changelog
bullet are **template-only edits**: no change to `TemplateTiers.cs` is required. Confirmed by
reading the regex at `:69-71` (always-tier: any `@<core-prefix><name>` line) and the loop at
`:84-104` (on-demand: every other line, `FoldToOwned` called once per known prefix per line).

**D2 — the stale-pointer-removal gap (R-005) is a small addition to an existing branch, and the
merge's duplicate-proposal gap (R-014) is a dedupe, not a new mechanism.** `ReportJob.ReviewLines`'s
always-tier branch (`:283-286`) is:
```csharp
if (!imports.Contains(rule, StringComparer.Ordinal))
{
    review.Add($"- add to `{entryName}`: `@{rule}`");
}
```
`EntryNamesRuleWithoutImport` (`:314-322`) already answers "does the entry text name this rule's
path outside its own `@import` line?" — exactly the test needed to spot a surviving on-demand
pointer for a rule that is now always-tier, but on its own it is too loose: a second refutation
round found it also matches owner prose merely mentioning a rule's path (e.g. this repository's
own `AGENTS.md:26` naming `core/pair-development.md` in a sentence, not a pointer line), which
would false-propose a removal. R-005 therefore gates the same way R-010 does — the matched text
must also be in the pointer grammar (`- Before <trigger>, read `<path>` — it is law, not a
reference.`) naming the rule alone — before proposing the remove; the check fires *ungated* from
whether the add-`@import` line above also fires (the refutation's finding: tying removal to the
add proposal means that once the owner applies the add, the stale pointer is never flagged
again), and is restricted to `tiers.Always.Contains(rule)` so a stack rule's prose never triggers
a false "remove" (`tiers.Always` is `TemplateTiers`' set of rules found on their own `@import`
line in the template — a stack rule is always-tier only by the template's silence about it, never
a member of this set). Separately, R-010/R-014 close two more gaps the same loop has: the
on-demand branch's add-pointer proposal (`:277-279`) is guarded by `!EntryNamesRuleWithoutImport`
today — a guard that was correct before this case (it means "only propose the add when the entry
doesn't already name this rule some other way") but, left as the *only* gate on the add, makes
round 3's refutation finding exactly: the merge's hurting case (`spec.md:230-232`) has the entry
naming each of `changelog.md`/`dev-journal.md` on its own old pointer line, so
`EntryNamesRuleWithoutImport` is already true and the add never fires — only the paired remove
R-010 adds would. R-010 therefore widens the add's gate to `!EntryNamesRuleWithoutImport ||
<the grammar-matched old line differs from the template's current line for the rule>` — the add
fires whenever either the rule is wholly new to the entry (the pre-case condition) or R-010's own
remove fires for it (the replace case) — then R-014 dedupes it. `TemplateTiers` already maps two
rules sharing one merged bullet to the *same* line text (`TemplateTiers.cs:95-103`) — so a report
against an edition-28 repo whose `AGENTS.md` names both the merged bullet's rules (the ordinary
all-`@` fixture shape) proposes the identical add-line twice once the gate above is widened. R-010
also extends the branch to propose a *remove* of the rule's own old single-rule pointer line when
that line is in the pointer grammar (so it is unambiguously this rule's stale line, not the
owner's rewording of something else); R-014 dedupes the review list's add-line proposals (e.g. a
`HashSet<string>` of already-added text, or `review.Distinct(StringComparer.Ordinal)` applied once
before the list is returned — an implementation call) so the merged bullet's add is proposed once
no matter how many rules name it.
Alternatives considered for R-005: (a) a second `TierModel` map of *every prior edition's*
on-demand lines, so the report can match the stale text verbatim and quote it back — rejected, it
is exactly the engine-side closed list R-015/R-016 (BL-484) forbid, and it breaks the moment an
edition changes a pointer's wording twice; (b) leaving it unfixed and calling it acceptable per
R-025 — rejected because the case brief (not the operator's ruling, which says only "returns to
the always tier… (`@` import)", `issue-75.md:4`) requires the stale pointer's removal too,
distinguishing this from R-025's "owner reworded it" case (Q1 in the spec draws the line
precisely; Q2 is closed and settles a different question, whether this proposal also fires inside
migration mode).

**D3 — the mode filter belongs on `VerifyJob`'s failure list, not on `Step4Targets.Snapshot`
itself, and the fix pattern already exists in `evals/grade.py` (BL-406).** `grade.py:81-110`'s
`scaffold_artifacts(mode)` reads each row's notes column and skips it when `SCAFFOLD_ONLY_RE`
(`:164-165`) matches and `mode != "scaffold"`. Round 3's refutation (#2) finds that porting this
filter *into* `Step4Targets.Snapshot` (round 2's design) leaks: `VerifyJob` passes `Snapshot`'s
result into the run record (`VerifyJob.cs:77-86`), and `ReportJob.cs:89,106-112` reads that same
persisted record back to build `scaffolded`/`Created` lines — a row excluded from `Snapshot`'s
result in migration or upgrade mode (e.g. the `AGENTS.md` row, whose own note "Only in
fresh-scaffold mode… legacy migration mode handles this file per Step 5 instead" also matches
`SCAFFOLD_ONLY_RE`) would silently drop out of the report too, hiding a migration-mode rename of
`AGENTS.md` from `Created`. So `Step4Targets.Of`/`Snapshot` gain **no** `mode` parameter and stay
exactly as they read today for every caller, `VerifyJob` included; instead `Step4Targets` gains a
query (e.g. `ScaffoldOnlyPaths(fs, skill, options)`) that identifies the one row this case narrows
to — `docs/journal/<today>.md` (`SKILL.md:116`) — by its **path**, not by porting
`SCAFFOLD_ONLY_RE`'s notes-column regex wholesale: that pattern (`fresh-scaffold mode only|only in
fresh-scaffold mode`, case insensitive, `grade.py:164-165`) matches on notes *text* alone, and the
`AGENTS.md` row's own note ("Only in fresh-scaffold mode… legacy migration mode handles this file
per Step 5 instead") matches the identical phrase — a verbatim port would exclude the `AGENTS.md`
row from `VerifyJob`'s failure list too, contradicting R-012 (the `AGENTS.md` row stays reported
missing in every mode; ruling 4 is about the journal row alone). `grade.py` can afford the wider
match because nothing downstream of `scaffold_artifacts(mode)` needs the `AGENTS.md` row reported
missing, so its notes-text pattern is not the right source for this filter. `ScaffoldOnlyPaths`
instead returns the singleton set containing the literal path `docs/journal/<today>.md` (the same
unresolved key `Snapshot`'s dictionary already uses for that row), and `VerifyJob` alone uses it,
after calling `Snapshot` unfiltered as today, to drop that one path from the "missing" failure
list it builds at `:69-71` when the mode differs from fresh-scaffold — the unfiltered `post`
dictionary is still what gets persisted to the record and read back by `ReportJob`. `VerifyJob`
has no run-mode read today — it never calls `Detection.Of` — so this case adds: read
`RunRecord.ModeKey` from the `--record` file when `RecordPath.Of(...)` exists (the same field
`ReportJob.cs:85` already reads back), falling back to `Detection.Of(fs, layout, ctx.Options,
ctx.Root, parsed.Skill.Version)` (the same call `ApplyJob.cs:81` already makes) only when there is
no record to read.
`<today>`-placeholder resolution (`grade.py:204-218`'s `_resolve_today`) is explicitly out of
scope (spec Boundary) — excluding the row from the failure list is sufficient to stop the false
"missing" report in upgrade mode; Step 6 runs verify in *every* mode including fresh scaffold
(`SKILL.md:157` — the refutation corrects an earlier, false claim here that verify is "not
normally run in fresh mode"), so a fresh-mode repo still carries the unresolved-placeholder gap
after this case, unchanged and still out of scope: the filter does not drop the row from the
failure list in fresh mode (mode equals the row's own restriction), so `VerifyJob` still checks
the literal, unresolved path for existence — the same pre-existing behavior as today, just no
longer misapplied to upgrade mode too.

**D4 — the "Law beats skills" fix is a wording-only change.** `skills.md:5` names
`docs/ai/rules/**` and `.claude/rules/**`; `project-rules.md:4` already describes the third shape
(a project rule reached only by its own pointer line). No new mechanism, no code: one sentence
gains a clause.

## Contracts

- **`AGENTS.md.tpl`** (and its delivered copy `docs/ai/rules/` wiring in every legislated repo):
  three `@` lines (`pair-development.md`, `decision-gate.md`, `verification.md`); the
  `### Read on demand` block drops the `verification.md` and `dev-journal.md` bullets and merges
  `dev-journal.md`'s trigger into the `changelog.md` bullet's wording, e.g.: "Before the first
  commit on a task branch, touching `CHANGELOG.md`/`docs/changes/`, or writing a fragment's
  `## journal` section or editing `docs/journal/` directly, read `docs/ai/rules/core/
  changelog.md` and `docs/ai/rules/core/dev-journal.md` — they are law, not a reference." (exact
  wording is an implementation task's call; the contract is: one bullet, both paths present as
  `.md`-terminated path runs so `FoldToOwned` folds both).
- **`opencode.json.tpl`**: `instructions` gains `docs/ai/rules/core/verification.md` as a third
  explicit path (alphabetical or trigger order, matching the other two's style).
- **`ReportJob.ReviewLines`**: the always-tier branch gains one guarded line, ungated from the
  add-`@import` line above it and restricted to `tiers.Always`:
  `EntryNamesRuleWithoutImport(entryText, rule) && line is in the pointer grammar naming only
  rule` → `review.Add($"- remove from \`{entryName}\`: the pointer line naming \`{rule}\` (now
  always-tier: imported, not read on trigger)")` (R-005; the grammar gate is the same test R-010
  uses, so owner prose merely mentioning the path is left alone). The on-demand branch's
  add-pointer line gains a paired remove-proposal when the entry's existing line for that rule is
  in the pointer grammar and differs from the template's current line for it (R-010), and the
  add's own gate widens from `!EntryNamesRuleWithoutImport` alone to `!EntryNamesRuleWithoutImport
  || <that same grammar-matched line differs from the template's current line>` so the add fires
  whenever the paired remove fires, not only when the rule is wholly new to the entry; both
  branches' add-line proposals are deduplicated before the review list is returned (R-014).
- **`Step4Targets`**: `Of`/`Snapshot` are unchanged — no `mode` parameter, every caller
  (`ApplyJob`, `ReportJob`, `VerifyJob`) keeps reading every row, unfiltered. `Step4Targets` gains
  a new query (e.g. `ScaffoldOnlyPaths(fs, skill, options)`) returning the singleton set containing
  `docs/journal/<today>.md`'s path alone — matched by path, not by porting `SCAFFOLD_ONLY_RE`'s
  notes-column regex (D3: that pattern also matches the `AGENTS.md` row's note). `VerifyJob.Run`
  gains the record-then-`Detection.Of` mode read (D3) and, after calling `Snapshot` unfiltered as
  today, excludes that one path from its "missing" failure list when the mode differs from
  `Detection.Fresh` — the unfiltered `post` dictionary is still what is persisted to the record.
- **`core/skills.md:5`**: the "Law beats skills" bullet's named surfaces grow a third clause
  covering a project rule reached only by its own pointer line (cross-reference
  `core/project-rules.md`), without weakening the existing two.

## Tasks

### Phase 0 — gate (runs first, before Phase 1)

- **T-01** Run `legislator sdd-lint` (or the explicit checklist if the binary is absent) and fix
  any coverage/shape finding against this case before implementation starts.
  - Done check: `legislator sdd-lint` shows no finding against BL-487 beyond the expected
    missing-fragment finding (R-019 not yet written).

### Phase 1 — production: engine `[P]` (file-disjoint within this phase; T-03 depends on T-02's
new `ScaffoldOnlyPaths` query and is not `[P]` against it)

- **T-02** `src/Legislator.Engine/Runs/Step4Targets.cs` — `Of`/`Snapshot` are untouched (no `mode`
  parameter, stay unfiltered for every caller); add a new `ScaffoldOnlyPaths(fs, skill, options)`
  query that returns the singleton set containing `docs/journal/<today>.md`'s path alone,
  identified by matching that one row's path, not by porting `SCAFFOLD_ONLY_RE`'s notes-column
  regex (`evals/grade.py:164-165`) — that pattern also matches the `AGENTS.md` row's note (D3),
  and R-012 requires the `AGENTS.md` row to stay in `VerifyJob`'s failure list in every mode.
  Confirm (no change expected) `evals/check_engine.py:863-870`'s Python twin `step4_targets()`: it
  is used only by `scaffold_all()` to pre-seed every possible target in a test fixture, so it
  stays unfiltered. *per R-012, R-025*
  - Done check: `dotnet build` succeeds; `Step4TargetsTests.cs` (T-13) passes.
- **T-03** `src/Legislator.Engine/Jobs/VerifyJob.cs` — read `RunRecord.ModeKey` from the
  `--record` file via `RunRecord.Read` when `RecordPath.Of(...)` exists, falling back to
  `Detection.Of` (same shape as `ApplyJob.cs:81`) only when there is no record; call
  `Step4Targets.Snapshot` unfiltered as today (its result still feeds `post`, persisted to the
  record unchanged), and when building the "missing" failure list at `:69-71`, exclude a target in
  `Step4Targets.ScaffoldOnlyPaths(...)` when the resolved mode differs from `Detection.Fresh`. *per
  R-012, R-013*
  - Done check: `VerifyJobTests.cs` (T-12) passes.

### Phase 2 — production: law text and templates `[P]` (file-disjoint from Phase 1 and each other)

- **T-04** `skill/assets/templates/AGENTS.md.tpl` AND `skill/assets/templates/opencode.json.tpl`,
  as one task (merged per round-3 refutation #3: `check_static.py:108` requires the opencode list
  equal the always tier, so neither template passes `check_static.py` alone — the pin coupling
  them is this assertion, not a wiring-count pin in `check_static.py`, which has none). In
  `AGENTS.md.tpl`: add the `verification.md` `@` line; drop its on-demand bullet; merge the
  dev-journal bullet into the changelog bullet and drop the standalone dev-journal bullet. In
  `opencode.json.tpl`: add `docs/ai/rules/core/verification.md` to `instructions`. *per R-001,
  R-002, R-003, R-006, R-007*
  - Done check: `python3 evals/check_static.py` passes on both files together (the always-tier /
    opencode-list equality at `:108`), but the `migration_wiring()` pin does not go green until
    T-16 lands its new count.
- **T-06** `skill/SKILL.md:147` (Step 5 migration-wiring prose, three always-tier imports naming
  `verification.md`, merged on-demand count, and reword "one pointer line per on-demand core rule"
  to "one per line in the template's `### Read on demand` block"), `skill/SKILL.md:157` (Step 6
  verify law text — "confirms every artifact from Step 4's table exists" extended to "…except a
  row restricted to another mode", per R-012), and `skill/SKILL.md:168` (Step 7 "Needs your
  review" law text — add R-005's removal proposal, R-010's replace proposal, and R-014's dedupe to
  the description, replacing the "never re-proposed" always-and-on-demand text the refutation
  flagged). *per R-004, R-008, R-009, R-023, R-024*
  - Done check: manual read of `SKILL.md:147,157,168` confirms `:147` names three always-tier
    imports and the reworded on-demand count, `:157` names the mode exception, and `:168` names
    the removal, the replace, and the dedupe (the `migration_wiring_derived_from_template`
    self-test does not go green until T-16's count change — that done check lives on T-16 alone).
- **T-07** `skill/references/migration.md:22,25,27` — same updates: the always-tier import list
  (now three), the "New sections to add" prose, the rewrite-order sentence, and the "one pointer
  line per on-demand core rule" reword (same as T-06's `SKILL.md:147`). *per R-004, R-008, R-023*
  - Done check: `python3 evals/check_static.py` passes.
- **T-08** `skill/assets/rules/core/skills.md:5` — extend the "Law beats skills" bullet with the
  third clause (Contracts). *per R-011*
  - Done check: a manual read against `core/project-rules.md:4` (`check_static.py` has no check
    for this bullet's wording today).

### Phase 3 — production: ReportJob, sequential (both edit `ReportJob.cs`, not `[P]` against each
other; `[P]` against Phase 1/2)

- **T-09** `src/Legislator.Engine/Jobs/ReportJob.cs` — always-tier branch (`:283-286`) gains the
  guarded stale-pointer removal line, gated on the same pointer grammar R-010 uses (not merely
  `EntryNamesRuleWithoutImport`, so owner prose naming the rule's path — e.g. this repository's
  own `AGENTS.md:26` naming `core/pair-development.md` — is never proposed for removal), ungated
  from the add-`@import` proposal, and restricted to `tiers.Always`. *per R-005*
  - Done check: `ReportJobTierTests.cs` (T-11a) passes.
- **T-10** `src/Legislator.Engine/Jobs/ReportJob.cs` — on-demand branch (`:262-280`) gains the
  template-vs-entry-line replace proposal (R-010: a remove of the entry's own old single-rule
  pointer line when it is in the pointer grammar and differs from the template's current line for
  the rule); the add-pointer proposal's own gate widens from `!EntryNamesRuleWithoutImport` alone
  to `!EntryNamesRuleWithoutImport || <that same grammar-matched old line differs from the
  template's current line for the rule>`, so the add fires whenever the paired remove fires too,
  not only when the rule is wholly new to the entry; and the review list's add-line proposals are
  deduplicated (R-014). *per R-010, R-014*
  - Done check: `ReportJobTierTests.cs` (T-11b) passes.

### Phase 4 — tests and evals (different worker; file-disjoint from Phase 1/2/3) `[P]` (T-15 and
T-16 are not `[P]` against each other — both touch `evals/check_static.py`; T-11 and T-14 are not
`[P]` against each other — both touch `ReportJobTierTests.cs`; T-16 and T-18 are not `[P]` against
each other — both touch `evals/grade.py`; T-17 and T-18 are not `[P]` against each other — both
touch `evals/setup_workspace.py`)

- **T-11** `tests/Legislator.Engine.Tests/Jobs/ReportJobTierTests.cs` — new fixtures: (a) a repo
  at the edition-28 shape (`verification.md` pointer bullet, no `@import`) proposes both the
  add-`@import` and the remove-pointer lines on report, in either order; a repo already at the
  edition-29 shape proposes neither; a repo whose entry names the rule's path only in owner prose
  outside the pointer grammar (e.g. `AGENTS.md:26`'s `core/pair-development.md` mention, shape
  from this repository) proposes no removal (R-005, negative case). (b) the same
  edition-28-shaped repo — standalone `changelog.md` bullet and standalone `dev-journal.md`
  bullet, both differing from the merged line — proposes two remove-old-line proposals (one per
  bullet) and a single add-merged-line (not two) (R-010, R-014); separately, a repo whose entry
  already imports both the merged bullet's rules with `@` lines (the shape
  `setup_workspace.py:111-113` builds) proposes the merged add exactly once, asserted directly.
  - Done check: `sh tools/kernel/dotnet-lock.sh bash evals/check_dotnet.sh` green on this file.
- **T-12** `tests/Legislator.Engine.Tests/Jobs/VerifyJobTests.cs` — a repo brought current by an
  `upgrade`-mode run (manifest/record mode `upgrade`) with no `docs/journal/<today>.md` on disk
  verifies clean; a `fresh`-mode repo missing it still reports it missing (regression guard for
  the row's actual duty, unchanged by this case per D3); a repo with no `--record` file falls back
  to `Detection.Of` and is filtered the same way a record-bearing run of that mode would be.
  *per R-012, R-013*
  - Done check: `sh tools/kernel/dotnet-lock.sh bash evals/check_dotnet.sh` green on this file.
- **T-13** new `tests/Legislator.Engine.Tests/Runs/Step4TargetsTests.cs` (none exists today) —
  unit-level coverage of `ScaffoldOnlyPaths` directly: it returns exactly
  `docs/journal/<today>.md` and no other — asserted against both rows the Step 4 table carries a
  "fresh-scaffold mode only" note for, so the test fails if the implementation matches by notes
  text and over-includes the `AGENTS.md` row; `Of`/`Snapshot` stay unfiltered regardless (a
  regression assertion that `Snapshot`'s returned dictionary is unchanged by this case). Parallel
  to `TemplateTiersTests.cs`'s shape. *per R-012, R-025*
  - Done check: `sh tools/kernel/dotnet-lock.sh bash evals/check_dotnet.sh` green on this file.
- **T-14** `tests/Legislator.Engine.Tests/Jobs/ReportJobTierTests.cs` (or a new
  `ReportJobLayoutTests.cs`) — the non-default `RulesDir`/`OkfDir` end-to-end case through
  `ReviewLines`, completing F-3/T-09 (BL-484's), not to be confused with this case's own T-09/T-10.
  *per R-016*
  - Done check: `sh tools/kernel/dotnet-lock.sh bash evals/check_dotnet.sh` green on this file.
- **T-15** `evals/check_static.py:116-119` — tighten the `core/project-rules.md` on-demand check
  to assert the bullet's own sentence (e.g. a substring match against the exact "reached only by
  a pointer line" clause), not the loose two-substring test. *per R-015*
  - Done check: `python3 evals/check_static.py` passes.
- **T-16** `evals/check_static.py:84-92`, `evals/grade.py` `core_rule_tiers()` (`:530-539`),
  `migration_wiring()` (`:518-527`) and the pin at `evals/grade.py:1868-1875` — no change expected
  in `check_static.py:84-92`/`core_rule_tiers()`/`migration_wiring()` (all three already derive
  from the template), but the `migration_wiring_derived_from_template` pin at `:1868-1875` SHALL
  change from `len(wiring) == 12` with its "2 … 9 …" comment to `len(wiring) == 11` with a
  "3 always-tier `@import` lines, 7 on-demand pointer lines (six core-rule bullets — one of them
  naming two rules, the merged changelog/dev-journal bullet, so 7 core rules named in all — plus
  the codebase-map bullet), and
  `## Boundaries`" comment. *per R-001, R-006, R-007, R-008*
  - Done check: the `selftest:derivation` run (`grade.py:2161-2165`) shows
    `migration_wiring_derived_from_template` green.
- **T-17** `evals/setup_workspace.py`, `evals/mutations.py`, `tests/Legislator.Parity.Tests/**`
  (`ReportTwins.cs`, `RunJobs.cs`, `DetectTwins.cs`) — audit every fixture that hard-codes the
  edition-28 `AGENTS.md.tpl` shape (pointer bullets, import counts) for drift against T-04;
  update in place where the shape assumption breaks, confirm-only where it already derives from
  the live template.
  - Done check: `sh tools/kernel/dotnet-lock.sh bash evals/check_dotnet.sh` passes AND
    `python3 evals/check_engine.py` passes (this task also touches `mutations.py` and
    `setup_workspace.py`, which `check_engine.py` covers).
- **T-18** `evals/setup_workspace.py`'s fixture builders (shape at `:686-708`), a new grader with
  asserts in `evals/grade.py`, the scenario's entry in `evals.json`, and `dashboard.py:35`'s
  `EXPECTED` — add an edition-28-shaped repository fixture (both tier-flip pointer bullets
  present, `verification.md`'s pointer bullet with no `@import`, and the standalone
  `dev-journal.md` bullet — `upgrade` imports everything with `@`, `setup_workspace.py:111`, so
  this fixture must be built directly, not assumed to already exist) upgrading to edition 29, so
  the benchmark exercises R-005's removal and R-010's replace, not only the unit tests in T-11.
  *per R-020*
  - Done check: `setup_workspace.py` builds the new edition-28-shaped fixture and the scenario is
    registered in `evals.json` and `dashboard.py:35`'s `EXPECTED`; the new grader's asserts going
    green is verified by T-25's benchmark run, which already names this fixture.

### Phase 5 — this repository's own D-3 and wiring gaps (hand-written, not `apply`)

- **T-19** Write `.claude/rules/verification.md` from `skill/assets/templates/
  verification-rules.md.tpl` (verbatim, no placeholders) and `docs/changes/README.md` from
  `skill/assets/templates/changes-README.md.tpl` (verbatim, no placeholders) into this repository.
  *per R-021*
  - Done check: run with the installed edition-28 binary, before any owned file changes this case
    makes: `ls .claude/rules/verification.md docs/changes/README.md` (both exist) AND
    `legislator verify`'s output no longer lists either file as missing.

### Phase 6 — gates

- **T-21** Build, then `sh tools/kernel/dotnet-lock.sh bash evals/check_dotnet.sh` and
  `python3 evals/check_static.py`, `python3 evals/check_engine.py`, `python3 evals/check_hooks.py`,
  `node evals/check_opencode_plugin.mjs` — all green before analyze.
  - Done check: all five commands exit 0.
- **T-22** Analyze gate: judge reuse-first (T-02/T-03 reuse `Detection.Of`,
  `EntryNamesRuleWithoutImport`, and `grade.py`'s existing mode-filter pattern rather than
  inventing new ones) and over-engineering (no general stale-pointer-removal mechanism beyond
  R-005's tier-flip case and R-010's template-line-changed case, per the Boundary).
  - Done check: analyze report written, no open finding against this case.
- **T-23** Release steps (orchestrator-run, not a worker task): bump `skill/VERSION` to `29`,
  `src/Legislator.Cli/Version.props`'s `<Version>` to `29.0.0`, `skill/assets/release/
  release.json`'s `"edition"` to `"29.0.0"` with `"digests": {}` — all three in one commit, as
  every prior edition bump does. *per R-017, R-018*
  - Done check: `python3 evals/check_static.py`'s version-pin check passes.
- **T-24** Write the change fragment `docs/changes/BL-487.md`. *per R-019*
  - Done check: `legislator sdd-lint`'s missing-fragment finding for BL-487 clears.
- **T-25** Run the full e2e benchmark, record it at `evals/benchmarks/v29.md`, on a commit before
  the delivery commit. *per R-020*
  - Done check: `evals/benchmarks/v29.md` exists and shows the recorded run's scenarios green,
    including T-18's edition-28 upgrade fixture.
- **T-26** Self-delivery: run `legislator apply` against this repository on the published
  edition-29 package (fleet member #0). *per the release process; does not touch R-021 — T-19
  already wrote `.claude/rules/verification.md` and `docs/changes/README.md` by hand, since apply
  does not scaffold Step 4 artifacts; does not touch R-022 either — T-20 runs after this.*
  - Done check: `legislator verify` against this repository is clean post-apply.
- **T-20** Bring this repository's own `AGENTS.md` to the edition-29 shape: add the
  `verification.md` `@import` line, drop its old on-demand bullet and the standalone
  `dev-journal.md` bullet, merge the changelog bullet. A production task, done after
  self-delivery (T-26), hence placed here rather than in Phase 5. *per R-022*
  - Done check: `legislator report` run against this repository proposes no tier lines.
- **T-27** Converge: judge the implementation against every requirement (R-001..R-025), the
  Clarifications (Q1..Q2), and the Contracts above; append the Converge section to this plan,
  append-only, per `core/sdd.md`. Loop implement → converge until clean.
