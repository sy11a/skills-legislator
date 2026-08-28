# BL-073 — The eval workspace needs a lock: one instrument at a time

**Tier: 1 (light).** Blast radius: the eval harness only — `tools/evals-bg.sh`,
`evals/mutate.py`, `tools/proc.py`, `evals/check_mutate.py`. No `skill/`
change, no VERSION, no benchmark. Novelty: low — a lockfile with a
liveness check, the shape every job runner already uses.

**Spec type: bugfix.** Branch `bl/073-eval-workspace-lock`. Source: the
v23 harness chronicle (`evals/benchmarks/v23.md`), two operator-induced
incidents.

## Current behavior

Two instruments write the same workspace with no coordination:

- A second `evals-bg.sh` invocation against a live workspace wipes every
  scenario's `grading.json` at invocation start — including scenarios the
  invocation never touches (a `--skip-smoke` run wipes `upgrade`'s record
  too) — and then runs its stall ladder against the first invocation's
  agents. Run-1 records survived the v23 incident only in
  `grade-history.jsonl`.
- A mutation pass started while a runner invocation is live has its
  substrate moved from under the Reverter mid-pass: one reverted line
  was silently lost from the restructure fixture, and two mutation
  summaries were produced on a moved substrate.

## Expected behavior

The workspace carries one lock. The runner and the mutation pass both
take it for their whole lifetime; the second instrument fails loud —
naming the holder — before it writes a byte. A lock whose holder is dead
is stale and is taken over with one printed line. The runner's
invocation-start cleanup removes only the records of scenarios the
invocation will touch.

## Unchanged behavior (the regression contract)

- Every stage, contract file (`run.json`, `queue.json`, `status.md`,
  `orchestrate.log`), grading path and exit code of `evals-bg.sh` when it
  is the only instrument.
- `mutate.py`'s verdicts, output format and exit codes when it is the
  only instrument; every existing `check_mutate.py` check.
- `grade.py` stays lock-free (clarified below).
- The dashboard is a reader and takes no lock.

## Boundary

**In:** the lock primitive in `tools/proc.py`; acquisition in
`evals-bg.sh` (before any write, released on every exit path) and in
`mutate.py` (around the whole pass); the narrowed invocation-start
cleanup; red-first checks in `evals/check_mutate.py`.

**Out:** `grade.py` and `setup_workspace.py` (operator-driven, quick;
a lock there is a separate decision); any cross-workspace lock (two
workspaces never share state); changing what the runner wipes per
scenario at `run_scenario` start.

## Requirements

- **R-731** — WHEN an instrument acquires the workspace lock THEN it
  SHALL create `<ws>/.lock` atomically (create-exclusive), recording the
  instrument name, its pid, its start time and its argv.
- **R-732** — WHILE `<ws>/.lock` names a live process, WHEN a second
  instrument (runner or mutation pass) starts against that workspace THEN
  it SHALL exit non-zero before writing any workspace file, printing the
  holder's instrument, pid and start time.
- **R-733** — WHEN `<ws>/.lock` names a process that no longer exists
  THEN the acquiring instrument SHALL take the lock over, printing one
  line that names the dead holder — never silently.
- **R-734** — `evals-bg.sh` SHALL hold the lock from before its first
  workspace write (`run.json`) until it exits by any path — normal end,
  a stage failure, or a signal — releasing it at that exit.
- **R-735** — `mutate.py` SHALL hold the lock for the whole pass and
  release it on exit, including an exception.
- **R-736** — WHEN a full `evals-bg.sh` run starts THEN its
  invocation-start cleanup SHALL remove `grading.json`,
  `grading_idempotency.json` and `grade.txt` only under the scenario
  directories that invocation will run (`--skip-smoke` leaves `upgrade`'s
  records in place).
- **R-737** — Every new check SHALL be shown red against the unchanged
  harness before it is shown green.

## The hurting case

GIVEN a workspace where `evals-bg.sh` is live mid-corpus, WHEN the
operator starts `python3 evals/mutate.py <ws>` (or a second runner
invocation), THEN the second instrument prints `workspace locked by
evals-bg (pid N, started T)` and exits non-zero, AND every `grading.json`
and every fixture byte are exactly as they were the instant before — the
live run does not notice. The case that hurts most: a stale lock from a
`kill -9`'d runner that silently blocks every future run — R-733 makes
takeover loud and automatic.

## Clarifications

### Session 2026-08-28

- **Q: stale lock — refuse, or take over?** → Take over automatically
  when the holder pid is dead, with one loud line; a live holder is
  always a refusal.
- **Q: does `grade.py` take the lock too?** → No — runner and
  `mutate.py` only, as the backlog sizes it; `grade.py` stays the quick
  operator regrade and is named out of scope.

## Converge — 2026-08-28

Judged against R-731–R-737, the boundary and the unchanged list. R-731:
`acquire_lock` creates `.lock` with `O_CREAT|O_EXCL`, record checked
field-by-field. R-732: live-holder refusal proven three ways — the
primitive, `mutate.py` as a subprocess (workspace holds only `.lock`
afterwards), `evals-bg.sh` as a subprocess (same). R-733: dead-pid
takeover proven in the primitive and live through the real runner (the
line lands on stderr and in `status.md`). R-734: the runner locks before
`run.json` (the provenance/cleanup block moved below the function
definitions so the lock and `scenario_dirs` precede every write);
EXIT/TERM/INT traps release; proven on the gate-failure path. R-735:
`try/finally` around `run_pass`. R-736: proven with `--skip-smoke`
against an unmaterialized workspace — `upgrade`'s record survives,
`restructure`'s is wiped. R-737: 5 discriminating FAILs shown before any
implementation (the other 15 new checks were coincidentally green on the
unchanged harness — non-zero exits for unrelated reasons — and are stated
as such, not counted as red). Unchanged list: the whole existing
`check_mutate.py` stays green; the runner's stages, contracts and exit
codes are untouched by construction (the block moved, not changed).
Verification: static, engine, hooks, opencode, anchors, sdd-lint,
check_mutate — all green. Residual, stated: `_pid_alive` on Windows goes
through `tasklist` and is unexecuted on this machine (BL-068's standing
residual class). Gaps: none (missing / partial / contradicts /
unrequested: none).

✅ Converged
