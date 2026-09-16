## Changelog

The root `CHANGELOG.md` keeps the [Keep a Changelog](https://keepachangelog.com/)
structure. A task branch adds one change fragment and never edits `CHANGELOG.md`
directly; the three rendered views — `CHANGELOG.md`'s `[Unreleased]` section,
`docs/okf/log.md` and `docs/journal/YYYY-MM-DD.md` — are written by
`legislator render` on the default branch, never on a task branch.

### Change fragments

One fragment per case: `docs/changes/<case>.md`, YAML front matter, three sections.

**Front matter (required, all four keys):**
- `case` — the case key (`BL-NNN` or `L-N`)
- `issue` — the tracker issue number or URL
- `kind` — one of `Added`, `Changed`, `Fixed`, `Removed`
- `date` — `YYYY-MM-DD`

**Sections (required, all three):**
- `## changelog` — one or more `-` bullets for the `[Unreleased]` section
- `## okf-log` — one paragraph for `docs/okf/log.md`
- `## journal` — one paragraph for the day's `docs/journal/YYYY-MM-DD.md`

**Example:**
```markdown
---
case: BL-193
issue: 327
kind: Added
date: 2026-09-16
---

## changelog

- Added change fragments: `docs/changes/*.md` power `CHANGELOG.md`, `docs/okf/log.md` and `docs/journal/` via `legislator render`.

## okf-log

- Added `ChangeFragment` concept and the `render` engine job.

## journal

- Implemented change fragments and rendered views (L-3): `legislator render` writes the three views from `docs/changes/*.md` fragments, idempotent by case key.
```

### Render discipline

- `legislator render` is idempotent by case key: a fragment whose case key already
  appears in a view is not inserted twice, and nothing else in the view is
  touched. A malformed fragment is refused naming the defect and the file.
- Running `legislator render` on a task branch is refused — render runs only
  on the default branch. `sdd-lint` checks fragments (front matter, kind in the
  closed set, case matching the branch) where it used to check the `[Unreleased]`
  section's shape by hand.
- At a release cut the `[Unreleased]` section folds under a `## [vN]` heading
  and the fragments are deleted.

### When to update

- Add a `docs/changes/<case>.md` fragment as part of every task completion
  checklist — the same commit that finishes the task adds its fragment
- Move `[Unreleased]` entries under a dated version heading only when the
  user cuts a release
- Write entries for the change's user-visible or API-visible effect, not
  implementation detail