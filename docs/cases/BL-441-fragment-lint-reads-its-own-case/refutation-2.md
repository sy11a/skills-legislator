# BL-441 — Second refutation round (after the rework)

Refuter: zai glm-5.3
Date: 2026-09-26
Subject under test: PR #71's rework commit 61f6006 (branch `bl/441-fragment-lint-reads-its-own-case`),
answering `refutation.md` (F-1 High, F-2/F-3 Medium, F-4/F-5/F-6 Low). The rework claims F-1…F-5 closed
and F-6 filed as a follow-up.

Method: engine built from HEAD by `bash evals/check_dotnet.sh` through the kernel dotnet lock — with one
lesson for the next runner: the lock gates whole gates. If every nested `dotnet` also routes through it,
`Legislator.Cli.Tests` (which publishes the arm itself) deadlocks on its own parent's slot whenever the
semaphore narrows to one; nested calls must pass through. Local arm `artifacts/linux-x64/legislator`,
digest `a8ff9a26d3f3…`, gate result `all 833 .NET tests passed`. The branch zoo and the release shapes
are scratch git repos under `~/.cache/bl441r2/` carrying this repository's real fragments (the zoo repo
minus `BL-441.md` and `L-3.md`, so the naming rows can fire). Mutations were applied to
`src/Legislator.Engine/Sdd/FragmentLint.cs`, built and run through the lock as
`dotnet exec …Legislator.Engine.Tests.dll` (378 tests), then reverted (`git checkout --`); the working
tree is byte-identical to 61f6006 except this file.

## Verdict

F-1 through F-5 are closed and stay closed under mutation; F-6 is correctly queued (issue #72 exists and
says the right thing). The rework's own new surface carries three defects: the suite never varies the
configured whitelist, so a mutation that ignores `case_branch_prefixes` survives all 378 tests (F-7); a
real fleet case branch — foundry's `bl/F19-stage-outcome`, case F-19 — is silently missed and no
configuration recovers it (F-8); and a separator-only instance value silently disables the branch check
(F-9). None re-opens the #51 defect class: no false finding was produced on any zoo branch or on any
branch of the fleet's real branch census.

## First round — closure

### F-1 (was High) — CLOSED

The leading-segment integration words no longer mint keys, in either form. Engine on the zoo repo:

```
rc/edition-27          exit 0   (no output)
release/edition-3      exit 0   (no output)
release/3              exit 0   (no output)
```

`rc/edition-27` — the branch that reddened in round one — is green.

### F-2 (was Medium) — CLOSED

The open blacklist is gone; a candidate whose letters are not a case prefix is no key:

```
task/12-x              exit 0   wave/2   exit 0   v27/task-12  exit 0
feature/fix-404-page   exit 0   dependabot/npm_and_yarn/x-8.57.0  exit 0
l/3-x                  exit 1   case 'L-3' has no change fragment → …
```

`l/3-x` names its case in the law's form — the closed set accepts `l`, not only `bl`.

### F-3 (was Medium) — CLOSED

The ticket is now the last segment, and the row is pinned:

```
feature/user/bl-441-x  exit 1  case 'BL-441' has no change fragment → a branch that names a case writes that case's fragment per core/changelog.md
```

Mutation M4 — the first-segment slice re-introduced (`LastIndexOf` → `IndexOf`) — reddens 1 test
(`A_branch_naming_its_case_reports_its_absent_fragment`); the round-one survivor is dead.

### F-4 (was Low) — CLOSED

Every slashed or lowercase probe now reports the law's written form — `case 'BL-441'`, `case 'L-3'` —
never `bl-441`. Mutation M3 (canonicalization removed, the key passed through) reddens 6 tests.

### F-5 (was Low) — CLOSED

`Assert.Single(all)` (total count) sits beside the predicate form at
`tests/Legislator.Engine.Tests/Sdd/FragmentLintTests.cs:82`. Mutation M6 — the presence flag never set,
so the pile-on returns — reddens 14 tests, `A_case_named_file_declaring_another_case_is_a_finding`
among them, where round one's M2 left it green.

### F-6 (was Low) — FILED, by design not closed

`sy11a/skills-legislator#72` exists, is OPEN, and carries the right content: names the law sentence
("case matching the branch"), the one-line amendment ("the branch's case has its fragment"), both files,
why it rides the next edition (law text out of BL-441's scope, edition bump by constitution-source
discipline), with refs to this case's spec and the round-one report. One correction to the round-one
record below, under Info.

## New findings

### F-7 (Medium) — the suite never varies the configured whitelist; M1 survives 378/378

- Where: `tests/Legislator.Engine.Tests/Sdd/FragmentLintTests.cs` (no test constructs options with a
  non-default `CaseBranchPrefixes` — grep over `tests/` finds the symbol nowhere); the read it leaves
  unpinned at `src/Legislator.Engine/Sdd/FragmentLint.cs:63`.
- Evidence — mutation M1, the option's value replaced by the default it happens to ship with:

  ```
  total: 378   failed: 0      (entire Legislator.Engine.Tests module, mutated)
  ```

  The engine itself honors the layer — instance file `case_branch_prefixes: task`, confirmed by
  `config show` (`case_branch_prefixes = task  [instance]`):

  ```
  task/12-x    exit 1  case 'TASK-12' has no change fragment → …
  ```

  So the gap is the suite's, not the engine's: a future edit that silently drops the configured value —
  exactly M1 — ships green. The rework's headline is a configurable closed set; the tests pin only its
  default, and the instance layer's contract (a repo may lawfully widen or narrow its case prefixes) is
  exercised by no one.
- Fix: one test with custom options — a `task/12-x` row that fires and a `bl/441-x` row that goes silent
  under the narrowed set — plus one narrowed-blanket row mirroring the engine probes above.

### F-8 (Medium) — a real fleet branch the whitelist misses: foundry `bl/F19-stage-outcome`, case F-19

- Where: `src/Legislator.Engine/Sdd/FragmentLint.cs` — `KeyAt` (`:350-379`) requires
  letters-separator-digits, so the ticket segment `F19` mints no candidate at all; the closed set's
  default at `src/Legislator.Core/Options/LegislatorOptions.cs:152`.
- Evidence: foundry is a legislated fleet repo whose register carries eighteen real cases keyed
  `F-02`…`F-19`; `docs/cases/F-19-stage-outcome/` exists, its branch `bl/F19-stage-outcome` exists, and
  its fragment home carries no `F-19.md`. Engine on a scratch repo in exactly that state:

  ```
  bl/F19-stage-outcome                     exit 0   (no output)   ← default whitelist bl,l
  bl/F19-stage-outcome                     exit 0   (no output)   ← case_branch_prefixes: f,bl,l
                                                           (config show: f,bl,l  [instance])
  ```

  The silence is not the prefix set's fault: `F19` is not letters-separator-digits, so no candidate is
  ever minted and no configuration recovers it. The branch check — the finding R-441 exists to emit — is
  silently off for a whole real case family. The miss predates the rework (the old derivation also
  minted nothing here), but the rework's clarification canonizes the closed set as derived from the law
  ("the law names the closed set of case prefixes") while the fleet's real case vocabulary already
  exceeds it — the same open-world assumption round one refuted in the other direction.
- Fix: an operator ruling on whether non-BL/L case keys are lawful. If they are, foundry's branches must
  carry the key with its dash (`bl/F-19-…` is read today once `f` is in the whitelist) and its instance
  file carries the prefix; if they are not, foundry's register is drift to repair. Either way, the
  whitelist's derivation should record the fleet's real key families, not the law's prose alone.

### F-9 (Low) — a separator-only value silently disables the branch check

- Where: `src/Legislator.Core/Options/OptionsValidator.cs:40-43` (emptiness judged on the raw string)
  vs `src/Legislator.Core/Options/OptionsComposer.cs:124` (`List(value)` splits on `,`).
- Evidence — instance-file probes on `bl/441-x`, whose fragment is missing:

  ```
  case_branch_prefixes:  (null)    exit 2   instance: case_branch_prefixes: must not be empty
  case_branch_prefixes: ""         exit 2   instance: case_branch_prefixes: must not be empty
  case_branch_prefixes: ","        exit 0   config show: case_branch_prefixes =   [instance]
  ```

  The degenerate value a reader could type as a placeholder passes validation, composes to a list of
  empty strings, and turns the branch check off without a word — the expensive error under
  `core/artifact-lifecycle.md`'s rule. The old blacklist failed loud in this direction (fewer prefixes
  meant more findings); the whitelist inverts it to fail silent.
- Fix: validate the split elements, not just the raw string — refuse a list with an empty entry, or at
  least one whose entries are all empty, mirroring the existing "must not be empty" refusal.

## Info (no action asked)

- The spec's live Boundary still names the retired option — `spec.md:14`, "the
  `non_case_branch_prefixes` option it leans on". The Rework section supersedes it, but the Boundary is
  the paragraph a reader meets first; the option it names no longer exists.
- Round one's F-6 cited `docs/ai/rules/core/changelog.md:66`; that delivered copy is the 18-line v26
  text (last touched by BL-034) and carries no such sentence. The skew #72 amends lives in the skill
  source; `legislator audit --root . --skill skill` names the whole delivered set edition lag (five
  owned-integrity rows plus manifest v26 vs skill v27) — pre-existing, out of BL-441's scope, repaired
  by the next legislator run.
- A narrowed whitelist is honored as configured: `case_branch_prefixes: task` alone silences `bl/441-x`
  (exit 0, the missing fragment unreported) — the mirror of round one's "value `release` alone re-opens
  `hotfix/9`" note, recorded here as as-configured behavior, not a defect.
- The rework's new tests pin what they claim. Mutation kill counts on this round's matrix: M2 (prefix
  match made case-sensitive) 1 red; M3 (canonical key removed) 6 red; M4 (first-segment slice) 1 red;
  M5 (presence match made case-sensitive) 13 red; M6 (pile-on reintroduced) 14 red. Only M1 survives
  (F-7).

## Checks that could have found something, and what each found

| Check | Result |
|---|---|
| Release shape (a): `bl/414-fix-the-gates` with own BL-414 + foreign BL-410/BL-347/L-3 | exit 0 |
| Release shape (b): `release/3` carrying the three foreign fragments | exit 0 |
| Zoo greens — 11 rows (`rc/edition-27`, `release/edition-3`, `release/3`, `task/12-x`, `wave/2`, `v27/task-12`, `feature/fix-404-page`, `dependabot/npm_and_yarn/x-8.57.0`, `master`, `main`, detached HEAD) | all exit 0, no output |
| Zoo findings — `bl/441-x`, `bl/BL-441-x`, `feature/bl-441-x`, `feature/user/bl-441-x` → `BL-441`; `l/3-x` → `L-3` | each exit 1, exactly one branch finding, eight foreign fragments silent |
| Fleet branch census (773 refs across the 19 legislated repos; every real case branch is `bl/NNN-slug`, `refute/bl-NNN`, or `bl/F19-…`) | every law-form branch resolves a key; the one miss is F-8 |
| Instance-file probes: null, `""`, `","`, old key, custom prefix | loud / loud / F-9 / loud unknown-key refusal / honored (`case 'TASK-12'`) |
| Mutations M1–M6 | M1 survivor (F-7); M2 1, M3 6, M4 1, M5 13, M6 14 red |
| `bash evals/check_dotnet.sh` through the lock | `all 833 .NET tests passed`; local arm `a8ff9a26d3f3…` |
| `python3 evals/check_static.py` | all static checks passed |
| `PARITY_ENGINE_CMD=<local rebuild> python3 evals/check_engine.py` | 2 reds — the known arm-integrity pair (local rebuild digest ≠ the release record; the released-arm info line), neither touching FragmentLint |
| `legislator audit --root . --skill skill` | the pre-existing v26/v27 edition lag named under Info; nothing BL-441 caused |
| `legislator sdd-lint --root .` on this repo at `bl/441-refute-2` | exit 0 |
| Literal scan (`"bl"` / `"l"` defaults outside `LegislatorOptions.cs`) | nothing — the closed set is declared once |
| Issue sy11a/skills-legislator#72 | OPEN, correct content (F-6's queue) |

## Scope of the rework diff

Seven files: the round-one record, the case spec, the fragment, the options model, the composer,
`FragmentLint.cs`, and its tests — all inside the declared boundary. Nothing under `skill/`, `docs/ai/`,
`plugin/`, or `evals/`; no VERSION movement, consistent with the case's declared no-edition-bump scope.
