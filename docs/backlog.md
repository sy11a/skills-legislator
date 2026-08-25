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
      → BL-003 harvest report             ← DONE 2026-07-10 (with BL-012)
         → BL-005b restructure eval scenarios   ← DONE 2026-07-10 (with BL-004)
            → BL-004 restructure flow      ← DONE 2026-07-10 — CHAIN COMPLETE

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
  Track B merged, Track C memo delivered. Wave 2 completed 2026-07-10:
  benchmark v7.3 63/63 — Wave 3 is next.)*
- **Wave 3 (serial, last):** BL-005b + BL-004 as one combined cycle — eval
  scenarios land before the restructure implementation, same pattern as
  BL-001+005a. *(Completed 2026-07-10: benchmark v7.4 81/81 — the queue is
  done; only dormant BL-008 remains.)*
- **Dormant:** BL-008 until a 2nd human/machine.

Rationale: BL-002→003→005b→004 is a true dependency chain (audit check 10
reads the keep schema; the restructure eval asserts BL-004's spec); BL-007 is
file-disjoint from all of it. Background agents can't pause to ask questions,
so anything dispatched to Track B must have its design fully settled at
Gate 0/1 — that ordering is what makes the parallelism safe.

## Edition plan (agreed 2026-08-22, after v17 closed at 177/177)

- **v18 — rights and names.** One theme: every fact about who may write
  what, and every concept's single name, stated in exactly one place.
  - **BL-038** (anchor) — the file-authority matrix; `grade.py` derives its
    protected/writable sets from it, `selftest:derivation` asserts the
    derivation.
  - **BL-030** — the constitution disambiguation sweep; same prose surface
    as BL-038 (SKILL.md, `references/**`), so the two ride one cycle rather
    than colliding in two — shipped in v17, residue only.
  - **BL-028** — manifest key `profiles` → `stacks`, with the legacy-key
    upgrade path and a legacy-manifest fixture — shipped in v17, residue only.
  - **BL-031** — leaves the cycle (docs-only).
- **v19 — file-authority residue.** **BL-041** alone (decided 2026-08-23,
  departing from the residue-rides-the-next-cycle precedent of BL-011…025):
  the wall's blind spot and the grader hardenings are worth landing *before*
  the edition that writes the most new prose, and BL-033's own entry asks
  for an empty landing. No fleet sweep for v19 on its own — upgrade is
  cumulative, the fleet moves 18 → 20 in one pass when BL-033 lands.
- **v20 — OKF v2 and the one engine.** **BL-033** (docs half): the OKF v2
  decomposition and the anchored class, for the reason its own entry gives:
  a transform of what "docs" means across the fleet must not share a
  landing with anything else.
- **v21 — generated baseline and the spec/plan linter.** **BL-043** alone:
  the baseline generator, the linter, and the `generated` role class's
  first member.
- **After v21:** BL-034 (self-legislation, depends on OKF v2 *and* the
  generated class, not on an edition number), then BL-027 (outer placement
  mode, needs its own design cycle first).
- **Cross-harness context control (raised 2026-08-23, while v20 was in
  flight):** BL-044 (research, runs any time and feeds the other two) →
  BL-045 (the owned import index) → BL-046 (the context-scope law, its own
  edition). BL-046 is recommended **before** BL-043 — see its entry.
- **Spikes (raised 2026-08-23, toward the framework goal):** BL-047 (the
  decision inventory — what is still model-decided), BL-048 (per-job model
  floor), BL-049 (report derivability), BL-052 (does the constitution load
  at all outside Claude Code and opencode), and **BL-054** (raised
  2026-08-24: full interchangeability of the two engine profiles as a design
  invariant — the axes BL-044 and BL-052 do not cover). Each is time-boxed,
  produces an answer rather than code, and sizes the cases that follow it.
- **Off the edition track:** BL-035 (docs-only, runs any time), BL-039 and
  BL-040 (repository-level operations, each wanting a deliberate moment),
  BL-050 and BL-053 (the two instruments — the eval runner and the fleet
  tool, each able to report success while having done nothing) — both
  **DONE 2026-08-24**.

## Agreed order after v21 (2026-08-25)

Settled with the owner at the close of the v21 cycle, after the fleet sweep.

1. **BL-060, designs D1+D2 — filed as BL-062, DONE 2026-08-25.** The
   `unmeasured` verdict and honest arithmetic. Grader only: no `skill/`
   change, no VERSION, no benchmark. **First, and the reason is not
   preference.** BL-060 measured that a third of the audit
   scenario passes with no report at all, and that run history cannot identify
   a useless assert even in principle. Until that is fixed, every corpus number
   is inflated by an unknown amount — and fixing law against a ruler that
   overstates itself is building on an unverified instrument.
2. **BL-057 with BL-043 as edition v22** — the false Criticals from audit check
   2, alongside the generated baseline and the spec/plan linter. Check 2 yields
   fourteen false Criticals in this repository; `Critical` is the severity that
   means "the layer is broken", and fourteen false ones train a reader to skim
   exactly the section that must never be skimmed.
3. **The instrument backlog, as one batch** — BL-061 (the false `FAIL`),
   BL-056 (`status` reads the working tree), BL-055 (`fleet.sh` cannot see its
   own repository), BL-059's prevention half. Each is small alone; together
   they are four places where a tool misinforms its operator, which is this
   repo's most expensive recorded defect class.

D3 (the mutation manifest) and D4 (mechanical pruning) follow D1+D2 — no assert
may be named for deletion until the mutation pass can name it by measurement.

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

**Status: DONE 2026-07-10** — full cycle (spec `docs/superpowers/specs/2026-07-09-harvest-report-design.md`, plan `docs/superpowers/plans/2026-07-09-harvest-report.md`, commits 3510c07..7d3f36d, VERSION stays 7, benchmark `evals/benchmarks/v7.3.md`: 63/63 after one mid-benchmark fix — the migration run drifted the section heading to H2, SKILL.md now pins it byte-for-byte, re-run 19/19). Design user-settled: report-only + user pastes survivors here; `<!-- legislator: not-law -->` suppression; migration/upgrade/audit modes. BL-012 riders shipped with it. The open design question below is resolved accordingly.

**What:** during any run and any audit, scan project-owned content (CLAUDE.md project sections, OKF conventions) for statements phrased as enforceable law (imperative, diff-checkable, not already covered by an owned rule) and add a **"Constitution candidates"** section to the Step 7 report: each candidate quoted verbatim with its source location. Proposals only — never writes. The user's operating loop (documented in README): review candidates → add to `assets/rules/**` centrally → bump VERSION → re-run `/legislator` across repos.

**Why:** this is the upward feedback loop — the system self-improves at fleet level while law stays one-way. What one repo learns, the constitution can adopt, deliberately, once.

**Open design question (settle in this task's brainstorm):** where candidates durably land. The Step 7 report is ephemeral chat output — unread candidates vanish with the session, and fleet-level review wants them collected across repos. Options to weigh: report-only (current sketch), a persisted file in the target repo (adds noise to that project), or the user pasting survivors into this backlog (manual but keeps the legislator repo as the single collection point). Writing into the legislator repo directly from a run is off the table — it breaks "never write outside the target repo".

**Done when:** on the legacy-migration fixture, harvest lists the hand-written architecture constraints as candidates with correct source locations; zero writes attributable to harvest.

## BL-004 — Restructure flow: approval-gated doctor for rotted or foreign AI layers

**Status: DONE 2026-07-10** — full cycle with BL-005b (spec `docs/superpowers/specs/2026-07-10-restructure-flow-design.md`, plan `docs/superpowers/plans/2026-07-10-restructure-flow.md`, commits 4927372..4d2dc05, VERSION stays 7, benchmark `evals/benchmarks/v7.4.md`: 81/81 after one fixture-bait fix — the audit's harvest bait was defensibly rejectable as instance data; reworded, re-run 17/17). First live run: 7 items applied, planted conflict decision-gated and left open, kept file immovable, fidelity verified; second run a zero-write no-op. New `references/restructure.md`; BL-012 Wave 3 rider shipped with it.

**What:** consumes an audit report (BL-001) and produces an explicit restructuring plan: per artifact, current location → target location in the standard layout, with content carve-out per `references/migration.md` — which this task extends from "CLAUDE.md only" to the whole layer. Strict protocol: **diagnose → propose → approve → apply.** Nothing moves without the user approving the plan; conflicts go to the decision gate; kept paths (BL-002) are immovable; content fidelity is absolute — every sentence of project content must survive somewhere (grep-verifiable), never silently dropped.

**Why:** the actual repair capability for repos whose AI layer (legislator-built or foreign) has gone chaotic. Deliberately last: it is the most destructive feature if wrong, so it builds on audit (visibility), keep-markers (protection), and BL-005b (fidelity evals) — never as a side effect of a normal upgrade run.

**Done when:** the rotted fixture is restructured with zero content loss (every planted sentence still greppable in the result), the kept file untouched in place, the decision gate triggered on the planted conflict, and a second run produces a zero diff.

## BL-005 — Eval coverage: rotted-layer fixture + audit/restructure scenarios

**Status: DONE 2026-07-10** — BL-005a shipped with BL-001 (v7.1); BL-005b (restructure scenario + `idempotency:restructure`, both permanent) shipped with BL-004 (v7.4). The suite now runs five scenarios + three idempotency passes.

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

## BL-012 — Keep-list follow-ups from the v7.2 final review (skill-file items)

**Status: DONE 2026-07-10** — rode the BL-003 cycle as planned (items 1 and 2
shipped in commit 3510c07; item 3 was bookkeeping-only). Benchmark
`evals/benchmarks/v7.3.md`.

Ride along with the next benchmarked `skill/**` cycle (BL-003 harvest is the
natural vehicle — it edits Step 7 anyway):

1. **Refused keep requests can vanish from the report (Important).** Step 3.6
   says a refused keep add is reported in Step 7, but Step 7's Keep list
   section triggers "only when this run changed the `keep` list" — a refusal
   changes nothing, so a run whose only keep event is a refusal omits the
   section. Fix: trigger on "changed the keep list *or refused a keep
   request*", and widen the parenthetical to cover both refusal reasons
   (path missing; path is an owned file).
2. **SKILL.md doesn't mandate the Bash copy path the write-guard exempts
   (Minor, cross-track seam).** Step 3.1 says "byte-for-byte copy operation
   (e.g. `cp`)" — an agent that satisfies this via the Write tool gets
   blocked by `legislator-hooks`' guard mid-run. Fix: "via a Bash copy
   (`cp`) — never the Write/Edit tools".
3. **Bookkeeping:** the keep-markers plan's "Deviations from spec" list has a
   fourth, undocumented deviation — owned files declared not-keepable in
   Step 3.6 (sanctioned in spirit by the spec's "Kept ≠ owned" decision).
   Recorded here since executed plans are never rewritten.

(The review's other three findings — vacuously-passable absent-markers,
stale plugin docstring step number, README repo-gate overclaim — were fixed
immediately in the eval/plugin layers, commits ea15fac + follow-up; the
strengthened markers were proven against the recorded v7.2 audit report.)

Rider for the next benchmarked `skill/**` cycle (Wave 3), from the v7.3 final
review: SKILL.md's candidates-section placement anchors ("after the Keep
list section, before Health") are both absent in the common migration
case — append "(when those sections are absent, make it the report's last
section)"; also pin the Keep list section's heading level.
*(Shipped 2026-07-10 in the BL-004 cycle, benchmark v7.4.)*

## BL-013 — Restructure follow-ups from the v7.4 final review (skill-file items)

**Status: DONE 2026-07-10** — rode the BL-014 cycle: all four items shipped
(fidelity-pass scoping + fix-deletion exemption, restructure runs Step 0
first, restructure.md §5 definition reword, pinned decision-item shape).
Benchmark `evals/benchmarks/v8.md`.

Ride along with the next benchmarked `skill/**` cycle (there is no queued one
— whichever behavioral change comes first carries these):

1. **Fidelity-pass law contradicts the `fix` action when read literally
   (Important).** SKILL.md's apply step demands every line of every file
   "moved, merged, or edited" survive greppable, and "every miss blocks its
   item" — but `fix` deletes lines by design (dangling imports, stale map
   rows, dead links) and restructure.md §3 replaces restating boilerplate.
   The v7.4 run passed on the charitable reading (tracked move/merge sources
   only). Fix: exempt deletions that are the approved item's stated purpose,
   or scope the inventory to move/merge sources. Same literal-reading drift
   class as the H2 heading and the ambiguous bait.
2. **Restructure skips Step 0's dirty-tree warning (Minor)** — it writes, so
   a dirty tree mixes user changes into the applied diff. Fix: "run Step 0
   first" in the Restructure section.
3. **restructure.md §5's AI-layer parenthetical reads as a forbidden-targets
   list (Minor)** — reword to "…outside the AI layer (the AI layer being:
   CLAUDE.md, `docs/**`, root-level foreign AI configs)".
4. **`decision` items don't fit the pinned `<current> → <target>` item shape
   (Minor)** — SKILL.md should carry the spec's decision-item example so
   agents don't improvise.

Bookkeeping from the same review (no file changes owed): the intro-routing
edit (e59a4e9) was a plan omission repaired mid-cycle — recorded here since
executed plans are never rewritten; and the restructure fixture + scenario
have not yet met post-bait-fix (4d2dc05) — ungraded surface only, covered
naturally by the next benchmark.

## BL-014 — Project rules: `.claude/rules/` as the project-law home (constitution v8)

**Status: DONE 2026-07-10** — full cycle (spec `docs/superpowers/specs/2026-07-10-project-rules-design.md`, plan `docs/superpowers/plans/2026-07-10-project-rules.md`, commits 43cc645..4966342, **VERSION 7→8**, benchmark `evals/benchmarks/v8.md`: 87/87 after two mid-benchmark catches — the tpl was missing the project-rules import (spec gap the upgrade run's own report exposed; fresh re-run 14/14) and suppression narration leaked a silenced statement (SKILL.md now mandates silent skipping; audit re-run 18/18). Migration carves law to `.claude/rules/`, audit check 11 live, restructure decision-gated both planted conflicts. BL-013 riders shipped with it. **Fleet action: run `/legislator` in each downstream repo to deliver v8.**

**What:** project-specific rules ("every feature ships behind a feature
toggle") get a dedicated home instead of bloating CLAUDE.md, using Claude
Code's native `.claude/rules/` (auto-loaded at CLAUDE.md priority, `paths:`
frontmatter for scoping — per the official memory docs). New core rule
`core/project-rules.md` teaches every agent the convention (**first
`assets/rules/**` change: VERSION 7→8**); Step 4 scaffolds the directory;
migration carves law-shaped legacy content there (instance data stays in
CLAUDE.md); harvest scans it; audit check 11 (`project-rules`) flags
conflicts with owned law; restructure routes law-shaped merges there.
BL-013 rides along.

**Why:** completes the two-strata design — `docs/ai/rules/**` fleet law vs
`.claude/rules/**` project law — on the ecosystem-native mechanism instead
of a custom convention. User-settled 2026-07-10 (location + full
integration in one cycle).

**Done when:** benchmark `evals/benchmarks/v8.md` green — migration carves
the decimal-money law into `.claude/rules/` while the branch convention
stays in CLAUDE.md; audit flags the planted conflicting project rule under
the `project-rules` slug; restructure decision-gates it byte-unchanged; the
upgrade scenario delivers `project-rules.md` itself (alphabetically last →
auto-withheld by the fixture generator); all idempotency passes zero-diff.

## BL-015 — Project-rules follow-ups from the v8 final review (skill-file items)

**Status: DONE 2026-07-10** — rode the BL-016 cycle: item 1 shipped (harvest
test 2 now counts an owned-law contradiction as covered, locked by a
candidates-section-scoped absent marker in the audit grader), item 2 shipped
(check 11 Info note when the manifest is current but `.claude/rules/` is
absent), item 3 shipped ("Narrative AI rules prose" in restructure.md §1),
item 4 shipped (check 10(b) exempts `.claude/rules/**` — auto-loading is the
wiring). Benchmark `evals/benchmarks/v9.md`.

Original items:

1. **Harvest test 2 doesn't exclude owned-law contradictions (Important).**
   "No rule under `docs/ai/rules/**` states it" reads literally as passing a
   statement that *contradicts* an owned rule — the v8c run's correct
   non-proposal of the flagged `.claude/rules/journal.md` line was judgment,
   not pinned text. Fix: append to test 2 "a statement contradicting an
   owned rule is covered by that rule — decision-gate material, never a
   candidate", and add a grader absent-marker locking it.
2. **Check 11's silent skip when `.claude/rules/` is absent (Minor).** Git
   drops empty directories, so cloned fresh-scaffolds lack the dir and the
   check silently skips. Consider an Info note when the manifest is current
   but the directory is missing.
3. **restructure.md §1 table-row overlap (Minor).** "AI rules prose →
   CLAUDE.md project sections" now overlaps the project-rules row for
   law-shaped text; §3 disambiguates but the table alone is ambiguous. Fix:
   "Narrative AI rules prose".
4. **Keep-list × `.claude/rules/` remedy is misleading (Minor).** A kept
   file under `.claude/rules/` trips check 10(b) "referenced from nowhere"
   though auto-loaded rules are legitimately unreferenced. Fix: exempt
   `.claude/rules/**` from 10(b) (auto-loading IS the wiring).

Bookkeeping: the v8 spec's Out-of-scope line ("migration writes carved files
ONCE") is in tension with its own Decision 6 (restructure merges foreign law
into `.claude/rules/` — the shipped, correct behavior); recorded here since
specs are historical. Also: only the audit scenario was graded against the
final shipped v8 bytes (post-4966342) — disclosed in v8.md note; migration/
upgrade/restructure ran pre-fix (both fixes narrow and strengthening-only).

## BL-016 — Stray rulebooks + .NET refactoring law (constitution v9)

**Status: DONE 2026-07-10** — full cycle (spec
`docs/superpowers/specs/2026-07-10-stray-rulebooks-design.md`, commits
8d8e698..HEAD, **VERSION 8→9**, benchmark `evals/benchmarks/v9.md`: 95/95 —
the first fully clean single-pass benchmark, no mid-run fixes). Audit check
12 flagged the planted stray rulebook and only it; harvest proposed its
generic line and excluded the project line and the owned-law contradiction
(BL-015 rider-1 lock); restructure merged it into `.claude/rules/` and
removed it with fidelity verified; all idempotency passes zero-diff.
BL-015 riders shipped with it. **Fleet action: run `/legislator` in each
downstream repo to deliver v9.** Rider logged: §3's
restatement-replacement path under restructure (and its interplay with the
fidelity pass's exemption list) is still untested — next fixture that
plants an owned-rule-restating line in a merged source should lock it.

**What:** two-part cycle (spec:
`docs/superpowers/specs/2026-07-10-stray-rulebooks-design.md`). (a) Generic
refactoring law absorbed from fleet-platform's orphaned
`docs/superpowers/refactoring-checklist.md` into concern-named files —
async/cleanliness bullets in `stacks/dotnet/coding-standards.md`, DI bullets
in `stacks/dotnet/architecture.md`, new `stacks/dotnet/data-access.md`
(**`assets/rules/**` change: VERSION 8→9**). (b) Stray-rulebook feature:
audit check 12 (`stray-rulebooks`, Warning) flags law-shaped rule/checklist
docs no session loads; harvest scans them; restructure merges their law
into `.claude/rules/<topic>.md` and removes the file. BL-015 rides along
(all four items).

**Why:** a rulebook parked in an unorthodox folder is law no agent ever
sees — fleet-platform's checklist was invisible to CLAUDE.md, harvest, and
every audit check. After this cycle "refactor X" in a legislated repo hits
v9 law (imported), project law (`.claude/rules/`, auto-loaded), and the
generic dotnet-refactoring skill — nothing orphaned. User-settled
2026-07-10 (concern-named law home + full loop incl. fleet-platform
validation).

**Done when:** benchmark `evals/benchmarks/v9.md` green — audit names the
planted `docs/superpowers/review-checklist.md` under the `stray-rulebooks`
slug and proposes its generic line as a candidate (project line + check-11
contradiction line absent from candidates); restructure merges it into
`.claude/rules/` and removes it; fresh/upgrade carry `data-access.md`;
idempotency zero-diff; then fleet-platform live validation (upgrade +
restructure) succeeds end-to-end.

## BL-017 — Stray-rulebook follow-ups from the v9 final review (skill-file items)

**Status: DONE 2026-07-11** — rode the BL-018 cycle: items 1–3 shipped
(check 12 exempts kept paths and conventional community docs, recognizes
`docs/okf/index.md` as a referrer, finding text reworded, check-7/9/12
precedence pinned — item 6 resolved by the same precedence sentence);
item 4 shipped (upgrade fixture withholds the last stack rule; grader
asserts the delivered file + the report's proposed import line); item 5
shipped as v10 law (captive-dependency bullet in dotnet architecture.md).
Item 7's grader-rot notes remain accepted-risk (heading is a must-contain
marker). Benchmark `evals/benchmarks/v10.md`.

Original items:

1. **Check 12 lacks a keep-list exemption (Important).** A keep-listed
   law-shaped doc draws a Warning whose remedy ("run restructure to
   consolidate") is an action restructure refuses on kept paths — an
   unresolvable finding. Fix: exempt kept paths from check 12; the keep
   entry is the user's ruling.
2. **Check 12's referrer set omits `docs/okf/index.md` (Important).** The
   OKF index is a surface sessions load (via `core/okf.md`), so a law-shaped
   doc properly linked from it is flagged as "no session loads" — factually
   wrong text — and a law-shaped orphan under `docs/okf/` gets two Warnings
   with contradictory remedies (check 7 vs 12). Fix: add the OKF index as a
   recognized referrer and define precedence between checks 7 and 12.
3. **Root-level scan false-positives on conventional docs (Important).**
   `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md` are imperative
   checklists unreferenced from CLAUDE.md → fleet audits will emit noise.
   Fix: extend the root exemption list with the conventional-doc set.
4. **Upgrade fixture never exercises a stacks/ file addition (Important).**
   The generated fixture withholds only the alphabetically last *core*
   rule, so delivering a new stack file (exactly what v9's fleet action
   does) is untested, and no assertion checks the Step 7 proposal of the
   new `@...` import line. Fix: also withhold the alphabetically last
   stack rule and assert the proposed import line.
5. **Captive-dependency law candidate (Minor, v10).** "No singleton
   services holding scoped/DbContext references" from the fleet-platform
   checklist is generic + diff-checkable and covered by neither new DI
   bullet — promote in the next law cycle.
6. **Check 12 × check 9 double-report on root `AGENTS.md` (Minor)** — same
   file, two slugs, two severities; remedies agree. Decide precedence when
   fixing item 2.
7. **Latent grader rot (Minor):** `grade.py` greps `stray_project_law`
   without `-F`; the candidates-section regex silently passes absent checks
   if the pinned heading changes (mitigated: heading is also a must-contain
   marker). Harvest's stray-rulebook scan is only exercised in audit mode —
   pairs with the BL-016 §3 restatement-path rider.

## BL-018 — Branch discipline: integrating is part of the task (constitution v10)

**Status: DONE 2026-07-11** — full cycle (spec
`docs/superpowers/specs/2026-07-11-branch-discipline-design.md`, commits
444cb7e..HEAD on `feature/bl-018-branch-discipline-v10`, **VERSION 9→10**,
benchmark `evals/benchmarks/v10.md`: 98/98, second consecutive single-pass
clean). Upgrade scenario delivered a withheld stack rule and proposed its
import line (the v9 fleet-action path, now locked); check-12 keep exemption
fired unprompted in the idempotency run; all idempotency passes zero-diff.
BL-017 shipped with it. **Fleet action: run `/legislator` in each
downstream repo to deliver v10.** Companion fleet-platform
`.claude/rules/branching.md` pending re-land (orphaned commit 0dd0ebd,
awaiting repo-idle signal from the user).

**What:** amend `core/pair-development.md` (spec:
`docs/superpowers/specs/2026-07-11-branch-discipline-design.md`) — WIP
limit 1: never cut a new task branch while an unmerged one exists; stack
dependent work (merge bottom-up); batch micro-changes; cut from
freshly-pulled main; merging stays the user's act (**`assets/rules/**`
change: VERSION 9→10**). BL-017 riders 1–4 ride along (check-12 keep/
referrer/conventional-doc hardening, upgrade fixture withholds a stack
rule) plus BL-017 item 5 (captive-dependency bullet in dotnet
architecture.md). Companion project law: fleet-platform
`.claude/rules/branching.md` (concrete mechanics — stacking commands,
2-day cap, chore batching).

**Why:** at AI-assisted pace, tasks outrun merges: fleet-platform hit 30+
local branches (mostly squash-merge ghosts plus stale real work), each new
branch cut from a staler master. Control comes from the gates (build/test,
review checklist, decision gate), not branch count — so the law makes
integration part of the task instead of an afterthought. User-settled
2026-07-11 (WIP-limit-1 + stacking, both layers, triage).

**Done when:** benchmark `evals/benchmarks/v10.md` green vs v9 — upgrade
scenario delivers the withheld stack rule and proposes its import line;
idempotency zero-diff ×3; final review READY; `/legislator` fleet rollout
delivers the amended rule.

## BL-019 — Branch-discipline follow-ups from the v10 final review (skill-file items)

**Status: DONE 2026-07-12** — rode the BL-020 cycle: item 1 shipped
(`IServiceScopeFactory`-scope carve-out on the service-locator bullet),
item 2 shipped ("from main" + "parked" defined), item 3 shipped (grader
scoped to the "Needs your review" block), item 4 shipped (check 9 elevates
to Warning for predominantly law-shaped foreign configs), item 5 shipped
(conventional-doc exemption covers `docs/` and `.github/` variants).
Item 6 honored: v11 benchmark notes stick to on-disk-verifiable claims.
Benchmark `evals/benchmarks/v11.md`.

Original items:

1. **Service-locator bullet needs a scope carve-out (Important).** The new
   captive-dependency remedy (`IServiceScopeFactory`) requires resolving
   from a created scope, which the constructor-injection-only bullet
   (dotnet architecture.md) literally forbids. Fix: append "except
   resolving from a scope you created via `IServiceScopeFactory`" to the
   service-locator bullet.
2. **Pair-development wording tightening (Minor).** "Never cut a new task
   branch while an unmerged one exists" is literally contradicted by its
   own stacking alternative; fix to "never cut a new task branch *from
   main* while…". Define "explicitly parked" (single occurrence).
3. **Import-line oracle too loose (Minor).** `report_proposes_stack_import_line`
   substring-matches anywhere in the report; scope it to the "Needs your
   review"/Add block.
4. **Check-9 precedence is a severity downgrade (Minor).** A law-shaped
   `AGENTS.md`/`.cursorrules` now reports only at Info (check 9) where v9
   flagged it Warning (check 12). Deliberate, but reconsider: foreign
   configs are the files most likely to hide law.
5. **Conventional-doc exemption surface (Minor).** Root-only: a law-shaped
   `docs/CONTRIBUTING.md` is still flagged; `.github/` variants unscanned.
   Make the convention surface consistent.
6. Bookkeeping: v10.md note 3 ("rider 1 fired unprompted") is
   plausible-but-unauditable (no run-2 transcript artifact); benchmark
   notes should stick to on-disk-verifiable claims.

## BL-020 — Glossary vitality: seed + law + detection (constitution v11)

**Status: DONE 2026-07-12** — full cycle (spec
`docs/superpowers/specs/2026-07-12-glossary-vitality-design.md`, commits
6fc11c4..HEAD on `feature/bl-020-glossary-vitality-v11`, **VERSION 10→11**,
benchmark `evals/benchmarks/v11.md`: 103/103 after three honestly-recorded
mid-benchmark catches — two domain-blank fixtures enriched (ambiguous-bait
class), a candidates-regex grader precision fix, and check 11 extended to
CLAUDE.md prose after the restructure run exposed that conflict-surfacing
there was judgment-dependent). Fresh run seeded 5 terms, migration carved
3 definitions into rows, restructure healed the empty glossary with 4
derived terms, all idempotency zero-diff. BL-019 shipped with it.
**Fleet action: run `/legislator` in each downstream repo to deliver v11**
(phase 2 backfill covers fleet-platform + fleet-api + fleet-agent).

**What:** three levers to stop OKF glossaries dying empty (spec:
`docs/superpowers/specs/2026-07-12-glossary-vitality-design.md`).
(1) `{{GLOSSARY_TABLE}}` derived at legislation (5–15 confirmed terms from
the repo's own domain) instead of an intentionally empty table; (2)
`core/okf.md`'s mandatory completion checklist gains a glossary item
(**`assets/rules/**` change: VERSION 10→11**); (3) audit check 13
(`glossary-vitality`, Warning) flags a zero-row glossary in a repo with
source code, healable by restructure via the Step 4 derivation rules
(token-fill precedent). BL-019 rides along (all five items).

**Why:** review of the fleet (2026-07-12) found fleet-platform and
fleet-api glossaries byte-empty since legislation while fleet-agent's
thrives — terms "emerge" only in design sessions; code-resident terms
never do. The glossary had no law behind it: not on the completion
checklist, not audited, seeded empty by design.

**Done when:** benchmark `evals/benchmarks/v11.md` green — fresh and
migration runs seed ≥1 real term; audit names the empty planted glossary
under the `glossary-vitality` slug; restructure heals it approval-gated;
idempotency zero-diff ×3; then live backfill lands real domain terms in
fleet-platform and fleet-api on feature branches.

## BL-021 — Glossary-vitality follow-ups from the v11 final review (skill-file items)

**Status: DONE 2026-07-12** — rode the BL-022 cycle: item 1 shipped
("unmerged, unparked"), item 2 shipped (derivation "up to 15, typically 5+
when the repo evidences them"), item 3 shipped (carve-out conditioned on a
longer-lived service reaching shorter-lived ones), item 4 shipped (check 13
names check 6's pinned ignore list), item 5 partially shipped
(`glossary_rows()` scoped to the term table; the absent-marker
post-Clean-checks blindness remains accepted), item 6 shipped (check 12
scans `.github/*.md`). Benchmark `evals/benchmarks/v12.md`.

Original items:

1. **"Parked" branch still trips the never-cut clause (Important).**
   pair-development.md: a parked branch is literally "an unmerged task
   branch", so parking never actually unblocks new work. Fix: "while an
   unmerged, unparked task branch exists".
2. **Derivation floor incoherent (Important).** "Derive 5–15 terms" vs
   "never invent" leaves 1–4 derivable terms undefined — two certified v11
   runs seeded 3 and 4 rows. Fix: "up to 15, typically 5+ when the repo
   evidences them".
3. **IServiceScopeFactory carve-out lifetime-unconditioned (Important).**
   architecture.md: as worded, any component may scope-and-resolve. Fix:
   condition the exception on a longer-lived service reaching shorter-lived
   ones.
4. **Check 13 "source directory" undefined (Minor).** Borrow check 6's
   pinned ignore list explicitly; consider the perpetual-Warning case of a
   code-bearing repo with genuinely nothing derivable.
5. **Grader robustness (Minor).** Absent markers are blind to text after
   `Clean checks:` and truncate at sub-headings; `glossary_rows()` counts
   any pipe line; presence/absence scoping asymmetric. Tighten when next
   touching grade.py.
6. **Check 12 `.github/` exemption is dead text (Minor).** The scan set
   never includes `.github/*.md` — either scan it or drop the exemption
   words; note a law-shaped `.github/foo.md` currently escapes entirely.
7. Bookkeeping: an unresolved `{{GLOSSARY_TABLE}}` double-fires checks 2
   and 13 (one defect, two findings); v11.md note 4's wall-time caveat
   attribution is cosmetic-muddled — both accepted as-is.

(v11 review rider "pure audit never ran under final bytes" was closed
before rollout: audit re-run at 20466a6, results appended to v11.md.)

---

## BL-022 — Skill governance + verification law (constitution v12)

**Status: DONE 2026-07-12** — full cycle (spec
`docs/superpowers/specs/2026-07-12-skill-governance-design.md`, commits
f90c4dc..HEAD on `feature/bl-022-skill-governance-v12`, **VERSION 11→12**,
benchmark `evals/benchmarks/v12.md`: 110/110, third single-pass-clean
benchmark). Upgrade delivered `verification.md` and proposed its import;
restructure split the planted foreign glossary exactly per the new routing
(definition→okf/glossary row, law→.claude/rules, file removed, fidelity 9
lines); the migration run unprompted flagged the missing per-repo
verification bindings — the new law steering behavior on first contact.
Companion actions done: 15 conflicting/irrelevant mattpocock skills pruned;
legislator repo CLAUDE.md bans AI co-author trailers explicitly. BL-021
shipped with it. **Fleet action: run `/legislator` in each downstream repo
to deliver v12** (phase 2 backfill covers all three + drafts their
verification/skills project rules).

**What:** two new core rules (spec:
`docs/superpowers/specs/2026-07-12-skill-governance-design.md`).
`core/skills.md` — law beats skills, skill outputs redirect to
constitutional homes (issues→backlog, foreign glossaries→okf/glossary,
mid-skill decisions→decision gate + ADR), no skill commits/pushes/merges/
files issues on its own authority, hook installs are decision-gate stops,
per-repo sanction lists in `.claude/rules/skills.md`. `core/verification.md`
— the verification ladder (tests at the right boundary, drive the real app
via repo-configured MCP tooling before "done", read-only DB checks, honest
build/test gate), with per-repo bindings in `.claude/rules/verification.md`
(**`assets/rules/**` change: VERSION 11→12**). Check 9 gains the
parallel-constitution artifacts (CONTEXT.md, UBIQUITOUS_LANGUAGE.md,
docs/agents/, .scratch/, root NOTES.md); BL-021 rides along. Companion
actions: 15 hostile/irrelevant mattpocock skills pruned from
`~/.claude/skills/`; legislator repo CLAUDE.md bans AI co-author trailers
explicitly.

**Why:** the mattpocock pack sweep (2026-07-12, verdicts in the spec)
showed any installed skill pack can out-instruct the constitution — a
parallel intake, parallel glossaries, auto-commits. The missing law was
precedence, not the pack. And verification had no law at all: the
build/test gate lived in a skill, and fleet-platform grew its own e2e
project rule (harvest signal) — agents need a pinned ladder to keep
control of what they build.

**Done when:** benchmark `evals/benchmarks/v12.md` green — upgrade
delivers `verification.md` (auto-withheld) and proposes its import; audit
flags the planted `UBIQUITOUS_LANGUAGE.md` at Warning under check 9;
restructure merges it into glossary rows; candidates carry its generic
line only; idempotency zero-diff ×3; then the three-repo backfill lands
v12 + drafted verification/skills project rules on PRs.

## BL-023 — Skill-governance follow-ups from the v12 final review (skill-file items)

**Status: DONE 2026-07-12** — rode the BL-024 cycle: item 1 shipped
(harness fixtures/seeding exempt from the read-only-DB bullet), item 2
shipped (absent-bindings fallback: ladder applies with repo defaults),
item 3 shipped (project-rules sanctions the two instance-data homes),
item 4 shipped (setup-gate self-exemption for legislator's CLAUDE.md
writes), item 5 shipped (check 12 exempts PULL_REQUEST_TEMPLATE.md +
.github/ISSUE_TEMPLATE/**), item 6 shipped (check 9 kept-path exemption),
item 7 shipped (upgrade grader asserts both import proposals — and the
assertion caught nothing because the run complied). Benchmark
`evals/benchmarks/v13.md`.

Original items:

1. **Read-only-DB bullet vs test harnesses (Important).** verification.md's
   read-only rule, strictly read, outlaws ordinary integration-test
   seeding/truncation. Scope it to manual/exploratory verification, or
   exempt test-harness fixtures explicitly.
2. **Absent-bindings fallback undefined (Important).** A UI repo with no
   `.claude/rules/verification.md` makes the drive-the-app gate
   unsatisfiable. Define the fallback (degrade to strongest available +
   flag the missing bindings in the report).
3. **Instance-data carve-out for the two named binding files (Important).**
   verification.md/skills.md mandate instance data into `.claude/rules/**`,
   which project-rules.md and the three-way split route to CLAUDE.md —
   sanction `.claude/rules/{verification,skills}.md` as named instance-data
   homes so restructure never proposes evicting them.
4. **Check 12 `.github/` scan over-broad (Important).** Recursive scan hits
   PULL_REQUEST_TEMPLATE.md / ISSUE_TEMPLATE/*.md (checklist-shaped,
   unreferenced by design) → fleet-wide false Warnings. Exempt GitHub's
   conventional templates.
5. **Check-9 Warning-elevation has zero mechanical coverage (Important).**
   Plant a predominantly-law-shaped foreign fixture with a severity-anchored
   marker.
6. **Precedence floor + CLAUDE.md rank (Minor).** skills.md lets a project
   rule override another skill's safety gate without contradicting owned
   law (check 11 silent); CLAUDE.md instructions are unranked. Define the
   floor and CLAUDE.md's place.
7. **Self-exemption wording (Minor).** Legislator's own migration/scaffold
   writes CLAUDE.md — literally the setup action skills.md gates. State
   "invoking /legislator is the approval" so literal agents don't insert
   stops.
8. **"Zero new warnings" baseline undefined (Minor)** — define vs the
   pre-change build.
9. **Kept foreign structure is a perpetual check-9 finding (Minor)** — add
   check 12's kept-path exemption to check 9.
10. **Upgrade grader: assert the withheld CORE rule's import proposal too
    (Minor)** — currently only the stack rule's line is asserted.

## BL-024 — Skill stage-routing + setup automation (constitution v13)

**Status: DONE 2026-07-12** — full cycle (spec
`docs/superpowers/specs/2026-07-13-skill-stage-routing-design.md`, commits
fc564c1..HEAD on `feature/bl-024-skill-stage-routing-v13` — stacked on the
open v12 branch per the pair-development stacking law, merge bottom-up —
**VERSION 12→13**, benchmark `evals/benchmarks/v13.md`: 117/117 after one
real catch and three honesty items: v12's tpl import gap found by the
migration run's own report (tpl fixed + class-killer static check +
grader completeness assertion), a fragile oracle removed exactly as the
v12 review predicted, and two run-noise re-runs with no law change).
Fresh scaffolds now seed a stage-mapped `.claude/rules/skills.md`;
`tools/link-skills.sh --check` verified clean live. BL-023 shipped with
it. **Fleet action: v12 PR merges first, then v13; run `/legislator`
in each downstream repo.**

**What:** skills wired to workflow steps + a setup story (spec:
`docs/superpowers/specs/2026-07-13-skill-stage-routing-design.md`).
(1) `core/skills.md` gains stage routing: `.claude/rules/skills.md` maps
sanctioned skills to pre-plan/implement/debug/review/docs stages,
consulted at stage boundaries (**`assets/rules/**` change: VERSION
12→13**); (2) create-once starter scaffold via new `skills-rules.md.tpl`
(derived from profiles ∩ installed skills, pinned stage-affinity table);
(3) audit check 14 `skill-bindings` (Info) — sanctioned-but-uninstalled
skills; (4) `tools/link-skills.sh` + README "Skill ecosystem setup"
tutorial. BL-023 rides along (all seven items).

**Why:** v12 settled which skills are lawful, not when — agents don't
proactively grill before plans or run tdd during implementation; and the
skill layer itself had no rot detection or setup docs (symlinks into a
non-git dump, tribal knowledge).

**Done when:** benchmark `evals/benchmarks/v13.md` green — fresh scaffolds
a stage-mapped starter (structural oracle); audit flags the planted
`made-up-skill` binding at Info; restructure routes it to For-the-team,
file byte-unchanged; upgrade asserts both new-rule import proposals;
idempotency zero-diff (create-once starter never rewrites); then the
fleet's three hand-written skills.md files gain stage headings via small
PRs.

## BL-025 — Stage-routing follow-ups from the v13 final review (skill-file items)

Ride along with the next benchmarked `skill/**` cycle:

1. **Unprompted heading-pin evidence (Important).** The counted v13
   migration re-run carried a pinned-format reminder; run v14's migration
   with the clean prompt — if the H2 drift recurs, move the pin adjacent
   to the save-report instruction in law.
2. **Severity-anchored markers (Important).** The counted v13 audit put
   `unresolved-placeholders` under Info (pinned: Critical) and no oracle
   noticed. Add severity-anchored markers (e.g. section-scoped presence:
   marker must appear under the pinned severity heading) for at least the
   Critical checks.
3. **Stage-affinity vs keep-list coherence (Minor).** The pinned table
   names `dotnet-refactoring`, which `tools/link-skills.sh`'s KEEP list
   can't provide (different source) — a fresh machine can't scaffold the
   dotnet review stage from the tutorial alone. Document its source or
   add a second-source mechanism to the script.
4. **link-skills.sh hardening (Minor).** `mkdir -p "$DST"`; non-zero exit
   on MISSING-SOURCE drift in link mode.
5. **Mapped-but-uninstalled fallback clause (Minor).**
   `core/skills.md` stage-routing bullet: "a mapped skill that is not
   installed is check-14 territory — note it and proceed."
6. **Grader tightening (Minor).** Restructure's skill-binding check should
   scope to `For the team:`; fresh/migration could cross-check backticked
   skill names in the scaffolded stage map against `~/.claude/skills/`.

## BL-026 — SDD gap harvest: execute the G1–G9 decisions from the landscape research

**Status: REVISED 2026-08-20** — the deep audit D0–D5
(`docs/superpowers/specs/2026-08-20-deep-audit-d0-d5.md`, second pass over
the stage × instrument matrix with fleet evidence) turned every gap into a
concrete decision. Per-gap verdicts:

- **G1 (living baseline): DECIDED — architecture D.** EARS lines carry
  stable ids (R-NNN) in specs → tests annotated with the same ids → a
  generator writes `baseline.md` (do-not-edit, like the manifest). Rot
  impossible by construction; converge checks both directions
  (missing-test / unrequested). The OpenSpec living-tree variant is
  conserved for Vector A / second hands. Non-testable norms live in ADRs
  + a marked baseline section. Implemented inside BL-033 (fleet-obs pilot).
- **G2 (requirements syntax): DECIDED — EARS lines mandatory** (5
  patterns), at least one named Gherkin "hurting case" per spec,
  observability as the quality test; no RFC 2119 table. → BL-032.
- **G3 (clarification): DECIDED — grill protocol codified**: taxonomy
  shrunk to 5–6 solo categories, budget max 5 / one at a time,
  recommended-first, **answers written into the spec** (`##
  Clarifications`), post-answer validation. → BL-032.
- **G4 (convergence): DECIDED — converge is a mandatory case-cycle
  gate**: gap taxonomy missing/partial/contradicts/unrequested,
  append-only traceable tasks in the case plan, loop to "✅ Converged",
  constitutional MUSTs as CRITICAL findings; plus the `stale-doc` axis.
  → BL-032.
- **G5 (cross-artifact analysis): DECIDED — analyze gate paired with
  converge** (before implementation): five mechanical passes (coverage
  R↔task, dangling refs, vagueness, duplicates, terminology) + two
  judgment axes (reuse-first, over-engineering, from Agent OS). → BL-032;
  mechanical passes run in the BL-033 engine.
- **G6 (monolithic plans): DECIDED — plan as a package in the case
  file**: research/data-model/contracts/quickstart domain-optional;
  split mandatory past ~10–15 KB or 2+ domains; one task = one session;
  [P] file-disjointness markers; ADR boundary rule. → BL-032.
- **G7 (spec validation): DECIDED — one engine, three jobs**: spec/plan
  linter + OKF anchor checks (source-symbol grounding) + baseline
  generation, a single tool. → BL-033.
- **G8 (parallelism markers): DECIDED — [P] markers** (machine-readable
  declaration of the existing Wave-1 practice). → BL-032.
- **G9 (process sizing): DECIDED — three ceremony tiers as law** (0
  direct / 1 light / 2 full), chosen at case opening on blast radius ×
  novelty, declared in the case header; converge may raise a tier.
  → BL-032.
- Also decided in D-pass: spec typology (feature / bugfix / exploration,
  type in the header; bugfix carries current/expected/unchanged;
  boundary + right-size rules), cross-repo case convention (case file in
  the initiating repo, reference rows + same bl/NNN branches in
  siblings), OKF v2 decomposition (generated / anchored / human), OKF
  hygiene (stale-doc in-cycle + sync-debt audit check), and the first
  steward cycle scheduled after the harvest lands. Rejected with
  reasons: Agent OS product layer, standards compilation, improve-skills;
  SpecKit pre/post constitution gate, auto-waves, MVP markers; Kiro
  conditional loading (D0); Vortex (unresolved — could not confirm it
  exists).

**Execution order (revised 2026-08-21, evening — eval hardening joins v17):** fleet-obs gold panel ✓ DONE (PR#19, before-snapshot 22%) → **edition v17** = BL-032 (SDD law + absorbed case home + lifecycle + riders BL-028/030 + BL-025 triage) **closing green first**, then BL-036 waves A–C on the same branch (BL-037 runs as Wave A; case-practice is the v17 release gate) → soak → **edition v18** = BL-033 (engine, OKF v2, baseline, fleet-obs pilot; BL-031 rides if templates move) → BL-034 (self-legislation) → **edition v19+** = BL-027 (outer mode, own design cycle) → first steward cycle. Rationale for the v17 expansion: the SDD law is a program for target-repo agents — shipping it without a suite that executes it (case-practice) and with a grader that hand-duplicates the skill's contracts would repeat the orphan problem the law exists to cure. BL-035 (docs-only, no VERSION) runs anytime. Full consistency record: the deep-audit spec's addendum.

**Why:** the workflow was built intuitively from practice and works (fleet
usage evidence in the research spec), but with nothing to compare against,
non-optimal solutions and gaps are invisible. The field's instruments —
read from the tools' own repos, not their marketing — close gaps without
adopting any foreign structure: techniques get translated into legislator
assets, nothing external survives contact with the law.

**Done when:** BL-032 and BL-033 ship green through their full cycles; the
fleet-obs pilot demonstrates EARS+baseline+converge end to end; the fleet-obs panel is
live before the first rollout; rejected decisions are recorded (this entry
+ the deep-audit spec serve as the record; ADR them at implementation time
if they need permanence).

## BL-027 — Outer placement mode (Vector A: legislation outside the codebase)

**Status: queued (behavioral — skill/ changes, VERSION bump + full e2e) → edition v21+, after its own design cycle. Renamed 2026-08-20 from "Enterprise sidecar placement mode": outer names the mode (the AI layer sits outside the codebase — ontology §Placement modes), the sidecar is only the mechanism that hosts it.**

**What:** a placement mode for legislating a repo the operator **cannot
commit to** (real enterprise case, anonymized — large fast-mutating
codebase, team doesn't use the legislator, MCPs to Jira/Confluence and a
documented DB exist; user runs own Claude Code on a local clone;
untracked files tolerable, commits not). Mechanics settled in the
2026-08-19 grill (rationale in the research spec's vectors section):
(1) **sidecar repo hosts the owned layer** — `docs/ai/rules/**`,
  `manifest.json`, and a personal-context OKF (own ADRs, tips, dead ends,
  blast-radius contracts only; company KB is a probe target via its MCPs,
  never a mirror — a codebase mirror rots in days there);
(2) **untracked stub in the target clone** — thin `CLAUDE.md`/`AGENTS.md`
  `@import`-ing the sidecar, excluded via `.git/info/exclude` (local-only,
  never leaves the machine); re-runs restore the stub after `git clean`;
(3) **probe-first doctrine + blast-radius documentation** as
  `enterprise-solo` profile law (the existing stacks mechanism — no fork:
  law overlap is ~90%, harvest/evals/steward stay shared in one lineage);
(4) **outbound redirection inversion** — backlog → Jira, durable
  reference → Confluence, via the already-approved MCP scope (read-only
  DB, no GitHub/ticketing automation per BL-008); in-repo artifacts
  become lifecycle working copies that die at feature merge;
(5) progressive-rigor floor (small feature in a big organism = lowest
  ceremony tier by law); (6) fleet-obs registry root addition for the sidecar.
**Design notes added 2026-08-20 (consistency review):** (a) the outer
**execution profile** — what changes in each law for an outer repo
(ceremony defaults, converge/analyze semantics when the baseline and
engine live untracked in the clone, harvest/steward without git access
to the target) — is decided **inside this case's design cycle**, not as
a separate case: one owner per topic; (b) generated artifacts
(baseline/codebase-map, BL-033) in an outer repo are untracked by
definition — the stub-restore semantics after `git clean -fd` must cover
**regeneration**, not just re-copy, of everything generated. Full e2e
benchmark when implemented (behavioral change; placement mode inverts
Step 3's owned-files-land-in-target-repo assumption — the biggest
structural change the skill has faced; design needs its own brainstorm
cycle for Step 0/3/6/7 rework, stub-restore semantics, and how audit/
restructure run against a sidecar).

**Why:** the enterprise adapter is one of two application vectors (the
other — solo fleet — is home ground and needs nothing structural). Without
a placement mode, Vector A either stays unlegislated or forces a fork,
and a fork costs permanent rent: doubled evals, split harvest, two
steward reviews forever.

**Done when:** a sidecar-legislated enterprise repo (anonymized in all
artifacts — masking law) carries the full constitution ambiently via the
stub; `git status` in the target clone stays clean; `git clean -fd` +
re-run restores the stub; probe-first/blast-radius law demonstrably keeps
the sidecar OKF personal-observation-only; benchmark green including a
new sidecar-placement eval scenario.

## BL-028 — Manifest key `profiles` → `stacks` (single-concept naming)

**Status: DONE in v17 (92d1e3d — stacks key + legacy fallback, upgrade fixture carries a `profiles` manifest); prose residue ("stack profile") closed in v18.**

**What:** rename the manifest's stack-subscription key from `profiles` to
`stacks` so the key carries the concept's only name (ontology R5-T1,
2026-08-20: "profile" has no standalone meaning — it is a leftover second
name for *stack*). Upgrade mode reads `stacks` and falls back to the legacy
`profiles` key (reconstruct, rewrite in the new serialization order);
fresh scaffolds write only `stacks`. Sweep SKILL.md prose, templates, and
audit check references for the old key and the compound "stack profile".
Fleet re-run migrates all field manifests.

**Why:** one word, one meaning (ontology §3.4) — the manifest is the most
visible artifact the skill owns, and it currently carries the only
remaining "profile" naming for a concept everywhere else called a stack.

**Done when:** fresh scaffold writes `stacks`; upgrade on a legacy
`profiles` manifest migrates without data loss (`keep` / `ownedFiles`
preserved); every eval scenario green including a legacy-manifest fixture;
fleet re-run leaves every manifest on the new key.

## BL-029 — Case-file home `docs/cases/` in the scaffold

**Status: ABSORBED into BL-032 (2026-08-20, consistency-review P2)** — the
case home is a mandatory component of the SDD law's edition (v17), not a
separate unit: a law that declares tiers in case headers and specs in
case files cannot land without the home. The original scope below ships
inside BL-032.

**What (original, ships via BL-032):** give upgrade/scaffold modes a
create-once `docs/cases/` home
(a README or template file — git does not track empty directories; same
pattern as audit check 11's `.claude/rules/` mkdir advice). New cases are
born in their home (`docs/cases/BL-NNN/`: spec, plan, summary); register
rows link into it. Migration is forward-only (ontology R2-T3): historical
specs/plans stay where they lie and their register rows link to legacy
paths; moving an active case's artifacts is optional per-repo restructure
work, never forced.

**Why:** A2 — a case is currently scattered across four homes (backlog
status line, specs/, plans/, journal) with no cover object saying "this is
the case, whole." The ontology fixes the concept (case file = one case,
one home); the scaffold must give the concept a place to exist.

**Done when:** scaffold creates the home create-once (never overwrites);
a fresh case demonstrably lands inside it; legacy register rows still
resolve to their historical paths; benchmark green.

## BL-030 — Constitution disambiguation sweep in skill prose (A5)

**Status: DONE in v17 (92d1e3d — sweep); residue ("AGENTS.md is the canonical constitution" ×3) closed in v18.**

**What:** sweep `skill/` prose so "constitution" means exactly one thing
(`docs/ai/rules/**` @ VERSION — ontology R1-T1): SKILL.md's "constitution
file" for AGENTS.md becomes "AGENTS.md" (the entry document);
`references/*` and any rule file carrying the same drift follow; glossary
pointers updated. No mechanical behavior change intended — prose only —
but it ships through the full cycle anyway per the testing law.

**Why:** A5 — one word meant three things (rules corpus, AGENTS.md,
loosely the whole installed layer). The ontology review fixed the concept;
the prose must follow or the ambiguity regrows from the skill's own text.

**Done when:** no prose under `skill/` uses "constitution" for anything
but the rules corpus @ VERSION; benchmark green with zero behavioral
diffs beyond the expected prose changes.

## BL-031 — Split backlog.md into queue + case register sections

**Status: queued (docs-only — `backlog.md.tpl` carries no queue/register structure, so the split concerns this repo's `docs/backlog.md` only; no VERSION, no benchmark; any time). Left the v18 cycle 2026-08-22.**

**What:** restructure `docs/backlog.md` into two named sections per the
ontology (R2-T2): **queue** — pending/active cases only, in intended work
order, kept short and living; **case register** — one row per case ever
opened, with status; today's DONE blocks become register records (their
history is preserved, compressed to record shape). A later split into two
files is possible but not required — the section boundary is the seed.
If the skill scaffolds a backlog template for target repos, the section
structure rides the same cycle (VERSION + benchmark); if the template
turns out repo-specific, this stays a legislator-repo docs change.

**Why:** A1 — one file smears two roles: a queue wants to be short,
ordered, and disposable; a register wants to be complete, stable, and
permanent. Each role's readers (planner vs archaeologist) want different
things from the artifact.

**Done when:** the queue section contains only pending/active work; every
case has exactly one register row; DONE history survives as register
records; template decision recorded; benchmark green if templates moved.

## BL-032 — `core/sdd.md`: the SDD law (spec format, clarify protocol, ceremony tiers, converge/analyze gates)

**Status: GREEN 2026-08-22 — corpus 177/177 and idempotency ×3 zero-diff**
(`evals/benchmarks/v17.md`, phase 2; model floor `sonnet` on Claude Code
2.1.239). Riders delivered with the cycle: **BL-028** (stacks key + legacy
fallback), **BL-030** (constitution sweep), **BL-025 triage** (items
1,2,4,5,6; item 3 excluded-stale). Phase 2 forced three further law
amendments, all benchmark-caused: entry-document authority stated once
(Step 7 had written one mode's constraint as a property of the file, and
Step 5 contradicted it in the same run), check-7 orphans pinned to `[link]`
(two lawful outcomes broke idempotency *and* let an open `[decision]` be
reclassified into an applied write), and check-11 findings routed to
`[decision]` explicitly. VERSION stays 17 — none of it touches
`assets/rules/**`. **Remaining: merge.**

**What:** give the SDD process its constitutional home (deep-audit D1
finding: the process is an orphan — practiced fleet-wide, governed
nowhere; hence 0 acceptance lines in ~20 specs and 92 KB plan monoliths).
New owned rule `core/sdd.md` + a spec template in assets, delivering the
D0–D5 decisions: spec typology (feature / bugfix / exploration, type in
the header; bugfix carries current/expected/unchanged); boundary
(in/out-of-scope) and right-size rules; EARS requirement lines (5
patterns) with stable ids R-NNN; at least one named Gherkin hurting-case
scenario per spec; the observability test; the grill clarification
protocol (5–6 solo categories, max 5 questions one at a time,
recommended-first, answers written into `## Clarifications`); ceremony
tiers 0/1/2 chosen at case opening on blast radius × novelty (converge
may raise); plan-as-package (research/data-model/contracts/quickstart
domain-optional, split past ~10–15 KB, one task = one session, [P]
file-disjointness markers, task traceability per R-NNN, ADR boundary
rule); **converge as a mandatory closing gate** (gap taxonomy
missing/partial/contradicts/unrequested, append-only traceable tasks,
loop to converged, constitutional MUSTs CRITICAL, stale-doc axis);
**analyze as the paired pre-gate** (reuse-first + over-engineering axes;
mechanical passes provided by the BL-033 engine when available,
**otherwise by the agent** — the fallback clause that decouples this
edition from v18); the cross-repo case convention (case file in the
initiating repo, reference rows + same bl/NNN branches in siblings).
**The case home ships here (absorbs BL-029, 2026-08-20):** every work
kind is a case and every spec lives in its case file `docs/cases/BL-NNN/`
— `docs/superpowers/specs/` becomes a purely legacy path, forward-only,
history never moves. **The artifact-lifecycle amendment is a green
condition of this cycle, not a rider:** `core/artifact-lifecycle.md`
must learn the new conventional home `docs/cases/` and the third role
class **generated** (machine-written locally, do-not-edit, dies with its
source) — the law currently enumerates lifecycle homes exhaustively
(`docs/superpowers/`, `docs/journal/`) and its binary reference/lifecycle
model has no slot for generated artifacts. Riding items: BL-028 (manifest
key profiles→stacks) and BL-030 (constitution disambiguation sweep) join
this cycle's benchmark. **On opening, triage BL-025** (seven v13-era
follow-ups promised as riders to "the next benchmarked skill/** cycle" —
this is it; each item rides or is excluded with a reason, some may have
gone stale over v14–v16).

**Why:** one rule file replaces muscle memory: the fleet already reached
for scenario form (fleet-obs Testing lines) and parallelism discipline
(Wave-1) on its own — the law makes the existing instincts deliverable,
auditable, and harvestable, and the tier law makes both
under-ceremony (unrequested gifts, debris) and over-ceremony (monoliths)
fixable. The case home cannot lag: the law declares the tier in the case
header and specs in the case file — a law referencing a nonexistent home
would be born contradicting itself.

**Done when:** `core/sdd.md` + template + case home + lifecycle amendment
shipped, VERSION bumped, full
e2e benchmark green including new scenarios (a spec written under the
law passes analyze; a converged case shows the append-only findings
trail); BL-028/030 riding items verified; fleet re-run delivers the rule
everywhere.

## BL-033 — OKF v2 and the anchor engine: link hardness, source anchors, the static rung

**Status: GREEN 2026-08-23 — corpus 194/194 and idempotency ×3 zero-diff on one law generation (`9dbb306`); model floor `sonnet` (Claude Code 2.1.241), unchanged from v17, v18 and v19. Benchmark `evals/benchmarks/v20.md`. Edition v20 closes at merge; tag `v20`.**

**What:** OKF v2 decomposition by link hardness — generated, anchored,
human — codified as an amendment to `core/okf.md`. The **anchored** class
ships in full this edition: every backticked path and PascalCase symbol a
knowledge document names is verified against the repository by the new
engine, `docs/ai/engine.py` (the first non-markdown asset the skill ships,
delivered by Step 3, listed in `ownedFiles`, byte-verified by audit check
3). Two new audit checks, 15 (`okf-anchors`) and 17 (`okf-sync-debt`);
anchor and debt findings route through restructure to the team.
`codebase-map.md` and `index.md` are **anchored, not generated** —
correcting deep-audit D2's assumption: the fleet showed their rows carry
judgment a generator would destroy, while their row set is already
machine-checked (audit checks 6 and 5). The **generated** role class is
declared in `core/artifact-lifecycle.md` but stays unpopulated: the
baseline generator, the spec/plan linter, and the fleet-obs registry
accounting this entry used to describe move to **BL-043** (edition v21).
BL-031 rides this cycle if it touches the backlog template.

**Why:** hand-maintained truth always rots — six documents in `fleet-api`
describing a removed model proved it in our own fleet, while our rot-free
artifacts (the law stratum, the manifest) are the machine-written ones.
Anchors give the handwritten knowledge layer a mechanical bond to the code
it describes: a document naming a symbol the source no longer contains
becomes detectably stale instead of silently wrong. Its own edition because
a transform of what "docs" means across nine repositories must not share a
landing with anything else — a failure here must not roll another law back
with it.

**Done when:** the engine ships as an owned file and both its jobs run
read-only in a legislated repository; the OKF v2 amendment and the static
rung ship with a VERSION bump and a green e2e benchmark; the two audit
checks are each exercised by a planted defect and route through restructure
to the team. The fleet-obs pilot and the fleet sweep run after the merge and
are not gates on the edition; the baseline half of the original entry is
BL-043's.

## BL-034 — Self-legislation: the legislator repo joins its own fleet

**Status: DONE 2026-08-24** — process only in the end: no `skill/` change, no
VERSION bump, no benchmark. Case home: `docs/cases/BL-034-self-legislation/`
(spec, plan, research, summary). Decision recorded in
`docs/adr/0002-the-legislator-repo-is-governed-by-its-own-constitution.md`.

**Shipped:** the repository is legislated at v20 in migration mode — thirteen
owned files delivered and byte-verified, `CLAUDE.md` renamed to `AGENTS.md`
and split three ways (`.claude/rules/evals.md`,
`.claude/rules/constitution-source.md`, `.claude/rules/records.md`; the
co-author-trailer rule removed as covered by `core/pair-development.md`), OKF
bundle seeded, case home created, `stacks: []`. The 48-term glossary moved
forward from `docs/glossary.md` into `docs/okf/glossary.md` with every row
carried. `README.md` gained deliver-to-self as release step 4 and the
branch version-skew rule.

**The two probes that carried the risk.** Before delivery, the write-guard was
driven over `skill/**` to prove it could not block development of the next
edition; after delivery the same probe showed it flipping to BLOCKED on
exactly `docs/ai/rules/**`, `docs/ai/engine.py` and `opencode.json`, and on
nothing else. Then the hurting case was run for real: a source rule edited,
`skill/VERSION` bumped to 21, delivered copy still holding v20 bytes, manifest
still v20, both commit gates green on the skew. Bootstrap compilation, not
self-modification — measured rather than argued.

**What it caught on its first run, which is the whole point of the case:**
audit check 2 produces **fourteen false Criticals** here (BL-057). It cannot
tell an unfilled template token from one quoted in prose about templates, and
this is the one repository whose documentation is *about* a templating system.
Invisible everywhere else; unmissable here.

**Status: was queued 2026-08-20 (process + one behavioral cycle) — after v20 lands (was "after v18", then "after v19", before the 2026-08-22/23 scope decisions; the dependency is on BL-033's OKF v2, not on an edition number). Not an edition of its own.**

**What:** apply the legislator to itself (A4, already seeded by
`docs/ontology.md`): scaffold the repo that hosts the skill — its own
manifest, owned copies, OKF, cases home, audit. From then on the repo is
fleet member #0 and every later edition updates the AI layer that
governs the skill's own development. Mechanics: (1) **deliver-to-self
becomes a release step** — bump → benchmark → upgrade onto this repo →
byte-verify → fleet; (2) **version-skew rule:** while developing v(N+1)
on a branch, the repo is legislated @ N while assets contain N+1 drafts —
that drift is branch-normal; owned-integrity drift on master is a
finding; (3) **migration of current practice:** this repo's existing
docs (`docs/superpowers/**`, manual spec/plan conventions) migrate
forward-only into the new law's homes (`docs/cases/`), and the manual
benchmark/release practice becomes an execution of `core/sdd.md` —
dogfooding before fleet rollout; (4) harvest/steward/audit close on the
repo itself. Recursive application is sound because it is
bootstrap-compilation, not self-modification: the using step never edits
the source (the law stratum stays byte-identical; changes flow only
through the normal edit → bump → deliver loop).

**Why:** every new law gets exercised by the skill's own development
before fleet rollout; the manual practices this repo already runs
(specs, plans, benchmarks) become instances of the law they built —
closing the loop is the strongest honesty test the system has.

**Done when:** the repo carries a current manifest and owned layer
byte-verified against its own assets; a real case runs end to end under
`core/sdd.md` in this repo (tier declared, analyze/converge executed);
the release runbook includes deliver-to-self; audit on the repo reports
clean or explains its findings.

## BL-035 — Docs overhaul: the philosophy manifest (`docs/philosophy.md`) + inner/outer modes

**Status: DONE 2026-08-22** — `docs/philosophy.md` written (seven sections: what this is, philosophy, practices, application, placement modes, horizon, where to read next). README, `docs/ontology.md` and `docs/okf/glossary.md` cross-reference it; five terms the manifest leans on gained glossary rows (drift, EARS, edition, grill, rot) so it introduces no orphan vocabulary. Docs-only as planned: no VERSION bump, no benchmark, static checks green. The **Horizon** section states what is designed but not built (BL-027, BL-033, BL-034, BL-038) and is expected to shrink as editions ship — a stale Horizon section is a finding for the edition that made it stale.

**What:** a standalone manifest document, `docs/philosophy.md` (English),
stating what the legislator is and how it is applied — the document a
stranger reads first: (1) **philosophy** — law-centrism (one constitution
delivered byte-identical, agents work under law rather than vibes),
mechanical truth-bonding (anything that can be generated or anchored to
code must be — hand-maintained truth always rots, proven in this fleet),
declarative lifecycles (artifacts carry death terms at birth), the
I/O-asymmetry (downward delivery is automatic, upward feedback is
proposal-only through harvest; the user is the sole decision authority);
(2) **practices** — the SDD case cycle (tiers, EARS/R-NNN, grill
clarify, analyze/converge gates), the fleet (VERSION editions,
deliver-to-self for the legislator's own repo), audit/restructure/
harvest/steward, the verification ladder; (3) **application** — how a
repo gets legislated, upgraded, audited; scenarios; (4) **placement
modes** — inner (default: the AI layer lives in the repo) and outer (the
layer sits outside the codebase — sidecar repo, untracked stub,
probe-first, outbound redirection; names decided 2026-08-20 on the axis
"relation of docs to code"; sidecar is the mechanism, outer is the
mode; the outer execution profile is decided in BL-027's design cycle).
README and `docs/ontology.md` gain pointers to the manifest; the
ontology stays the canonical term model, the manifest is the narrative
layer above it.

**Why:** the system's rationale is currently scattered across README,
ontology, rule files, and two research specs — a stranger (or a future
session) meets mechanics without the why. One manifest, referenced from
everywhere, written against the ontology's terms. Naming reviewed:
"inner/outer" accepted (non-colliding, self-explaining, sized by
placement not by company scale); "harness-/ai-framework" rejected —
"harness" names an agent runtime in the field, a meaning this system
does not have.

**Done when:** `docs/philosophy.md` exists covering the four sections;
README, ontology, and glossary cross-reference it; every term the
manifest uses exists in the glossary (no orphan vocabulary); static
checks pass.

## BL-036 — Eval authenticity: honest triggers, agents-first migration, gap closures, contract derivation

**Status: DONE 2026-08-22 — all waves delivered inside edition v17.**
Honest triggers, the agents-first migration scenario, the gap closures and
contract derivation all ship; `selftest:derivation` keeps the derivations
alive. Phase 2 of the benchmark closed six further grader defects the waves
had not reached — three of which had been passing *falsely* in every prior
version. The suite's own bar is now written down in `evals/POLICY.md`.

**Execution waves (priority order):**

- **Wave A — test infrastructure (nothing else is reliable without it):**
  BL-037's background runner (staged execution, notifications, status
  contract) + trigger minimization + waiver rework + contract derivation
  (SCAFFOLD_ARTIFACTS parsed from Step 4's table, `protected` derived
  from it, manifest pins into one oracle, migration wiring derived from
  AGENTS.md.tpl, severity+slug parsed from SKILL.md, restructure action
  set parsed from restructure.md — every place the grader hand-duplicated
  the skill's contract dies here) + README additions (machine-dependence
  prerequisite and the weakened `stages >= 1` assert, why idempotency
  has no migration carrier, new-stack fixtures as a known gap).
- **Wave B — coverage and law lines:** agents-first fixture + both-exist
  decision gate (migration.md §0 + SKILL.md Step 5); upgrade gap
  closures (missing-artifact assert, keep-refusal branch, drop-stack
  fixture); audit additions (cases-exempt planted artifact,
  report-outside-repo assert); restructure package (§1 table gains the
  cases row and legacy-only qualifiers for the superpowers rows, grader
  lock on §1, misplaced-case relocatable, link/map post-state asserts,
  stray plans always relocate to the legacy home); the parity law (every
  audit check ↔ a planted defect marker, enforced by a setup-time
  assert) with the three new rot checks it demands: new-home violation
  (file born in a legacy home after the legislation date), working-dir
  debris (`.superpowers/sdd/`-class) in check 9, dangling keep entry in
  check 10.
- **Wave C — the acceptance test:** the case-practice scenario
  (legislated fixture, "add feature X, tier 1" task; the grader asserts
  the case home, tier header, EARS R-NNN lines, hurting case,
  Clarifications, append-only converge trail) — the only test that
  executes `core/sdd.md` rather than merely asserting its delivery, and
  the point of the whole edition: the law ships proven executable.

**What:** four parts, one theme — the eval suite must test the skill, not
the prompt.

1. **Trigger minimization.** All five `evals.json` triggers shrink to the
   honest user voice: "Set this repo up for AI development" /
   "re-run the legislator here" — no deliverable enumeration ("rules, OKF
   docs, backlog, the works" leaks Step 4's table), no mode leak ("I just
   spun up" tells the agent it is fresh; "we changed the core rules and
   bumped VERSION" tells it why it is upgrading). Runner waiver block
   becomes: "every confirmation the skill asks for is pre-approved; answer
   with the skill's own default plus what the repo evidences; the skill
   knows what to deploy". evals/README gains the rule: a trigger never
   names deliverables — Step 4's table is the only source. v17 runs on
   the historical prompts (pass-rate comparability with v16); the model
   switch is recorded as a confound in benchmarks/v17.md.
2. **Agents-first migration fixture.** New fixture
   `legacy-migration-agents-first/` (hand-written AGENTS.md, no CLAUDE.md,
   the same law/instance markers) + scenario + grader branch: the
   migration asserts minus rename expectations, plus "CLAUDE.md created
   fresh as symlink". The law already specifies the branch
   ("If AGENTS.md already exists, it stays canonical") — the suite never
   exercised it. **Companion law line** (migration.md §0 + SKILL.md
   Step 5): both a real AGENTS.md and a real CLAUDE.md present = two
   canonical candidates = decision gate ("which is canonical?"), never a
   silent overwrite — today `ln -s` over a lingering real file would
   silently destroy content, the exact loss class migration exists to
   prevent.
3. **Upgrade gap closures.** (a) `grade_upgrade` gains
   `upgrade_creates_missing_artifacts` — the v17 fixture has no
   `docs/cases/README.md`, upgrade must create it, nothing asserted that
   (found by review 2026-08-21); (b) keep-refusal branch — the upgrade
   trigger adds "also protect `docs/ai/rules/core/okf.md`" (an owned
   path → the skill must refuse with a reason in the `### Keep list`
   section; one grader assert); (c) new mini-fixture
   `upgrade-drop-stack` (manifest with dotnet+aurelia, prompt asks to
   drop aurelia) — asserts aurelia owned files deleted, dotnet
   untouched: the only deletion-semantics-by-stack branch, currently
   untested; `PROFILES` hardcode dissolves into per-fixture meta.
4. **Contract derivation.** `SCAFFOLD_ARTIFACTS` stops being a hand-list
   (README's "the one thing to maintain by hand" dies): it is parsed
   from SKILL.md Step 4's table at grade time — the table and the
   asserts cannot diverge (this exact divergence happened twice: v17's
   cases README was added by hand, and upgrade's copy stayed unasserted).
   Manifest pins (key order, inline arrays, keep serialization) gather
   into one `expected_manifest_schema()` oracle beside `expected_owned()`.
   Deliberately left manual: fixture content markers (decimal-money,
   bl/NNN) — those are intentional test-data oracles, not contract.

**Why:** three defects surfaced in one review session (2026-08-21): the
prompts leak answers, one law branch and three upgrade branches are
untested, and the grader hand-duplicates the skill's contracts — every
edition pays a manual toll and twice the duplication actually split.

**Done when:** minimized triggers in evals.json + README rule;
agents-first scenario graded green; both-exist decision gate in law and
exercised; the three upgrade asserts/fixture in; SCAFFOLD_ARTIFACTS
derived from Step 4's table with a test proving divergence is impossible
(manually editing the list breaks); full suite green on the cycle's
benchmark.

## BL-037 — Background eval runner with staged execution and notifications

**Status: DONE 2026-08-22 — shipped as `tools/evals-bg.sh`, and grown
past the original scope.** Beyond staged execution and notifications it now
carries two runner profiles (`--runner opencode|claude`), targeted
idempotency (`--idem`), fixture reset anchored on an `eval-base` tag, and
workspace-scoped process matching. Run provenance (runner, model, law
commit) travels into every grade and onto the dashboard.

**What:** the orchestration that the v17 benchmark improvised in /tmp,
productized. (1) `tools/evals-bg.sh` — detached sequential runner:
stall detection (log size + repo dirty-count frozen → kill), resume
ladder (`opencode run --continue`, up to N resumes per attempt), full
fixture reset between attempts (`git reset` → `checkout -- .` →
`clean -fd` — staged renames survive a bare checkout and poison the
next attempt; found the hard way), confirmation-waiver prompt block,
MODEL env passthrough (glm-5.3 drops long streams on this endpoint —
313 stream errors logged; glm-5-turbo verified streaming 27 KB/3.8k
words in one 5-minute probe). (2) **Staged execution kills duplicate
agent runs:** `check_static` (seconds) → smoke gate = the upgrade
scenario (most change-sensitive: owned layer, manifest, keep, Step 7) →
the full remaining corpus only on green smoke; idempotency ×3 launches
only after a green main corpus (no relaxation — every assert still runs;
what disappears is running five scenarios × retries off a dead-on-
arrival change). (3) **Notifications:** desktop `notify-send` on
per-scenario DONE / whole-run DONE / FAILED; a machine-readable
`$WS/status.md` (stage, attempt, last log line) as the session contract —
the interactive session polls a file, never a process, and its context
does not grow with run logs. Workspace stays outside the repo
(`/tmp/legislator-eval-vN/`). (4) evals/README documents the background
procedure beside the manual one.

**Why:** the v17 benchmark could not run in-session: provider stream
drops × five parallel agents killed the night run; interactive polling
starved the session (multi-minute sleeps inside tool calls); every
failed attempt re-ran everything. The runner is already de-facto
specified by two days of live iteration — this case writes it down.

**Done when:** `tools/evals-bg.sh` runs a full corpus unattended to
completion with resume surviving an induced stall; notify-send fires on
scenario and run boundaries; status.md contract documented; staged
order (static → smoke → corpus → idempotency) enforced by the script;
README section landed.

## Note — OKF content-accuracy check is an open idea, not yet a backlog item

The fleet-api v11 backfill (2026-07-12) found six `docs/okf/domain/*.md`
files still describing a domain model an ADR had removed — no audit check
covers OKF *content accuracy against source* (check 5 covers links, check 6
the map's shape). **Resolved 2026-08-20: promoted into BL-033** — the
source-symbol grounding (anchors) + the OKF-sync-debt audit check are the
designed mechanism; this note stays as the origin record.

## BL-038 — File-authority matrix: one table resolves every mode's rights over every artifact class

**Status: GREEN 2026-08-23 — corpus 185/185 and idempotency ×3 zero-diff on one law generation (`67e14c0`); model floor `sonnet` (Claude Code 2.1.239), unchanged from v17. Benchmark `evals/benchmarks/v18.md`; spec `docs/superpowers/specs/2026-08-22-file-authority-matrix-design.md`, plan `docs/superpowers/plans/2026-08-22-file-authority-matrix.md`. The benchmark forced five fixes — three law (`heal`'s missing manifest cell reference; `never-touch` read as "report and stop", contradicting `heal`; harvest scanning only `AGENTS.md` and missing the pre-v14 entry document audit finds) and two grader (an order-sensitive check-14 marker; a delegated `heal` write judged by restructure's own column). Edition v18 closes at merge; tag `v18`. Final review residue (one prose right the wall cannot see; the `replace`/manifest tension) filed as BL-041 for v19.**

**What:** replace the prose statements of "may this mode write this file?"
with a single matrix — artifact class × mode → one permission from a closed
vocabulary — and make every other mention a reference to it. The table is
the law; nothing else states file rights.

**Origin — the incident that raised it.** v17's benchmark caught the
`legacy-migration-agents-first` scenario failing three asserts on a
migration run that had left `AGENTS.md` untouched. Root cause was not the
model: the same fact was stated in four places, and one of them stated a
single mode's constraint as a property of the file — Step 7's "`AGENTS.md`
is project-owned, so the Legislator never edits it directly", written
unconditionally while Step 4 and Step 5 both write that very file. Step 5
and Step 7 execute in the same migration run, so the procedure contradicted
itself mid-run. The contradiction then propagated: the eval runner's ground
rules quoted Step 7 near-verbatim, and the harness ended up forbidding
exactly what it was testing. Two model families had been hiding it by
ignoring the harness rule and following the skill; a third resolved the
conflict the other way and exposed it. Fixed for v17 by stating the
invariant once (SKILL.md's **Entry-document authority**), which is the
narrow repair — this item is the general one.

**Why a matrix and not more prose.** The invariant that resolved this case
turned out to be structural, not verbal: the boundary is *whether
`docs/ai/manifest.json` exists* — the layer is being installed (the entry
document is the skill's to write) or it is being maintained (the document
is the owner's, and the skill proposes). That boundary governs more than
the entry document, and today each artifact class re-derives it in prose.
A table makes the shared axis explicit and the exceptions visible as cells
rather than as hedges ("even though AGENTS.md is otherwise project-owned"
was a hedge around an overstated rule).

**Sketch.** Rows: entry document, owned rules under `docs/ai/rules/**`,
manifest, Step 4 scaffolded artifacts, project rules under
`.claude/rules/`, OKF docs, foreign structures, kept paths. Columns: the
modes, grouped by the state they act on rather than by verb (the naming
question is part of the work — "installing" vs "maintaining" is the axis
the incident exposed; per-mode names should say which repo state they
assume). Cells: one of a closed set — `create-if-absent`, `write`,
`rewrite-lossless`, `propose-only`, `delete-dead-wiring`, `read-only`,
`never-touch`.

**Why it pays beyond tidiness.** `grade.py` already derives its
expectations from tables in the skill source — `SCAFFOLD_ARTIFACTS` from
Step 4, the protected set from it, migration wiring from `AGENTS.md.tpl`,
audit severities from the check list, restructure actions from
`restructure.md` §2 — and `selftest:derivation` asserts those derivations
stay alive (BL-036). A permission matrix is the natural next derivation:
the grader's "project-owned files untouched" expectations would come from
the table instead of hand-maintained lists, and a divergence between law
and grader would become impossible to introduce silently. The eval prompt
would stop restating law altogether, which is what let this defect spread.

**Done when:** the matrix exists as one table; every prose statement of
file rights is replaced by a reference to it; `grade.py` derives its
protected/writable sets from the table; `selftest:derivation` asserts that
derivation; the benchmark is green with no scenario regression.

**Risks:** the permission vocabulary must stay small — a cell that needs a
sentence is a sign the closed set is wrong. And the matrix must not become
a *second* place stating file rights next to surviving prose; the migration
is only done when the prose is gone.

## BL-039 — Split the eval suite into its own repo

**Status: PROPOSED 2026-08-22 — raised while hardening the artifact boundary
in the v17 cycle.**

**What:** move the suite — fixtures, `grade.py`, `setup_workspace.py`,
`tools/evals-bg.sh`, `dashboard.py`, `streamfmt.py`, `POLICY.md` — into a
repo of its own, which pulls in a version of the legislator skill and runs
against it. The skill's repo keeps only what is *about the skill*.

**Why — the boundary should be structural, not a discipline.** An eval run
happens on a developer's machine and produces graded output, agent
transcripts, raw event streams and a dashboard. Those carry absolute local
paths, this machine's installed-skill list, and whatever prose an agent
wrote here. Today nothing stops such a file from being committed except
`.gitignore` and attention — and attention already failed once:
`evals/grading.json` sat tracked in the repo long after the code stopped
writing there, and `selftest:derivation` wrote a fresh artifact into the
repo tree on every invocation. Both fixed 2026-08-22, but the fix is a
patch on a category of mistake, not a wall against it. Two repos make the
wall: nothing a run produces can even be staged in the skill's repo,
because the run does not happen there.

**Second reason — the suite has its own lifecycle.** It is now roughly as
large as the thing it tests, and it has its own defects, its own fixes and
its own release bar (`POLICY.md`). In the v17 cycle alone the suite
accounted for more repaired defects than the law did — four grader defects
and two harness defects against two law defects. Work of that weight
deserves its own history, not entries interleaved with constitution
changes.

**Design questions to settle first — none of them obvious:**

- **How the skill under test is supplied.** A git submodule pinned to a tag
  is the honest form (the suite states which edition it measured), a path
  argument is the convenient one (what the runner does today). Probably
  both: a pinned default, a path override for local iteration.
- **Where `benchmarks/v<N>.md` lives.** These record *the edition*, not the
  suite — the pass rate, the model floor, the confounds and the defect
  chronicle are properties of a legislator version. Argument for keeping
  them with the skill; argument against: they are produced by the suite and
  would then be the one artifact crossing the boundary. Decide deliberately.
- **How the "testing is mandatory" rule reaches across two repos.** Today
  `CLAUDE.md` can point at `evals/POLICY.md` by relative path. After a
  split it must point somewhere real and enforceable, or the rule quietly
  becomes advisory.
- **Whether `check_static.py` follows.** It needs no agent, no workspace and
  no artifacts — it is a lint of the skill source, and it plausibly belongs
  with the skill even after everything else leaves.
- **What the fixtures may name.** They are synthetic today (InvoiceApi,
  LegacyBilling) and must stay that way; a separate repo is a good moment to
  state that as a rule rather than a habit.

**Interim measure (already applied 2026-08-22):** the stale tracked
artifact removed, `selftest` redirected to write into the workspace instead
of the repo tree, and `.gitignore` extended to cover graded output,
transcripts, raw streams, prompts, queue/status files and the dashboard.

**Done when:** the suite runs from its own repo against a pinned skill
version; the skill's repo contains no eval machinery beyond whatever the
`check_static` decision leaves; the benchmark-location question is answered
in writing; and a full edition cycle has been driven end-to-end across the
two repos at least once.

## BL-040 — Redact git history, not just the working tree

**Status: PROPOSED 2026-08-22 — the other half of the redaction done that
day; deliberately deferred because it rewrites every commit hash.**

**What:** rewrite the repository's history so that fleet repository names
and absolute local paths are gone from past commits too, not only from the
current tree — `git filter-repo` with the same mapping the working-tree
redaction used (`~/.claude/legislator-fleet-aliases.md`).

**Why it is not already done.** On 2026-08-22 the working tree was redacted
in three commits: 34 mentions in live docs, 71 names and 46 absolute paths
in historical specs and plans, plus a `check_static.py` guard so the names
cannot grow back. None of that touches history. All ~110 original mentions
and 46 paths remain reachable in earlier commits with a single command, so
**the current state gives privacy against a reader of the tree and none
against a reader of the repository.** Anyone treating the redaction as
sufficient before publishing would be wrong.

**Why deferred rather than done.** History rewriting changes every commit
hash from the first affected commit onward. That invalidates any clone,
requires a force-push, breaks every existing reference to a commit (this
backlog and several specs cite short hashes; benchmark records carry law
generation stamps of the form `v17-<commit>-g<hash>`), and cannot be
undone selectively. It is a one-shot operation that wants a deliberate
moment, not a Friday.

**Do it before, and only before, one of these:** the repository becomes
public or is pushed to a host outside the owner's control; it is shared
with anyone outside the fleet; or BL-039 splits the eval suite out (a good
moment, since the new repo starts clean and this one is already being
restructured).

**Done when:** history carries no fleet name and no absolute local path;
the law-generation stamps in `evals/benchmarks/*.md` and the commit
citations in `docs/` are reconciled with the new hashes, or explicitly
declared stale with a note saying why; and the decoding key still resolves
every alias used anywhere in the rewritten history.

**Check first:** whether any commit message (not just file content) carries
a name or path — `filter-repo` handles both, but the two need separate
expressions, and a message is easy to forget.

## BL-041 — File-authority residue: one prose right the wall cannot see, and the `replace` carve-out for the manifest

**Status: GREEN 2026-08-23 — corpus 185/185 and idempotency ×3 zero-diff on one law generation (`a8584aa`); model floor `sonnet` (Claude Code 2.1.241), unchanged from v18. Benchmark `evals/benchmarks/v19.md`. Edition v19 on its own (see the edition plan; was "rides v19 as a rider" until the 2026-08-23 decision), departing from the residue-rides-the-next-cycle precedent of BL-011…023. Item 2 settled as a carve-out in the `replace` bullet, not a ninth value. All five new asserts were committed in their red state (`d732b57`) before the fix that greened them (`a8584aa`) — the habit this case asked for, now reproducible from history. The only reds this cycle were one model flake (`report_proposes_stack_import_line`, cleared by a re-run) and a session-quota kill that cost three scenario runs, all clean on retest; no law or grader defect was found by the benchmark. Edition v19 closes at merge; tag `v19`.**

**What:** the two Important findings the v18 final review deferred rather
than pay a re-benchmark for wording, plus three grader/static-check
hardenings from the task reviews.

1. **A prose right survived** — SKILL.md's Step 4 row for `CLAUDE.md` still
   ends "Create-only-if-absent", and the Step 4 header says "the right is
   `create-if-absent`" over rows whose classes are *entry document* and
   *project rules*. The wall's regex knows `create-once` and `create it
   only if` but not the hyphenated form. Fix: row note → `(authority: entry
   document × scaffold)`; narrow the header to the rows it governs; add
   `create-only-if-absent` to `AUTH_PROSE` — shown red against v18 first.
2. **`manifest × upgrade = replace` read literally licenses discarding
   `keep`** — `replace` is defined as "comes whole from the skill, byte-for-
   byte, never merged", but the manifest is generated with `keep` carried
   forward and `ownedFiles` recomputed (Step 3.6). Practice is right and
   `manifest_healed_keep_carried` guards it; the definition is the same
   hazard shape v18's defect L2 paid to discover. Fix: a manifest carve-out
   in the `replace` bullet (or a ninth value — decide in-cycle; a ninth
   value is a deliberate spec edit, not drift).
3. **Grader/wall hardenings:** anchor the `[heal]` delegation gate in
   `grade_restructure` to an item line (`^\d+\. \[heal\]`), not a bare
   substring; exact-length row check in both `authority_matrix()` and the
   wall (a ninth body row is currently ignored, not rejected); scan
   SKILL.md line-by-line with a section-exclusion range so the wall's
   reported line numbers are real (today they are short by the section's
   length for everything after it).

4. **Delegation stated twice:** SKILL.md's `never-touch` bullet names the
   two heal-delegated classes (owned law, manifest) in prose, while the
   machine-readable cell references live only in `restructure.md` §2's heal
   bullet. A third delegated class added to §2 would leave SKILL.md's prose
   silently stale — `heal_delegation_derived` would catch it through the
   grader, not the wall. Fix: SKILL.md points at §2's bullet instead of
   restating the classes.

**Habit for v19, from the same review:** commit a new assert in its red
state *before* the fix that greens it, so red-before-green is reproducible
from history, not only from the benchmark record.

**Done when:** the wall goes red on the `CLAUDE.md` row note before the
edit and green after; `replace`'s manifest reading is settled in the
vocabulary; the three hardenings carry asserts; benchmark green.

## BL-042 — The corpus verdict must not be overwritable by a grade of a mutated fixture

**Status: DONE 2026-08-23 — shipped in edition v19 (`evals/**` only: no VERSION bump, no benchmark per README's testing rules). Verified mechanically rather than by a corpus run, at the user's direction; the first agent-run confirmation comes free with v20.**

**What:** two changes to the eval harness's record-keeping.

1. **A guard in `grade.py`.** Every fixture carries an `eval-base` tag
   (`setup_workspace.py`), placed there for precisely this hazard — its own
   comment says so. Before grading any corpus scenario the grader now
   compares the fixture's `HEAD` against that tag and, when they differ,
   **refuses**: it prints how far the fixture has moved, why the grade would
   be wrong, where the authoritative verdict lives, and the one command that
   restores the fixture. Nothing is computed and nothing is written.
   `idempotency:` is exempt — grading the committed run-1 state is its job.
2. **The verdict carries its generation.** `grading.json` gains the `law`
   stamp (skill VERSION + repo HEAD + grader hash) that
   `grade-history.jsonl` already recorded, and the dashboard renders a
   mismatch as an error line instead of showing the number bare.

**Why:** found by the v19 cycle, from the inside. The idempotency stage
commits `run 1` into the fixture *on purpose* (`tools/evals-bg.sh`
`idem_scenario`). A later re-grade of the same scenario therefore measures a
different repo state — `nothing_committed` requires an uncommitted tree and
one seed commit — and `grading.json` is overwritten **in place**. Three v19
scenarios were re-graded after the idempotency stage during a confound
check and their dashboard verdicts became 20/21, 18/19 and a
provenance-shifted 34/34, while the true corpus verdicts (21/21, 19/19,
34/34) survived only in the append-only `grade-history.jsonl`. Nobody was
misled for long, but the failure mode is the one POLICY §8 names: **one
fact in two places, and the mutable copy is the one on screen.** The
underlying shape is worse than the incident — any future re-grade, by any
hand, silently republishes a verdict for a state the corpus never measured.

**Why a guard and not a POLICY line:** POLICY §8 argues the countermeasure
for a mechanical hazard is a mechanism. A rule saying "do not re-grade after
the idempotency stage" would be a fourth thing to remember, and the
anchor to enforce it already existed unused.

**Verification (no agent run, by the user's decision):** at `eval-base`,
`upgrade` grades 21/21 and the verdict now carries
`law=v19-6a48231-g334ad2c`. With the idem commit replayed by hand, the same
command prints `== upgrade: REFUSED ==`, exits 1, and leaves `grading.json`
holding the 21/21 it already had — the exact damage, now impossible.
`idempotency:upgrade` still grades. All eight scenarios re-grade to
185/185 with the guard in place, and the runner's only corpus-grade call
site (`tools/evals-bg.sh:313`) fires immediately after a run, when the
fixture is at its base.

**Residue for whoever next touches this:** `grading.json` remains a second
copy of a fact `grade-history.jsonl` owns. The guard and the stamp make
divergence loud, not impossible. Collapsing the two — the dashboard reading
the append-only record and `grading.json` becoming a derived cache or
disappearing — is the real fix, and it is a bigger change than this one.

## BL-043 — Generated baseline and the spec/plan linter (edition v21)

**Status: queued 2026-08-23 → edition v21**

**What:** the baseline generator (`R-NNN` ↔ annotated tests →
`docs/ai/baseline.md`), the linter and its binding in `core/sdd.md`'s
analyze gate, the population of the `generated` role class in
`core/artifact-lifecycle.md`, and the fleet-obs registry `generated`
content-type with its gold-panel exclusion.

## BL-044 — Cross-harness parity: the asymmetry study and channel subtraction

**Status: PROPOSED 2026-08-23** — research case (no `skill/` change, no VERSION, no benchmark; its deliverable is a spec the two cases below consume). Raised while v20 was in flight, from a review of how Claude Code and opencode each assemble a session's context.

**What:** establish by experiment — not by reading either tool's documentation — what each harness actually loads and what can be prevented from loading, then record the findings as a spec.

Three questions, each answered by a reproducible probe:

1. **What is actually eager, per harness, in a legislated repo.** Claude Code concatenates from the filesystem root down (managed policy → user scope → project → `CLAUDE.local.md`) and expands `@`-imports recursively; opencode walks up from the working directory and takes the **first** entry document it finds, then adds `instructions` from `opencode.json`. Measure both with a canary token planted per file, so the answer is observed rather than inferred.
2. **What can be subtracted.** `claudeMdExcludes` (glob over absolute paths) and `OPENCODE_DISABLE_CLAUDE_CODE`. For each: what it removes, whether it can be set from inside the repository or only per machine, whether the legislator may own that setting, and what remains unremovable — a user's own `~/.claude/CLAUDE.md` is expected to be unremovable; confirm it rather than assume it.
3. **Where the two diverge in our own delivery today.** Three are known and to be confirmed and completed: the glossary is listed in `opencode.json`'s `instructions` but is not `@`-imported by `AGENTS.md.tpl`; a newly subscribed stack is picked up automatically by opencode's `stacks/*/*.md` glob but needs an `@import` line in the entry document that an upgrade may only *propose* (authority: entry document × upgrade); a nested entry document in a monorepo is additive under Claude Code and substitutive under opencode.

**Why:** the two harnesses must be interchangeable — the same repository must govern a session identically whichever tool opens it. That is asserted today and never measured, and the glossary divergence is proof the assertion does not enforce itself. It is also the only honest input for a law: a rule written from documentation is a rule about documentation.

**Done when:** a spec under `docs/superpowers/specs/` records, per harness, the observed eager set, each removable channel with its mechanism, and the unremovable remainder; every claim in it is backed by a probe another person can re-run; the three known divergences are confirmed or corrected; and each finding is marked enforceable-by-the-legislator or advisory-for-the-owner.

## BL-045 — One declaration, two projections: the owned import index

**Status: PROPOSED 2026-08-23** — behavioral (`skill/` changes, VERSION bump + full e2e). The mechanism was settled in discussion 2026-08-23; the cycle it rides is not.

**What:** the eager set stops being two hand-maintained lists and becomes one machine-written declaration with a projection per harness.

- A new owned file, `docs/ai/rules/index.md`, lists every delivered rule as an `@`-import. Step 3 writes it like any other owned file and rewrites it on every run.
- `AGENTS.md` carries exactly one wiring line — `@docs/ai/rules/index.md` — written once at scaffold and never edited again (`@`-imports resolve recursively).
- `opencode.json`'s `instructions` remains the second projection, and a static check derives both from the same source and fails on divergence — the pattern BL-038 established for the file-authority table.

**Why:** three problems collapse into one fix. Parity stops being something to check and becomes structural. The **propose-only bottleneck disappears**: today a newly subscribed stack loads immediately under opencode and only after the owner applies a proposed `@import` line under Claude Code, so the repository is governed differently by the two tools for as long as that proposal sits unapplied. And the entry document stops carrying machine wiring at all, becoming what it is meant to be — project-instance data.

**Done when:** `docs/ai/rules/index.md` ships as an owned file and appears in `ownedFiles`; `AGENTS.md.tpl` carries one import line in place of the block; adding or dropping a stack changes the eager set in both harnesses with no owner action; the static check derives both projections from one source; and the corpus carries an assert that a stack added during an upgrade is loaded by both wirings without any applied proposal.

## BL-046 — Context-scope law: four classes, enforced and advisory, proven by canary

**Status: PROPOSED 2026-08-23** — behavioral, its own edition. Depends on BL-044's findings and is cleanest after BL-045's single declaration. Recommended **before** BL-043: that case introduces a new artifact class into every repository, and without this law the question "is the baseline eager?" gets answered in passing — which is exactly how the glossary divergence happened.

**What:** context becomes a governed resource, with a declared scope per artifact class, stated in `core/`:

- **eager** — the law and the entry document: loaded every session, in both harnesses, identically.
- **lazy** — reached only when needed. Our lazy channel is **skills, and only skills**: path-scoped rules exist in Claude Code and have no opencode equivalent, so any economy taken through them yields a repository governed strictly under one tool and loosely under the other, silently.
- **deliberately excluded** — named, with the subtraction mechanism per harness (BL-044 supplies them), and an honest split between what the legislator can enforce and what only the repo owner can.
- **dynamically added on scope growth** — a newly subscribed stack, and any future class, enters the eager set in both harnesses at once (BL-045 is what makes this true).

The law states the classes; the evals prove the loading. Static checks prove the declared set matches the wirings. A **canary scenario** proves the loaded set matches the declaration: a unique token per class, and the agent is asked which tokens it can see without opening a file — eager tokens must be named, lazy must not be, excluded must not surface even under a follow-up question.

**Why:** an always-on layer competes with itself for attention — a constitution is obeyed less the longer it grows, which is why ours holds at roughly 217 lines across nine repositories. Lazy loading here is not a saving on tokens but a defence of the law against dilution. And a scope that is declared but never measured drifts: "documented as lazy, in fact always loaded" is invisible precisely because everything appears to work.

**Done when:** the class law ships in `core/` with the enforced/advisory split stated; the subtraction mechanisms are wired wherever the legislator owns them; the canary scenario is in the corpus and goes red when a class is mis-declared; and `docs/philosophy.md` records that skills are the only lawful lazy channel.

## BL-047 — Spike: the decision inventory — what is still decided by a model

**Status: SPIKE PROPOSED 2026-08-23** — exploration (the spec type `core/sdd.md` names), time-boxed, no `skill/` change, no VERSION, no benchmark. Its deliverable is an answer, not code.

**The question:** which decisions in this system are taken by a model, and which of them could be taken by code instead?

**The probe:** classify every line of the delivered law (`assets/rules/**` — 217 lines of core plus the stack rules) and every decision point in `SKILL.md`'s procedure into three buckets: **(a)** already enforced by a check, a hook or the engine; **(b)** enforceable by a check that does not exist yet — name the check and estimate its cost; **(c)** genuinely needs interpretation. Count the buckets and rank bucket (b) by how often the decision is taken.

**What the answer decides:** the achievable size of the constitution, and the order in which the engine should grow. Every line moved from (b) to (a) removes a place where a run can deviate, which is the entire content of "predictable output". It also tests a claim this repo has never checked: that its law is *enforceable* law. A rule sitting in (c) that nobody can adjudicate is not law, it is advice — and advice in an always-on file is dilution.

**Why a spike and not a case:** the ranked list is what tells us whether the constitution can shrink by ten lines or by a hundred. Those two answers imply completely different roadmaps, and neither is guessable from here.

**Stop condition:** the inventory is the deliverable. No check is written inside this spike; each bucket-(b) entry becomes its own case, sized from the measurement.

## BL-048 — Spike: per-job model floor — can a small local model take the classification work?

**Status: SPIKE PROPOSED 2026-08-23** — exploration, time-boxed, no `skill/` change, no VERSION, no benchmark.

**The question:** is there any job in this pipeline where a small local model matches the current floor's quality — or is `sonnet` the floor because every job needs judgement?

**The probe:** the pipeline mixes two kinds of work that today share one model. **Judgement** — deciding a restructure plan, splitting a legacy entry document three ways, executing a case under the SDD law. **Classification** — "is this line law-shaped?" (the constitution-candidates test and audit check 12 use the same test), and glossary term extraction. Only the second kind is a candidate. Build a labelled set from fixtures that already exist: the rotted fixture plants law-shaped lines, project-instance lines that must *not* be proposed, and one line suppressed by the not-law marker — the graders' expectations are the labels. Run candidate local models against it and measure agreement with the current floor.

**What the answer decides:** whether "model floor" becomes a per-job property instead of a per-edition one. If it does, the cheap jobs move off the paid model and the corpus gets cheaper to run, which is what makes a larger corpus affordable. If it does not, the answer is worth having in writing so nobody re-opens it.

**Stop condition:** the measured table is the deliverable. No routing is wired, no rule changes; a positive result becomes its own case.

## BL-049 — Spike: how much of the Step 7 report is machine-derivable?

**Status: SPIKE PROPOSED 2026-08-23** — exploration, time-boxed, no `skill/` change, no VERSION, no benchmark.

**The question:** what fraction of a Step 7 report could the engine print from the run's own facts, and where exactly is the seam with the parts that need a model?

**The probe:** take the reports the corpus already produces (fresh scaffold, both migrations, upgrade, stack drop) and classify every line as derivable from facts the run already holds — files created, overwritten, deleted, the keep-list delta, the health checks — or as requiring prose judgement, which is essentially the constitution-candidates section. Then count how many existing grader asserts exist *only* because a model composes the report: the pinned heading levels, the byte-for-byte section names, the order-independence workarounds.

**What the answer decides:** whether the report emitter is a large win or a small one. The suspicion worth testing is that a large share of this repository's historical benchmark defects were report-*shape* defects — a drifted heading level, a missing section, an order-sensitive marker — and that a printed skeleton would delete that entire class along with the asserts guarding it.

**Stop condition:** the classification and the assert count are the deliverable. No emitter is written.

## BL-050 — Stage 1 must verify the workspace was materialized before an hour of agent runs starts

**Status: DONE 2026-08-24** — evals/tooling only, no VERSION, no benchmark.

**Shipped:** stage 1 of `tools/evals-bg.sh` opens with a workspace
precondition, ahead of the static and engine checks. Every scenario
directory the invocation will touch — derived from its own flags, so
`--only` and `--idem` are not blocked by fixtures they never open — must
hold a `repo/` carrying the `eval-base` tag, else the failing directories
are named and the run exits 1 having spawned nothing. The witness is the
tag rather than mere directory presence because it is the same anchor
`reset_repo` trusts: the check measures exactly what the run will later
use. The dashboard now launches after the precondition, so an invocation
that cannot run no longer opens a browser.

**Measured red first:** on an empty workspace the previous script printed
`static green`, spawned three `upgrade` attempts and exited 0. It now names
the missing directories and exits 1 with an empty `orchestrate.log`.

**What:** `tools/evals-bg.sh` spends an hour of agent runs without ever
checking that the workspace was materialized. On 2026-08-23 it graded
`upgrade` 21/21 CLEAN and `legacy-migration-agents-first` 22/22 CLEAN
against fixtures of unknown provenance, and nothing objected. The remedy is
a precondition at stage 1 — every scenario directory holds a `repo/`
carrying an `eval-base` tag, else exit non-zero — which converts an hour of
waste into an instant error and closes a path on which a benchmark can read
green while measuring nothing.

**Why:** found by the v20 cycle (`evals/benchmarks/v20.md`, "Harness
finding"). The first corpus attempt (`20260823-1716`) was launched against
a workspace that had never been materialized — `evals/README.md` step 1 is
`python3 evals/setup_workspace.py <ws>` and the runner does not do it
itself. Agents improvised in absent repositories and two scenarios had no
`repo/` directory at all, yet two other scenarios graded CLEAN anyway. An
hour of agent runs ran to completion with no check standing between a
missing workspace and a green verdict.

**Done when:** stage 1 of `tools/evals-bg.sh` refuses to proceed — before
any scenario agent runs — unless every scenario directory it is about to
use holds a `repo/` carrying the `eval-base` tag, printing which
directories failed the check and exiting non-zero.

## BL-051 — v20 final-review residue (edition v21)

**Status: DONE 2026-08-25 — edition v21 shipped.** 199/199 corpus in one pass,
idempotency ×3 zero diff, one law generation, model floor `sonnet` reproduced.
Record: `evals/benchmarks/v21.md`. Case home:
`docs/cases/BL-051-v20-residue/`.

All five items resolved: `status: removed` documents exempt from anchoring in
law and engine; build output excluded at any depth, not only top level; the
engine exits 3 on an unhandled exception and checks 15/17 treat an exit outside
`{0,1}` as a check failure; both checks carry the `python3`-absent branch;
both keep refusals name the whole owned set; and `audit-engine-absent` is a new
corpus scenario covering check 15's engine-absent branch, which shipped in v20
as law with no measurement.

**The corpus found five law defects on the way, none of them in the five
items.** Four were scope-or-completion — a rule correct but silent about where
it applies or when you are done — the shape nine of thirteen law defects in
this repo's history take. One of them, the suppressed-line narration, had been
filed as **model-class in v18 and left unfixed**; it returned three editions
later and was law all along.

**A model floor was nearly raised for the wrong reason.** Two reds were
classified model-class and an opus corpus run was launched; the owner stopped
it with *"if something breaks on sonnet, work out the situation and fix it,
rather than raising the floor."* Both were law ambiguity. `POLICY.md` §1 now
carries the burden of proof this produced, and the model class's 0-for-3
record across the whole history.

**Status: was PROPOSED 2026-08-23** — behavioral (`skill/` changes, VERSION
bump + full e2e — which is exactly why it is not being done now).

**What:** five findings from the v20 final review, none acted on in v20 so
the edition's benchmark record (measured against law generation
`v20-9dbb306-g22c1e5f`) would stay valid:

1. **`status: removed` documents can never be clean, and the rung is
   global.** `core/okf.md` tells an owner to keep a document for a removed
   concept and mark it `status: removed`; the anchored class then covers
   it, and `core/verification.md`'s rung makes a broken anchor block "done"
   for *any* task in that repository. A document behaving exactly as the
   checklist demands wedges unrelated work, and restructure cannot help —
   it routes such findings to `## For the team:` by design. This is the
   same "a class that systematically yields no action is excluded
   mechanically" argument that excluded directory anchors from the debt
   job, carried one step further. Remedy: exempt `status: removed`
   documents from anchoring, in the law and in the engine.
2. **Nested build output is not excluded, only top-level.** The engine's
   ignore list applies to top-level directories; a stale
   `src/App/obj/Debug/App.dll` containing a removed symbol makes that
   symbol resolve, so the check silently misses the rot it was built for,
   and a clean CI clone and a developer clone disagree about a gate on
   "done".
3. **A crashing engine audits clean.** An unhandled exception exits 1 with
   empty stdout, and audit checks 15/17 read stdout lines only, so a crash
   reads as "no findings". The verification rung fails closed; the audit
   fails open. Remedy: a top-level handler returning a distinct exit code,
   and law in checks 15/17 saying an exit beyond the findings code is a
   check failure, not a clean check.
4. **Checks 15/17 have no `python3`-absent branch**, though
   `core/verification.md` gained one — the audit's behaviour on such a
   machine is undefined.
5. **Two smaller slips:** the `keep` refusal in Step 3.6 names only owned
   files "under `docs/ai/rules/`", so `docs/ai/engine.py` can now be
   keep-listed, putting the kept-paths row and the owned-law row in
   conflict; and check 15's "bundle present, engine absent → Info" branch
   has no fixture exercising it.

**Why:** found by the whole-branch final review that closed BL-033 (v20),
after the edition's benchmark was already recorded. None of the five is a
regression against v19 — each is a gap the anchor engine's own design
surfaces once it exists — but each is real: two are correctness gaps in
what the engine covers (1, 2), one is a fail-open failure mode in what the
audit trusts (3), one is an unhandled-environment gap the static rung
already closed for a sibling check (4), and two are small conflicts between
what the law says and what the mechanism allows (5).

**Done when:** all five items are resolved — `status: removed` documents
excluded from anchoring in law and engine; nested build-output directories
excluded from anchor resolution at any depth; the engine exits a distinct
non-zero code on an unhandled exception and checks 15/17 treat that exit as
a check failure; checks 15/17 carry a `python3`-absent branch matching
`core/verification.md`'s; the Step 3.6 `keep` refusal's owned-files
description matches what is actually owned; and a fixture exercises check
15's "bundle present, engine absent → Info" branch. Full e2e benchmark
recorded per `evals/README.md`, compared against v20.

## BL-052 — Spike: does the constitution load at all outside the two harnesses we test?

**Status: SPIKE PROPOSED 2026-08-23** — exploration, time-boxed, no `skill/` change, no VERSION, no benchmark. Widens BL-044 (Claude Code ↔ opencode) to the whole provider field; BL-044 stays the deeper study of the two harnesses this repo actually measures.

**The question that makes this urgent, and it is arithmetic rather than opinion.** A legislated repo's `AGENTS.md` is **1,122 bytes**, of which twelve `@`-import lines stand in for **25,778 bytes** of delivered law. `@path` expansion is a Claude Code feature; opencode never parses it and gets the law through a second channel, the `instructions` array in `opencode.json`. Every other agent that reads `AGENTS.md` — Codex, Cursor, Copilot, Windsurf, Amp, Gemini CLI, Antigravity — would therefore load a kilobyte of pointers and **none of the law they point at**. If that holds, the constitution today is enforced in exactly two harnesses and is decorative in the rest, which is not what "AGENTS.md is the canonical entry document" implies to anyone reading it.

**The probe, in the order that answers the most per hour:**

1. **Load or not.** For each agent, open a legislated repository and ask a question answerable only from a rule body (not from the pointer line). A canary token planted in one rule file makes this binary. Do not read the vendor's documentation for the answer — that is what produced this backlog entry, and it is exactly the class of claim this repo requires a probe for.
2. **Adapter shape per agent.** For each one that fails (1), determine the cheapest wiring that fixes it, and classify it: **thin** — a pointer, a symlink, or a config array naming files (`CLAUDE.md` → `AGENTS.md`, opencode's `instructions`) — or **thick** — a mechanism that requires the rule *text* to be duplicated into a tool-specific file (Cursor `.mdc` for glob scoping, Copilot `.instructions.md` with `applyTo`). This is the decisive question for this project: **a thick adapter is a second copy of the law, and a second copy rots.** A thick adapter is only acceptable if it is *generated* from the same single declaration — which is what BL-045 builds — never hand-maintained.
3. **Truncation headroom.** Codex concatenates from the repository root down with a byte ceiling (`project_doc_max_bytes`, 32 KiB by default) and truncates **silently**. Measure what a legislated repo actually feeds it once the law loads at all: 25,778 bytes of rules is already ~79% of that ceiling before the entry document, the codebase map, or any project rule is counted, and v20 grew the law. Silent truncation of law is indistinguishable from law that was never written.
4. **Activation modes.** Cursor and Antigravity both offer four (always / glob / model-decision / manual); our law has exactly one, always. Confirm that targeting "always" is portable everywhere, since that is the only mode every agent shares — the same conclusion BL-046 reaches for two harnesses, generalized to the field.
5. **Collisions and inheritance.** Two worth checking because they bite silently: Codex's `AGENTS.override.md`, the only inheritance-breaking mechanism in the field and a possible model for our monorepo gap; and the report that Gemini CLI and Antigravity disagree over the same `~/.gemini/` home.

**What the answer decides:** whether "the legislator governs a repository" is a claim about repositories or a claim about two harnesses. If thin adapters cover most of the field, the fix is small and BL-045's projection model absorbs it. If several agents need thick adapters, then either the fleet's tool choice narrows deliberately, or the generated-projection machinery becomes load-bearing for the whole system rather than a convenience — and that is a much larger commitment, worth knowing before it is made by accident.

**Stop condition:** the measured table — agent × loads-the-law × adapter shape × truncation headroom — is the deliverable. No adapter is written inside this spike; each becomes its own case, sized from the measurement.

## BL-053 — `fleet.sh` has one engine and no failure signal

**Status: DONE 2026-08-24** — tooling only (`tools/fleet.sh`), no `skill/` change, no VERSION, no benchmark. Both defects were met on the same run: the v20 sweep of 2026-08-23.

**Shipped:** the invocation moved behind `run_upgrade_agent()` and takes a
runner profile with the same shape `evals-bg.sh` uses (`--runner` / `RUNNER`,
`opencode` by default, `claude` as the second profile); an unknown profile is
rejected once, before the first repository. The `claude` profile passes its
prompt on **stdin by construction**, so item 3's `--add-dir` trap cannot
recur whatever flag order a later edit introduces. The five outcomes — ok,
still behind, failed, skipped-dirty, excluded — are counted separately, and
the sweep exits non-zero when any repository did not reach the current
version; an `--exclude` is the operator's own decision and does not fail the
sweep, `--dry-run` never fails. Verified over a fake scan root with a stub
runner: every outcome branch, both profiles, an unknown profile, `--dry-run`,
and a runner that exits 0 without draining stdin. Each failure case exited 0
before the change.

**Found while implementing, and NOT fixable here — a case for `fleet-obs`.**
The `claude` profile has no counterpart to `--agent service-fleet`. That
project ingests Claude Code sessions (it ships a hook adapter and a
transcript miner), but only its opencode miner records an agent identity into
bronze; the Claude adapter's `raw` is a clone of the hook payload, which
carries no such field. Its silver and gold views select service sessions with
`raw.agent_mode LIKE 'service-%'`, so the predicate can never match a Claude
Code session — and by its ADR-0039 an unmarked session counts as practice.
A sweep run under the `claude` profile therefore inflates exactly the lenses
that ADR was written to protect. The launcher side is already correct; the
missing half is a marking path in that project's Claude adapter. Recorded at
the invocation in `fleet.sh` so the operator reads the next report knowing
it.

**Not the whole of it, and not accepted as shipped.** This profile went in
with *two* divergences from the opencode one — the service marking above, and
the fact that it runs under the operator's full ambient context while the
opencode profile runs under a dedicated service agent. Two accepted-in-passing
divergences in one profile is a design signal, not a pair of gaps: nothing in
this repo states what a profile owes. See **BL-054**, which takes
interchangeability as the invariant and measures every axis against it. The
two divergences here are its first inputs.

**What:** two independent faults in the fleet delivery tool, plus one caller trap worth recording beside them.

1. **One hardcoded engine.** Delivery is `opencode run --dir "$repo" --agent service-fleet`, with no alternative. On 2026-08-23 that agent's credential failed (`API key is invalid`) and the entire sweep became impossible — not one repository could be upgraded — although the delivery itself is engine-agnostic: the skill's owned-file work is `cp` plus a regenerated manifest, and any harness that can follow `SKILL.md` performs it identically. The repo already solved this problem once: `tools/evals-bg.sh` carries runner profiles (`opencode` / `claude`) precisely so the stages, contracts and grading survive a change of engine. Remedy: the same profile mechanism here, defaulting to `opencode`.
2. **Failures do not reach the exit code.** The `FAIL` branch prints a line and nothing more: no counter, no non-zero exit. On the same run the tool reported three consecutive failures and exited `0`. This is the family BL-050 belongs to — an operation that failed everywhere reporting success at the process level — and it is worse here than in the eval runner, because a sweep is the step that puts law into nine repositories and its result is usually read from a scrollback, not from a grading file. The version re-check after each run (`WARN … still at v<N>`) is the right idea and should feed the same signal. Remedy: count failures and skips, exit non-zero when any repository did not reach the current version.
3. **Implementation note for whoever adds the `claude` profile.** `--add-dir` is variadic: `claude -p … --add-dir "$DIR" "$prompt"` silently swallows the prompt as a second directory and the agent starts with no task. Pass the prompt on stdin, or place a flag between `--add-dir` and the positional argument. This cost one wasted pass on 2026-08-23; `evals-bg.sh` avoids it by accident, because other flags follow its `--add-dir`.

**Why:** the sweep is the moment the constitution stops being a local artifact and becomes law in other people's repositories. A tool that cannot run when one vendor's credential expires, and that cannot tell success from total failure in its own exit status, is the weakest link in that chain — and neither fault is visible until the day it matters.

**Done when:** `fleet.sh` takes a runner profile with the same shape `evals-bg.sh` uses and the sweep completes under either engine; a run in which any repository fails or stays behind exits non-zero; and the `--add-dir` trap is either avoided by construction in the new profile or noted where the invocation is built.

## BL-054 — Spike: full engine interchangeability as a design invariant, not a per-tool option

**Status: SPIKE DONE 2026-08-24** — exploration, time-boxed, no `skill/`
change, no VERSION, no benchmark. Its deliverable is an answer and a
recommended contract, not code.

**Answer:** `docs/superpowers/specs/2026-08-24-engine-interchangeability-spike.md`.
Nine findings, each probed rather than read. The sharpest is **F1**: the two
write-guard arms protect different sets — `docs/ai/engine.py` is blocked
under Claude Code and **allowed** under opencode, so under that engine an
agent may rewrite the anchor engine whose findings gate "done" via
`core/verification.md`'s rung. It entered in v20, when the Claude arm was
extended and the opencode arm was not. **F2** is why it shipped: the two
guard suites (`evals/check_hooks.py`, `evals/check_opencode_plugin.mjs`) are
independent hand-written sets deriving from no shared declaration, and both
were green through a whole edition. F1 is the symptom; F2 is the defect.

Also measured: the two delivery profiles start their agents from different
states (F4 — the claude one loads the operator's personal `CLAUDE.md` and can
invoke an installed `legislator` skill instead of reading the files the prompt
points at); opencode has **no** `--safe-mode` equivalent, so the eval suite's
fair-harness property is achievable under one profile only and is
declarable-only, not closable (F5); permission posture differs in the
dangerous direction (F6); model identity has no shared vocabulary, making
"the same model floor under both profiles" currently **unexpressible** (F7);
and `tools/link-skills.sh` is Claude-Code-only by construction (F8). The
formatter arms are at parity; the stall oracle, kill pattern, resume flag and
prompt channel are legitimate adapter differences, not divergences.

**Recommendation:** the guarantee cannot live per-invocation — every
divergence entered the same way, a profile added in one file by one case with
nothing stating what a profile owes. A stated **engine contract** (seven
properties, one adapter per engine, a mechanical conformance check in the
commit gate, and a named exemption list for the declarable-only ones) is the
recommended shape; it is BL-045's "one declaration, two projections" applied
to invocation instead of imports, and the two should share a mechanism.

**Also confirmed during the run:** the opencode credential is *still*
invalid, so the fleet is deliverable today only under the claude profile —
the emergency path is the only path. `opencode run` does exit `1` on that
failure, so BL-053's FAIL branch fires correctly against it.

**Five cases sized, none filed** — the spec lists them; order is the user's
call.

**The principle, settled by the user 2026-08-24 and not in question here:**
every part of this resource must work under both profiles — opencode and
Claude Code — and the two must be **fully interchangeable**. Where a tool
behaves differently depending on which engine drives it, the tool's design
is wrong; interchangeability is the foundation, not a feature one instrument
may opt into. This spike does not decide the principle. It measures where we
currently violate it and answers *at which layer* the guarantee has to live.

**Why now, and why it is a design question rather than a bug list.** BL-053
added a second engine to `fleet.sh` and shipped it with two divergences
accepted in passing — the same way the glossary divergence entered (BL-044).
That is the signal: divergence is not being introduced deliberately, it is
being introduced because nothing states what a profile *owes*. Two profiles
added by two different cases, in two different files, converge on nothing.

**The inventory to measure, per axis. Four are known to diverge today; the
spike's job is to complete the list and price each entry.**

1. **Delivery — `tools/fleet.sh`.** Five known divergences. *(a)* Service
   marking: the opencode profile passes `--agent service-fleet`, the
   mechanism by which `fleet-obs` excludes a sweep from practice metrics
   (its ADR-0039); the claude profile has no counterpart and none is
   reachable from this side (see BL-053's finding). *(b)* Isolation: the
   opencode profile runs under a dedicated service agent, the claude profile
   under the operator's full ambient context — `CLAUDE.md`, auto-memory,
   hooks, plugins, MCP, installed skills — so the two agents do not start
   from the same state, and one of them can have an installed `legislator`
   fire as a skill instead of being read as a file. *(c)* Prompt channel:
   argv versus stdin. *(d)* Write scope: `--dir` versus `cd` plus two
   `--add-dir`. *(e)* Model identity: `provider/model` strings versus bare
   names, with no shared vocabulary — so "the same model floor" cannot even
   be *stated* across profiles.
2. **Measurement — `tools/evals-bg.sh`.** The suite already declares the
   profiles non-comparable in writing (`evals/README.md`: "pass rates compare
   *within* a profile, never across one"), and each edition's model floor is
   recorded against one engine. Under the principle above that declaration is
   no longer an acceptable resting place — it is the measurement axis of the
   same defect. Also one-sided: `--safe-mode` is what makes the claude
   profile a fair harness, and nothing isolates the opencode profile from the
   machine's global agents and config, so the "fair harness" claim holds for
   one profile only. The stall oracle, the kill pattern and the resume flag
   differ too; those are adapter details and probably legitimate — the spike
   must separate *legitimate adapter difference* from *behavioural
   divergence*, which is the distinction the whole case turns on.
3. **Enforcement — `plugin/`.** Two arms exist: `plugin/hooks/` for Claude
   Code and `plugin/opencode/legislator-guard.ts` for opencode. Whether they
   guard the same operations with the same verdicts has never been measured.
   A write-guard that fires under one engine and not the other means the law
   is enforced in one tool and advisory in the other, silently.
4. **Law loading.** Already owned by BL-044 (the two-harness asymmetry study)
   and BL-052 (the whole provider field). This spike must not re-run them —
   it consumes their findings and covers the axes they do not: delivery,
   measurement and enforcement.

**The design question the inventory serves.** Is the guarantee per-invocation
— each tool's profile block gets fixed once and re-diverges on the next edit
— or is it a stated **engine contract**: a short list of properties every
profile must deliver (isolation posture, prompt channel, write scope, service
marking, permission posture, model identity), one adapter per engine that
satisfies it, and a conformance check that fails when a profile cannot. The
second shape is BL-045's "one declaration, two projections" applied to
invocation instead of imports, and the two cases should probably share a
mechanism. The spike recommends one shape and says what it costs.

**Where interchangeability turns out to be impossible** — the `fleet-obs`
service marking is the current candidate, since the fix lives in another
repository — the answer must say so explicitly and name what is *declared*
instead. An undeclared impossibility is the failure mode this whole case
exists to stop.

**Stop condition:** the deliverable is (a) the completed table — axis ×
profile × observed behaviour × legitimate-adapter-difference or divergence,
(b) a recommended shape for the guarantee with its cost, and (c) each
divergence marked closable-here, closable-elsewhere, or declarable-only. No
adapter is rewritten inside this spike; each entry becomes its own case,
sized from the measurement.

## BL-055 — `fleet.sh` discovery cannot see a repository nested one level deeper

**Status: PROPOSED 2026-08-24** — tooling only (`tools/fleet.sh`), no `skill/`
change, no VERSION, no benchmark. Found by BL-034, which is barred from fixing
it (that case's spec §Boundary).

**What:** discovery is `find $SCAN_ROOTS -maxdepth 4 -path '*/docs/ai/manifest.json'`.
Fleet repos sit at depth 4 and are found; this repository sits at depth 5 and
is not. Having just become fleet member #0, it appears in neither `status` nor
`upgrade`.

**Why it is not a one-character fix.** Raising the depth decides the permanent
delivery channel as a side effect — whether this repo is swept like every other
member, or maintained by a distinct release step. That choice has a real
hazard on one side (a sweep editing the repository that holds the law's source,
mid-edition) and creates a second delivery path on the other, which is the
divergence class BL-054 exists to stop. Decide the channel, then implement it.

**Done when:** the channel is decided and recorded (ADR-0002 leaves it open),
and `fleet.sh` implements exactly that one.

## BL-056 — `fleet.sh status` reports uncommitted work as delivered

**Status: PROPOSED 2026-08-24** — tooling only, no `skill/` change, no VERSION,
no benchmark.

**What:** `status` reads `docs/ai/manifest.json` from the working tree. On
2026-08-24 it reported three repositories at v20 whose committed HEAD was v16,
v16 and v14 — the 2026-08-23 sweep had upgraded them and nobody had reviewed or
committed the diff. The table said 3 of 9 delivered; the true committed answer
was **0 of 9**.

There is a second-order effect: because those repos are dirty, the next sweep
*skips* them, so the tool would leave them in the "upgraded but nowhere" state
indefinitely, saying nothing beyond a `skip` line.

**Why:** the sweep's whole purpose is putting law into other repositories, and
law that is not committed is not there. A dashboard that counts a working-tree
edit as delivery is optimistic in exactly the situation where the operator is
relying on it — the same family as BL-050 and BL-053.

**Done when:** `status` distinguishes committed version from working-tree
version and names any repository where they differ; a repo carrying an
unreviewed upgrade is visibly pending, not `ok`.

## BL-057 — Audit check 2 cannot tell a quoted token from an unfilled one

**Status: PROPOSED 2026-08-24** — behavioral (`skill/` change: SKILL.md's audit
section, VERSION bump + full e2e). Found by BL-034's first audit run, which is
barred from fixing it.

**What:** check 2 (`unresolved-placeholders`) flags any `{{TOKEN}}` pattern in
`AGENTS.md`, any `.md` under `docs/`, or any `.md` under `.claude/rules/`,
exempting only `docs/adr/template.md`. In this repository that yields
**fourteen Critical findings**, every one a token quoted inside backticks in
prose that discusses the templating system — in `docs/backlog.md` and in
thirteen historical specs and plans under `docs/superpowers/**`.

**Why it matters beyond cosmetics.** Critical is the severity that means "the
layer is broken". Fourteen false ones train the reader to skim the Critical
section, which is precisely the section that must never be skimmed. And the
failure is systematic, not incidental: any repository documenting a templating
system trips it, and every repository that legislates *another* repository will
document one.

**Two candidate remedies, both cheap:** skip inline-code spans (a token inside
backticks is a quotation, not a placeholder), and/or exempt the directories
the other checks already treat as history and record — `docs/superpowers/**`,
`docs/cases/**`, `docs/backlog.md`. The first is the more honest: it fixes the
test rather than narrowing where it looks.

**Done when:** a repository whose prose quotes template tokens audits clean,
a genuinely unfilled token in a scaffolded artifact still reports Critical, and
a fixture in the corpus covers both directions.

## BL-058 — The corpus stage cannot see a graded failure

**Status: DONE 2026-08-24** — tooling only (`tools/evals-bg.sh`), no `skill/`
change, no VERSION, no benchmark. Found by the v21 baseline run, fixed in the
same cycle because the v21 corpus could not otherwise be trusted.

**What:** stage 2 (smoke) reads the grading file and stops the run when the
smoke scenario grades below 100%. Stage 3 (corpus) did not. `finish_scenario`
computed the verdict, wrote `partial` into the queue — and returned nothing, so
`CORPUS_FAILED` was set only when a scenario failed to *run*. A scenario that
ran to completion and graded 43/44 left the corpus green.

Measured, not inferred: the 2026-08-24 baseline of `v20` on `claude`/`sonnet`
graded **193/194** with `rotted-layer` at 43/44, and the runner printed
`=== ALL STAGES GREEN ===` and exited `0`.

**Why it is the worst member of its family.** `evals/POLICY.md` §1 makes 100%
the release bar and says there is no known red and no waiver. The instrument
that decides releasability could not see a 99.5%. BL-050 let a run grade
fixtures that did not exist; BL-053 let a sweep fail everywhere and exit 0;
this one let an edition ship below its own stated bar. All three are the same
defect wearing different clothes — a stage that computes a verdict and does not
propagate it.

**Shipped:** `finish_scenario` returns the failed-assert count; stage 3 sets
`CORPUS_FAILED` on a graded failure as well as on a run failure and says which
in `status.md`; the `--only` path exits non-zero when any targeted scenario
fails to run or grades red. Verified by replaying the baseline's own grading
files through the new logic: old verdict GREEN, new verdict FAIL.

## BL-059 — The .NET runtime leaks into `/tmp` until the quota kills every eval run

**Status: PROPOSED 2026-08-25** — evals/tooling, no `skill/` change, no VERSION,
no benchmark. Found by losing three consecutive v21 corpus runs to it.

**What:** `/tmp` on this machine is a tmpfs with a per-user quota (6124 MB).
Every eval run that touches a dotnet fixture leaves `.NNN.so` files behind —
runtime images the .NET host writes and never removes. On 2026-08-25 there were
**1001 of them totalling 5.0 GB**, 835 older than a day, against a total of
655 MB for every eval workspace of every edition combined. The quota was full,
and nothing said so.

**How it presents, which is the expensive part.** Not as "disk full". Writes
fail with `OSError: [Errno 122] Disk quota exceeded`, so:

- `streamfmt.py` dies mid-scenario and `run.jsonl` stops growing, which the
  stall oracle correctly reads as a stalled agent — the runner then spends
  three attempts and four resumes per scenario producing nothing;
- every shell command that emits output fails while commands that emit none
  succeed, because the harness cannot write the result file;
- `df` reports the *filesystem* healthy (90 GB free on the btrfs root) and is
  therefore actively misleading — `quota -s` is the instrument, and reading the
  wrong one cost a thirteen-hour run and a retracted-then-reinstated diagnosis
  on 2026-08-24/25.

**Why it deserves a case when the workspace clutter did not.** 655 MB of old
workspaces is untidiness. This is a silent killer with a delay fuse: it does
not fail the run that creates the garbage, it fails some later run, and it
fails it in a way that looks exactly like model stalling. Every future edition
walks into it.

**Done when:** an eval run cannot be killed by this — either the runner checks
free quota before stage 2 and refuses with the real reason (the BL-050 shape),
or it cleans stale `.so` files it can prove are unowned, or the dotnet fixtures
stop producing them. Whichever is chosen, the failure mode must name the quota
rather than present as a stalled agent.

**Half shipped 2026-08-25 — detection, not prevention.** Stage 1 now allocates
a 512 MB probe file under the workspace before any agent runs and refuses the
run when it cannot, printing the real reason and the cleanup command. It
**writes rather than queries** on purpose: `df` reports the filesystem and
`quota -s` reports one quota system, while what actually fails is a write — so
the probe performs the failing operation instead of predicting it. That turns
a three-hour masquerade as model stalls into a one-second refusal.

**Still open:** nothing stops the leak, and a run that starts with 512 MB can
still exhaust the quota mid-corpus. The remaining half is prevention — clean
provably-unowned images, or stop the fixtures producing them.

## BL-060 — The eval suite's false green, and pruning what measures nothing

**Status: ANALYSIS DONE 2026-08-25** — exploration; findings recorded in
`evals/POLICY.md` §§1b and 1c, full record in
`docs/cases/BL-060-eval-false-green-audit/`. Four cases sized, none filed.

**Measured, not argued.** Blanking a graded scenario's report and re-grading:
the `audit` scenario **still scores 14/44 — 32% — with no report at all**, of
which only 3 are legitimate. `restructure` survives at 79% and that is honest,
because its evidence is the repository tree; its 8 report-reading asserts went
red correctly. The asymmetry is the finding: audit is a zero-writes mode, so
the report is its entire output.

Two mechanisms. Nine `does NOT contain` asserts are vacuously true against an
absent artifact — v17's `ghost_import_fixed` fixed as one assert and never as a
class, so the class regrew. And `step7_report_saved` tests `path.exists()`
where substance is meant, so a zero-byte file passes.

**Run history cannot find a useless assert.** 199 of 200 asserts were green in
every surviving run — a statistic with no information, because a healthy corpus
is green by design and a perfect assert is indistinguishable from a dead one.
Any pruning method built on "never fails" returns the whole suite. This is why
§3's red-before-green rule, though right, is not sufficient: it binds only the
moment an assert is authored.

**Where defects come from, over 36 recorded across v17–v21:** law 13 (nine of
them scope-or-completion, not error), harness 12 (over half false green),
grader 8 (two green and empty in every version), model 3 (none survived
scrutiny). The cross-cutting theme is silent non-measurement.

**Designed:** an `unmeasured` third verdict that is fatal to its scenario; a
scenario reporting measured-and-passed as two numbers; a mutation manifest
where every assert carries a corruption that must turn it red, run against
recorded artifacts at no token cost; and mechanical pruning — no mutation, or a
mutation that leaves it green, or a mutation identical to another's, means
delete rather than weaken.

**Deliberately not done:** no assert is named for deletion. The mutation pass
must run first; naming candidates by inspection would repeat exactly the
judgement-call failure the mechanical criteria exist to prevent.

**Filed since:** D1+D2 as **BL-062** (done 2026-08-25). D3, D4 and the
substance half of D5 remain unfiled, in that order.

## BL-061 — `fleet.sh`'s FAIL branch trusts the exit code and never checks the version

**Status: PROPOSED 2026-08-25** — tooling only (`tools/fleet.sh`), no `skill/`
change, no VERSION, no benchmark. Found by the v21 sweep the same day.

**What:** the upgrade loop is

```
if run_upgrade_agent …; then   <re-read the manifest; ok or WARN-still-behind>
else                           FAIL
fi
```

The success branch does **not** trust the exit code — it re-reads the manifest,
because a runner can exit 0 having achieved nothing (that is BL-053's `WARN`).
The failure branch has no such scepticism: a non-zero exit is reported `FAIL`
without ever asking whether the repository advanced.

**Measured, not hypothetical.** On the v21 sweep of 2026-08-25 the Claude
session limit was reached mid-run. `fleet-obs` was reported `FAIL — claude exited
non-zero`; its owned layer was afterwards verified **16/16 byte-identical to
v21**. The agent had finished the delivery and the CLI exited non-zero later,
plausibly while writing its Step 7 report. The work was complete and the tool
said it had failed.

**Why it matters more than the inverse.** A false `FAIL` is cheap to recover
from — delivery is idempotent, so re-running costs one agent run. But it
corrupts the exit code that BL-053 exists to make trustworthy: a sweep in which
every repository was in fact delivered can still exit non-zero, which trains an
operator to disbelieve exactly the signal that was just made honest. The
version re-check is the authority in the success branch; it must be the
authority in both.

**Done when:** the loop re-reads the manifest in **both** branches and decides
on the version, not on the exit code — a repository that reached the current
version is `ok` whatever the runner returned (noting the odd exit), and one
that did not is `FAIL` or `WARN`. The exit code becomes evidence, not verdict.

## BL-062 — `unmeasured` as a third verdict, and honest scenario arithmetic

**Status: DONE 2026-08-25** — tier 1, case `docs/cases/BL-062-unmeasured-verdict/`,
branch `bl/062-unmeasured-verdict`. Grader and runner only: no `skill/` change,
no VERSION bump, no benchmark. Implements BL-060's designs D1 and D2, which
`evals/POLICY.md` §1b had carried as law with nothing executing it.

**What it measured first.** BL-060 blanked two scenarios' reports; this case
blanked all seven that read one. `legacy-migration-agents-first` scored a
perfect **22/22 with a report containing nothing** — not partial credit, a full
green — because its only report-reading assert tested `path.exists()` and a
zero-byte file exists. `audit` reproduced BL-060's 14/44 exactly.

**What changed.** Every assert declares the artifact it reads, as a required
argument to `Grader.check` — so an assert naming no source cannot be written.
When that artifact is absent, empty or unparseable the assert is `unmeasured`:
not passed, not failed, and fatal to its scenario. One assert per artifact may
go red on its absence — the probe, whose subject *is* the artifact. Scenarios
print `<measured>/<total> measured, <passed> passed`, and the pass rate's
denominator is `measured`. `tools/evals-bg.sh` gained `grade_clean`, one
definition of green for all four stages that decide one; the dashboard shows
measured, marks unmeasured red, and counts persistently-unmeasured asserts
apart from flaky ones. Two silent skips were removed with it: an `if
has_report:` guard that dropped an assert from the corpus entirely, and
`case-practice`'s early return that dropped five.

**After.** The same blanked reports now read `audit` 5/44 measured, 4 passed,
and every scenario red. On unmutated v21 artifacts all ten scenarios reproduce
their recorded verdicts fully measured — verification cost no agent and no
tokens, which is the point of a grader-only case.

**Riders:** `evals/POLICY.md` §5 gained the grader-change baseline rule (take
the baseline from the last commit carrying the previous law with the *current*
grader, never from the previous tag, whose grader is a different instrument);
one fleet repository name that had leaked into BL-061's entry was replaced by
its alias.

**Left open, deliberately:** D3 (the mutation manifest) and D4 (pruning) — no
assert is named for deletion, because D1 proves an artifact was read, not that
a present artifact is measured meaningfully. The substance half of D5 stays
open too: "empty" here means whitespace-only, so a structurally junk report is
still `measured`.

## Note — master-agent / mini-agent routing system is a separate skill, not a Legislator feature

A master-agent that reviews an incoming request in a project and decides whether to route it to an existing project-local mini-agent (`.claude/agents/<name>.md`) or create a new fine-grained specialized one (task-appropriate model, scoped MCPs) is being built as its **own, separate skill** — not as part of Legislator. Rationale: Legislator is build-time scaffolding (runs occasionally, evolves via VERSION/manifest); request routing is a runtime concern with its own lifecycle. Folding both into one skill would blur SRP.

Legislator has **no involvement** here — no convention hook, no `.claude/agents/` scaffold, nothing. The separate skill owns its own convention entirely: it injects whatever `.claude/agents/` setup it needs directly into a repo when applied, independent of Legislator. (An earlier version of Legislator scaffolded a placeholder `.claude/agents/README.md` via `agents-README.md.tpl` — that has been removed; scaffolding it was itself scope creep into the other skill's responsibility.)
