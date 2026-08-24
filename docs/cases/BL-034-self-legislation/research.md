# BL-034 — research

The before-picture, recorded at case opening on 2026-08-24 against
`bl/034-self-legislation` cut from `master`. Every later verification compares
to this.

## Decision — the migration is safe to attempt

**Decision:** proceed with migration mode. **Rationale:** the write-guard keys
on `docs/ai/rules/**`, `docs/ai/engine.py` and the root `opencode.json`, and
on nothing under `skill/`. Legislating this repository therefore cannot stop
it from producing the next edition. Measured, not read — see T-02 below.
**Alternatives:** none viable; had the guard covered `skill/`, the case would
have had to stop and become a `skill/` change instead.

## T-02 — the write-guard's reach, measured before delivery

`plugin/hooks/guard_owned_files.py` driven directly with an `Edit` payload per
path, in this repository as it stands:

| path | exit | verdict |
|---|---|---|
| `skill/assets/rules/core/okf.md` | 0 | allowed |
| `skill/VERSION` | 0 | allowed |
| `docs/ai/rules/core/okf.md` | 0 | allowed — no manifest yet, the guard no-ops |
| `opencode.json` | 0 | allowed — same reason |

The last two rows are the control: they must flip to BLOCKED after delivery
while the first two stay allowed. That flip is the observable proof that the
guard covers exactly the owned set and nothing of the source. Re-measured in
T-09.

## T-01 — the entry document, section by section

`CLAUDE.md`, 72 lines, two top sections. Verdict per block, which is the plan
T-03 executes:

| block | verdict | destination |
|---|---|---|
| `# Legislator — skill development repo` — what the repo is, what `skill/`, `evals/`, `docs/` hold | instance data | stays in `AGENTS.md` |
| `## Testing is mandatory` — POLICY pointer, the red-before-green rule, the two commit gates, the e2e benchmark trigger, editions tagged at merge | law-shaped, repo-specific | `.claude/rules/` |
| `## Testing is mandatory` — "One exception rides every edition" (the Horizon clause) | law-shaped, repo-specific | `.claude/rules/` |
| Never add AI co-author trailers | **already covered** by `core/pair-development.md` | removed |
| Editing `skill/assets/rules/` ⇒ bump `skill/VERSION` in the same commit | law-shaped, repo-specific | `.claude/rules/` |
| Rule files carry only enforceable law; how-to delegated by pointer | law-shaped, generalizable | `.claude/rules/` — and a constitution candidate |
| Historical specs/plans never rewritten, with the identifier-redaction carve-out | law-shaped, generalizable | `.claude/rules/` — and a constitution candidate |
| Tracked files carry no fleet names and no absolute local paths | law-shaped, repo-specific (the alias set is this project's) | `.claude/rules/` |

Two blocks are marked generalizable. They are expected to surface in the
migration's own `## Constitution candidates` section, which is the harvest
working as designed. Promoting them is a later edition and out of scope here
(spec §Boundary).

## T-01 — references to `docs/glossary.md`

Tracked files outside `docs/superpowers/**` that name the path, and which T-05
must therefore update:

- `docs/philosophy.md`
- `docs/ontology.md`
- `docs/backlog.md`

`docs/superpowers/**` also names it and is left untouched: legacy history,
never rewritten.

## T-01 — the four static suites, before

| suite | verdict |
|---|---|
| `evals/check_static.py` | green |
| `evals/check_engine.py` | green |
| `evals/check_hooks.py` | green |
| `evals/check_opencode_plugin.mjs` | green |

All four green before the case. R-011 requires all four green after it, so any
red that appears is caused by this case and must be attributed, not absorbed.
