# BL-063 — plan

One package (one domain: the eval suite). Decisions that outlive the case:
none expected — the engine/write question was ADR-0003's and does not recur
(the runner writes only into the throwaway workspace).

## Research notes (Decision / Rationale / Alternatives)

- **Manifest form** — `evals/mutations.py`: a table of
  `(scenario, assert-name-or-derivation, operation)` where operations are a
  small closed vocabulary over the substrate: report ops (`remove-line`,
  `insert-line`, `move-out-of-section`, `truncate`), tree ops (`edit-file`,
  `delete-path`, `write-path`, `git-commit`), manifest ops (`json-set`,
  `json-del`), meta ops (`json-list-del` on fixture_meta). Derived entries
  come from the same fixture_meta/skill sources the asserts derive from.
  Alternative rejected: mutations inline in grade.py — the grader must not
  carry knowledge used only by the pass (altitude).
- **Kill criterion** — the target assert flips to `failed` (R-602); other
  asserts may incidentally redden (minimality is a design goal, not a
  checked invariant — checking it would need N² grading).
- **Revert** — snapshot the bytes (or absence) of every path an operation
  touches; restore after grading. Git-state mutations (`git-commit`) revert
  via recorded HEAD reset. No per-mutation copies (R-606).
- **Substrate validation** — per scenario: if `fixture_off_base`,
  reconstruct run-1 (reset --hard <run-1>; clean -fd; reset --mixed
  eval-base — the v22 cycle's proven recipe); re-grade; compare
  verdict-by-assert-name with the recorded corpus `grading.json`. Disagree
  → `unusable`, red (R-605).
- **Duplicate detection** — canonical tuple of the operation and its
  arguments, compared within a scenario (cross-scenario duplicates act on
  different artifacts by construction).

## Tasks

1. **Runner skeleton + red 1** — `evals/mutate.py` with substrate
   validation, apply/revert engine, reporting; empty manifest → 201
   uncovered, exit 1 against the v22 workspace. Commit the red.
   per R-603, R-605, R-606, R-607
2. **Runner selftest + red 2** — the planted always-green assert case
   proving `survived` detection; plus revert-fidelity check (substrate
   bytes identical after a mutation round-trip). per R-604, R-602
3. **The manifest** — derived entries first (report markers, absent
   markers, severity anchors, candidate markers — ~54 report asserts),
   then the named tree/manifest/meta asserts (~147). Iterate to 201/201
   killed. per R-601, R-602
4. **POLICY §1c binding + benchmark template** — the command, the cadence,
   the required summary section. per R-608
5. **First full pass, recorded** — the v22 workspace's results (killed /
   survived / uncovered / duplicates) into the case summary; survivors and
   duplicates become the D4 input list, no deletions. per R-604, R-607
6. **Bookkeeping** — glossary (`mutation`, `killed/survived` if minted),
   OKF log, CHANGELOG, backlog, journal; converge.

Ordering: 1 → 2 → 3 (iterative) → 4/5 [P] → 6.
