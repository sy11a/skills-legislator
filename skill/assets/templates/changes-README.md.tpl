# Change Fragments

One fragment per case: `<case>.md` (e.g. `BL-193.md`, `L-3.md`).

Each fragment carries YAML front matter (`case`, `issue`, `kind`, `date`) and three sections — `## changelog`, `## okf-log`, `## journal`. A task branch adds one fragment and never edits `CHANGELOG.md`, `docs/okf/log.md` or `docs/journal/` directly. Those three views are written by `legislator render` on the default branch. See `docs/ai/rules/core/changelog.md` for the full discipline.