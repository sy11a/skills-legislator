# Refutation — BL-357, the bindings file gains a form, a template and a check

The core mechanism works: the parse mirrors the kernel exactly, the check fires on the two prose-only products and is silent on the three with real tables, and the `OptionsComposer.Apply` arm is in place. The findings below are real gaps, none of them a reason to block merge.

---

## Claim 1 — "The check mirrors the kernel's parse exactly"

**Confirmed with one gap.**

Kernel (`GateBindings.cs:17-39`) vs. check (`AuditChecks.cs:576-613`), rule by rule:

| Rule | Kernel | Check | Match? |
|------|--------|-------|--------|
| Split on `|` | `line.Split('|')` | `line.Split('|')` | ✓ |
| Require exactly 5 pieces | `cells.Length != 5` | `cells.Length != 5` | ✓ |
| Skip empty first cell | `cells[1].Trim().Length == 0` | `name.Length == 0` (after trim) | ✓ |
| Skip `Gate` header | `gate == "Gate"` | `string.Equals(name, "Gate", Ordinal)` | ✓ |
| Skip separator | `gate.All(c => c == '-' \|\| c == ':')` | `name.All(c => c is '-' or ':')` | ✓ |

The parse rules are byte-for-byte equivalent. Where they differ:

- **MINOR** — **The kernel's record names the third field `BoundBy`, the law and template name it "what it proves."** `GateBinding(Gate, Command, BoundBy)` (`GateBindings.cs:3`), the kernel class comment says `| Gate | Command | Bound by |` (`GateBindings.cs:6-7`). The law (`core/verification.md:20`) calls it `| <name> | <command> | <what it proves> |`. The template calls it `What it proves`. Since the parser skips the header, this doesn't affect parse behavior — but having three different column names for the same cell is drift that will confuse anyone tracing a gate row from law to kernel. The law hedges ("what it proves or what binds it", line 21) but the kernel record uses only `BoundBy`. Pick one name and use it in all three places.

---

## Claim 2 — "The form as written is the form the kernel accepts"

**Confirmed with one language gap.**

Each form rule from `core/verification.md:19-24` checked against the parser:

- "Three cells between four pipes" → `cells.Length != 5` rejects non-3-column rows ✓
- "A row whose first cell is literally `Gate`" → `gate == "Gate"` skips it ✓
- "Or is only dashes and colons" → `gate.All(c => …)` skips it ✓
- "A header is optional" → no header-checking logic exists; a table with no header row works ✓
- "A `|` inside a command breaks its row" → `cells.Length != 5` rejects (pieces > 5) ✓
- "Prose … are not gate rows" → no pipe → `cells.Length == 1` → skip ✓

- **MINOR** — The form description `| <name> | <command> | <what it proves> |` uses angle brackets that could be misread as requiring literal `<` and `>` characters. The prose clarifies ("Exactly three cells between four pipes"), so a careful reader gets it right, but the form's visual shape invites the wrong reading. Drop the angle brackets: `| name | command | what it proves |`.

---

## Claim 3 — "The template is a file that actually parses"

**Confirmed — 1 row — with one false universal claim.**

Running the kernel's parse over the template content:

```
| Gate | Command | What it proves |    → skip (header)
|------|---------|----------------|    → skip (separator)
| anchors | `legislator anchors` | …  → 1 row recorded
```

The template declares exactly one gate row. The kernel reads it correctly.

- **SERIOUS** — `verification-rules.md.tpl:21-22`: *"The row above is runnable in every repository that carries this constitution, so there is always at least one."* This is false. The law itself acknowledges the contrary on the very next line of `core/verification.md:10`: *"Where the `legislator` binary is absent the rung cannot run at all."* A repository on a machine with no legislator binary (non-Linux host, a CI runner without it, a fresh checkout) carries the constitution but cannot run `legislator anchors`. The template asserts a universal that the law contradicts — and said repository would write a one-row table whose one command fails, which is worse than no table at all (a gate that fails to run is not a gate that passed, but it's also not a gate that can be interpreted as "nothing to check"). Qualify the sentence: "runnable in every repository where the legislator binary is on PATH" or "runnable in every repository that carries this constitution **and the binary**."

---

## Claim 4 — "It fires on the two products and is silent on the three"

**Confirmed.** Ran the binary at `<repo>/artifacts/linux-x64/legislator` against all five repos:

| Repository | Result |
|-----------|--------|
| aidispatcher | **fired**: prose-only `.claude/rules/verification.md` |
| runpool | **fired**: one-line prose `.claude/rules/verification.md` |
| Architector | bindings-form in clean checks |
| foundry | bindings-form in clean checks |
| dev-flow | bindings-form in clean checks |

`config show` confirms `bindings_file = .claude/rules/verification.md  [defaults]` on a configured repo.

---

## Claim 5 — "Absence is deliberately silent"

**The split is correct, but the check could show awareness.**

The law (`core/verification.md:12`): *"When that file is absent, the ladder still applies with repo defaults (build + full test suite)."* The check is silent on absence — and should be, because the law declares absence a lawful fallback. The merge queue throwing on absence is a kernel defect (`sy11a/Architector#398`), not this check's.

- **MINOR** — The check is the kernel's proxy, and it silently certifies a state the kernel handles worse than zero rows. A one-line Info note ("file is absent — lawful per core/verification.md but the merge queue will throw on this until the kernel half is closed") would bridge the gap without changing the severity model. The operator who reads an all-clean audit and sees no gates file would have no signal that the merge queue will reject it. Without that note, the check is correctly silent but unhelpfully so.

---

## Weakness 1 — `legislator anchors` always runnable?

Covered under Claim 3 (SERIOUS). The binary-not-installed case contradicts the template's universal claim.

Related, not raised in the brief: what about a repo with no `docs/okf/`? `legislator anchors` checks that anchored OKF documents' backticked symbols resolve. A repo with no OKF documents has no anchors to check — `legislator anchors` exits 0 trivially. So it IS runnable, just vacuously. The claim holds on this dimension.

---

## Weakness 2 — Warning severity vs. "merges everything"

- **SERIOUS** — The check's own finding message says: *"Zero rows is read as no gates at all, and a gate set with no rows is not one that passed."* The consequence: every task in a zero-row repository merges with its gate stage passing. That is a silent bypass of the entire gate system — the release's primary quality control. Warning is the audit's second-lowest severity. A gate set that the kernel reads as empty should report at a severity that cannot be read as "noted, will fix later." The argument for Warning is that audit findings don't gate the merge queue directly, but severity is a signal to the operator about urgency, and "your gates are invisible to the machine" should not read as the same urgency as "a file named only by case collision."

---

## Weakness 3 — OptionsComposer.Apply arm forgotten, caught by theory test

The arm is present (`OptionsComposer.cs:99`). The option is complete: declared (`LegislatorOptions.cs:84-85`), in the key map (`LegislatorOptions.cs:207`), in `AllOptions()` (line 294), applied (`OptionsComposer.cs:99`), and printed by `config show`. No remaining gap.

---

## Weakness 4 — The check reads a configurable path; the kernel reads a hardcoded one

- **MINOR** — `options.BindingsFile` defaults to `.claude/rules/verification.md`, which is what the kernel hardcodes. If the operator changes the option, the check certifies a file the kernel doesn't read. The defense is that changing the option is an explicit operator act and the operator is responsible for keeping the two in sync. But the check's raison d'être is to be the kernel's proxy, and it can diverge from the kernel on a single configuration change. Document the coupling in the option's doc comment: "This must match the path the kernel's merge queue reads (currently hardcoded to `.claude/rules/verification.md` in foundry)."

---

## Weakness 5 — Six tests: shapes they miss

- **MINOR** — No test for a `|` inside a command cell. The law says this "breaks its row" and it's the most explicit form rule. Both parsers handle it correctly (cells.Length > 5 → skip), but the one rule that got its own bullet in the law has no test exercising it.
- **MINOR** — No test for a file with valid rows mixed with prose. A repo that adds a proper table row but leaves old prose paragraphs would get green from the check while the prose gates are silently lost. Correct per the law ("prose declares nothing") but this is a real scenario: the operator converts one gate to a table row, leaves the others as prose, and the check certifies the file as readable — while the prose gates are invisible to the kernel.

---

## Where the work is right

- The parse mirror is exact — no case where the check says readable and the kernel reads zero, or the reverse. This is the one claim where a mismatch would be dangerous (a green the reader won't honour) and it holds.
- The feedback loop this repo's own static law caught two violations in the work itself (`check_static.py` flagged the path literal and the authority-shaped prose) is the case's own argument paid inside it.
- The `Every_key_is_settable_from_a_layer` theory caught the missing `Apply` arm — the same trap from four days earlier, caught again. The pull request was red when it went up and the failure is stated rather than amended away. This is honest process, not a defect.
- Calling the missing arm "[t]he same trap c17ca65 fixed" and not silently repairing it is the right call. A new option is two edits and looks like one — stating the failure warns the next person adding an option.

## What my checking covered

- Side-by-side parse rule diff between `GateBindings.cs:17-39` and `AuditChecks.cs:576-613` (Claim 1)
- Each form rule from `core/verification.md:19-24` against the parser (Claim 2)
- Manual parse of the template through the kernel's rules (Claim 3)
- `legislator audit` run against all five repos — aidispatcher, runpool, Architector, foundry, dev-flow — using the prebuilt binary (Claim 4)
- `config show` confirming the option surfaces correctly
- Confirmed `OptionsComposer.Apply` carries the `BindingsFile` arm
- Confirmed `LegislatorOptions.cs` declares the option, the key map, and `AllOptions()`
- Reading the actual prose files in aidispatcher and runpool to confirm the finding text matches the defect