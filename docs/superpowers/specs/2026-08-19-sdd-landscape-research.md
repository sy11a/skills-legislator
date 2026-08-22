# SDD landscape research — legislator vs the field (BL-026/BL-027 feeder)

**Status:** research record, approved for capture 2026-08-19 (grill session:
corpus selection, engagement end-line = stop-at-proposals, two-vector strategy,
sidecar placement for the enterprise case). Reference artifact — do not rewrite.

## Purpose

The legislator's spec/plan workflow was built intuitively, from lessons
learned in practice. It works (fleet evidence below), but with nothing to
compare against, non-optimal solutions and gaps are invisible. This document
diffs the legislator's process against established Spec-Driven Development
(SDD) practice — reading the tools' own repos and real artifacts, not their
marketing — to surface gaps worth closing and techniques worth harvesting.
Nothing here is implemented; BL-026 and BL-027 in the backlog carry the
execution decisions.

## Fleet baseline (verified 2026-08-19, 8 legislated repos)

The current loop **is** SDD already: brainstorm → `docs/superpowers/specs/
*-design.md` (freeform prose+tables, Status line, ADR links, "Rejected
alternative" section, execution phases → BL numbers) → `docs/superpowers/
plans/*` (phased checkboxes, per-phase `Acceptance:`, "phase = BL = branch =
PR") → backlog BL-NNN → journal/ADR/OKF. Usage is heavy, not ceremonial:
fleet-platform 12 specs / 48 plans (May–Jul 2026), fleet-api 3/2 with
dense bidirectional traceability (spec § ↔ BL ↔ ADR ↔ journal, including
cross-repo), 105 ADRs fleet-wide, backlogs actively cleared on completion.
Zero foreign SDD tooling in any repo. The only SDD-ish working-dir pattern
in active use: `.superpowers/sdd/task-N-{brief,report}.md` + superpowers
skills (brainstorming ×30, writing-plans ×20, executing-plans ×8 in fleet-obs's
event log).

## Sources (what was actually read)

**Deep — the two dogfooding repos:**
- **OpenSpec** (Fission-AI, 65.5k★): `openspec/specs/openspec-conventions/
  spec.md` (the full delta/archive convention law), a real in-flight change
  (`fix-spec-parser-fidelity/` proposal.md + tasks.md — their house spec
  style is strikingly close to ours), repo structure incl. `changes/
  IMPLEMENTATION_ORDER.md`, `initiatives/` (decisions.md/questions.md/
  work-items), `work/` model, ~80 archived changes.
- **SpecKit** (github, 130k★): `templates/commands/{specify,clarify,plan,
  tasks,implement,converge,analyze}.md` — the actual command prompts, i.e.
  the real methodology. Notably richer than the README.

**Light survey:**
- **Kiro** (AWS): specs docs (3-phase workflow, wave-parallel task
  execution, Analyze Requirements) + steering docs (inclusion modes:
  always / fileMatch / manual / auto; global-vs-workspace scope).
- **BMAD-METHOD** (52k★): README + delivery loop — right-sized process,
  Clarify→Plan→Build→Learn loop, agent personas, durable context.
- **EARS** (Wikipedia + Kiro usage): the five requirement patterns.
- **spec-workflow** (MCP): repo 404'd at survey time; its one
  distinguishing gene (per-artifact approval gates) is already covered by
  the legislator's decision-gate rule. Nothing lost.

## The five philosophies

| Gene | Holder | Core idea |
|---|---|---|
| **Living baseline** | OpenSpec | `specs/` = what IS true; `changes/` = deltas in flight; archive merges deltas back. Law: "Do not rewrite specs for future intent until behavior changes with an implementation slice." |
| **Pipeline + reconciliation** | SpecKit | constitution → specify → clarify → plan → tasks → implement → **analyze** (pre-implementation consistency) / **converge** (post-implementation reconciliation) — a gated pipeline with two consistency checkpoints |
| **Syntax discipline** | EARS (→ Kiro) | 5 constrained patterns (`WHEN/WHILE/WHERE/IF-THEN/ubiquitous` + `SHALL`); testable by construction; zero tooling needed |
| **Context economy** | Kiro steering, BMAD | knowledge loads conditionally (fileMatch/auto inclusion; plan compression); process sized to the work (Quick Spec; "small changes go straight to build") |
| **Persona-driven** | BMAD | PM/architect/dev/QA agent personas; delivery loop with explicit **Learn** phase |

## Stage × instrument matrix

(● = the legislator today)

| Stage | OpenSpec | SpecKit | Kiro | BMAD | **Legislator** |
|---|---|---|---|---|---|
| Intent capture | proposal.md (Why/What/Impact + explicit **From/To/Reason** per change) | specify (WHAT/WHY only; informed guesses + documented assumptions; max 3 NEEDS-CLARIFICATION markers) | NL prompt | Clarify phase | ● brainstorm → design spec (freeform, rich) |
| Clarification | /opsx:explore | **clarify: 12-category ambiguity taxonomy, max 5 questions, Impact×Uncertainty prioritization, recommended-answer-first, answers written back into the spec incrementally** | upfront questions | — | ● grill-me (ad hoc) |
| Requirements syntax | `### Requirement:` / `#### Scenario:` headers = machine-matchable IDs; ADDED/MODIFIED/REMOVED/RENAMED delta sections | FR-###/SC-### stable keys | **EARS** acceptance criteria | — | ✗ freeform |
| Design | design.md optional | plan phase → **research.md (Decision/Rationale/Alternatives) + data-model.md + contracts/ + quickstart.md**; constitution check pre/post design | design.md + sequence diagrams | Plan phase (+compression) | ● same doc as spec; ADRs |
| Task breakdown | tasks.md + issue links | T### + **[P] parallelization markers** + [US] story mapping + phases + MVP scope | tasks + **dependency waves** | — | ● plan phases + BL numbers |
| Execution control | changes/ in-flight; IMPLEMENTATION_ORDER.md cross-change ordering | checklist gate table (STOP on FAIL before implement); mark [X] | wave-parallel runner | Build & verify | ● phase=BL=branch=PR |
| Verification / convergence | verify skill | **converge: gap taxonomy missing/partial/contradicts/unrequested; append-only remediation phase; source-ref traceability per appended task; byte-unchanged when converged** | Correctness (property-based testing) | verify | ● verification ladder (tests, not spec-vs-code) |
| Baseline maintenance | **archive merge algorithm** (rename→remove→modify→add, header-based matching, conflict detection) | ✗ | ✗ | — | ✗ (specs frozen by design; OKF = concepts as-built) |
| Cross-artifact analysis | validate (format + delta-vs-spec conflicts) | **analyze: coverage matrix req↔task with %, terminology drift, duplication, constitution alignment; strictly read-only** | Analyze Requirements | — | ✗ |
| Knowledge layer | project.md | constitution.md (loaded everywhere, non-negotiable, violations = CRITICAL) | **steering: always/fileMatch/manual/auto inclusion modes** | durable briefs | ●● OKF + constitution imports (strongest of the field) |
| Multi-repo | **Stores (beta): one planning repo shared across code repos; read-only consumption** | ✗ | ✗ | ✗ | manual cross-refs |
| Process sizing | **Progressive Rigor** (law: rigor scales with risk/coordination complexity) | rigid pipeline | **Quick Spec** (no gates) | right-sized | implicit |
| Learning loop | archive history | converge iterations (fewer findings each pass) | — | **Learn & adjust** phase | ● journal + harvest + steward duties |

## Gaps (legislator vs the field)

| # | Gap | Harvest source | Cost if adopted |
|---|---|---|---|
| **G1** | No living behavioral baseline — OKF documents concepts *as-built*; specs are frozen point-in-time design records. Nothing answers "what is the system *required* to do today?" | OpenSpec delta+archive model | High: new artifact class, artifact-lifecycle amendment, fleet-obs registry touch. Pilot in exactly one repo. |
| **G2** | No acceptance-criteria syntax — `Acceptance:` lines in plans are freeform prose | EARS-lite (WHEN…SHALL) on *acceptance lines only* | Low: template + rule line |
| **G3** | Clarification is ad hoc — grill exists but no taxonomy, no cap, no write-answers-into-spec discipline | SpecKit clarify | Low: codify into spec-writing guidance |
| **G4** | No convergence step — nothing checks code vs spec after implementation | SpecKit converge (gap taxonomy + append-only) | Med: new procedure/rule |
| **G5** | No cross-artifact coverage check — BL ↔ plan phase ↔ spec § ↔ OKF doc consistency never verified | SpecKit analyze coverage matrix | Med |
| **G6** | Monolithic plans (94 KB fleet-platform example) — agent context economy suffers | SpecKit artifact split (research/data-model/contracts), BMAD compression | Med |
| **G7** | No machine validation of specs/plans (format, dangling refs) | OpenSpec validate, SpecKit checklists ("unit tests for English") | Med: a check_static-style linter |
| **G8** | No parallelism markers in plans | SpecKit [P] markers, Kiro waves | Low |
| **G9** | Process sizing is intuitive, not law | OpenSpec Progressive Rigor / Kiro Quick Spec | Low: one rule |

**Recommendation (from the grill session):** pilot **G2–G4 first** (cheapest
high-value; slot into existing artifacts with zero new structures), **G1 in
exactly one repo** (the only structural gap; OpenSpec's genuine
differentiator; collides hardest with the artifact-lifecycle law — new
artifact class, role declaration, fleet-obs registry). G5/G7 batch later as "spec
tooling"; G6 partially solves itself if G2/G4 land; G8/G9 minor.

**Where the legislator already leads the field:** constitution-as-code with
fleet delivery (VERSION + manifest + fleet.sh — no surveyed tool has this),
OKF as a knowledge layer, fleet-obs practice observability, ADR/journal discipline,
the harvest upward loop, steward duties with model-release benchmarking.

## Application vectors (strategy, settled 2026-08-19)

Two usage vectors exist for the legislator. They are opposite points on a
**control gradient**; nearly every design decision falls out of four axes.

| Axis | **Vector A — Enterprise adapter** | **Vector B — Solo fleet** |
|---|---|---|
| Context | Real company (anonymized here), very large fast-mutating codebase, big team, others don't use the legislator; documented DB + MCPs to Jira and Confluence; company developer tips in a knowledge base | Individual developer, multiple local projects (connected or not), all synced to latest legislator, most legislated from birth |
| Where the layer lives | **External sidecar repo** — the target repo cannot be committed to (verified) | In-repo, always |
| What OKF means | **Personal context store**: own ADRs, tips, dead ends — things the team cannot invalidate; a codebase mirror would rot in days | System knowledge mirror — viable, rituals maintain it |
| Where outputs go | **Into the enterprise systems of record**: Jira = backlog, Confluence = reference (redirection law inverted outbound) | Constitutional homes in-repo (current `core/skills.md` law) |
| Trust model for code knowledge | **Probe-first**: read code at task start, trust no doc older than the task; document only **blast radius** (interfaces the feature touches) | Docs trustable; specs/plans/OKF authoritative |

### Vector A mechanics (settled in grill)

- **User runs own Claude Code on a local clone**; untracked files in the
  working tree are tolerable; commits are not.
- **Stub + sidecar**: a thin untracked `CLAUDE.md`/`AGENTS.md` stub in the
  target clone, excluded via `.git/info/exclude` (local-only, never leaves
  the machine), `@import`-ing the constitution from the sidecar repo.
  This is the only mechanism that preserves ambient constitutional loading
  (the thing the whole fleet is built on). `git clean -fd` wipes the stub;
  a legislator re-run restores it. Alternatives rejected: launcher/persona
  (easy to bypass, scoping risk) and MCP context server (kills ambient law).
  Precedent: OpenSpec Stores (planning in a repo of its own, consumed by
  agents in code repos — same shape, validated at scale).
- **Probe-first doctrine**: company KB is a probe target, not a mirror.
  Sidecar OKF holds only what the operator alone observes/decides (dead
  ends, blast-radius contracts, personal ADRs); everything the company
  owns gets queried live via its MCPs.
- **Redirection inverted outbound**: backlog → Jira, durable reference →
  Confluence (via the documented MCPs — read-only DB, no GitHub/no
  ticketing-automation MCPs per the BL-008 scope ruling); in-repo
  artifacts become lifecycle working copies that die at feature merge.
- **Progressive-rigor floor** (OpenSpec's law): "small feature in a big
  organism" = lowest ceremony tier by law, not by judgment.

### One core, two placement modes — not a fork

**Decision: stay one core.** Law content overlap is ~90% (decision gate,
verification ladder, pair development, ADR, artifact lifecycle — all
vector-blind), and harvest/evals/steward only stay shared in a single
lineage. A fork means harvest candidates from A can't flow up to B, the
eval suite doubles, every steward review happens twice — permanent rent on
two constitutions.

The divergent laws are **profile-shaped** and the mechanism already exists
(`manifest.json → profiles → assets/rules/stacks/<name>/`): an
`enterprise-solo` profile (redirection inversion, probe-first,
blast-radius OKF, progressive-rigor defaults) plus a placement mode in the
legislator itself (sidecar hosts owned files + manifest; stub in target
clone). The placement mode is the biggest structural change the skill has
faced — it inverts Step 3's "owned files land in the target repo"
assumption — and earns its own cycle: **BL-027**.

**Unifying law worth adopting regardless of vector** (from OpenSpec):
*ceremony scales with control and artifact lifetime* — the constitutional
sentence that makes B-maximal and A-minimal the same law, not two laws.

## fleet-obs measurement recipe (build BEFORE any enrichment rollout)

Verified 2026-08-19: fleet-obs is well-positioned to measure an SDD process
change. Spec/plan reads/writes and SDD-skill invocations already flow into
bronze with session/repo/path/timestamp fidelity (the registry glob
`~/Repository/*/docs` tags them; `.superpowers/sdd/` events captured with
full paths). The 60-day windows give a before/after baseline; the
service-session split (ADR-0039 pattern: mark at launch) keeps rollout
noise out. All gaps are gold-side and additive (ADR-0002 schema law):

1. **Spec-before-code ordering** — within a session: spec/plan-path read
   events timestamped before first code-write event; per repo × week.
   (The write→read loop in `DashboardComputer` already does time-ordered
   per-subject correlation — same pattern.)
2. **Writes-by-content-type** — docs-written vs code-written per
   session/repo/week (`ContentKind.Of()` is pure; reads-by-content-type
   already exists per ADR-0025 — mirror it for writes).
3. **SDD-skill rate** — `skill.invoked` share for spec/plan-writing skills
   per session/repo/week (harvest-sourced; note harvest-lag).

Honest limitation: fleet-obs has no session-outcome metric — process *adoption*
is measurable, "specs made code better" is not. Proxies: failed-search
rate, KB-touch, write→read loop, tokens. No commit-level tracking exists
(observable equivalent: `knowledge.written` events on spec paths — sees
writes before commit, arguably better).
