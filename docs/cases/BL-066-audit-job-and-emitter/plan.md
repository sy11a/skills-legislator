# Edition v23 — the plan package (BL-065 + BL-066 + the parked riders)

One branch, one MR, one benchmark (`bl/065-066-edition-v23`). Tasks in
execution order; `[P]` marks file-disjoint tasks. Riders trace to their
origin cases; requirements to the two v23 specs.

## Research (decisions already taken — pointers, not restatements)

- Emitter scope: audit slice only — composition clarify, 2026-08-26
  (Step-7/restructure emitters pair with engine apply/verify in v24).
- Model-findings channel: a JSON file the model writes, the engine merges
  — BL-066 spec, "The model-findings channel".
- D3 form: scaffolded `.gitattributes` (create-if-absent) — composition
  clarify, 2026-08-26.
- Baseline (§5): law **and** grader change in v23, so the reference is
  the v22 benchmark on its own instrument (`evals/benchmarks/v22.md`,
  201/201, floor sonnet) — confound named per §9, no extra baseline run.

## Tasks

1. `[P]` Engine encoding sweep — `encoding="utf-8"` on every engine
   read/write; check_engine stays green byte-identically. per R-666.
2. Fail-loud git absence: `okf-debt` (and the new audit job) exit outside
   {0,1} with stderr when git is unavailable; red-first unit tests
   reproducing BL-069's M4 repo. per R-665.
3. BL-065 lints, red-first pair per lint in `evals/check_engine.py`, then
   the implementations in the engine's `sdd-lint`. per R-651…R-659.
4. Member #0 self-lint: run the grown `sdd-lint` on this repository and
   repair every real finding it names (live-fixture rule in the BL-065
   hurting case). per R-651…R-659.
5. Law text: `core/sdd.md` names the grown lint families. per R-660.
6. Engine audit job — checks 1–10, 13, 14, 16 implemented with a
   red-first synthetic fixture per check in check_engine; 15/17 folded;
   writes-nothing test. per R-661, R-667.
7. The emitter: report printing (header, severity sections, clean-checks
   line, stamp) + the model-findings merge, byte-stable; defensive
   validation fails loud. per R-661, R-662, R-663, R-669.
8. SKILL.md Audit section rewrite: engine invocation + model-findings
   workflow; check definitions, slug sentence and severity list stay
   parsable — `check_static` and `selftest:derivation` prove it.
   per R-664.
9. D3: `gitattributes.tpl` + Step 4 row (create-if-absent); the Step-4
   derivation auto-extends the grader's scaffold expectations (red
   against v22 by construction); mutation entry for the new artifact's
   assert. per BL-068 D3, R-668.
10. New corpus asserts per BL-066's eval-design table, shown red against
    the v22 workspace artifacts (no agents — the stamps don't exist yet),
    plus their `evals/mutations.py` entries. per R-668.
11. E1/E2: portable process control for `tools/evals-bg.sh` — python
    spawn/stop helpers (`start_new_session`), `stat -c` replaced; the v23
    benchmark run itself is the live verification. per BL-068 E1/E2.
12. VERSION 22 → 23.
13. Benchmark cycle (agents — **fires only on the owner's explicit go**):
    corpus on the intended floor model, idempotency ×3, the mutation
    pass on the run's own workspace, `evals/benchmarks/v23.md` with
    confounds named (grader+law change vs v22). per POLICY §§1–9.
14. Deliver to member #0 (refresh `docs/ai/**` from `skill/`,
    byte-verify), then the fleet sweep after merge+tag — the release
    runbook.
15. Case closure: converge BL-065 and BL-066, flip backlog entries
    (BL-065, BL-066, BL-070's parked half, BL-049's emitter pointer),
    changelog, journal, OKF (glossary rows for the audit job /
    model-findings channel; log entry).
