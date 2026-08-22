# Legislator — Eval Suite

Regression testing for the skill itself, run after every constitution/procedure
change (i.e. whenever `skill/VERSION` is bumped or `skill/SKILL.md` edited).

> **`POLICY.md` is the companion to this file and outranks it.** This one is
> the *how* — what an eval is, how to materialize a workspace, how to run and
> grade. That one is the *when, against what, and at what bar*: evals-first
> design, the 100% release bar, red classification, the model floor, and the
> baseline-before-change rule.

Two layers, mirroring unit vs. e2e tests:

| Layer | Command | Cost | When |
|---|---|---|---|
| Static checks | `python3 evals/check_static.py` | seconds, no agent | every commit |
| E2E scenarios | procedure below | ~8 agent runs (~40–60k tokens each) | every VERSION bump / SKILL.md change |

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
   fixture is generated from the current skill source — the alphabetically
   last core rule AND the alphabetically last dotnet stack rule withheld as
   "added since", one retired rule planted for deletion):

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
   the target repo, which the audit must not touch). The migration and
   upgrade agents must likewise be told to save their Step 7 reports to
   `<ws>/legacy-migration/outputs/migration-report.md` and
   `<ws>/upgrade/outputs/upgrade-report.md` respectively — the harvest and
   proposed-import assertions grade those files.
   The restructure agent gets the restructure prompt (blanket approval minus decision items is part of it) and must save its final report to <ws>/restructure/outputs/restructure-report.md.

   Naming convention (one format, no exceptions): every scenario's final
   report is `<mode>-report.md` in its `outputs/` directory —
   `migration-report.md`, `upgrade-report.md`, `audit-report.md`,
   `restructure-report.md`. Never named after a skill step, never mixed
   formats.

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
- **upgrade** — added-rule pickup (one core rule and one dotnet stack rule
  withheld by the generator), retired-rule deletion propagation, the Step 7
  report proposing the new stack rule's @import line, profiles reused
  without re-asking, project-owned files (including CLAUDE.md) untouched,
  keep-list carry-forward + prompt-driven add, pinned keep serialization.
- **idempotency** — a second run with nothing changed produces a zero diff.
  Catches serialization/formatting drift (this exact class of bug was found
  and fixed at VERSION 5). Run against fresh-scaffold-dotnet, upgrade (proves
  a populated keep list re-serializes byte-identically), and restructure
  (proves an already-standard layer is a zero-write no-op apart from
  re-surfacing open decision items).
- **audit** — read-only rot detection: the report must name every planted
  defect in the rotted fixture (see `setup_workspace.py:materialize_rotted`
  for the fifteen planted defects, including an unlinked keep-listed file, an empty glossary in a repo with source code, a root UBIQUITOUS_LANGUAGE.md foreign glossary store, and
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

## Trigger discipline (BL-036)

A trigger never names deliverables, modes, or reasons. The user's voice is
minimal — "Set this repo up for AI development", "Re-run the legislator in
this repo" — because anything more answers the skill's questions for it:
"rules, OKF docs, backlog, the works" leaks Step 4's table; "I just spun
up" tells the agent the mode; "we changed the core rules" tells it why it
is upgrading. What the skill must know, the repo evidences; what the repo
cannot evidence, the runner's CONFIRMATION WAIVER pre-approves ("every
confirmation is pre-approved; answer with the skill's own default plus
what the repo evidences; never end your turn on a question"). The
deliverables checklist lives in `expected_output` for humans and in
`grade.py`'s derived contracts for machines — never in the prompt.

## Derived contracts (BL-036)

`grade.py` derives its expectations from the skill source at grade time —
`SCAFFOLD_ARTIFACTS` is parsed from SKILL.md Step 4's table, the protected
set from it, migration wiring from `AGENTS.md.tpl`, audit check
severities from the SKILL.md check list, the restructure action set from
`restructure.md` §2. A divergence between law and grader is impossible to
introduce silently; `python3 evals/grade.py <ws> selftest:derivation`
asserts the derivations are alive (it is a pure check — no agent run).
Deliberately manual: fixture content markers (decimal-money, bl/NNN) —
intentional test-data oracles, not contract.

## Grade history and flaky analysis

Every grading appends `outputs/grade-history.jsonl` with a **generation
stamp** — skill VERSION, repo HEAD, and a hash of the grader itself
(`v17-<commit>-g<hash>`). The dashboard's flaky panel counts persistent
(every run) vs flaky (some runs) failures **within one generation only**:
a law fix or grader fix starts a new population, and pre-fix runs never
vote on post-fix stability.

## Background procedure (BL-037) — the recommended way to run a benchmark

```bash
tools/evals-bg.sh /tmp/legislator-eval-vN [--only SCEN] [--skip-smoke] \
                  [--runner opencode|claude] [--model NAME]
```

Staged, in order, each stage only on the previous green:
`check_static` → **smoke** (the upgrade scenario — the most
change-sensitive surface) → the full corpus → idempotency ×3 (run-1
results committed in-fixture, second pass, zero-diff grade). The runner
is detached: stall detection (log size + repo dirty-count frozen), a
resume ladder (`opencode run --continue`), a full fixture reset on every
invocation, prompts read from `evals.json` (single source), auto-grade
after every scenario, desktop notifications (`notify-send`) on scenario
and run boundaries, and `queue.json` + `status.md` as the machine-readable
contract — the interactive session polls a file, never a process.

## Run profiles — two engines, one contract

The stages, the prompts, the fixtures and every assertion are
engine-agnostic; only how a scenario agent is spawned differs. Pick with
`--runner` (or `RUNNER=`); each profile carries its own default model,
overridable with `--model` (or `MODEL=`).

| | `opencode` (default) | `claude` |
|---|---|---|
| Spawn | `opencode run --dir <sc> -m <model>` | `claude -p --model <model> --safe-mode` |
| Default model | `zai-coding-plan/glm-5-turbo` | `haiku` |
| Resume ladder | `--continue` | `-c` |
| No-prompt writes | opencode permission config | `--permission-mode bypassPermissions` |
| `run.log` | prose, written directly | transcript rendered by `streamfmt.py` |
| Stall oracle | `run.log` | `run.jsonl` (raw stream-json) |

Two things are worth knowing before choosing.

**`--safe-mode` is what makes the claude profile a fair harness.** It
disables the operator's `CLAUDE.md`, auto-memory, hooks, plugins, MCP
servers and installed skills, so the agent gets the prompt and the skill
path and nothing else — including no chance for an installed `legislator`
to fire as a skill instead of being read as a file. (`--bare` goes further
but demands `ANTHROPIC_API_KEY` and never reads OAuth, so it cannot run on
a subscription.) Under this profile the fixture's own entry document is
*not* auto-injected either — the skill has to find it by its own procedure,
which is stricter than what opencode gave it.

**A profile switch is a bigger confound than a model switch.** Different
system prompt, different tool set, different edit semantics. Pass rates
compare *within* a profile, never across one — record the profile in the
benchmark file next to the model, and keep a fresh reading of the previous
version before treating any delta as a regression.

Provenance travels with every result: `run.json` stamps `runner`, `model`
and `law_commit` per run; `grading.json` and `grade-history.jsonl` carry
`run_id`, `runner` and `model` on every graded entry; the dashboard shows
the profile in its header badge and in each row of the run-history modal.
Process matching (stall sweeps, the dashboard's live-agent count) is scoped
to the workspace path in both profiles — a sweep must never reach an
unrelated agent running elsewhere on the machine.

The **live dashboard** (`evals/dashboard.py <ws> [--open]`) renders it:
per-scenario state (done / w-errors / failed / running / queued #N),
grade bars, full-log modals (newest first; refresh pauses while a log is
open or text is selected), flaky-vs-persistent panels, and the
orchestrator tail. Static HTML + meta-refresh — the kbo pattern: no
server, nothing leaves the machine. `NO_BROWSER=1` for service runs.

Notes and known bounds: the suite assumes the machine's installed skills
when deriving `.claude/rules/skills.md` (the `stages ≥ 1` assert is
weakened accordingly); idempotency has no migration carrier by design (a
second migration run is an upgrade in disguise — the manifest exists);
new-stack fixtures (aurelia-class) are a known gap; cross-repo case
conventions cannot be exercised by single-repo fixtures.
