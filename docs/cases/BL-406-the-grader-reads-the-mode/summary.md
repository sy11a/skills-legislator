# BL-406 — the grader reads the mode, and the target resolves

**Tier: 0** (direct). Opened by a red smoke stage on the first benchmark run since 2026-09-20.

## What the benchmark found

The run that measured edition 27 on `sonnet` stopped at smoke, 24 of 25:

```
upgrade_creates_missing_artifacts
  upgrade failed to scaffold: ['docs/journal/<today>.md']
```

The agent was right and the measurement was wrong. `skill/SKILL.md` Step 4's table says of that
row:

> **Fresh-scaffold mode only** — an upgrade writes no entry, because the day's work is the
> upgrading repository's to record.

That clause arrived with BL-360 on 2026-09-20 (`e5997f3`). `evals/grade.py` was not told.

## Two defects, one row

**The notes column was never read.** `scaffold_artifacts()` matched `| <target> | <template> |`
and skipped only `(empty directory)` rows. Every mode therefore inherited every row, so the
upgrade and both migration scenarios asserted a file the law forbids them to write.

**The target was never resolved.** The row's target is written `docs/journal/<today>.md`, and
nothing substituted the day. The literal string went into `Path.exists()`, which is false for
every repository that has ever existed — so `scaffold_artifacts_present` could report only
failure, in `scaffold` and `migrate` alike, and `upgrade_creates_missing_artifacts` likewise.
Both were latent from the same commit and neither was observed, because **no benchmark ran
between BL-360 and this one**: `evals/benchmarks/v27.md` was written 2026-09-08, twelve days
before the law changed.

## What was done

- `scaffold_artifacts(mode)` reads the notes column. Reading the third column is the same act
  the second already performed for `(empty directory)`: the law states the restriction in its own
  words and the derivation obeys them.
- `_resolve_today()` substitutes the day, and **raises** on a placeholder it cannot resolve — the
  alternative is a check that cannot pass, quietly telling the reader the run is at fault.
- `scaffold_checks(repo, mode)` takes the mode; its three call sites pass `scaffold`, `migrate`,
  `migrate`.
- **The assertion BL-360 legislated and nothing measured**: an upgrade or a migration must not
  write a reserved target. A presence-only assert cannot tell obedience from the absence of the
  act — the day file is missing both when the run correctly wrote none and when it never reached
  Step 4 at all.
- Both new asserts are emitted **only where the law actually withholds something from that mode**.
  In `scaffold` the reserved rows are owed, so the question has one possible answer, and an assert
  that cannot fail reports coverage it does not have.
- Three controls in `grade_derivation_selftest` pin the derivation: that some row is detected as
  reserved, that the day file is among them, and that every target resolves to a path expression.
  If the law's wording moves and the pattern stops matching, these redden — rather than every
  scenario silently asserting a forbidden artifact again for another three days.
- Mutations for both new asserts, derived from the grader's own list so the law is stated once.

## Evidence

Re-grading **the same recorded run** that scored 24/25:

```
$ python3 evals/grade.py <ws> upgrade
== upgrade: 26/26 measured, 26 passed ==
```

Three mutations, each killing its assert:

```
# the upgrade writes the reserved day file
FAIL  upgrade_writes_no_scaffold_only_artifact — upgrade wrote a target the law
      reserves to fresh scaffold: ['docs/journal/<today>.md']

# a genuinely owed artifact removed — the narrowed list still catches a real miss
FAIL  upgrade_creates_missing_artifacts — upgrade failed to scaffold: ['docs/cases/README.md']

# the law's marker reworded — the derivation control reddens instead of the corpus
journal_day_is_scaffold_only -> False
```

`selftest:derivation` 19/19 measured (16 before this case), all three new controls green.

## Found and not fixed here

`audit_slugs_derived` is **red on `origin/master`** and was before this branch: 22 audit checks
carry a severity, 21 carry a pinned slug — `Bindings form` (BL-357) has a severity and no slug.
The same class as this case: law changed in one place, its machine-readable half untouched. Its
repair edits `skill/SKILL.md`, which is a behavioral change and a second intent; filed rather
than folded in. Its check also prints its **success** message on failure, which is why a red
reads as a pass.
