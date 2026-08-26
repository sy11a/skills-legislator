# BL-047 — The decision inventory (law @ v22)

Classified 2026-08-26 against `skill/assets/rules/**` @ v22 and `skill/SKILL.md`
@ v22. Unit: the normative clause (see `spec.md` — Method). Buckets:

- **(a) enforced** — a deterministic arm adjudicates it where the decision is
  taken: `engine.py` (`anchors`, `okf-debt`, `sdd-lint`, `baseline`), the hooks
  (`guard_owned_files.py`, `okf_sync_check.py`, `format_on_edit.py`), or the
  opencode guard. Corpus asserts are falsifiability, not enforcement
  (clarified in spec).
- **(b) enforceable** — a nameable check could adjudicate it; cost S/M/L;
  cadence = the event the clause binds to (`every-edit` > `every-commit` >
  `every-task` > `every-run` — "run" meaning a legislator invocation).
- **(c) interpretation** — no mechanical test can say whether a diff obeys it.

A clause whose *presence* is checkable but whose *substance* is judgement is
split into two rows where that matters; otherwise it sits in (b) with a
"presence only" note. Unclassifiable → (c) by the cheap-error rule.

## Core law — `skill/assets/rules/core/`

### okf.md (13 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| okf-1 | an OKF update accompanies every concept-touching change (presence) | a | `okf_sync_check.py` (Stop reminder, src/ vs docs/okf/) + engine `okf-debt` (30-day out-of-cycle net) | — | — |
| okf-2 | the OKF update is *correct* — properties, decisions, prose match the code | c | — | — | — |
| okf-3 | new concepts get a document created | c | concept identification is judgement | — | — |
| okf-4 | changed concepts' docs track paths/symbols | a | engine `anchors` (path- and symbol-anchors resolve) | — | — |
| okf-5 | `status` field holds a closed-set value, flipped on state change | b | engine lint: front-matter `status` ∈ closed set | S | every-task |
| okf-6 | `timestamp` updated to today on change | b | engine lint: doc changed in diff ⇒ timestamp = today | S | every-task |
| okf-7 | cross-links added where relevant | c | "relevant" is judgement | — | — |
| okf-8 | new/renamed domain terms get a glossary row | c | term identification is judgement | — | — |
| okf-9 | `log.md` gets an entry per change | b | engine lint: OKF diff ⇒ log.md gained a dated entry | S | every-task |
| okf-10 | anchor definition (closed): what counts, how it resolves | a | engine `anchors` executes the definition | — | — |
| okf-11 | `anchors` exits clean before "done" | a | engine `anchors` (the rung in verification.md) | — | — |
| okf-12 | `okf-debt` names 30-day-stale anchored docs | a | engine `okf-debt` | — | — |
| okf-13 | repair is an owner update, never an automatic rewrite | c | a prohibition on process, not on a diff | — | — |

### pair-development.md (9 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| pair-1 | work one task at a time | c | — | — | — |
| pair-2 | each task gets its own branch, named per convention | b | branch-name lint (session hook or git hook) against the repo's declared pattern | S | every-task |
| pair-3 | task not done until merged or explicitly parked | c | "parked = recorded decision" is semantic | — | — |
| pair-4 | never cut a new task branch while an unmerged, unparked one exists | b | git check: enumerate unmerged `bl/`-branches at branch creation; needs a parked-register to be exact | M | every-task |
| pair-5 | cut branches from freshly-pulled main, never a stale checkout | b | hook: compare fork-point with `origin/<main>` at branch creation | S | every-task |
| pair-6 | never merge to the main branch yourself | b | PreToolUse git-guard blocking `merge`/`push` onto main (git-guardrails pattern) | S | every-commit |
| pair-7 | no AI attribution anywhere in the VCS record | b | commit-msg / PR-body grep guard (`Co-Authored-By`, "Generated with") | S | every-commit |
| pair-8 | no next task without explicit user approval | c | — | — | — |
| pair-9 | never hand-edit `docs/ai/rules/**` or `docs/ai/manifest.json` | a | `guard_owned_files.py` + opencode guard (rules, engine, opencode.json; the manifest is deliberately unguarded — Step 3.7 regeneration heals it) | — | — |

### decision-gate.md (7 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| gate-1 | stop, describe, wait on any listed trigger | c | — | — | — |
| gate-2 | trigger: trade-off with no obvious right answer | c | — | — | — |
| gate-3 | trigger: "could be done later / simplified for now" | c | — | — | — |
| gate-4 | trigger: divergence from the task description | c | — | — | — |
| gate-5 | trigger: correctness/security/architecture concern | c | — | — | — |
| gate-6 | trigger: unexpected code contradicting the plan | c | — | — | — |
| gate-7 | trigger: migration deleting data without backup | c | trigger recognition is judgement; a destructive-migration lint (DROP/DELETE ops) could pre-flag candidates | — | — |

### adr.md (7 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| adr-1 | record an ADR on the three trigger classes | c | trigger recognition | — | — |
| adr-2 | file `NNNN-kebab-title.md`, numbered sequentially from 0001 | b | engine lint: filename shape + gapless sequence | S | every-task |
| adr-3 | template sections Status/Context/Decision/Consequences present | b | engine lint: heading presence | S | every-task |
| adr-4 | status ∈ {proposed, accepted, deprecated, superseded by NNNN} | b | engine lint: closed-set value | S | every-task |
| adr-5 | never renumber or delete a past ADR | b | git check: no deletion/rename of `docs/adr/NNNN-*` in history since last check | S | every-commit |
| adr-6 | ADR written in the same task as the decision | c | "same task" is a semantic boundary | — | — |
| adr-7 | ADR linked from the affected OKF document(s) | b | engine lint: each ADR referenced from ≥1 OKF doc | M | every-task |

### dev-journal.md (5 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| jrnl-1 | one file per working day, `YYYY-MM-DD.md` | b | engine lint: filename shape under docs/journal/ | S | every-task |
| jrnl-2 | entry covers work, decisions, dead ends, open questions | c | content substance | — | — |
| jrnl-3 | append at task boundaries | b | recency check (audit check 8 is the 30-day model-run proxy; an engine port makes it deterministic) | S | every-task |
| jrnl-4 | summary per unit of work, not a line per tool call | c | — | — | — |
| jrnl-5 | day file created per README format | b | engine lint: header shape | S | every-task |

### changelog.md (4 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| chlog-1 | CHANGELOG.md follows Keep-a-Changelog structure | b | engine lint: section headings | S | every-task |
| chlog-2 | the task-finishing commit adds its `[Unreleased]` line | b | engine/commit check: case-closing diff touches CHANGELOG.md | M | every-task |
| chlog-3 | entries move under a version heading only at a user-cut release | c | the release is a user act | — | — |
| chlog-4 | entries state user-visible effect, not implementation detail | c | — | — | — |

### artifact-lifecycle.md (9 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| life-1 | every artifact's role declared at creation (reference/lifecycle/generated) | c | classification at creation | — | — |
| life-2 | lifecycle artifacts live only in their conventional homes | b | placement check (audit check 16 is the model-run variant; engine port) | M | every-task |
| life-3 | generated artifacts never hand-edited; regenerated on demand | b | engine: regenerate `baseline` and diff against the committed file | S | every-commit |
| life-4 | completed lifecycle artifacts never rewritten | b | git check: converged `docs/cases/**` files immutable | M | every-commit |
| life-5 | reference artifacts die by review, never silently | c | — | — | — |
| life-6 | worklists carry only actionable items; noise excluded mechanically | c | — | — | — |
| life-7 | alert thresholds derive from the declared cadence | c | — | — | — |
| life-8 | unknown classification fails toward the cheap error | c | — | — | — |
| life-9 | no silent caps — truncation states what was withheld | c | a property of report prose | — | — |

### project-rules.md (8 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| prule-1 | project rules live in `.claude/rules/`, one topic per file | b | placement lint | S | every-task |
| prule-2 | rules are law-shaped: short, imperative, diff-checkable | c | law-shapedness is the model classifier (BL-048's subject) | — | — |
| prule-3 | partial-tree rules scoped with `paths:` frontmatter | b | frontmatter syntax lint | S | every-task |
| prule-4 | never put project rules in `docs/ai/rules/**` | a | `guard_owned_files.py` + opencode guard block the write | — | — |
| prule-5 | never inline project rules into CLAUDE.md's body | c | detecting law-shaped prose = model classification | — | — |
| prule-6 | keep each rule file under ~30 lines | b | line-count lint | S | every-task |
| prule-7 | the two sanctioned instance-data homes are not restructure targets | b | allowlist in restructure/audit logic | S | every-run |
| prule-8 | generalizable rules are constitution candidates | c | generalizability is judgement | — | — |

### sdd.md (29 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| sdd-1 | every unit of work is a case `BL-NNN` under this law | b | branch ↔ `docs/cases/BL-NNN-*/` correspondence check | M | every-task |
| sdd-2 | one case, one home; `docs/superpowers/**` never rewritten/moved | b | placement + immutability check (with life-2/life-4) | M | every-task |
| sdd-3 | tier declared in the case header | b | `sdd-lint` extension: header grep | S | every-task |
| sdd-4 | tier chosen on blast radius × novelty | c | — | — | — |
| sdd-5 | converge may raise a tier | c | — | — | — |
| sdd-6 | cross-repo cases live once, reference rows in siblings | c | — | — | — |
| sdd-7 | spec type declared in the header | b | `sdd-lint` extension: header grep | S | every-task |
| sdd-8 | bugfix spec has current/expected/unchanged sections (presence) | b | `sdd-lint` extension: section presence when type=bugfix | S | every-task |
| sdd-9 | boundary stated: in-scope and out-of-scope | b | `sdd-lint` extension: section presence | S | every-task |
| sdd-10 | one intent per spec | c | "and also" is a signal, not a test | — | — |
| sdd-11 | requirements are EARS lines: one `R-NNN`, one SHALL each | b | `sdd-lint` extension: shape lint (id + single SHALL per line) | M | every-task |
| sdd-12 | ids are permanent | b | git check: no `R-NNN` renumbering in spec history | S | every-commit |
| sdd-13 | every spec ships ≥1 named GIVEN/WHEN/THEN hurting case | b | `sdd-lint` extension: scenario presence | S | every-task |
| sdd-14 | every THEN/SHALL response is observable | c | observability is judgement | — | — |
| sdd-15 | clarify session recorded in `## Clarifications` (dated, Q→A) | b | `sdd-lint` extension: section presence for tier ≥1 | S | every-task |
| sdd-16 | plan is a package in the case file; split past ~10–15 KB | b | size check | S | every-task |
| sdd-17 | one task = one session | c | — | — | — |
| sdd-18 | `[P]` only on file-disjoint tasks | b | verify disjointness from tasks' named files | M | every-task |
| sdd-19 | every task traces `per R-NNN` | a | engine `sdd-lint` (coverage R↔task, dangling refs) | — | — |
| sdd-20 | decisions that outlive the case → ADR; local → research.md | c | — | — | — |
| sdd-21 | analyze judges reuse-first and over-engineering | c | — | — | — |
| sdd-22 | the mechanical analyze passes run via `sdd-lint` | a | engine `sdd-lint` (the law names the arm) | — | — |
| sdd-23 | findings are proposals; the human decides | c | — | — | — |
| sdd-24 | converge judges code against every promise, never against diffs | c | the core model-judgement act | — | — |
| sdd-25 | gaps classified missing/partial/contradicts/unrequested | c | — | — | — |
| sdd-26 | findings appended as traceable tasks, append-only | b | git check: converge edits to plan.md are append-only | M | every-task |
| sdd-27 | constitutional violations are CRITICAL | c | — | — | — |
| sdd-28 | converge checks OKF docs anchored to touched files; stale prose is a finding | c | anchors/okf-debt catch rot (a-partial); prose staleness is judgement | — | — |
| sdd-29 | a case closes only on "✅ Converged" | b | marker grep at case close / branch merge | S | every-task |

### skills.md (7 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| skl-1 | law beats skills; perform the lawful form of the step | c | — | — | — |
| skl-2 | skill outputs redirect to constitutional homes | c | recognition of a skill's output class; a foreign-file write-block (CONTEXT.md etc.) could catch the worst | — | — |
| skl-3 | no skill may commit/push/merge/file issues on its own authority | b | git-guard hook (same arm as pair-6) | S | every-commit |
| skl-4 | setup skills stop at the decision gate (legislator scaffold excepted) | c | — | — | — |
| skl-5 | per-repo skill law consulted before overlapping invocation | c | — | — | — |
| skl-6 | stage routing consulted at stage boundaries | c | — | — | — |
| skl-7 | absent `.claude/rules/skills.md` is never an error | c | a definition of absence semantics | — | — |

### verification.md (12 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| ver-1 | new behavior ships with tests at the boundary where it lives | c | boundary choice is judgement (test presence is lintable, adequacy is not) | — | — |
| ver-2 | UI flows verified by driving the real app via configured tooling | c | execution obligation; bindings presence is checkable | — | — |
| ver-3 | mock at the boundary, never what you own | b | analyzer: flag mocks/fakes of types declared in this solution | M | every-commit |
| ver-4 | direct DB access during verification is read-only | c | — | — | — |
| ver-5 | no inter-test order dependence | b | randomized-order test runs | M | every-commit |
| ver-6 | no sleeps as synchronization | b | banned-API lint in test sources | S | every-commit |
| ver-7 | deterministic data and seeds | c | — | — | — |
| ver-8 | engine `anchors` exits clean before "done" | a | engine `anchors` | — | — |
| ver-9 | gate: zero build errors, zero new warnings, tests green | b | the repo's own build/CI adjudicates; legislator's part is that the gate exists | S | every-commit |
| ver-10 | failures reported verbatim, never paraphrased away | c | — | — | — |
| ver-11 | concrete bindings live in `.claude/rules/verification.md`, consulted first | c | consulting is behavior; file presence is checkable | — | — |
| ver-12 | absent `python3` is a gap to close, never a licence | c | — | — | — |

## Stack law — `skill/assets/rules/stacks/`

### dotnet/coding-standards.md (12 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| cs-1 | meaningful names, never single-letter/abbreviated | b | analyzer/lint for the single-letter half; semantics stay judgement | S | every-edit |
| cs-2 | explicit types over `var` for built-ins | b | IDE0008 as error via .editorconfig (the law itself defers there); `format_on_edit.py` applies but does not gate | S | every-edit |
| cs-3 | braces on all `if` statements | b | IDE0011 as error | S | every-edit |
| cs-4 | no alignment-padding around `=` | b | formatter setting | S | every-edit |
| cs-5 | no comments unless the WHY is non-obvious | c | — | — | — |
| cs-6 | no empty catch / swallowed exceptions | b | analyzer (e.g. empty-block + catch rules) | S | every-edit |
| cs-7 | no fire-and-forget async | b | CS4014 + discard-pattern lint | M | every-edit |
| cs-8 | no sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) | b | banned-API analyzer | S | every-edit |
| cs-9 | no `Thread.Sleep`/`Task.Delay` as synchronization | b | banned-API analyzer in prod+test code | S | every-edit |
| cs-10 | async DB/external calls take and forward `CancellationToken` | b | CA2016 + forwarding analyzer | M | every-edit |
| cs-11 | no dead code | b | IDE0051/0052/0005 as errors | M | every-edit |
| cs-12 | warnings-as-errors where the project enables it | b | msbuild property check | S | every-commit |

### dotnet/architecture.md (8 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| arch-1 | `Domain` has zero NuGet dependencies | b | msbuild/graph check per build | S | every-commit |
| arch-2 | strict layer reference graph | b | architecture-test (NetArchTest-style) in CI | M | every-commit |
| arch-3 | business logic never touches `HttpContext` directly | b | banned-API by layer | M | every-commit |
| arch-4 | all AI/LLM provider calls via one abstraction | b | banned-API (provider SDK types outside the adapter) | M | every-commit |
| arch-5 | mandatory tenant filter on every tenant-scoped entity | b | EF model reflection test enumerating global query filters | L | every-commit |
| arch-6 | constructor injection only; no service locator (one scoped exception) | b | banned-API + the exception makes edge cases reviewable | M | every-commit |
| arch-7 | no static mutable state | b | analyzer (static mutable field detection) | M | every-commit |
| arch-8 | no captive dependencies | b | `ValidateScopes`/`ValidateOnBuild` at composition root | S | every-commit |

### dotnet/data-access.md (6 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| da-1 | no N+1 query patterns | b | EF interceptor/heuristic in integration tests; inherently approximate | L | every-commit |
| da-2 | `AsNoTracking()` on read-only queries | b | analyzer/heuristic on query sites | M | every-commit |
| da-3 | no unbounded result sets | b | analyzer: bare `ToListAsync()` without `Take`/pagination | M | every-commit |
| da-4 | no `SaveChangesAsync()` inside a loop | b | syntax analyzer | M | every-commit |
| da-5 | raw SQL only through parameterized forms | b | banned-API: string-built SQL into `FromSqlRaw`/`ExecuteSqlRaw` | S | every-commit |
| da-6 | deterministic disposal of connection-holding resources | b | CA2000-class analyzers | M | every-commit |

### aurelia/coding-standards.md (2 clauses)

| id | clause | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| au-1 | follow the existing component file layout | c | "existing patterns" is repo-relative judgement | — | — |
| au-2 | keep the file thin; expand via the normal update loop | c | process guidance | — | — |

## SKILL.md — the procedure's decision points (38 points)

Each row is a place the executing model decides. Bucket (b) here means the
decision is mechanically derivable from the repo and the skill package — code
could take it; "human" in the evidence column marks the part that is owner
input by design (never a model decision, never a code decision).

| id | decision point | bucket | evidence / proposed check | cost | cadence |
|---|---|---|---|---|---|
| sk-1 | Step 0: "looks like a project root" | b | `.git` presence test | S | every-run |
| sk-2 | Step 0: dirty-tree warning | b | `git status --porcelain` (asking is human) | S | every-run |
| sk-3 | Step 1: mode detection (upgrade/migration/fresh) | b | file-presence decision tree, fully specified | S | every-run |
| sk-4 | Step 1 edge case: reconstruct subscription/ownedFiles from disk | b | directory enumeration (confirmation is human) | M | every-run |
| sk-5 | Step 2: stack-signal detection | b | glob rules, fully specified (confirmation is human) | S | every-run |
| sk-6 | Step 3: byte-for-byte copy of owned files | b | engine `apply` job (cp is already mandated; the decision *which* files is mechanical) | M | every-run |
| sk-7 | Step 3: `ownedFiles` computation | b | same engine job | S | every-run |
| sk-8 | Step 3: deletions diff + empty-dir cleanup | b | same engine job | S | every-run |
| sk-9 | Step 3: keep-list carry, dedupe, refusal rules | b | closed rules; parsing the user's keep *request* stays model | M | every-run |
| sk-10 | Step 3: manifest serialization, byte-stable | b | engine writer (2-space, key order, inline arrays — a serializer spec) | S | every-run |
| sk-11 | Step 3: v14 file-model canonicalization (rename + symlink) | b | scripted rename/symlink logic | S | every-run |
| sk-12 | Step 4: create-if-absent scaffolding of verbatim templates | b | file-presence + copy | S | every-run |
| sk-13 | Step 4: mechanical placeholders (`{{TODAY_ISO}}`, `{{STACK_IMPORTS}}`, `{{STACK_SUMMARY}}`) | b | derivation rules are closed formulas | S | every-run |
| sk-14 | Step 4: owner placeholders (`{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, `{{PROJECT_ARCHITECTURE_NOTES}}`, `{{BUILD_TEST_COMMANDS}}`, `{{CATEGORY_MAPPING_TABLE}}`) | c | asked of the user by design (human input; detection heuristics assist) | — | — |
| sk-15 | Step 4: `{{CODEBASE_MAP_TABLE}}` — one-line directory descriptions | c | tree listing is mechanical; describing a directory is prose judgement | — | — |
| sk-16 | Step 4: `{{GLOSSARY_TABLE}}` — domain-term derivation | c | term extraction is the model-classification job (BL-048) | — | — |
| sk-17 | Step 4: `{{SANCTIONED_SKILLS_BY_STAGE}}` — installed detection | b | directory/symlink enumeration; stack-relevance judgement stays model | M | every-run |
| sk-18 | Step 4: `{{BOUNDARIES}}` — no-touch candidate detection | b | glob candidates; repo-specific additions are human | S | every-run |
| sk-19 | Step 5: three-way split of the legacy entry document | c | law-shaped carving — the flagship interpretation job | — | — |
| sk-20 | Step 5: category-table extraction and equivalence test | c | "equivalent table" is semantic | — | — |
| sk-21 | Step 5: plans/specs relocation + reference fixing | b | move + link rewrite, mechanically specified | M | every-run |
| sk-22 | Step 5: conflict with an owned rule → surface, never resolve | c | contradiction detection is semantic (same class as audit check 11) | — | — |
| sk-23 | Step 6: byte-verify every owned file against source | b | diff loop — engine `verify` job | S | every-run |
| sk-24 | Step 6: confirm every Step 4 artifact exists | b | file-presence loop | S | every-run |
| sk-25 | Step 7: report skeleton + Created/Overwritten/Deleted/Keep sections | b | derivable from the run's own facts — the report emitter (BL-049's question) | M | every-run |
| sk-26 | Step 7: `Needs your review` proposed lines | b | import-delta lines are derivable; free prose stays model | M | every-run |
| sk-27 | Step 7/audit: constitution-candidates scan (3 tests) | c | law-shaped + covered-by-meaning + generalizable — model classification (BL-048); the suppression-marker skip is mechanical | — | — |
| sk-28 | Audit checks 1–6 (imports, placeholders, owned-integrity, staleness, index links, codebase map) | b | pure file/diff/glob logic — engine `audit` job | M | every-run |
| sk-29 | Audit check 7 (orphan docs) — closed "referenced" definition | b | same engine job | M | every-run |
| sk-30 | Audit check 8 (journal recency) | b | git dates | S | every-run |
| sk-31 | Audit check 9 (foreign structures) — the listing | b | glob list; the law-shaped escalation to Warning stays model | S | every-run |
| sk-32 | Audit check 10 (keep-list integrity) | b | same engine job | M | every-run |
| sk-33 | Audit checks 11–12 (project-rule contradictions, stray rulebooks) | c | contradiction detection and predominant-law-shapedness are model classification | — | — |
| sk-34 | Audit checks 13–14 (glossary vitality, skill bindings) | b | row-count + install-path enumeration | S | every-run |
| sk-35 | Audit checks 15/17 wrappers (run engine, read exit code) | a | engine `anchors`/`okf-debt` are the check; the wrapper is trivially scriptable | — | — |
| sk-36 | Audit check 16 (legacy-home violation) | b | first-commit dates vs legislation date | M | every-run |
| sk-37 | Restructure: findings → plan items in the closed action set | c | mostly rule-driven, but `decision` items and merge/move judgement are the point of the mode | — | — |
| sk-38 | Restructure: fidelity pass — every moved line still greppable | b | mechanical grep over the moved/merged line set | M | every-run |

## Totals

Recount rule: each table row above is one clause/point; totals below are
`grep`-derived from this file (rows per bucket letter in the bucket column).

| source | clauses | (a) | (b) | (c) |
|---|---|---|---|---|
| core rules (11 files) | 110 | 10 | 44 | 56 |
| stack rules (4 files) | 28 | 0 | 25 | 3 |
| SKILL.md decision points | 38 | 1 | 28 | 9 |
| **total** | **176** | **11** | **97** | **68** |

Source-line framing (the backlog's "217 lines"): core 222 lines, stacks 44
lines, SKILL.md 219 lines @ v22.

## The ranked bucket-(b) list

Grouped by cadence (how often the decision is taken), highest first. Within a
group, cheapest first. Costs: S = a session, M = a case, L = its own edition.

### every-edit / every-commit — fires constantly, across the whole fleet

1. **Git conduct guard** (pair-6, pair-7, skl-3; + adr-5, sdd-12, life-3,
   life-4 ride along) — a PreToolUse git-guard + commit-msg check: block
   merge/push to main, block AI attribution, protect ADR/case/baseline
   immutability. Cost **S–M**. The highest-frequency unenforced law in the
   constitution.
2. **Dotnet analyzer binding** (cs-1…cs-4, cs-6…cs-12, arch-1…arch-8, da-1…da-6 —
   25 clauses, 89% of stack law) — not engine growth: wire the *existing*
   analyzer/editorconfig/architecture-test ecosystem into legislated repos and
   let the repo's own build adjudicate. Cost **M** to specify the binding;
   the checks themselves are off-the-shelf. Biggest single bucket-(b) mass.

### every-task — fires once per case, fleet-wide

3. **sdd-lint extensions** (sdd-3, 7, 8, 9, 11, 13, 15, 16, 29; okf-5, okf-6,
   okf-9; adr-2, 3, 4, 7; jrnl-1, 3, 5; chlog-1, 2) — header/section/shape
   lints over the case file, OKF front matter, ADRs, journal, changelog: ~20
   clauses into the engine's existing `sdd-lint` frame. Cost **S–M** each;
   the frame already exists.
4. **Placement & immutability job** (life-2, life-4, sdd-1, sdd-2, sdd-26) —
   engine port of the audit-16 idea plus append-only converge edits. Cost **M**.

### every-run — fires once per legislator invocation, but the run is the product

5. **Engine `audit` job** (sk-28…sk-32, sk-34, sk-36 — 13 of the 17 audit
   checks) — the checks are file/diff/glob/date logic already specified to
   mechanical precision; only 11, 12, and the candidates scan genuinely need a
   model. Cost **M**. Removes the largest block of model-executed mechanics
   in SKILL.md.
6. **Engine `apply`/`verify` jobs** (sk-6…sk-13, sk-23, sk-24) — Step 3's
   copy/diff/manifest mechanics and Step 6's byte-verify as engine commands
   the model invokes instead of performs. Cost **M–L** (the manifest
   serializer alone is S and worth taking first).
7. **Report emitter** (sk-25, sk-26) — print the Step 7 skeleton and the
   fact-derived sections from the run's own record; the model fills only the
   prose seams. Cost **M**. This is BL-049's question — that spike measures
   the win before this is built.
8. **Fidelity-pass job** (sk-38) — the restructure guarantee as a grep job.
   Cost **M**.

## The answer

**The law is mostly enforceable; the procedure is mostly mechanical; the
genuinely interpretive core is small and coherent.**

- **11 of 176** units are already deterministically enforced (bucket a) —
  the engine and hooks cover anchor resolution, OKF sync presence, owned-file
  write-protection, and R↔task traceability. That is the entire deterministic
  surface as of v22.
- **97 of 176** are bucket (b): more than half of everything a model currently
  decides could be adjudicated by code that does not exist yet. Three
  concentrations stand out: the stack law (25/28 clauses — delegable to
  off-the-shelf analyzers, not engine code), the SKILL.md run mechanics
  (28/38 points — the engine-growth path: audit, apply/verify, report
  emitter), and the case-shape lints (~20 clauses over the existing
  `sdd-lint` frame).
- **68 of 176** are bucket (c). But the list is not amorphous: it decomposes
  into (1) **classification** — law-shapedness, contradiction detection, term
  extraction (exactly the jobs BL-048 asks whether a small model can take);
  (2) **prose judgement** — converge, decision-gate triggers, ADR triggers,
  report prose, OKF substance; (3) **owner input** — placeholders and
  confirmations that are human by design, not model residue. Category (2) is
  the irreducible model core; it is also precisely what the eval corpus
  measures.
- **On "is this law or advice":** no core clause was found that *nobody* can
  adjudicate — every (c) clause is adjudicable by a competent reviewer
  against a diff or a session transcript, which meets the law-not-advice bar
  even where no machine can take it. The dilution risk sits in the handful of
  process-conduct clauses (skl-5, skl-6, ver-11 — "consult X before Y"),
  which are observable only in a transcript, never in a diff.
- **Roadmap implication:** the constitution does not shrink by deleting law —
  it hardens by moving (b) to (a). The cheapest, highest-frequency moves are
  the git conduct guard and the sdd-lint extensions; the largest single mass
  is the analyzer binding for the dotnet stack; the run itself becomes
  predictable through the engine `audit`/`apply` jobs, with BL-049 gating the
  report emitter.
