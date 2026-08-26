# BL-047 — Spike: the decision inventory — what is still decided by a model

**Tier: 1 (light).** Blast radius: none in code — no `skill/` change, no
VERSION, no benchmark. Novelty: the law has never been measured for
enforceability. The deliverable is an answer, not code.

**Spec type: exploration.** Branch `bl/047-decision-inventory`. Time-boxed
spike per the backlog entry (raised 2026-08-23).

## The question

Which decisions in this system are taken by a model, and which of them could
be taken by code instead?

## Scope

**In:** every normative clause of the shipped law — `skill/assets/rules/core/`
(11 files, 222 lines) and `skill/assets/rules/stacks/` (4 files, 44 lines) —
and every decision point in `skill/SKILL.md`'s procedure (219 lines).

**Out:** `skill/assets/templates/**` (scaffold content, not law),
`evals/POLICY.md` and `.claude/rules/**` of this repo (instance law of member
#0, not shipped), the delivered copies under `docs/ai/rules/**` (byte-identical
by construction), and `skill/references/**` (how-to, not law). No check is
written inside this spike; each bucket-(b) entry becomes its own case, sized
from the measurement.

## Method

- **Unit of classification:** the normative clause — a bullet or sentence
  carrying an obligation, prohibition, or definition — not the raw source
  line: lines wrap mid-sentence and would double-count. The inventory reports
  both the clause count and the source line count per file, so the backlog's
  "217 lines" framing stays comparable.
- **Buckets:**
  - **(a) enforced** — a deterministic arm already adjudicates the clause
    where the decision is taken: `engine.py` (anchors, okf-debt, sdd-lint,
    baseline), the Claude Code hooks (`guard_owned_files.py`,
    `okf_sync_check.py`, `format_on_edit.py`), or the opencode guard. The
    evidence column names the arm.
  - **(b) enforceable** — a check that does not exist yet could adjudicate
    it; the entry names the check and estimates its cost (S/M/L).
  - **(c) interpretation** — the clause genuinely needs judgement; no
    mechanical test can say whether a diff obeys it.
- **Ranking of (b):** by decision cadence — how often the decision is taken:
  `every-edit` > `every-commit` > `every-task` > `every-legislator-run`.
  Cadence is read off the clause itself (what event it binds to).
- **SKILL.md decision points:** each numbered step is scanned for the places
  the executing model chooses (classify, propose, merge, name, judge); each
  such point is one inventory row, same three buckets.

## Acceptance (the case it would hurt to get wrong)

GIVEN the shipped law at v22, WHEN the inventory is complete, THEN every
normative clause and every SKILL.md decision point appears in exactly one
bucket with named evidence — a reader who never saw this spike can recount
the buckets from the inventory alone and get the same totals; AND the
bucket-(b) list is ranked with a named check and cost per entry. An
unclassifiable clause is itself a finding (recorded, not skipped) — per
artifact-lifecycle, unknown classification fails toward the cheap error:
it lands in (c), never silently in (a).

## Deliverable

`docs/cases/BL-047-decision-inventory/inventory.md` (lifecycle artifact) —
the classified table, the bucket totals, the ranked (b) list; a summary
paragraph lands in the backlog entry (status flip) and the day's journal.

## Stop condition

The inventory is the deliverable. No check is written; no rule is edited; a
positive (b) entry becomes its own case.

## Clarifications

### Session 2026-08-26

- **Q: do corpus grader asserts count as bucket-(a) evidence?** → No. (a)
  means the clause is adjudicated where the decision is taken — in the
  legislated repo, by the engine, the hooks, or the opencode guard. The
  corpus verifies the legislator's own behavior once per edition; that is
  falsifiability, not enforcement.
- **Q: do top bucket-(b) entries become backlog lines in this branch?** →
  Yes. The spike feeds v23: the top candidates are filed as PROPOSED entries
  in `docs/backlog.md` in the same MR; sizing and priority stay the owner's
  act when the next case is chosen.

## Converge — 2026-08-26

Judged against every promise above and the backlog entry: the inventory
classifies all 176 units (110 core clauses, 28 stack clauses, 38 SKILL.md
decision points) with named evidence per row; totals are grep-derived from
the file itself after the hand count was caught wrong twice; the (b) list is
ranked by cadence with a named check and cost per entry; BL-064–BL-067 filed
per the clarification; changelog, journal, glossary and OKF log updated; no
check written, no rule edited, no VERSION moved. Verification: check_static,
check_engine, engine anchors and sdd-lint all clean. No gaps (missing /
partial / contradicts / unrequested: none).

✅ Converged
