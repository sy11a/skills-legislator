# BL-049 — Report derivability (v22 corpus, 2026-08-26)

Material: the seven report artifacts the live v22 workspace holds
(`/tmp/legislator-eval-v22/*/outputs/*report*.md`, 294 lines), the 56
report-targeting asserts of the 201-assert corpus (from the scenarios'
`grading.json`, idempotency duplicates excluded), and the v17–v22
chronicles. Zero agents were run.

## Line classes

- **D — derivable**: printable by an emitter from facts the run holds in
  its record — file events (created/overwritten/deleted), manifest fields,
  check results, import deltas, keep rules, pinned skeleton text.
- **Dm — model-slot**: a pinned-format line whose *content* is a semantic
  verdict the model supplied earlier in the run (a constitution candidate,
  a contradiction, a law-shapedness call, a term derivation, a migration
  disposition) — the emitter prints the slot, the model fills it.
- **P — free prose**: text composed at report time (rationale,
  explanation) with no slot in the skeleton.

Default is D; the tables list only the exceptions, so a reader recounts by
subtraction (the totals below are script-derived exactly that way).

## Per-report classification

| report (lines) | Dm lines | P lines | D / Dm / P |
|---|---|---|---|
| `upgrade` (28) | — | — | 28 / 0 / 0 |
| `upgrade-drop-stack` (56) | 11 codebase-map row descriptions; 12 glossary terms derived; 17 skills stage-map relevance | — | 53 / 3 / 0 |
| `legacy-migration` (43) | 14 glossary terms carved; 20 law-carve + not-covered verdict; 36–39 candidates ×4 | 7 split-disposition rationale | 36 / 6 / 1 |
| `legacy-migration-agents-first` (68) | 26 law-carve verdict; 27 skills relevance; 31 glossary terms; 43–45 disposition verdicts; 61–64 candidates ×4 | 42 kept-verbatim rationale; 47 not-a-conflict reasoning | 56 / 10 / 2 |
| `rotted-layer` audit (40) | 17, 18 law-shaped escalations (check 9); 21 contradiction (check 11); 22 stray rulebook (check 12); 35, 36 candidates | — | 34 / 6 / 0 |
| `audit-engine-absent` (14) | — | — | 14 / 0 / 0 |
| `restructure` (45) | 5 placeholder derivation; 8 map-row description; 13–15 merge dispositions; 16–20 decision items ×5; 34, 35 candidates | — | 33 / 12 / 0 |
| **total (294)** | | | **254 / 37 / 3 = 86% / 13% / 1%** |

Two whole reports — the plain upgrade and the engine-absent audit — are
100% derivable: every line either skeleton or a mechanical fact. The
scaffold report does not exist as an artifact at all: fresh-scaffold
prints its report to chat only, nothing persists, and **zero asserts
grade it** — an emitter would make it a gradeable artifact for free.

## Assert attribution (56 report asserts of the 201 corpus)

Classes: **S — shape-only** (exists only because a model composes the
report: pinned headings, section membership); **M — mechanical-fact**
(tests a deterministically derivable finding or delta; with the engine
producing it, the assert leaves the model corpus and is reborn as an
engine test); **J — judgement** (tests a semantic verdict; stays).

| scenario | asserts | S | M | J | the J list |
|---|---|---|---|---|---|
| `rotted-layer` | 40 | 4 (`## Constitution candidates` heading; 3× anchors-under-`## Critical`) | 27 | 9 | check-11 contradiction; check-12 stray rulebook; 2 candidate presences; instance-not-law exclusion; 4 candidate exclusions |
| `restructure` | 8 | 0 | 5 (glossary-heal mapping, 3× routing rules, fidelity line) | 3 | authority conduct, conflict-as-decision ×2 |
| `upgrade` | 3 | 0 | 3 (2 import-delta proposals, keep refusal) | 0 | |
| `upgrade-drop-stack` | 1 | 0 | 1 (import removal proposal) | 0 | |
| `legacy-migration` | 2 | 0 | 0 | 2 | harvest includes law / excludes instance |
| `audit-engine-absent` | 2 | 0 | 2 (pinned Info line; absent-warning control) | 0 | |
| **total** | **56** | **4** | **38** | **14** |

**42 of 56 report asserts (75%) — 21% of the entire corpus — exist only
because a model composes what an engine could print.** The 4 shape-only
asserts die outright with an emitter; the 38 mechanical-fact asserts
migrate to deterministic engine tests (seconds, no agent); 14 stay as the
model's report corpus — and they are exactly the harvest/contradiction/
candidates core that BL-047 classified (c).

## The historical count

Of the 36 classified defects in the v17–v21 chronicles (BL-060's count),
**8 are report-composition defects**, each named:

1. v17, grader — `## For the team:` heading regex graded typography over value.
2. v17, grader — fidelity `-i` / glossary markers red on a lawful table reformat.
3. v17, grader — skill-binding routing marker: same class.
4. v18, grader defect 1 — check-14 report marker order-sensitive (`skill-bindings] made-up-skill` adjacency).
5. v18, law defect 4 — the run invented a `## Health (never-touch…)` section under ambiguous law.
6. v19, model — `report_proposes_stack_import_line`: a *derivable* proposal line simply omitted; re-run clean.
7. v21 — the suppression leak: a compliance note quoting the suppressed statement (sighted in the v20 baseline too).
8. v21 — the finding-namespace defect: `[check-15]`/`[check-17]` instead of pinned slugs in the restructure report (v20 sighting: `okf_anchors_routed_to_team` red).

**22% of every classified defect in this system's history is
report-composition** — and every one of the eight is either prevented by
construction (1–4, 6, 8: the emitter prints the shape and the deltas) or
loses its channel (5, 7: no free-prose slot to invent a section or a
compliance note in — the P class measured above is 3 lines of 294, and
the emitter contract simply does not carry it).

## The seam — the emitter contract this sizes

- **The emitter prints:** the audit report whole (header, severity
  sections filled from check results, `Clean checks:`, the zero-writes
  verification line); the Step-7 skeleton with Created/Overwritten/
  Deleted/Keep list/Health filled from the run record, including the
  import-delta lines of `Needs your review`; the restructure plan
  skeleton (findings → items via the closed action mapping, outcomes,
  the fidelity line).
- **The model fills slots:** candidate rows, contradiction descriptions
  (checks 11/12 and decision items), law-shaped escalations, migration
  dispositions, glossary/term derivations, map-row descriptions — the 37
  Dm lines, all of them format-pinned.
- **Free prose is not a slot.** The 3 P lines were rationale; the
  contract drops the channel, which is also what closes the v21
  suppression-leak class for good.

## The answer

**The report emitter is a large win, measured three ways:** 86% of every
report line the corpus produces is pure emitter output (99% with pinned
model slots); 75% of the report asserts — a fifth of the whole corpus —
stop measuring a model's typing and become deterministic engine tests;
and 22% of the system's entire classified defect history is the exact
class the emitter deletes. For the v23 composition this sizes BL-066's
emitter slice as: audit report + Step-7 skeleton + restructure skeleton,
with the 14 judgement asserts staying as the model corpus and the
scaffold report becoming a persisted, gradeable artifact for the first
time.
