# BL-051 — plan

Branch `bl/034-self-legislation` (batched; one MR as edition v21). Every task
traces to a requirement in `spec.md`.

## Phase 1 — evals first, red before green

**T-01 — engine asserts.** *(per R-001, R-003, R-005, R-006)* `[P]`
Four asserts plus five controls in `evals/check_engine.py`; run against the
unchanged v20 engine and confirm red.
**Acceptance:** each new assert red, each control green. ✅ 4 red, 5 green.

**T-02 — law-text asserts.** *(per R-007, R-008, R-009)* `[P]`
Six asserts in `evals/check_static.py`, parsed out of `SKILL.md` rather than
restated. **Acceptance:** all six red, both parseability controls green.
✅ 6 red, 2 green.

## Phase 2 — the fixes

**T-03 — engine.** *(per R-001, R-003, R-005, R-006)*
`status: removed` leaves the anchored class; `BUILD_DIRS` excluded at any
depth; a top-level handler exits 3 on an unhandled exception.
**Acceptance:** T-01's asserts green, controls still green. ✅

**T-04 — law text.** *(per R-002, R-007, R-008, R-009)*
`core/okf.md` states the removed-status exemption beside the class it belongs
to; checks 15 and 17 gain the `python3`-absent branch and the exit-code rule;
both keep refusals name the whole owned set. **Acceptance:** T-02 green. ✅

**T-05 — VERSION.** *(per R-011)* `skill/VERSION` → 21. ✅

## Phase 3 — the unmeasured branch

**T-06 — fixture and scenario.** *(per R-010)*
`materialize_audit_engine_absent` builds a v19 layer with the OKF bundle and no
engine; scenario `audit-engine-absent` wired into `evals.json`, `grade.py` and
`tools/evals-bg.sh`. **Acceptance:** the fixture materializes in the intended
state and the grader is red for want of a report. ✅

## Phase 4 — measurement

**T-07 — baseline.** *(per R-011)* Corpus against v20's law in a worktree at
tag `v20`, on `claude`/`sonnet`. Running.

**T-08 — the v21 corpus.** *(per R-011)* Fresh workspace, same harness and
model, full corpus plus idempotency ×3. 100% or the edition does not ship.

**T-09 — deliver to self.** *(per `README.md` release step 4)*
Run `/legislator` on this repo so the owned layer moves v20 → v21, byte-verify,
commit. This is the first exercise of the step BL-034 introduced.

**T-10 — record.** *(per R-011)* `evals/benchmarks/v21.md` against the
baseline: pass rate per scenario, model floor, idempotency, confounds named
(the corpus gained a scenario), and the defect chronicle.

## Phase 5 — gates

**T-11 — analyze.** Coverage R↔task, dangling ids, unresolved placeholders.

**T-12 — converge.** Judge against every promise in `spec.md`; append findings
here; `summary.md`; close only on "✅ Converged".
