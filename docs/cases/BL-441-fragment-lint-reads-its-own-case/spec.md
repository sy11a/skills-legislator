# BL-441 — FragmentLint reads its own case, not every other case's fragment

**Tier: 1 (light).** An engine defect against the law as written (`core/changelog.md`:
"one fragment per case", "a task branch adds one change fragment"): no `skill/` law
change, no VERSION bump, no benchmark. Branch `bl/441-fragment-lint-reads-its-own-case`;
closes `sy11a/skills-legislator#51`.

**Spec type: bugfix.**

## Boundary

In scope: `FragmentLint.Findings`' branch check only — inverting the question from
"every fragment belongs to this branch" to "this branch's case has a fragment", the
`BranchCaseKey` resolution that walks the branch name, and the `non_case_branch_prefixes`
option it leans on. Out of scope: the fragment shape checks themselves (they are the
unchanged regression contract), `RenderJob`, any law text under `skill/assets/**` or
`docs/ai/**`, the retired Python prototype, and any edition or VERSION bump.

## Current behavior

`FragmentLint.Findings` walks every case-named file under the fragment home and, for each,
asks whether its `case` matches the current branch (`BranchMatchesCase`). Fragments
accumulate until a release cut (`core/changelog.md`: "At a release cut … the fragments are
deleted"), so the moment a second case's fragment exists, every branch but one reports a
finding on a correct file:

```
docs/changes/L-3.md: case 'L-3' does not match current branch 'bl/347-retelling-layer'
  → a fragment belongs to the branch that writes it per core/changelog.md
```

It fires from `master` too, with a single fragment in the tree. Release 3 is blocked on it:
foundry's merge queue runs its declared gates in the task worktree on the task branch after
rebasing onto `release/3`, where the earlier merged cases' fragments sit — so every
candidate reds (`sy11a/skills-legislator#51`).

## Expected behavior

- **R-441** — WHEN a branch carries a case key and no fragment names that case, THEN
  `sdd-lint` SHALL emit one finding naming that case and its missing fragment.

- **R-442** — WHEN a branch carries a case key, THEN `sdd-lint` SHALL emit no branch
  finding for any fragment whose case is not that key.

- **R-443** — WHEN the current branch carries no case key (`master`, `main`, `release/3`,
  a detached HEAD), THEN `sdd-lint` SHALL emit no branch finding at all.

## Unchanged

- Every fragment shape check still runs on every fragment, whichever case: YAML front
  matter present; all four keys (`case`, `issue`, `kind`, `date`) non-empty; `kind` in the
  closed set; `date` parseable; the case-named file declares the case it is named for;
  exactly one `## changelog` bullet; a `## journal` section present; and `README.md` of the
  home is furniture, not a fragment (the BL-410 gate).

## Hurting case

**GIVEN** a task branch `bl/414-…` rebased onto `release/3`, whose worktree carries its own
well-formed `BL-414.md` fragment plus three already-merged cases' well-formed fragments
(`BL-410`, `BL-347`, `L-3`),
**WHEN** the merge queue runs declared gate `legislator sdd-lint` in that worktree,
**THEN** the gate exits 0 with no branch finding — where today it reds on all three foreign
fragments (and it alone blocks the release, since every candidate carries them).

## Clarifications

- 2026-09-26 (session) — Q: how is "carries a case key" decided, and does a branch with no
  case at all report anything? A (brief + issue #51): the branch carries a key when its name
  places one at the ticket position — verbatim (`bl/BL-347-verbatim`, `feature/bl-441-…`) or
  slashed (`bl/347-retelling-layer`, `l/3-change-fragments`) — judged by the same matching
  `BranchMatchesCase` already performs, inverted. A branch whose name carries no case key —
  `master`, `main`, `release/3`, a detached HEAD — reports no branch finding at all. A
  leading letters prefix that is an integration word (`release/3`, `hotfix/…`, `rc/…`) names
  a release, not a case, and reads as no key.

- 2026-09-26 (session) — Q: does a fragment the branch's case *has* but which is malformed
  count as present? A: presence is the fragment file's name, not its shape — a malformed
  fragment is reported by the shape checks (which still run on every fragment), never as an
  absent one, so a branch cannot be told both "your fragment is missing" and "your fragment
  is malformed" at once.

## Converge — 2026-09-26

- **per R-441 (complete).** `BranchCaseKey` resolves the branch's case (`BranchMatchesCase`
  kept as the judging primitive); when no fragment file names that case, `Findings` emits
  `case '<key>' has no change fragment …`. Red-first: `The_branchs_case_without_a_fragment_is_a_finding`,
  shown failing on the unrepaired code and green after.
- **per R-442 (complete).** The per-fragment branch stub is gone; a branch reads only its
  own case. Red-first: `A_second_cases_fragment_is_silent_on_this_branch` (the #51 repro),
  and `A_case_named_file_declaring_another_case_is_a_finding` now reports the misdeclaration
  alone.
- **per R-443 (complete).** `non_case_branch_prefixes` (`release`, `hotfix`, `rc`) reads an
  integration name as no key, and `BranchCaseKey` returns null for `master`/`main`/detached.
  Red-first: `A_branch_that_names_no_case_reports_no_branch_finding`.
- **Unchanged verified.** Every existing shape test stays green; `A_number_is_not_truncated_to_a_shorter_case`
  guards `BranchMatchesCase`'s number boundary through the inverted path. Suite 819 green,
  `check_static` clean.