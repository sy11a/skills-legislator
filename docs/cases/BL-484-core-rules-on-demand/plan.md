# BL-484 — plan

Branch `bl/484-core-rules-on-demand`. Every task traces to a requirement in
`spec.md`.

## Research

- **Decision — the template is the tier source, not an engine-side list
  (D1/R-001).** `AGENTS.md.tpl` is hand-authored and copied byte-for-byte by
  `SKILL.md` Step 3; it already distinguishes `@import` lines from every other
  line by construction, so it already *is* the tier data — a second,
  engine-side `OwnedSet.OnDemandCoreRules` list would be a parallel source that
  `check_static.py` would then have to keep in sync with the template by
  content, which is exactly the duplication R-001 forbids. `ReportJob.ReviewLines`
  takes `parsed.Skill` and reads
  `{parsed.Skill.Root}/{options.SkillAgentsTemplate.Value}` directly at report
  time (the same package the run was pointed at with `--skill`, per
  `SkillPackage`'s own doc comment — a repo mid-upgrade may carry an older
  delivered engine, so reading the package's own template, not a compiled-in
  constant, keeps `report` honest about which edition it is proposing against).
  WHEN that file does not exist, `ReviewLines` throws a named error
  (`AgentsTemplateMissingException` or equivalent) instead of falling back to
  "treat everything as always-tier" — a silent fallback would hide exactly the
  drift this case exists to prevent. For each owned core-rule path (and
  `docs/okf/codebase-map.md`), the tier is decided by where that path appears
  in the template text, not by matching a fixed grammar: on an
  `^@docs/ai/rules/core/<name>\.md$` line → always-tier; on any other
  non-blank line → on-demand, and the add-pointer text proposed is that exact
  line, verbatim, whatever its shape (round-2 finding 1 — this is what makes
  `project-rules.md`'s existing `AGENTS.md.tpl:11` prose line classify as
  on-demand with no special-casing: it names the path and is not an `@import`
  line, so it *is* the pointer). A path appearing nowhere in the template
  (there should be none among owned core rules once T-03 lands) falls back to
  today's add-`@import` behavior, which also covers stack rules automatically
  since they never appear in the core template at all.
  - Rejected (superseded): the previous research round's `OwnedSet.cs`
    `HashSet<string> OnDemandCoreRules` plus a Python-side mirror — review
    finding 8 holds that this is a *third* copy of the trigger table (template
    prose, engine set, Python mirror) when the template's own lines already
    carry everything `ReviewLines` needs to propose, verbatim.
  - Also rejected (round 2 finding 1): a fixed-grammar regex
    (`` ^- Before .+, read `(docs/\S+)` — it is law, not a reference\.$ ``) to
    recognize pointer lines, and hard-coded C# text for `project-rules.md`'s
    and `docs/okf/codebase-map.md`'s pointers. Both would be a second,
    engine-side copy of prose the template already owns, and the grammar
    regex would miss `project-rules.md`'s existing non-conforming line
    entirely, misrouting it to the add-`@import` fallback.
  - Alternatives still rejected from the prior round, for the same reasons:
    YAML front matter on each rule file (rule files are delivered byte-for-byte
    as visible prose; front matter would become loaded, human-visible content);
    a JSON asset under `skill/assets/release/` (invents a new cross-language
    data file for one bit of information per rule that the template already
    encodes).
- **Decision — `AGENTS.md.tpl` and `opencode.json.tpl` stay hand-authored, not
  generated from anything at apply time.** `SKILL.md` Step 3 copies these
  templates byte-for-byte (no placeholder substitution for the import/pointer
  block — placeholders are documented as project-instance values, not the
  import list). `evals/check_static.py` is the cross-check that the template
  text and the set of delivered rule files cannot silently drift apart: for
  every file under `skill/assets/rules/core/`, its path must appear in the
  template in exactly one of the two forms and never both.
- **Decision — no `AuditChecks.cs` behavior change.** Confirmed by reading
  check 1 (`imports-resolve`, `AuditChecks.cs:117-133`) and check 7
  (`orphan-docs`, `AuditChecks.cs:234-252`, `636-643`, `704-723`): neither
  assumes an owned rule is `@`-imported, and `docs/ai/rules/**` is already
  exempt from check 7's scan. Because that exemption is directory-wide, no core
  rule file can ever reach `OrphanCandidates()` — a confirmation test must use
  `docs/okf/codebase-map.md` (walked by `OrphanCandidates`, not exempt) as its
  subject, not a core rule file, or it would pass vacuously (finding 20). T-10
  pins this.
- **Decision — withhold an always-tier rule in the "proposes a core import"
  fixture, not an on-demand one.** `grade.py:1141-1146`
  (`report_proposes_core_import_line`) withholds `core_src[-1]`
  (`setup_workspace.py:70`) and asserts an `@import` proposal appears for it.
  Under the new tier split that withheld rule must be always-tier
  (`pair-development.md` or `decision-gate.md`), or the assertion is checking a
  proposal shape (`@import`) the engine no longer produces for it. Changing
  which rule is withheld keeps the existing assertion text meaningful without
  rewriting it to check for a pointer-line proposal instead — the fixture's
  intent ("the review proposes adding a missing owned import") still holds for
  an always-tier rule.
  `setup_workspace.py:108` (the all-`@`-imports `AGENTS.md`, "legislated one
  edition ago") is not this fixture and keeps its current all-`@` shape
  unchanged (round 2 finding 5): it is the hurting case's own input, so the
  review's remove-`@`-plus-add-pointer proposal is what exercises it, not an
  already-updated shape — `T-12` adds the grade check that pins that proposal
  instead of changing `:108`.
- **Alternative rejected — a new "tier-mismatch" audit check** flagging an
  on-demand rule still `@`-imported post-upgrade. Rejected as unrequested
  scope: the upgrade proposal is propose-only by design (authority: entry
  document × upgrade), and an owner who declines it is exercising that
  authority, not creating a defect for the audit to find.

## Contracts

**Tier source** (read by `ReportJob.ReviewLines`, not stored anywhere else):

```csharp
var tplPath = $"{parsed.Skill.Root}/{options.SkillAgentsTemplate.Value}";
if (!fs.File.Exists(tplPath))
    throw new AgentsTemplateMissingException(tplPath); // named error, no fallback
var tpl = fs.File.ReadAllText(tplPath);
var lines = tpl.Split('\n');
// Template paths are written against the default layout; re-root them under
// this repo's actual layout before matching, so a non-default RulesDir/OkfDir
// still matches (round 2 finding 3, round 3 finding 2):
string Rerooted(string defaultPath) => defaultPath switch
{
    _ when defaultPath.StartsWith("docs/ai/rules/core/", StringComparison.Ordinal) =>
        $"{layout.Relative(layout.RulesCoreDir)}/{defaultPath["docs/ai/rules/core/".Length..]}",
    "docs/okf/codebase-map.md" => $"{layout.Relative(layout.Okf)}/{layout.CodebaseMapFile}",
    _ => defaultPath,
};
var alwaysTier = AlwaysImport().Matches(tpl).Cast<Match>().Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
// AlwaysImport(): ^@docs/ai/rules/core/(\S+)$  (multiline) — matches the
// template's own (default-layout) text; results are default-layout names,
// then re-rooted via Rerooted() before comparing against ownedRules paths.

// For every owned core-rule path (and docs/okf/codebase-map.md) not in alwaysTier:
// on-demand iff the template's default-layout path (the reverse of Rerooted,
// i.e. the owned path with this repo's layout folded back to the default one)
// appears on some other, non-"@import", line of the template; the pointer
// text proposed is that line, verbatim, with the default-layout path replaced
// by the repo's actual Rerooted() path — no fixed grammar, no C#-side text
// beyond this substitution.
string? OnDemandLine(string path) => lines.FirstOrDefault(l =>
    l.Contains(path, StringComparison.Ordinal) && !l.TrimStart().StartsWith('@'));
```

`project-rules.md`'s pointer is the existing `AGENTS.md.tpl:11` line — it
matches `OnDemandLine` like any other on-demand rule (it names the path and is
not an `@import` line), so no special-casing is needed for it or for
`docs/okf/codebase-map.md`: both are "a path present outside `@import` lines",
same as every pointer-table rule.

**`ReviewLines` propose shape** (R-007, R-008), replacing
`ReportJob.cs:231-245`:

- For each owned rule path in `ownedRules` (core and stack):
  - **always-tier** (name in `alwaysTier`, or not a core-rule path at all —
    covers stack rules): unchanged today's logic — propose `add @<rule>` if
    missing from `imports`.
  - **on-demand** (`OnDemandLine(path)` is non-null, or the rule is
    `docs/okf/codebase-map.md`): propose `remove @<rule> (on-demand: read on
    trigger, not imported)` if present in `imports`; propose `add` the matched
    template line verbatim (`OnDemandLine(path)`, the same line for every
    on-demand rule including `project-rules.md` and `docs/okf/codebase-map.md`
    — no rule-specific text in C#) — **unless** the entry text, with every
    `@import` line for that rule stripped out first, already contains the
    rule's path (this is deliberate, R-007: an owner's own pointer wording is
    never second-guessed against the template's). Never propose `add @<rule>`
    for anything in this branch.
- `docs/okf/codebase-map.md` always takes the on-demand branch, never gated on
  `scaffolded.Contains(map)` — replacing `ReportJob.cs:240-245` entirely, not
  just its on-demand half.

**Pointer-line grammar** (R-003), triggers per the spec's per-file table:

```
- Before <trigger>, read `<path>` — it is law, not a reference.
```

One bullet per on-demand entry except `project-rules.md`, under a `### Read on
demand` sub-heading placed directly after the two `@import` lines in
`AGENTS.md.tpl`.

**Engine default** (R-006): `LegislatorOptions.cs:76`
`LegislationMarker` default becomes `new("core/pair-development.md",
OptionsLayer.Defaults)`.

**New option** (R-001, round 2 finding 3): `SkillAgentsTemplate` added beside
`SkillOpencodeTemplate` (`LegislatorOptions.cs:136`), default
`new("assets/templates/AGENTS.md.tpl", OptionsLayer.Defaults)`; wired through
`OptionsComposer.cs` (`:121`) and the key-name maps (`LegislatorOptions.cs:252,341`)
the same way `SkillOpencodeTemplate` is.

## Tasks

### Phase 1 — production: engine

**T-01 — template-reading tier helper.** *(per R-001, round 2 finding 3,
round 3 finding 2)*
Add `SkillAgentsTemplate` to `LegislatorOptions.cs` (beside
`SkillOpencodeTemplate`, wired through `OptionsComposer.cs` and the key-name
maps). Add a helper (e.g. `ReportJob.TierFromTemplate` or a sibling static)
that takes `parsed.Skill`, `layout` and `options`, reads
`{parsed.Skill.Root}/{options.SkillAgentsTemplate.Value}`, and returns the
always-tier name set plus an `OnDemandLine(path)` lookup over the template's
non-`@import` lines, per the contract above. The template's paths are written
against the *default* layout; the helper strips that default prefix from each
template line's path and re-roots it under this repo's actual
`layout.Relative(layout.RulesCoreDir)` / `layout.Relative(layout.Okf)` +
`layout.CodebaseMapFile` (and does the reverse — folding an owned path back to
its default-layout form — before looking it up in the template), so a repo
with a non-default `RulesDir` or `OkfDir` still matches instead of silently
falling back to `add @…`. WHEN the template file is missing, THEN the helper
SHALL throw a named exception (`AgentsTemplateMissingException` or
equivalent) — no fallback to always-tier. No `OwnedSet` change, no new
engine-side closed list, no fixed-grammar pointer regex.

**T-02 — `ReviewLines` tier-aware proposals.** *(per R-007, R-008)*
Rewrite `ReportJob.cs:225-258` (`ReviewLines`) to take `parsed.Skill` and
`layout`, call T-01's helper, and branch per the contract above, for both the
rules loop and the codebase-map block (replacing `:240-245` outright,
including its remove half); update the `ReviewLines` call at
`ReportJob.cs:155` to pass `parsed.Skill` and `layout` through.

**T-02a — `LegislationMarker` default.** *(per R-006)*
Change `LegislatorOptions.cs:76`'s default from `"core/okf.md"` to
`"core/pair-development.md"`.

### Phase 2 — production: law text and templates `[P]` (file-disjoint from Phase 1 and each other)

**T-03 — `AGENTS.md.tpl`.** *(per R-002, R-003)* `[P]`
Replace the eleven-line `@import` block (`AGENTS.md.tpl:16-26`) with two
`@import` lines (`pair-development.md`, `decision-gate.md`) followed by a
`### Read on demand` block of nine pointer lines (eight on-demand core rules —
excluding `project-rules.md`, which keeps its existing `:11` line unchanged —
plus `docs/okf/codebase-map.md`), per the trigger table and grammar above.
Remove the now-redundant standalone `@docs/okf/codebase-map.md` line.

**T-04 — `opencode.json.tpl`.** *(per R-004)* `[P]`
Replace `"docs/ai/rules/core/*.md"` (`opencode.json.tpl:4`) with the two
explicit always-tier paths; remove `"docs/okf/codebase-map.md"`
(`opencode.json.tpl:6`) from `instructions`.

**T-05 — `core/project-rules.md`.** *(per R-009)* `[P]`
Add a bullet: a project rule may live outside `.claude/rules/`, reached only by
a pointer line in the entry document — neither Claude Code's directory
autoload nor opencode's `.claude/rules/*.md` glob loads it, so the pointer is
the only wiring such a rule has.

**T-06 — `SKILL.md` Step 1 edge case.** *(per R-006)*
Change the import marker Step 1 checks (`SKILL.md:26`) from
`@docs/ai/rules/core/okf.md` to `@docs/ai/rules/core/pair-development.md`
(text only — T-02a is the behavior change). Update
`DetectJobTests.cs:181`, `DetectTwins.cs:145,166` and `check_engine.py:934` to
the new marker.

**T-07 — `SKILL.md` Step 5 + `references/migration.md`.** *(per R-005)*
Change the wiring Step 5 writes directly into a canonicalized `AGENTS.md`
(`SKILL.md:147`, `references/migration.md` §1 including its line-25 "New
sections to add" bullet and its line-27 rewrite-order prose) from the full
eleven-import block to the two-import-plus-pointer shape (T-03's content).

**T-08 — `SKILL.md` Step 7 description.** *(per R-007, R-008)*
Update the "Needs your review" prose (`SKILL.md:168`) to describe the
tier-aware proposal (remove-and-add for on-demand, add-only for always),
matching T-02.

### Phase 3 — tests and evals (different worker; file-disjoint from Phase 1/2) `[P]`

**T-09 — engine unit tests.** *(per R-001, R-007, R-008)* `[P]`
`tests/Legislator.Engine.Tests`: T-01's helper parses `AGENTS.md.tpl`'s real
content into the expected always/on-demand split, including `project-rules.md`
classifying on-demand from its existing `:11` prose line with no
rule-specific C# text; a `ReportJobTests` (or sibling) case per branch —
always-tier missing import still proposes add; on-demand rule present as
`@import` proposes remove-and-add-pointer; on-demand rule present as neither
proposes add-pointer only; on-demand rule already pointer-wired proposes
nothing; an on-demand rule present under a *reworded* pointer (text differing
from the template's) proposes nothing, pinning that the owner's wording is
never overwritten (R-007, round 2 finding 4); `docs/okf/codebase-map.md`
present as `@import` proposes remove-and-add unconditionally (not gated on
scaffold state); a stack rule missing its import still proposes `add @<rule>`
(R-008's path-scoping); a skill package with no `AGENTS.md.tpl` makes
`ReviewLines`/T-01's helper throw the named error, not fall back silently
(round 2 finding 2); a repo configured with a non-default `RulesDir` and/or
`OkfDir` still classifies and proposes correctly (round 2 finding 3, round 3
finding 2) — not the silent `add @…` fallback.

**T-10 — audit confirmation test.** *(research decision — no audit code
change, finding 20)*
A test (new or extended in `tests/Legislator.Engine.Tests/Audit/`) that builds
a repo whose `AGENTS.md` carries a pointer line (no `@import`) for
`docs/okf/codebase-map.md` and asserts check 7 does not flag it as orphaned —
not a core rule file, since `ExemptHomes` already excludes
`docs/ai/rules/**` wholesale and a core-rule-file version of this test would
pass vacuously. A second case pins check 1 raising nothing for a pointer line
(not an `@import`) naming an on-demand rule.

**T-11 — `evals/check_static.py` rewrite.** *(per R-010, R-004, R-009)* `[P]`
Replace the loop at `check_static.py:79-84`: for each file in
`skill/assets/rules/core/`, assert its path appears in `AGENTS.md.tpl` in
exactly one of the two forms (`@`-import line, or inside a pointer/bullet line
naming it) and never both. Add the always-tier-only check to the
`opencode.json.tpl` `instructions` assertion (`check_static.py:86-94`) — this
is R-004's only test (round 2 finding 8: nothing else pins it). Add a check
that `core/project-rules.md` carries the on-demand-project-rule bullet
(R-009's only test, same finding).

**T-12 — fixture updates across the Python and C# suites.** *(per R-002,
R-004, R-005, R-010)* `[P]`
- `check_engine.py`: `CORE_RULES`-built `AGENTS.md` fixtures at `:1060`,
  `:1099` (`root2`) move to the two-import-plus-pointer shape; `:1075` imports
  `okf.md`, not `sdd.md` (round 2 finding 6) — its review assertion (`:1091`)
  checks the tier-aware remove-`@okf.md`-plus-add-okf-pointer text from T-02's
  contract, and separately asserts the add-only `sdd.md` pointer (present as
  neither import nor pointer in this fixture) instead of a bare `@import`
  string for either; `:1098-1099` (the Health-check/`ghost.md` assertion
  against `root2`) updated to the new fixture shape, no assertion-text change
  needed since it is a Health check, not a review-proposal check.
- `grade.py`: `:518-524` (`migration_wiring`) derives pointer-block lines too,
  not only `@` lines, and `:1025`/`:1082` (the `v2_wired` counts built from
  `len(migration_wiring())`) are pinned by an expected wiring count asserted
  in the grader self-check at `:1831-1834`, next to the codebase-map pointer
  assertion, instead of only trusting the derivation (round 2 finding 8);
  `:1833` asserts the codebase-map *pointer* string is among the derived
  wiring instead of the `@import` string; `:982` (`agents_md_imports_rules`)
  needs no change (still
  true — the two always-tier imports remain `@docs/ai/rules/core/...`).
  `:1141-1146` (`report_proposes_core_import_line`) withholds an always-tier
  rule instead of `core_src[-1]`/`verification.md` (research decision above);
  `setup_workspace.py:70` passes that choice through. `setup_workspace.py:108`
  (writes `@` for every owned rule, the hurting case's own input) is
  unchanged (round 2 finding 5) — instead, add a grade check asserting the
  review proposes remove-`@` plus the on-demand pointer for a rule built from
  that fixture (not re-add).
- `mutations.py:256-262` (`drop_one_core_import`/`drop_all_core_imports`): no
  fixture change needed — both still work unchanged against the two-import
  shape, since they target any `@docs/ai/rules/core/...` line and the fixture
  still carries two. `mutations.py:559` is unrelated (the `imports-resolve`
  slug-coverage mutation, not an import-block fixture) — drop it from this
  task, round 2 finding 7. `mutations.py:699` (`ghost_import_fixed`, appends a
  fake `@import` line) needs no change either.
- C# parity twins: add a minimal `AGENTS.md.tpl` to the fake skill packages
  (`tests/Legislator.Parity.Tests/Engine/RunJobs.cs:63-83`'s `AddSkill`, and
  the Engine-test `ApplyFixture`, `tests/Legislator.Engine.Tests/Apply/ApplyFixture.cs:45-49`)
  carrying pointer lines (not `@import` lines) for `okf.md`, `sdd.md` and
  `docs/okf/codebase-map.md`, so both rules read as on-demand and `ReportJob`'s
  template read does not throw in every report test and twin (round 2
  finding 2); add an `@import` line only for a third, always-tier rule if a
  twin needs one present as always-tier. `tests/Legislator.Parity.Tests/Engine/RunJobs.cs:48`
  (`CoreRules`) and `:242-245` (`WiredEntry`) rebuilt from that same template
  so an entry document built from it has nothing left to review;
  `ReportTwins.cs:50`'s input `AGENTS.md` imports `okf.md`, not `sdd.md`
  (round 2 finding 6, unchanged by this fix — it is the reviewed document,
  not the template) — `ReportTwins.cs:184-187`
  (`Report_review_carries_import_deltas_and_scaffold_wiring`) asserts the
  tier-aware remove-`@okf.md`-plus-add-okf-pointer text and the add-only
  `sdd.md`-pointer text, and for `docs/okf/codebase-map.md`, instead of bare
  `@import` strings.

### Phase 4 — gates

**T-13 — analyze.** Coverage R↔task, dangling `per R-NNN` references,
unresolved placeholders, `legislator sdd-lint` if available.

**T-14 — release.** *(per R-011)*
`skill/VERSION`, `src/Legislator.Cli/Version.props`'s major, and
`skill/assets/release/release.json`'s `edition` bump together in one commit; a
change fragment under `docs/changes/BL-484.md`; `bash evals/check_dotnet.sh`
and the Python suites green; the e2e benchmark run and recorded at
`evals/benchmarks/v<N>.md` against the prior edition's baseline. `release.json`'s
per-rule digests still wait for tag time (`tools/publish-legislator.sh`),
unchanged from today. The orchestrator runs the benchmark and the tag/publish
steps (they need the released binary and the fleet-wide measurement harness);
a worker can do the version/edition bump, the change fragment, and the green
local suites.

**T-15 — deliver to self.** *(per README.md "Update the constitution" step 4)*
Run `/legislator` on this repository so its own `AGENTS.md` and
`opencode.json` move to the new shape, byte-verify, commit — fleet member #0
before any other repository sees the edition.

**T-16 — converge.** Judge the implementation against every `spec.md` promise
and every plan decision; append findings here (append-only); close only on
"✅ Converged".

**T-17 — Architector probe re-run.** *(per R-012; orchestrator-run, last)*
Re-run the Architector first-call token probe (main session and a
general-purpose subagent, same method as `issue-73.md`) against the released
edition; record the result showing the predicted drop (~11,000 tokens from the
core alone). This closes the issue's Done-when and needs the tagged/published
binary from T-14, so it runs after T-14–T-16.
