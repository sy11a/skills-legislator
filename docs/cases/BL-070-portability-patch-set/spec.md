# BL-070 — The portability patch set (ADR-0005 phase 1, light half)

**Tier: 1 (light).** Blast radius: both conduct-guard arms, the hook
launcher lines, two eval harness scripts, the dashboard, the two link
scripts. No `skill/` change in this case. Novelty: low — every item
implements a fix the BL-068/BL-069 audits already specified.

**Spec type: feature.** Branch `bl/070-portability-patch-set`. Source:
BL-068's ranked patch list + BL-069's findings; ADR-0005 phase 1.

## Boundary

**In (the light half, per the sizing clarification):** A3/B2 command
heads; A1 launcher shim; E5 link scripts off bash; E6 check_engine
Windows guard; E8 dashboard portability; F2 node floor assert.

**Out, parked loudly (not dropped):** C1 (engine `encoding="utf-8"`) and
F1 (`okf-debt` git-absence fail-loud) — engine VERSION riders, ride
edition v23; D3 (the autocrlf/.gitattributes ruling) — edition material,
rides v23; E1/E2 (evals-bg process control off bash) — honestly
verifiable only by a live eval run, rides the v23 benchmark cycle. The
Windows *execution* verification of the launcher shim is an explicit
residual: this machine cannot observe what shell Claude Code uses for
hook commands on Windows.

## Requirements

- **R-701** — WHEN a Bash command invokes git or gh via a Windows-style
  head — `git.exe`, an upper/mixed-case name, or a `\`-separated path —
  THEN both conduct-guard arms SHALL recognize it exactly as they
  recognize `git`/`gh`, blocking and allowing per R-641–R-645.
- **R-702** — The hooks.json command lines SHALL resolve the interpreter
  by trying `python3`, then `py`, then `python`, executing the first one
  present; WHILE none is present the command SHALL exit 0 (fail-open,
  the hooks contract). Linux/macOS behavior is byte-equivalent to today,
  proven by the existing harness staying green.
- **R-703** — The two link scripts SHALL be Python (`tools/link_skills.py`,
  `tools/link_opencode_plugin.py`), same modes and defaults as the bash
  versions they replace; WHERE creating a symlink fails (Windows without
  Developer Mode) they SHALL fail loud with the remedy named, never
  silently copy. The bash originals are removed; every reference updated.
- **R-704** — WHILE running on Windows (`os.name == "nt"`), check_engine's
  chmod-based unreadable-file test SHALL be skipped with a printed
  reason — never silently.
- **R-705** — WHEN `ps` is absent or fails, the dashboard SHALL treat the
  live-agent count as 0 without crashing; its `/tmp` literals SHALL go
  through `tempfile.gettempdir()`.
- **R-706** — `check_opencode_plugin.mjs` SHALL assert node ≥ 22.6 before
  importing the plugin, failing loud with the found version named.
- **R-707** — Every changed behavior with an existing harness SHALL show
  its new checks red against the unchanged code before green.

## The hurting case

GIVEN a legislated repo on its default branch, WHEN the agent runs
`git.exe merge topic` (or `C:\Program Files\Git\bin\git.exe push origin
master` — quoted as one word), THEN both arms block exactly as for `git`
— AND `ls -la`, a `github`-named binary, and every currently-green
harness check behave byte-identically to before the patch. The case that
hurts most: a launcher shim that changes Linux hook behavior — the fleet
runs on Linux today, and phase 1 must not wobble it.

## Deliverable

The six patches, their red-first checks, updated references
(plugin/README, tools), and the usual case artifacts.

## Clarifications

### Session 2026-08-26

- **Q: sizing — which half now?** → The light half (this spec's In list).
  C1+F1+D3 are v23 edition riders; E1/E2 rides the v23 benchmark cycle
  where the runner is exercised for real. No extra benchmark is spent on
  this case — the owner's standing economics (verification cost down,
  never up).

## Converge — 2026-08-26

Judged against R-701–R-707 and the boundary: R-701 shown red (3 py + 2
mjs) then green in both arms — the red also surfaced a latent
case-sensitivity bug in the pre-filter, fixed in the same stroke; R-702
proven by a harness check running the actual hooks.json command with only
`python` on PATH (red 127, green through the shim), Linux behavior
unchanged (whole harness green); R-703 ported both scripts with all modes
verified against temp dirs (link/check/prune/unlink, exit codes matched
to the bash originals) and the fail-loud symlink contract in place, bash
originals removed, live references updated (historical records left as
records); R-704 guarded loudly; R-705 guarded; R-706 asserted. Parked
half is recorded in the backlog with destinations, never dropped. The OKF
bundle is untouched (no concept changed; the codebase map's `tools/` row
names no filenames). Verification: all six rungs green (static, engine,
hooks, opencode, anchors, sdd-lint). Residual, stated: the launcher's
behavior under Claude Code's Windows hook execution cannot be observed
from this machine. Gaps: none (missing / partial / contradicts /
unrequested: none).

✅ Converged
