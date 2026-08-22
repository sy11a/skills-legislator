# File-authority matrix — design (BL-038, anchor case of edition v18)

**Date:** 2026-08-22 · **Case:** BL-038 · **Edition:** v18 (VERSION 17 → 18)
· **Tier:** 2 (full) — blast radius: every invocation mode and the grader;
novelty: a new law form (a table that is itself the law, derived by the
grader, fenced by a static check).

Riders in the same cycle: the prose residue of BL-028 and BL-030 (§7), both
substantially shipped in v17 (`92d1e3d`) with stale backlog statuses.
BL-031 leaves the cycle (§7). The Horizon section of `docs/philosophy.md`
drops BL-038 when this edition closes (`check_static.py` enforces it).

## 1. Problem

The question "may this mode write this file?" is answered in prose, in
several places, per artifact class — and each place re-derives the same
underlying boundary. The v17 benchmark showed what that costs: the same fact
about the entry document was stated four times, one of them overstated
("`AGENTS.md` is project-owned, so the Legislator never edits it directly" —
written unconditionally while Step 4 and Step 5 both write that file), and the
eval harness quoted the overstatement. Two model families hid the
contradiction by following the skill over the harness; a third resolved it
the other way and exposed it. The v17 repair stated the invariant once
(SKILL.md's **Entry-document authority** paragraph). This case is the general
repair: one table, referenced everywhere, derived by the grader, and a wall
against prose growing back.

Scan of the current surface (baseline for §6): 13 authority-shaped phrases in
`skill/SKILL.md` (lines 42, 58 ×3, 64 ×2, 79 ×2, 81 ×3, 95, 120) and 1 in
`skill/references/migration.md` (line 27). `grade.py` derives its protected
set as "Step 4's table minus `AGENTS.md` and `CLAUDE.md`" — a hand-written
exclusion that encodes one cell of the matrix by name
(`protected_project_files()`, lines 75–81).

## 2. Decisions (brainstorm 2026-08-22)

| # | Decision | Alternatives rejected |
|---|---|---|
| D1 | **Form:** artifact class × mode, modes grouped under a state header (installing / maintaining / inspecting). | Three state columns with footnotes (footnotes are prose — the thing being removed); long table of (class, mode) rows (~40 rows, unreadable as law). |
| D2 | **Scope:** skill invocation modes only. `core/pair-development.md`'s "never hand-edit `docs/ai/rules/**`" and the hooks plugin's write-guard address a different actor (a human or agent outside a run) and are read where SKILL.md is not present; they stay as they are, each marked as the mirror of a named cell. | An eighth "project agent" column (SKILL.md is not in the target repo, so the delivered rule could not point at it); moving the matrix into a delivered rule file (every project session would read law about the skill's internal modes — context noise). |
| D3 | **Vocabulary:** eight closed values, each cell self-sufficient, no footnotes, definitions beside the table and themselves law. | Five values + footnotes; right + open qualifier in parentheses (the parenthesis is an open set). |
| D4 | **Columns:** five — scaffold, migrate, upgrade, restructure, audit. **Harvest** is a report section inside migrate/upgrade/audit and writes nothing by construction; **steward** is a human duty performed on the skill's own repository (README § Steward duties) and never acts on a legislated repo. Neither gets a column; one line under the table says so, so the absence reads as a decision. | Seven columns with two all-`read-only` columns (an empty column in law is an invitation to fill it — and a human role would quietly become a machine mode). |
| D5 | **Strategy:** A + C — the matrix is the sole source (prose becomes references), the grader derives its sets from it, and `check_static.py` fails on authority-shaped prose outside the matrix. | A alone (nothing stops the next edition from writing a right back into a step); B (table beside surviving prose — the original disease half-cured). |
| D6 | **Evals first:** every new assert is written and shown RED against the unchanged v17 law before any law edit; the baseline counts are recorded in this spec (§6). | — (policy, not a choice). |

## 3. The matrix

Lives in `skill/SKILL.md` as a section headed exactly `## File authority`,
placed after Step 3 (where the Entry-document authority paragraph sits today)
and before Step 4 (the first step that references it). The heading and the
table shape are pinned — `grade.py` and `check_static.py` parse them.

The shipped table has **two header rows**: the state row and the mode row.
Markdown has no column groups, so the state row repeats its name in every
column it spans — the grader reads a mode's state from the cell above it.

```
| artifact class | installing | installing | maintaining | maintaining | inspecting |
| | scaffold | migrate | upgrade | restructure | audit |
|---|---|---|---|---|---|
| entry document (`AGENTS.md`; `CLAUDE.md` is its symlink) | replace | lossless-write | propose-only | lossless-write | read-only |
| owned law (`docs/ai/rules/**`, `opencode.json`) | replace | replace | replace | never-touch | read-only |
| manifest (`docs/ai/manifest.json`) | replace | replace | replace | never-touch | read-only |
| project rules (`.claude/rules/**`) | create-if-absent | lossless-write | create-if-absent | move-or-merge | read-only |
| scaffolded artifacts (Step 4's table, the OKF bundle included) | create-if-absent | create-if-absent | create-if-absent | move-or-merge | read-only |
| relocated owner content (glossary rows, the OKF mapping table, legacy plans/specs, `BL-NNN` directories) | read-only | lossless-write | read-only | move-or-merge | read-only |
| foreign structures (`.cursorrules`, stray rulebooks, non-standard AI dirs) | read-only | read-only | read-only | move-or-merge | read-only |
| kept paths (manifest `keep`) | link-only | link-only | link-only | link-only | read-only |
```

(Markdown renders only the first row as a header; the mode row reads as a
body row visually. That is accepted: the grader and the static check parse
by position, the row of mode names is unmistakable to a reader, and one
pinned byte form beats two renderable ones — §5 checks this exact shape.)

**State rule (the one sentence of prose the table needs):** *`docs/ai/manifest.json`
is the boundary — absent, the layer is being installed; present, it is being
maintained or inspected. A mode's column is fixed; its state header names the
repo state the mode assumes.* This is the v17 invariant, now a header rather
than a paragraph.

**Vocabulary (closed, eight values; these definitions are law):**

- `replace` — the content comes whole from the skill; whatever exists is
  replaced byte-for-byte (Bash `cp`), never merged, never edited.
- `create-if-absent` — created from a template when missing; an existing file
  is never touched, whatever its content.
- `lossless-write` — the run writes owner content (into the file, or out of it
  into its home) such that every sentence survives; the fidelity pass is the
  proof. Removing machine wiring that points at nothing (a dangling
  `@import`, a stale map row) is inside this right — such a line is not owner
  content, and the fidelity pass already exempts a `fix` item's named dead
  lines.
- `propose-only` — never written; exact lines are printed in the report under
  `## Needs your review`, and the owner applies them.
- `move-or-merge` — relocated or folded whole under an approved plan item
  (`references/restructure.md` §2); content is carried, never edited.
- `link-only` — a link *to* the path may be added elsewhere; the path itself
  is never moved, merged, fixed, or rewritten.
- `read-only` — read to judge and report; zero writes.
- `never-touch` — outside the mode's jurisdiction: not written, not proposed
  about; another mode owns the repair (owned law drifts → upgrade/heal, not
  restructure's own hands).

**Under the table (pinned prose, two lines):** the harvest/steward line of
D4, and the state rule above.

**Cell notes that are *not* footnotes** — they are consequences the reader can
verify against the vocabulary, listed here for the spec reader only:

- entry document × migrate = `lossless-write`: the `git mv CLAUDE.md →
  AGENTS.md` rename and the symlink are part of the pair, so the pair is one
  class and the rename is not a violation.
- entry document × restructure = `lossless-write`, **not** a narrower
  delete-only right (corrected while planning, 2026-08-22): restructure's
  `fix` performs the v14 canonicalization on a pre-v14 repo — rename plus
  wiring *appended* to the verbatim content — and the restructure fixture
  exercises exactly that (`grade.py` `v14_model_canonicalized`). A
  delete-only cell would have failed lawful behaviour. The dangling-import
  removal that the v17 paragraph singled out is a sub-case of
  `lossless-write`; a ninth value existed only to name it, and a value no
  cell needs is the same defect as an empty column.
- owned law × restructure = `never-touch` while restructure's `heal` action
  "runs SKILL.md Steps 2–3 as-is": heal *invokes* the upgrade column, it does
  not write under restructure's own authority. `restructure.md` §2's heal
  bullet gains the words "(authority: owned law × upgrade — heal is a
  delegated upgrade, not a restructure write)".
- kept paths × restructure = `link-only`: today's sentence "the only action
  allowed to touch a kept file is linking to it" becomes this cell.

## 4. The grader derives from the matrix

New in `evals/grade.py` (contract-derivation block, after `scaffold_artifacts`):

- `authority_matrix() -> dict[tuple[str, str], str]` — parses the
  `## File authority` section: the mode row gives the columns, the state row
  gives each mode's state, each body row gives a class (the text before the
  first parenthesis, lowercased) and five cells. Raises if the shape is not
  8 rows × 5 modes or a cell is outside the eight values (a malformed matrix
  must fail loudly, not grade leniently).
- `class_paths(repo, cls) -> list[str]` — resolves a class to concrete
  repo-relative paths: entry document → `AGENTS.md`, `CLAUDE.md`; owned law →
  `expected_owned()`; manifest → the manifest; project rules → tracked files
  under `.claude/rules/`; scaffolded artifacts → `SCAFFOLD_ARTIFACTS`; kept
  paths → the fixture manifest's `keep`; relocated owner content and foreign
  structures → the fixture's own declared lists (fixture meta, as
  `expected_stacks` already does).
- `protected_project_files(repo)` — **rewritten**: every path of every class
  whose `upgrade` cell is in {`create-if-absent`, `propose-only`, `read-only`,
  `link-only`, `never-touch`} and which existed at HEAD. The
  `("AGENTS.md", "CLAUDE.md")` exclusion is deleted; the entry document drops
  out because its cell says `propose-only`.
- `check_mode_authority(g, repo, mode)` — one generic assert,
  `mode_respects_authority`, run in `grade_fresh`, `grade_migration`,
  `grade_migration_agents_first`, `grade_upgrade`, `grade_upgrade_drop_stack`,
  `grade_restructure`, `grade_audit`: for each class, the tracked-file diff
  restricted to its paths must satisfy the cell — `replace`: any change;
  `create-if-absent`: only additions (`A` status); `lossless-write` and
  `move-or-merge`: any change, the existing fidelity assertions are the
  content proof; `propose-only`, `read-only`, `never-touch`: no change;
  `link-only`: no change to the path itself. The assertion message names the violating cell: `entry document ×
  upgrade = propose-only, but AGENTS.md modified`.

`selftest:derivation` gains:

- `authority_matrix_shape` — 8 classes × 5 modes parsed, every cell in the
  eight-value set.
- `authority_states_pinned` — scaffold, migrate → installing; upgrade,
  restructure → maintaining; audit → inspecting.
- `protected_set_derived_from_cells` — replaces
  `protected_excludes_entry_document_pair`: `AGENTS.md` is absent from the
  protected set **and** the matrix cell (entry document, upgrade) is
  `propose-only`; if someone flips the cell to `read-only`, `AGENTS.md` must
  appear in the set — the assert checks both directions by computing the set
  from a patched copy of the matrix.

## 5. The wall — `check_static.py`

New section `== file authority: one table, no prose rights ==`:

1. `SKILL.md` has exactly one `## File authority` section; the table parses to
   8 × 5; every cell is one of the eight values; the mode row is exactly
   `scaffold | migrate | upgrade | restructure | audit`.
2. **No authority-shaped prose outside the table.** `SKILL.md` and
   `references/*.md`, minus the `## File authority` section and minus the
   vocabulary list, are scanned line by line for
   `never (edit|edits|edited|touch|touches|touched|overwrite|overwrites)`,
   `is project-owned`, `project-owned, so`, `project-owned after creation`,
   `overwritten on every run`, `create[d]?-once`, `create it only if`,
   `only if it does not already exist`. A hit is a FAIL naming file:line.
   The sanctioned form is a cell reference: `(authority: <class> × <mode>)`.
   The regex list is in the check, not in law; a false positive is fixed by
   narrowing the regex or by rewording the prose, both of which are visible
   in the diff.
3. Every `(authority: <class> × <mode>)` reference in `SKILL.md` and
   `references/*.md` resolves to a real row and column — a reference to a
   class that was renamed is a FAIL, so the references cannot go stale.

The delivered rules (`assets/rules/**`) and templates are **not** scanned:
`core/pair-development.md` keeps its "never hand-edit" line by D2.

## 6. Evals first — the red baseline

Order of work, per `evals/POLICY.md`; nothing under `skill/` changes before
step 4.

1. Write `authority_matrix()`, `class_paths()`, the rewritten
   `protected_project_files()`, `check_mode_authority()`, and the three
   selftest asserts. Run `selftest:derivation` against v17 law — expected
   RED: `authority_matrix_shape` fails with "no `## File authority` section".
   Record the output in `evals/benchmarks/v18.md` § Red baseline.
2. Wire `mode_respects_authority` into the seven graders; run `grade.py` on
   the existing v17 workspace (the v17 benchmark run's outputs are still on
   disk in the scratch workspace; if not, re-materialize and re-run the
   upgrade scenario only — the assert's red does not depend on the model, it
   depends on the missing table). Expected RED in every wired scenario with
   the same cause. Record.
3. Write the wall; run `check_static.py` against v17 — expected RED with
   **14 hits** (13 in SKILL.md, 1 in migration.md — §1's scan) plus the
   missing-section failure. Record the hit list verbatim; it is the migration
   worklist for step 4. Any hit that is *not* an authority statement (the
   scan's regex may catch "never invent content") is classified here: regex
   narrowed, or the sentence reworded — decided per hit, written down.
4. Only now: the matrix into SKILL.md; the 14 phrases become cell references
   or are deleted (§7's table); `restructure.md` §2 heal/`link` bullets and
   §5 reference cells; `migration.md:27` parenthesis deleted. VERSION → 18.
5. `check_static.py` green. Full e2e per `evals/README.md`: all nine
   scenarios, idempotency ×3, grade; `evals/benchmarks/v18.md` against v17
   (177/177 → N/N with the new asserts counted; model floor re-measured —
   v17's was sonnet).

## 7. Prose migration and riders

| Where | Today | Becomes |
|---|---|---|
| `SKILL.md:58` Entry-document authority paragraph | ~190 words: rule, reasoning, the anti-phrasing | Deleted. The matrix row + the state rule replace it. The sentence "Never phrase this as a property of the file" is now enforced by the wall, not requested by prose. |
| `SKILL.md:42` "project-owned files are never touched anyway" | aside inside the keep rule | "(authority: kept paths × upgrade)" |
| `SKILL.md:64` Step 4 header "create it only if it does not already exist — never overwrite" | the right in full | "Right: `create-if-absent` (authority: scaffolded artifacts × every installing/maintaining mode)." |
| `SKILL.md:79, 81, 95` "create-once, project-owned after creation" in Step 4 rows | the right repeated per row | Removed from the rows; the table's row carries it once. |
| `SKILL.md:120` Step 7 "a run against an existing manifest never edits AGENTS.md" | the fourth copy from the v17 incident | "(authority: entry document × upgrade = propose-only)" |
| `SKILL.md:186` restructure: "Entry-document authority names stale wiring as the one thing…" | points at the deleted paragraph | "(authority: entry document × restructure = lossless-write — a dangling import is not owner content)" |
| `references/migration.md:27` "(Upgrade mode, which never edits AGENTS.md, proposes instead)" | an aside about another mode | Deleted — migration.md does not speak for upgrade. |
| `references/restructure.md` §2 heal, §2 link, §5 | state restructure's rights in prose | Cell references; the kept-path sentence becomes "(authority: kept paths × restructure = link-only)". |

**BL-028 residue** (key rename shipped in v17): the compound "stack profile"
at `migration.md:27` and "profile" at `SKILL.md:31, 41` → "stack". Backlog
status → `DONE in v17 (92d1e3d); prose residue closed in v18`.

**BL-030 residue** (sweep shipped in v17): "`AGENTS.md` is the canonical
constitution" at `SKILL.md:60, 151` and `restructure.md:17` → "the canonical
entry document". The remaining "constitution" uses in `skill/` name the rule
corpus and stay. Backlog status → same form as BL-028.

**BL-031** leaves v18: `backlog.md.tpl` is four lines and carries no
queue/register structure; the split concerns `docs/backlog.md` of this repo
only → docs-only, any time. Backlog status updated to say so.

**Unchanged by design:** `core/pair-development.md` (D2); the hooks plugin
write-guard (D2); README's "created once and never touched again" (README is
the operator's guide, not law — the wall does not scan it).

## 8. Risks

- **Generic authority assert over-fires on lawful behaviour.** Known cases
  are designed in (§3 cell notes): the entry-document pair as one class; heal
  as delegated upgrade. Unknown cases will show in step 5 as reds and are
  classified per POLICY (law / grader / harness / model) before being fixed —
  a grader red here is a *cell* being wrong, which is the matrix doing its
  job.
- **The wall's regex catches non-authority prose.** Handled in step 3 by
  explicit classification; the regex list is visible in the diff.
- **The vocabulary grows.** A cell that needs a sentence is a sign the eight
  values are wrong — the static check rejects a ninth value, so growth is a
  deliberate edit to the check and the spec, never a drift.
- **Scenario coverage of cells.** Some cells are never exercised (foreign
  structures × scaffold). That is acceptable: the matrix is the law; the
  grader proves the cells the corpus reaches and `selftest` proves the shape.

## 9. Done when

- `## File authority` is the only place in `skill/` stating a mode's file
  rights; `check_static.py` proves it (section 5) and went red first (§6.3).
- `grade.py` derives `protected_project_files` and `mode_respects_authority`
  from the table; `selftest:derivation` proves the derivation in both
  directions (§4).
- VERSION 18; `evals/benchmarks/v18.md` records the red baseline, the green
  corpus at 100%, idempotency ×3 zero-diff, the model floor.
- BL-028/BL-030 residue gone; backlog statuses corrected; BL-031 re-homed.
- `docs/philosophy.md` § Horizon no longer lists BL-038.
