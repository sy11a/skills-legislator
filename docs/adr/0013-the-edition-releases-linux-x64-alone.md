# 0013 — The edition releases `linux-x64` alone

## Status

accepted

## Context

Since BL-082 the edition has declared four released RIDs — `linux-x64`, `win-x64`, `osx-x64`,
`osx-arm64` — and `.github/workflows/dotnet.yml` built all four, because NativeAOT does not
cross-compile between operating systems and the matrix is the only place three of them could
come from.

Three facts stood against that on 2026-09-23.

**`win-x64` has been red since BL-372** — four projects, 111 failures. Every run of the matrix on
`master` since then has carried one failing job, and `fail-fast: false` means the run finishes
red and stays red.

**The operator develops on Linux only.** Asked directly whether the Windows port should be
repaired before edition 27 ships, the ruling was to defer it: *"виндоуз пока можно отложить — я
веду разработку пока что только на линукс."*

**A tag inherits the red.** The digests an edition releases are recorded by hand at the tag
(`README.md` step 7); nothing in CI writes them. What the matrix contributes to a tag is a
workflow run — and with three RIDs nobody exercises, that run is red the moment it starts, which
makes the redness permanent and unreadable rather than informative.

Asked to choose between releasing on one RID, releasing on three and marking `win-x64` absent,
and holding the tag, the operator ruled **linux-only**.

## Decision

**The edition releases `linux-x64` and nothing else.**

**`released` is a statement about the release, not a gate on building.** The first draft made it
one, and the refutation round showed the price: with a one-RID set the publish script refused the
host's own default RID on every macOS and Windows machine, which takes the arm,
`evals/check_dotnet.sh` (it publishes after a green test run) and audit check 20's remedy —
`install-legislator.sh` needs `artifacts/<host rid>/legislator` — away from every contributor not
on Linux, in one act, while the README said the opposite. What is unbuildable is a cross-OS RID,
and that refusal is the toolchain's and stays fatal. Building a RID the edition does not release
is lawful and is said out loud on standard error, because the digest it produces has nowhere to
go in `release.json`.

`tools/publish-legislator.sh`'s `released=(...)` line is the **single statement** of that set.
The release matrix builds it, and `evals/check_static.py` **reads that line** rather than
restating the set — a restatement is how two surfaces of one fact part company, which is the
defect BL-406 spent a day repairing one directory over.

Two checks hold the pair together, and each was proven by deleting what it guards:

- every released RID appears as a matrix row;
- the matrix builds no RID the edition does not release — a job whose RID nothing releases spends
  a runner and reddens a tag for nothing.

**The workflow is read as YAML and the bash array is read by bash.** Three drafts of this check
failed before one held, each the same way — reading prose and calling it a fact:

1. `rid not in workflow_body` was true of a *comment* saying nothing builds that RID. Both
   mutations survived.
2. A regex over flow-style matrix rows reddened a green tree five ways (a block-style row, a
   quoted value, an anchored row, a one-line `include: [...]`) and passed a matrix that builds
   nothing, by moving the row under `exclude:`.
3. A regex over `released=(...)` read `#` and a runner name as released RIDs when the array
   carried an inline comment — the shape someone restoring a RID would most naturally write,
   since the workflow annotates its retired rows exactly that way.

A fourth draft followed, because the second seat of the round took the third apart:

4. Handing the `released=` line **to bash** was the worst of them. A command substitution in
   that line *executes* — `released=($(echo linux-x64; touch PWNED))` created the file, on every
   gate run, on every host. A static check must not have a code path. The paren matcher also
   counted parens in raw text, so one stray `(` inside an element's comment made it scan past
   the array and hand bash the `case` block below — red on a script `bash -n` calls valid. And
   it read only the **first** assignment, so `released+=(osx-arm64)` on the next line left a
   released RID with no builder and the gate green.

So: `yaml.safe_load` for the workflow, and a **Python** parser for the array — every
`released=`/`+=` assignment, `shlex` for quotes and comments, and anything needing a shell to
evaluate (a substitution, a variable, a brace expansion) **refused by name** rather than
guessed. PyYAML is not a declared dependency, so its absence **fails** the check rather than
falling back to a text match: an unmeasured check that says so is worth more than a weaker one
that says "ok".

Eight further checks came out of mutations that survived a draft. The first round found three:
a build that dropped `-warnaserror`, a deleted publish step, a removed tag trigger. The second
found the rest, and they are the ones worth naming, because each let the matrix report green
while building nothing:

- `continue-on-error: true` on the job — two bytes, and build, publish and upload can all fail
  with the run still green. `if:` was only one member of that family.
- the same key on a step, which makes the strict build advisory;
- a step-level `if:` on Publish, which the run-string check could not see;
- `on: push` and `on: [push, …]` — both documented shorthands — **crashed** the gate on a chain
  of `.get`, losing its summary and five later checks with it;
- an axis-style matrix (`rid: [linux-x64]`), ordinary GitHub Actions, read as "builds nothing".

**The `exclude:` ban is gone.** It said an exclude "can subtract a row this check has just
counted as built", and GitHub's own syntax says the opposite: *"All `include` combinations are
processed after `exclude`."* An exclude can never remove an include row. The ban reddened a
documented pattern for a mechanism that does not exist — the same error as reading prose, one
level up: reading a platform's behaviour from an assumption instead of its documentation.

The retired rows are kept in the workflow as a comment — what image each ran on, and why
`osx-x64` names `macos-15-intel` rather than `macos-13` (BL-370) — so restoring one is adding a
row back, not archaeology.

## Consequences

**What gets better.** A tagged release produces a green workflow run, and the released set lives
in exactly one place. Check 20 (`arm-integrity`) is unaffected either way — it looks the *running*
binary's RID up in the digest map and compares that one entry, so it never compared against four;
and an edition with no digests recorded is still not a fault (ADR 0010).

**What this costs.** The edition no longer claims to be cross-platform, and nothing verifies that
its source still builds on Windows or macOS. A regression on any of those three will not be seen
until someone restores the row — and by then it may be old. **This is deliberate**: the port was
already unverified in practice, since the one RID with a builder was failing and the failure was
being carried rather than read.

**Restoring a RID is two edits in one act** — a row in `.github/workflows/dotnet.yml` and a word
in `released=(...)`. Either alone reddens `check_static.py`, which is the point. The first draft
made that false for the one RID someone will restore first: it had also deleted
`artifacts/<rid>/legislator.exe` from the upload paths, so a `win-x64` restored by exactly the
two documented edits went red at upload with no check to say why. The path is back.

**`Architector#436` / BL-372 leaves the critical path.** It is no longer a release blocker; it is
the work that must be done before `win-x64` is added back.
