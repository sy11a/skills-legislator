# BL-051 — v20 final-review residue (edition v21)

**Tier: 2 (full).** Blast radius: the engine every legislated repository now
carries, plus the audit checks that read it and the verification rung that
gates "done" on it. Novelty: three of the five are fail-open paths, and a
fail-open defect cannot be found by the thing it disables.

**Spec type: bugfix.** Current / expected / **unchanged** is stated per item;
the unchanged column is the regression contract.

**Status:** approved 2026-08-24 (batched into `bl/034-self-legislation`, one MR
as edition v21). Baseline against v20 measured before any law change, per
`evals/POLICY.md` §5.

## Purpose

Five findings from the whole-branch review that closed BL-033, none acted on in
v20 so that edition's benchmark record stayed valid against the law generation
it measured. None is a regression against v19 — each is a gap the anchor engine
surfaces once it exists. Three are fail-open: the audit reports clean in
exactly the situations it was built to catch.

The engine is now in nine repositories. Every edition delivered before these
are fixed widens the population running a check that cannot fail.

## Boundary

**In scope** — the five items below: the law text in `core/okf.md`,
`core/verification.md` and `SKILL.md`'s audit section; the engine at
`skill/assets/engine/engine.py`; the asserts that prove each; the fixture for
item 5b; `skill/VERSION` → 21; the full e2e benchmark recorded in
`evals/benchmarks/v21.md`.

**Out of scope**

- **BL-057** (audit check 2 cannot tell a quoted token from an unfilled one).
  Found by BL-034 the same day and genuinely in `SKILL.md`'s audit section,
  but it is a different defect with its own fixture work; folding it in would
  make one benchmark measure two unrelated law changes.
- **BL-055 / BL-056** (`fleet.sh`). Tooling, no VERSION, no benchmark.
- **Anything the baseline turns up that is not one of the five.** A red that is
  not this edition's is classified and filed, not fixed here.

## Item 1 — `status: removed` documents can never be clean, and the rung is global

**Current.** `core/okf.md`'s checklist instructs the owner to flip a removed
concept's document to `status: removed` and keep it. The anchored class covers
"every concept document" with no exemption, so that document's references — now
deliberately naming code that is gone — are anchor findings. `core/verification.md`
makes `engine.py anchors` gate "done" for **any** task in that repository, so a
document behaving exactly as the checklist demands wedges unrelated work.
Restructure cannot clear it: check-15 findings are routed to `## For the team:`
by design.

**Expected.** A document whose front matter carries `status: removed` is
outside the anchored class: both engine jobs skip it, and `core/okf.md` states
the exemption where it defines the class.

**Unchanged.** Every other status stays anchored. `glossary.md` and `log.md`
stay the human class. A `status: removed` document is still scanned by every
check that is not about anchoring.

## Item 2 — nested build output is not excluded, only top-level

**Current.** `IGNORED_DIRS` filters **top-level** source roots. Inside a root,
`resolve_symbols()` walks `root.rglob("*")` and skips only hidden path parts,
so a stale `src/App/obj/Debug/App.dll` still containing a removed symbol makes
that symbol resolve. The check silently misses the rot it exists to find, and a
clean CI clone and a developer clone disagree about a gate on "done".

**Expected.** A path carrying a build-output directory (`bin`, `obj`,
`node_modules`, `dist`) at **any** depth is skipped during symbol resolution.

**Unchanged.** Top-level root selection, including `docs` staying out of the
source roots. A symbol genuinely present in source still resolves. Path-anchors
are unaffected — a path that exists, exists.

## Item 3 — a crashing engine audits clean

**Current.** An unhandled exception propagates out of a job: Python prints a
traceback to stderr and exits 1. Exit 1 is the engine's "findings printed to
stdout" code, and audit checks 15 and 17 read **stdout lines only** — an empty
stdout reads as "no findings". The verification rung fails closed; the audit
fails open on the same engine.

**Expected.** The engine catches unhandled exceptions at top level, prints the
error to stderr, and exits **3** — a code distinct from clean, findings and
usage. `SKILL.md` checks 15 and 17 state that an exit outside `{0, 1}` is a
**check failure**, reported as a finding, never as a clean check.

**Unchanged.** Exit 0 = clean, 1 = findings, 2 = usage. Both jobs stay
read-only. The rung's own behaviour.

## Item 4 — checks 15/17 have no `python3`-absent branch

**Current.** `core/verification.md` gained the branch in v20 ("Where `python3`
is absent the rung cannot run at all — that is a gap to close, never a licence
to report done unverified"). Checks 15 and 17 carry no counterpart, so the
audit's behaviour on such a machine is undefined — and undefined, on this
evidence, means clean.

**Expected.** Both checks carry the branch, matching the rung's meaning: where
`python3` is absent the check cannot run and says so; it is never reported
clean.

**Unchanged.** The rung's wording in `core/verification.md`; the checks'
skip-conditions for a missing engine or a missing bundle.

## Item 5a — the `keep` refusal names less than the owned set

**Current.** `SKILL.md` Step 3 item 6 refuses a keep request for "an owned file
under `docs/ai/rules/`", and Step 7's Keep-list section repeats that phrasing.
Since v20 the owned set also holds `docs/ai/engine.py` and the root
`opencode.json`, so both are keep-listable — putting the kept-paths row
(`link-only`) and the owned-law row (`replace`) of the file-authority table in
direct conflict.

**Expected.** Both places name the whole owned set, so no owned file can be
keep-listed.

**Unchanged.** Keep semantics for project-owned paths; the dedupe rule; the
refusal-reporting requirement.

## Item 5b — check 15's engine-absent branch has no fixture

**Current.** Check 15 has a branch for "bundle present, engine absent → Info".
Nothing in the corpus exercises it, so it is law with no measurement.

**Expected.** A fixture puts a repository in exactly that state and an assert
reads the resulting Info line.

**Unchanged.** The branch's own wording and severity.

## Requirements

- **R-001** — WHEN an OKF document's front matter carries `status: removed`,
  THEN `engine.py anchors` and `engine.py okf-debt` SHALL produce no finding
  for that document.
- **R-002** — `core/okf.md` SHALL state the `status: removed` exemption where
  it defines the anchored class.
- **R-003** — WHEN a symbol occurs only under a `bin`, `obj`, `node_modules` or
  `dist` directory at any depth, THEN `engine.py anchors` SHALL report it
  unresolved.
- **R-004** — WHEN a symbol occurs in a source file outside those directories,
  THEN `engine.py anchors` SHALL report it resolved.
- **R-005** — WHEN a job raises an unhandled exception, THEN the engine SHALL
  exit 3 and print the error to stderr.
- **R-006** — The engine SHALL exit 0 with no findings, 1 with findings, and 2
  on a usage error.
- **R-007** — `SKILL.md` checks 15 and 17 SHALL state that an engine exit
  outside `{0, 1}` is a check failure reported as a finding.
- **R-008** — `SKILL.md` checks 15 and 17 SHALL each carry a `python3`-absent
  branch whose meaning matches `core/verification.md`'s rung.
- **R-009** — `SKILL.md` Step 3 item 6 and Step 7's Keep-list section SHALL
  name the whole owned set, so no owned file is keep-listable.
- **R-010** — A corpus fixture SHALL place a repository in the "bundle present,
  engine absent" state, and an assert SHALL read check 15's Info line for it.
- **R-011** — `skill/VERSION` SHALL read 21, and `evals/benchmarks/v21.md`
  SHALL record the result against the v20 baseline measured on the same
  harness and model.

## The hurting case

**H-1 — a repository following the checklist must not wedge.**

> **GIVEN** a legislated repository whose `docs/okf/payments.md` was flipped to
> `status: removed` exactly as `core/okf.md`'s checklist instructs, and whose
> backticked references therefore name deleted code,
> **WHEN** a developer finishes an unrelated task and runs the verification
> rung,
> **THEN** `engine.py anchors` exits 0, so "done" is not blocked by a document
> doing what the law told it to do.

**H-2 — a broken engine must never read as a clean audit.**

> **GIVEN** an engine that raises on this repository (a malformed document, a
> permission error, any unhandled path),
> **WHEN** audit check 15 runs,
> **THEN** the audit reports a check failure naming the exit code — never
> "clean", and never silence.

Both are observable by a tester who never read the code: build the state, run
the command, read the exit code and the report.

## Eval design (`POLICY.md` §3)

Written before the change, as the policy requires. Every assert below is to be
shown **RED against the unchanged v20 law** before it is shown green.

### Items 1, 2, 3 — `evals/check_engine.py`, no agent

These are engine behaviours with deterministic inputs, so the cheap suite
carries them. Its `make_repo(docs, sources)` helper materializes the exact
`<repo>/docs/ai/engine.py` shape each case needs.

| # | assert | reads | planted defect | negative control | a red means |
|---|---|---|---|---|---|
| R-001 | `removed_doc_not_anchored` | engine stdout + exit for `anchors` | an OKF doc with `status: removed` backticking `src/Gone.cs`, which does not exist | a sibling doc with `status: implemented` backticking the same missing path **must** still report | law (the class definition) |
| R-001 | `removed_doc_no_debt` | engine stdout for `okf-debt` | same doc, with a tracked source committed 60 days after it | the `implemented` sibling must still report debt | law |
| R-003 | `nested_build_output_ignored` | engine stdout for `anchors` | symbol `PaymentProcessor` present **only** in `src/App/obj/Debug/App.js` | — | law (engine) |
| R-004 | `real_source_still_resolves` | engine stdout for `anchors` | symbol present in `src/App/PaymentProcessor.cs` | this is the control for the row above: it must stay silent | law (engine) |
| R-005 | `crash_exits_distinctly` | engine **exit code** and stderr | a document whose read raises (mode `000`), or a monkeypatched job | exit must not be 0, 1 or 2 | law (engine) |
| R-006 | `exit_codes_unchanged` | exit codes for clean / findings / bad-job | — | — | regression contract |

`crash_exits_distinctly` is the delicate one: it must provoke a *genuine*
unhandled exception rather than assert on a code path written to be tested. The
plan uses an unreadable document, which reaches `read_text` inside
`job_anchors` and is not currently caught.

### Items 4, 5a — `evals/check_static.py`, no agent

Prose obligations, derived from the skill source rather than restated:

- `checks_15_17_have_python3_branch` — parses `SKILL.md`'s audit section and
  requires both check bodies to name `python3` in an absent-branch sentence.
  **Red today:** neither does.
- `checks_15_17_name_nonzero_exit` — requires both to state the exit-code rule.
  **Red today:** neither does.
- `keep_refusal_covers_owned_set` — requires that neither the Step 3 refusal
  nor the Step 7 Keep-list section describes the owned set as "under
  `docs/ai/rules/`" alone. **Red today:** both do.

### Item 5b — a corpus fixture and an audit assert

The existing `rotted-layer` fixture is legislated at a current version and
ships the engine, so it cannot reach the state. A new fixture variant is the
smallest change that can: a repository with `docs/okf/` present and
`docs/ai/engine.py` **deleted**, manifest otherwise current.

- Assert `check15_engine_absent_info` reads the **audit report** for check 15's
  Info line naming `docs/ai/engine.py`.
- Negative control: the report must **not** carry a check-15 Warning for that
  repo — an absent engine is Info, not a finding about anchors.
- A red means law (the branch is wrong) or grader (the assert reads the wrong
  artifact) — distinguished by whether the Info line appears anywhere.

**Item 5b is the one assert that cannot be red against the unchanged law, and
saying why is part of the design.** The branch already exists in v20's check
15; what was missing is any fixture that reaches it. So there is no law change
to be red against — the assert is red today by construction, because the
scenario does not exist and there is nothing to grade. Its falsifiability
comes from elsewhere and is real: `fixture_state_is_bundle_without_engine`
asserts the fixture's own premise, so a later edit that ships the engine here
turns the scenario red instead of letting it pass for the wrong reason; and
`check15_engine_absent_info` fails whenever an agent does not take the branch.
Recorded here rather than glossed, because "this assert was green the first
time I ran it" is exactly the shape `POLICY.md` §3 warns about.

### What is deliberately not measured by a new assert

R-002 and R-009's law text are prose; `check_static.py` covers the mechanical
half (item 5a) and the corpus covers behaviour. Restating the exemption
sentence in an assert would be one fact in two places (`POLICY.md` §8).

## Clarifications

Session 2026-08-24, in chat.

**Q1 — one MR or two, given BL-034 is already on this branch?**
A: One. BL-051 is batched into `bl/034-self-legislation` and the MR is opened
once, as edition v21. Commit granularity is unaffected.

## Rejected alternatives

- **Fix item 3 by having checks 15/17 read stderr too.** It treats the symptom
  at every call site instead of at the engine, and leaves every future caller
  to remember. The engine owns its own failure signal.
- **Exempt `status: removed` documents in the engine only.** The engine would
  then disagree with the class definition it implements, which is the
  one-fact-two-places failure `POLICY.md` §8 names. The law states the
  exemption and the engine executes it.
- **Fold BL-057 in.** One benchmark would then measure two unrelated law
  changes, and a red could belong to either.
