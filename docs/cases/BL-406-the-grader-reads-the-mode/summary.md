# BL-406 — the grader reads the mode, and the target resolves

**Tier: 0** (direct). Opened by a red smoke stage on the first benchmark run to grade a Step 4
assertion since 2026-09-20.

## What the benchmark found

The run measuring edition 27 on `sonnet` stopped at smoke, 24 of 25:

```
upgrade_creates_missing_artifacts
  upgrade failed to scaffold: ['docs/journal/<today>.md']
```

The agent was right and the measurement was wrong — twice over, from one commit.

**The notes column was never read.** `scaffold_artifacts()` matched `| <target> | <template> |`
and skipped only `(empty directory)` rows, so every mode inherited every row.

**The target was never resolved.** The literal `docs/journal/<today>.md` went into
`Path.exists()`, false for every repository that has ever existed — so `scaffold_artifacts_present`
could report only failure, in `scaffold` and `migrate` alike.

Both arrived with BL-360 (`e5997f3`, 2026-09-20) and neither was observed, because **no scenario
that asserts Step 4 was graded between that commit and this run**. One scenario *was* graded on
2026-09-22 during BL-397's refutation — `upgrade-drop-stack`, which carries no Step 4 assert at
all, a gap this case also closes.

## The law disagrees with itself — ruled, not resolved here

`## File authority` declares itself

> the only statement of what an invocation mode may do to a file in the target repo

and grants `scaffolded artifacts` × `migrate` and × `upgrade` the right `create-if-absent`.
Step 4's day-file row answers **Fresh-scaffold mode only**. The heading excludes migrate; the
table includes it; and BL-360's own rationale — the day file exists because a run committing
paths outside `docs/` reddens `journal-recency` — applies to a migration exactly as it does to a
scaffold.

**Ruled by the operator, 2026-09-23: the table governs, and the prohibition narrows to `upgrade`**
— the only mode the note names and the only one it gives a reason for. The derivation therefore
keys on the reasoned clause (`an upgrade writes no entry`) and not on the heading. Repairing the
heading's overreach is a `skill/SKILL.md` edit and a separate intent.

## What was done

- `scaffold_artifacts(mode)` reads the notes column; the notes cell is stripped of its trailing
  pipe.
- `_resolve_today()` substitutes the day and **does not raise** — a raise inside
  `scaffold_checks`' comprehension propagates out uncaught, `grading.json` is never written, and
  the scenario is not measured at all. `unresolved()` reports it as a named finding instead.
- **The question is what the run did, not what day it is.** `day_files_this_run_added()` reads
  the run's own trace — untracked, plus added against `eval-base`. A day file dated any other day
  was invisible to a calendar-keyed check, and re-grading a recorded run the next morning called
  a present day file missing, which makes `mutate.py`'s substrate check refuse the scenario.
- The assertion BL-360 legislated and nothing measured: an upgrade must not **write** a withheld
  target. A presence-only assert cannot tell obedience from the absence of the act.
- Both upgrade scenarios carry it. `upgrade-drop-stack` had inherited the upgrade path without
  asserting any of its artifact rights.
- `upgrade_creates_missing_artifacts` resolves its targets, which the sibling call site already
  did.
- **`selftest:derivation` is now a gate** in `tools/evals-bg.sh` stage 1. It held real controls
  and ran nowhere the benchmark ran — which is how `audit_slugs_derived` stayed red for three
  days unremarked. One standing red (`#64`) is named rather than silenced; any other failing
  control stops the run, and that exception line dies with the issue.
- The derivation controls pin the **exact list** of withheld rows, not a count ≥ 1: a count stays
  green while a newly restricted row goes unread, which is this case's own defect returning.

## Evidence

Re-grading recorded runs with the repaired grader:

```
upgrade                        26/26 measured, 26 passed   (was 24/25)
fresh-scaffold-dotnet          21/21 measured, 21 passed
```

Mutations, each killing its assert and only its assert:

```
upgrade writes the day file          -> FAIL upgrade_writes_no_forbidden_artifact
upgrade writes one dated YESTERDAY   -> FAIL upgrade_writes_no_forbidden_artifact
  (the refutation round's probe A; a calendar-keyed check saw nothing)
a genuinely owed artifact removed    -> FAIL upgrade_creates_missing_artifacts
fresh scaffold loses its day file    -> FAIL scaffold_artifacts_present
a migration writes the day file      -> no finding, per the ruling
the law's clause reworded            -> FAIL upgrade_forbidden_rows_detected
```

`selftest:derivation`: 19 measured, 18 passed — the one red is `#64`, standing on `origin/master`
before this branch.

## The round

Two seats: `deepseek-v4-pro` on a separate provider, and `fable` — a Claude Code child on the
author's own provider, which it declared first, so its confirmations carry a reader's weight and
not a round's. **Both found the law's self-contradiction independently**, before the operator
ruled on it. Six findings changed the code: the calendar keying, the raise that loses a scenario,
the unresolved sibling call site, the uncovered `upgrade-drop-stack`, the controls that ran
nowhere, and a count where a list was owed. Two corrected the record: the second defect was a
paragraph inside this case's issue rather than a filed item (now `#64`, and larger than described
— the engine emits `bindings-form` at `Critical` where the law says `Warning`), and the date claim
was true of the defect but loose as written.

## Found and not fixed here

`#64` — check 22 carries a severity and no pinned slug, law and engine disagree on that severity,
and the control that would catch it prints its success message on failure. Its repair edits
`skill/SKILL.md`; a second intent.
