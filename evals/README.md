# Legislator — Eval Suite

Regression testing for the skill itself, run after every constitution/procedure
change (i.e. whenever `skill/VERSION` is bumped or `skill/SKILL.md` edited).

Two layers, mirroring unit vs. e2e tests:

| Layer | Command | Cost | When |
|---|---|---|---|
| Static checks | `python3 evals/check_static.py` | seconds, no agent | every commit |
| E2E scenarios | procedure below | ~4 agent runs (~60k tokens each) | every VERSION bump / SKILL.md change |

All grading is deterministic scripting (byte-diffs against the skill source,
git state, manifest parsing) — no AI judge. Expectations are **derived from
the current skill source at grade time** (VERSION, `assets/rules/**`), so the
suite does not rot when rules are added, removed, or renamed. The one thing to
maintain by hand: `SCAFFOLD_ARTIFACTS` in `grade.py` mirrors SKILL.md Step 4's
table — update it if that table changes.

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
   `legacy-migration`, `upgrade`.

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
  convention must survive somewhere in the result, never silently dropped.
- **upgrade** — added-rule pickup, retired-rule deletion propagation,
  profiles reused without re-asking, project-owned files (including
  CLAUDE.md) untouched.
- **idempotency** — a second run with nothing changed produces a zero diff.
  Catches serialization/formatting drift (this exact class of bug was found
  and fixed at VERSION 5).

## Baseline comparisons

Without-skill baseline runs (what does a bare agent produce?) answered the
"does the skill add value" question once (+33 pass-rate points, 2026-07-09)
and are **not** part of the per-version regression loop — regressions are
measured against the previous version's with-skill results, at half the cost.
