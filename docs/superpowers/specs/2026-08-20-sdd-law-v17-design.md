# SDD law (constitution v17) — design spec

Date: 2026-08-20 · Case: BL-032 (absorbs BL-029's case home; riders:
BL-028 manifest key, BL-030 constitution sweep, BL-025 triage) ·
Feeders: deep-audit D1–D5 verdicts
(`2026-08-20-deep-audit-d0-d5.md`), consistency review P1–P6 (its
addendum). Edition split: this is **v17**; BL-033 (engine, OKF v2,
baseline) is v18 and explicitly not a dependency (fallback clause below).

## Why

The SDD process is an orphan: practiced fleet-wide, governed by no rule
(deep-audit fleet evidence — 0 acceptance lines in ~20 specs, 92 KB plan
monoliths, ceremony varying 0/0 to 12/48 per repo). Practice without law
drifts; the law makes existing fleet instincts (kbo's proto-EARS Testing
lines, Wave-1 parallel discipline) deliverable, auditable, harvestable.

## What — decisions shipped in this edition

1. **`core/sdd.md`** (new owned rule) — the SDD law, imperative bullets
   only (content discipline; how-to stays in the case README):
   - every work kind is a **case** (`BL-NNN`); one case one home
     `docs/cases/BL-NNN/` (spec, plan package, summary); register row
     links in; `docs/superpowers/**` is legacy (forward-only — history
     never moves)
   - spec **typology**: feature / bugfix / exploration, type in the
     header; bugfix carries current / expected / **unchanged**
   - **boundary** (in + out of scope) and **right-size** (one intent per
     spec; "and also" is the split signal)
   - requirements are **EARS lines** (WHEN/WHILE/WHERE/IF-THEN/ubiquitous
     + SHALL) with stable ids **R-NNN**; at least one named Gherkin
     hurting-case scenario per spec; the observability test applies to
     both lines and scenarios
   - **clarify protocol**: before approval, grill — max 5 questions, one
     at a time, recommended option first, answers written into
     `## Clarifications` (dated session, Q→A), contradicted text replaced
     not duplicated
   - **ceremony tiers**: 0 direct (no spec; converge still audits) /
     1 light (EARS spec + hurting case + grill) / 2 full (research →
     contracts → analyze → implement → converge) — chosen at case opening
     on blast radius × novelty, declared in the case header; converge may
     raise a tier; tier-0 is lawful, not a shortcut
   - **plan as a package** in the case file: research / data-model /
     contracts / quickstart domain-optional; split mandatory past ~10–15
     KB or 2+ domains; one task = one session; `[P]` file-disjointness
     markers; every task traces `per R-NNN`; ADR boundary — a decision
     that outlives the case goes to ADR, local ones to research.md
   - **analyze pre-gate**: before implementation judge reuse-first
     (checked existing before writing new) and over-engineering; run
     mechanical passes (coverage R↔task, dangling R-NNN, placeholders)
     **with the engine when available, otherwise as an explicit agent
     checklist** (the fallback clause decoupling v17 from v18)
   - **converge closing gate**: mandatory before a case closes — judge
     code against every promise (spec lines, plan decisions,
     constitutional MUSTs), never against git diffs; classify
     missing / partial / contradicts / unrequested; findings append as
     traceable tasks (`per <source-ref> (<gap-type>)`), append-only;
     loop to "✅ Converged"; constitutional violations are CRITICAL;
     includes the stale-doc axis (OKF docs anchored to touched
     files/symbols — still true?)
   - **cross-repo case**: the case file lives in the initiating repo;
     sibling repos get reference rows + the same `bl/NNN` branches;
     converge judges the whole tree
2. **`core/artifact-lifecycle.md`** (amendment — green condition, P3):
   conventional lifecycle homes gain `docs/cases/`; new role class
   **generated** — machine-written locally, do-not-edit, regenerated
   from source, dies with its source; neither hand-reference nor
   hand-lifecycle. (Populated with real artifacts in v18; the class
   enters law now so BL-033 does not need a law edit later.)
3. **`cases-README.md.tpl`** (new template) — the create-once
   `docs/cases/README.md`: what a case is, the tier line format, type
   header, links to the law. Project-owned after creation (never
   overwritten — the `.claude/rules/`-advice pattern materialized as a
   real create-once file).
4. **SKILL.md** (edits):
   - Step 4 table: `docs/cases/README.md` row (create-once); fresh
     scaffolds stop creating empty `docs/superpowers/{specs,plans}/`
     dirs (the home is cases; Step 5's relocation of stray legacy plans
     still targets `docs/superpowers/plans/` — history stays legacy)
   - harvest do-not-scan set (Step "Constitution candidates"): +
     `docs/cases/**` (case prose is lifecycle, not law source)
   - audit check 7 orphan exemptions: + `docs/cases/**`
   - audit check 12 stray-rulebooks scan set: exclude `docs/cases/**`
     (same reason as specs/plans)
   - **BL-028**: manifest key `profiles` → `stacks` — write `stacks`,
     read with fallback to legacy `profiles` (reconstruct + rewrite);
     the edge-case reconstruction path likewise; serialization order
     keeps position, only the key renames; sweep all prose incl. the
     compound "stack profile"
   - **BL-030**: "constitution file" → "AGENTS.md" sweep (the word
     constitution means only the rules corpus @ VERSION)
5. **`core/skills.md`** (one line, BL-025 item 5): a mapped skill that
   is not installed is check-14 territory — note it and proceed.
6. **VERSION 16 → 17.**

## Explicitly not in this edition

The G7 engine, baseline generation, OKF v2, the sync-debt audit check
(all v18 / BL-033); any spec template beyond the case README (the law's
format bullets are the template); changing existing fleet repos' history
(forward-only — fleet re-run delivers law + home; old specs stay).

## BL-025 triage (recorded for the cycle)

| Item | Verdict |
|---|---|
| 1 heading-pin evidence | **ride** — observation task in the v17 migration runs (clean prompt; record whether H2 drift recurs; pin moves into law only if it does) |
| 2 severity-anchored markers | **ride** — grade.py: Critical checks get severity-anchored assertions |
| 3 stage-affinity vs KEEP coherence | **exclude — stale**: v14's derivation rework removed the hardcoded `dotnet-refactoring` from the scaffolded map (now derived), and link-skills.sh documents its KEEP source (`~/.agents` canonical library); the fresh-machine gap no longer exists as described |
| 4 link-skills.sh hardening | **ride** — `mkdir -p "$DST"`, non-zero exit on MISSING-SOURCE drift in link mode (tools/, no VERSION impact) |
| 5 mapped-but-uninstalled clause | **ride** — the skills.md line above |
| 6 grader tightening | **ride** — restructure skill-binding check scoped to `For the team:` (grade.py, alongside item 2) |

(Correction: six items, not seven as BL-032's text said — the seventh
was BL-021 bookkeeping that already shipped.)

## Testing (the verification ladder for this cycle)

1. `check_static.py` green (new template referenced + present).
2. Full e2e per `evals/README.md`: setup_workspace / 5 scenarios / grade
   / idempotency ×3 / record in `benchmarks/v17.md` vs v16.
3. Grader updates riding the cycle: `SCAFFOLD_ARTIFACTS` + cases README
   + `stacks`-key assertions; upgrade fixture gains a hand-planted
   legacy `profiles` manifest (migration path graded); severity
   anchoring + `For the team:` scoping (BL-025.2/.6).
4. Benchmark observation recorded: BL-025.1 heading-pin result.

## Done when

v17 ships green (full pass rate, no idempotency regressions vs v16);
fresh scaffold creates the case home and writes `stacks`; upgrade
migrates a legacy `profiles` manifest losslessly; fleet re-run delivers
the law + home to all repos; BL-028/030/025-rides recorded done in the
backlog.
