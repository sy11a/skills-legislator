# BL-068 — Cross-platform audit (system @ v22, 2026-08-26)

Axes: **Linux** (baseline — the whole fleet today, every verdict `verified`
by the green check suite), **macOS**, **Windows-native** (Git Bash present:
the system requires Git, and Git for Windows ships it; Claude Code's Bash
tool requires it anyway). WSL: informational only (Linux-equivalent),
clarified out as an answer.

Verdicts: `fine` / `degrades` (works but quietly stops doing part of its
job) / `breaks` (invariant violated or run fails). Evidence: `verified`
(executed here) / `inspected` (named construct) / `reasoned` (documented OS
semantics). Every non-`fine` cell names its cheapest fix with cost
(S = a session, M = a case, L = its own edition).

## A. Fleet-side: Claude Code hooks

| # | surface / mechanism | macOS | Windows-native | evidence | cheapest fix |
|---|---|---|---|---|---|
| A1 | **hook invocation**: `hooks.json` runs `python3 "${CLAUDE_PLUGIN_ROOT}/…"` | fine (python3 ships with CLT) | **degrades, dangerously**: `python3` usually absent → command fails with a non-2 exit → Claude Code treats it as a non-blocking error → **every guard silently stops enforcing while looking installed** | inspected (hooks.json) + reasoned (Windows python launcher reality; hook-exit semantics) | **M**: interpreter-resolving shim in the command line (`python3` → `py -3` → `python`), verified once on a real Windows machine — the shim's own syntax depends on what shell Claude Code uses for hook commands on Windows, which this repo cannot observe from Linux |
| A2 | `guard_owned_files.py`, `okf_sync_check.py`: pathlib walks, `git status` subprocess | fine | fine (given A1) — path logic is `pathlib`, git ships | inspected | — |
| A3 | `guard_git_conduct.py`: command-head extraction `seg[0].rsplit("/", 1)` (line 192) | fine | **degrades**: `git.exe` or a backslashed path is not recognized as `git` → missed block, fail-open | inspected | **S**: strip `.exe`/`.EXE`, split on both `/` and `\` — both arms (py + TS) |
| A4 | `format_on_edit.py`: best-effort `dotnet format` / `npx prettier` via `shutil.which` | fine | fine — degrades gracefully by design everywhere | inspected | — |

## B. Fleet-side: opencode plugin

| # | surface / mechanism | macOS | Windows-native | evidence | cheapest fix |
|---|---|---|---|---|---|
| B1 | `legislator-guard.ts`: `node:path`/`node:fs` only; git state read from `.git` files (HEAD, loose refs, packed-refs — contents use forward slashes on every OS) | fine | fine — the most portable arm | inspected + verified (mjs harness) | — |
| B2 | same command-head caveat as A3 | fine | degrades (missed block) | inspected | covered by A3's fix |

## C. Fleet-side: the delivered engine

| # | surface / mechanism | macOS | Windows-native | evidence | cheapest fix |
|---|---|---|---|---|---|
| C1 | **text encoding**: zero `encoding=` in `engine.py` — every `read_text` uses the locale default (cp1252 on Windows); UTF-8 docs (arrows, ✅, Cyrillic) misdecode | fine (UTF-8 locale) | **degrades**: mojibake in scanned lines → anchors/sdd-lint can mis-tokenize; `errors="ignore"` hides it silently | inspected (0 matches for `encoding=`) | **S**: explicit `encoding="utf-8"` on every read/write (VERSION bump — the engine is law-delivered) |
| C2 | **invocation name**: the law itself says `python3` (verification rung, audit checks 15/17, CLAUDE.md build lines) | fine | **degrades**: the law's own absent-python3 branches fire permanently — audits forever carry the "could not run" Info line | inspected (law text) | **M**: law-text change naming a resolution order, or the port (below) — either way an edition |
| C3 | engine internals: `subprocess git`, `os.replace`, `Path.rglob` | fine | fine | inspected | — |

## D. Fleet-side: the file model and git configuration

| # | surface / mechanism | macOS | Windows-native | evidence | cheapest fix |
|---|---|---|---|---|---|
| D1 | **checkout of the committed `CLAUDE.md` symlink**: without `core.symlinks=true` (the Windows default is false unless Developer Mode + explicit config), git materializes a *plain text file containing the string `AGENTS.md`* | fine | **breaks, silently — the deadliest cell**: Claude Code reads that one-word file as the entry document → **the constitution does not load at all**, and nothing errors | reasoned (documented git-for-Windows behavior) | **M–L, a design decision**: (a) require Developer Mode + `core.symlinks=true` + `MSYS=winsymlinks:nativestrict`, enforced by a new audit check; or (b) change the v14 file model — a real `CLAUDE.md` whose entire content is the import line `@AGENTS.md` is symlink-free and portable everywhere, but it is a law + eval + grader change (its own case, edition-size) |
| D2 | **creating** the symlink: SKILL.md's `ln -s` under Git Bash silently produces a *copy* by default | fine | breaks (same invariant; audit check 9 catches it *after* the run) | reasoned (MSYS default) | rides D1's decision |
| D3 | **`core.autocrlf`**: owned-file byte-diff (audit check 3, Step 6 verify) vs CRLF-rewritten working tree | fine | **breaks, loudly**: permanent false Critical on every owned file; Step 6 re-copy loops | reasoned (git semantics) | **S–M**: scaffold a `.gitattributes` ruling for owned paths (new scaffolded artifact — edition) or document a required `core.autocrlf=input` and audit it |

## E. Operator side: tools and the eval suite

| # | surface / mechanism | macOS | Windows-native | evidence | cheapest fix |
|---|---|---|---|---|---|
| E1 | `tools/evals-bg.sh`: `setsid` (absent on macOS AND Git Bash), process-group `kill -TERM -$pgid`, `pkill -f` | **breaks** (setsid absent) | **breaks** (setsid, process groups under MSYS) | inspected (lines 196–303, 486) | **M**: port the runner's process control to Python (`subprocess` + `start_new_session=True` is portable); the biggest single operator-side item |
| E2 | `tools/evals-bg.sh`: `stat -c%s` (GNU syntax; BSD stat wants `-f`) | **breaks** (activity probe) | fine under Git Bash (GNU stat ships) | inspected (line 303) | **S**: `wc -c <` or python one-liner; folds into E1 |
| E3 | `tools/evals-bg.sh`: the BL-059 `/tmp` reclaim (`fuser`, tmpfs quota premise) | degrades (skips — by design, fuser guard exists) | degrades (skips) | inspected (lines 501–512 fail-open) | — (already fail-open; the leak it cleans is Linux-specific anyway) |
| E4 | `tools/fleet.sh`: `find`, `$HOME` scan roots, `python3 -c` manifest reads, `claude -p` / `opencode run` | fine | degrades at every `python3 -c` (A1's launcher problem, operator flavor) | inspected | rides the launcher fix; the CLIs themselves ship on Windows |
| E5 | `tools/link-skills.sh`, `link-opencode-plugin.sh`: `ln -s`/`ln -sfn`, `readlink -f` (absent on macOS < 12.3) | degrades (readlink -f on old macOS) | **breaks**: silent copies instead of links → skills/plugin go stale on every source update without anyone noticing | inspected | **M**: python replacement (junctions or copy-with-manifest strategy on Windows) |
| E6 | `evals/check_engine.py`: `chmod 0o000` unreadable-file test | fine | **degrades**: chmod is a read-permission no-op on Windows → that check false-fails | inspected (lines 272–274) | **S**: skip-on-Windows guard with a printed reason (never a silent skip) |
| E7 | `evals/mutate.py` + idem stage: symlink-aware save/restore (`path.symlink_to`) | fine | **breaks** without symlink privilege (Developer Mode) | inspected (lines 138–171) | rides D1's decision — the eval substrate needs whatever the file model decides |
| E8 | `evals/dashboard.py`: `ps -eo cmd` (BSD ps has no `cmd` keyword; MSYS ps is minimal), literal `/tmp` fallback | degrades (agent detection blind) | degrades (same + `/tmp` literal) | inspected (lines 173, 698) | **S**: `psutil`-free portable probe or feature-drop with a printed note; `tempfile.gettempdir()` |
| E9 | `evals/setup_workspace.py`, `grade.py`, `streamfmt.py`, `check_static.py`, `check_mutate.py` | fine | fine (pathlib/shutil/subprocess-git throughout; check_mutate's symlink test rides E7) | inspected | — |

## Totals

21 surfaces judged (A1–A4, B1–B2, C1–C3, D1–D3, E1–E9), three axes each.
Linux: 21× `fine` (verified — today's green suite). macOS: 16 `fine`,
3 `degrades` (E3, E5, E8), 2 `breaks` (E1, E2 — both operator-side).
Windows-native: 6 `fine`, 9 `degrades` (A1, A3, B2, C1, C2, E3, E4, E6,
E8), 6 `breaks` (D1, D2, D3, E1, E5, E7). The two deadliest Windows cells
are **silent**: A1 (enforcement absent while looking installed) and D1
(the constitution not loading at all).

## The patch list, ranked

1. **S — A3/B2**: command heads (`git.exe`, backslash) in both conduct-guard arms.
2. **S — C1**: `encoding="utf-8"` across the engine (VERSION bump rider).
3. **S — E6, E8**: check_engine Windows guard; dashboard ps/tmp portability.
4. **M — A1/E4**: the interpreter-resolving launcher, verified once on real Windows.
5. **M — D3**: the autocrlf/.gitattributes ruling for owned files.
6. **M — E1/E2**: evals-bg process control off bash (Python), stat fix inside.
7. **M — E5**: link scripts off bash.
8. **M–L — D1/D2/E7**: the symlink decision — config-and-enforce vs the
   `@AGENTS.md`-import file model. A design fork, decision-gate material,
   its own case.

## Patch vs port — the recommendation

**Criterion:** a port pays only where the breakage lives in *our logic's
runtime*; it buys nothing where the breakage lives in the *environment
contract* (interpreter resolution, git symlink semantics, line-ending
config) — those cells survive any language.

Judged by that criterion:

- **The two deadliest Windows cells (A1, D1) are environment-contract
  cells.** A .NET port does not remove them: `dotnet run engine.cs` has an
  interpreter-resolution story of its own, and the symlink/autocrlf rows
  are git-level, language-blind.
- **Hooks should stay Python.** They fire on *every* matching tool call;
  Python cold-start is ~30 ms, `dotnet run` file-based apps pay a
  compile-and-cache first run and a materially larger warm start. Latency
  budget, not portability, rules here — plus A1's fix covers them.
- **The engine is the one legitimate .NET candidate** — invoked once per
  rung, not per call; C# file-based (`dotnet run engine.cs`) keeps it a
  byte-verifiable delivered text; and the dotnet fleet already guarantees
  the SDK where `python3` is not guaranteed. But it is L-cost against C1+C2
  at S+M — worth taking **only if** BL-069's register concludes that
  python3-on-Windows cannot be made a declared, checkable dependency while
  the dotnet SDK can. Defer to that register; do not decide from this
  table alone.
- **The operator bash scripts are the genuine port targets** — there the
  *language itself* is the broken part (E1/E2/E5 break on macOS and/or
  Windows). Port them to Python (already required everywhere the evals
  run), evals-bg's process control first.

**Bottom line: patch, with one targeted mini-port.** Fleet-side = the S
patches now plus the launcher fix and the symlink design case; operator
side = migrate off bash incrementally; the engine's .NET question stays
open pending BL-069, with the latency and delivery-model constraints
recorded here as its entry conditions.
