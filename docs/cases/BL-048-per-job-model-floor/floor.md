# BL-048 — Per-job model floor (measured 2026-08-28)

Probe: `probe/run_probe.py` over `probe/dataset.json` (labels with
provenance), raw results in `probe/results.json`. Identical prompts per
model; ollama at temperature 0; haiku through the Claude Code CLI in
safe mode. The floor reference is the label set itself — every label is a
v23-grader expectation or a fixture disposition sonnet meets at 100% on
the corpus.

## The labelled set

**Job A — the constitution-candidate test** (law-shaped ∧ not covered ∧
generalizable, plus the not-law marker): 15 statements from the rotted
and migration fixtures — 6 candidates, 9 non-candidates (a
marker-suppressed rule, an instance rule naming a project service, two
domain facts, a rule contradicting owned law, the project's branch
pattern, an environment detail, a description, template wiring).
Provenance per row in `dataset.json`.

**Job B — glossary term extraction:** the migration fixture's Domain
notes; expected terms `invoice`, `dunning run` (what the lawful runs
seeded).

## Results

| model | job A accuracy | how it fails | job B recall (extras) | latency/call |
|---|---|---|---|---|
| `qwen2.5:3b` (local, 1.9 GB) | **9/15 (60%)** | degenerate: answers NOT to everything — 0/6 candidates found, 9/9 non-candidates by default | 100% (0 extras) | 0.6 s |
| `llama3.2:3b` (local, 2.0 GB) | **7/15 (47%)** | the opposite degeneracy: 7 false candidates incl. the **marker-suppressed** line, the branch pattern, a description and template wiring; 1 candidate missed | 100% (0 extras) | 1.1 s |
| `haiku` (API, cheap) | **12/15 (80%)** | 2 candidates read as instance (#4, #5 name `Services/` and `Data/Repositories` — a defensible reading the labels reject); 1 false candidate (the past-tense naming convention) | 100% (4 extras: `billing period`, `payment webhook`, `psp`, `dunning`) | 10 s |
| `sonnet` (the floor) | 15/15 by construction | — | 100% (by construction) | (corpus runs) |

## The answer

**Job A — no.** The candidate test is a three-way conjunction with a
suppression rule, and it is exactly the kind of judgement the backlog
suspected: both 3B models collapse to a constant answer (one always
NOT, one nearly always CANDIDATE — the suppressed line proposed as fleet
law is the worst possible miss), and haiku's 80% is three wrong
proposals per fifteen statements — for a scan whose output is *proposed
law*, each miss is a wrong candidate in front of the owner. Sonnet stays
the floor for this job; the per-job floor idea does not apply here.

**Job B — a qualified yes.** All three models, including both 3B locals,
extracted the fixture's terms at 100% recall with zero extras in under a
second — the extraction job is shaped for small models (spot bolded
domain nouns, list them). The qualification is the evidence's size: two
terms from one text. It is enough to say "worth measuring at scale", not
enough to route on.

**What this decides.** "Model floor" does not become a per-job property
in this edition. The one positive is filed as its own case: a larger
job-B set (every fixture's domain notes + this repo's own glossary
history as labels) and, only if it holds at scale, a routing design in
which the *engine* invokes a local extractor for `{{GLOSSARY_TABLE}}`
seeding while the model reviews. The classification jobs BL-047 marked
(c) — candidates, contradictions, law-shapedness — stay at the floor; the
economics answer the backlog wanted is: the corpus does not get cheaper
by swapping models, it gets cheaper by moving asserts off models (BL-049,
BL-066 — 38 asserts already did).

Environment fact recorded: the two extraction-tuned locals already
installed (LFM2-350M-Extract, NuExtract-tiny) were not candidates —
single-purpose resume extractors by their model cards.
