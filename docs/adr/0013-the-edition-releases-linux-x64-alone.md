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

`tools/publish-legislator.sh`'s `released=(...)` line is the **single statement** of that set.
The release matrix builds it, and `evals/check_static.py` **reads that line** rather than
restating the set — a restatement is how two surfaces of one fact part company, which is the
defect BL-406 spent a day repairing one directory over.

Two checks hold the pair together, and each was proven by deleting what it guards:

- every released RID appears as a matrix row;
- the matrix builds no RID the edition does not release — a job whose RID nothing releases spends
  a runner and reddens a tag for nothing.

Both read the matrix's `include:` rows and not the file's text. The first draft matched a
substring of the whole workflow, and both of its mutations survived: the retired RIDs are named
in a comment, and `"win-x64" in body` was true of a line explaining that nothing builds
`win-x64`. A check that reads prose cannot tell a builder from a footnote.

The retired rows are kept in the workflow as a comment — what image each ran on, and why
`osx-x64` names `macos-15-intel` rather than `macos-13` (BL-370) — so restoring one is adding a
row back, not archaeology.

## Consequences

**What gets better.** A tagged release produces a green workflow run. `audit` check 20 compares a
machine's arm against one digest instead of four, and an edition with no digests recorded is
still not a fault (ADR 0010). The released set now lives in exactly one place.

**What this costs.** The edition no longer claims to be cross-platform, and nothing verifies that
its source still builds on Windows or macOS. A regression on any of those three will not be seen
until someone restores the row — and by then it may be old. **This is deliberate**: the port was
already unverified in practice, since the one RID with a builder was failing and the failure was
being carried rather than read.

**Restoring a RID is two edits in one act** — a row in `.github/workflows/dotnet.yml` and a word
in `released=(...)`. Either alone reddens `check_static.py`, which is the point.

**`Architector#436` / BL-372 leaves the critical path.** It is no longer a release blocker; it is
the work that must be done before `win-x64` is added back.
