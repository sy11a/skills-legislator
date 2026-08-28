# Edition v24 — the plan package (BL-075)

One branch, one MR, one benchmark (`bl/075-edition-v24`). Tasks in
execution order; `[P]` marks file-disjoint tasks. Requirements trace to
`spec.md` (R-751…R-770).

## Research (decisions taken — pointers, not restatements)

- **Slice:** apply/verify + detect + Step-7 emitter; restructure emitter +
  fidelity → BL-076 (v25) — composition clarify, 2026-08-28.
- **The engine writes the owned layer:** ADR-0006 (accepted 2026-08-28),
  extending ADR-0003's narrowing sentence with a fourth clause.
- **Run record home:** outside the repo, at a path derived from the
  resolved root (R-751/R-752) — so the grader can re-derive it and a run
  leaves no untracked file. Alternatives rejected: `docs/ai/run.json`
  (breaks idempotency's zero-diff, or needs a scaffolded `.gitignore`
  line), a harness-supplied path (the prompt would restate law, POLICY §7).
- **Report lines are pinned facts, not prose:** `- \`path\` — <reason>`
  where the reason is a closed phrase per event class (BL-049: free prose
  is not a slot). The v23 reports' per-item rationale disappears by
  design.
- **Baseline (§5):** law and grader change together again; the reference
  is `evals/benchmarks/v23.md` (205/205, floor sonnet) on its own
  instrument; confounds named in the v24 benchmark per §9.
- **Model floor:** the judgement jobs are unchanged (Step 4/5, candidates)
  — floor expected to stay sonnet; measured, not assumed.

## Contracts

- `engine.py detect --skill <p> [--root <r>]` → JSON on stdout, exit 0/2/3.
- `engine.py apply --skill <p> --stacks <a,b> [--keep-add p::reason]*
  [--keep-remove p]* [--record <file>] [--root <r>]` → summary on stdout,
  record written, exit 0 / 3 (crash) / 4 (both entry documents real —
  the decision-gate stop, nothing written).
- `engine.py verify --skill <p> [--record <file>] [--root <r>]` → failure
  lines on stdout, exit 0 clean / 1 failures / 3.
- `engine.py report --skill <p> [--record <file>]
  [--model-findings <json>] [--root <r>]` → the report on stdout, exit 0
  / 2 usage / 3 (malformed findings included).
- Record shape and model-findings shape: spec.md.

## Tasks

1. Backlog rows BL-075 / BL-076, ADR-0006, this package. per R-759.
2. Red-first engine units in `evals/check_engine.py` for every row of the
   eval-design table's last line — shown FAIL against the v23 engine
   (unknown job → exit 2). per R-770, R-772.
3. `detect`: mode decision tree, edge case, stacks (subscribed +
   candidates), old ownedFiles reconstruction. per R-753.
4. The run record: path derivation, write, read, `pre` snapshot (Step-4
   targets parsed from `<skill>/SKILL.md`; entry-doc imports). per R-751,
   R-752.
5. `apply`: copies, ownedFiles, deletions + empty-dir cleanup, keep rules
   and refusals, manifest serializer, file model with the both-exist stop,
   crash code. per R-754, R-755, R-756, R-757, R-758, R-771, R-759.
6. `verify`: byte-diff with one re-copy, Step-4 presence, record `post`
   snapshot. per R-760, R-761.
7. `report`: skeleton, event lines, import deltas and wiring proposals,
   Keep list gating, candidates merge + `review` lines, Health via audit
   checks 1–6, stamp, byte-stability, malformed-findings loud exit.
   per R-762, R-763, R-764, R-765, R-766, R-767.
8. `[P]` SKILL.md Steps 1/3/6/7 rewired; the engine docstring's job list
   and `check_static`'s job-declaration list grow; `README` runbook line
   for the record. per R-768.
9. `[P]` Harness: ground rule drops "via Bash cp"; fresh-scaffold gets a
   report path (`outputs/scaffold-report.md`). per R-769.
10. Grader: the new asserts per the eval-design table, shown red against
    `/tmp/legislator-eval-v23` (no agents); `evals/mutations.py` entries
    (stamp-strip, line-strip, re-print mismatch by editing a Created
    line). per R-770, R-772.
11. VERSION 23 → 24; `docs/philosophy.md` Horizon unchanged (no item
    closes). per `.claude/rules/constitution-source.md`.
12. Benchmark cycle (agents — **fires only on the owner's explicit go**):
    corpus on sonnet, idempotency ×3, the mutation pass on the run's own
    workspace, `evals/benchmarks/v24.md` with confounds named.
13. Deliver to member #0 (refresh `docs/ai/**` from `skill/`, byte-verify —
    now `engine apply` on this repo), then the fleet sweep after
    merge+tag.
14. Case closure: converge, backlog flip, changelog, journal, OKF
    (glossary rows `run record`, `apply job`; log entry).
