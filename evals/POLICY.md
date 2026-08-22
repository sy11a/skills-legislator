# Eval Policy — the bar an edition must clear

`README.md` in this directory is the **how**: what an eval is, how to
materialize a workspace, how to run and grade one. This file is the
**when, against what, and at what bar** — the policy an edition is held to.
Where the two disagree, this file wins.

Written 2026-08-22, out of the v17 cycle. Every rule below has a scar
behind it; the scars are named so a future reader can tell a principle from
a preference.

## 1. Passing the suite is a deliverable, not a check

An edition is **not releasable below 100%** — the full scenario corpus plus
idempotency ×3, all within one law generation. There is no "known red", no
waiver, no "we'll fix it next edition". A red is one of exactly four
things, and all four are the edition's problem:

| Class | Meaning | Fix lands in |
|---|---|---|
| **law** | the skill's law is wrong, ambiguous, or self-contradictory | `skill/**` |
| **grader** | the assert measures the wrong thing, or nothing | `evals/grade.py` |
| **harness** | the runner or its prompt made the outcome impossible | `tools/`, `evals/setup_workspace.py` |
| **model** | the law is right and the model could not execute it | the model floor (§4) |

Only the fourth class is resolved by changing *nothing* — and then only by
raising the recorded model floor, never by lowering the bar.

## 2. Classify every red before fixing it

Write the classification down before touching code. A red diagnosed from
its message alone is a guess: in v17 the assert `conflict_not_auto_resolved`
reported "conflict line gone — auto-resolved without the user" while the
line was intact on disk; the real defect was a missing v14 canonicalization,
and the confident wrong message would have sent the fix to the wrong file.
Read the artifact the assert read, then classify.

## 3. Design the evals **before** the change

The assert for a change is written while the change is being designed, not
after it fails. Two reasons, both paid for in v17:

- **An unstated law is an unmeasurable one.** Writing the assert forces the
  law into checkable terms. `core/sdd.md`'s "ship the hurting case" was
  checkable; the restructure `fix`/`decision` boundary was not, and it cost
  five law generations of benchmark-driven repair.
- **A missing assert is indistinguishable from a passing one.** v17 shipped
  a `ghost_import_fixed` that compared against an empty string whenever
  `AGENTS.md` was absent, so a dangling import survived every run of every
  version without a single red. `grade_restructure` never called
  `no_unresolved_tokens`, so a Critical unresolved `{{TOKEN}}` scored 100%.
  Both were found by accident, months late.

Per-edition eval design belongs in the edition's spec under
`docs/superpowers/specs/`, alongside the law it measures — not in a
separate document that can drift from it.

## 4. Harness and model are recorded; the model floor is a published property

Every run records **provider, provider version, and model**; `run.json`,
`grading.json` and `grade-history.jsonl` carry them, and the dashboard shows
them per run. Two runs are comparable only within one (provider, model)
pair — a harness switch is a **larger** confound than a model switch:
different system prompt, different tool set, different edit semantics.

Choose the **cheapest model that reaches 100%**, and record it as the
edition's **model floor**: the guarantee is "this edition passes at 100% on
*at least* this model", not "on some model somewhere".

What v17 established, on the same law generation:

| Harness | Model | Result |
|---|---|---|
| opencode 1.18.21 | `zai-coding-plan/glm-5-turbo` | usable; needed the resume ladder constantly (provider stream drops) |
| Claude Code 2.1.239 | `haiku` (4.5) | clean on mechanical modes (upgrade, drop-stack, scaffold, migration); **short** on judgement-heavy ones — restructure 23/32, audit 32/37 |
| Claude Code 2.1.239 | `sonnet` (5) | **100%** — the v17 floor |

The split is not noise and it is worth expecting again: copying bytes,
migrating a manifest key, serializing a keep list are mechanical and cheap
models do them exactly. Deciding whether a finding is a `[fix]` or a
`[decision]`, or whether a project line is fleet-generalizable, is judgement
— and that is where a cheap model silently substitutes a plausible answer.

Run the corpus on the cheap model first when cost matters, but re-run every
red on the floor candidate before classifying it: a red under a model that
cannot do the work carries no information about the law.

## 5. Baseline first, then change the law

On a new edition's branch, run the suite **against the previous version's
law, on the harness you intend to use**, before applying the change. The
baseline costs one corpus run and buys the only thing that makes a later red
readable: a reference measured on the same instrument.

v17 changed law, harness and model in the same cycle and paid for it — every
red was ambiguous until each variable was isolated one at a time, which took
three full runs instead of one.

## 6. Idempotency is not optional, and it is not a formality

A second run over an unchanged repo must produce a zero diff. In v17 the
idempotency stage found **three** defects the whole corpus had passed over:

- a mis-sorted `ownedFiles` list (run 2 corrected run 1 — the "diff" was the
  repair, not the damage);
- a check-7 orphan with **two lawful outcomes**, which made a previously
  **open `[decision]` silently reclassify into an applied write** on the next
  run — the decision gate leaking through a nondeterministic classification;
- an unresolved `{{PROJECT_OVERVIEW}}` left by run 1 and filled by run 2,
  invisible because `grade_restructure` never asserted it.

The general lesson: **a second run that writes is a first run that left work
undone, or a law that permits two outcomes.** Both are defects. Any action
whose classification depends on judgement ("this file looks dead") rather
than on repo state cannot be idempotent — pin it to state.

## 7. The prompt never restates the law

Eval prompts carry **harness constraints only** — where to write a report,
that nothing may be committed, that confirmations are pre-approved. They
never restate a rule the skill already carries, and never name deliverables,
modes, or reasons (`README.md`, "Trigger discipline").

v17's cost for breaking this: the runner's ground rules quoted SKILL.md's
entry-document clause almost verbatim. The law's own phrasing was
overstated — one mode's constraint written as a property of the file — so
the harness ended up **forbidding exactly what it was testing**, and four
asserts across two scenarios went red for obeying the prompt. Two model
families had hidden the defect for months by ignoring the ground rule; the
third obeyed it and exposed it.

A prompt that repeats the law inherits every flaw in the law's phrasing and
adds a second place to fix.

## 8. One fact, one place

Where the same fact is derived twice, the two copies diverge — it is a
question of when, not whether. v17 hit it four times: an assert comparing
law check *titles* against fixture *slugs*; a grader demanding a migrated
rule stay in `AGENTS.md` while its sibling demanded the opposite; the
dashboard computing scenario state in two blocks with different rules; and
the entry-document rule stated in four places, one of them wrong.

The standing countermeasure is **derivation**: `grade.py` parses its
expectations out of the skill source (Step 4's table, the audit check list,
`restructure.md` §2's action set, the pinned check slugs) and
`selftest:derivation` asserts those derivations stay alive. Prefer deriving
over restating; when you must restate, make one copy authoritative and the
rest pointers to it. See BL-038 for the general form.

## 9. Recording the result

Each edition gets `benchmarks/v<N>.md`, and it records:

- pass rate per scenario, per (harness, model) pair — never mixed into one
  column;
- the **model floor** for the edition;
- harness and provider versions;
- idempotency outcomes;
- **confounds**, named explicitly — a model switch, a harness switch, a
  prompt change all make numbers incomparable to the previous edition, and a
  reader who is not told will compare them anyway;
- the defect chronicle: what went red, its class (§1), and what it forced.

The chronicle is the point. A pass rate says the edition is shippable; the
chronicle is what a future reader learns from.
