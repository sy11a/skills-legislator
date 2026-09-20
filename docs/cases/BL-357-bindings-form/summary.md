# BL-357 — the bindings file gains a form, a template and a check

**Tier: 0 (direct)** · Gate item `sy11a/Architector#398`, the law and scaffold
half. Cross-repo case; the record lives in `sy11a/Architector`.

## The defect this closes half of

`.claude/rules/verification.md` is parsed by foundry's merge queue into gate
commands. **The law that creates the file never said it had a form.** So two of
Release 2's products wrote their gates as prose — honestly, carefully, and
unreadably — and the kernel read **zero gates** from both.

Zero is not nothing: the release end refuses on zero rows, and the merge queue
does not. Every task in a zero-row repository merges with its gate stage passing.

## What landed here

- **`core/verification.md` states the form** — a gate is one row of a
  three-column table; exactly three cells between four pipes; the `Gate` header
  and a dashes-and-colons separator are skipped and a header is optional; a `|`
  inside a command breaks its row; prose declares nothing. With the sentence the
  two products needed: **a repository with no real gates still writes a table**,
  because every repository carrying this constitution has at least one gate —
  `legislator anchors`.
- **A template**, `verification-rules.md.tpl`, scaffolded only when the file is
  absent. The skill had told every repository to create this file and shipped no
  shape for it.
- **Audit check 22, `bindings-form`** — a file that declares no readable row is a
  Warning, in every repository, on every audit.

## Why the check mirrors the kernel's parse exactly

It splits on `|`, demands five pieces, skips `Gate` and the separator — the same
rules, deliberately. **A check kinder than the reader it stands in for reports a
green that reader will not honour**, which is the defect one level up rather than
a repair of it. So a four-column table is a finding here, because it is invisible
to the kernel there.

The other way on **absence**: an absent file is silent. `core/verification.md`
declares absence a lawful fallback (*"the ladder still applies with repo
defaults"*), and that the merge queue handles absence *worse* than zero rows —
it throws rather than parking — is a kernel defect, filed, and not this check's
to report.

## Verified rather than assumed

Run against the two products as they stand today:

```
=== aidispatcher ===
- [bindings-form] .claude/rules/verification.md: declares no gate row a machine can read …
=== runpool ===
- [bindings-form] .claude/rules/verification.md: declares no gate row a machine can read …
```

It fires on both — the exact repositories that shipped a release in that state.
And it is silent on Architector, foundry and dev-flow, which declare real tables.

## Verification

| Gate | Result |
|---|---|
| `bash evals/check_dotnet.sh` | **705 tests pass** — 8 added, plus the theory case the new option adds |
| `python3 evals/check_static.py` | all static checks pass — **after it caught two of this case's own violations** |
| `legislator audit` over this repository | `bindings-form` among the clean checks |

The six tests are the shapes that matter: prose is a finding; a three-column
table is silent; **a header-and-separator-only file is a finding** (the case a
kinder check would miss, because the file *looks* like a table); a four-column
table is a finding; a headerless table is silent; and an absent file says
nothing.

## What is left, and it is the half that bites

**The kernel.** `MergeQueueDriver` still reports green over zero rows where
`ReleaseEndDriver` refuses, and still throws on an absent file instead of
parking. That is foundry's case, and `#398` stays open for it.


## What this repository's own static law caught, in this case's work

`check_static.py` failed twice on the first draft, and both were real:

- **`AuditChecks.cs` carried a path literal.** I hardcoded
  `.claude/rules/verification.md` in the check. Repo law C-03 says add an option
  instead, and every neighbouring check reads one. It is now
  `options.BindingsFile`, declared, named in the option map, and printed by
  `config show` like the rest.
- **`SKILL.md` carried authority-shaped prose outside the File authority table.**
  My scaffold note said the file is *"the repository's own and is never
  overwritten"* — which is an authority claim, and authority claims live in one
  table. Reworded to state the Step 4 rule that already covers it.

Neither would have been caught by reading. Both were caught by a gate this
repository wrote against itself — which is the argument of the whole case, paid
inside it.

## And the trap this repository had already paid for once

Adding `options.BindingsFile` put the key in the option map and **not** in
`OptionsComposer.Apply`. The suite went red at once:

```
BindingsFile is in KeyMap but not applied
```

That is `Every_key_is_settable_from_a_layer`, a theory over every key in the map
— and it is the **same trap** this repository fixed four days ago in commit
`c17ca65`, *"fix: missing ChangesDir arm in OptionsComposer.Apply"*. A new option
is two edits and looks like one.

It is also the reason the red mattered: the pull request had already been opened
when the suite failed, so the failure is stated here rather than quietly amended
away. The arm is added and the suite is green at 703.

## The refutation round, run late and said so

The pull request was opened **before** this round, against the standing rule, and
its suite was red when it went up. The brief told the refuter both. **No
BLOCKING; two SERIOUS and five MINOR.**

**The claim that mattered held.** It diffed the check's parse against the
kernel's rule by rule and found them **byte-for-byte equivalent** — no case where
the check says *readable* and the kernel reads zero, or the reverse. That is the
one claim whose failure would have been dangerous, because it would be a green
the reader will not honour.

**SERIOUS — the template asserted a universal the law contradicts.** It said the
`legislator anchors` row is *"runnable in every repository that carries this
constitution"*. `core/verification.md` says, on its own line 10: *"Where the
`legislator` binary is absent the rung cannot run at all."* Qualified, and
pointed at the better answer — keep the row and say so in its third cell, because
**a gate that cannot run is a different thing from a gate nobody declared, and
only one of them is visible**.

**SERIOUS — the severity was wrong.** A gate set the kernel reads as empty means
every task in that repository merges with its gate stage passing: a silent bypass
of the release's primary quality control. Warning is the audit's middle severity
and reads as *noted, will fix later*. It is **Critical** now.

**MINOR, taken:** the angle brackets in the form invited a literal reading; the
third column is called three different things across the law, the template and
the kernel's own record, so the law now says plainly that **the header is never
read and the third cell is free prose**; the option's doc comment names its
coupling to the path foundry hardcodes; and two tests were missing — a `|` inside
a command (the one form rule with its own bullet and no test) and the real
migration shape, one row beside leftover prose.

## Where the round was half right, and the half I did not take

It proposed an **Info line on an absent file**, so an operator reading an
all-clean audit would know the kernel's merge queue throws on absence rather than
parking. The point is right. The line is wrong: **five repositories of this fleet
have no bindings file**, so it would print on every audit of every one of them —
a class of item that yields no action, which `core/artifact-lifecycle.md`
requires be excluded mechanically rather than left for a human to filter. A
worklist that is mostly noise gets ignored, and this check's one real finding
would be ignored with it.

Implemented, then reverted when the pinned clean-report test went red — which is
how the cost showed itself. The reasoning is now a comment in the check and a
test that pins it, so the next person to have the same good idea meets the
argument rather than the silence.

## And a mistake of my own, in the fixing

The first attempt to write that reasoning spliced on `if (!Exists(path))` — a
line that occurs in more than one check — and **cut the keep-list check in half**.
The build caught it. Re-applied anchored on lines that are unique. An index into
a file is a position, not a meaning.
