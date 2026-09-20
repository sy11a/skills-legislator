# BL-370 — the release matrix reads its own runner

**Tier: 0 (direct)** · Item `sy11a/Architector#435`. Type: bugfix.

## The defect

`.github/workflows/dotnet.yml` is the release matrix — the only builder of three
of the edition's four RIDs. From the day BL-082 merged it to 2026-09-20 it ran 24
times: 15 `failure`, 9 with no conclusion, **0 `success`**. It never blocked a
merge, so it was read as noise, and a check that is always red says nothing on
the day it has something to say.

Two causes, independent:

1. **Every job failed at `Test` with the suite passing.** `evals/check_dotnet.sh`
   read each module's count with a `sed` anchored to the bare line. A runner's
   terminal colours the Microsoft.Testing.Platform summary — the line arrives as
   `ESC[m  total: 36` — so the reader read nothing, and the instrument-fault guard
   (*"zero tests ran — the runner is broken, not the suite"*) fired by its own
   correct reading. A workstation's output is uncoloured: green on every machine
   that wrote the gate, red on every machine that ran it.
2. **`osx-x64` never started.** The matrix named `macos-13`, an image GitHub has
   retired. The job sat `queued` until the 24-hour cancel, so the run had no
   conclusion at all.

## What landed

- **The reader strips what is the terminal's** — SGR sequences and CR — before it
  reads the line, in expressions BSD `sed` has (the old one used `\+`, which a
  macOS runner's `sed` does not).
- **The reader proves itself before every run**: seven controls
  (`bash evals/check_dotnet.sh --self-test`), three of them red against the old
  reader — a coloured line, colour on both sides, a CRLF line. A reader that fails
  them stops the gate: what it would report cannot be believed.
- **A red module prints everything it said.** The gate printed six lines whatever
  the verdict, so the first real reds arrived as counts with no names.
- **The AOT smoke publishes the host's RID** (`tools/publish-legislator.sh`), where
  it named `linux-x64` on every operating system.
- **The matrix names `macos-15-intel`**; the three actions move to the majors that
  run on the runner's Node; a pushed commit cancels the run of the one before it.
- **`PublishScriptTests`' foreign RID is relative to the host.** It named
  `osx-arm64` — the host's own RID on one macOS runner.

## What the first honest runs found

Everything behind `Test` had never executed. Walked on real runners, in order:

| Run | What it showed |
|---|---|
| 35522116441 | `linux-x64` green end to end — the first green job this workflow has had. Windows and both macOS red at `Test`, with counts and no names |
| 35522288742 | the names: one test on macOS (above), **71 on Windows across 24 classes** |
| 35522537537 | a marked one-run probe let `win-x64` past `Test`: **all four RIDs build, publish and upload** (2.2–2.5 MB each). Withdrawn in the next commit |

## What is left, and it is not this case's to decide

**legislator has never been exercised on Windows, and it is not portable there.**
The 71 are not one cause. Some are the tests' own assumptions (`/usr/bin/dotnet`
expected from `ExecutableLookup`; a temp path under `C:/`). Some are the product:
`orphan-docs` reports `docs/okf/index.md` and `codebase-map.md` as unreferenced
in a fixture where they are referenced, the anchors and baseline jobs print
different text, three jobs throw on a report whose shape differs. An edition that
releases `win-x64` ships a binary whose audit is wrong there.

So `dotnet` is now red for a reason, on one job, and says which tests. Whether
`win-x64` is ported, dropped from the released RIDs, or built without being held
to the suite is a ruling on what the edition releases (operator ruling
2026-09-04 made it four RIDs; `evals/check_static.py` holds the matrix to them).

## Verification

| Gate | Result |
|---|---|
| `bash evals/check_dotnet.sh --self-test` | 7 controls pass; 3 were red against the reader as it stood |
| `bash evals/check_dotnet.sh` | 705 tests pass, host RID published |
| `python3 evals/check_static.py` | all static checks pass |
| Actions, run on this branch's head | `linux-x64`, `osx-x64`, `osx-arm64` green; `win-x64` red at `Test` on the 71 |
