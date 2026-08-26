---
type: System
title: Legislator — Domain Glossary
description: Domain terms mapped to their meaning in this codebase.
tags: [system, glossary, domain]
timestamp: 2026-08-24T00:00:00Z
status: implemented
---

# Domain Glossary

Map internal jargon to what it means in this codebase, so any session edits the right files. Add terms as they emerge; keep meanings current (the okf.md sync rule applies).

Canonical term list. Status values: **industry** — established SDD/software-field
term used in its field sense · **home** — legislator's own established term ·
**coin** — home coinage minted where the field is silent · **legacy** —
deprecated alias, kept only where it physically lives. Naming rules and the
entry path for new terms: `docs/ontology.md` §3; the narrative these terms
serve: `docs/philosophy.md`.

| Term | Definition | Status | Lives |
|---|---|---|---|
| AGENTS.md | The repo's entry document: first thing a session reads; imports the constitution, carries project-instance data. The filename is the term — no second name. | industry | repo root |
| ADR | Architecture Decision Record: one decision, one immutable document (context / decision / status / consequences); superseded, never edited. | industry | `docs/adr/` |
| analyze | The paired pre-implementation gate (converge's mirror): judges a case's artifacts before code — coverage R↔task, dangling refs, vagueness, duplicates, terminology (mechanical, engine-provided when available) + reuse-first and over-engineering (judgment). Read-only; remediation by proposal only. | home (kin to SpecKit analyze) | `core/sdd.md` (BL-032) |
| anchored | A knowledge document bonded to code by its own text: every backticked path and PascalCase symbol resolves in the repository. Checked by `docs/ai/engine.py anchors`; `glossary.md` and `log.md` are the human-class exceptions. | coin | `core/okf.md` (BL-033) |
| artifact lifecycle | Artifacts carry declared lifetimes and die on schedule; nothing accumulates by default. Kin to OpenSpec's change lifecycle. | industry (kin) | `rules/core/artifact-lifecycle.md` |
| audit | Read-only inspection of the whole AI layer against structural invariants; severity-ranked rot report, zero writes. Wider than the field's `validate` (specs): audits the layer, not spec conformance. | home | SKILL.md invocation mode |
| backlog | The queue of pending cases in intended work order. Narrowed to the queue role only (industry sense); the registry role moved to *case register*. | industry (narrowed) | `docs/backlog.md`, queue section |
| baseline | The generated answer to "what must the system do today": `docs/ai/baseline.md`, regenerated from EARS ids (R-NNN) ↔ annotated tests by `python3 docs/ai/engine.py baseline`. Do-not-edit; a deleted test is a deleted line, visible in the diff. Rot-proof by construction. | coin | `docs/ai/baseline.md` (generated; BL-043, v22) |
| case | The unit of work, numbered `BL-NNN`: law, chore, or research. Minted because the field has no word for a long-lived numbered container outside a tracker — not a story, issue, or task. | coin | backlog + register rows; branches `bl/NNN-*` |
| case file | One case, one home: everything a case produces lives in its own place; the register row links to it. Forward-only migration — history stays where it lies. | coin | `docs/cases/BL-NNN/` (home scaffolded by BL-029) |
| case register | The registry of all cases with statuses (pending / active / done / dormant). Register rows are permanent records; queue entries are transient. | coin | `docs/backlog.md`, register section (split queued: BL-031) |
| changelog | Release-facing record of what changed for users, release by release (Keep-a-Changelog style). | industry | repo changelog file |
| CLAUDE.md | Managed symlink → `AGENTS.md` (pre-v14 it was the real file; since v14 AGENTS.md is canonical). Mechanics, not a concept. | home | repo root |
| constitution | `docs/ai/rules/**` @ VERSION — core rules plus subscribed stacks, delivered as one edition. Means exactly this and nothing else (R1-T1); the old loose usage for AGENTS.md is retired (cleanup: BL-030). | home | `skill/assets/rules/**` → `docs/ai/rules/**` |
| converge | The mandatory closing gate of a case cycle: judges code against every promise (spec requirements, plan decisions, constitutional MUSTs) — never against git diffs — classifies gaps missing/partial/contradicts/unrequested, appends traceable remediation tasks append-only, loops to "✅ Converged". Adapted from SpecKit converge. | home (adapted) | `core/sdd.md` (BL-032) |
| declared artifact | The source an assert reads, named as data rather than as a comment — report, repo tree, manifest, prompt, case, law, or the grader itself. Required by `Grader.check`, which is what makes "not measured is not passed" executable instead of advisory. | coin | `evals/grade.py` `Artifact` (BL-062) |
| decision gate | The escalation point where the machine stops and the human decides; conflicts are never silently resolved. | home | `rules/core/decision-gate.md` |
| drift | Any divergence of an installed copy from the central law: a hand-edited owned file, a repo left behind an edition, a local amendment. Surfaced by audit; repaired by re-run, never by editing in place. | home | ontology §1 |
| EARS | Easy Approach to Requirements Syntax: the one-line requirement forms (ubiquitous / WHEN-THEN / WHILE / WHERE / IF-THEN) every spec requirement is written in, one behavior per line, each carrying an `R-NNN`. | industry | `core/sdd.md` (BL-032) |
| edition | The whole rule corpus as delivered at one VERSION — the unit of change, adoption, and measurement. A repo is at an edition or behind it; there is no per-file patch and no partial adoption. | home | `skill/VERSION`; `evals/benchmarks/v<N>.md` |
| fleet | All legislated repos, managed through re-runs of the skill; the law stratum stays identical across it. | industry | cross-repo |
| file authority | The one table (SKILL.md `## File authority`) stating what each invocation mode may do to each artifact class, in a closed eight-value vocabulary; prose elsewhere only references a cell. The grader derives its protected set from it; the static check keeps it the only place. | coin | `skill/SKILL.md`; `evals/grade.py` `authority_matrix()` |
| generated | Third ownership class: artifacts a machine writes locally — do-not-edit, regenerated from source, die with it; not in `ownedFiles`, not keepable. First member: `docs/ai/baseline.md` (BL-043, v22). | coin | ontology §2; `core/artifact-lifecycle.md` |
| grill | The clarify round a spec passes before approval: at most five pointed questions, one at a time, recommended option first; accepted answers are written into the spec's `## Clarifications` and replace contradicted text rather than sitting beside it. | home | `core/sdd.md` (BL-032) |
| harvest | Collecting constitution candidates (law-shaped, uncovered, generalizable statements) from field repos' project prose. Proposals only — the user promotes centrally. Minted: the field is silent on upward law feedback. | coin | audit's constitution-candidates section |
| inner mode | The default placement: the AI layer lives inside the git repo it governs (owned rules, manifest, OKF, cases — all committed). Every current fleet repo is inner. | coin | ontology §Placement modes |
| journal | Append-only chronicle of what was done, session by session. Not decisions (ADR), not concept state (OKF), not releases (changelog). | industry (accepted) | `docs/journal/` |
| keep | Manifest key: project-declared protection order for named project-owned files; one reason per entry; changed only on explicit user request. Imperative form is deliberate — contrast `ownedFiles`. | home | `manifest.json` |
| law stratum | The machine-owned layer: central `assets/rules/**` @ VERSION and its byte-identical installed copies. Strictly one-way (skill → repos); per-repo mutation is drift. | home | ontology §1 |
| legislated | The state of a repo carrying the constitution via a manifest. | home | manifest presence |
| manifest | The install record: `legislatorVersion`, stack subscription, `keep`, `ownedFiles`. Written in a pinned serialization so no-change re-runs are byte-identical. | industry | `docs/ai/manifest.json` |
| OKF | Open Knowledge Format: the living documentation bundle of a system's concepts (`docs/okf/` — codebase-map, glossary, per-concept docs, `log.md`). Not specs, not steering — living docs. Coin: the field had no word. | coin | `docs/okf/` |
| opencode.json | The owned wiring file (skill bindings), machine-managed, refreshed every run. | home | repo root |
| outer mode | Placement for repos the operator cannot commit to: the layer sits outside the codebase (sidecar repo + untracked stub + probe-first + outbound redirection). Sidecar is the mechanism, not the mode's name. | coin | ontology §Placement modes; BL-027 (edition v19+) |
| owned / project-owned | Who commands a file: the machine (owned: `docs/ai/rules/**`, `opencode.json` — never hand-edited) or the project (everything else; keep protects named files). | home | ontology §2 |
| ownedFiles | Manifest key: machine-owned inventory of files the skill placed, recomputed on every run. Nominal form is deliberate — it describes state; `keep` gives orders. | home | `manifest.json` |
| profiles | Legacy alias for the stack subscription in the manifest key. The concept is *stack*; the key rename is queued (BL-028). | legacy | `manifest.json` key |
| project rules | Project-authored law in `.claude/rules/**`, local to one repo, subordinate to the constitution. | home | `.claude/rules/**` |
| project stratum | The human-owned layer (OKF, ADRs, backlog + register, journal, case files, AGENTS.md sections, project rules); grows per repo — where rot happens and where audit/restructure operate. | home | ontology §1 |
| R-NNN | The stable id every EARS requirement line carries, flowing through tasks (`per R-NNN`), annotated tests, and the generated baseline — the mechanical web that makes analyze/converge checks deterministic. | coin | specs (BL-032) |
| restructure | Propose-and-apply sanitation of the project stratum (move / merge / link / fix / heal; decisions escalated). Wider than SpecKit's `converge`: fixes a layer, not a spec↔code pair. | home | SKILL.md invocation mode |
| rot | Decay of the project stratum: docs that no longer match the code, unreachable files, a stopped journal, unresolved placeholders, foreign AI-layer structures. The failure mode the law stratum is structurally immune to and audit exists to surface. | home | audit report; ontology §1 |
| scaffold · migrate · upgrade | The three installation modes: build the layer fresh; convert a pre-v14 CLAUDE.md repo; re-deliver the current edition to a legislated repo. | home | SKILL.md |
| stack | One concept: a named package of stack-specific rule additions (`stacks/<name>/`), and by extension a repo's subscription to it. "Stack profile" as a compound is retired prose. | industry | `assets/rules/stacks/` → `docs/ai/rules/stacks/` |
| steward | Periodic review of the law itself: preference-or-compensation per rule, constitution benchmark on new models, deletion habit. The user decides; steward prepares. Minted: the field is silent. | coin | README "Steward duties" |
| tier | Ceremony level of a case: 0 direct (no spec) / 1 light (EARS spec in the case file) / 2 full (research → contracts → analyze → implement → converge), chosen at case opening on blast radius × novelty; converge may raise a tier. | coin | `core/sdd.md` (BL-032) |
| unmeasured | An assert's third verdict, beside passed and failed: the artifact it declared could not be read (absent, empty, unparseable), so the assert scored nothing. Fatal — any unmeasured assert makes its scenario red, because a run that produced no artifact is not a partial pass. | coin | `evals/POLICY.md` §1b; `evals/grade.py` `Artifact` (BL-062) |
| verification ladder | static checks → e2e benchmark → idempotency pass; each rung green before the next means anything. | coin | repo CLAUDE.md, evals |
| annotated test | A test source file carrying the literal marker `per R-NNN` — the annotation that binds it to a requirement; the baseline generator and `sdd-lint` both read exactly this form. | coin | `core/sdd.md`; engine `baseline` job (BL-043) |
| VERSION | The constitution's edition number (plain integer). A bump is a new edition of the whole corpus, never a per-file patch. | home | `skill/VERSION` |
| git conduct guard | The fourth enforcement hook (BL-064): PreToolUse on Bash blocking merge/push onto the default branch, AI attribution in commit/PR text, and `gh pr merge`, in legislated repos, fail-open on every undecidable case. The first every-commit clause moved (b)→(a). | coin | `plugin/hooks/guard_git_conduct.py`; `docs/cases/BL-064-git-conduct-guard/` |
| dependency register | BL-069's classification of every external tool the system invokes: class (hard / best-effort / operator-side), declaration home, and measured absence behavior (fail-open / fail-loud / crash / silent false green — the forbidden class). Gate 1 of the ADR-0005 binary-arm path. | coin | `docs/cases/BL-069-dependency-register/register.md` (BL-069) |
| decision inventory | BL-047's classification of every normative clause of the law and every SKILL.md decision point into three buckets: (a) enforced by a deterministic arm, (b) enforceable by a nameable check that does not exist yet, (c) genuinely needs interpretation. The ranked (b) list is the engine's growth order. | coin | `docs/cases/BL-047-decision-inventory/inventory.md` (BL-047) |
