# BL-043 — Generated baseline and the spec/plan linter (edition v22)

**Tier: 2 (full).** Blast radius: the constitution's engine (an owned
executable delivered to every fleet repository), `core/sdd.md`'s analyze gate,
`core/artifact-lifecycle.md`'s generated class, and the first machine-written
artifact the constitution has ever produced in a target repo. Novelty: high —
the `generated` ownership class has been declared and unpopulated since v20.

**Spec type: feature.** Edition v22, branch `bl/043-baseline-and-linter`;
BL-057 rides the same edition (its case file:
`docs/cases/BL-057-quoted-tokens/`).

**Carried from:** BL-033's out-of-scope section
(`docs/superpowers/specs/2026-08-23-okf-v2-anchors-design.md` §9) and
`docs/philosophy.md` §Horizon, whose "Generation at full strength" item this
edition removes.

## Intent

Close the gap the truth-bonding principle still has: anchoring (v20) verifies
what documents *name*; nothing verifies what they *promise*. The baseline is
the generated answer to "what must the system do today" — the R-NNN
requirement register joined against the tests that exercise it — and the
linter is the mechanical half of `core/sdd.md`'s analyze gate, which today
says "run the mechanical passes with the engine when available" while no
engine job exists to run.

## Boundary

**In scope:**

- Two new engine jobs in `skill/assets/engine/engine.py`: `sdd-lint`
  (read-only) and `baseline` (writes exactly one declared file).
- `core/sdd.md`: the analyze gate names the concrete job.
- `core/artifact-lifecycle.md`: the generated class gains its first member.
- SKILL.md: whatever the modes must know about `docs/ai/baseline.md`
  (see R-207).
- Repository bookkeeping: ontology, glossary, philosophy §Horizon,
  backlog, OKF log, CHANGELOG.
- The eval additions for all of the above, designed below.

**Out of scope:**

- **The fleet-obs registry work** (a `generated` content-type and its
  gold-panel exclusion) — a sibling-repo change, done in that repository
  after v22 merges; this case carries the reference row per `core/sdd.md`'s
  cross-repo rule.
- **The fleet sweep** — after merge, per the release runbook.
- **Auto-generation during legislator runs** — no mode runs the generator
  unprompted; the baseline is regenerated on demand by its owner (R-207).

## Requirements

### The annotation contract

R-201 — WHEN a test source file contains the literal marker `per R-NNN`,
THEN the engine SHALL treat that file as an annotated test for requirement
R-NNN. A test source file is a file under the source roots whose path
contains `test` (case-insensitive) — covering `*.Tests/` and `*Tests.cs`
(dotnet), `*.test.ts`/`*.spec.ts`, `test_*.py` — with the engine's existing
build-dir and size exclusions.

R-202 — The requirement register SHALL be the set of `R-NNN` ids defined by
EARS definition lines in `docs/cases/*/spec.md` — every case, whatever its
status. *(Amended at converge: "definition line" replaced "### heading". The
first dry-run against this repository's real case tree found three definition
forms in use — `### R-NNN — t`, `- **R-NNN** — t`, bare `R-NNN — t` — and the
em-dash after the id is the signature all three share; the heading-only form
would have dropped BL-034's and BL-051's registers entirely.)* The baseline is the R↔tests mapping, not a case
tracker: an open case's requirement without a test is honestly visible in
the uncovered list, and no status parsing keeps the generator
deterministic.

### The baseline job

R-203 — WHEN `python3 docs/ai/engine.py baseline` runs, THEN it SHALL write
`docs/ai/baseline.md` and nothing else: one table row per requirement —
id, its title, the annotated test files carrying its marker — plus an
explicit list of requirements no test carries. The job lives in the engine
as its third job; the engine's write guarantee is restated as "the check
jobs write nothing; `baseline` writes exactly its declared target and
nothing else" (ADR this decision).

R-204 — The generated file SHALL be deterministic: same repo state, same
bytes — sorted rows, no timestamps — so a second run over an unchanged repo
is a zero diff (the idempotency property, one level down).

R-205 — The generated file SHALL declare itself: a do-not-edit header naming
its generator and its sources.

### The linter job

R-206 — WHEN `python3 docs/ai/engine.py sdd-lint` runs, THEN it SHALL
report, read-only, in the engine's finding format: **dangling** `per R-NNN`
references (the id resolves to no EARS definition in *any* case's spec),
**uncovered** requirements (an EARS definition in a case that has a
`plan.md`, with no `per R-NNN` task tracing it — a case without a plan is
not lint, tier 0/1 is lawful), and **unresolved placeholders** (`{{TOKEN}}`
outside inline code and fences, in case files — the same quotation rule
BL-057 gives audit check 2). *(Amended at converge, three ways the dry-run
forced: dangling is judged against every case's definitions, because a rider
lawfully traces the case it rides with — this case's own plan traces
BL-057's R-101; `per R-001, R-002` list references count for every id
listed, because the first real plan written under this law used the list
form; and a case carrying "✅ Converged" is history
(`core/artifact-lifecycle.md`) and is skipped entirely — without that,
BL-034 and BL-051 yield 25 retroactive findings the lint's owner cannot act
on.)*

### Law and lifecycle

R-207 — `core/artifact-lifecycle.md`'s generated class SHALL name
`docs/ai/baseline.md` as its first member, and SKILL.md's audit SHALL treat
it as generated (do-not-edit, never keepable, not owned) — no new audit
check in this edition.

R-208 — `core/sdd.md`'s analyze gate SHALL name the concrete command
(`python3 docs/ai/engine.py sdd-lint`), replacing "with the engine when
available" by the same absent-python fallback wording checks 15/17 use.

### The engine contract

R-209 — Both jobs SHALL keep the engine's exit contract: 0 clean, 1
findings, 2 usage, 3 crash — and `check_engine.py` SHALL exercise both jobs
red and green without an agent.

## Hurting case

**GIVEN** a legislated repository whose case spec defines R-001..R-003, whose
plan traces R-001 and R-002 `per R-NNN`, and whose test tree carries the
marker for R-001 only,
**WHEN** `sdd-lint` and `baseline` run,
**THEN** `sdd-lint` reports R-003 uncovered and exits 1, and `baseline.md`
lists R-001 with its test file and names R-002 and R-003 as carrying no test
— and a hand edit to `baseline.md` is destroyed byte-for-byte by the next
run, which is the generated class's defining property, observed.

## Eval design (POLICY §3, written before the change)

1. **Engine rung (no agent):** `check_engine.py` gains cases for both jobs —
   a synthetic case tree with a dangling reference, an uncovered
   requirement, a quoted (exempt) token and a bare (reported) one; a
   baseline run asserted byte-deterministic across two invocations, and
   destroying a hand edit. **Red first:** these tests run against the v21
   engine (no such jobs) before the jobs exist.
2. **Corpus:** `case-practice` — the grader additionally runs the
   *delivered* engine's `sdd-lint` against the agent-written case and
   asserts exit 0 (artifact: the repo tree). Red against the v21 law by
   construction: the delivered engine has no such job, exit 2.
   `owned_files_verbatim` already proves delivery of the new engine in
   every scenario; no new assert needed there.
3. **Negative control:** the quoted-token exemption inside sdd-lint's
   placeholder pass (shared with BL-057's fixture direction).
4. **Derived or restated?** The linter derives everything from case files at
   run time; `check_engine.py` builds its fixtures inline. No restated
   contract.
5. **A red would mean:** engine-rung red = the job's logic (grader class is
   impossible — no grader involved); case-practice red = law class (sdd.md
   or SKILL.md fails to deliver/describe the job) or model class under §1's
   burden of proof.

## Clarifications

### Session 2026-08-25

- **Q1 — annotation format?** → The literal marker `per R-NNN` in a test
  source file. Language-agnostic, zero infrastructure, and the same lexicon
  tasks already use in plans. (Alternatives — framework attributes, test-name
  conventions — rejected: per-stack machinery, or noise in test names.)
- **Q2 — where does the generator live?** → The engine's third job. One owned
  executable per fleet repo; delivery and byte-guarding already exist; the
  read-only guarantee is narrowed, not dropped: check jobs write nothing,
  `baseline` writes exactly its declared target. Recorded as ADR-0003.
- **Q3 — register scope?** → All `docs/cases/*/spec.md`, whatever the case
  status. The baseline maps R↔tests; coverage gaps are data, not shame, and
  status parsing would trade determinism for a distinction the uncovered
  list already expresses.
- **Q4 — fleet-obs sequencing?** → After v22 merges and the fleet sweep
  delivers it; this case carries the reference row only.

## Converge — 2026-08-26

Judged against every promise: R-201..R-209, the plan's tasks, ADR-0003's
consequences, and the constitutional MUSTs the edition touches. Findings:

1. **per R-202 (contradicts, spec side).** The spec pinned `###` headings;
   the repository's own cases use three definition forms. The
   implementation follows the em-dash signature; the spec text is amended
   above with the reasoning. Grader-visible: `check_engine.py`'s
   `sdd_lint_accepts_three_definition_forms_and_list_refs`.
2. **per R-206 (partial, spec side).** Dangling scope (same-case → any-case),
   list references, and the converged-case skip were all forced by the
   dry-run against this repo and pinned by checks before the engine
   existed; the spec text is amended above. All three were committed red
   first (`aa50794`).
3. **per R-203/R-204 (unrequested, accepted).** The baseline is keyed by
   (case, id), not by id: the dry-run found R-001 defined in three cases —
   ids are unique within a case only, and a test's `per R-NNN` marker maps
   into every case defining that id, ambiguity displayed rather than
   silently resolved. Recorded in the engine's docstring; no spec line
   demanded it and none forbids it.
4. **per R-207 (complete).** Generated member named in
   `core/artifact-lifecycle.md`; the keep gate refuses `docs/ai/baseline.md`;
   no new audit check, as specified.
5. **per R-208 (complete).** `core/sdd.md`'s analyze gate names the command,
   with the python3-absent fallback wording.
6. **per R-209 (complete).** Exit contract exercised red and green by
   `check_engine.py` — 62 checks, all green on the final generation.

Measured close: corpus 201/201 in one pass on `v22-e277e4c`, idempotency ×3
zero diff, model floor sonnet (`evals/benchmarks/v22.md`). One law defect
found and closed in-cycle (fix round 1 — restructure's ask-the-user token
derivation); one grader defect found by the baseline and closed before the
corpus ran (the whole-tree token scan). Self-delivery byte-verified 13/13;
the delivered engine's own `sdd-lint` ran against this repository and caught
one bare token in this edition's own evidence file — fixed, now clean — and
`baseline` generated `docs/ai/baseline.md`: 49 requirements, all honestly
uncovered (this repository carries no annotated tests yet).

Cross-repo remainder (reference row, per the sdd cross-repo rule): fleet-obs
gains a `generated` content-type and its gold-panel exclusion — after this
edition merges and the sweep delivers v22 there.

✅ Converged.
