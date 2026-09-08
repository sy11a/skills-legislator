# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- `docs/backlog.md` — the queue moved to Architector's idea and pre-release backlogs (Architector ADR 0009, case BL-011, 2026-09-08): the "Agreed order after v24" roadmap is replaced by a pointer table to Architector #33–#35, #53, #61–#65; sixteen open sections carry a moved-to line, eleven a closed line (delivered, moot or absorbed); BL-088 (Release 0's L-1) is the one task that stays.

### Removed

- **The `audit-engine-absent` eval scenario** (BL-082 T-13.10, owner's ruling).
  It falsified check 15's *"bundle present, engine absent → Info"* branch, which
  the retirement above deletes. `evals.json` carries 9 entries and the graded
  scenario set is 8 directories. The obligation itself
  survives at the unit boundary (`ArmIntegrityCheckTests`, plus `check_static.py`
  asserting that checks 15 and 17 state the absent-arm branch); what is no longer
  measured end-to-end is whether a model audits honestly on a machine with no arm.
- **The Python arm** (BL-082 T-13). `skill/assets/engine/engine.py` and the four
  `plugin/hooks/*.py` scripts are gone; `plugin/hooks/hooks.json` names the
  binary. A repository that still carries `docs/ai/engine.py` from an earlier
  edition keeps an ordinary file: it left `ownedFiles`, the owned-files guard is
  silent on it, and an upgrade run deletes it.

### Fixed

- **CI can start the .NET gate** — the workflow invoked it through `sh`, which
  overrides the script's `#!/usr/bin/env bash` shebang; on a runner whose
  `/bin/sh` is dash it died at `set -o pipefail` before the first test, on every
  job of every v26 run. It runs `bash evals/check_dotnet.sh` now, and
  `check_static.py` fails on any live file that calls the gate the old way.

### Changed

- **The hooks fail open silently when the arm is absent** (BL-082, ADR-0011).
  `hooks.json` runs `command -v legislator >/dev/null 2>&1 || exit 0; exec
  legislator hook <name>`: a machine that has not installed the binary gets
  exit 0 and no output instead of `command not found` on every tool call. Up to
  v25 the Python launcher had this property by accident; v26's first form lost
  it. Audit check 20 is where an absent arm is said out loud — once per audit,
  not once per keystroke.
- **The mutation manifest follows the renamed emitter stamp** (BL-082 T-14).
  Six asserts about `Emitted by …` were unfalsifiable: `grade.py` moved to the
  binary's stamp in T-13 and `evals/mutations.py` did not, so the mutation
  deleted lines that no longer existed and the assert passed unmoved. Run
  history cannot find that class — a healthy corpus is green either way.
- **CI runs the .NET suite through `evals/check_dotnet.sh`** (BL-082 T-14.1) —
  its test step called `dotnet test src`, which discovers nothing on this SDK.
- **The audit corpus covers the two checks v26 added** (BL-082 T-14.1): the
  rotted fixture plants a case-collision against an owned rule, and
  `arm-integrity` is declared environment-relative in the grader with its
  reason — its subject is the machine, which no repository fixture can plant.
- **The eval runner puts the arm on the scenario agent's `PATH`** (BL-082 T-14)
  and `setup_workspace.py` stops copying a Python engine into two fixtures.
  From v26 the law names a binary; a harness that does not put it in front of
  the agent measures the machine instead of the edition — the v26 benchmark's
  first attempt lost two asserts to exactly that.
- **README documents the arm's install path** (BL-082 T-14) — publish with
  `tools/publish-legislator.sh`, install with `tools/install-legislator.sh`,
  verify with `legislator version --json`; the release runbook gains the digest
  recording that audit check 20 reads, and the Windows copy is named as manual.
- **The law names one command per job** (BL-082 T-13). Every rule sentence that
  spelled `python3 docs/ai/engine.py <job>` now spells `legislator <job>` — the
  static rung in `core/verification.md`, the executing-arm bullets in
  `core/okf.md`, the analyze gate in `core/sdd.md`, the baseline sentence in
  `core/artifact-lifecycle.md`, and audit checks 15 and 17 in `SKILL.md`. Those
  two checks keep BL-051's obligation in the binary's voice: an arm that is not
  on the machine is an Info line and never a clean check, and an exit beyond
  clean-or-findings is a check failure.
- **An audit's Info findings no longer raise its exit code** (BL-082 T-13, ADR-0010) —
  only Warning and above do. Check 20 prints an Info line on every edition that
  has not been tagged yet, and an audit that exited 1 for it would teach its
  callers to stop reading the exit code.
- **`evals/check_dotnet.sh` runs the test modules directly** (BL-082 T-13) and
  fails by name when a module reports zero tests. `dotnet test` discovers
  nothing on this SDK with the xunit MTP adapter — a gate that reports nothing
  is worse than one that fails.

### Added

- **`L-1` — the development law reads by mode: `pair` and `waterflow`
  (Architector Release 0, track E; ADR-0012).** The constitution's development
  law gains a mode: a repository's entry document declares which reading its
  law takes — `pair`, today's text, the default — or `waterflow`, the second
  mode spelled for each cornerstone rule; the entry-document template declares
  the default (`- Development law mode: pair`). Audit check 21 `waterflow-mode`
  (Warning) reports a repository that declares `waterflow` without naming its
  release branch, and stays silent for one that names a convention — and for
  every `pair` or undecorated repository. The eval fixture plants the omission
  in the rotted-layer entry, so the finding is demonstrably caught. No
  `skill/VERSION` bump taken here — proposed in the pull request; editions are
  assigned at merge, never reserved.
- **`L-2` — task entry defaults to `/autoflow` (Architector Release 0, track
  E; ADR-0009).** `core/pair-development.md` gains one line: a task is
  opened via `/autoflow` unless the operator names `/flow`, conditioned on
  that skill pair being installed. This repository's own
  `.claude/rules/skills.md` gains a `flow-sessions` class naming `autoflow`
  first, and the generated stage map gains the same class: the
  `{{SANCTIONED_SKILLS_BY_STAGE}}` derivation in `skill/SKILL.md` pins
  `flow-sessions` — `autoflow` — as the first stage affinity, so fresh
  legislated repositories emit it wherever `autoflow` is installed. No
  `skill/VERSION` bump taken here — proposed in the pull request; editions
  are assigned at merge, never reserved.
- **Edition v25 — tracker slots (BL-085 answered; cross-repo case clerk
  BL-016).** A repository may now keep its work items in a task tracker
  instead of `docs/backlog.md`, and the constitution makes room for that
  without knowing what a tracker is: the entry document gains a
  `Task tracker:` pointer line naming the project's sources note;
  `backlog.md.tpl` gains a pointer body (`{{BACKLOG_BODY}}`, chosen by
  whether a tracker is recorded); the three law sentences — skill output
  redirection, the case-register row, the branch convention — now name both
  homes; audit check 18 `tracker-drift` reports a work item standing in the
  pointer region or a file that has become a second source of truth, judging
  only files inside the repository. No tracker is read, no vendor is named,
  and no repository is migrated by this skill: that is the companion's work.
- **`SKILL.md` Step 7.1 — the gated source-binding offer (BL-087 answered,
  option A).** A legislating run closes by offering to bind the project's
  sources through `/flow-setup`, hands over what it already established,
  invokes nothing on its own authority, and skips the offer when the skill
  is not installed.
- **The release path for the deterministic arm** (BL-082 T-12):
  `tools/publish-legislator.sh` publishes a NativeAOT binary for the host's RID
  and refuses any other by name — the AOT toolchain does not cross-compile
  between operating systems, so the edition's other three RIDs come from the
  matrix in `.github/workflows/dotnet.yml`, which is now part of the edition
  rather than incidental configuration. `tools/install-legislator.sh` copies the
  result onto `PATH`. Every publish records the binary's SHA-256 in
  `artifacts/SHA256SUMS`.
- **`legislator version --json`** (BL-082 T-12) — the version, the RID the binary
  was built for, and the SHA-256 of the executable that is running, taken by the
  binary itself rather than of a path a caller names. The plain `legislator
  version` is unchanged, and an unknown flag is now a usage error rather than a
  silently ignored argument.
- **Edition 26 pins the tool** (BL-082 T-12): `skill/VERSION` and
  `src/Legislator.Cli/Version.props` carry the same number, and a static check
  makes them impossible to move apart. The number was assigned from
  freshly-fetched `master`, never reserved.

- **The four Claude Code hooks, ported** (BL-082 T-11): `guard_owned_files`,
  `guard_git_conduct`, `format_on_edit` and `okf_sync_check` run as
  `legislator hook <name>`, reading the hook payload on stdin and answering exit 0
  to allow or exit 2 to block — never another code, and never an exception
  escaping: the host wraps every hook in a catch-all, so a bug in a guard cannot
  stop the user's work. A misspelled hook name is a loud usage error rather than a
  silent no-op, because a guard disabled by a typo is the failure nobody notices.
  `plugin/hooks/hooks.json` still names the Python scripts; it is rewritten to the
  binary in the task that retires them.

- **`legislator apply`, `verify` and `report` — the write path, ported** (BL-082
  T-10): Step 3 whole (byte-for-byte copies of the owned set, retirement of files
  the package no longer delivers, the keep rules with their three named refusals,
  the pinned manifest and the v14 file model), Step 6's byte-verify with exactly
  one re-copy per diverged file, and Step 7's report printed from the run record
  rather than from the tree. The record is written outside the repository by
  construction and refuses any path inside it. Two real entry documents stop the
  run at exit 4 before the first write, so the promise that nothing was written
  rests on nothing having been written yet. All thirty-six `check_engine.py`
  assertions the three answer carry a named `[Parity]` twin, five of them
  strengthened past the ruler because the ruler's own assertion is satisfied by an
  engine that does nothing; the label ledger fell from 96 to 60, and the full
  engine ruler is green on the published binary.

- **Case-collision detection beside the owned set** (BL-082 T-10): a file whose
  path differs from an owned path only by letter case is named with the path it
  collides with. It is one file on a case-insensitive checkout and two on this
  one, which is why nothing else notices it.

- **`legislator audit` — the read-only health check, ported** (BL-082 T-09):
  fourteen mechanical checks and the pinned report printed from them, byte-stable
  over an unchanged repository and writing nothing. The audit is the second
  caller of the anchors and okf-debt jobs rather than a second derivation of
  them, and it fails loud where git cannot be run at all instead of reporting a
  clean layer. All twenty `check_engine.py` assertions it answers carry a named
  `[Parity]` twin; the label ledger fell from 116 to 96.

- **`legislator detect` — the mode decision, ported** (BL-082 T-09): Step 1's
  decision tree and Step 2's stack signals as JSON on stdout, writing nothing —
  `fresh` / `migration` / `upgrade`, the entry document (an alias that is the
  symlink is not one), the subscription read from the modern `stacks` key or the
  legacy `profiles`, and the owned set reconstructed off disk when the manifest
  is gone but the layer is plainly installed. All six `check_engine.py`
  assertions it answers carry a named `[Parity]` twin; the label ledger fell from
  122 to 116.

- **`legislator baseline` — the requirement-to-test register, ported** (BL-082
  T-08): the engine's one write, deterministic over an unchanged repository and
  destroying any hand edit, byte-identical to the Python's on the same tree
  (30 747 bytes over this repository). All eight `check_engine.py` assertions it
  answers carry a named `[Parity]` twin; the label ledger fell from 130 to 122.

- **`legislator sdd-lint` — the analyze gate's mechanical passes, ported**
  (BL-082 T-08): coverage of requirement to task, dangling `per R-NNN`
  references, unresolved placeholders, and the case, ADR, journal, changelog and
  OKF-front-matter shapes, byte-identically to the Python job. All thirty-eight
  `check_engine.py` assertions it answers carry a named `[Parity]` twin; the
  label ledger fell from 168 to 130.

- **`legislator okf-debt` — the staleness job, ported** (BL-082 T-08): it names
  anchored documents whose sources moved on without them, byte-identically to
  `python3 docs/ai/engine.py okf-debt`, and fails loud where git cannot be run
  at all instead of reporting clean. All eleven `check_engine.py` assertions it
  answers carry a named `[Parity]` twin; the label ledger fell from 179 to 168.

- **`legislator anchors` — the first ported engine job** (BL-082 T-07): the
  OKF link-hardness check runs from the published binary with byte-identical
  findings and the same exit codes as `python3 docs/ai/engine.py anchors`, and
  the seventeen `check_engine.py` assertions it answers each carry a named
  `[Parity]` twin. The label ledger fell from 196 to 179. The options model
  gained `max_file_bytes`, the ceiling past which a file is not scanned for
  symbols.

- **The parity rulers can measure either arm** (BL-082 T-06):
  `evals/check_engine.py` and `evals/check_hooks.py` run the command named by
  `PARITY_ENGINE_CMD` / `PARITY_HOOK_CMD` when it is set — the
  published `legislator` binary — and the Python engine and hook scripts when
  it is not, on identical fixture trees; each ruler prints the arm it is
  measuring before its first check. `evals/parity_labels.py` reads every
  assertion label out of both rulers, and `tests/Legislator.Parity.Tests`
  holds the coverage ledger against it: a `[Parity(ruler, label)]` twin per
  assertion, with the debt recorded as a number that may not grow.

- **Backlog BL-091 filed** (as `BL-088`, renumbered at the rebase — `master` had
  minted that key for the Release-0 waterflow item) — the roadmap names cases,
  not edition numbers:
  every forward reference to an edition version becomes a reference to the
  case that carries it, so a queue reshuffle cannot leave stale version
  claims behind. Backlog entry only.

- **Spike BL-085 filed** — the legislator inherits the host repository's
  work-tracking discipline (Jira, GitHub Issues, Linear … via their MCP
  servers) instead of imposing the text backlog, which becomes the
  lowest-priority home with a standing migration recommendation. Backlog
  entry only; no behaviour change.

### Changed

- **Output redirection names the pen (clerk BL-032).** Where a repository
  records a tracker, the skill-output law now says *how* a work item reaches
  it — through `clerk file`, never as a hand-written row in a generated
  mirror — so a skill with something to file has a command to reach for
  instead of a file to edit. One sentence in `core/skills.md`; delivered
  with the next edition.
- **The deterministic substrate becomes .NET from v25** (ADR-0008, BL-082):
  BL-072 is pulled forward to step zero of edition v25 — one `src/`
- **The parity rulers' arm variables left the `LEGISLATOR_*` namespace**
  (BL-082 T-07): they are `PARITY_ENGINE_CMD` and `PARITY_HOOK_CMD`. The binary
  reads every `LEGISLATOR_*` variable as an option key and refuses an unknown
  one, so a ruler variable in that prefix stopped the arm it was measuring.

- **Edition numbers are assigned at merge, never reserved** (BL-082,
  applying the roadmap ruling made at the v25 merge). The .NET tool pin
  `src/Legislator.Cli/Version.props` holds `0.0.0` until the edition is
  merged; BL-082 Task 12 reads the number that is free then, bumps
  `skill/VERSION` to it and sets the pin's major to the same, with the
  static check binding the two from that commit on. BL-082 and BL-077 no
  longer claim v25 — that number went to BL-085/BL-087.

- **The deterministic substrate becomes .NET** (ADR-0008, BL-082):
  BL-072 is pulled forward to step zero of the edition — one `src/`
  solution (Core, Engine, Hooks, CLI), the engine and hooks ported
  red-first against the Python checks, an options model with four
  configuration layers and `config show` provenance; Python becomes
  prototype-only; the MCP host (BL-084) and the complete
  configuration layer (BL-083) are queued behind it. Project law
  `.claude/rules/dotnet-substrate.md`. Docs only — no `skill/` change in
  this commit.

- **The outer-only pivot is decided and specified** (ADR-0007, BL-077):
  the AI layer leaves the code repository for an external control
  directory; the backlog is re-prioritised around it (BL-077 → BL-078
  migration → BL-079 layering; fleet-obs prerequisites
  BL-080 first; semver BL-081 deferred), and BL-027/044/045/052/071 are
  absorbed. Docs only — no `skill/` change in this commit.

### Added

- **Edition v24** (BL-075): the engine gains `detect`, `apply`, `verify` and
  `report`. Step 3 — the owned-file copies, deletions, keep rules, the pinned
  manifest and the v14 file model — is one engine invocation (ADR-0006: the
  engine writes the owned layer); Step 6 is `verify`; the Step-7 report is
  printed from the run record with pinned model slots (candidates, review
  lines), the scaffold report becoming a persisted artifact. Two real entry
  documents stop `apply` (exit 4) at the decision gate, writing nothing.

- The per-job model floor probe (BL-048, spike): the constitution-candidate
  test stays at the sonnet floor (3B locals collapse to constant answers,
  haiku 12/15); glossary term extraction is the one job every candidate
  did at 100% — filed as BL-074 for measurement at scale. Reproducible
  probe in `docs/cases/BL-048-per-job-model-floor/probe/`.
- **Edition v23** (BL-065 + BL-066 + the BL-070/BL-069 riders): the engine
  `audit` job executes all fifteen mechanical audit checks and prints the
  report (the model supplies checks 11/12 and candidates via the
  model-findings JSON channel; the report carries an emitter stamp);
  `sdd-lint` grows nine case-shape lint families; scaffolds gain
  `.gitattributes` (LF pinned on machine-managed paths); the engine reads
  UTF-8 explicitly and fails loud without git; the eval runner's process
  control is portable (`tools/proc.py`). Benchmark: 205/205 corpus,
  idempotency ×3 zero diff, mutation pass 205/205 killed, model floor
  sonnet (`evals/benchmarks/v23.md`).

### Added

- Report derivability (BL-049, spike): the live v22 corpus measured — 86%
  of report lines are pure-emitter, 75% of report asserts (21% of the
  whole corpus) test what an engine could print, 22% of all classified
  defects in the chronicles are report-composition; the emitter contract
  for BL-066/v23 is stated in
  `docs/cases/BL-049-report-derivability/derivability.md`.
- The portability patch set, light half (BL-070): both conduct-guard arms
  recognize Windows-style command heads (`git.exe`, backslashed paths);
  the hooks launcher resolves `python3` → `py` → `python`; the link
  scripts are Python (`tools/link_skills.py`,
  `tools/link_opencode_plugin.py`) and fail loud when symlinks cannot be
  created; the eval harnesses gained a Windows chmod-skip, a guarded
  dashboard `ps` probe, and a node ≥ 22.6 floor assert. Engine riders
  (encoding, okf-debt fail-loud), the autocrlf ruling and the evals-bg
  port ride edition v23.
- The dependency register (BL-069, spike): 13 external dependencies
  classified with measured absence behavior on the load-bearing cells —
  one silent false green found (`engine okf-debt` without git reports
  clean), a dependency-discipline policy drafted as a constitution
  candidate, and the three future candidates (DB, binary arm, analyzer
  binding) given verdicts, in
  `docs/cases/BL-069-dependency-register/register.md`.
- ADR-0005 (accepted): the deterministic arm's end state is one
  machine-installed .NET binary (NativeAOT per platform) — the law stays
  delivered text, arm integrity moves to version-pin + checksum; phased as
  existing fixes first (BL-070, BL-071), the binary arm (BL-072) after.
- The cross-platform audit (BL-068, spike): 21 executable surfaces judged
  on Linux/macOS/Windows-native — two silent Windows killers found (the
  hook launcher and the CLAUDE.md symlink checkout), a ranked patch list,
  and the patch-vs-port verdict with its criterion, in
  `docs/cases/BL-068-cross-platform-audit/audit.md`.
- The git conduct guard (BL-064): a fourth enforcement hook
  (`plugin/hooks/guard_git_conduct.py`, PreToolUse on Bash, with the
  opencode port) blocks merge/push onto the default branch, AI attribution
  in commit and PR text, and `gh pr merge` — in legislated repos, fail-open
  on every undecidable case. The first every-commit-cadence law moved from
  enforceable to enforced.
- The decision inventory (BL-047, spike): the shipped law and `SKILL.md`
  measured for enforceability — 176 units, split 11 enforced / 97
  enforceable-by-nameable-check / 68 genuinely interpretive; ranked
  bucket-(b) list in `docs/cases/BL-047-decision-inventory/inventory.md`,
  top candidates filed as BL-064–BL-067.
- The mutation pass (`python3 evals/mutate.py <workspace>`): every corpus
  assert carries a named minimal corruption that must flip it to failed,
  derived from fixture data where the assert names are data; uncovered or
  surviving asserts are red. Mandatory per edition cycle before the
  benchmark file (POLICY §1c). First full pass: 201/201 killed, zero
  pruning candidates (BL-063).

### Fixed

- The eval workspace takes a lock (BL-073): `tools/evals-bg.sh` and
  `evals/mutate.py` refuse to run alongside a live instrument, naming the
  holder before writing anything; a dead holder's lock is taken over with
  one loud line. A full run's invocation-start cleanup removes only the
  records of scenarios it will run (`--skip-smoke` no longer wipes
  `upgrade`'s grading). Both v23 harness incidents are now impossible by
  construction (`evals/check_mutate.py`, 20 new checks, shown red first).
- `tools/fleet.sh status` names member #0 explicitly — delivered as a
  release step, never swept (ADR-0004); the line is informative and outside
  the exit contract. `tools/evals-bg.sh` stage 1 reclaims provably-unowned
  dotnet map files from `/tmp` (owner + open in no process) before the
  headroom probe, closing BL-059: first pass reclaimed 457 files / 1.88 GB.

- `tools/fleet.sh upgrade` no longer reports `FAIL` over a completed
  delivery: both branches re-read the manifest and decide on the version —
  the runner's exit code is evidence on the line, never the verdict
  (BL-061). `tools/fleet.sh status` reads the committed manifest; an
  uncommitted upgrade shows as `pending review`, never `ok` (BL-056).
  Verified by the new stub-runner harness `tools/fleet-harness.sh`.

### Added

- Edition v22: the engine gains `sdd-lint` (the analyze gate's mechanical
  passes — dangling per-R-NNN references, uncovered requirements in planned
  cases, unresolved placeholders; converged cases are history and are
  skipped) and `baseline` (writes `docs/ai/baseline.md`, the R-NNN ↔
  annotated-tests register — the `generated` class's first member, per
  ADR-0003). Audit check 2 learns the quotation rule: a `{{TOKEN}}` inside
  backticks or a fence is prose about templating, never a Critical
  (BL-057 — fourteen false Criticals in this repository alone).

- The eval grader carries a third verdict, `unmeasured`: every assert declares
  the artifact it reads, and an assert whose artifact is absent or empty scores
  nothing instead of passing. Any unmeasured assert makes its scenario red and
  the run exit non-zero. Scenarios now report two numbers — how many asserts
  were measured, and how many of those passed — and the pass rate is computed
  over what was measured (BL-062, `evals/POLICY.md` §1b).

- Edition v21: `status: removed` OKF documents leave the anchored class; build
  output is excluded from symbol resolution at any depth; the engine exits 3 on
  an unhandled exception and the audit treats any exit outside `{0,1}` as a
  check failure; audit checks 15 and 17 carry a `python3`-absent branch; no
  owned file can be keep-listed. New corpus scenario `audit-engine-absent`.
- This repository is legislated by its own constitution (fleet member #0):
  manifest, owned law under `docs/ai/`, OKF bundle, case home, project rules
  under `.claude/rules/`. See ADR-0002 and `docs/cases/BL-034-self-legislation/`.
- `tools/fleet.sh` takes a runner profile and exits non-zero when any
  repository did not reach the current version (BL-053).
- `tools/evals-bg.sh` refuses to run against a workspace that was never
  materialized (BL-050).

### Changed

- The entry document is `AGENTS.md`; `CLAUDE.md` is now a symlink to it.
- The domain glossary moved from `docs/glossary.md` to `docs/okf/glossary.md`,
  all 48 terms carried, under `core/okf.md`'s sync checklist.

### Fixed

- **The engine parity ruler measured Python where it claimed to measure the arm
  under test** (BL-082 T-09): four checks — the three `okf_debt_git_absent`
  labels and `engine_audit_fails_loud_without_git` — hardcoded the interpreter
  instead of going through `_engine_argv`, so they were green on the .NET arm
  without ever running it. They now follow `PARITY_ENGINE_CMD` like every other
  check. The two `usage`-family labels keep the same fault by the T-07 ruling
  that assigned them to the CLI task.

### Removed
