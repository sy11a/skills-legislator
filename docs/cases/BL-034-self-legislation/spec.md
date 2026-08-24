# BL-034 — Self-legislation: the legislator repo joins its own fleet

**Tier: 2 (full).** Blast radius: the entry document, every doc home, the
release runbook. Novelty: this repository has never been legislated, and it
is the one repository whose legislation is recursive.

**Spec type: feature.**

**Status:** approved 2026-08-24. First case in this repository's `docs/cases/`
home — the directory is created by this case, which is the bootstrap it
describes.

## Purpose

Apply the legislator to the repository that hosts it. Every law this project
ships is currently exercised only in other people's repositories; the repo
that writes the law runs on hand-maintained convention instead. Closing that
loop is the strongest honesty test the system has: a rule that cannot govern
its own author is not a rule, it is a recommendation.

## Boundary

**In scope**

- Migration of this repository into the standard layout: manifest, owned
  layer, OKF, case home, project rules, entry document.
- Forward-only migration of the practices already run here by hand
  (glossary, spec/plan conventions) into the homes the law names.
- The release runbook gaining deliver-to-self, and the version-skew rule
  that makes branch development lawful.

**Out of scope — and the out-of-scope half is half the assignment**

- **Any change under `skill/`.** No VERSION bump, no e2e benchmark. If this
  case surfaces a gap in the law, that gap becomes its own edition; it is not
  fixed here.
- **`tools/fleet.sh`.** Its discovery is `-maxdepth 4`, and this repository
  sits one level deeper, so it cannot be found by the sweep. Confirmed, filed,
  deliberately not fixed: the first delivery is manual and the permanent
  channel is a later decision (see Clarifications, Q1).
- **`docs/superpowers/**`.** Legacy history. Never rewritten, never moved.
- **Promoting this repo's rules into the constitution.** Migration will
  surface constitution candidates; they are proposals for a later edition.
- **The `opencode.json` question.** Step 3 writes it as owned law and this
  case accepts it, though the opencode profile was frozen on 2026-08-24.
  Removing an owned artifact is a `skill/` change and therefore out of scope.

## Requirements

Ids are permanent; tasks and verification reference them.

**The delivered layer**

- **R-001** — The repository SHALL carry `docs/ai/manifest.json` recording
  `legislatorVersion` equal to `skill/VERSION` at delivery time, `stacks` as
  the empty list, and an `ownedFiles` list naming every owned file delivered.
- **R-002** — Every path named in `ownedFiles` SHALL be byte-identical to its
  source under `skill/assets/`.
- **R-003** — WHEN the entry document is canonicalized, THEN `AGENTS.md`
  SHALL be the only real entry document and `CLAUDE.md` SHALL be a symlink to
  it.
- **R-004** — WHEN a statement in the pre-migration `CLAUDE.md` is law-shaped
  and specific to this repository, THEN it SHALL live in a file under
  `.claude/rules/` after migration.
- **R-005** — WHEN a statement in the pre-migration `CLAUDE.md` is already
  stated by an owned rule, THEN it SHALL NOT appear in `AGENTS.md` or under
  `.claude/rules/` after migration.
- **R-006** — `AGENTS.md` SHALL carry the v2 wiring: the `@docs/ai/rules/**`
  import block, the `@docs/okf/codebase-map.md` import, a `## Boundaries`
  section, and the glossary pointer line.

**The forward-only migration**

- **R-007** — WHEN the glossary is migrated, THEN every term row present in
  `docs/glossary.md` before this case SHALL be present in
  `docs/okf/glossary.md` after it, and `docs/glossary.md` SHALL NOT exist.
- **R-008** — No tracked file SHALL carry a *live* reference to
  `docs/glossary.md` after this case. A live reference is one a reader would
  follow to find the glossary. Two classes of mention are not live and are
  exempt: `docs/superpowers/**`, which is retired history and never rewritten,
  and this case's own record under `docs/cases/BL-034-self-legislation/`,
  which must name the pre-migration path to describe what moved.
- **R-009** — `docs/okf/codebase-map.md` SHALL anchor only to paths that
  exist in the repository.

**The repository stays workable**

- **R-010** — WHEN `python3 docs/ai/engine.py` runs against this repository,
  THEN it SHALL report no unresolved anchor and no OKF sync debt.
- **R-011** — `evals/check_static.py`, `evals/check_engine.py`,
  `evals/check_hooks.py` and `evals/check_opencode_plugin.mjs` SHALL all pass
  after this case.
- **R-012** — WHEN audit mode runs against this repository, THEN it SHALL
  report clean, or every finding SHALL be explained in this case's summary.
- **R-013** — No file under `skill/` SHALL be modified by this case.

**The practice**

- **R-014** — `README.md` SHALL document deliver-to-self as a release step.
- **R-015** — WHILE an edition v(N+1) is under development on a branch, the
  repository's owned layer remaining at v(N) SHALL NOT be a finding; ON the
  default branch, owned-integrity drift SHALL be a finding.

## The hurting case

The one it would hurt most to see broken. Self-legislation's characteristic
failure is that the law, once installed, prevents the repository from
producing the next version of that law.

**H-1 — developing the next edition must stay possible.**

> **GIVEN** the repository is legislated at v20 and the write-guard plugin is
> active,
> **WHEN** a developer edits `skill/assets/rules/core/okf.md`, bumps
> `skill/VERSION` to 21, and commits on a branch,
> **THEN** no hook blocks the edit; `docs/ai/rules/core/okf.md` still holds
> the v20 bytes; the manifest still reads `legislatorVersion: 20`; and no
> check reports the skew as a finding.

Observable by a tester who never read the code: make the edit, run the four
static suites, read the manifest.

**H-2 — the anchor rung must not wedge unrelated work.**

> **GIVEN** the repository carries `docs/okf/` with a codebase map anchored to
> source paths,
> **WHEN** a developer finishes any unrelated task and runs the verification
> rung,
> **THEN** `docs/ai/engine.py` reports no unresolved anchor, so the rung does
> not block "done" for work that never touched the OKF.

## Clarifications

Session 2026-08-24 (design approval, in chat).

**Q1 — BL-034 says both "fleet member #0" and "deliver-to-self is a release
step". How should this repo receive the law?**
A: Manually for the first delivery. `tools/fleet.sh` is not changed, its
`-maxdepth 4` blind spot is recorded rather than fixed, and the permanent
channel is decided after the first real diff is visible.

**Q2 — What happens to the two glossaries?**
A: Merge forward into `docs/okf/glossary.md`, carrying the existing columns;
delete `docs/glossary.md`; fix every reference. One term list, in the home the
law names, under `core/okf.md`'s sync checklist. (R-007, R-008.)

**Q3 — What is the accepted risk on `check_static.py`?** (raised in the same
session, answered by the design approval)
A: The sixteen owned copies under `docs/ai/rules/**` may read to some static
check as a second source of law. Adapting `check_static.py` so it knows about
owned copies is inside this case's scope; changing what the checks *mean* is
not.

**Q4 — R-008 forbade every reference to `docs/glossary.md` outside
`docs/superpowers/**`; the case's own spec, plan and research name it.**
(found while executing T-05, 2026-08-24)
A: The requirement was written too broadly. Its intent is that no *live*
pointer survives — a mention inside the record of the move is not a pointer to
follow. R-008 amended above to say so, rather than either weakening the check
or rewording a record to satisfy a mis-stated rule.

## Rejected alternatives

- **Keep two glossaries with separate roles** — honest about their different
  shapes, but the second one sits outside the OKF sync checklist, which is
  exactly how the divergence this project exists to prevent begins.
- **Fix `fleet.sh` discovery inside this case** — it decides the permanent
  delivery channel as a side effect of a scaffolding task. The channel
  deserves its own decision, taken after the first diff is on disk.
- **Retro-file BL-034 as a case after the migration** — it would satisfy the
  letter of "a real case runs end to end" and none of its meaning. The case
  home is created by hand first so that this case is genuinely the first one
  filed under the law, not the first one back-dated into it.
