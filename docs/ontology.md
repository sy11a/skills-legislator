# Legislator — Ontology

Canonical model of the legislator system: strata, entities, relations, and the
naming conventions that govern every term the product uses. Decisions recorded
here came out of the 2026-08-20 ontology review (sessions R1–R5: every term
checked against SDD-field conventions first, internal harmony second). Companion
files: `docs/okf/glossary.md` (term-by-term definitions and statuses) and
`docs/philosophy.md` (the narrative layer above this model — what the system is
and why it is built this way; it argues, this document defines). This document
is the seed of self-legislation (A4): the legislator repo's own ontology, kept
honest by the same review discipline the skill prescribes for other repos.

## 1. Strata

Three layers, one direction of authority:

| Stratum | Contents | Owner | Mutability |
|---|---|---|---|
| **Law stratum** | `assets/rules/**` @ VERSION (central) → `docs/ai/rules/**` (installed copies) | the legislator (machine) | edition-based: edit centrally → bump VERSION → re-run; installed copies are byte-identical, never hand-edited |
| **Project stratum** | OKF, ADRs, backlog + case register, journal, case files, AGENTS.md project sections, project rules | the project (humans) | grows per repo; this is where rot happens |
| **Wiring** | `opencode.json` (owned), `CLAUDE.md` (managed symlink → `AGENTS.md`) | the legislator (machine) | refreshed every run |

Flow directions:

- **Downward — law delivery.** Central rules → byte-for-byte copy → manifest
  records the edition. Strictly one-way; per-repo mutation of law is drift.
- **Downward — health.** audit (read-only inspection) → restructure
  (propose → approve → apply — never automatic).
- **Upward — feedback.** harvest: constitution candidates are proposals only;
  the user promotes worthy ones centrally (VERSION bump) and re-runs fleet-wide.

## 2. Entities

### Documents

- **constitution** — `docs/ai/rules/**` @ VERSION: core rules plus the
  subscribed stacks' rules, delivered as one edition. The word means exactly
  this and nothing else (R1-T1). The former loose usage for `AGENTS.md`
  ("constitution file") is retired — swept in v17–v18.
- **AGENTS.md** — the repo's entry document. The filename is the term
  (industry convention); it carries no second name. Its role — the single
  entry point a session reads first, importing the constitution and carrying
  project-instance data — is described in lowercase prose, not capitalized.
- **CLAUDE.md** — a managed symlink → `AGENTS.md`. Mechanics, not a concept.
- **opencode.json** — the owned wiring file (skill bindings), machine-managed.
- **project rules** — `.claude/rules/**`: project-authored law, local to one
  repo, subordinate to the constitution.
- **manifest** — `docs/ai/manifest.json`, the install record:
  `legislatorVersion`, the stack subscription (`stacks`; a legacy manifest
  may carry it as `profiles`, read as the same field), `keep`,
  `ownedFiles`.

### Manifest key conventions

- `ownedFiles` — machine-owned **inventory**, recomputed on every run
  (nominal, state-describing).
- `keep` — project-declared **protection order**, one reason per entry,
  changed only on explicit user request (imperative). The grammatical
  asymmetry with `ownedFiles` is deliberate and follows the semantic one:
  inventory vs order.
- **owned vs project-owned** — the standing distinction for who commands a
  file. Owned (machine): `docs/ai/rules/**`, `docs/ai/engine.py`,
  `opencode.json` — never hand-edited, refreshed by re-run. Project-owned:
  everything else; the keep list protects named project-owned files from
  restructure.
- **generated** — the third ownership class (decided 2026-08-20, deep-audit
  D2; scoped 2026-08-23, BL-033): artifacts written by a machine **locally
  in the repo**, not delivered from the center and not hand-maintained.
  Properties: do-not-edit, regenerated from their source on demand, die
  together with their source; not listed in `ownedFiles` (nothing is
  byte-copied onto them), not keepable. The class's first member is
  `docs/ai/baseline.md` (BL-043, edition v22): the R-NNN ↔ annotated-tests
  register, written by `python3 docs/ai/engine.py baseline` and by nothing
  else. `codebase-map.md` and `index.md` are *not* members —
  D2 assumed they were, and the fleet showed otherwise: their rows carry
  judgment a generator would destroy, while their structure is already
  machine-checked (audit checks 6 and 5). They are anchored instead.
- **anchored** — a reference document bonded to code by its own text: every
  path and PascalCase symbol it backticks resolves in its repository,
  verified by `docs/ai/engine.py anchors`. The OKF bundle's default class;
  `glossary.md` and `log.md` are the human-class exceptions.

### Work

- **case** — the unit of work, numbered `BL-NNN`. Any kind: law, chore,
  research. Deliberately not a story (no user-value ceremony), not an issue
  (no tracker behind it), not a task (tasks are the level inside a case).
- **backlog** — the queue of pending cases in intended work order. The
  industry sense of the word, deliberately narrowed to the queue role (R2-T2).
- **case register** — the registry of all cases with their statuses
  (pending / active / done / dormant). Currently a named section of
  `docs/backlog.md`; the split into two artifacts is queued (BL-031).
- **case file** — one case, one home: everything a case produces lives in
  its own place (`docs/cases/BL-NNN/` — spec, plan package, summary), and
  the register row links to it. Every work kind is a case, including
  explorations (the 2026-08-20 consistency review retracted the earlier
  exception — no second spec home). Migration is forward-only (R2-T3):
  history stays where it lies (`docs/superpowers/**` is the legacy path),
  the home ships with the SDD law (BL-032 absorbed BL-029); moving active
  cases is optional restructure work per repo.

### Processes (skill verbs)

scaffold · migrate · upgrade · audit · restructure · harvest · steward —
all reviewed against the field (R3) and all kept. Where the field has
near-words, ours deliberately differ in scope: audit inspects a whole layer
(not spec conformance, so not `validate`); restructure sanitizes a layer
(not reconciling a pair, so not `converge`); scaffold builds a structure
(not initializing an entry point, so not `init`). The jurisprudential
register is the brand.

### Knowledge homes — each answers one question

| Home | Answers | Mutability |
|---|---|---|
| ADR (`docs/adr/`) | what was decided, and why | immutable; superseded, never edited |
| journal (`docs/journal/`) | what was done, session by session | append-only chronicle |
| OKF (`docs/okf/`) | what the concepts are | living docs; `log.md` records concept changes |
| changelog | what changed for users, release by release | release-facing |

### Mechanics

- **stack** — one concept: a named package of stack-specific rule additions
  (`stacks/<name>/`), and by extension a repo's subscription to such a
  package. "Profile" is a legacy alias surviving only as the manifest key
  name (R5-T1); the key rename is queued (BL-028).
- **fleet** — all legislated repos, managed through re-runs of the skill.
- **VERSION** — the constitution's edition number (plain integer). A bump is
  a new edition of the whole corpus, never a per-file patch.
- **decision gate** — the escalation point where the machine stops and the
  human decides. Conflicts are never silently resolved.
- **verification ladder** — static checks → e2e benchmark → idempotency
  pass; each rung must be green before the next means anything.
- **artifact lifecycle** — artifacts carry declared lifetimes and die on
  schedule; nothing accumulates by default.

### Placement modes — inner / outer

Two ways a legislated AI layer can sit relative to the codebase, named
2026-08-20 on the axis "relation of the docs to the code":

- **inner mode** (default) — the owned layer lives inside the git repo
  itself: `docs/ai/rules/**`, manifest, OKF, cases, journal all committed
  beside the code they govern. Every fleet repo today is inner.
- **outer mode** — the layer sits outside the codebase for an operator who
  cannot commit to the target repo: a sidecar repo hosts the owned layer,
  an untracked stub in the local clone imports it, knowledge flows through
  probes at external systems (Jira/Confluence). The **sidecar is the
  mechanism, not the mode's name**; outer names the whole placement:
  sidecar + stub + probe-first doctrine + outbound redirection +
  progressive-rigor floor (BL-027, edition v19+).

The mode is a property of a legislated repo, declared in its manifest.
One core, no fork: law overlap between modes is ~90%; the differences are
placement mechanics and ceremony defaults, decided in BL-027's design
cycle.

## 3. Naming conventions — how terms enter and stay

1. **Industry term first.** Where the SDD/software field has an established
   word for the meaning (spec, plan, ADR, changelog, manifest, stack,
   backlog, journal), that word wins. No invention against convention.
2. **Home-established second.** Terms the system already runs on
   (constitution, OKF, harvest, steward, case, fleet) stay unless they
   mislead — stability is itself a value, and migration cost is real.
3. **Coinage last.** A new word is minted only where the field is silent
   (OKF and harvest were such cases). A coinage must be non-colliding,
   register-consistent (jurisprudential, matching the legislator brand),
   and immediately glossed in `docs/okf/glossary.md`.
4. **One word, one meaning.** The constitution ambiguity (one word, three
   referents — A5) is the cautionary tale. Any term drifting toward a second
   meaning goes back to review.
5. **Entry path.** New terms enter only through steward review, and the same
   session that decides them updates this ontology and the glossary.
   Metaphor systems and codenames are fine for private thinking; they never
   ship.

## 4. Review provenance

This ontology is the product of the 2026-08-20 review — R1 documents,
R2 work units, R3 processes, R4 knowledge, R5 mechanics. A cancelled
metaphor branch (a play-ontology explored and rolled back the same day)
deliberately left no trace here; its structural findings survive as the
queued cases BL-028 through BL-031.
