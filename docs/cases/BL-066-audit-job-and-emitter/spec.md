# BL-066 — The engine audit job and the audit-report emitter (edition v23)

**Tier: 2 (full).** Blast radius: the audit mode entirely — 13 of 17
checks move from model execution to the engine, and the report becomes
engine-printed with pinned model slots; `SKILL.md`'s Audit section, the
engine, the grader's derivations, the rotted-layer/audit-engine-absent
scenarios and their mutations. Novelty: first engine job that *prints a
report*; first model-findings channel.

**Spec type: feature.** Edition branch `bl/065-066-edition-v23`
(one MR per version; BL-065 and the BL-070/BL-069 riders ride the same
branch). Sources: BL-047 (sk-28…sk-36), BL-049's emitter contract,
BL-068/BL-069 riders. Scope clarified 2026-08-26: **audit slice only** —
Step-7 and restructure emitters ride v24 with engine apply/verify.

## Requirements

- **R-661** — WHEN `python3 docs/ai/engine.py audit` runs from a repo
  root, the engine SHALL execute audit checks 1–10, 13, 14, 15, 16, 17
  (all mechanical checks, the two existing engine checks folded in) and
  print the complete audit report in the pinned format — header line,
  constitution line, severity sections, `Clean checks:` line — from its
  own results, writing nothing (the check-job discipline, ADR-0003).
- **R-662** — WHERE a model-findings file is passed
  (`engine audit --model-findings <path>`, JSON), the engine SHALL merge
  its entries — semantic findings for checks 11 and 12, check-9
  law-shaped severity escalations, and constitution candidates — into the
  correct severity sections and the candidates appendix, sorted with the
  engine's own findings; the report SHALL be byte-stable for identical
  inputs.
- **R-663** — The printed report SHALL carry a provenance line naming the
  engine and its constitution version (the emitter stamp); the report
  format specification in SKILL.md gains this line.
- **R-664** — SKILL.md's Audit section SHALL instruct the executing model
  to: run the two semantic checks (11, 12) and the candidates scan, write
  the model-findings file, invoke the engine, and deliver the engine's
  output verbatim as the report — while the 17 check *definitions*, the
  pinned slug list and the severity map remain in SKILL.md, parsable by
  the grader's existing derivations (POLICY §8).
- **R-665** — WHILE git is absent or a check cannot execute, the engine
  audit SHALL fail loud, never clean: an exit outside {0, 1} with a
  stderr line (this closes BL-069 F1 for `okf-debt` both standalone and
  folded, and generalizes the rule to the audit job).
- **R-666** — Every engine read/write SHALL pass `encoding="utf-8"`
  (BL-068 C1); behavior on this machine byte-identical (check_engine
  green unchanged).
- **R-667** — The zero-writes contract SHALL hold and be verified: the
  audit scenarios' repos are byte-identical before and after the run
  (existing asserts continue to bind; the engine's own tests add a
  writes-nothing check for the new job).
- **R-668** — Every new assert SHALL be shown red against the v22 law
  before green, and SHALL carry a mutation in `evals/mutations.py`
  (uncovered = red, POLICY §1c).

## The model-findings channel (the contract's data shape)

```json
{
  "findings": [
    {"check": "project-rules", "severity": "Warning",
     "line": "<the full finding line, pinned format>"},
    {"check": "foreign-structures", "severity": "Warning",
     "escalates": "<path the engine listed at Info>",
     "line": "<the escalated finding line>"}
  ],
  "candidates": ["- \"<quote>\" — <path>"]
}
```

The engine validates shape defensively (a malformed file is a loud exit,
never a silently partial report), inserts `findings` into their severity
sections, replaces an escalated Info entry, and appends the candidates
section only when non-empty (the law's omit-when-empty rule).

## Eval design (POLICY §3 — before the change)

| new assert | scenario | artifact | planted defect / bite | negative control | red means |
|---|---|---|---|---|---|
| `audit_report_carries_engine_stamp` | rotted-layer, audit-engine-absent | report | none needed — red against v22 by construction (no stamp exists) | stamp absent → red | law (SKILL.md didn't wire the engine) or harness |
| `audit_mechanical_findings_match_engine` | rotted-layer | report + repo tree | grader re-runs `engine audit` (no model findings) on the scenario repo and requires every engine finding line verbatim in the report | a finding line the re-run produces that the report lacks → red | law/model (the model edited engine output) |
| `model_findings_in_pinned_sections` | rotted-layer | report | check-11/12 lines must sit inside `## Warning` (the engine's merge, not appended prose) | the same line outside its section → red | law (merge broken) |
| `engine_audit_writes_nothing` | check_engine (unit) | tmp repo | run audit job; tree hash before == after | any write → red | engine bug |
| `engine_audit_fails_loud_without_git` | check_engine (unit) | tmp repo, PATH without git | exit ∉ {0,1} + stderr names git | exit 0 → red (the BL-069 F1 measurement, now a permanent test) | engine bug |
| `okf_debt_fails_loud_without_git` | check_engine (unit) | the BL-069 M4 repo shape | same | exit 0 clean → red | engine bug |
| engine unit replicas of checks 1–10, 13, 14, 16 | check_engine | synthetic fixtures per check (the rotted-layer defect set, replicated small) | each check's planted defect found; clean tree silent | finding on a clean fixture → red | engine bug |

Existing rotted-layer/engine-absent asserts (40 + 2) stay as written —
after this change they measure the engine's output through the model's
delivery, and the D4 pruning pass may migrate them later; deleting is not
this case's act. Mutations: stamp-strip, finding-line-strip,
section-displacement — named entries per assert in `evals/mutations.py`.

## Out of scope

Step-7 and restructure emitters (v24, with engine apply/verify); deleting
or migrating any existing assert (D4 is owner-reviewed, separate); any
change to checks 11/12's semantic definitions.

## The hurting case

GIVEN the rotted-layer fixture, WHEN the agent runs audit under v23, THEN
the report is the engine's print — every mechanical finding byte-equal to
an engine re-run on the same tree, the model's check-11/12 lines sitting
inside their severity sections, the stamp present — AND the repo is
byte-identical before and after. The case that hurts most: a report that
*looks* engine-printed but was hand-composed — the stamp plus the
re-run-match assert exist precisely to make that unfakeable-by-accident.
