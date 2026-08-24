# 0002. The legislator repo is governed by its own constitution

## Status

accepted

## Context

Until 2026-08-24 this repository produced a constitution for nine other
repositories and ran on hand-maintained convention itself: a real `CLAUDE.md`
holding a mixture of law and instance data, a glossary in a home the law does
not name, specs and plans under a directory the law calls legacy, and no
manifest. Every rule the project ships was therefore exercised only in other
people's repositories — the one repository able to catch a rule that does not
work was the one repository exempt from it.

The objection to legislating it is that the arrangement looks circular: the
repository that generates the law would be subject to the law it generates.

## Decision

The repository is legislated by its own constitution and becomes fleet member
number zero.

The circularity is only apparent, because the two strata never touch. The law
has exactly one source — `skill/assets/rules/**` — and everything under
`docs/ai/rules/**`, here as in every other repo, is a delivered copy that is
never edited in place. This is bootstrap compilation, not self-modification:
the using step never writes to the source. Changes flow one way only, through
edit → bump `skill/VERSION` → benchmark → deliver.

Two consequences of that separation are decisions in their own right and are
recorded here rather than left implicit:

- **Version skew on a branch is normal.** While an edition v(N+1) is being
  developed, the repository stays legislated at v(N). Owned-integrity drift on
  the default branch remains a finding.
- **The knowledge homes the law names win over the ones this repo invented.**
  The 48-term register at `docs/glossary.md` moved forward into
  `docs/okf/glossary.md`, where `core/okf.md`'s sync checklist reaches it.
  `docs/superpowers/**` stays exactly where it lies as retired history.

The first delivery was performed by hand. Whether this repository is
maintained by `tools/fleet.sh` like every other member, or by a distinct
release step, is deliberately left open — `fleet.sh`'s discovery cannot reach
this path at all today, and choosing the permanent channel as a side effect of
a scaffolding task is how second delivery paths get created by accident.

## Consequences

**Easier.** Every new rule is exercised by the skill's own development before
it reaches anyone else's repository. That started paying immediately: the first
audit run surfaced fourteen false Critical findings from check 2, which cannot
distinguish an unfilled template token from one quoted in prose about
templates — a defect invisible in every repo that does not document a
templating system, and unmissable in the one that does.

**Harder.** Two disciplines now bind work here that previously did not: a
journal entry and a changelog line per completed task, and an ADR whenever a
decision-gate stop is resolved. The write-guard also blocks edits to
`docs/ai/rules/**`, `docs/ai/engine.py` and `opencode.json` in this
repository — verified as covering exactly those and nothing under `skill/`,
which is what makes the arrangement workable at all.

**Accepted as-is.** `opencode.json` ships as owned law at the repository root
one day after the opencode profile was frozen. It is a dead artifact here.
Removing it would be a change to the skill, not to this repo, so it stays and
is recorded rather than quietly deleted.
