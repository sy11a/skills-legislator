# BL-077 — Research

Findings gathered 2026-08-29 during the pivot brainstorm. Local decisions
live here; the ones that outlive the case are ADR-0007.

## 1. The neighbours: kbl and fleet-obs as they are

**kbl** is pre-code: 23 files, all Markdown plus the legislator-owned
engine. What is fixed is the canon — *legislator writes the laws, kbl
keeps the fund, fleet-obs audits the practice* — and one hard rule: kbl
keeps no roots list of its own; roots come from the fleet-obs registry,
read-only. Designed, not built: typed cards in git (YAML front matter,
≤40 lines, ≥1 link) → DuckDB index → Obsidian projection; `store:slug`
addressing where `store` is a fleet-obs registry source id; planned
components `installer`, `migrator`, `core/` (linter), `consolidator`,
`kb-graph`, `projector`, `resolver`. Its canon names a fourth member, "the
upcoming task-solving framework" — the dev-flow tool.

**fleet-obs** is mature .NET in daily production (294 tests, hourly pulse
timer, 43 ADRs). It observes agent events (read / search / write / skill),
not repositories; bronze NDJSON → silver DuckDB → gold under
`<vault>/_generated/`. Its single source of truth about roots is a
hand-maintained per-machine `registry.yaml` (layers `global | framework |
local | skills`; exactly one `global` = the vault, mandatory). The
in-repo assumption is hardcoded in five places: `ConstitutionFleet` scans
`*/docs/ai/manifest.json`; `NoteRole` classifies by the literals
`/docs/ai/`, `/journal/`, `/superpowers/…`; the SDD panel's SQL matches
`/docs/cases/` and `/docs/superpowers/`; the adapters emit
`context.loaded` for `<cwd>/CLAUDE.md` and `<cwd>/.claude/rules/*.md`
without following `@import`; the glob root `Repository/*/docs`. Moving
the layer out without touching fleet-obs silently zeroes spec-before-code,
drops repos from the fleet, and lights "unregistered knowledge".

**Decision — one owner per fact.** *Where the law for this clone lives*
is placement: the legislator's registry. *Where the knowledge agents read
lives* is the fleet-obs registry — generated from the legislator's by the
fleet-obs module, with a hand overlay for foreign sources. Rationale: a
third hand-maintained roots list is exactly what kbl forbade itself;
fleet-obs already reads one legislator file (`skill/VERSION`, its
ADR-0038), so `constitution.instances` is the same pattern.
Alternatives rejected: legislator writes rows into the hand registry (two
writers, one file); fleet-obs scans instance roots itself (a second
discovery mechanism to keep in sync).

**The three derived fields.** `knowledge_root` → the `global` source
(the vault; fleet-obs hard-fails without one); `constitution:` →
`{versionFile, instances}` replacing `scanRoots`; `sdd.skills` → the
union of every node's `pre-plan` stage map plus `legislator` (today two
hand copies that can drift).

**Side finding (machine state, not the repo):** this machine's fleet-obs
capture hooks in `~/.claude/settings.json` and the opencode plugin symlink
point at the repository's pre-rename path and no longer resolve; capture
has been silent since the rename. Reported to the owner; not touched.

## 2. The link mechanism — facts and the comparison

Verified against the Claude Code documentation and the opencode 1.18.25
source (one empirical run against a nested-clone fixture):

| Capability | Claude Code | opencode |
|---|---|---|
| Context from directories **above** `.git` | `CLAUDE.md` yes (to filesystem root); `.claude/rules/` **no** (stops at the repo root) | **no** — hard stop at the git worktree for everything |
| Absolute import outside the repo | `@/abs` and `@~/…`, 4 hops, **trust dialog once per project** | no `@import`; `instructions: [/abs/path, /abs/dir/*.md]` yes |
| Attach an external config directory | `--add-dir X` + `CLAUDE_CODE_ADDITIONAL_DIRECTORIES_CLAUDE_MD=1` → CLAUDE.md, rules, skills from X | `OPENCODE_CONFIG_DIR=X` → config, instructions, commands, agents, plugins, skills |
| Global hook/plugin that knows cwd | user-level hooks receive `cwd`; `SessionStart` can inject `additionalContext` | global plugin receives `directory` + `worktree`; `experimental.chat.system.transform` injects |
| Path-scoped lazy rules | `.claude/rules/` with `paths:` — project tree only | none |

The machine already runs the "global arm routes by cwd" model: the
fleet-obs hooks sit in `~/.claude/settings.json`, `legislator-guard.ts`
in `~/.config/opencode/plugins/`.

| | M1 untracked stub | M2 clone inside the control tree | M3 env-attach | M4 global router injects law |
|---|---|---|---|---|
| Files in the clone | 2 untracked | 0 | 0 | 0 |
| Fidelity | imports are eager (lazy `paths:` lost); no skills | — | **full**, sanctioned by both harnesses | text only |
| Launch dependency | none (IDE, CLI, CI alike) | none | **launcher / shell-init** required; IDE launches miss the env | none |
| Failure when absent | `git clean` → silent no-law (cured by a sentinel) | opencode: **does not work** | env unset → silent no-law | unregistered cwd → loud by construction |
| fleet-obs sees the law | no (`@import` unexpanded) | — | likely (spike) | only if we emit |
| Harness specificity | low — any agent reading `CLAUDE.md`/`AGENTS.md` | — | two env contracts | high; opencode side is `experimental.*` |

**Decision — layered, one truth.** Registry as truth; M1 (two files,
`.claude/settings.json` dropped — hooks are global) as the delivery
channel; M4 in its degenerate form as the sentinel (restore, fail loud,
emit `context.loaded`); M3 reserved. Accepted loss: lazy path-scoped
rules — everything imported is eager; v27's layering keeps context small
by construction, which matters more.

**Note — the `legislator` CLI and M3.** The target evolution of the link
is env-attach: `CLAUDE_CODE_ADDITIONAL_DIRECTORIES_CLAUDE_MD=1 claude
--add-dir <root>/<project>` and `OPENCODE_CONFIG_DIR=<root>/<project>/.opencode`
— the only way to obtain the *full* projection (rules with `paths:`,
skills, commands, agents, plugins) with zero files in the clone. It needs
a launcher: the legislator as a console application with a shell-init
(`eval "$(legislator shell-init)"`, the direnv/mise pattern, exporting the
env per cwd from the registry) and/or `legislator run claude|opencode`.
When the CLI exists the stub is replaced by an env projection without an
architecture change — the truth is the registry; stub and env are two
renders of one fact. v25 carries only the spikes in the spec; their
result decides whether M3 is v26/v27 or later. BL-072 (the binary arm)
is the natural carrier.

## 3. Engine, guards, fleet — design notes

- **Two roots, no machine state inside the engine.** The engine takes
  `--control` and `--code`; the wrapper (SKILL.md, hooks) reads the
  registry. Keeps the engine a pure function over two trees and testable
  from fixtures alone.
- **Path-anchor routing by first segment** (spec R-7713). A path whose
  first segment exists as a top-level directory in both trees is resolved
  in the control tree first (documents describe their own bundle more
  often than code) and reported when it resolves in neither.
- **Guards depend on machine state for the first time.** Hence R-7703:
  registry absent → one warning, fail open (enforcement) — BL-069's
  policy; verification stays loud.
- **Fleet without an agent.** Step 3 is wholly deterministic since v24
  (`engine apply`); with instances enumerated from the registry the sweep
  is a script. The vendor-key incident of 2026-08-23 cannot recur for
  upgrade; the agent remains only for audit's model findings and
  proposals.
- **Project identity for `link`**: normalised git remote first, root
  commit second — the same scheme opencode uses for its project id, so
  the two agree on what "the same project" is across clones.

## 4. Backlog revision (2026-08-29)

Open items classified against the pivot; the backlog carries the status
changes, this is the record of why.

| Item | Verdict | Reason |
|---|---|---|
| BL-027 outer mode | absorbed → BL-077 | mechanics (sidecar, stub, exclude, restore, regeneration) are v25 verbatim; "outer as *a* mode", per-repo sidecar, the `enterprise-solo` profile and the fleet-obs root addition are superseded |
| BL-044 cross-harness parity | absorbed → BL-077 spikes | its questions become the spikes; divergences re-expressed against the control tree |
| BL-045 owned import index | absorbed → BL-077 | "one wiring line, written once" *is* the stub; the index and `opencode.json` move to the control tree |
| BL-052 loads outside two harnesses | absorbed → BL-077 | premise (1,122-byte `AGENTS.md` standing in for 25 KB) deleted; the question becomes "which agents follow an untracked stub"; Codex 32 KiB cap survives |
| BL-071 symlink vs import-line | obsolete | decided by construction: the stub is always an untracked real file; residue (control tree's own `AGENTS.md`/`CLAUDE.md` pair, E7 save/restore) noted in BL-077 |
| BL-067 analyzer binding | unblocked | config-is-code rule; implement after v26 |
| BL-076 restructure emitter | re-targeted → v26 | its subject is now BL-078's migration; the fidelity job is the migration's inventory |
| BL-008 plugin marketplace | absorbed → BL-077+ | "capabilities travel with the machine" is the singleton; premise sentence rewritten |
| BL-072 binary arm | strengthened | the singleton with a registry and modules *is* the arm's shape; carrier for M3 |
| BL-031, 039, 040, 046, 074 | unaffected | BL-040 more urgent (instance repos of work projects carry employer names by construction → R-7706) |
| BL-034 / ADR-0002 / ADR-0004 | re-targeted | member #0 becomes a project inside an instance (R-7721) |
| BL-064 riders, BL-016 §3 | absorbed → BL-077 / BL-078 | re-pathed to the instance tree |
| Edition-plan blocks (post-v21 ordering) | obsolete | superseded by "Agreed order after v24" |

Done items whose mechanics change (22 entries) cluster into six knots —
engine `ROOT`, the guards' legislated-repo test, fleet discovery by
manifest, the file-authority matrix's one-tree assumption, audit paths,
the inner-mode corpus — and those six are BL-077's sections.

## 5. Spike results

*(pending — filled when the three spikes in the spec run)*
