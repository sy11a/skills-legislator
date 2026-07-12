# Legislator — skill development repo

This repo develops the `legislator` Claude Code skill. `skill/` is the shipped
package (symlinked into `~/.claude/skills/legislator`); `evals/` is its
regression suite; `docs/` holds this repo's own backlog and historical
specs/plans.

## Testing is mandatory — no change to `skill/` is done until verified

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
  executed — never rewrite them.
