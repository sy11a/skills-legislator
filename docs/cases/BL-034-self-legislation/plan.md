# BL-034 — plan

Branch `bl/034-self-legislation`. One MR, at the end, as a version delivery.
Every task traces to a requirement in `spec.md`. `[P]` marks file-disjoint
tasks that may run in any order relative to their siblings.

## Phase 0 — pre-flight

**T-01 — record the pre-migration state.** *(per R-013, H-1)*
Capture, in `research.md`: the current `CLAUDE.md` section inventory with a
law-shaped / instance-data / already-covered verdict per section; the list of
tracked files referencing `docs/glossary.md`; the four static suites' current
verdicts. This is the before-picture every later verification compares to.
**Acceptance:** `research.md` exists and carries all three inventories.

**T-02 — prove the write-guard cannot block skill development.** *(per R-013,
H-1)*
Before anything is delivered, drive `plugin/hooks/guard_owned_files.py` over
`skill/assets/rules/core/okf.md` and `skill/VERSION` in this repository and
record the verdicts. The guard keys on `docs/ai/rules/**`, `docs/ai/engine.py`
and `opencode.json`; if it blocks anything under `skill/`, self-legislation is
unsafe and the case stops here.
**Acceptance:** both paths return exit 0, recorded in `research.md`.

## Phase 1 — deliver the layer

**T-03 — run the legislator against this repository in migration mode.**
*(per R-001, R-002, R-003, R-004, R-005, R-006)*
Follow `skill/SKILL.md` exactly, as a fleet repo would. `stacks` is the empty
list — neither `dotnet` nor `aurelia` applies. Nothing is committed by the
run itself; the diff is reviewed here.
**Acceptance:** `docs/ai/manifest.json` exists with `legislatorVersion` equal
to `skill/VERSION`; `AGENTS.md` is real and `CLAUDE.md` is a symlink to it;
`.claude/rules/` holds the carved repo-specific law; the co-author trailer
rule is gone from both (covered by `core/pair-development.md`).

**T-04 — byte-verify the owned layer.** *(per R-002)*
`diff` every path in `ownedFiles` against its source under `skill/assets/`.
**Acceptance:** every diff empty; a non-empty diff stops the case.

## Phase 2 — forward-only migration

**T-05 — merge the glossary forward.** *(per R-007, R-008)*
Move all 48 rows of `docs/glossary.md` into `docs/okf/glossary.md`, keeping
the `Status` and `Lives` columns; delete `docs/glossary.md`; update every
reference outside `docs/superpowers/**` (`docs/philosophy.md`,
`docs/ontology.md`, `README.md` if present).
**Acceptance:** row count preserved; `docs/glossary.md` absent; a repo-wide
grep for it returns hits only under `docs/superpowers/**`.

**T-06 — seed the OKF codebase map.** *(per R-009, R-010)* `[P]`
Fill `docs/okf/codebase-map.md` anchored to paths that exist: `skill/`,
`evals/`, `tools/`, `plugin/`, `docs/`. Under OKF v2 anchors are hard links
the engine resolves, so a wrong anchor blocks "done" through
`core/verification.md`'s rung.
**Acceptance:** `python3 docs/ai/engine.py` reports no unresolved anchor and
no sync debt.

## Phase 3 — keep the repository workable

**T-07 — re-run the four static suites and adapt what breaks.** *(per R-011)*
`check_static.py`, `check_engine.py`, `check_hooks.py`,
`check_opencode_plugin.mjs`. The expected failure mode is a check reading the
sixteen owned copies under `docs/ai/rules/**` as a second source of law.
Adapting a check to know about owned copies is in scope; changing what a
check means is not — that is a finding, not a fix.
**Acceptance:** all four green, and any adaptation is described in
`summary.md` with the reason.

**T-08 — run audit mode against this repository.** *(per R-012)*
**Acceptance:** clean, or every finding explained in `summary.md`.

**T-09 — verify the hurting cases.** *(per H-1, H-2, R-015)*
H-1: edit an owned-source rule under `skill/assets/`, bump `skill/VERSION`,
confirm no hook blocks it, the delivered copy still holds the old bytes, the
manifest still reads the old version, and no check calls the skew a finding.
Revert the probe edit afterwards.
H-2: confirm the rung reports clean on work that never touched the OKF.
**Acceptance:** both scenarios observed and recorded in `summary.md`.

## Phase 4 — the practice

**T-10 — README: deliver-to-self and version skew.** *(per R-014, R-015)*
`[P]`
Document the release step (bump → benchmark → deliver to self → byte-verify →
fleet) and the branch skew rule: v(N+1) under development on a branch while
the repo is legislated at v(N) is normal; owned-integrity drift on the
default branch is a finding.
**Acceptance:** both stated in `README.md`.

**T-11 — backlog bookkeeping.** *(per R-014)* `[P]`
Close BL-034 with a pointer into this case home; file the two findings this
case is not allowed to fix — `fleet.sh`'s `-maxdepth 4` blind spot, and
`fleet.sh status` reading the working tree rather than HEAD — as their own
cases.

## Phase 5 — gates

**T-12 — analyze.** Judge reuse-first and over-engineering; run the mechanical
passes (coverage R↔task, dangling `R-NNN`, unresolved placeholders). Findings
are proposals.

**T-13 — converge.** Judge the result against every promise in `spec.md` —
each `R-NNN`, both hurting cases, and the constitutional MUSTs — never against
the diff. Classify gaps missing / partial / contradicts / unrequested and
append each as a traceable task here. Loop implement → converge until clean.
Write `summary.md`. The case closes only on "✅ Converged".
