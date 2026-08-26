# BL-065 — `sdd-lint` grows the case-shape lints (edition v23)

**Tier: 2 (full).** Blast radius: the engine's `sdd-lint` job in every
legislated repo, `core/sdd.md`'s naming of the mechanical passes, and this
repo's own case corpus (member #0 is linted by what it ships). Novelty:
the lints reach beyond the case file into ADRs, the journal, the
changelog and OKF front matter.

**Spec type: feature.** Rides edition branch `bl/065-066-edition-v23`.
Source: BL-047's ranked list, group 3 (the every-task clauses).

## Requirements

Each lint: one clause, one finding format (`<path>: <what> → <remedy>`),
converged cases skipped (history is never re-linted), findings exit 1,
clean exits 0 — the existing `sdd-lint` contract.

- **R-651** — WHEN a case spec under `docs/cases/*/spec.md` lacks a
  declared tier (`**Tier: N`) or spec type (`**Spec type:`) in its
  header, `sdd-lint` SHALL report it (sdd-3, sdd-7).
- **R-652** — WHEN a spec of type bugfix lacks current/expected/unchanged
  behavior statements, `sdd-lint` SHALL report it (sdd-8, presence only).
- **R-653** — WHEN a spec of tier ≥ 1 lacks an in-scope/out-of-scope
  boundary or a named GIVEN/WHEN/THEN scenario, `sdd-lint` SHALL report
  it (sdd-9, sdd-13).
- **R-654** — WHEN a requirement bullet defines an `R-NNN` id whose line
  carries zero or more than one SHALL, `sdd-lint` SHALL report it
  (sdd-11, shape only; quoted tokens stay quotation per BL-057).
- **R-655** — WHEN a spec of tier ≥ 1 lacks a `## Clarifications`
  session, `sdd-lint` SHALL report it (sdd-15).
- **R-656** — WHEN a file under `docs/adr/` breaks the ADR shape —
  filename not `NNNN-kebab-title.md`, a gap in the number sequence, a
  missing Status/Context/Decision/Consequences section, or a status
  outside the closed set — `sdd-lint` SHALL report it (adr-2, adr-3,
  adr-4); `template.md` is exempt by name.
- **R-657** — WHEN a file under `docs/journal/` (except `README.md`) is
  not named `YYYY-MM-DD.md`, `sdd-lint` SHALL report it (jrnl-1).
- **R-658** — WHEN `CHANGELOG.md` lacks the `## [Unreleased]` heading,
  `sdd-lint` SHALL report it (chlog-1, structure only).
- **R-659** — WHEN an OKF concept document's front-matter `status` is
  outside {planned, partial, implemented, removed}, `sdd-lint` SHALL
  report it (okf-5). Human-class documents (`glossary.md`, `log.md`) are
  exempt, as in the anchors job.
- **R-660** — `core/sdd.md`'s sentence naming the mechanical passes SHALL
  name the grown families (case shape, ADR shape, journal shape,
  changelog structure, OKF front matter) — law text change, part of the
  v23 VERSION bump.

**Dropped from the backlog's ~20, with reasons stated (no silent caps):**
sdd-29 (the "✅ Converged" close marker binds a *closing act*, not a
static tree state — unlintable without a merge event); chlog-2 (the
task-commit changelog touch needs a task-boundary event); okf-6
(timestamp-vs-diff needs the working diff — okf-debt already owns the
committed-history half); adr-7 (ADR↔OKF linkage: this bundle has no
concept docs to link from — premature fleet-wide); sdd-12/sdd-16/sdd-18
(git-history and disjointness checks — a later, git-aware lint family).

## Eval design (POLICY §3)

All lints are engine behavior: red-first lives in `evals/check_engine.py`
— one failing fixture and one clean fixture per lint, written and shown
red against the v22 engine before the lint exists. The corpus is touched
only through R-660 (law text) — no scenario's *agent* behavior changes,
so no new corpus asserts; `selftest:derivation` continues to bind the
SKILL.md-derived maps, and the member-#0 tree itself must lint clean
(this repo's own cases are the live fixture — any red here is a real
finding to fix before the edition ships).

| check_engine test (per R) | fixture bite | negative control |
|---|---|---|
| `lint_tier_and_type_required` (R-651) | spec without header lines | headered spec silent |
| `lint_bugfix_sections` (R-652) | bugfix spec missing "unchanged" | feature spec exempt |
| `lint_boundary_and_hurting_case` (R-653) | tier-1 spec without boundary/scenario | tier-0 case exempt |
| `lint_ears_single_shall` (R-654) | R-line with two SHALLs; R-line with none | backticked `SHALL` is quotation |
| `lint_clarifications_required` (R-655) | tier-1 spec without the section | tier-0 exempt |
| `lint_adr_shape` (R-656) | gap in sequence; bad status; missing section | template.md exempt |
| `lint_journal_filename` (R-657) | `notes.md` in docs/journal/ | README.md exempt |
| `lint_changelog_unreleased` (R-658) | CHANGELOG without the heading | present → silent |
| `lint_okf_status_closed_set` (R-659) | `status: shipped` | glossary/log exempt |

Mutations: engine unit tests are outside the corpus manifest (POLICY §1c
covers corpus asserts); their falsifiability is the red-first fixture
pair itself, recorded in check_engine.

## The hurting case

GIVEN this repository (member #0) at the edition commit, WHEN
`python3 docs/ai/engine.py sdd-lint` runs, THEN it exits 0 — every
shipped case, ADR, journal file and OKF document passes the very lints
the edition ships — AND GIVEN any one planted defect from the table
above, THEN the lint names exactly that file with its remedy. The case
that hurts most: a lint that fires on lawful history — converged cases
and human-class documents must stay exempt, or the fleet's first upgrade
run buries owners in findings about finished work.
