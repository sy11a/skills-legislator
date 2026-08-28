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

### The model class carries a burden of proof

**"Model" is the only class whose fix costs nothing to write, which is exactly
why it is the one to distrust.** Before a red may be called model-class, show
that the law is unambiguous *at the point of use* — not merely correct
somewhere in the file. Two questions, both answerable from the artifact:

1. **Is the rule reachable from where the agent was working?** A prohibition
   that binds the whole report but lives mid-paragraph in one section is
   ambiguous in the operative sense, however exact its wording.
2. **Does the rule enumerate what "done" means?** "Findings are always
   `[decision]` items" does not say one item per file, so an agent that raised
   one item has obeyed the sentence it read.

Both questions failed on 2026-08-24 (v21). Two reds — an agent narrating that
it had honored a suppression marker, and an agent raising one check-11 item
where two were due — were classified model, and a full corpus run on a more
expensive model was launched to "raise the floor". The owner stopped it: *if
something breaks on sonnet, work out the situation and fix it, rather than
raising the floor.* Re-read as law defects, both were ambiguity of exactly
the two kinds above, and both were closed in SKILL.md's text.

Raising the floor is what you do when the law is provably clear and the model
still cannot execute it. It is not what you do when reading the law is hard.

**The class has a track record, and it is 0 for 3.** Every red ever filed as
model-class was later shown to be something else:

| Filed | Verdict |
|---|---|
| v18, the suppressed line quoted in a report's Notes — "law right, assert right, harness silent… did not recur" | Recurred in the v20 baseline and again in v21, three editions later. A law-placement defect: the prohibition binds the whole report but sits mid-paragraph in the candidates section, unreachable from the report-format spec. Fixed in v21. |
| v18, `{{PROJECT_NAME}}` left unfilled | Filed model, described in the same row as "a redundancy gap… belongs to a later edition" — the author had already seen the law defect. |
| v19, a missing stack-import line | Re-run clean, never examined again. |

A label that has never once survived scrutiny is not a diagnosis. Treat
"model" as a hypothesis of last resort, and write down what you checked.

### What thirty-six recorded defects say about where they come from

Counted from the chronicles of v17–v21, the first editions under this policy:

| Class | Count | The shape it almost always takes |
|---|---|---|
| **law** | 13 | **Nine of thirteen are scope or completion**, not error: the rule is correct but never says *where* it applies (unreachable from the point of use) or *when you are done* (no enumeration, so an agent stops at the first satisfied clause). Those are exactly the two questions above. |
| **harness** | 12 | Over half are **false green**: a stage that computes a verdict and does not propagate it (v20's unmaterialized workspace graded two scenarios CLEAN; v21's corpus stage printed ALL STAGES GREEN at 43/44), or an environment failure wearing a model's clothes (v21's quota exhaustion presenting as stalled agents). |
| **grader** | 8 | Typography graded instead of value — two disjoint namespaces, order-sensitive markers, an assert reading the wrong artifact. **Two were green and empty in every run of every version** until found by accident. |
| **model** | 3 | See above: none survived. |

The single cross-cutting theme, spanning harness and grader both, is
**silent non-measurement**. A red announces itself; a thing that was never
measured does not. Every rule in the two sections below exists to make
non-measurement noisy.

## 1b. Not measured is not passed

The suite's arithmetic must never award a point for an artifact that does not
exist. Measured on 2026-08-25 against a real graded run, by blanking the
artifact and re-grading:

| Scenario | Survives an empty report | Legitimate |
|---|---|---|
| `audit` (`rotted-layer`) | **14 of 44 — 32%** | 3. The other 11 are free points. |
| `restructure` | 30 of 38 — 79% | ~29. Its substance is the repo tree, not the report; 8 asserts correctly went red. |

The concentration is not accidental. **Audit is a zero-writes mode, so all of
its evidence is the report** — a scenario that produced nothing still scored
nearly a third. Two mechanisms produce that:

- **Negative asserts are vacuously true on an empty artifact.** Nine
  `does NOT contain` asserts pass when the report is missing, which is the
  `ghost_import_fixed` defect of v17 alive at scale: it was fixed as one
  assert and never as a class.
- **Existence is not substance.** `step7_report_saved` and its siblings test
  `path.exists()`, so a zero-byte file passes.

Two rules follow, and they are the same rule the runner learned in BL-058 —
a verdict that is not propagated is a verdict that was not reached:

1. **Every assert declares the artifact it reads.** When that artifact is
   absent or empty, the assert is **not passed and not failed — it is
   `unmeasured`**, and any unmeasured assert makes the scenario red. A
   negative assert may never draw its truth from an absent artifact.
2. **A scenario reports two numbers**: how many asserts were measured, and how
   many of those passed. `44/44 measured, 44 passed` and `14/44 measured, 14
   passed` must not both print as a percentage that looks like progress.

Both rules execute as of BL-062. `Grader.check` in `evals/grade.py` takes the
artifact as a **required** argument, so an assert that names no source cannot
be written; the pass rate's denominator is `measured`; and `grade_clean` in
`tools/evals-bg.sh` is the single definition of green that every stage and gate
calls, because the previous shape restated `summary.failed == 0` at each site.
Re-measured on the v21 artifacts afterwards, `audit` scores **5 of 44 measured,
4 passed** against the blanked report it used to score 14/44 on, and
`legacy-migration-agents-first` — which scored a full **22/22** with an empty
report — goes red on its probe. One assert per artifact is entitled to fail on
its absence: the probe, whose subject *is* the artifact. Every other assert
that declares it is `unmeasured`.

## 1c. An assert must be falsifiable, and the suite must prove it

§3's "a new assert must be shown RED before it is shown green" is right and
insufficient, because it binds only the moment an assert is written. Nothing
re-checks it afterwards, and **run history cannot help**: a healthy corpus is
green by definition, so "green in every recorded run" describes a perfect
assert and a dead one identically. On 2026-08-25, 199 of 200 observed asserts
were green in every surviving run — a statistic with no information in it.

Falsifiability therefore has to be *executed*, not remembered:

- **Every assert carries a mutation** — a named, minimal corruption of the
  artifact it reads that MUST turn it red. Mutations run against recorded
  artifacts, so the whole pass costs no agent and no tokens.
- **An assert with no mutation, or whose mutation leaves it green, measures
  nothing and is deleted.** Not weakened, not annotated — deleted. An assert
  that cannot fail is worse than no assert: it consumes a run and buys a false
  sense of coverage.
- **Two asserts whose mutations are identical are one assert.** Duplicate
  coverage inflates the denominator and makes a pass rate look like breadth.

The pruning criteria are deliberately mechanical, because the judgement call
"is this assert important?" is exactly the one that keeps dead asserts alive.

This section executes as of BL-063. **`python3 evals/mutate.py <workspace>`
is the pass**: the manifest lives in `evals/mutations.py` (marker-named
asserts derive their mutations from the same fixture_meta that names them —
§8 applied to mutations), and an assert with no entry is `uncovered`, which
is red — the obligation covers every future assert by construction. The
cadence: **the full pass is mandatory in every edition cycle — after the
green corpus, before `evals/benchmarks/v<N>.md` is written — on the
benchmark run's own workspace, after every agent has finished** — the
pass and the runner share one workspace lock (BL-073), so a pass started
against a live run refuses rather than measuring a moving substrate — and
the benchmark file records the summary
(killed / survived / uncovered / duplicate groups). Survivors and duplicates
are the pruning candidates; deletion itself is a separate, owner-reviewed
step (the D4 half), never part of the pass. Three operational rules the
first pass paid for: the runner validates the substrate against the recorded
corpus verdict before mutating (a verdict measured on a moved state is not a
verdict); the idempotency stage snapshots each scenario's corpus report
before run 2 (`*-report.corpus.md`), because run 2 overwrites the pass's
substrate; and a kill is `failed` — `unmeasured` counts only for the probe
asserts, whose mutation IS removal.

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

### A new assert must be shown RED before it is shown green

This is the operational core of the rule, and the one thing not to skip.
Write the assert, run it **against the unchanged law**, and confirm it
fails. An assert that is green before the change is measuring nothing, and
you cannot tell that by reading it — v17 shipped two that had been green
and empty in every version: `ghost_import_fixed` compared against an empty
string whenever `AGENTS.md` was absent, and `grade_restructure` never
checked for unresolved `{{TOKEN}}`s at all. Both were found by accident,
months late, and both would have been caught in seconds by demanding a red
first.

Where the assert needs a defect to bite on, plant it in the fixture in the
same step, and check the parity map — a law check with no planted defect is
unfalsifiable by construction.

### What "designing the eval" concretely produces

In the edition's spec under `docs/superpowers/specs/`, alongside the law it
measures — never a separate document that can drift from it:

1. **Which scenario exercises it** — an existing one, or a new fixture, and
   why the existing ones cannot.
2. **The assert, by name**, with the postcondition it checks and **which
   artifact it reads** (the repo tree, the report, the manifest, git state).
   Naming the artifact is what keeps an assert from reading the wrong file
   and reporting a confident wrong diagnosis.
3. **The planted defect** the assert bites on, and its slug in the parity
   map when it is an audit check.
4. **The negative control** — what must NOT appear. An assert that only
   checks for presence is passed by an agent that does everything plus the
   forbidden thing; v17's audit scenario needs both `report_markers` and
   `absent_markers` for exactly this reason.
5. **Derived or restated?** Prefer parsing the expectation out of the skill
   source (`SCAFFOLD_ARTIFACTS`, the audit check list, the action set) over
   writing it twice. If it must be restated, say which copy is
   authoritative.
6. **What a red would mean** — which of §1's four classes it would point at.
   An assert whose failure could mean any of the four is not yet a
   measurement.

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

**Editions are tagged at merge** (`v17`, `v18`, …), which is what makes the
baseline a two-line operation — the tag carries the previous law *and* its
grader together, so the baseline measures that edition as it actually was:

```bash
git worktree add /tmp/legislator-baseline-v<N-1> v<N-1>
cd /tmp/legislator-baseline-v<N-1>
python3 evals/setup_workspace.py /tmp/legislator-eval-baseline
NO_BROWSER=1 tools/evals-bg.sh /tmp/legislator-eval-baseline \
  --runner claude --model <the model you intend to use>
# ... record the numbers, then:
git worktree remove /tmp/legislator-baseline-v<N-1>
```

Do not try to point the current suite at an old skill directory instead: the
grader derives its expectations from the skill source, so mixing a new
grader with an old law measures neither. The worktree keeps the pair
together.

**When the grader changed and the law did not**, the tag is the wrong
worktree: `v<N-1>` resurrects the previous edition's *grader* along with its
law, and the baseline would then be measured on a different instrument than
the edition it is the baseline for — the confound §9 requires you to name,
introduced by the very act of taking the baseline. Take it from the last
commit that still carries the previous law, with the current grader:

```bash
git worktree add /tmp/legislator-baseline-v<N-1> <commit where VERSION was N-1
                                                  and the grader is today's>
```

The rule generalizes: **the baseline isolates the law, so everything else —
grader, harness, model, prompt — is held at the value the edition will be
measured at.** BL-062 is the first case that made the two diverge; it changed
`evals/grade.py` alone, so the v22 baseline is taken from master at that
change, not from the `v21` tag.

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
