# BL-062 — what was measured, and how to measure it again

Companion to `spec.md`. This is the evidence half: the numbers the case was
judged on, and the commands that produce them. No agent runs, no tokens — the
whole verification is a replay of artifacts the v21 cycle already recorded.

## The substrate

Two workspaces survived the v21 cycle and both were used:

| Workspace | Used for | Why |
|---|---|---|
| `/tmp/legislator-eval-v21f` | nine scenarios | the final v21 corpus run |
| `/tmp/legislator-eval-v21e` | `restructure` only | v21f's restructure **report was overwritten by the idempotency run 2**, which had no `[heal]` item; replaying it there judges a healed tree against the wrong authority column. v21e's report is the one the recorded 38/38 was taken on. |

That overwrite is worth remembering: a scenario's `outputs/` is not immutable
after grading, so a replay substrate must be validated against the recorded
verdict before it is trusted. Every scenario below was.

Three fixtures (`fresh-scaffold-dotnet`, `upgrade`, `restructure`) carry a
`run 1` commit from the idempotency stage, so the corpus state has to be
reconstructed rather than merely reset — `reset --mixed eval-base` alone leaves
run 2's writes in the tree:

```bash
run1=$(git -C "$R" log --format='%H %s' | awk '$2=="run" && $3=="1" {print $1; exit}')
git -C "$R" reset --hard "$run1" && git -C "$R" clean -fdq
git -C "$R" reset --mixed "$(git -C "$R" rev-parse eval-base)"
```

## A — the regression contract: healthy artifacts, unchanged verdicts

Every scenario reproduces its recorded `grading.json` verdict, fully measured.
An assert that had gone `unmeasured` here would mean a mislabeled artifact.

| Scenario | Before | After |
|---|---|---|
| `fresh-scaffold-dotnet` | 19/19 | 19/19 measured, 19 passed |
| `legacy-migration` | 27/27 | 27/27 measured, 27 passed |
| `legacy-migration-agents-first` | 22/22 | 22/22 measured, 22 passed |
| `upgrade` | 21/21 | 21/21 measured, 21 passed |
| `upgrade-drop-stack` | 16/16 | 16/16 measured, 16 passed |
| `case-practice` | 7/7 | 7/7 measured, 7 passed |
| `audit` | 44/44 | 44/44 measured, 44 passed |
| `audit-engine-absent` | 5/5 | 5/5 measured, 5 passed |
| `restructure` (v21e) | 38/38 | 38/38 measured, 38 passed |
| `selftest:derivation` | 16/16 | 16/16 measured, 16 passed |

Exit code 0 in both columns. 215 asserts, none reclassified.

## B — the mutation: one report truncated to zero bytes

```bash
: > "<ws>/<scenario>/outputs/<report>.md" && python3 evals/grade.py "<ws>" "<scenario>"
```

| Scenario | Before | After | Exit before → after |
|---|---|---|---|
| `legacy-migration-agents-first` | **22/22 — a full green** | 22/22 measured, 21 passed | **0 → 1** |
| `upgrade-drop-stack` | 15/16 | 15/16 measured, 14 passed | 1 → 1 |
| `legacy-migration` | 25/27 | 25/27 measured, 24 passed | 1 → 1 |
| `upgrade` | 18/21 | 18/21 measured, 17 passed | 1 → 1 |
| `audit-engine-absent` | 4/5 | 3/5 measured, 2 passed | 1 → 1 |
| `restructure` | 30/38 | 30/38 measured, 29 passed | 1 → 1 |
| `audit` | **14/44 — 32%** | **5/44 measured, 4 passed** | 1 → 1 |

The headline is the first row, not the last. `legacy-migration-agents-first`
scored a **perfect 22/22 and exited 0** against a report containing nothing,
because its one report-reading assert tested `path.exists()` and a zero-byte
file exists. That is the shape of the defect: not a low score, a full one.

In `audit`, all nine `does NOT contain` asserts now report `UNMS`. They are the
class `ghost_import_fixed` was fixed as a single instance of in v17; this closes
the class, by construction rather than by review.

## C — the mutation nobody could see: the report deleted

`upgrade-drop-stack` guarded its content assert behind `if has_report:`.

| | Before | After |
|---|---|---|
| `upgrade-drop-stack`, report deleted | `14/15` — the assert **left the corpus** | `15/16 measured, 14 passed` — 1 failed, 1 unmeasured |
| `audit`, report deleted | `13/44` | `5/44 measured, 4 passed` |

A denominator that shrinks when an artifact vanishes is the purest form of the
disease: the suite reported a *higher* proportion for having measured less.
`case-practice` carried the same shape as an early return — five asserts that
disappeared whenever the run produced no case directory.

## D — propagation, the BL-058 half

`tools/evals-bg.sh` decided "green" in four places, each restating
`summary.failed == 0`. All four now call one `grade_clean`, which is red on
`failed` **or** `unmeasured`, and treats an unreadable `grading.json` as red.
Verified on a green grade, a red one, and a missing file.

The dashboard renders both numbers, marks any unmeasured scenario `gbad`,
suppresses the pass-rate percentage entirely while anything is unmeasured, and
counts persistently-unmeasured asserts separately from flaky ones. Workspaces
graded before this change still render: the new keys are read with defaults, so
a pre-BL-062 `grading.json` degrades to "everything was measured", which is
what it meant when it was written.

## E — the rest of the ladder

`check_static.py`, `check_engine.py`, `check_hooks.py`,
`check_opencode_plugin.mjs`, `docs/ai/engine.py anchors` and
`docs/ai/engine.py okf-debt`: all clean. No `skill/` file was touched, so no
benchmark was owed (`.claude/rules/evals.md`).

`check_static.py` caught one thing on the way: a fleet repository name that had
leaked into BL-061's backlog entry, merged the same day in PR #16. Replaced
with its alias.
