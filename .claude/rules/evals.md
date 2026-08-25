# Testing is mandatory — no change to `skill/` is done until verified

**Read `evals/POLICY.md` before planning any change to `skill/`.** It is the
authoritative bar: evals are a deliverable, not a check; the eval for a
change is designed *before* the change; an edition ships only at 100% on the
corpus plus idempotency ×3; every red is classified (law / grader / harness /
model) before it is fixed; and each edition records its **model floor** — the
cheapest model at which it reaches 100%. The one rule not to skip: **a new
assert must be shown RED against the unchanged law before it is shown
green** — an assert that is green before the change is measuring nothing,
and reading it will not tell you that.

Editions are tagged at merge (`v17`, `v18`, …). The tag carries a law and
its grader together, which is what makes POLICY's baseline run a two-line
`git worktree` operation instead of an archaeology exercise.

Any edit under `skill/` (SKILL.md, `assets/rules/**`, `assets/templates/**`,
`references/**`, VERSION) must pass, before being reported as complete:

1. **Every commit:** `python3 evals/check_static.py` and `python3 evals/check_engine.py` — seconds, no agent.
2. **Every behavioral change** (VERSION bump, SKILL.md procedure edit, rule
   content change, template change): the full e2e benchmark per
   `evals/README.md` — materialize a workspace, run the scenario agents,
   grade, run the idempotency pass, and record the results in
   `evals/benchmarks/v<N>.md` compared against the previous version's file.
   A pass-rate drop or new idempotency diff is a regression: investigate and
   fix (or explicitly surface it to the user) — never commit over it
   silently.

Documentation-only edits (README, `docs/**`, `evals/**` itself) need neither.

**One exception rides every edition:** `docs/philosophy.md` §Horizon lists what
is designed but not built. The edition that closes one of those cases removes
its item in the same cycle — `check_static.py` fails while a Horizon entry
names a case the backlog reports as closed. The manifest must never claim a
gap the system no longer has.
