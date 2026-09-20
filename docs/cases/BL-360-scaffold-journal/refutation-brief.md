# Refutation brief — BL-360, the scaffold records its own day

You are the refuter. Success is measured in **defects that survive scrutiny**.
Refute the framing if it is wrong. You are not obliged to find anything.

## What this is

A freshly scaffolded repository **fails its own audit on the day it is created**:
the scaffold commits paths outside `docs/` and writes no dated journal entry, so
`journal-recency` fires on the constitution's own act
(`sy11a/Architector#364`). Found independently by two unattended runs on two
subscriptions.

The repair: the scaffold writes the day's entry, from a new template.

The work is `<repo>` (branch
`bl/360-scaffold-journal`) — **read it, do not write to it.** Its whole diff is
`.refutation/legislator-bl-360.patch`. legislator's master is
`<repo> (master)`.

## The claims to check rather than inherit

1. **"Without the entry, a fresh scaffold fails its own audit."** Verify the
   mechanism: read `journal-recency` (check 8) in
   `src/Legislator.Engine/Audit/AuditChecks.cs` and say exactly what it compares.
   Does a dated entry for *today* actually satisfy it when the scaffold commit is
   today? Is there a case where it still fires — an empty repository with no
   commits, a repository whose last outside-`docs/` commit predates the scaffold?
2. **"The entry is true rather than filler."** Read
   `skill/assets/templates/journal-scaffold-entry.md.tpl`. Is every claim in it
   true of a freshly scaffolded repository? It asserts there is *no ADR but the
   one recording that ADRs are kept*, *no case*, and *no gate beyond `legislator
   anchors`*. Check those against what Step 4 actually scaffolds.
3. **"Fresh-scaffold mode only."** The SKILL.md row says an upgrade writes no
   entry. **Is that the right call**, or does an upgrade have the same problem —
   it also commits owned files outside `docs/`?
4. **"The alternative was worse."** The issue offered exempting the scaffold's own
   commit from check 8. I rejected it. **Argue the other side**: is a repository
   with an empty journal more honest than one with a scaffold entry, and is a
   template that every repository carries identically a kind of filler after all?
5. **"No control is possible here."** I claim Step 4 is skill-performed and no
   engine test can assert a scaffolded repository passes its own audit. **Check
   it** — does any test or job write these templates? Is there a seam I missed
   where this could be tested?

## Where I think this is weakest — attack here first

- **The template has three `{{...}}` placeholders** and the SKILL.md row says how
  each is filled. Is `{{STACKS}}` well-defined when there is no stack? Does the
  placeholder convention match how every other template declares its own?
- **The filename is `docs/journal/<today>.md`** — the only Step 4 target whose
  *name* is computed. Does anything else in the skill or engine assume journal
  filenames it does not create?
- **I redacted five absolute paths** in `docs/cases/BL-357-bindings-form/`'s
  refutation files, under `records.md`'s carve-out. **Was that lawful**, and did
  the redaction change any claim, date or conclusion? Read the diff.
- **The whole case is an instruction change.** Is an instruction two independent
  agents already failed to follow repaired by a better instruction?

## Rules for your run

- **Write exactly one file**: `.refutation/REFUTATION.md` in this checkout.
  Nothing else, in any repository.
- **One commit of that one file** when finished (`git add .refutation/REFUTATION.md
  && git commit -m "refute BL-360: <one line>"`), and **do not push**.
- **Never run `dotnet` bare** — go through
  `sh <fleet>/architector/tools/kernel/dotnet-lock.sh <command>`.
  The prebuilt binary at `<repo>/artifacts/linux-x64/legislator`
  is fine to run directly.
- Do not stop any process. No tracker writes.
- **Rank every finding `BLOCKING`, `SERIOUS` or `MINOR`** with file, line, the
  failure scenario, and what you would do instead.
- **Say where the work is right**, and what your checking covered.
