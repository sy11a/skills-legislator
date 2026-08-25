# BL-043 — plan (edition v22)

One package: the spec's requirements are few and one domain (the engine and
the law that binds it), so research/contracts collapse into this file.
Decisions that outlive the case: ADR-0003 (the engine's write policy).
Local decisions are inline. BL-057 rides the edition; its plan is its spec's
eval-design section (tier 1).

## Research notes (Decision / Rationale / Alternatives)

- **Marker regex** — `per R-\d{3}` literal, same as plans. Alternatives in
  the spec's Clarifications (Q1).
- **Test-file predicate** — path contains `test` (case-insensitive) under
  the engine's source roots, minus build dirs and oversized files. Reuses
  `source_roots()`; a repo with no tests yields an all-uncovered baseline,
  which is the honest output, not an error.
- **EARS heading regex** — `^### (R-\d{3}) — (.+)$`. The em-dash form is
  what `core/sdd.md`'s practice produces and what every case in this repo
  already uses; a spec dodging the form simply contributes no rows, and
  sdd-lint's dangling check will name any `per R-NNN` that then resolves
  nowhere.
- **Atomicity** — `baseline` writes to a temp file in the same directory
  and `os.replace`s it (ADR-0003's crash consequence).
- **sdd-lint scope** — `docs/cases/*/` only. `docs/superpowers/**` is
  retired history and must never enter a lint pass.

## Tasks

Ordering: baseline corpus run completes before any `skill/` edit (the
skill symlink serves the working tree). Tasks 1–2 are eval-first per
POLICY §3 — asserts written and shown RED before the change they measure.

1. **check_engine red** — add `check_engine.py` cases for `sdd-lint` and
   `baseline` (synthetic case tree: dangling ref, uncovered R, quoted token
   exempt, bare token reported; determinism ×2; hand-edit destroyed;
   usage/exit contract). Run against the v21 engine: must FAIL. Commit the
   red. per R-206, R-203, R-204, R-209
2. **audit fixture red** — plant the quoted-token doc in the rotted-layer
   fixture, add its `absent_markers` entry; extend `grade_case_practice`
   with the delivered-engine `sdd-lint` exit-0 assert. Grade the v22
   baseline workspace (v21 law): both must be RED. Record. per R-101,
   R-102 (BL-057), and BL-043's corpus item
3. **engine** — implement `sdd-lint` and `baseline` jobs; docstring's
   write-policy sentence per ADR-0003. check_engine green. per R-201..R-206,
   R-209
4. **law** — SKILL.md check 2 quotation rule (BL-057 R-101..R-103);
   `core/sdd.md` analyze gate names the command (R-208);
   `core/artifact-lifecycle.md` generated member (R-207); SKILL.md's
   audit/upgrade prose wherever baseline.md needs naming (R-207);
   VERSION 21 → 22. per R-101..R-103, R-207, R-208
5. **bookkeeping** — ontology `generated` entry populated; glossary
   `baseline` row updated; philosophy §Horizon item removed (check_static
   enforces); backlog rows; OKF log; CHANGELOG. per R-207
6. **benchmark** — full corpus + idempotency ×3 on sonnet per POLICY;
   `evals/benchmarks/v22.md` against the v22 baseline run (v21 law, BL-062
   grader — POLICY §5's grader-change rule, first use); classify every red;
   record the model floor.
7. **self-delivery** — deliver v22 to this repo (fleet member #0),
   byte-verify; converge both cases; PR.

## The [P] map

Tasks 1 and 2 are file-disjoint [P]. Tasks 3–5 are sequential (3 unblocks
4's gate wording; 5 depends on 4's law text). 6 gates 7.
