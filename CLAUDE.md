# Legislator — skill development repo

This repo develops the `legislator` Claude Code skill. `skill/` is the shipped
package (symlinked into `~/.claude/skills/legislator`); `evals/` is its
regression suite; `docs/` holds this repo's own backlog and historical
specs/plans.

## Testing is mandatory — no change to `skill/` is done until verified

**Read `evals/POLICY.md` before planning any change to `skill/`.** It is the
authoritative bar: evals are a deliverable, not a check; the eval for a
change is designed *before* the change; an edition ships only at 100% on the
corpus plus idempotency ×3; every red is classified (law / grader / harness /
model) before it is fixed; and each edition records its **model floor** — the
cheapest model at which it reaches 100%. The one rule not to skip: **a new
assert must be shown RED against the unchanged law before it is shown
green** — an assert that is green before the change is measuring nothing,
and reading it will not tell you that.

Editions are tagged at merge (`v17`, `v18`, …). The tag carries a law and
its grader together, which is what makes POLICY's baseline run a two-line
`git worktree` operation instead of an archaeology exercise.

Any edit under `skill/` (SKILL.md, `assets/rules/**`, `assets/templates/**`,
`references/**`, VERSION) must pass, before being reported as complete:

1. **Every commit:** `python3 evals/check_static.py` — seconds, no agent.
2. **Every behavioral change** (VERSION bump, SKILL.md procedure edit, rule
   content change, template change): the full e2e benchmark per
   `evals/README.md` — materialize a workspace, run the scenario agents,
   grade, run the idempotency pass, and record the results in
   `evals/benchmarks/v<N>.md` compared against the previous version's file.
   A pass-rate drop or new idempotency diff is a regression: investigate and
   fix (or explicitly surface it to the user) — never commit over it
   silently.

Documentation-only edits (README, `docs/**`, `evals/**` itself) need neither.

## Other repo rules

- **Never add AI co-author trailers to commits in this repo** — no
  `Co-Authored-By: Claude ...` lines of any kind. The fleet law
  (`skill/assets/rules/core/pair-development.md`) applies to the legislator
  repo itself, even though this repo is not legislated.
- Editing any file under `skill/assets/rules/` means the constitution
  changed: bump `skill/VERSION` in the same commit (see README.md).
- Rule files contain only enforceable law; how-to guidance is delegated by
  pointer — see "Content discipline for rule files" in README.md.
- Historical specs/plans under `docs/superpowers/` record decisions already
  executed — never rewrite them. **Carve-out:** redacting *identifiers* is
  not rewriting a decision. Replacing a fleet repo's name with its alias, or
  an absolute local path with `<repo>`/`<fleet>`, leaves every claim, date
  and conclusion untouched and is permitted — indeed required by the rule
  below. Changing what a record *says* remains forbidden.
- **Tracked files carry no fleet repository names and no absolute local
  paths.** Fleet repos are referred to by stable alias (`fleet-api`,
  `fleet-platform`, `fleet-agent`, `fleet-obs`); the decoding key lives
  outside every repository, at `~/.claude/legislator-fleet-aliases.md`.
  Paths are `<repo>` / `<fleet>/<alias>`. Aliases are stable identifiers —
  never reused for another repo, never renamed, so cross-references between
  documents keep resolving. `check_static.py` enforces this on every commit
  (the name half of the check needs the decoding key, so it is strongest on
  a machine that has it). The one deliberate exception is the environment
  variable `KBO_EVALS_NO_BROWSER`: an integration contract, not prose.
  Note that this governs the working tree only — git history still contains
  what was committed before (BL-040).
