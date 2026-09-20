# Refutation brief — BL-357, the bindings file gains a form, a template and a check

You are the refuter. Success is measured in **defects that survive scrutiny**.
Refute the framing if it is wrong. You are not obliged to find anything.

**This round is late and that is the defect to know about going in:** the pull
request (`sy11a/skills-legislator#56`) was opened before this round ran, and its
test suite was **red** when it went up (a missing `OptionsComposer.Apply` arm,
since fixed). Treat the work as unreviewed, because it is.

## What this is

`.claude/rules/verification.md` is parsed by foundry's merge queue into the gate
commands it runs. **The law that creates the file never said it had a form.** Two
of Release 2's products wrote their gates as prose — honestly, carefully — and
the kernel read **zero gates** from both (`sy11a/Architector#398`). Zero is not
nothing: the release end *refuses* on zero rows and the merge queue does not, so
every task in a zero-row repository merges with its gate stage passing.

This change is the **law and scaffold half**: a form in `core/verification.md`, a
template the skill had never shipped, and audit check 22 `bindings-form`.

The work is in `<repo>` (branch
`bl/357-bindings-form`) — **read it, do not write to it.** Its whole diff is also
`.refutation/legislator-bl-357.patch`. legislator's master is
`<repo>`.

## The claims to check rather than inherit

1. **"The check mirrors the kernel's parse exactly."** The kernel's is
   `foundry/src/Foundry.Domain/MergeQueue/GateBindings.cs:17-39`. **Read both and
   diff the rules by hand.** Any case where the check says *readable* and the
   kernel reads zero — or the reverse — is a finding, and the first direction is
   the dangerous one: a green the reader will not honour.
2. **"The form as written is the form the kernel accepts."**
   `skill/assets/rules/core/verification.md`'s new section claims: three cells
   between four pipes; `Gate` header and dash/colon separator skipped; a header
   optional; a `|` in a command breaks its row; prose declares nothing. **Verify
   each against the parser**, not against the prose.
3. **"The template is a file that actually parses."** Read
   `skill/assets/templates/verification-rules.md.tpl` and run the kernel's rules
   over it by hand. How many rows does it declare? Is `legislator anchors`
   genuinely runnable in every repository carrying this constitution, as the law
   now claims?
4. **"It fires on the two products and is silent on the three."** Run it
   yourself: `legislator audit --root <repo> --skill
   <repo>/skill` over `aidispatcher`, `runpool`,
   `Architector`, `foundry`, `dev-flow`. Use the binary at
   `<repo>/artifacts/linux-x64/legislator`.
   **Do not run `dotnet`.**
5. **"Absence is deliberately silent."** The case argues an absent file is a
   lawful fallback by `core/verification.md`'s own last clause, and that the
   merge queue throwing on absence is a kernel defect filed elsewhere. Is that
   split right, or is the check declining to report the worse state?

## Where I think this is weakest — attack here first

- **The law says a repository with no gates still writes a table**, on the claim
  that `legislator anchors` is always runnable. Is it? What about a repository
  with no `docs/okf/` at all, or one where the binary is not installed?
- **Check 22 is a Warning, not Critical.** A gate set the kernel reads as empty
  merges everything. Is Warning the right severity?
- **The option was added and the `Apply` arm forgotten**, caught only by an
  existing theory test. Is anything *else* about `options.BindingsFile`
  incomplete — `config show`, the option map, docs?
- **The check reads the file at a fixed path**, now `options.BindingsFile`. The
  kernel reads a **hardcoded** path. If they ever disagree, the check certifies a
  file the kernel does not read.
- **Six tests.** Is there a shape they miss that a real repository would produce?

## Rules for your run

- **Write exactly one file**: `.refutation/REFUTATION.md` in this checkout.
  Nothing else, in any repository. Never write to `legislator-357` or to any
  other repository.
- **One commit of that one file** when finished (`git add .refutation/REFUTATION.md
  && git commit -m "refute BL-357: <one line>"`), and **do not push**.
- **Never run `dotnet`** — the host's build lock is shared. The prebuilt binary
  named above is fine to run.
- **Rank every finding `BLOCKING`, `SERIOUS` or `MINOR`** with file, line, the
  failure scenario, and what you would do instead.
- **Say where the work is right**, and what your own checking covered.
