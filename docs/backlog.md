# Legislator — Backlog

Ideas and future work for the Legislator skill itself, discussed and refined before being added here. Each entry should be concrete enough to brainstorm from directly when picked up.

**Process per task:** each entry gets its own cycle — brainstorm → design spec → implementation plan → user approval → implementation → full e2e benchmark per `evals/README.md` (every entry below is a behavioral change) → VERSION bump only if `assets/rules/**` content changed.

---

## Roadmap — rot prevention & fleet feedback loop (agreed 2026-07-09)

Direction settled with the user:

- The **law stratum** stays strictly one-way (skill → repos, verbatim, versioned). It never adapts itself per-project — per-repo mutation of law is drift, the exact disease the manifest design cures.
- The **project stratum** (OKF, ADRs, backlog, journal, CLAUDE.md project sections) grows per-repo and is where rot actually happens. Legislator gains a *downward* capability to keep it healthy (audit → propose → approve → apply — never automatic restructuring) and an *upward* capability to feed proven project rules back into the central constitution (harvest — proposals only, user promotes).

Execution order (dependencies, not dates):

```
Rot-prevention chain:
BL-001 audit (+ BL-005a rotted fixture)   ← DONE 2026-07-09 (with BL-010)
   → BL-002 keep-markers                  ← DONE 2026-07-09 (with BL-011)
      → BL-003 harvest report             ← next: Wave 2
         → BL-005b restructure eval scenarios
            → BL-004 restructure flow      ← last: most destructive if wrong

Best-practices track (2026-07-09 large-codebases review, decisions user-approved):
BL-006 CLAUDE.md.tpl v2                   ← DONE 2026-07-09
BL-007 hooks plugin                       ← DONE 2026-07-09 (Wave 1 Track B)
   → BL-008 full plugin + marketplace     ← deferred until a 2nd human/machine
BL-009 steward practice                   ← DONE 2026-07-09 (docs-only, README)
```

### Implementation queue (agreed 2026-07-09, parallel where file-disjoint)

- **Gate 0 (user):** answer two pre-settled design questions — BL-002 keep-list
  manifest serialization; BL-007 write-guard mechanism.
- **Gate 1 (user):** review the two specs (BL-002+BL-011, BL-007) in one pass.
- **Wave 1 (parallel):**
  - *Track A — main tree:* BL-002 + BL-011 riding: plan → SDD → benchmark.
  - *Track B — isolated worktree, background agent:* BL-007 full
    implementation from its approved spec/plan; verification = hook behavior
    tests (no e2e benchmark — plugin/ only, no skill/SKILL.md changes); lands
    as a branch, merged after Track A (only overlap: backlog status lines).
  - *Track C — background, no repo writes:* options memo for BL-003's open
    design question (where harvest candidates durably land), to speed its
    brainstorm.
- **Wave 2 (serial, after Track A):** BL-003 brainstorm (using the memo) →
  full cycle. *(Wave 1 completed 2026-07-09: Track A benchmark v7.2 58/58,
  Track B merged, Track C memo delivered — Wave 2 is next.)*
- **Wave 3 (serial, last):** BL-005b + BL-004 as one combined cycle — eval
  scenarios land before the restructure implementation, same pattern as
  BL-001+005a.
- **Dormant:** BL-008 until a 2nd human/machine.

Rationale: BL-002→003→005b→004 is a true dependency chain (audit check 10
reads the keep schema; the restructure eval asserts BL-004's spec); BL-007 is
file-disjoint from all of it. Background agents can't pause to ask questions,
so anything dispatched to Track B must have its design fully settled at
Gate 0/1 — that ordering is what makes the parallelism safe.

Personal machine to-do (not a Legislator task): adopt the official C# LSP
plugin (`csharp-ls`) locally — symbol-level navigation for the dotnet fleet;
independent of this repo entirely.

---

## BL-001 — Audit mode: read-only rot detection for the project-owned AI layer

**Status: DONE 2026-07-09** — full cycle (spec `docs/superpowers/specs/2026-07-09-audit-mode-design.md`, plan `docs/superpowers/plans/2026-07-09-audit-mode.md`, commits 5840d54..3f01b12, VERSION stays 7, benchmark `evals/benchmarks/v7.1.md`: 45/45 + zero-diff idempotency; audit found all 9 planted defects on its first live run; BL-005a delivered with it; BL-010 riding items shipped). Follow-ups: BL-011.

**What:** a new invocation path in SKILL.md (user asks to "audit"/"check" the AI layer, no scaffolding intent). Inventories `docs/**` + `CLAUDE.md` and checks structural invariants: every `@import` in CLAUDE.md resolves; every OKF-index link resolves; no orphan `.md` under `docs/` (reachable from no index/import); no unresolved `{{TOKEN}}`s outside `docs/adr/template.md`; journal recency vs. recent git activity; foreign AI-layer structures detected and listed (`.cursorrules`, `agents.md`, non-standard ADR/plans dirs, wiki folders); keep-listed files (BL-002) are actually linked. Output: a severity-ranked rot report. **Zero writes** — `git status` before and after must be identical.

**Why:** rot in the project stratum is invisible today; the constitution can restore law but walks past a decaying knowledge layer. Visibility is 80% of prevention, and a read-only mode ships with zero risk.

**Done when:** on the rotted fixture (BL-005a) the report names every planted defect; on a clean freshly-legislated repo it reports clean; no writes in either case.

## BL-002 — Keep-markers: manifest `keep` list for do-not-touch project artifacts

**Status: DONE 2026-07-09** — full cycle (spec `docs/superpowers/specs/2026-07-09-keep-markers-design.md`, plan `docs/superpowers/plans/2026-07-09-keep-markers.md`, commits f6f0f8f..9ab7e3e, VERSION stays 7, benchmark `evals/benchmarks/v7.2.md`: 58/58 + zero-diff idempotency on both fresh and the new permanent `idempotency:upgrade` scenario; keep carry-forward + prompt-driven add + pinned serialization all verified live; BL-011 riders shipped with it).

**What:** `docs/ai/manifest.json` gains a `keep` section — entries of `{path, reason}` naming ultra-specific project artifacts that work as-is and must never be restructured. SKILL.md's pinned serialization is extended (order, formatting) so idempotency holds; upgrade runs must carry old manifests without a `keep` key forward deterministically (default `[]`). Audit (BL-001) warns when a kept file is linked from nowhere — protected content must stay wired into the layer, not become an orphan. Restructure (BL-004) treats kept paths as immovable.

**Why:** the user's requirement verbatim: project-specific things that work well are marked untouchable *and* linked effectively into the structure — protection without orphaning.

**Done when:** idempotency eval stays zero-diff with a populated `keep` list; keep entries survive upgrade runs; audit flags an unlinked kept file on the rotted fixture.

## BL-003 — Harvest report: propose promoting proven project rules into the constitution

**What:** during any run and any audit, scan project-owned content (CLAUDE.md project sections, OKF conventions) for statements phrased as enforceable law (imperative, diff-checkable, not already covered by an owned rule) and add a **"Constitution candidates"** section to the Step 7 report: each candidate quoted verbatim with its source location. Proposals only — never writes. The user's operating loop (documented in README): review candidates → add to `assets/rules/**` centrally → bump VERSION → re-run `/legislator` across repos.

**Why:** this is the upward feedback loop — the system self-improves at fleet level while law stays one-way. What one repo learns, the constitution can adopt, deliberately, once.

**Open design question (settle in this task's brainstorm):** where candidates durably land. The Step 7 report is ephemeral chat output — unread candidates vanish with the session, and fleet-level review wants them collected across repos. Options to weigh: report-only (current sketch), a persisted file in the target repo (adds noise to that project), or the user pasting survivors into this backlog (manual but keeps the legislator repo as the single collection point). Writing into the legislator repo directly from a run is off the table — it breaks "never write outside the target repo".

**Done when:** on the legacy-migration fixture, harvest lists the hand-written architecture constraints as candidates with correct source locations; zero writes attributable to harvest.

## BL-004 — Restructure flow: approval-gated doctor for rotted or foreign AI layers

**What:** consumes an audit report (BL-001) and produces an explicit restructuring plan: per artifact, current location → target location in the standard layout, with content carve-out per `references/migration.md` — which this task extends from "CLAUDE.md only" to the whole layer. Strict protocol: **diagnose → propose → approve → apply.** Nothing moves without the user approving the plan; conflicts go to the decision gate; kept paths (BL-002) are immovable; content fidelity is absolute — every sentence of project content must survive somewhere (grep-verifiable), never silently dropped.

**Why:** the actual repair capability for repos whose AI layer (legislator-built or foreign) has gone chaotic. Deliberately last: it is the most destructive feature if wrong, so it builds on audit (visibility), keep-markers (protection), and BL-005b (fidelity evals) — never as a side effect of a normal upgrade run.

**Done when:** the rotted fixture is restructured with zero content loss (every planted sentence still greppable in the result), the kept file untouched in place, the decision gate triggered on the planted conflict, and a second run produces a zero diff.

## BL-005 — Eval coverage: rotted-layer fixture + audit/restructure scenarios

**What:** a new `rotted-layer` fixture with planted, greppable defects: broken `@import`, orphan doc, stale OKF-index link, unresolved `{{TOKEN}}`, a `.cursorrules` file, an unlinked keep-listed file, a journal that stopped while commits continued, one deliberate conflict with an owned rule. `grade.py` gains two scenarios: **audit** (report names each planted defect; `git status` unchanged) and **restructure** (fidelity greps for every planted sentence + idempotency zero-diff).

**Why:** the repo's CLAUDE.md mandates benchmarking for behavioral changes; audit and restructure are the highest-risk features yet and must not ship blind. Split delivery: BL-005a (fixture + audit scenario) is built *with* BL-001; BL-005b (restructure scenario) lands *before* BL-004 implementation starts.

## BL-006 — CLAUDE.md.tpl v2: ambient codebase map, boundaries, glossary pointer

**Status: DONE 2026-07-09** — implemented via full cycle (spec
`docs/superpowers/specs/2026-07-09-claude-md-tpl-v2-design.md`, plan
`docs/superpowers/plans/2026-07-09-claude-md-tpl-v2.md`, commits
85213d1..87d428d, VERSION 7, benchmark `evals/benchmarks/v7.md`: 33/33 +
zero-diff idempotency, final whole-branch review: ready). Follow-ups spun
into BL-010.

Decisions settled with the user (2026-07-09, from the large-codebases best-practices review):

**What:**

1. **Codebase map — an OKF artifact, made ambient via @import.** New scaffolded
   file `docs/okf/codebase-map.md` (create-if-missing): a table of top-level
   directories with one-line descriptions, derived from the actual repo tree at
   scaffold time and confirmed with the user. `CLAUDE.md.tpl` gains one line:
   `@docs/okf/codebase-map.md`. Rationale: the map is knowledge → single source
   of truth in OKF (no duplication across the layer, per the user's explicit
   concern); the @import makes it ambient in every session, which is what
   actually prevents navigation wandering. Freshness is covered by the existing
   okf.md sync law; BL-001 audit later adds a "map entries vs. actual
   directories" check.
2. **Boundaries — split by stratum.**
   - Law (core rule file, propagates to every repo on re-run, VERSION bump):
     "Never hand-edit `docs/ai/rules/**` — these files are machine-managed by
     the Legislator and overwritten on every run; change them centrally."
   - Project-owned (`{{BOUNDARIES}}` section in CLAUDE.md.tpl, filled at
     scaffold): repo-specific no-touch zones — generated dirs, legacy areas,
     migration output.
3. **Domain glossary — pointer only.** Scaffold a thin `docs/okf/glossary.md`
   (create-if-missing) and add a pointer line (not an @import — jargon lookup
   is on-demand, no ambient budget spent).

**Consequences to handle in the plan:** new templates registered in SKILL.md
Step 4's table (static checks enforce referenced<->present); `SCAFFOLD_ARTIFACTS`
in `evals/grade.py` extended; migration mode must respect an existing map/glossary
(create-if-missing as always); VERSION bump (rule content changes); full e2e
benchmark per evals/README.md.

**Done when:** fresh scaffold produces map+boundaries+glossary with no unresolved
placeholders; the map @import resolves; upgrade run propagates the new law line
to an already-legislated repo; benchmark shows no regression.

## BL-007 — Hooks plugin: the deterministic enforcement arm of the constitution

**Status: DONE 2026-07-09** — built per spec `docs/superpowers/specs/2026-07-09-hooks-plugin-design.md` in an isolated worktree (Wave 1 Track B), reviewed Approved, merged as 6f61774 (+README follow-up 3591325). `plugin/` ships `legislator-hooks` v0.1.0 (write-guard on `docs/ai/rules/**`, format-on-edit, OKF-sync stop hook); `evals/check_hooks.py` 26/26. Gate 0 decision honored: Edit/Write-family guard only, the `cp` asymmetry is the mechanism; manifest deliberately unguarded. Local install + BL-008 marketplace packaging remain out of scope.

Decisions settled with the user (2026-07-09): hooks are law's enforcement arm
(CLAUDE.md/rules are advisory; hooks are guaranteed), and they are delivered
**via a plugin, not `.claude/settings.json`** — a settings fragment would break
the clean owned/project split (merge problem in a user-edited file); a plugin
bundles hooks versioned and cleanly per machine.

**What:** a new plugin package (lives in this repo, e.g. `plugin/`, becoming the
skeleton BL-008 extends) shipping three hooks:

1. **PreToolUse write-guard on `docs/ai/rules/**`** — blocks Edit/Write to owned
   files in legislated repos, making per-repo law drift mechanically impossible
   instead of merely detectable.
2. **PostToolUse format-on-edit** — `dotnet format` on `.cs`, prettier on the
   Aurelia/TS side; per-file, fast, non-blocking (`; exit 0`). Consequence:
   mechanically-enforced style rules can then be *deleted* from
   `coding-standards.md` (deletion habit — law shrinks when a machine takes over
   enforcement).
3. **Stop-hook OKF-sync check** — session end: if `src/**` changed but
   `docs/okf/**` did not, exit 2 with a reminder. Runtime rot prevention — the
   enforcement arm of okf.md's sync law.

**Open design question (settle in this task's brainstorm):** the write-guard
must not block the Legislator's own runs — the skill updates owned files via
`cp` (Bash), which a PreToolUse guard on Edit/Write doesn't intercept; decide
whether that asymmetry is the mechanism (guard file-editing tools only) or
whether an explicit escape (env flag) is needed, and whether Bash writes to
`docs/ai/rules/**` should also be guarded against non-legislator use.

**Why:** law without deterministic enforcement is advisory; hooks close the gap.
Installation is build-time scaffolding — legislator's jurisdiction (unlike
runtime agents, which stay in the separate master-agent skill).

**Done when:** in a legislated repo with the plugin installed, a hand-edit to an
owned rule file is blocked; an edited `.cs` file comes out formatted; a session
that touches `src/` without touching `docs/okf/` gets the sync reminder; a
legislator upgrade run still completes (guard does not break owned-file
updates).

## BL-008 — Package the toolchain as a plugin in a private marketplace

**What:** extend BL-007's plugin skeleton into the full capability bundle:
the legislator skill itself, the BL-007 hooks, LSP configs (C# `csharp-ls`,
TypeScript for Aurelia), and MCP configs — **scope decided by the user:
Microsoft Learn Docs MCP, context7, and a read-only DB MCP only; explicitly NO
GitHub MCP and NO ticketing MCPs** (no automation against external
sources/ticketing). Distributed via a private marketplace repo
(`/plugin marketplace add <repo>`).

**Why:** the constitution travels with each repo (committed `docs/ai/**`);
capabilities travel with the machine (plugin). Complementary strata — plugin
form kills tribal-knowledge setup drift the moment a second human or second
machine appears.

**Trigger:** deferred until that second human/machine exists. Until then the
symlink install stays.

**Done when:** on a clean machine, `marketplace add` + `plugin install` yields
working `/legislator`, active hooks, LSP navigation in a dotnet repo, and the
three approved MCPs — with no manual setup steps beyond the two commands.

## BL-009 — Steward practice: constitution review cadence + model-release benchmark

**What:** document in README a standing "Steward duties" section: (1) every
3–6 months or after a major model release, review each rule with the question
*preference or compensation?* — delete compensations (instructions that padded
over a limitation models no longer have; they start to actively constrain);
(2) after each major model release, re-run the eval benchmark unchanged and
record `evals/benchmarks/v<N>-<model>.md` — pass-rate/token deltas measure
empirically whether the constitution helps or hinders the new model; (3) triage
harvest candidates once BL-003 ships; (4) the deletion habit — a rule that
changed no review outcome in months is either internalized (delete) or ignored
(delete or start enforcing; decide, don't let it linger).

**Why:** instructions written for today's model can work against a future one
(the article's core maintenance insight). The eval suite doubles as the
measurement loop nobody else has.

**Status: DONE 2026-07-09** — docs-only, executed without a full cycle;
"Steward duties" section added to README.md.

## BL-010 — Migration-mode v2 wiring + two SKILL.md/migration.md wording touch-ups

Small follow-ups from BL-006's v7 benchmark and final review — ride along
with the next cycle that edits `skill/**` anyway (its mandatory benchmark
covers them; not worth a full e2e run on their own):

1. **Migration mode should write the full v2 CLAUDE.md wiring directly.**
   The v7 run left the `@docs/okf/codebase-map.md` import, `## Boundaries`
   section, and glossary pointer as Step 7 proposals even though migration
   mode rewrites CLAUDE.md anyway — friction the rewrite could absorb.
   Requires updating `references/migration.md` §1's import-block description.
2. **`references/migration.md` "mirroring `CLAUDE.md.tpl`'s import block"
   wording is now ambiguous** — the tpl's import block ends with the
   codebase-map import; qualify the sentence so a future run doesn't inline
   it by accident (moot if item 1 makes inlining the intended behavior —
   settle both together).
3. **SKILL.md Step 4 glossary row omits its placeholders** — cosmetic
   inconsistency: the codebase-map row names `{{PROJECT_NAME}}`/`{{TODAY_ISO}}`,
   the glossary row doesn't though its template carries both.

(The fourth final-review finding — the grader's dead unresolved-token scan —
was fixed immediately in the eval layer, verified with a planted-token
negative test; no benchmark required for `evals/**`.)

## BL-011 — Audit follow-ups from the v7.1 final review (skill-file items)

**Status: DONE 2026-07-09** — rode the BL-002 cycle as planned: item 1 fixed generically (inline-code path mentions count as references in checks 7 and 10; locked in by the audit scenario's absent-markers), item 2 resolved (missing-keep note goes to the Info section, exact line pinned in SKILL.md), item 3 fixed (migration.md glossary quote byte-identical to CLAUDE.md.tpl). Benchmark `evals/benchmarks/v7.2.md`.

Ride along with the next benchmarked `skill/**` cycle (BL-002 keep-markers is
the natural vehicle — it edits SKILL.md's audit check 10 anyway):

1. **Orphan check flags the constitution's own hub files (Important).** Check
   7 counts only markdown links and `@import`s as references, but in a
   healthy freshly-legislated repo `docs/okf/index.md` is referenced only as
   backtick code (in okf.md's rule text) and `docs/okf/glossary.md` only via
   CLAUDE.md's pointer *bullet* — so a strict clean-repo audit reports both
   hubs as Warning orphans. Fix deliberately: either exempt the two hub files
   by name or count inline-code path mentions as references. Add a clean-repo
   audit expectation to the eval when fixing (the deferred clean-audit
   scenario, or a targeted assertion).
2. **Check 10's "keep-list: not present" note has no slot in the fixed report
   format** — define where it goes (Info section vs. clean-checks line) so
   agents place it consistently.
3. **migration.md §1 quotes the glossary pointer line without its leading
   `- ` bullet/backticks** — align the quoted line with CLAUDE.md.tpl's exact
   text so migrated and fresh CLAUDE.mds don't drift textually.

(The review's two eval-layer minors — weak grep markers, commit-count-based
zero_writes — were fixed immediately with a positive + amend-HEAD negative
test; no benchmark needed for `evals/**`.)

---

## Note — master-agent / mini-agent routing system is a separate skill, not a Legislator feature

A master-agent that reviews an incoming request in a project and decides whether to route it to an existing project-local mini-agent (`.claude/agents/<name>.md`) or create a new fine-grained specialized one (task-appropriate model, scoped MCPs) is being built as its **own, separate skill** — not as part of Legislator. Rationale: Legislator is build-time scaffolding (runs occasionally, evolves via VERSION/manifest); request routing is a runtime concern with its own lifecycle. Folding both into one skill would blur SRP.

Legislator has **no involvement** here — no convention hook, no `.claude/agents/` scaffold, nothing. The separate skill owns its own convention entirely: it injects whatever `.claude/agents/` setup it needs directly into a repo when applied, independent of Legislator. (An earlier version of Legislator scaffolded a placeholder `.claude/agents/README.md` via `agents-README.md.tpl` — that has been removed; scaffolding it was itself scope creep into the other skill's responsibility.)
