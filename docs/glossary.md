# Legislator — Glossary

Canonical term list. Status values: **industry** — established SDD/software-field
term used in its field sense · **home** — legislator's own established term ·
**coin** — home coinage minted where the field is silent · **legacy** —
deprecated alias, kept only where it physically lives. Naming rules and the
entry path for new terms: `docs/ontology.md` §3.

| Term | Definition | Status | Lives |
|---|---|---|---|
| AGENTS.md | The repo's entry document: first thing a session reads; imports the constitution, carries project-instance data. The filename is the term — no second name. | industry | repo root |
| ADR | Architecture Decision Record: one decision, one immutable document (context / decision / status / consequences); superseded, never edited. | industry | `docs/adr/` |
| artifact lifecycle | Artifacts carry declared lifetimes and die on schedule; nothing accumulates by default. Kin to OpenSpec's change lifecycle. | industry (kin) | `rules/core/artifact-lifecycle.md` |
| audit | Read-only inspection of the whole AI layer against structural invariants; severity-ranked rot report, zero writes. Wider than the field's `validate` (specs): audits the layer, not spec conformance. | home | SKILL.md invocation mode |
| backlog | The queue of pending cases in intended work order. Narrowed to the queue role only (industry sense); the registry role moved to *case register*. | industry (narrowed) | `docs/backlog.md`, queue section |
| case | The unit of work, numbered `BL-NNN`: law, chore, or research. Minted because the field has no word for a long-lived numbered container outside a tracker — not a story, issue, or task. | coin | backlog + register rows; branches `bl/NNN-*` |
| case file | One case, one home: everything a case produces lives in its own place; the register row links to it. Forward-only migration — history stays where it lies. | coin | `docs/cases/BL-NNN/` (home scaffolded by BL-029) |
| case register | The registry of all cases with statuses (pending / active / done / dormant). Register rows are permanent records; queue entries are transient. | coin | `docs/backlog.md`, register section (split queued: BL-031) |
| changelog | Release-facing record of what changed for users, release by release (Keep-a-Changelog style). | industry | repo changelog file |
| CLAUDE.md | Managed symlink → `AGENTS.md` (pre-v14 it was the real file; since v14 AGENTS.md is canonical). Mechanics, not a concept. | home | repo root |
| constitution | `docs/ai/rules/**` @ VERSION — core rules plus subscribed stacks, delivered as one edition. Means exactly this and nothing else (R1-T1); the old loose usage for AGENTS.md is retired (cleanup: BL-030). | home | `skill/assets/rules/**` → `docs/ai/rules/**` |
| decision gate | The escalation point where the machine stops and the human decides; conflicts are never silently resolved. | home | `rules/core/decision-gate.md` |
| fleet | All legislated repos, managed through re-runs of the skill; the law stratum stays identical across it. | industry | cross-repo |
| harvest | Collecting constitution candidates (law-shaped, uncovered, generalizable statements) from field repos' project prose. Proposals only — the user promotes centrally. Minted: the field is silent on upward law feedback. | coin | audit's constitution-candidates section |
| journal | Append-only chronicle of what was done, session by session. Not decisions (ADR), not concept state (OKF), not releases (changelog). | industry (accepted) | `docs/journal/` |
| keep | Manifest key: project-declared protection order for named project-owned files; one reason per entry; changed only on explicit user request. Imperative form is deliberate — contrast `ownedFiles`. | home | `manifest.json` |
| law stratum | The machine-owned layer: central `assets/rules/**` @ VERSION and its byte-identical installed copies. Strictly one-way (skill → repos); per-repo mutation is drift. | home | ontology §1 |
| legislated | The state of a repo carrying the constitution via a manifest. | home | manifest presence |
| manifest | The install record: `legislatorVersion`, stack subscription, `keep`, `ownedFiles`. Written in a pinned serialization so no-change re-runs are byte-identical. | industry | `docs/ai/manifest.json` |
| OKF | Open Knowledge Format: the living documentation bundle of a system's concepts (`docs/okf/` — codebase-map, glossary, per-concept docs, `log.md`). Not specs, not steering — living docs. Coin: the field had no word. | coin | `docs/okf/` |
| opencode.json | The owned wiring file (skill bindings), machine-managed, refreshed every run. | home | repo root |
| owned / project-owned | Who commands a file: the machine (owned: `docs/ai/rules/**`, `opencode.json` — never hand-edited) or the project (everything else; keep protects named files). | home | ontology §2 |
| ownedFiles | Manifest key: machine-owned inventory of files the skill placed, recomputed on every run. Nominal form is deliberate — it describes state; `keep` gives orders. | home | `manifest.json` |
| profiles | Legacy alias for the stack subscription in the manifest key. The concept is *stack*; the key rename is queued (BL-028). | legacy | `manifest.json` key |
| project rules | Project-authored law in `.claude/rules/**`, local to one repo, subordinate to the constitution. | home | `.claude/rules/**` |
| project stratum | The human-owned layer (OKF, ADRs, backlog + register, journal, case files, AGENTS.md sections, project rules); grows per repo — where rot happens and where audit/restructure operate. | home | ontology §1 |
| restructure | Propose-and-apply sanitation of the project stratum (move / merge / link / fix / heal; decisions escalated). Wider than SpecKit's `converge`: fixes a layer, not a spec↔code pair. | home | SKILL.md invocation mode |
| scaffold · migrate · upgrade | The three installation modes: build the layer fresh; convert a pre-v14 CLAUDE.md repo; re-deliver the current edition to a legislated repo. | home | SKILL.md |
| stack | One concept: a named package of stack-specific rule additions (`stacks/<name>/`), and by extension a repo's subscription to it. "Stack profile" as a compound is retired prose. | industry | `assets/rules/stacks/` → `docs/ai/rules/stacks/` |
| steward | Periodic review of the law itself: preference-or-compensation per rule, constitution benchmark on new models, deletion habit. The user decides; steward prepares. Minted: the field is silent. | coin | README "Steward duties" |
| verification ladder | static checks → e2e benchmark → idempotency pass; each rung green before the next means anything. | coin | repo CLAUDE.md, evals |
| VERSION | The constitution's edition number (plain integer). A bump is a new edition of the whole corpus, never a per-file patch. | home | `skill/VERSION` |
