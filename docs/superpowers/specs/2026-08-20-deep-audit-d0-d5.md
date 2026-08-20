# Deep audit D0–D5 — stage × instrument matrix, second pass

> Captured 2026-08-20. Companion to `2026-08-19-sdd-landscape-research.md`
> (the first pass: five frameworks, G1–G9). This pass deepened each matrix
> stage against primary sources, collected fleet evidence from all nine
> legislated repos, and produced per-stage verdicts. Execution decisions:
> BL-026 revised, BL-032/BL-033 added.

## Method

Six sessions (D0–D5). Each: field mechanics from primary sources → fleet
evidence (artifacts, not declarations) → verdicts (adopt / adapt / reject,
the protocol already fixed in BL-026). One grill decision at a time; the
user arbitrates.

## Fleet evidence (the input for every session)

Collected read-only across all 9 legislated repos on 2026-08-20:

- **Acceptance instrument absent**: zero Acceptance sections in any spec
  fleet-wide; the only four `**Acceptance:**` lines live in one
  RKruiterApi plan, freeform. kbo's spec `## Testing` sections are a
  spontaneous prototype of scenario form (named test class + behavior +
  idempotency) — the fleet reached for the shape without having the syntax.
- **Monolithic plans**: CareerPlatform 48 plans / 1.8 MB total, top file
  92 KB — an order of magnitude beyond every other repo.
- **SDD adoption is uneven**: 12 specs / 48 plans (CareerPlatform) vs 0/0
  (RKruiterSecurity works straight off its backlog) — tiering exists in
  practice, but is intuitive, not law.
- **Hygiene**: `.superpowers/sdd/` debris in 3 repos (4.4 MB in
  CareerPlatform); an archived repo still carries its manifest and keeps
  being picked up by fleet scans; stale OKF/journal on ~4 repos;
  RKruiterApi's six domain docs described a model an ADR had removed.
- **The SDD process is an orphan**: specs/plans follow the
  `docs/superpowers/` convention, but no constitution rule governs spec
  format; the spec-workflow skill is dead (404 in the first-pass research);
  the grill practice is a personal skill of the user. The process lives in
  muscle memory — hence the variance.

## D0 — field expansion: Agent OS (and the Vortex non-finding)

Primary sources: buildermethods/agent-os repo (README, profiles/default
workflows, agents, commands).

**Adopted as feed:** spec-QA axes (verify-spec agent: task→requirement
traceability, reuse-first "don't write new until you checked existing",
over-engineering check, vagueness flags) → fed D4; the live-checklist
pattern (verifier ticks roadmap `[x]` at delivery — baseline updates at
the moment of work completion, not by a separate ceremony) → fed D2.

**Rejected:** the product layer (mission/roadmap/effort scale — ordering
work is the backlog's job; mission is the user; escape hatch: a single
OKF document if a repo ever needs a recorded mission); per-task-group
standards compilation via orchestration.yml (our law is ambient through
imports; conditional loading solves a stranger's problem);
improve-skills (covered by steward); installer/profile mechanics
(validates our scaffold, nothing to take).

**Vortex (Vercel): unresolved.** Four independent probes (github.com/vercel
and vercel-labs 404, blog 404, two search engines empty) could not confirm
it exists. Excluded from the matrix per the epistemic rules; recorded here
so nobody re-chases it without a lead.

## D1 — intent capture · clarification · requirements syntax

**Intent capture — adopt:** boundary discipline (every spec states
in-scope AND out-of-scope; the boundary is half the assignment);
right-size signals (one intent per spec; "and also" is the split signal);
Kiro's bugfix structure (current / expected / **unchanged** — regression
protection in the recording form itself).

**Typology — adopt:** three declared spec types — feature / bugfix /
exploration — one home (`docs/superpowers/specs/`), type declared in the
document header. Exploration legalizes existing practice (memos like the
old "Track C"). Quick is a ceremony tier, not a type (→ D4).

**Clarification — adapt (SpecKit clarify, from the full primary source):**
keep the question budget (max 5 per session, strictly one at a time, early
stop), the recommended-option-first format, **writing answers into the
spec** (`## Clarifications` → dated session → Q→A lines; replace, never
duplicate, contradicted text), and post-answer validation. Shrink the
12-category taxonomy to 5–6 solo-fleet-relevant categories (scope/edges,
data model, non-functional, integration, terminology). The user's grill
practice already runs this protocol — D1's discovery is that it is written
down nowhere.

**Requirements syntax — adapt (merged EARS + Gherkin + the kbo
prototype):** every requirement is one EARS line (WHEN/WHILE/WHERE/
IF-THEN/ubiquitous — the five patterns cover the fleet's whole zoo:
always-true, reactivity, states, errors, flags/profiles). Each spec
carries at least one named Gherkin scenario for its "hurting case" (the
one case it would hurt most to see broken); observability is the quality
test for both floors ("could a tester who never saw the code tell?").
Rejected: the RFC 2119 strength table (SHALL-only; a solo fleet does not
grade obligatoriness), scenario-per-requirement mass (bloat for us).

**The home decision (closing D1):** the SDD process gets a constitutional
home — new owned rule `core/sdd.md` + a spec template in assets. This is
the only variant that fixes the orphan finding: delivered byte-for-byte
fleet-wide, auditable, harvestable. → BL-032.

## D2 — verification/convergence · baseline maintenance

**Converge — adopt as a mandatory case-cycle gate** (SpecKit converge,
from the full primary source): intent inventory (spec requirements, plan
decisions, constitutional MUSTs) → code judged against every promise, not
against git diffs → gap taxonomy missing / partial / contradicts /
**unrequested** (agents gift; 4.4 MB of CareerPlatform debris shows
nobody inventories gifts today) → findings appended as traceable tasks to
the case's plan (`per <source-ref> (<gap-type>)`), append-only, never
rewriting → loop implement→converge until "✅ Converged". Constitutional
violations are CRITICAL findings — the constitution gains an execution
consumer in field repos, not just delivery.

**Baseline (G1) — architecture D, not A.** The axis that matters: where
"what the system must do today" lives. Hand-maintained documents (OpenSpec
tree A, OKF sections B) always rot — RKruiterApi proved it; our own
rot-free artifacts (law stratum, manifest) are machine-written. Therefore:
EARS lines carry stable ids (**R-NNN**) in specs → tests are annotated
with the same ids → a generator writes `baseline.md` (do-not-edit, like
the manifest). Rot is impossible by construction: a deleted test is a
deleted line, visible in the diff. Converge checks both directions
(spec line without test = `missing-test`; test without spec line =
`unrequested`). Non-testable norms (SLA, compliance) live in ADRs and a
separately-marked baseline section. Variant A (the OpenSpec living tree)
is conserved for Vector A / second hands, where human-readable contracts
must survive without code.

**OKF hygiene — two cleaners:** converge gains a `stale-doc` axis (OKF
docs anchored to files/symbols the case touched — still true?) for
in-cycle changes; a new audit check "OKF-sync debt" (commits touching
concept files after a doc's timestamp, with no linked OKF update) catches
out-of-cycle drift — repair through restructure, never automatic. This
legalizes the open "OKF content-accuracy" backlog note into a designed
mechanism.

**OKF v2 — decomposition by link hardness.** Generated (baseline from
tests; codebase-map from code; index from docs) / anchored (concept docs
stay handwritten, but every backticked symbol/path is verified against
src/ — source-symbol grounding) / human (glossary, log — deliberately).
Four of six OKF functions become hard-linked; the bootstrap layer for
incoming sessions stays (the alternative — doc-comments in code — was
considered and rejected: it would kill context economy and concept
synthesis). **Pilot for the whole package: kbo** (smallest active repo,
already home of the proto-EARS Testing lines). → BL-033.

## D3 — design · task breakdown

**Design — adopt (riding the case file, BL-029):** the plan is a package,
not a monolith: research.md (Decision/Rationale/Alternatives — resolves
every NEEDS-CLARIFICATION before design) / data-model / contracts /
quickstart (a runnable hand-proof of the feature — the manual pair to
converge) — domain-optional. Split threshold: past ~10–15 KB or 2+
domains, splitting is mandatory. The ADR boundary rule: a decision that
outlives the case → ADR; local to it → research.md; plans leak neither.
Rejected: SpecKit's pre/post constitution gate as a separate ceremony
(ours is ambient; converge already took the CRITICAL source role).

**Task breakdown — adopt:** `[P]` markers declaring file-disjointness
(makes the existing Wave-1 practice machine-readable: parallel dispatch
is safe when file-disjoint and design-settled); "one task = one session"
granularity rule (the anti-monolith anchor); task traceability `per
R-NNN` (closes the spec↔task↔converge triangle; SpecKit traces to user
stories, we trace to EARS ids). Rejected: automatic dependency waves (we
are not an IDE — dispatch is a human decision [P] informs); MVP-scope
markers (right-sizing D1 handles whole cases).

## D4 — cross-artifact analysis · process sizing

**Analyze — adopt as the paired gate:** analyze **before** implementation,
converge after — symmetric read-only judgments, remediation by proposal
only (decision-gate style). Seven passes: five mechanical (coverage
R↔task, dangling refs, vagueness, duplicates, terminology) + two judgment
axes from Agent OS (reuse-first, over-engineering — they audit the quality
of intent, not just format). The mechanical passes run in the same engine
as G7 validation and OKF-anchor checks and baseline generation — **one
engine, three jobs**. After D1–D3 the analysis is largely mechanical:
the id web (R-NNN everywhere) replaces SpecKit's semantic keyword
inference. Rejected: hooks, the 50-findings cap.

**Process sizing (G9) — adopt as law, three tiers** chosen at case
opening on two axes (blast radius × novelty), declared in the case
header; converge may raise a tier (found missing behavior → the work was
bigger than the tier):

| Tier | Ceremony | When |
|---|---|---|
| 0 — direct | no spec: backlog line → branch → journal; converge still audits unrequested | local, reversible, no concept touched |
| 1 — light | spec in case file: EARS lines + hurting case + grill ≤5 | externally observable, familiar domain |
| 2 — full | research → contracts → analyze gate → implement → converge | new domain, cross-repo, security, irreversible |

The fleet already lives in tiers without the law (RKruiterSecurity is a
permanent tier-0; CareerPlatform over-ceremonies). Tier-0-by-law is what
makes the debris problem and the 92-KB problem both solvable.

## D5 — the strong rows, checked against evidence

- **Knowledge layer** — confirmed leading after D2's OKF v2
  modernization; conditional loading stays rejected (D0). No new harvest.
- **Multi-repo — adopt one convention (cross-repo case):** a case file
  lives in the initiating repo; sibling repos get reference rows in their
  backlogs and the same `bl/NNN` branches; converge judges the whole case
  tree. Today's practice ("Task split across repos" heading in a
  CareerPlatform spec) becomes a convention, not an accident.
- **Learning loop** — architecture confirmed (journal + harvest +
  steward; harvest has actually run; kbo telemetry accrues), but "leading"
  is conditional: **no steward cycle has ever been executed**. The first
  one is scheduled after the D-harvest lands — its natural subject is the
  fresh laws themselves.

## Primary sources read this pass

Agent OS: profiles/default/{workflows/planning/create-product-roadmap,
workflows/implementation/compile-implementation-standards,
workflows/specification/verify-spec, agents/implementation-verifier,
commands/improve-skills/improve-skills}.md, README. SpecKit:
templates/commands/{clarify, converge, plan, analyze}.md. OpenSpec:
docs/{concepts, writing-specs}.md. Kiro: docs index + specs overview.
Fleet: read-only inventory of 9 repos (structure, sizes, dates, quotes).

## Execution consequences

- BL-026 revised: every gap G1–G9 now carries its concrete decision and
  a pointer here.
- BL-032 (core/sdd.md — the SDD law) and BL-033 (OKF v2 + the G7 engine,
  kbo pilot) added as behavioral cases.
- Order: kbo gold panel → BL-032 (+ BL-028/030 riding) → BL-033 (kbo
  pilot) → BL-029/031 → the first steward cycle.
