# BL-048 — Spike: per-job model floor — can a small local model take the classification work?

**Tier: 1 (light).** Blast radius: none in code — no `skill/` change, no
VERSION, no benchmark. Novelty: the model floor has only ever been
measured per-edition; nobody has measured it per-job.

**Spec type: exploration.** Branch `bl/048-per-job-model-floor`. Backlog
entry of 2026-08-23. The deliverable is a measured table, not code.

## The question

Is there any job in this pipeline where a small local model matches the
current floor's quality — or is `sonnet` the floor because every job
needs judgement?

## Scope

**In:** the two classification jobs the backlog names — **(A)** the
law-shapedness / constitution-candidate test (the same three-test rule
Step 7 and audit check 12 use) and **(B)** glossary term extraction — as
a labelled set built from artifacts whose labels already exist: the
rotted-layer fixture's candidate and candidate-absent markers, the
migration fixtures' carve dispositions (what became `.claude/rules/` law,
what stayed instance data, what became glossary rows), all of them
adjudicated by the v23 grader at 100% on the corpus (the floor's answers
ARE the labels).

**Out:** judgement jobs (restructure planning, migration splits, converge)
— the backlog rules them out a priori; any routing implementation; any
model installation beyond the clarified candidate list.

## Method

- **Candidates (clarified):** `qwen2.5:3b` and `llama3.2:3b` (ollama,
  local), plus `haiku` (Claude Code CLI) as the cheap-API reference; the
  two extraction-tuned local models already installed are recorded as
  environment fact but not run — they are single-purpose resume
  extractors, not classifiers (stated, not assumed: their model cards).
- **Job A protocol:** each statement is presented with the law's own
  three-test definition (law-shaped / not covered / generalizable) and a
  forced binary answer; accuracy, per-class errors, and the specific
  failure statements are recorded. The floor reference is the label set
  itself (sonnet's corpus-verified behavior).
- **Job B protocol:** the fixture's domain-notes text, extraction of term
  candidates; compared against the expected term set.
- Latency per call recorded (the economics half of the question).
- Totals script-derived; every model sees identical prompts.

## Acceptance (the case it would hurt to get wrong)

GIVEN the labelled set, WHEN the runs complete, THEN the table shows
per-model accuracy on job A (with each miss quoted) and term
precision/recall on job B, plus latency — AND the answer states, per
job, whether any candidate matches the floor's labels well enough that
"model floor" should become a per-job property. The case that hurts
most: a labelled set contaminated by items whose label is itself
debatable — every label must trace to a grader expectation or a fixture
disposition, never to my own judgement of what the answer should be.

## Deliverable

`docs/cases/BL-048-per-job-model-floor/floor.md` — the labelled set with
label provenance, the per-model table, the answer; summary to the backlog
(status flip) and the day's journal.

## Stop condition

The measured table is the deliverable. No routing is wired, no rule
changes; a positive result becomes its own case.

## Clarifications

### Session 2026-08-28

- **Q: which candidate models?** → Pull `qwen2.5:3b` + `llama3.2:3b`
  (~4 GB total) and include `haiku` as the cheap-API reference — three
  points on the cost→quality axis. The installed extraction tunes are
  environment fact, not candidates.

## Converge — 2026-08-28

Judged against the spec: every label carries provenance to a grader
expectation or fixture disposition (the acceptance's contamination
guard); every model saw identical prompts; misses are quoted per model;
latency recorded; the answer is stated per job and the positive becomes
its own case (BL-074), never a routing change here. Dead end recorded:
the runner's first version overwrote its results file between model
runs — fixed to merge, qwen re-run (identical result). The installed
extraction tunes were recorded as environment fact, not run. No
`skill/` change, no VERSION, no benchmark. Verification: check_static,
check_engine, engine anchors and sdd-lint clean. Gaps: none.

✅ Converged
