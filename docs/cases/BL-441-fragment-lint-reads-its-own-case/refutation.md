# BL-441 — Refutation round

Refuter: zai glm-5.3
Date: 2026-09-26
Subject under test: PR #71 (commit f943c4d, branch `bl/441-fragment-lint-reads-its-own-case`) against issue #51.

Method: the two release shapes and a branch-name zoo were built as scratch git repos under
`~/.cache/bl441r/` and driven with the engine built from HEAD by `bash evals/check_dotnet.sh`
(`artifacts/linux-x64/legislator`, digest `adc363a7401a…`). Mutations were applied to
`src/Legislator.Engine/Sdd/FragmentLint.cs`, built and run through
`sh <kernel>/dotnet-lock.sh --short dotnet exec …Legislator.Engine.Tests.dll`, then reverted
(`git checkout --`); the working tree is byte-identical to f943c4d except this file.

## Verdict

The inversion itself holds: both foundry shapes go green, the regression contract fires, and
every new test's named mutation reddens. But the key-derivation half is refuted in both
directions — it mints **false case keys** on real integration branches of this very fleet
(`rc/edition-27`, the branch v27 was cut on, reddens today), and it **silently loses** the key
on multi-segment task branches. #51's defect class ("a lint that fires on correct work")
survives the fix in a narrower but real shape.

## Findings

### F-1 (High) — the non-case exemption never consults the branch's leading segment; `rc/edition-27` reddens

- Where: `src/Legislator.Engine/Sdd/FragmentLint.cs:323-329` (verbatim ticket slice) and
  `:380-398` (`IsCasePrefix` tests the candidate key's own letters, not the branch's first
  segment); doc claim it contradicts at `:298-300`.
- Evidence (engine on a scratch repo carrying this repository's real nine fragments, branch
  checked out per row):

  ```
  rc/edition-27                                 exit 1  case 'edition-27' has no change fragment → a branch that names a case writes that case's fragment per core/changelog.md
  rc/edition-28                                 exit 1  case 'edition-28' has no change fragment → …
  release/edition-3                             exit 1  case 'edition-3' has no change fragment → …
  release/3                                     exit 0
  ```

  `rc/edition-27` is this repository's own RC convention (`91f924c` merged from it); the RC
  branch is exactly where every pending fragment accumulates. The doc comment says "A branch
  whose leading letters prefix is an integration word (`release/3`, `hotfix/4`, `rc/…`)
  carries no case key" — the code only applies the exemption when the integration word is the
  candidate's own letters (the slashed form `rc/3`). In the verbatim form the ticket's letters
  ("edition") are tested instead, so `rc/<word>-<digits>` and `release/<word>-<digits>` mint a
  key the branch does not carry. This is #51 reborn on the release branch of the very next
  edition cut.
- Fix: in `BranchCaseKey`, before deriving any candidate, return null when the branch's first
  path segment equals a `non_case_branch_prefixes` entry — making the code match the doc
  comment's existing sentence.

### F-2 (Medium) — any `letters-digits` is accepted as a case key; the law closes the form to `BL-NNN` or `L-N`

- Where: `src/Legislator.Engine/Sdd/FragmentLint.cs:303-340` (no validation of the candidate's
  letters prefix); law: `skill/assets/rules/core/changelog.md:14` ("the case key (`BL-NNN` or
  `L-N`)"); the option's own doc knows the closed set — `src/Legislator.Core/Options/LegislatorOptions.cs:149`
  ("Case prefixes (`bl`, `l`) are never listed here").
- Evidence (engine, scratch repo with two well-formed fragments, no other findings):

  ```
  task/12-x          exit 1  case 'task-12' has no change fragment → …
  wave/2             exit 1  case 'wave-2' has no change fragment → …
  v27/task-12        exit 1  case 'task-12' has no change fragment → …
  feature/fix-404-page exit 1  case 'fix-404' has no change fragment → …
  r3/5-x             exit 0
  dependabot/npm_and_yarn/frontend/eslint-8.57.0  exit 0
  ```

  `feature/<kebab-case-description>` is the development law's own default branch naming
  (`core/pair-development.md`, "Each task gets its own branch — naming:
  `feature/<kebab-case-description>`"), and a kebab description beginning `word-digits`
  (`fix-404-page`) reddens with a key that is not a lawful case key. A blacklist of three
  integration words cannot enumerate the fleet's non-case vocabulary; the option's doc
  comment already names the actual closed set (`bl`, `l`) without enforcing it.
- Fix: whitelist the case prefixes (a `case_branch_prefixes` option defaulting to `bl,l`,
  mirroring `NonCaseBranchPrefixes`) and reject candidates whose letters are not in it —
  a closed form checked closed, instead of an open form checked against an open blacklist.

### F-3 (Medium) — multi-segment task branches silently lose their key; the surviving mutation

- Where: `src/Legislator.Engine/Sdd/FragmentLint.cs:324` (ticket sliced after the **first**
  slash) vs `:292` (doc: "the ticket (the **last** segment) begins `letters-dash-digits`");
  the "cannot drift" claim at `:300-301`.
- Evidence: `feature/user/bl-441-x` — `BranchMatchesCase(branch, "BL-441")` is true (verbatim
  contains), yet the shipped engine reports nothing:

  ```
  --- shipped (unmutated) build on feature/user/bl-441-x:   exit 0   (no output)
  case 'bl-441' has no change fragment → …        ← only under the mutation below
  ```

  Mutation M7 — one line, ticket slice reads the last segment (the doc comment's own words):

  ```
  total: 364   failed: 0      (entire Legislator.Engine.Tests module, mutated)
  mutated build: case 'bl-441' has no change fragment → …  exit 1
  shipped build: exit 0
  ```

  No test in the suite kills M7; the behavior it reveals is a branch that names a case (by the
  primitive's own judgement) and is told nothing — the silent-false-green direction, the
  expensive error under `core/artifact-lifecycle.md`'s rule. Doc and code disagree about which
  segment is the ticket; whichever is chosen, a test should pin it.
- Fix: pick one (last segment matches the doc and the shape `group/user/bl-NNN-slug`), fix the
  slice, and add the multi-segment row to `A_branch_names_its_own_case_in_either_form`.

### F-4 (Low) — the finding names the case in the branch's casing, not the law's key form

- Where: `src/Legislator.Engine/Sdd/FragmentLint.cs:151`; law form at
  `skill/assets/rules/core/changelog.md:14` (`BL-NNN`); the same file's misdeclaration check is
  deliberately Ordinal because "The law writes the key `BL-NNN`" (`FragmentLint.cs:106`).
- Evidence: every probe on a slashed branch reports `case 'bl-414'`, `case 'bl-441'`,
  `case 'bl-3470'` — lowercase keys the law never writes and no tracker mints. A reader told
  to go write `bl-414`'s fragment is being told a key shape the rest of the mechanism
  (`RenderJob` keys by `StringComparer.Ordinal`) treats as a different case from `BL-414`.
- Fix: canonicalize the reported key (upper-case the letters segment) at the finding site, or
  emit the branch's own text in quotes separate from the key.

### F-5 (Low) — "reports the misdeclaration alone" is claimed but not asserted

- Where: `tests/Legislator.Engine.Tests/Sdd/FragmentLintTests.cs:81-84`.
- Evidence: under M2 (the old per-fragment branch stub re-introduced verbatim — exactly the
  behavior this test's comment says is gone), the test stays green:

  ```
  failed A_branch_that_names_no_case_reports_no_branch_finding (master, main, release/3 rows)
  failed The_branchs_case_without_a_fragment_is_a_finding
  failed A_second_cases_fragment_is_silent_on_this_branch
  total: 21   failed: 5        ← A_case_named_file_declaring_another_case_is_a_finding NOT among them
  ```

  `Assert.Single(all, x => x.Contains("declares case 'BL-999'"))` pins one *matching* finding,
  not one finding *overall*, so the pile-on the comment disclaims can return unnoticed (only
  sibling tests catch it, by other routes).
- Fix: add `Assert.Single(all)` (total count) beside the predicate form.

### F-6 (Low) — the law's description of `sdd-lint` now misdescribes the check

- Where: `skill/assets/rules/core/changelog.md:66` (and the delivered copy
  `docs/ai/rules/core/changelog.md:66`): "`sdd-lint` checks fragments (front matter, kind in
  the closed set, **case matching the branch**, exactly one `## changelog` bullet, …)".
- Evidence: the engine now answers "the branch's case has a fragment", not "the case matches
  the branch"; the old reading is the defect #51 fixed. The spec scopes law text out
  ("Out of scope: … any law text under `skill/assets/**`"), which is a lawful scope line, but
  the result is the shipped law describing a check the shipped engine no longer performs —
  the same doc/code divergence the FragmentLint doc comment was updated to avoid. Repair needs
  an edition bump by `constitution-source` discipline, so it wants a queued follow-up case
  riding the next edition, not silence.
- Fix: one-line law amendment ("the branch's case has its fragment") in the next edition's
  case; until then this refutation records the skew.

### Info (no action asked)

- The BL-441 fragment's changelog bullet links `docs/cases/BL-441-…/spec.md`, while the law's
  own example links a `summary.md` (`core/changelog.md`, "linking the case's own record
  (`docs/cases/<case>/summary.md)`"). Pre-existing divergence: `L-3.md` links a `summary.md`
  that does not exist in its case dir. Tier-1 cases ship no summary; law text and practice
  disagree independently of BL-441.
- The OKF bundle carries no concept document for the fragment mechanism at all, so no OKF doc
  went stale through this change (also pre-existing).
- The PR's account of the two engine-ruler reds is accurate and reproduced here:
  `audit_clean_repo_clean_report` fails only against a local rebuild (digest `adc363a7401a…` ≠
  released `f604750c85a8…`), and `audit_untagged_edition_is_an_info_line_not_a_finding` also
  fails against the released arm at `~/.local/bin/legislator` — neither touches FragmentLint.

## Checks that could have found something, and what each found

| Check | Result |
|---|---|
| Foundry shape (a): task branch `bl/414-fix-the-gates` after rebase, own fragment + BL-410/BL-347/L-3 foreign fragments | exit 0 — green (the #51 blocker is gone) |
| Foundry shape (b): `release/3` carrying BL-410/BL-347/L-3 | exit 0 — green |
| R-441 red: same shape minus the own fragment | exit 1, `case 'bl-414' has no change fragment` |
| R-442 + unchanged: own fragment well-formed, foreign `BL-2.md` malformed | exit 1 with BL-2's shape finding only, no branch finding |
| Branch zoo (22 names + detached HEAD) | greens: `master`, `main`, `release/3`, `release/3.1`, `hotfix/4`, `rc/3`, `r3/5-x`, `v27/renamed-gate`, `dependabot/…`, `feature/add-login-form`, detached; false keys: F-1/F-2 rows above; silent miss: F-3 |
| Nine shape checks, one defect at a time, real binary | each fires with its own message (front matter, case, issue, kind, date, misdeclaration, changelog section, bullet count, journal) |
| README/notes exclusion | exit 0 with `README.md` + `notes.md` furniture present |
| `legislator sdd-lint --root .` on this repo at `bl/441-refute` | exit 0 |
| Baseline `bash evals/check_dotnet.sh` | `all 819 .NET tests passed` |
| `python3 evals/check_static.py` | `all static checks passed` |
| `PARITY_ENGINE_CMD=<local rebuild> python3 evals/check_engine.py` | 2 reds, both arm-integrity (see Info) |
| `non_case_branch_prefixes` null / `""` via instance file | exit 2, `instance: non_case_branch_prefixes: must not be empty` — loud refusal, the cheap error; value `release` alone re-opens `hotfix/9` as a key, as configured |
| Literal restatement scan (`release`/`hotfix`/`rc` in `src/`, `tests/`) | only `LegislatorOptions.cs:150` (the options model) and a doc comment — no restated constant |
| Mutations M1–M5 (one per new test's named mutation) | M1 case-sensitive fragment match → 10 red; M2 old per-fragment stub → 5 red; M3 missing-fragment finding deleted → 2 red; M4 non-greedy digits → 3 red; M5 `IsCasePrefix` ignored → `release/3` row red |
| Mutation M7 (survivor) | 0 red across 364 — F-3 |

## Scope of the diff (Hunt 5)

Six files, all inside the declared boundary: case spec + fragment (case artifacts), the option
declaration/composer/enumeration triple, `FragmentLint.cs`, and its tests. Nothing under
`skill/`, `docs/ai/`, `plugin/`, or `evals/`; no VERSION movement; the two pre-existing test
edits (adding `branch: "master"`) are required by the new default-branch interplay, not scope
creep. The deliberate no-edition-bump is surfaced in the PR body with its consequence (the
installed binary keeps the defect until the next publish) — recorded here as consistent with
the case's declared scope, with F-6 as its one open debt.
