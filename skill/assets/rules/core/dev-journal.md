## Developer Journal

`docs/journal/` holds one file per working day: `docs/journal/YYYY-MM-DD.md`.

A task branch adds the journal paragraph through its change fragment
(`docs/changes/<case>.md` `## journal` section) and never edits the day file
directly; `legislator render` assembles each day's file from the fragments that
name that day.

### What goes in an entry

- What was worked on and why
- Decisions taken and the reasoning (cross-link to an ADR if one was written)
- Dead ends: approaches tried and abandoned, and why
- Open questions or context for the next session

### When to write

- Write a summary paragraph in the `## journal` section of the case's change
  fragment — one paragraph per case, at task boundaries
- Not a running log of every action; write a summary paragraph per completed
  unit of work
- Create the day's file using `docs/journal/README.md`'s format if it doesn't
  exist yet