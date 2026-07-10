# Legislator — Eval Suite

Regression testing for the skill itself, run after every constitution/procedure
change (i.e. whenever `skill/VERSION` is bumped or `skill/SKILL.md` edited).

Two layers, mirroring unit vs. e2e tests:

| Layer | Command | Cost | When |
|---|---|---|---|
| Static checks | `python3 evals/check_static.py` | seconds, no agent | every commit |
| E2E scenarios | procedure below | ~5 agent runs (~40–60k tokens each) | every VERSION bump / SKILL.md change |

All grading is deterministic scripting (byte-diffs against the skill source,
git state, manifest parsing) — no AI judge. Expectations are **derived from
the current skill source at grade time** (VERSION, `assets/rules/**`), so the
suite does not rot when rules are added, removed, or renamed. The one thing to
maintain by hand: `SCAFFOLD_ARTIFACTS` in `grade.py` mirrors SKILL.md Step 4's
table — update it if that table changes.

## What an eval actually is

`SKILL.md` is a program whose interpreter is a language model. There is no
function to call, so it cannot be unit-tested like code — an eval tests it
the way a service is e2e-tested: put the world into a known state, trigger an
execution, assert on the observable result. One eval = three parts:

1. **Fixture** — a known input state: a git repo that is fresh, legacy
   (hand-written CLAUDE.md), or previously legislated at an older version.
2. **Trigger** — a realistic user prompt (see `evals.json`) given to a
   **fresh agent** with no context beyond the skill itself. Fresh matters:
   the skill must work for a future session that knows nothing, so the test
   agent must not be the session that wrote the skill.
3. **Assertions** — machine-checkable postconditions on the resulting file
   tree (manifest parses with exactly the derived ownedFiles, rules
   byte-identical to source, project-owned files untouched, nothing
   committed).

Grading deliberately avoids the "LLM-as-judge" pattern (asking another model
whether the output looks good — subjective, drifty). The legislator's output
is a file tree, so every assertion is a `diff`, a `git status`, or a JSON
parse: binary and reproducible. The only model judgment in the loop is inside
the run under test — the grading itself is plain code.

## How this becomes a benchmark

One graded run is a test result. A **benchmark** is the same assertions
tracked across versions: each `benchmarks/v<N>.md` records three numbers, and
comparing v(N) against v(N−1) answers "did my change increase or decrease
productivity":

| Number | Question it answers |
|---|---|
| Pass rate (per scenario) | Did the change break correctness? Any drop is a regression to find before downstream repos do. |
| Tokens + wall time | Did the change make runs more expensive? A SKILL.md edit that makes agents wander costs real money across many repos. |
| Idempotency diff count | Did the change introduce noise? A re-run with nothing changed must produce a zero diff (this caught the manifest-formatting bug at v5). |

Caveat: model runs are nondeterministic. A single failed assertion means
*investigate* (read the agent's run summary and the failing evidence line),
not automatically *revert* — and a suspicious or flaky-looking result
warrants re-running that scenario 2–3 times before drawing a conclusion.

## E2E procedure

1. **Materialize a workspace** (fresh fixtures, git-initialized; the upgrade
   fixture is generated from the current skill source — one core rule
   withheld as "added since", one retired rule planted for deletion):

   ```bash
   python3 evals/setup_workspace.py /tmp/legislator-eval-vN
   ```

2. **Run the skill in each repo** — one agent per scenario, in parallel. Each
   agent gets: the skill path (`<repo>/skill`), the target repo path from the
   workspace, and the scenario's prompt from `evals.json`. Instruct it to
   follow SKILL.md exactly, treat profile confirmation as pre-approved
   (`dotnet`), and never commit. Scenarios: `fresh-scaffold-dotnet`,
   `legacy-migration`, `upgrade`, `audit` (the audit agent must be told to
   save its report to `<ws>/rotted-layer/outputs/audit-report.md` — outside
   the target repo, which the audit must not touch). The migration agent must
   likewise be told to save its Step 7 report to `<ws>/legacy-migration/outputs/step7-report.md` — the harvest assertions grade that file.
   The restructure agent gets the restructure prompt (blanket approval minus decision items is part of it) and must save its final report to <ws>/restructure/outputs/restructure-report.md.

3. **Grade:**

   ```bash
   python3 evals/grade.py /tmp/legislator-eval-vN
   ```

4. **Idempotency** — commit run 1's result in one repo, run the skill again
   with the same prompt, then assert zero diff:

   ```bash
   git -C /tmp/legislator-eval-vN/fresh-scaffold-dotnet/repo add -A
   git -C /tmp/legislator-eval-vN/fresh-scaffold-dotnet/repo commit -m "run 1"
   # ... agent runs the skill a second time ...
   python3 evals/grade.py /tmp/legislator-eval-vN idempotency:fresh-scaffold-dotnet
   python3 evals/grade.py /tmp/legislator-eval-vN idempotency:upgrade
   python3 evals/grade.py /tmp/legislator-eval-vN idempotency:restructure
   ```

5. **Record the result** — copy the printed pass/fail summary (plus tokens and
   wall time from the agent-run notifications, if tracked) into
   `evals/benchmarks/vN.md`. Comparing against the previous version's file is
   the productivity answer: pass rate = correctness, tokens/time = cost,
   idempotency = diff noise.

## What each scenario protects

- **fresh-scaffold-dotnet** — mode detection, verbatim copy, manifest
  serialization (incl. single-line `profiles`), full Step 4 scaffold, no
  unresolved `{{TOKEN}}`s (adr template carve-out respected), no commit.
- **legacy-migration** — everything above plus content fidelity: the
  fixture's hand-written architecture constraints and `bl/NNN-…` branch
  convention must survive somewhere in the result, never silently dropped;
  harvest: the decimal-money constraint is proposed as a constitution
  candidate, the branch convention (instance data) is not; law carved to .claude/rules/, instance data kept in CLAUDE.md.
- **upgrade** — added-rule pickup, retired-rule deletion propagation,
  profiles reused without re-asking, project-owned files (including
  CLAUDE.md) untouched, keep-list carry-forward + prompt-driven add, pinned
  keep serialization.
- **idempotency** — a second run with nothing changed produces a zero diff.
  Catches serialization/formatting drift (this exact class of bug was found
  and fixed at VERSION 5). Run against both fresh-scaffold-dotnet and upgrade
  (the latter proves a populated keep list re-serializes byte-identically).
- **audit** — read-only rot detection: the report must name every planted
  defect in the rotted fixture (see `setup_workspace.py:materialize_rotted`
  for the twelve planted defects, including an unlinked keep-listed file and
  a stray rulebook under docs/superpowers/; hub
  files must not be flagged (BL-011 regression lock)), and the repo must be
  byte-untouched afterwards (zero-writes contract); harvest appendix:
  planted law-shaped lines quoted (incl. the stray rulebook's generic rule), not-law-suppressed line absent; the stray rulebook's project-specific line and the owned-law-contradicting project rule are never proposed as candidates; project-rule conflicting with owned law flagged under the project-rules slug.
- **restructure** — approval-gated repair: zero content loss (fidelity greps), kept path immovable, owned-rule conflict decision-gated (never auto-resolved), foreign/misplaced structures reach the standard layout (incl. the stray rulebook merged into .claude/rules/ and removed); conflicting project rule decision-gated byte-unchanged; second run is a zero-diff no-op.

## Baseline comparisons

Without-skill baseline runs (what does a bare agent produce?) answered the
"does the skill add value" question once (+33 pass-rate points, 2026-07-09)
and are **not** part of the per-version regression loop — regressions are
measured against the previous version's with-skill results, at half the cost.
