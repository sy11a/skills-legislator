# BL-484 — core rules on demand

**Tier: 2 (full).** Blast radius: the law delivered to every legislated repository
(`skill/assets/rules/core/*`, `skill/assets/templates/AGENTS.md.tpl`,
`skill/assets/templates/opencode.json.tpl`) and the engine that reads and proposes
against it (`src/Legislator.Engine/Jobs/ReportJob.cs`, `skill/SKILL.md`'s Step 1
edge case and Step 5 migration). Novelty: the entry document gains a second,
non-`@import` way of binding a rule into a session, and every surface that
currently assumes "owned rule ⇒ `@`-imported" must stop assuming it for nine of
the eleven core rules. Branch `bl/484-core-rules-on-demand`; closes
`sy11a/skills-legislator#73`.

**Spec type: feature.**

## Boundary

**In scope** — the core rule tier split only: `AGENTS.md.tpl`'s import block and
new pointer block, `opencode.json.tpl`'s `instructions` array, the upgrade
report's "Needs your review" proposals (`ReportJob.cs`), `skill/SKILL.md` Step 1's
manifest-less edge case (including the engine default it actually depends on),
Step 5's migration wiring, `references/migration.md`, `core/project-rules.md`'s
note that a project rule may itself be on-demand, the evals that assert on the
import block (`check_static.py`, `check_engine.py`, `grade.py`, `mutations.py`,
`setup_workspace.py`) and their C# parity twins (`ReportTwins.cs`, `RunJobs.cs`,
`DetectTwins.cs`), `skill/VERSION` / `Version.props` / `release.json` / the
change fragment / the e2e benchmark / an Architector probe re-run, and delivery
to this repository (fleet member #0).

**Out of scope** — stack rule files (`docs/ai/rules/stacks/**`) and
`.claude/rules/*.md` stay always-loaded; no tier split for them here (follow-up,
D5/Q4). `docs/okf/glossary.md` stays always-loaded (not in plan 0010's on-demand
list). Any change to `AuditChecks.cs` beyond confirming existing checks already
hold — research below finds checks 1 and 7 need no code change, only
confirmation. Stage skills in other repositories (Architector's own stage
prompts naming a trigger file) are out of scope here, tracked as a follow-up in
plan 0010 itself ("stage skills that run a trigger … name the file"). A new
"tier-mismatch" audit check flagging a stale `@import` post-upgrade is out of
scope (the upgrade proposal is propose-only by design).

## Current behavior

- `AGENTS.md.tpl:16-26` hand-lists eleven `@docs/ai/rules/core/*.md` imports,
  unconditionally, for every fresh scaffold, plus a standalone
  `@docs/okf/codebase-map.md` import at `:28`.
- `AGENTS.md.tpl:11` already carries a pointer-shaped line for
  `project-rules.md`: "Project-specific rules: `.claude/rules/` — … read
  `docs/ai/rules/core/project-rules.md` before adding one" — the only core rule
  already reached this way.
- `opencode.json.tpl:4` globs `docs/ai/rules/core/*.md` into `instructions`, and
  `:6` lists `docs/okf/codebase-map.md` explicitly, so opencode autoloads all of
  them regardless of what the entry document imports.
- `skill/SKILL.md:26` (Step 1's manifest-less edge case) detects "this repo was
  already legislated" by checking whether the entry document contains the
  `@docs/ai/rules/core/okf.md` import line specifically. The check this prose
  describes is implemented by `LegislatorOptions.LegislationMarker`
  (`LegislatorOptions.cs:76`, default `"core/okf.md"`), read into
  `RepoLayout.LegislationImport` (`RepoLayout.cs:37`) and consumed by
  `Detection.cs:43-63` to set `reconstructed` (`DetectJob.cs:64`, `SKILL.md:28`)
  — the engine default, not just the doc prose, decides this.
- `skill/SKILL.md:147` (Step 5) writes the full eleven-import block plus the
  codebase-map import and the `## Boundaries` section/glossary pointer directly
  into the canonicalized `AGENTS.md`, never proposing it.
  `references/migration.md:25-27` describes the same shape — "ADR, dev journal,
  and changelog disciplines are covered by their own owned rule files, imported
  via the same `@docs/ai/rules/core/...` block" and "the core `@import` lines
  (one per file under `assets/rules/core/`)".
- `skill/SKILL.md:168` (Step 7's emitter description) proposes, in upgrade mode,
  one `@import` line to add "for every owned rule file the entry document does
  not import" — no notion of a rule that should never be `@`-imported.
- `ReportJob.cs:225-236` (`ReviewLines`) computes `ownedRules` from the
  manifest's `ownedFiles` filtered to the rules directory (both core and stack
  rules), and proposes `add @<rule>` for every one not already imported, `remove
  @<rule>` for every import no longer owned — both sides blind to tier, so once
  on-demand rules exist this logic proposes re-adding the very imports the case
  wants removed. `ReportJob.cs:240-245` proposes `add @<map>` for
  `docs/okf/codebase-map.md` only `scaffolded.Contains(map)`, and the remove
  branch at `:234-236` never matches it (it only matches the `rules/` prefix),
  so nothing proposes removing an already-present `@docs/okf/codebase-map.md`.
- `AuditChecks.cs:117-133` (check 1, `imports-resolve`) only verifies that every
  `@<path>` line resolves to a real file — it does not care what *should* be
  imported, so it needs no change for the tier split: a pointer line is not an
  `@`-prefixed line and is invisible to this check.
- `AuditChecks.cs:234-252` (check 7, `orphan-docs`) exempts `docs/ai/rules/**` by
  directory convention (`AuditChecks.cs:636-643`, `ExemptHomes`), and its
  `Referenced` helper (`AuditChecks.cs:704-723`) matches any document whose text
  contains the candidate's repo-relative path as a plain substring — a pointer
  line naming `docs/okf/codebase-map.md` already satisfies it exactly as an
  `@import` line does today. Because `ExemptHomes` already excludes
  `docs/ai/rules/**` wholesale, no core rule file can ever reach
  `OrphanCandidates()` (`AuditChecks.cs:645-662`, which only walks `docs/okf/*.md`
  and top-level `docs/*.md`) — a test asserting "check 7 does not flag a core
  rule" would be vacuous; `docs/okf/codebase-map.md` (not exempt, reached only by
  a pointer under the new design) is the real subject for this confirmation.
  Neither check needs a code change; both need the tier split verified against
  them (see Clarifications).
- `evals/check_static.py:79-84` asserts `AGENTS.md.tpl` contains
  `@docs/ai/rules/core/{name}` for **every** file under
  `skill/assets/rules/core/` — this assertion is false for the nine on-demand
  rules under the new design and must be rewritten.
- `evals/check_engine.py:907` (`CORE_RULES`, globs `assets/rules/core/*.md`) and
  its `AGENTS.md`-building fixtures (`:1060`, `:1075`, `:1099`/`root2`) assume the
  eleven-import shape. `:1091` asserts the review proposes `sdd.md` add and
  `ghost.md` remove; `:1098-1099` is the Health-check assertion
  (`[imports-resolve]`/`ghost.md`) against the `root2` fixture, not a review
  proposal assertion. `evals/grade.py:982` (`agents_md_imports_rules`) and
  `evals/mutations.py:258-262,559,699` carry the same assumption.
- `evals/grade.py:518-524` (`migration_wiring`) derives the strings migration
  must write directly into `AGENTS.md` from every `@`-line `AGENTS.md.tpl`
  carries; `:1833` asserts `"@docs/okf/codebase-map.md" in wiring`. Once
  `AGENTS.md.tpl` carries that path only in a pointer line, both the derivation
  and the assertion go stale.
- `evals/grade.py:1141-1146` (`report_proposes_core_import_line`) withholds
  `core_src[-1]` (`setup_workspace.py:70`), which is `verification.md` — an
  on-demand rule under the new design — and asserts an `@import` proposal for
  it; `setup_workspace.py:108` writes an `@` line for every owned rule.
- `tests/Legislator.Parity.Tests/Engine/RunJobs.cs:48` (`CoreRules =
  ["okf.md", "sdd.md"]`) and `ReportTwins.cs:184-187,242-245` (`WiredEntry`,
  asserting `@docs/ai/rules/core/sdd.md` and `@docs/okf/codebase-map.md` in the
  review) are the C# twins of `check_engine.py:907/1091/1099` and will fail or
  stop matching once the Python side changes.
- `core/project-rules.md:1-9` says nothing about a project rule living outside
  `.claude/rules/`; every project rule today is assumed auto-loaded.

## Requirements

- **R-001** — The always/on-demand split for each core rule SHALL be declared
  as data in exactly one place: `skill/assets/templates/AGENTS.md.tpl` itself.
  A core rule's path on an `@docs/ai/rules/core/<name>.md` line declares that
  rule always-tier; a core rule's path appearing anywhere else in the template
  on a line that is not an `@import` line (a pointer line under `### Read on
  demand`, R-003, or an existing prose line such as `AGENTS.md.tpl:11`)
  declares it on-demand — the same rule applies to `docs/okf/codebase-map.md`.

- **R-013** — `ReportJob.ReviewLines` SHALL read the always/on-demand split
  (R-001) directly from the template shipped at the path named by a new
  `SkillAgentsTemplate` layout option (default
  `assets/templates/AGENTS.md.tpl`, beside `SkillOpencodeTemplate`), resolved
  under `parsed.Skill.Root`.

- **R-014** — `ReportJob.ReviewLines` SHALL propose the template's own
  matching line (per R-013) verbatim as the add half of an on-demand
  proposal.

- **R-015** — The engine SHALL NOT hard-code any rule-specific pointer text
  in C#.

- **R-016** — There SHALL be no engine-side closed list (no
  `OwnedSet.OnDemandCoreRules`) and no Python-side mirror set —
  `check_static.py` is the cross-check that the template agrees with the
  delivered rule files, not a second source of the split.

- **R-017** — WHEN the template file at the path named by R-013 is missing,
  THEN `ReportJob` SHALL fail the report loudly with a named error, never
  silently falling back to treating every rule as always-tier.

- **R-002** — WHEN the legislator scaffolds a fresh repository, THEN
  `AGENTS.md.tpl` SHALL carry exactly two `@docs/ai/rules/core/*.md` imports
  (`pair-development.md`, `decision-gate.md`).

- **R-018** — WHEN the legislator scaffolds a fresh repository, THEN
  `AGENTS.md.tpl` SHALL carry one pointer line per on-demand core rule
  (`okf.md`, `sdd.md`, `verification.md`, `changelog.md`,
  `artifact-lifecycle.md`, `skills.md`, `dev-journal.md`, `adr.md`) plus
  `docs/okf/codebase-map.md`, under a `### Read on demand` heading.

- **R-019** — `project-rules.md` is on-demand too, but its pointer SHALL
  merge into the existing line at `AGENTS.md.tpl:11` ("… read
  `docs/ai/rules/core/project-rules.md` before adding one") rather than gain a
  second, separate bullet.

- **R-003** — Each pointer line under `### Read on demand` SHALL follow the
  fixed grammar "- Before `<trigger>`, read `` `<path>` `` — it is law, not a
  reference."

- **R-020** — Each pointer line under `### Read on demand` SHALL name the
  moment the rule starts governing, never a moment after (see the per-file
  trigger table below).

- **R-004** — `opencode.json.tpl`'s `instructions` array SHALL list only the
  always-tier core rule paths explicitly (never the `docs/ai/rules/core/*.md`
  glob, never `docs/okf/codebase-map.md`), so opencode does not autoload an
  on-demand rule either.

- **R-005** — WHEN Step 5 (legacy migration) writes the owned-rule wiring into a
  canonicalized `AGENTS.md`, THEN it SHALL write the same two-import-plus-pointer
  shape as a fresh scaffold (R-002, R-003), never the eleven-import block.

- **R-021** — `references/migration.md` SHALL describe the same shape as
  R-005 throughout (including its §1 "New sections to add" bullet).

- **R-006** — Step 1's manifest-less edge case (`SKILL.md:26`) SHALL detect an
  already-legislated repository using an always-tier import marker
  (`@docs/ai/rules/core/pair-development.md`), never an on-demand one
  (`okf.md`), since a repository legislated under this case's design no longer
  imports `okf.md` at all.

- **R-022** — `LegislatorOptions.LegislationMarker`'s default
  (`LegislatorOptions.cs:76`) SHALL change from `"core/okf.md"` to
  `"core/pair-development.md"`, since that option — not the `SKILL.md` prose
  alone — is what `Detection.cs` actually reads to decide `reconstructed`.

- **R-007** — WHEN the upgrade report's "Needs your review" section proposes
  import changes for an on-demand owned rule (including
  `docs/okf/codebase-map.md`, which follows this branch unconditionally, never
  gated on whether the file was freshly scaffolded this run), THEN it SHALL
  propose removing the rule's `@import` line if present in the entry document.

- **R-023** — Under the same WHEN as R-007, the report SHALL propose adding
  the rule's pointer line unless the entry document — checked with any
  `@import` line for that rule excluded — already contains the rule's path.
  (The exclusion matters: the `@import` line itself contains the rule's path,
  so checking the unfiltered text would make the add-pointer half silently
  never fire while the import is still present — exactly the case this
  requirement exists to cover.)

- **R-024** — Under the same WHEN as R-007, the report SHALL NOT propose
  adding an `@import` line for an on-demand rule under any circumstance.

- **R-025** — R-023's guard is deliberately "the rule's path is present
  outside `@import` lines", not "the entry's pointer line matches the
  template's pointer line byte-for-byte": an owner may reword an existing
  pointer (an older edition's trigger text, or their own prose), and that
  wording is the owner's to keep — the proposal SHALL NOT re-propose adding
  the pointer merely because its wording differs from the template's.

- **R-008** — WHEN the upgrade report's "Needs your review" section proposes
  import changes for an always-tier owned rule, THEN it SHALL propose adding an
  `@import` line exactly as it does today (unchanged).

- **R-026** — Tier (R-007/R-008) SHALL be tested only against
  `docs/ai/rules/core/<name>` paths — a stack rule under
  `docs/ai/rules/stacks/**` is never on-demand under this case and keeps
  today's add-only behavior regardless of its filename.

- **R-009** — `core/project-rules.md` SHALL state that a project rule may also
  be on-demand: placed outside `.claude/rules/`, reached only by a pointer line,
  so neither Claude Code's directory autoload nor opencode's
  `.claude/rules/*.md` glob loads it.

- **R-010** — `evals/check_static.py` SHALL assert, for every file under
  `skill/assets/rules/core/`, that its path appears in `AGENTS.md.tpl` in
  exactly one of the two forms — an `@import` line, or inside a pointer/bullet
  line naming it — and never in both, replacing the current "every core rule is
  `@`-imported" assertion.

- **R-027** — `evals/check_static.py` SHALL also assert that
  `opencode.json.tpl`'s `instructions` array lists exactly the always-tier
  core rule paths and no on-demand one.

- **R-011** — `skill/VERSION`, `src/Legislator.Cli/Version.props`'s major, and
  `skill/assets/release/release.json`'s `edition` SHALL bump together in one
  commit (as `a9c34b2` did).

- **R-028** — `release.json`'s per-rule digests SHALL be filled at tag time,
  unchanged from today.

- **R-029** — A change fragment SHALL record the case under `docs/changes/`.

- **R-030** — The full e2e benchmark SHALL be recorded at
  `evals/benchmarks/v<N>.md` against the prior edition's baseline before
  delivery to this repository.

- **R-012** — Before this case is delivered to this repository, an Architector
  probe re-run (main session and a general-purpose subagent, first-call token
  count measured the same way `issue-73.md` measured it) SHALL be recorded,
  showing the token drop the issue predicts (about −11,000 tokens from the core
  alone). The orchestrator runs this probe, last, since it needs the released
  binary and the fleet-wide measurement harness.

### Per-file trigger table

| file | tier | trigger |
| --- | --- | --- |
| `pair-development.md` | always | every git act |
| `decision-gate.md` | always | any turn |
| `okf.md` | on-demand | before changing code that implements a concept |
| `sdd.md` | on-demand | before starting any unit of work or merging |
| `verification.md` | on-demand | before writing tests or implementation code, and before reporting done |
| `changelog.md` | on-demand | before the first commit on a task branch, or touching `CHANGELOG.md`/`docs/changes/` |
| `artifact-lifecycle.md` | on-demand | creating, deleting or reporting on an artifact |
| `skills.md` | on-demand | at each stage boundary (plan, implement, debug, review), or before invoking a skill |
| `dev-journal.md` | on-demand | writing a fragment's `## journal` section, or editing `docs/journal/` directly |
| `project-rules.md` | on-demand | adding or editing a `.claude/rules` file |
| `adr.md` | on-demand | closing a decision-gate stop, introducing a new invariant, or deliberately keeping an accepted antipattern or tradeoff |
| `docs/okf/codebase-map.md` | on-demand | finding where something lives |

## Hurting case

**GIVEN** a legislated repository whose `AGENTS.md` still carries the eleven-import
block from a prior edition,
**WHEN** the owner runs `/legislator` to upgrade to this edition,
**THEN** the report's "Needs your review" section proposes removing the nine
on-demand `@import` lines and adding their nine pointer lines (plus the
`codebase-map.md` pointer) in the same proposal — never proposing to re-add any
of them — and the regenerated `opencode.json`'s `instructions` array no longer
names any on-demand file, so the owner's very next session (main or a
general-purpose subagent with a fresh brief) does not pay to load `okf.md`,
`sdd.md`, `verification.md`, `changelog.md`, `artifact-lifecycle.md`,
`skills.md`, `dev-journal.md`, `project-rules.md`, `adr.md`, or
`codebase-map.md` on its first call.

The issue's Done-when ("no Needs your review import lines") describes the state
*after* the owner applies this report's proposals: the upgrade run that
discovers the stale eleven-import block legitimately produces proposals — that
is the hurting case above — and it is the *following* run, against the
now-updated entry document, that reports nothing left to review.

## Clarifications

Ruled by the orchestrator; recorded here, not reopened.

- **Q1 — exact pointer-line grammar (D2).** Ruled: `- Before <trigger>, read
  \`<path>\` — it is law, not a reference.` (one bullet per on-demand file,
  grouped under a labeled sub-heading in `AGENTS.md.tpl`, `### Read on
  demand`), with each trigger naming the moment the rule starts governing,
  never a later moment (per-file trigger table above, findings 9-15).

- **Q2 — where the tier split is declared as data (D1/R-001).** Ruled:
  `skill/assets/templates/AGENTS.md.tpl` is the single source — an `@` line is
  always-tier, a pointer line is on-demand. `ReportJob.ReviewLines` reads the
  template from `parsed.Skill` and proposes the template's own pointer line
  verbatim. No engine-side set, no Python mirror. `evals/check_static.py`
  asserts every core rule appears in the template exactly once in exactly one
  form, and that `opencode.json.tpl`'s `instructions` lists exactly the
  always-tier files.

- **Q3 — does `docs/okf/codebase-map.md` get a pointer, same as a core rule
  (D4)?** Ruled: yes, on-demand like a core rule, including the remove half —
  `ReviewLines` proposes removing its `@import` unconditionally (not gated on
  `scaffolded.Contains`), exactly like any on-demand rule, even though
  `SKILL.md` Step 4 (not Step 3) is what writes the file — only its
  entry-document wiring changes from `@import` to pointer.

- **Q4 — do stack rules or `.claude/rules/*.md` get any tier treatment here
  (D5)?** Ruled: no — out of scope per the Boundary above; they stay
  always-loaded until a later case extends the split.

- **Q5 — is the upgrade proposal atomic (remove + add in one proposal) or can
  an owner apply the removal now and the pointer later (D3)?** Ruled: atomic —
  `ReviewLines` always emits both halves together for a given on-demand rule,
  so a partial apply is the owner's choice, never the tool's suggestion.

- **Q6 — does `project-rules.md` get a second pointer bullet (D6)?** Ruled: no
  — its pointer merges with the existing line at `AGENTS.md.tpl:11`, which
  already names the path and reads like a pointer; a second bullet for the same
  file under `### Read on demand` would be redundant.

- **Q7 — does the edition bump and the Architector re-probe happen in the same
  commit as the rest of the case (D7)?** Ruled: no — `skill/VERSION`,
  `Version.props`'s major, and `release.json`'s `edition` move together in one
  commit; `release.json`'s digests still wait for tag time as they do today.
  The Architector re-probe is a requirement (R-012) and the last task, run by
  the orchestrator against the released binary.

- **Q8 — does the manifest-less detection default change (D8)?** Ruled: yes —
  `LegislatorOptions.LegislationMarker`'s default moves from `"core/okf.md"` to
  `"core/pair-development.md"`, since that option, not just `SKILL.md`'s prose,
  decides `Detection.cs`'s `reconstructed` outcome.

- **Q9 — how is a rule's tier decided when it is reached by an existing
  prose line, not a pointer-grammar bullet (D9, review round 2 finding 1)?**
  Ruled: tier by template, not by grammar — a core rule path (or
  `docs/okf/codebase-map.md`) that appears in `AGENTS.md.tpl` only on a line
  that is not an `@import` line is on-demand, whatever that line's shape. The
  text `ReviewLines` proposes adding is that template line verbatim —
  `project-rules.md`'s `AGENTS.md.tpl:11` line included. No rule-specific
  pointer text is hard-coded in C#.

- **Q10 — what happens when the skill package ships no `AGENTS.md.tpl` (D10,
  review round 2 finding 2)?** Ruled: the report fails loudly with a named
  error; there is no "treat everything as always-tier" fallback. `T-12` adds a
  small `AGENTS.md.tpl` fixture to the C# test skill packages
  (`RunJobs.AddSkill`, the Engine test `ApplyFixture`) so the twins have a
  template to read.

- **Q11 — does the template path, and the core/okf path matching, honor a
  repo's non-default layout options (D11, review round 2 finding 3)?** Ruled:
  yes — a new `SkillAgentsTemplate` option sits beside `SkillOpencodeTemplate`;
  the template path and the core-rule/okf paths it is matched against are
  resolved through the layout's options, not hard-coded strings.
  `ReportJob.ReviewLines` SHALL take `parsed.Skill` so it can resolve them.

- **Q12 — is an entry's pointer wording ever corrected to match the template
  (D12, review round 2 finding 4)?** Ruled: no, deliberately — the add-pointer
  guard is "the rule's path is present outside `@import` lines"; an existing
  pointer's exact wording is the owner's to keep, never overwritten because it
  drifted from the template. R-025 states this; `T-09` pins it with a test.

- **Q13 — does `T-12` change `setup_workspace.py:108`'s all-`@` shape (D13,
  review round 2 finding 5)?** Ruled: no — that shape is the hurting case's
  input (a repo "legislated one constitution version ago") and stays as every
  owned rule `@`-imported. `T-12` instead adds a grade check that the review
  proposes remove-`@` plus the pointer for an on-demand rule built from that
  fixture. Only the rule withheld at `setup_workspace.py:70` moves to an
  always-tier one (unchanged from the prior research decision).
