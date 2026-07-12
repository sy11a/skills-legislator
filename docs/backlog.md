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
refactoring law absorbed from CareerPlatform's orphaned
`docs/superpowers/refactoring-checklist.md` into concern-named files —
async/cleanliness bullets in `stacks/dotnet/coding-standards.md`, DI bullets
in `stacks/dotnet/architecture.md`, new `stacks/dotnet/data-access.md`
(**`assets/rules/**` change: VERSION 8→9**). (b) Stray-rulebook feature:
audit check 12 (`stray-rulebooks`, Warning) flags law-shaped rule/checklist
docs no session loads; harvest scans them; restructure merges their law
into `.claude/rules/<topic>.md` and removes the file. BL-015 rides along
(all four items).

**Why:** a rulebook parked in an unorthodox folder is law no agent ever
sees — CareerPlatform's checklist was invisible to CLAUDE.md, harvest, and
every audit check. After this cycle "refactor X" in a legislated repo hits
v9 law (imported), project law (`.claude/rules/`, auto-loaded), and the
generic dotnet-refactoring skill — nothing orphaned. User-settled
2026-07-10 (concern-named law home + full loop incl. CareerPlatform
validation).

**Done when:** benchmark `evals/benchmarks/v9.md` green — audit names the
planted `docs/superpowers/review-checklist.md` under the `stray-rulebooks`
slug and proposes its generic line as a candidate (project line + check-11
contradiction line absent from candidates); restructure merges it into
`.claude/rules/` and removes it; fresh/upgrade carry `data-access.md`;
idempotency zero-diff; then CareerPlatform live validation (upgrade +
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
   services holding scoped/DbContext references" from the CareerPlatform
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
downstream repo to deliver v10.** Companion CareerPlatform
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
architecture.md). Companion project law: CareerPlatform
`.claude/rules/branching.md` (concrete mechanics — stacking commands,
2-day cap, chore batching).

**Why:** at AI-assisted pace, tasks outrun merges: CareerPlatform hit 30+
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
(phase 2 backfill covers CareerPlatform + RKruiterApi + RKruiterAgent).

**What:** three levers to stop OKF glossaries dying empty (spec:
`docs/superpowers/specs/2026-07-12-glossary-vitality-design.md`).
(1) `{{GLOSSARY_TABLE}}` derived at legislation (5–15 confirmed terms from
the repo's own domain) instead of an intentionally empty table; (2)
`core/okf.md`'s mandatory completion checklist gains a glossary item
(**`assets/rules/**` change: VERSION 10→11**); (3) audit check 13
(`glossary-vitality`, Warning) flags a zero-row glossary in a repo with
source code, healable by restructure via the Step 4 derivation rules
(token-fill precedent). BL-019 rides along (all five items).

**Why:** review of the fleet (2026-07-12) found CareerPlatform and
RKruiterApi glossaries byte-empty since legislation while RKruiterAgent's
thrives — terms "emerge" only in design sessions; code-resident terms
never do. The glossary had no law behind it: not on the completion
checklist, not audited, seeded empty by design.

**Done when:** benchmark `evals/benchmarks/v11.md` green — fresh and
migration runs seed ≥1 real term; audit names the empty planted glossary
under the `glossary-vitality` slug; restructure heals it approval-gated;
idempotency zero-diff ×3; then live backfill lands real domain terms in
CareerPlatform and RKruiterApi on feature branches.

## BL-021 — Glossary-vitality follow-ups from the v11 final review (skill-file items)

Ride along with the next benchmarked `skill/**` cycle:

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

## Note — OKF content-accuracy check is an open idea, not yet a backlog item

The RKruiterApi v11 backfill (2026-07-12) found six `docs/okf/domain/*.md`
files still describing a domain model an ADR had removed — no audit check
covers OKF *content accuracy against source* (check 5 covers links, check 6
the map's shape). A deterministic version is hard (needs source-symbol
grounding, e.g. "every type name an OKF doc cites in backticks exists in
src/"); promote to a BL item when the shape is clear.

## Note — master-agent / mini-agent routing system is a separate skill, not a Legislator feature

A master-agent that reviews an incoming request in a project and decides whether to route it to an existing project-local mini-agent (`.claude/agents/<name>.md`) or create a new fine-grained specialized one (task-appropriate model, scoped MCPs) is being built as its **own, separate skill** — not as part of Legislator. Rationale: Legislator is build-time scaffolding (runs occasionally, evolves via VERSION/manifest); request routing is a runtime concern with its own lifecycle. Folding both into one skill would blur SRP.

Legislator has **no involvement** here — no convention hook, no `.claude/agents/` scaffold, nothing. The separate skill owns its own convention entirely: it injects whatever `.claude/agents/` setup it needs directly into a repo when applied, independent of Legislator. (An earlier version of Legislator scaffolded a placeholder `.claude/agents/README.md` via `agents-README.md.tpl` — that has been removed; scaffolding it was itself scope creep into the other skill's responsibility.)
