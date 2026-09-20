# Refutation — BL-360, the scaffold records its own day

The fix works: a fresh scaffold with today's journal entry passes the audit. Two
flaws survive scrutiny — one a real gap in the template's `{{STACKS}}` definition
that two agents will fill differently, and one a wording choice that will confuse
a reader of the entry. The split on upgrade is defensible, the redaction was
lawful, and the "no control" claim holds. Every claim checked.

---

## Claim 1 — "Without the entry, a fresh scaffold fails its own audit"

**Confirmed.** The mechanism (`AuditChecks.cs:254-292`):

1. Enumerates `docs/journal/*.md` (skipping `README.md`), reads dates from
   filenames (`YYYY-MM-DD.md` prefix, `LeadingDate()` regex) then from content
   (`AnyDate()` regex).
2. Gets the last commit touching paths outside `docs/` via
   `git log -1 --format=%cs -- . :(exclude)docs/`.
3. If no dated entry is found OR `codeDate − newestDate > 30` → fires at Warning.

A fresh scaffold commits `.gitattributes`, `AGENTS.md`, `CLAUDE.md`,
`opencode.json` — all outside `docs/`. The journal directory has `README.md`
but no dated entry. `newest is null` → fires. Confirmed.

**With the fix:** the scaffold writes `docs/journal/2026-09-20.md`. `lastCode` is
the scaffold commit's own date (today). `newest` is today. Delta = 0 < 30 → clean.
Confirmed.

**Edge cases checked:**

- **No commits at all (`lastCode is null`):** check yields break (line 281–283),
  silent. After a scaffold there IS a commit, so this doesn't apply.
- **Last outside-`docs/` commit predates the scaffold by months:** `codeDate −
  todayDate` is negative < 30 → clean. The journal entry for today is treated as
  "up to date" with old code. Arguably this is too permissive — a journal day
  with no code changes "catches up" to code that hasn't been touched — but that
  is the existing check's design, not this case's.

---

## Claim 2 — "The entry is true rather than filler"

**Mostly confirmed — one wording finding.**

The template asserts:

- "no ADR but the one recording that ADRs are kept" ✓ — Step 4 scaffolds exactly
  `docs/adr/0001-record-architecture-decisions.md` (the Michael Nygard
  self-recording ADR) and `docs/adr/template.md`.
- "no case" ✓ — Step 4 scaffolds `docs/cases/README.md` but no `BL-NNN/`
  directory. A case is a `BL-NNN/` directory with a `summary.md`.
- "no dead ends" ✓ — a fresh scaffold has no work history.
- "no verification bindings that name a real gate beyond `legislator anchors`" —
  this is true in substance (`.claude/rules/verification.md` has one row:
  `legislator anchors`) but the phrasing is unfortunate.

**SERIOUS** — `skill/assets/templates/journal-scaffold-entry.md.tpl:19`: the
wording *"no verification bindings that name a real gate beyond `legislator
anchors`"* reads as "there are no verification bindings" to a reader who
parses "no verification bindings that name a real gate" as the primary clause
and "beyond `legislator anchors`" as a qualifier. A reader of the journal
entry who then looks at `.claude/rules/verification.md` and finds an actual
gate table will wonder whether the entry is false. This is a reading error,
not a factual error, but journal entries are read by future operators — say
*"The only gate is `legislator anchors`"* instead, or *"The verification
bindings name no gate beyond `legislator anchors`"* with a different syntax
that makes the verb positive ("name") and the subject the bindings.

Every other claim in the template is true: the constitution was laid at a
named edition (Step 3's `apply` write `legislatorVersion` from `VERSION`),
the stack subscription is whatever Step 2 confirmed, and the ritual is seeded.

---

## Claim 3 — "Fresh-scaffold mode only"

**The split is defensible, but the justification is thinner than the case says.**

The SKILL.md row (line 115) scopes the journal entry to fresh-scaffold mode.
Step 4's header says the step runs in EVERY mode, so the exclusion is a
per-row override, which is valid per the table's design.

An upgrade run DOES commit paths outside `docs/` — Step 3's `apply` job
writes `opencode.json` at the repo root, and in migration mode the
`CLAUDE.md` → `AGENTS.md` rename + symlink also touches outside-`docs/`
files. These could trigger `journal-recency` on the upgrade commit if no
journal entry exists for that day.

The case's justification: *"the day's work is the upgrading repository's to
record."* The distinction is: on a fresh scaffold the agent commits files the
operator never saw and could not have written an entry about; on an upgrade
the operator initiated the run and could record it. This is a difference of
degree, not kind — both are legislator-invoked runs that produce non-`docs/`
commits — but it is a real difference. An agent that invents a journal entry
for an upgrade the operator drove is writing content the operator never
reviewed about work the operator was present for.

**MINOR** — The template's "What this entry is" paragraph states the rationale
for its own existence and references `sy11a/Architector#364`. A future
operator reading it will understand why the entry exists. The upgrade
decision could benefit from the same transparency: the SKILL.md notes column
is the machine-readable instruction, and the "why" is there (the last two
sentences of the row). But the agent reading it has no instruction to state
the exclusion in the report — upgrades write no entry silently, and nothing
in the audit report signals why a just-upgraded repo might still have a stale
journal. That is a gap in the skill's Step 7 reporting, not in this row.

---

## Claim 4 — "The alternative was worse"

**Argued fully; the counter-argument is real but the decision is right.**

The alternative — exempting the scaffold's own commit from check 8 — would
leave a new repository's journal empty. The advantage: an empty journal is
honest about "no work happened." The disadvantage: the ritual never starts,
and the entry that appears in the first real task session will be the *only*
entry — no template, no example, no "this is what a journal entry looks like."

The chosen approach seeds the ritual. A journal entry written by a template is
not work, but it IS a real entry — the constitution was laid, at a named
edition, with a named stack subscription. The template is not wholly generic:
`{{TODAY}}`, `{{EDITION}}`, and `{{STACKS}}` are filled per-repo, so two
repositories scaffolded on different days carry different entries. The
structural paragraphs ("Why this entry exists," "What is not decided yet") are
identical, but they serve the same purpose as `README.md` — instruction, not
data.

A repository with an empty journal is more honest in the strict sense but less
useful. A repository with a scaffold entry is slightly less honest (it was not
a "working day" in the human sense) but far more useful — the entry tells the
first real worker what goes in a journal and why the ritual exists. The
decision is right.

**MINOR** — The template's third paragraph ("What is not decided yet … belongs
in the next entries") is the scaffold's version of a seed comment, and like
every seed comment, it risks becoming noise. A repository that sits untouched
for months after scaffolding carries this paragraph as "current" content. The
audit does not read journal entry bodies, so there is no mechanical prompt to
replace it. That is inherent to a seed entry, not a defect of this one.

---

## Claim 5 — "No control is possible here"

**Confirmed — no engine test can assert that a scaffolded repository passes its
own audit, and the one seam is too thin to close the gap.**

Step 4 is performed by the skill (an agent following SKILL.md instructions),
not by the engine. The engine's `apply` job writes owned files and the
manifest; it does not write scaffolded artifacts (Step 4's table: OKF bundle,
backlog, journal, changelog, ADRs, project rules). The journal entry template
has placeholders (`{{TODAY}}`, `{{EDITION}}`, `{{STACKS}}`) whose values are
derived by the agent from context (today's date, `VERSION`, Step 2's confirmed
list) — none of which the engine knows at apply time.

The engine's own `Check08JournalRecencyTests` (five tests) cover the check's
logic against constructed filesystems and git dates. They are unchanged and
still cover the check. But they cannot assert that an agent actually fills
`{{TODAY}}` correctly, names the file after it, or fills `{{EDITION}}` from
the skill package's `VERSION`.

**Is there a seam?** One could imagine a CI job that:
1. Runs a full fresh scaffold in a temporary git repo via an agent
2. Runs `legislator audit` on the result
3. Asserts `journal-recency` is clean

But that requires an AI agent in CI, which is outside the engine's scope. The
engine could test that the template exists and has the right placeholders, but
`check_static.py` does not enumerate `assets/templates/`. A simple static
check — "the journal template's filename matches `YYYY-MM-DD`, its
placeholders are declared in the SKILL.md row, and `{{TODAY}}` occurs in the
`#` heading" — would be a lightweight seam that doesn't require an agent.

**MINOR** — No static check verifies the journal-scaffold-entry template is
well-formed (filename pattern, placeholder coverage). The engine's static
checks (`check_static.py`, `check_dotnet.sh`) cover rules, parity, and hooks
but not templates. The gap is real but small — the template is a text file
whose only active consumer is an agent reading SKILL.md's instructions.

---

## Weakness A — `{{STACKS}}` when there is no stack

**SERIOUS** — `skill/SKILL.md:115`: `{{STACKS}}` is *"Step 2's confirmed list,
or `none` where there is none."* This is underspecified in three ways:

1. **Format.** The SKILL.md row says "Step 2's confirmed list" but does not
   say how the list should be formatted. Step 2's confirmed list is stored as
   `["dotnet", "aurelia"]` in the manifest. The convention from
   `{{STACK_SUMMARY}}` (line 128) is a human-readable form (*"`.NET`" for
   `dotnet`, "`.NET, Aurelia`" for both*). Should `{{STACKS}}` use the same
   human-readable form, or the raw stack names? Two agents will fill this
   differently, producing different journal entries.

2. **The "none" literal.** The SKILL.md says *"or `none` where there is
   none"* — meaning the agent writes the literal string `none`. The template
   renders this as "Stack subscription: **none**." A reader seeing a bolded
   `none` after "Stack subscription:" could read it as a stack named "none"
   rather than an absence. The convention from other templates is to write
   something like "none" or "no stacks" in prose — but here it is within a
   bold-marked field, which carries the semantics of a value.

3. **Separation from `{{STACK_SUMMARY}}`.** The AGENTS.md template uses
   `{{STACK_SUMMARY}}` for the same information. The journal template uses
   `{{STACKS}}` — a different token for what appears to be the same data. Two
   tokens for one concept is a maintenance hazard: if the display format of
   stack names changes (e.g., "C#, Aurelia" instead of ".NET, Aurelia"), the
   human-readable rule must be edited in two columns' documentation, not one.

**What I would do:** Name the placeholder `{{STACK_SUMMARY}}` (reusing the
existing derivation rule from line 128, which already defines the
human-readable form), and when there are no stacks, write *"none"* in the
prose just as the derivation rule already permits ("ask the user for
additions"). This closes all three gaps: format is from an existing rule, the
"none" is prose rather than a bold field value, and the concept is one token
declared once.

---

## Weakness B — Placeholder naming convention

**MINOR.** The journal template introduces three placeholders not used
elsewhere: `{{TODAY}}` (vs `{{TODAY_ISO}}` / `{{TODAY_ISO_DATE}}` in other
templates), `{{EDITION}}` (unique to this template), and `{{STACKS}}` (vs
`{{STACK_SUMMARY}}`). `{{TODAY}}` differs from `{{TODAY_ISO}}` only in
precision (date vs date/time) — it is functionally `{{TODAY_ISO_DATE}}` by
another name. `{{EDITION}}` has no synonym in the derivation rules — it is
defined only in the inline notes cell at line 115. This is not a bug, but a
future editor adding a second template that needs the edition number would
have to rediscover its name from the journal row's notes column rather than
from the derivation rules table.

Two of the three (`{{TODAY}}` and `{{STACKS}}`) have near-synonyms already
defined in the derivation rules; only `{{EDITION}}` is genuinely novel. The
template is consistent with itself but inconsistent with the rest of the
template set.

---

## Weakness C — `<today>.md` filename

**Checked — no conflict with any engine assumption.** The file is named
`YYYY-MM-DD.md`, which is exactly the format `JournalLint.cs:11` enforces
(`^\d{4}-\d{2}-\d{2}\.md$`) and `Check08JournalRecency` reads. The `RenderJob`
also writes to `docs/journal/YYYY-MM-DD.md`. The filename convention is
uniform across engine, skill, and template. No finding.

---

## Weakness D — BL-357 redaction

**Lawful and claim-preserving.** The diff shows five absolute local paths in
two files (`refutation-brief.md` and `refutation.md`) replaced with `<repo>`.
The `records.md` carve-out (lines 4–8) explicitly permits this: *"Replacing …
an absolute local path with `<repo>`/`<fleet>`, leaves every claim, date and
conclusion untouched and is permitted — indeed required by the rule below."*

The redacted paths were:
- `refutation-brief.md:54-55`: `<fleet>/legislator-357` →
  `<repo>` (the work location)
- `refutation-brief.md:58-59`: `<repo> (master)`
  → `<repo>` (legislator's master — note: this was the older path, already
  inaccurate after the repo move)
- `refutation-brief.md:67-70`: three paths naming `aidispatcher`, `runpool`,
  `Architector`, `foundry`, `dev-flow` — fleet repos redacted with
  `<repo>/skill` for the skill path and `<repo>/artifacts/...` for the
  binary
- `refutation.md:84`: `<fleet>/legislator-357/artifacts/...`
  → `<repo>/artifacts/...`

None of these changes alter any claim. The brief's claim is "the work is in
this branch of this repo." The refutation's claim is "ran the binary against
all five repos." Both claims survive the redaction — `<repo>` refers to the
same legislator repository either way. Dates, conclusions, and the audit
results table are untouched.

One note: the refutation's Claim 4 results table (five repos tested) uses
`<repo>/artifacts/linux-x64/legislator` for the binary path. A reader who
doesn't know legislator's repo path cannot reproduce the run from the
redacted text. But `records.md` says redaction is *required* — the decoding
key `~/.claude/legislator-fleet-aliases.md` is the mapping. A reader on
this machine has it; a reader elsewhere does not, which is the design of
the redaction rule. The statement "the run is reproducible" is not one of the
refutation's claims.

**Confirmed: the redaction was lawful, and no claim, date or conclusion was
changed.**

---

## Bonus: the static gate catch

The case summary states that `check_static.py` caught the five absolute paths
on entry (BL-360's own entry, before the first commit). This is the same gate
that BL-357 failed to run on its last commit — the diff shows BL-357's
refutation files carried absolute paths that had been merged because
`check_static.py` wasn't run on the final commit. The irony: the case that
adds the journal entry as a self-check was itself checked by the static gate
on entry, and found a violation inherited from the previous case. *"A gate is
only as good as the last commit it ran on"* — this is true, and the BL-360
branch's own first commit passed the gate.

---

## Where the work is right

- The mechanism is correct: a dated journal entry for today defeats the check
  that fires on the scaffold's own non-`docs/` commit. The test suite
  (`Check08JournalRecencyTests`) covers the check's logic and is unchanged.
- The template's substantive claims (no ADR beyond 0001, no case, no dead
  ends) are true of a freshly scaffolded repository. The claims survive
  inspection against every file Step 4 writes.
- The `records.md` carve-out is correctly invoked: the BL-357 redaction
  replaced identifiers with placeholders and changed no claim, date, or
  conclusion. The redaction was lawful and complete.
- The "no control" claim is honest: the engine cannot test that an agent fills
  a template correctly, because the template's placeholders are derived from
  context the engine doesn't have at apply time. The engine's existing
  `journal-recency` tests are unchanged and still cover the check.
- The `JournalLint` regex (`YYYY-MM-DD.md`) matches the template's filename
  convention exactly — no drift between engine and skill.
- The `check_static.py` reddening on entry is a working control: the gate
  caught a violation before the case's own first commit.

## What my checking covered

- `Check08JournalRecency` (`AuditChecks.cs:254-292`): full logic read,
  compared with the scaffold's commit shape, verified the fix defeats the check
- `Check08JournalRecencyTests.cs`: five tests read, confirmed they cover the
  check's logic and are unchanged by this case
- `JournalLint.cs`: filename convention checked against template naming
- Template `journal-scaffold-entry.md.tpl`: every claim checked against
  Step 4's scaffold table — ADR-0001, case directory, verification bindings,
  dev-journal content requirements
- SKILL.md Step 4 table (lines 99–139): template row read, placeholder
  conventions compared across templates, `{{STACKS}}` definition traced to
  Step 2, `{{EDITION}}` traced to `VERSION` file
- BL-357 refutation diff (patch lines 46–78): all five redacted paths
  verified against `records.md` carve-out
- `records.md`: read in full; carve-out confirmed
- All template placeholders (`grep` over `assets/templates/`): naming
  conventions compared
- Engine `apply` job: confirmed it writes `opencode.json` (outside `docs/`)
  and the v14 file model (AGENTS.md/CLAUDE.md symlink), verifying the upgrade
  case for Claim 3
- `VERSION` file: `26` — the current edition