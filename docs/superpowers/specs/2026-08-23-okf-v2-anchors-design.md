# OKF v2 and the anchor engine — design (BL-033, edition v20)

**Date:** 2026-08-23 · **Case:** BL-033 · **Edition:** v20 (VERSION 19 → 20)
· **Tier:** 2 (full) — blast radius: a new owned artifact class (executable),
the OKF law, the verification ladder, two audit checks and the fleet's whole
knowledge stratum; novelty: the first machine the constitution ships into a
repository rather than a document.

BL-033 splits in this cycle. v20 carries the docs half — anchoring made
mechanical. The SDD half (generated baseline, spec/plan linter) becomes
**BL-043** and lands as edition v21, for the reason §9 gives: it has no input
to bind to yet. The Horizon section of `docs/philosophy.md` moves its
generation-and-anchoring item from BL-033 to BL-043 when this edition closes
(`check_static.py` enforces that the section names only open cases).

## 1. Problem

The knowledge stratum is the one part of a legislated repository with no
mechanical bond to the truth it describes. The law stratum cannot rot: it is
byte-copied from the centre and byte-verified on every audit. The manifest
cannot rot: it is regenerated from the skill's own inputs. Everything under
`docs/okf/**` is hand-written prose about code, and prose about code goes
stale silently — six documents in `fleet-api` describe a model that was
removed from the codebase, and no check in the system can see it. The OKF
sync rule (`core/okf.md`) asks every session to update the affected document,
which is a duty, not a mechanism; the Stop hook reminds when `src/**` moved
without `docs/okf/**`, which catches the omission in the same session and
nothing after it.

What is missing is a *bond*: a way for a document to be provably about the
code it claims to be about, checkable by a machine, on demand, in any clone.

## 2. Decisions (brainstorm 2026-08-23)

1. **BL-033 splits into two editions, docs half first.** v20 = anchoring;
   v21 (BL-043) = baseline generator + spec/plan linter. Rationale: anchoring
   has 15 documents to bind in `fleet-obs` alone today, while the baseline
   binds `R-NNN` requirement ids to annotated tests and neither exists yet in
   any fleet repository — the SDD law shipped in v17 and the fleet is
   legislated at v16–v18. A generator with no input generates an empty file
   in nine repositories.
2. **The engine is an owned repository artifact**, delivered by Step 3 to
   `docs/ai/engine.py`, listed in `ownedFiles`, byte-verified by audit check
   3. Rejected: a machine-level plugin, and a script invoked from
   `<skill-path>`. Both break a property no rule has ever broken — every owned
   rule is repo-relative and self-contained. A law that says "run the engine"
   must name a command that works in a fresh clone on a machine that has
   never heard of the legislator, or the law stops being self-contained. The
   repo-owned engine also removes the hedge: there is no "when available".
3. **`codebase-map.md` and `index.md` are anchored, not generated** —
   correcting an assumption of deep-audit D2. The live evidence: `fleet-obs`'s
   map carries rows like "Agent-side hook assets (Claude Code: capture script
   + settings snippet; the C# mapping code lives in `src/<App>/Adapters/`)" and
   its index carries a twelve-row change-to-document mapping. The row *set* of
   both is already machine-checked (audit checks 6 and 5); the row *content*
   is judgment a generator would destroy. Generation was never the right verb
   for these two files — verification was.
4. **The anchor check is a static rung on the verification ladder**, not an
   advisory. Rejected: audit-only (rot is then caught only when a human
   remembers to audit) and a Stop-hook reminder (works on one machine, gives
   the law no rung).
5. **The `fleet-obs` pilot is a separate step after merge** — it writes to
   another repository and is not covered by this repository's benchmark, so it
   is not a gate on the edition.

## 3. The engine

**Source** `skill/assets/engine/engine.py` — the first non-markdown asset the
skill ships. **Target** `docs/ai/engine.py`, copied byte-for-byte by Step 3
alongside `opencode.json`, added to `ownedFiles`.

- One file, standard library only, `python3`.
- Invocation `python3 docs/ai/engine.py <job>`.
- Exit codes: `0` clean, `1` findings, `2` usage error.
- One finding per line, the shape fixed here so the grader and the audit
  report can both consume it. Anchor findings carry a line number; a debt
  finding is about a whole document and carries none:

  ```
  docs/okf/silver.md:42: path-anchor: src/Billing/Gone/Old.cs → no such file
  docs/okf/audit.md:17: symbol-anchor: LegacyProcessor → not found in adapters/, src/, tests/
  docs/okf/registry.md: okf-sync-debt: registry/registry.yaml changed 47 days after this document
  ```

- **Source roots are derived, never configured**: every non-hidden top-level
  directory except `docs/` and check 6's ignore list (`bin/`, `obj/`,
  `node_modules/`, `dist/`). A configuration key would rot exactly like the
  prose it is meant to police.
- **Neither v20 job writes anything.** The engine is read-only this edition,
  so it introduces no idempotency surface at all; the first writing job is
  v21's baseline generator.

**Accepted limitation:** every legislated repository now depends on `python3`
for its static rung, including the dotnet ones. The hooks plugin already
assumes it on the machine; this extends the assumption to CI.

### Job `anchors`

Scans `docs/okf/**/*.md` except the two `human`-class files (§5). Applies the
anchor definition of §4 to every backticked token and reports every one that
does not resolve.

### Job `okf-debt`

Re-uses the anchors: a document's path-anchors mechanically define which
source files it is about. A document is **in debt** when an anchored source
file has a commit more than 30 days newer than the document's own newest
commit.

Two details carry the design:

- The comparison uses the document's **git date, not its front-matter
  `timestamp`**. A hand-written timestamp is hand-maintained truth, and this
  whole case exists because hand-maintained truth rots.
- The 30-day threshold is check 8's (journal recency), reused rather than
  invented — `core/artifact-lifecycle.md` forbids a threshold restated as an
  independent constant.

## 4. The anchor definition

The definition is law and lives in `core/okf.md`; the engine is its executing
arm and the only place it is implemented. It is closed, and it needs no
per-repository configuration.

A backticked token inside an `anchored` document is an **anchor** when it
carries no space, none of `<`, `>`, `*`, `?`, and does not start with `~` or
`/`. An anchor is:

- a **path-anchor** when it contains `/` and its first segment is an existing
  top-level directory. It resolves when the path exists. A trailing member
  suffix (`.Member()`) is stripped before the test when the remainder plus a
  source-file extension exists.
- a **symbol-anchor** when it is PascalCase of at least four characters. It
  resolves when it occurs literally anywhere under the source roots.

Everything else a document backticks — commands, JSON field names, prose
emphasis, lowercase identifiers, placeholder templates — is not an anchor and
is never reported.

**Measured against `fleet-obs`'s 15 OKF documents (666 backticked tokens):**
71 path-anchors, 66 resolve; 76 symbol-anchors, 75 resolve. All six
non-resolving cases are accounted for by the definition above rather than by
an ignore list: four are angle-bracket templates
(`schemas/<type>/<version>.json`), one is a member suffix
(a path with a trailing `.Member()` suffix), and the last —
`VaultGitJob` — sits in `log.md`, which is `human` class precisely because a
chronicle legitimately names what has since been removed. The healthy-repo
false-positive rate after the definition is zero, which is what
`core/artifact-lifecycle.md` demands of any surfaced worklist: "a class of
items that systematically yields no action is excluded mechanically".

**Deliberately coarse:** a symbol-anchor asks whether the identifier occurs in
the source, not whether the declaration it names still has that shape. A
renamed method inside a surviving class still resolves. The alternative —
language-aware resolution — needs a parser per stack and buys precision this
case does not need: the rot it must catch is the removed concept, and a
removed concept's name disappears from the tree.

## 5. OKF v2 — three classes in `core/okf.md`

The amendment classifies the bundle by link hardness and states one new duty.

| Class | Files | Bond |
|---|---|---|
| anchored | `index.md`, `codebase-map.md`, every concept document | every path and symbol it backticks resolves in this repository |
| human | `glossary.md`, `log.md` | none — a glossary defines terms, a log records what was true then |
| generated | (none in v20) | written by a machine from a source it mirrors — populated in v21 |

The `generated` row is declared and empty this edition. That is honest: no
artifact in v20 is machine-written, and `core/artifact-lifecycle.md` is
therefore **not amended** — anchoring is not a fourth role. The three roles
answer "when does this artifact die"; anchoring answers "what is it bonded
to". A property of a reference artifact belongs in the rule that owns
reference artifacts, which is `core/okf.md`.

## 6. Verification ladder

`core/verification.md` gains a static rung: before "done", `python3
docs/ai/engine.py anchors` is clean, alongside zero build errors and green
tests.

No "when available" clause. Because the engine is an owned repository file,
every repository at v20 or above has it by construction, and a repository
below v20 is reading the previous `verification.md`, which has no rung. The
conditional phrasing in `core/sdd.md`'s analyze gate stays as written — it
governs the linter, which is v21's.

## 7. SKILL.md

- **Step 3.1** copies the engine the same way it copies `opencode.json`:
  byte-for-byte Bash `cp`, never Write or Edit.
- **Step 3.4** — `ownedFiles` gains `docs/ai/engine.py`. The manifest's pinned
  serialization is unchanged; the new path sorts ahead of `docs/ai/rules/…`.
- **File authority** — the owned-law row's class gains `docs/ai/engine.py`
  beside `docs/ai/rules/**` and `opencode.json`. Rights are unchanged and the
  table keeps its pinned 8 × 5 shape.
- **Audit check 3** gains its third comparison: `docs/ai/engine.py` against
  `<skill-path>/assets/engine/engine.py`.
- **Audit check 15 `okf-anchors` (Warning)** — run the `anchors` job, report
  each finding in the audit's line format. When `docs/ai/engine.py` is absent
  (a repository below v20), skip the check and say so in Info; check 4 already
  reports the staleness that caused it. Skip silently when `docs/okf/` is
  absent — there is nothing to anchor.
- **Audit check 17 `okf-sync-debt` (Warning)** — run the `okf-debt` job, same
  treatment. (15 was never used; 16 exists. The two new checks take 15 and
  17, closing the gap. Slugs are the identity — the numbers are ordinals.)
- **Restructure** — both new findings are owner prose, not wiring. The closed
  scope of `fix` already forbids touching them, so they route to the
  `## For the team:` section with the documents byte-unchanged, exactly as
  check-14's findings do. Stated explicitly in the restructure section so the
  routing is law rather than inference.
- Audit's zero-writes contract is unaffected: both jobs are read-only, and the
  `git status` / `HEAD` comparison around the run proves it per run.

## 8. Evals first — the red baseline

Per `evals/POLICY.md`, every new assert is committed in its red state against
the unchanged law before the change that greens it — the habit BL-041
established (`d732b57` → `a8584aa`).

- **`evals/check_engine.py`** — deterministic unit tests of the engine, no
  agent, on the `check_hooks.py` pattern: the six token classes, each
  exclusion (`<`/`>`, glob, `~`, absolute), the `.Member()` suffix rule, the
  two exempt files, source-root derivation, the three exit codes, and the
  30-day threshold with a fixture git history.
- **`check_static.py`** — the engine exists under `assets/engine/`, the File
  authority row names it, and the derived `ownedFiles` set includes it.
- **Rotted fixture** — two planted defects: a concept document naming a
  `src/…` path that does not exist and a PascalCase symbol found nowhere in
  the source roots; and a second document whose anchored source file carries a
  commit 40 days newer than the document's own. Without them `grade.py`'s slug
  parity goes red by construction — every law check must be exercised by a
  planted defect and every planted defect by a law check.
- **Scenario asserts** — audit reports both slugs at Warning; restructure
  routes both to `## For the team:` and leaves both documents byte-identical;
  fresh-scaffold and upgrade find `docs/ai/engine.py` present, byte-identical
  to source, and listed in `ownedFiles`; idempotency stays zero-diff (the
  engine writes nothing, so a regression here would mean a delivery bug).
- **Plugin** — `guard_owned_files.py` extends its block to
  `docs/ai/engine.py`, with cases in `check_hooks.py`. A `plugin/`-only change
  needs no e2e benchmark (BL-007's precedent), but it ships in this cycle
  because the engine is otherwise the one owned file a stray `Edit` can drift.

## 9. Out of scope

**BL-043 (edition v21)** takes: the baseline generator (`R-NNN` ↔ annotated
tests → `docs/ai/baseline.md`), the spec/plan linter (dangling `R-NNN`,
coverage R↔task, unresolved placeholders) and its binding in `core/sdd.md`'s
analyze gate, the population of the `generated` role class in
`core/artifact-lifecycle.md`, and `fleet-obs`'s registry work — a `generated`
content-type and its exclusion from the gold-panel docs-vs-code metric. That
last item follows the others automatically: with no generated artifact landing
in v20, there is nothing yet for the panel to miscount.

**The `fleet-obs` pilot and the fleet sweep** run after this edition merges.
The sweep moves the fleet from v16/v18 to v20 in one cumulative pass — v19 was
deliberately not swept.

## 10. Repository bookkeeping

- `docs/ontology.md` §2 — the `generated` entry drops `codebase-map` and
  `index`; **anchored** is introduced as a term under §3's naming rules.
- `docs/glossary.md` — the `generated` row loses the two OKF halves; a new
  `anchored` row; the `baseline` row's case reference moves to BL-043.
- `docs/philosophy.md` §Horizon — the generation-and-anchoring item is
  rewritten to name BL-043 and to say that anchoring landed in v20.
- `docs/backlog.md` — BL-033 rewritten to its actual scope and marked done at
  v20; BL-043 filed; the edition plan updated (v20 = BL-033, v21 = BL-043,
  BL-034 after v21 since its dependency is OKF v2 *and* the generated class).
- `skill/VERSION` 19 → 20.

## 11. Risks

- **A repository with real anchor debt cannot report "done" until it is
  clean.** The measurement says `fleet-obs` has none, but `fleet-api` is the
  case that motivated this design and may have many. Mitigation: the debt is
  surfaced by audit before the rung ever bites, because the sweep runs an
  upgrade (which reports Health) before any task runs under the new ladder.
  If a repository turns out to carry unpayable debt, the honest answer is a
  restructure pass there, not a weaker rung.
- **A symbol-anchor is coarse** (§4) — accepted deliberately, documented in
  the law so no reader mistakes it for a declaration check.
- **`python3` in CI** — accepted, stated in §3.
- **Executable content in the owned layer** is new. It is guarded the same way
  law is (write-guard + byte-diff), and it is the smallest possible surface:
  one file, no dependencies, no writes.

## 12. Done when

- `python3 docs/ai/engine.py anchors` and `okf-debt` run in a legislated
  repository, exit 0 clean and 1 with findings, and write nothing.
- `check_engine.py`, `check_hooks.py`, `check_static.py` green.
- The corpus is 100% with the new asserts, and every new assert was seen red
  first, in a commit that precedes its fix.
- Idempotency ×3 zero-diff on fresh, upgrade and restructure.
- The model floor is recorded in `evals/benchmarks/v20.md`.
- `docs/philosophy.md` §Horizon names BL-043, not BL-033, and
  `check_static.py` agrees.
