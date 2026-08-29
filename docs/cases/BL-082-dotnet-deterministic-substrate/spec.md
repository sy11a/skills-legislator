# BL-082 — The deterministic substrate becomes .NET (edition v25, with BL-077)

**Tier: 2 (full).** Blast radius: every deterministic surface of the
system — the engine's jobs, the four Claude Code hooks, the operator
tools, and everything BL-077 adds on top of them — changes substrate;
the law's verification rung and audit checks change the command they
name; the eval suite's `check_engine.py` becomes a parity ruler for a
port before it retires. Novelty: the first compiled code in this
repository, the first machine-installed binary, the first configuration
layer, and the first time an edition is built on a substrate that does
not exist at branch start.

**Spec type: feature.** Edition branch `bl/082-dotnet-deterministic-substrate`
(one MR per version; BL-077's work lands on this branch once the
substrate stands — the two cases share edition v25). Sources: the
2026-08-29 requirements session (this file's `## Clarifications`),
ADR-0005 (end state, amended by ADR-0008), ADR-0007 §6 (default
deterministic), BL-068 (portability audit), BL-069 (dependency register
and adoption policy), BL-070 (the measured behaviour contract), BL-072
(absorbed).

Companion cases this spec sizes but does not contain: **BL-083** (v26 —
the configuration layer complete), **BL-084** (v27 — the MCP host).

## Boundary

**In:** the solution skeleton (`Legislator.Core`, `Legislator.Engine`,
`Legislator.Hooks`, `Legislator.Cli`, tests) and its build discipline;
the port of every engine job that exists at v24 (`anchors`, `okf-debt`,
`sdd-lint`, `baseline`, `audit`, `detect`, `apply`, `verify`, `report`)
red-first against `check_engine.py`'s contracts; the port of the four
Claude Code hooks; the single entry point `legislator <job>` and
`legislator hook <name>`; the options model as the only home of default
values, with the machine and instance files and the environment
overlay as the first three configuration layers; `legislator config
show` with provenance; the law text that names the command (`core/
verification.md`, `core/okf.md`, `core/sdd.md`, audit checks 15/17);
the machine install and version/checksum audit from ADR-0005; the
status of Python as prototype-only, written into project law; the
retirement plan for `docs/ai/engine.py` and `plugin/hooks/*.py`.

**Out:** the configuration layer's full migration of every path the
registry and the arms read (BL-083 — v25 ships the model and the
layers, v26 finishes moving every literal into it); the MCP host
(BL-084); the opencode guard, which stays TypeScript (ADR-0005's
accepted exception); the eval harness in Python (`evals/grade.py`,
`setup_workspace.py`, `mutate.py`, `dashboard.py`, `tools/evals-bg.sh`)
— an instrument, not a deterministic arm, and a separate question;
`fleet.sh` beyond what BL-077's registry enumeration needs; any change
to the law's content beyond the command names.

## Doctrine this edition writes into law

- **One core, several hosts.** Every deterministic behaviour lives once,
  in `Legislator.Core`/`Legislator.Engine`; the CLI, the hooks and later
  the MCP server are thin hosts that parse input, call the core and
  render output. A host with logic of its own is a defect.
- **Python is a prototype, never a product.** A Python script may be
  written inside a case to try a step out; it does not ship in an
  edition, is not named by law, and is deleted or ported before the case
  converges.
- **Defaults live in one place.** Every path, file name, branch name,
  threshold, cadence and version floor is a member of the options model
  with a declared default there; a literal elsewhere is a defect the
  static check reports. Law content (rules, templates, `R-NNN`) is not
  configuration.

## Requirements

### The solution

- **R-8201** — The repository SHALL carry one solution under `src/`
  with the projects `Legislator.Core` (domain, file model, registry,
  options), `Legislator.Engine` (the jobs), `Legislator.Hooks` (the
  Claude Code hooks as commands), `Legislator.Cli` (the single entry
  point) and one test project per source project under `tests/`.
- **R-8202** — The build SHALL target .NET 10, enable nullable reference
  types, treat warnings as errors, enforce code style in build and run
  the .NET analyzers at the `Recommended` level, all declared once in
  `Directory.Build.props`; a project that relaxes any of these SHALL be
  a static-check finding.
- **R-8203** — `Legislator.Cli` SHALL publish NativeAOT for `linux-x64`,
  `win-x64`, `osx-x64` and `osx-arm64`, and every job SHALL start in
  under 50 ms on the reference machine (the PreToolUse budget from
  ADR-0005).
- **R-8204** — `Legislator.Core` and `Legislator.Engine` SHALL take the
  file system, the clock and the process environment through injected
  abstractions (`IFileSystem`, `TimeProvider`, an environment
  interface); a direct static call to `File`, `Directory`, `DateTime`
  or `Environment` in those projects SHALL be a static-check finding.

### The port

- **R-8205** — Every engine job that exists at v24 SHALL exist as a
  `legislator <job>` command with the same arguments, the same exit
  codes and byte-identical stdout on the eval fixtures, until
  `check_engine.py` reports parity.
- **R-8206** — Every assertion in `check_engine.py` and `check_hooks.py`
  SHALL have a named .NET test twin before the Python job or hook it
  covers is removed; the twin SHALL be shown red against an empty
  implementation before it is shown green.
- **R-8207** — WHEN parity is reached for a job THEN the law text that
  named `python3 docs/ai/engine.py <job>` SHALL name `legislator <job>`
  and the Python job SHALL be removed in the same edition; WHILE parity
  is not reached the Python job stays the arm and the .NET one is not
  named by law.
- **R-8208** — The four Claude Code hooks SHALL run as `legislator hook
  <name>` reading the hook JSON on stdin and returning the exit codes
  `check_hooks.py` specifies; `hooks.json` SHALL name the binary, never
  an interpreter.

### Configuration

- **R-8209** — `Legislator.Core` SHALL define one options model whose
  members are the only declared default for every path, file name,
  branch or tag name, threshold, cadence and dependency version floor
  the system uses; the static check SHALL report a path or name literal
  outside the options model and its tests.
- **R-8210** — Effective options SHALL be composed from, in order of
  precedence low→high: the model's defaults, `~/.config/legislator/
  legislator.yaml`, `<instance root>/legislator.yaml`, and
  `LEGISLATOR_*` environment variables; each layer SHALL be optional.
- **R-8211** — WHEN any layer contains an unknown key or a value that
  fails the option's type or range THEN the command SHALL exit non-zero
  naming the layer, the key and the reason before doing any work.
- **R-8212** — `legislator config show` SHALL print every effective
  option with the layer it came from; `--json` SHALL print the same as
  one JSON object.
- **R-8213** — Law content — rules, templates, `R-NNN` identifiers, the
  EARS form, the audit checks' meaning — SHALL NOT be configurable.

### Install and integrity

- **R-8214** — The edition SHALL pin the tool version; `legislator
  version` SHALL print it; `audit` SHALL report the installed version
  and the per-platform release checksum against the values recorded at
  tag time, as ADR-0005 specifies.
- **R-8215** — WHILE the binary is absent on a machine, the Claude Code
  hooks SHALL fail open with one warning and verification jobs SHALL
  fail loud (the BL-069 absence policy).

### Discipline

- **R-8216** — Every PR touching `src/` or `tests/` SHALL pass a
  code-review session with the owner before merge; the case plan SHALL
  carry that review as a task per PR.
- **R-8217** — `.claude/rules/dotnet-substrate.md` SHALL state the
  substrate law: new deterministic logic in `src/`, Python as prototype
  only, no literal outside the options model, tests at the boundary.

## Hurting case

**GIVEN** a fleet machine with the v25 binary installed and
`~/.config/legislator/legislator.yaml` overriding the cases directory
name, **WHEN** an agent runs `legislator sdd-lint --control <dir>` from
a registered clone, **THEN** the job reads cases from the overridden
directory, prints the same findings the v24 Python job printed on the
same tree, exits with the same code, and `legislator config show` names
the machine file as the source of that directory name — a tester who
never read the code can diff the two outputs and read the provenance
line.

## Clarifications

### Session 2026-08-29 (the requirements session)

- Q: Does v25 (BL-077) build on Python and port later, or on .NET from
  the start? → **A: .NET from the start.** BL-072 becomes step zero of
  v25: the solution skeleton, the engine port as pilot, then BL-077's
  content written in .NET. No line of v25 Python is written to be thrown
  away.
- Q: What is configurable? → **A: Environment and placement**: paths,
  file names, branch/tag names, cadence and thresholds, dependency
  version floors, the list of arms. Layered defaults → machine file →
  instance file → env, schema-validated, `config show` with provenance.
  Law content is not configuration.
- Q: When does the MCP host come, and what gates it? → **A: A separate
  edition after the pilot port** (v27, BL-084), as another host of the
  same core with tools equal to engine jobs; gated on Core/Engine
  coverage ≥ 90 %, `check_engine.py` parity, and the Python engine
  removed from law.
- Q: Where does the code live? → **A: This repository**, `src/` and
  `tests/`, one solution, .NET 10, xUnit v3 on Microsoft.Testing.Platform,
  NativeAOT CLI. One repository, one CI, one benchmark, one case per
  edition.
