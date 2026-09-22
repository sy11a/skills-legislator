# BL-397 — the retirement and the declaration move in one act

**Tier: 1 (light)** — one engine component, its controls, one step of the skill's law, an edition.
**Spec type: bugfix.**
**Case home:** this directory. **Branch:** `bl/397-the-upgrade-moves-the-declaration`.

Closes `sy11a/skills-legislator#60`; clears the standing order in `sy11a/Architector#448`.

## Current behaviour

Edition 26 retires `docs/ai/engine.py`: `apply` computes the new owned set, finds the path in the
old list and not the new, and deletes it. Four governed repositories declare that same file as
their own gate, in hand-authored instance data the legislator owns nothing of:

| repo | where |
|---|---|
| clerk, debrief, dev-flow | `AGENTS.md` § Build & Test |
| **foundry** | `AGENTS.md`, **and `tools/gate.sh:22-23`** |

**The day one of them takes the upgrade is the day its declared gate names a file that is gone.**
The failure is loud — a missing file fails loudly — but the *declaration* is then wrong, and a
session reading the entry document for how to verify is sent to a command that cannot work.

## Why neither ordering is safe on its own

> "Move the declaration first and the entry document contradicts the edition-25 rule files it
> `@`-imports (which name `engine.py` as *"the executing arm of this rule"* and as the rung before
> "done"); move it after, and the upgrade day is the day the gate names a missing file."

That is not hypothetical. An attempt to move clerk's declaration ahead of its upgrade was refused
by its own refutation round on two grounds: it adds a binary the repository tells no contributor to
install, against declared tooling of *"Python 3 stdlib + `gh`"*; and it makes the entry document
contradict the law it imports. Both are real and neither is fixed by being careful.

## Expected behaviour

`apply` sweeps for commands naming a path **this run is about to retire**, before its first write.

- **Declaration homes are rewritten**: the entry document (and a real alias beside it),
  `.claude/rules/**`, and the scripts under `tools/` the repository owns. `python3 <retired> <job>`
  becomes `legislator <job>`, in place, keeping the rest of the line and the file's own ending.
- **An unrecognised shape stops the run at exit 4**, naming the file and the line, before anything
  is written — including the retirement. Guessing at a shape the rewrite does not know is how a
  migration silently breaks the gate it was asked to protect.
- **History is reported and left**: a case summary, a journal day or a changelog entry naming the
  same command records what was true when it was written, and its going out of date is the design
  (`core/artifact-lifecycle.md`). They print as `left as history`.
- **Owned files are in neither set** — this same run rewrites them byte-for-byte and they
  self-heal. That is the distinction between `kbl` (only owned text names the engine, so nothing is
  owed) and `clerk` (its own entry document does).

## Unchanged behaviour — the regression contract

- **Nothing about what `apply` copies, deletes, keeps or writes into the manifest changes.** The
  sweep adds a refusal before the first write and a rewrite after the retirement; the 720 tests
  that described the old behaviour pass unchanged.
- **The rule keys on "a path this run retires", not on `engine.py`.** No literal for that file
  enters the engine; a later edition retiring something else is covered by the same act.
- **A repository retiring nothing sweeps nothing** — the plan short-circuits on an empty set, so an
  ordinary re-apply reads no file it did not read before.

## The hurting case

**GIVEN** an edition-25 repository whose manifest owns `docs/ai/engine.py`, whose `AGENTS.md`
declares — **on one line, as all four of them do** — ``- Every commit: `python3 docs/ai/engine.py
anchors`, `python3 docs/ai/engine.py sdd-lint`, and `python3 -m unittest discover tools/tests` ``,
and a package that no longer ships that file,
**WHEN** `legislator apply --skill <package> --root . --stacks ""` runs,
**THEN** it exits 0, the file is gone, **and the line reads ``- Every commit: `legislator anchors`,
`legislator sdd-lint`, and `python3 -m unittest discover tools/tests` ``** — every invocation moved
and the command that is not the retired one untouched — and its stdout names the line it rewrote.

The first version of this fix rewrote **one** occurrence per line, saw the retired path still there
and refused; every one of the four repositories writes both invocations on one line, so it refused
all four and offered the remedy the ruling forbids — correct it by hand. Its control was written
with one invocation per line, a shape no fleet entry document uses. **A fixture of a shape the
subject does not have is a control over a corpus the change never meets.**

**AND GIVEN** the same repository whose `AGENTS.md` instead reads
`Verification: read docs/ai/engine.py and run what it says.`,
**THEN** the run exits 4 naming `AGENTS.md:1`, **and nothing is written at all** — the retired file
still there, the manifest still at version 24, **and not one owned file of the new edition laid
down**. Asserting only the first two passes a migration that copies the v27 law in and then stops,
which leaves the entry document importing law that names the binary while still declaring the
python form: the contradiction this case exists to prevent, reached through the stop.

It is the case it would hurt most to see broken because it is the ordering trap itself: the two
acts are one act, and a control that only checks the rewrite would pass a migration that retires
the file and leaves a declaration it could not read.

## Boundary

**In scope.** The `apply` job's sweep, its controls, Step 3 of the skill's law, and the edition
bump that carries them.

**What the edition assignment owes, and does not pay here.** `skill/VERSION` moves 26 → 27, which
assigns the number L-1's benchmark record left to the operator; the recommendation taken is that 27
carries both halves, since neither is released and splitting them would make every repository take
two upgrades for one week's work. Owed before a repository takes 27, recorded in
`evals/benchmarks/v27.md`: a published `27.0.0` binary with its digest, and the e2e benchmark
re-run against that arm. Until the binary exists, `python3 evals/check_engine.py` reports
`[arm-integrity] the installed arm reports 26.0.0 where this edition pins 27.0.0` on a host whose
arm is 26 — the check working, not a defect.

**Out of scope, each with its own record.** Rewriting the four repositories' declarations by hand
— that is the trap this case exists to close, and each moves when it takes the edition. The
`AGENTS.md` § Build & Test wording beyond the command itself, which is the repository's own. And
whether the four take edition 27 at once or one at a time, which is the operator's ordering.

## Clarifications

**Session 2026-09-22 (operator ruling).** The question `sy11a/Architector#448` leaves open —
*"whether legislator's own migration should perform that rewrite"* — was put as a recommendation
and ruled yes. The reasoning recorded with it: the migration already writes to the entry document
under the sanctioned exception; the edition knows what it stopped delivering and the repository
does not; and a standing order that a human performs the rewrite in the same act is exactly the
class of unenforced law this fleet keeps repairing.
