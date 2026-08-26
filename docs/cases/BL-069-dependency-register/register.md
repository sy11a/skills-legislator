# BL-069 — The dependency register (system @ v22, 2026-08-26)

Classes: **hard** (the surface cannot do its job without it) /
**best-effort** (designed to degrade gracefully) / **operator-side**
(needed on the operator's machine only, never in a legislated repo).
Absence taxonomy: `fail-open` (allows; enforcement quietly absent) /
`fail-loud` (states it could not run) / `crash` (loud but unhandled) /
`silent false green` (reports clean having measured nothing — the worst
class). Evidence: `measured` (M-cells below — dependency hidden from PATH,
surface executed on this machine) / `inspected` (named guard line).
Measurement depth per the clarify decision: load-bearing cells only —
this spike spends verification budget where silence is expensive, nowhere
else.

## Measurements (the load-bearing cells)

- **M1** `guard_git_conduct.py`, `git merge` on the default branch, git
  absent → exit 0, no block. **M1b** control with git → exit 2, message.
  Verdict: fail-open, per the hooks contract.
- **M2** `okf_sync_check.py`, dirty `src/`, git absent → exit 0 silent.
  **M2b** control → exit 2, reminder. Verdict: fail-open (a lost reminder,
  not lost enforcement).
- **M3** `engine anchors`, git absent → exit 0, findings unaffected (the
  job never touches git). Verdict: unaffected.
- **M4c/M4d** `engine okf-debt` on a repo carrying an 85-day debt — with
  git: the finding, exit 1; **git absent: exit 0, clean, not a word.**
  Verdict: **silent false green — the register's headline finding (F1).**
- **M5** `check_static.py`, git absent → `FileNotFoundError` traceback.
  Verdict: crash (loud; ugly but honest).
- **M6** `python3` hidden, the hooks.json command line → exit 127 →
  Claude Code treats a non-2 exit as a non-blocking error → enforcement
  silently absent (BL-068 A1, now with the measured exit).

## The register

| dependency | surfaces (class of use) | class | declared where | absence behavior (evidence) |
|---|---|---|---|---|
| **git** | conduct guard + OKF-sync hook (subprocess); engine `okf-debt` (`git log`); eval suite (mutate, check_*, setup_workspace, dashboard); `tools/*`; SKILL.md steps (`git mv`, `git status`) | hard, fleet + operator | nowhere | hooks: fail-open (M1, M2); engine okf-debt: **silent false green** (M4); check_static: crash (M5); fleet.sh: quiet `?` (inspected, `\|\| echo '?'`) |
| **python3** | hook runner (hooks.json commands); engine invocation; eval suite; `fleet.sh` inline JSON reads | hard, fleet + operator | **the one law-declared dependency**: absent-branches in `core/verification.md` and audit checks 15/17 | hook command exits 127 → enforcement silently absent (M6 + BL-068 A1); verification rungs fail-loud *by law* — the model the rest of this register should follow |
| **bash** | `tools/*.sh`; SKILL.md's Bash steps; evals-bg | hard, operator + procedural | nowhere | harness-provided in practice (Claude Code requires bash/Git Bash); scripts without it: command-not-found, loud |
| **node ≥ 22.6** | opencode plugin runtime (via opencode); `check_opencode_plugin.mjs` — imports the `.ts` directly via type-stripping, a **version-sensitive** feature | hard, operator (+ fleet where opencode is used) | nowhere; the version floor is documented nowhere | too-old node: import error, fail-loud (inspected); absent: command-not-found (F2: the floor deserves a declared home) |
| **claude / opencode CLIs** | `fleet.sh` runners; `evals-bg.sh` spawn | hard, operator | usage comments only | command-not-found, loud (inspected) |
| **gh** | no shipped surface invokes it — the PR workflow uses it; the conduct guard parses its *syntax* only | operator convenience | nowhere | workflow inconvenience only |
| **dotnet SDK** | `format_on_edit.py` (which-guarded); eval fixture builds (dotnet scenarios); the stack law's own premise | best-effort (format) / hard-operator (fixtures) | nowhere | format: silent skip **by design** (inspected + harness-verified); fixture builds: fail loud |
| **npx / prettier** | format hook; opencode after-hook | best-effort | nowhere | silent skip by design (config + which guards, inspected) |
| **fuser** (psmisc) | evals-bg `/tmp` reclaim (BL-059) | best-effort, operator | nowhere | guarded and **stated**: "reclaim skipped" (inspected, lines 501–512) — the best-effort behavior done right |
| **notify-send** | evals-bg notifications | best-effort, operator | nowhere | `\|\| true` silent skip (inspected, line 179) — fine: pure convenience |
| **ps** | `dashboard.py` `count_runner` | hard for that feature, operator | nowhere | **unguarded** `subprocess.run(["ps"…])` → crash (inspected, line 173; F3) — plus the BSD/MSYS syntax cell (BL-068 E8) |
| **GNU coreutils / util-linux** (`stat -c`, `du`, `find`, `ln -s`, `setsid`, `pkill`, `mktemp`) | `tools/*.sh`; SKILL.md procedure | hard, operator + procedural | nowhere | per BL-068 E1/E2/E5 — breaks off-Linux; on-Linux absent is not a real state |
| **Claude Code / opencode** (the harnesses) | the surfaces that load the law at all | hard, existential | the skill's own docs | out of this register's scope — BL-044/BL-052 own harness behavior |
| **Python stdlib-only** (the engine's *negative* dependency rule) | `engine.py` | — | **declared AND enforced**: `check_static.py` "engine imports only stdlib" | the model case: a dependency rule as an executed check — what the policy below generalizes |

## Findings

- **F1 (the headline):** `engine okf-debt` with git absent returns clean —
  silent false green, measured (M4). It violates the principle the law
  already applies to absent `python3` (fail-loud, "never report the check
  clean"). Fix S: the engine detects git absence and exits outside
  `{0, 1}` with a stderr line, and audit check 17's caller already handles
  non-{0,1} exits as check failures — the SKILL.md side needs zero change.
  Rides the next edition with BL-070's C1 (encoding) — both are engine
  VERSION riders.
- **F2:** the mjs harness's node ≥ 22.6 floor (type-stripping `.ts`
  import) is declared nowhere; an older node fails with a confusing import
  error. Fix S: version assert with a plain message at the top of
  `check_opencode_plugin.mjs`. Operator-side, no VERSION.
- **F3:** `dashboard.py`'s `ps` call is unguarded → crash where `ps` is
  absent/incompatible. Fix S: already BL-070's E8 item; this register adds
  the crash evidence.
- **F4 (structural):** exactly one dependency is law-declared (`python3`)
  and one is check-enforced (stdlib-only). Everything else is folklore.
  That is what the policy below exists to end.

## The policy draft (proposal — a constitution candidate, not written law)

Law-shaped, for the owner to promote via an edition (home: a new
`core/dependencies.md`, or a section of `core/verification.md`):

> **Dependency discipline**
>
> - Every external tool or runtime a shipped surface invokes is declared
>   in the system's dependency register with a class: `hard`,
>   `best-effort`, or `operator-side`.
> - Absence behavior is defined per class and proven by a check, not
>   assumed: enforcement arms **fail open** (a hook bug must never stop
>   the user's work); verification rungs and check jobs **fail loud** —
>   an absent dependency MUST never yield a clean verdict.
> - A `best-effort` surface may skip silently only when its job is polish;
>   when its job recovers or verifies something, the skip is stated in
>   output (the `fuser` reclaim line is the model).
> - A version-sensitive dependency declares its minimum version where the
>   invoking surface can check it and fail loud beneath it.
> - A new dependency enters only with all of the above plus a
>   cross-platform story on BL-068's axes; a dependency that cannot state
>   its absence behavior is not added.

## The three candidate verdicts (the policy applied)

1. **The constitution DB.** Admissible cheaply *if* it is Python's stdlib
   `sqlite3`: no new external binary, the engine's stdlib-only rule holds
   unchanged, absence is not a state. A server DB would fail the policy's
   entry bar today (no absence story for a fleet repo). Verdict: **pass,
   with the stdlib constraint**; in the binary arm the driver is bundled.
2. **The binary arm (ADR-0005 / BL-072).** Published NativeAOT
   **self-contained**, it *removes* rows from this register (python3 for
   hooks and engine) rather than adding one — the framework-dependent
   alternative would add a .NET-runtime row with a version pin, which is
   strictly worse under this policy. One new obligation appears: an
   **absent or version-skewed binary re-creates the A1 silent-absence
   problem** — so the arm's presence and version become an audited
   declaration (manifest pin + checksum, per ADR-0005), which is exactly
   the policy's declaration-home requirement. Verdict: **pass, with
   self-contained publishing and the presence-audit as entry conditions.**
3. **The analyzer binding (BL-067).** Analyzers are repo-owned build
   dependencies (NuGet/editorconfig), not system tools: they enter through
   the repo's own package management, fail loud in the build by nature,
   and the legislator's only duty is auditing that the wiring exists.
   Verdict: **pass; outside the system register by class** — the binding
   case needs no policy exception.

## The answer

The system stands on **13 external dependencies**; **one** is declared in
law (`python3`), **one** is enforced by a check (the engine's stdlib-only
rule), and the other eleven are folklore. Measured where silence is
expensive, the absence behaviors are mostly right — the hooks fail open
exactly as contracted (M1, M2) — with one worst-class exception:
**`okf-debt` without git reports clean, silently** (F1, an S fix riding
the next edition). The policy draft above generalizes the two good
precedents the system already owns (the law's python3 branches; the
stdlib-only check) into an entry bar any future dependency must clear —
and applying it to the three known candidates yields: DB yes-if-stdlib,
binary arm yes-if-self-contained-plus-presence-audit, analyzer binding
out-of-register by class. For BL-072 this register is gate 1 delivered:
the binary arm should *shrink* this table, and the one new row it creates
(itself) arrives pre-declared.
