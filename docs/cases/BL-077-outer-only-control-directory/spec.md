# BL-077 — Outer-only placement: the AI layer leaves the code repository (edition v25)

**Tier: 2 (full).** Blast radius: every legislated repository — the owned
layer, the project layer, the cases, the OKF bundle and the project law
move out of the code repository into an external *control directory*;
fleet discovery, the four enforcement arms, the engine's root model, the
audit checks, the file-authority matrix, the eval corpus and member #0's
own governance all change shape. Novelty: the first edition that inverts
Step 3's founding assumption (owned files land in the target repo), and
the first machine-level state the legislator owns (`instances.yaml`).

**Spec type: feature.** Edition branch `bl/077-outer-only-control-directory`
(one MR per version). Sources: the 2026-08-29 pivot brainstorm (this
file's `## Clarifications`), BL-027's mechanics (absorbed), BL-044/045/052
(absorbed), the backlog revision in `research.md` §4, ADR-0007.

Companion cases this spec sizes but does not contain: **BL-078** (v26 —
inner→outer migration with rule reconciliation), **BL-079** (v27 —
sub-group layering), **BL-080** (the fleet-obs module and its three
fleet-obs-side cases), **BL-081** (semver and cross-tool version sync,
deferred).

## Boundary

**In:** the machine registry `~/.config/legislator/instances.yaml` and its
schema; the instance repository layout (group node + project trees); the
two-file untracked stub and its `.git/info/exclude` wiring; the `link` /
`restore` / `init-machine` / `instance create` / `project add` flows;
the sentinel behaviour of the global hooks and the opencode plugin; the
engine's two-root model; the registry-based legislated-repo predicate in
all four arms; fleet enumeration from the registry; audit re-basing plus
the new `unmigrated` finding; the D/A step classification and its
`sdd-lint` pass; the paired-MR convention in `pair-development.md`; the
module contract (`modules:`) as declaration only; member #0's manual
migration as the proof; the corpus re-cut into control+code pairs and the
new scenarios; three spikes; ADR-0007; Horizon and ontology rewrites.

**Out:** the automatic inner→outer migration and rule reconciliation
(BL-078); sub-group inheritance and stack-tag resolution (BL-079); any
fleet-obs or kbl code (BL-080 — fleet-obs's own cases: registry from an
external source without crashing on unknown keys, note roles from the
node manifest, `@import` expansion in adapters); the `legislator` CLI and
the env-attach projection (M3 — see `research.md` §2; a note, not a
deliverable); semver (BL-081); restructure/fidelity (BL-076, re-targeted
to v26); the analyzer binding (BL-067, unblocked by this spec's
config-is-code rule, implemented after v26).

## Doctrine this edition writes into law

- **Config is code.** The AI layer is what agents *read*; anything the
  build or CI *executes* (`.editorconfig`, `Directory.Build.props`,
  analyzer packages, generators) is code and travels through the code
  repository's own MR. The legislator may generate such files
  deterministically from stack law and propose them; it never owns them.
- **Native adjudication before agent judgement.** Where a stack's own
  analyzers, compilers or generators can enforce a clause, the law binds
  to them; an agent judges only what no deterministic tool can.
- **Default deterministic.** Every step of every flow is classed **D**
  (runs with no agent) or **A** (needs one). A is the exception and must
  state why determinism is impossible — not inconvenient.

## Requirements

### The machine registry

- **R-7701** — The legislator SHALL keep exactly one machine registry at
  `~/.config/legislator/instances.yaml` (override: `LEGISLATOR_REGISTRY`),
  a generated artifact written only by legislator commands and never
  hand-edited, carrying `machine`, `knowledge_root`, and `instances[]`,
  each instance carrying `id`, `root`, `modules[]` and `projects[]`, each
  project carrying `id` and `clones[]` (absolute paths).
- **R-7702** — WHEN a command needs the project for a working directory
  THEN it SHALL resolve by walking up to the git top-level and matching
  that path against every `clones[]` entry; no match SHALL be reported as
  "not linked", never treated as an inner-mode repository.
- **R-7703** — WHILE the registry is absent or unreadable, enforcement
  arms SHALL print one warning and fail open (no block), and verification
  jobs SHALL fail loud (non-zero, the reason on stdout).

### The instance repository

- **R-7704** — `legislator instance create <id> <root>` SHALL initialise a
  git repository at `<root>` from templates — `legislator.yaml` (group
  manifest: `legislatorVersion`, default `stacks`, `modules`), `README.md`,
  `rules/` (group law, empty), `.gitattributes` — and add the instance to
  the registry; the command SHALL be idempotent on re-run.
- **R-7705** — `legislator project add <instance> <id>` SHALL scaffold
  `<root>/<id>/` with the same tree an inner repository carried at v24 —
  `CLAUDE.md`, `AGENTS.md`, `.claude/rules/`, `opencode.json`,
  `docs/ai/{rules/**,engine.py,manifest.json}`, `docs/{okf,cases,adr,
  journal}/`, `docs/backlog.md`, `CHANGELOG.md` — so that BL-078's
  migration is a move, not a rewrite, and fleet-obs's path literals keep
  matching.
- **R-7706** — The instance repository SHALL carry no absolute local path
  and no fleet repository name in any tracked file: clone paths live only
  in the machine registry; projects are named by alias (`records.md`
  extends to the instance template).

### Link, stub, restore

- **R-7707** — `legislator link <clone> [--project <instance>/<id>]` SHALL
  identify the project from the clone's git remote (normalised) or root
  commit, require `--project` when the match is absent or ambiguous,
  append the clone path to `clones[]` (deduplicated), render the stub, and
  add the stub paths to `.git/info/exclude`.
- **R-7708** — The stub SHALL consist of exactly two untracked files in
  the clone: `CLAUDE.md` containing one `@<root>/<project>/CLAUDE.md`
  import line, and `opencode.json` whose `instructions` are absolute
  paths into the control tree; no `.claude/settings.json` is written —
  hooks are global.
- **R-7709** — `legislator restore [<clone>]` SHALL re-render the stub of
  every registered clone (or the one named) from the registry and SHALL
  change nothing when the stub already matches (idempotent).
- **R-7710** — WHEN a session starts in a registered clone whose stub is
  missing or differs from the rendered form THEN the global Claude Code
  `SessionStart` hook and the opencode plugin SHALL restore it and report
  the restoration in one line; WHEN the directory is not registered THEN
  they SHALL stay silent.
- **R-7711** — The sentinel SHALL emit one `context.loaded` event per file
  the stub's import chain names (the fleet-obs capture contract), so that
  the entry-overhead measurement sees the law an agent actually loads.

### The engine: two roots

- **R-7712** — Every engine job SHALL accept `--control <dir>` (the
  project tree in the instance repository; default: the engine's own
  `parents[2]`) and `--code <dir>` (the clone; default: resolved from the
  registry — the first existing clone, with a warning when several
  exist); when both are given the job SHALL read no machine state.
- **R-7713** — `anchors` SHALL resolve a path-anchor whose first segment is
  a top-level directory of the control tree against the control tree, and
  one whose first segment is a top-level directory of the code tree
  against the code tree; symbol-anchors SHALL be searched in the code
  tree only.
- **R-7714** — `okf-debt` SHALL take a document's newest commit from the
  control tree's git history and an anchored source's newest commit from
  the code tree's git history; a missing git in either tree SHALL be a
  loud finding, never a silent clean.
- **R-7715** — `sdd-lint` SHALL report a plan task that carries neither a
  `[D]` nor an `[A]` class marker, and an `[A]` task with no `why:`
  justification clause, as findings.
- **R-7716** — `audit` SHALL run its checks against the control tree and
  SHALL add one check — **unmigrated**: any of `docs/ai/`, `docs/okf/`,
  `docs/cases/`, `.claude/rules/`, a tracked `CLAUDE.md`/`AGENTS.md` with
  `@docs/ai/` imports, or a tracked `opencode.json` present in the code
  tree is a Warning naming BL-078 as the repair.

### The enforcement arms

- **R-7717** — The four arms (`guard_owned_files`, `guard_git_conduct`,
  `okf_sync_check`, `legislator-guard.ts`) SHALL decide "legislated" by
  registry resolution of the working directory (R-7702), sharing one
  registry reader per language with no third-party dependency.
- **R-7718** — `guard_owned_files` SHALL block agent writes under
  `<root>/<project>/docs/ai/**` of any registered instance by absolute
  path, whichever directory the session runs in.
- **R-7719** — `guard_git_conduct` SHALL apply its commit rules in both
  the clone and the instance repository, and SHALL warn when a commit in
  one half of a pair lands on a branch whose name the other half does not
  carry.

### Fleet and member #0

- **R-7720** — `tools/fleet.sh status|upgrade` SHALL enumerate instances
  from the registry and SHALL never scan the filesystem for manifests;
  the unit of delivery is the instance (one branch, one MR per instance
  per edition), and `upgrade` SHALL run without any agent.
- **R-7721** — This repository SHALL be migrated by hand under this
  edition's mechanics (registry entry, instance repository, moved trees,
  stub) before the edition is tagged; ADR-0004's deliver-to-self step
  becomes "upgrade the instance that holds the legislator project".

### Law text

- **R-7722** — `core/pair-development.md` SHALL state the paired-MR
  rule: a case branch `bl/NNN-…` exists in the clone and in the instance
  repository; the instance half merges first.
- **R-7723** — `core/verification.md` and `core/okf.md` SHALL describe the
  two-root anchor rule (R-7713) in place of the single-repository wording.
- **R-7724** — `docs/philosophy.md` §Horizon SHALL name BL-078 (fleet
  migration pending) instead of BL-027, and `docs/ontology.md` §Placement
  modes SHALL record outer as the only mode, dated.

### Verification substrate

- **R-7725** — `evals/setup_workspace.py` SHALL materialise each scenario
  as a control tree and a code tree under an isolated `HOME`
  (`CLAUDE_CONFIG_DIR`, `XDG_CONFIG_HOME`, `LEGISLATOR_REGISTRY` inside
  the workspace) and SHALL never touch the operator's registry.
- **R-7726** — The corpus SHALL include the scenarios `fresh`, `link`,
  `restore`, `upgrade`, `audit`, `audit-engine-absent`, `guards`,
  `two-root-engine`, `case-practice`, `fleet-obs-module` and three
  idempotency passes, every new assert shown red against the v24 law
  first (`evals/POLICY.md`).

## The hurting case

**GIVEN** a registered project whose clone was just re-created with
`git clone` (no stub, nothing in `.git/info/exclude`),
**WHEN** a developer opens Claude Code in that clone and starts a session,
**THEN** the SessionStart hook prints one line naming the restored stub,
`CLAUDE.md` and `opencode.json` exist untracked, `git status` is clean,
the first assistant turn already carries the project law (observable: the
`context.loaded` events name `<root>/<project>/CLAUDE.md` and its imports),
**AND** a session opened in an unregistered directory prints nothing and
loads nothing from any instance.

## Eval design (POLICY §3 — before the change)

Per scenario in R-7726: fixtures are control+code pairs tagged
`eval-base` on both; the file-authority matrix gains a tree column
(control / code), and `grade.py` derives protected/writable sets per tree.
Mutations (BL-063) cover the new asserts: stub byte-shape, exclude lines,
registry `clones[]` after double link, sentinel silence outside the
registry, two-root anchor resolution, `unmigrated` warning, guard blocks
in both trees, fleet enumeration without a filesystem scan.

## Spikes (exploration, before the link mechanism is implemented)

1. Claude Code — does `permissions.additionalDirectories` in user settings
   plus `CLAUDE_CODE_ADDITIONAL_DIRECTORIES_CLAUDE_MD=1` load a control
   tree without the CLI flag (the IDE-launch cost of M3)? What is the size
   limit of `SessionStart` `additionalContext`?
2. opencode — does `OPENCODE_CONFIG_DIR` resolve relative `instructions`
   against itself? How stable is `experimental.chat.system.transform`
   across 1.18.x?
3. The workspace-trust dialog on an external `@import` — behaviour under
   `claude -p` (headless); if it blocks, the Claude runner profile needs a
   pre-approval step.

Findings land in `research.md` §5 and decide whether M3 is v26/v27 or later.

## Clarifications

### Session 2026-08-29 (the pivot brainstorm)

- Q: Does inner mode survive? → **A: No.** Outer is the only mode; the
  fleet migrates (BL-078); member #0 is the first, by hand (R-7721).
- Q: What is the git unit? → **A: The instance** (a group of projects
  sharing rules) is one repository; projects are subtrees; a light group
  node (README + manifest + settings, no heavy dependencies). Sub-groups
  are v27 (BL-079); stacks stay orthogonal tags.
- Q: What stays in the code repo? → **A: Nothing tracked.** Untracked stub
  restored on every run; a launcher/CLI is a later evolution (M3 note in
  `research.md`).
- Q: Where is the truth about clone ↔ control? → **A: The machine
  registry only** (approach A), fail loud, `clones[]` per project; the stub
  is a projection.
- Q: kbl / fleet-obs — modules or contract? → **A: Modules of the
  legislator** (B, built modularly). Roles: legislator = structure,
  mappings, templates, version merge, pre-configuration and patches; kbl =
  the operational core of knowledge (dev-flow writes through it);
  fleet-obs = analytics over everything, machine-wide, shared across
  instances, learning "what is where" from the legislator.
- Q: fleet-obs registry — generated or amended? → **A: Wholly generated**
  from the machine registry plus a hand-written overlay for foreign
  sources; `knowledge_root` becomes the `global` source, `constitution:`
  points at `instances.yaml`, `sdd.skills` derives from the stage maps.
- Q: Analyzer binding vs "zero AI files in the code repo"? → **A: Config is
  code** (B); native analyzers/generators/compilers before agents, for
  every stack.
- Q: The link mechanism? → **A: Layered** — registry as truth; two-file
  stub as the delivery channel; global hooks/plugin as sentinel (restore,
  fail loud, `context.loaded`); env-attach (M3) reserved for the CLI.
- Q: Build order? → **A: v25 = this case; v26 = migration (BL-078); v27 =
  layering (BL-079); fleet-obs cases in parallel with v25; semver later
  (BL-081).**
