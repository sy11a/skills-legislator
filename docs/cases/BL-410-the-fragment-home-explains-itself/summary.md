# BL-410 — the fragment home explains itself, and is not read as a fragment

**Tier: 0** (direct). Closes `#53`, filed 2026-09-20 by the first delivery of edition 26 into a
consuming repository and standing since.

## What forced it now

The edition-27 benchmark's `case-practice` scenario graded 7/8 on 2026-09-23:

```
delivered_engine_sdd_lint_clean
  exit=1: docs/changes/README.md: no YAML front matter — declare case, issue, kind and date
```

That assertion runs the **delivered** engine's `sdd-lint` over a freshly legislated repository:
the fixture carries no `docs/changes/`, the *run* scaffolds it at Step 4, and the lint then reads
the README it just wrote. So the defect is in the product, not a nuisance in one consuming repo.

**It was not the whole of that red, and the case first said it was.** Both refuters challenged
the claim; the re-run settled it. On the repaired arm the README finding is gone and the scenario
is still 7/8, on a second finding that stood underneath it — `CHANGELOG.md: no ## [Unreleased]
section`, the eval fixture's own heading missing its brackets. That is POLICY §4's harness class
and belongs to BL-406, which repaired it; with both repairs the scenario is 8/8. What this case
clears is one of two, and the sentence claiming it unblocked the corpus was written before
anything had measured that.

## The two defects, both at first contact

**The lint read the home's own explainer as a fragment.** `FragmentLint.Findings` enumerated
`docs/changes/*.md` and applied the fragment shape to every one. Step 4 of the skill scaffolds
`docs/changes/README.md` from `skill/assets/templates/changes-README.md.tpl`, so a repository
went red **the moment it adopted the mechanism correctly**, and could not clear the finding
without deleting its own README. A lint that fires on correct adoption teaches its reader to
ignore it.

**The template contradicted the law of its own edition.** It told every author that a fragment
carries three sections including `## okf-log`, while `core/changelog.md` — delivered by the same
edition — makes that section optional and normally absent, requires **exactly one** `##
changelog` bullet, and cuts `## journal` to dead ends, open questions and decisions. The
scaffolded README is what a repository reads first, and it was the one a human reads.

## What was done

**The name is the gate.** A file in the fragment home is a fragment when it is *named* for a case
key — `BL-193.md`, `L-3.md`. Everything else the home carries is its own furniture. `#53`
offered two directions and called this the stronger one, because the name is also what ties a
fragment to its case: a case-named file must **declare that case**, which catches a fragment
filed under someone else's key. `legislator render` inserts by case key, so such a fragment would
otherwise land in the wrong place silently.

Naming as the gate must not become a way to smuggle a broken fragment past the lint, so a
case-named file is still held to the whole shape — front matter, kind, date, one bullet, a
journal section.

**The template now states its own edition's law**: the four required front-matter keys, exactly
one changelog bullet and why a second is a signal, the journal's three contents and the one-line
form when there are none, and `## okf-log` as optional and normally absent. It closes by saying
what it is — the home's explainer — and pointing at the law.

## Evidence

Four controls added; the suite is 812 green and `check_static` clean. Each repair was run
backwards:

```
remove the name gate              -> 2 controls red (README, and a file named nothing)
remove the case/name match        -> 1 control red (a fragment under another case's key)
```

The arm was republished from this branch, since the scenario that found this asserts the
**delivered** engine and not the source — a fix in `src/` that nobody publishes is invisible to
the thing that found it.

`case-practice`, fresh workspace, the repaired arm and BL-406's fixture repair together: **8/8**,
where both earlier runs were 7/8.

## What this does not close

`#51`, the other standing finding of the same lint — it demands that *every pending* fragment
match the current branch, while fragments accumulate until a release cut, so every branch reddens
on every other case's fragment. Same class, same function, separate intent, and it does not
redden the corpus.
