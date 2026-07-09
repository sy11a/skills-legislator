# Hooks Plugin — Design (BL-007)

**Date:** 2026-07-09
**Status:** Draft — awaiting Gate 1 review
**Backlog:** BL-007 (docs/backlog.md)
**Gate 0 decision (user-settled):** the write-guard covers file-editing
tools only (Edit/Write/MultiEdit/NotebookEdit); legislator's own `cp`-based
owned-file updates pass because Bash is not guarded — the asymmetry IS the
mechanism. No env-flag escape, no Bash command inspection.

## Goal

Give the constitution a deterministic enforcement arm. CLAUDE.md and rule
files are advisory (an agent can ignore them); hooks are guaranteed. Three
hooks, delivered as a plugin (settled earlier with the user: a
`.claude/settings.json` fragment would break the owned/project split; a
plugin ships hooks versioned, per machine).

## Package layout (in this repo — the skeleton BL-008 extends)

```
plugin/
  .claude-plugin/plugin.json      # name "legislator-hooks", version, description
  hooks/
    hooks.json                    # event wiring, ${CLAUDE_PLUGIN_ROOT} paths
    guard_owned_files.py          # hook 1
    format_on_edit.py             # hook 2
    okf_sync_check.py             # hook 3
```

Hook scripts are Python 3 stdlib-only (JSON on stdin is the hook contract;
python parses it robustly, and the scripts become directly testable by
piping fixtures). Format mirrors real published plugins
(claude-plugins-official/security-guidance): `hooks.json` maps events to
`{"type": "command", "command": "python3 \"${CLAUDE_PLUGIN_ROOT}/hooks/<script>.py\""}`
with tool matchers.

## Hook 1 — PreToolUse write-guard on owned law

- Matcher: `Edit|Write|MultiEdit|NotebookEdit`.
- Reads the tool input's `file_path`; walks up from it looking for
  `docs/ai/manifest.json` (the "am I in a legislated repo?" test). If found
  and the file lies under that repo's `docs/ai/rules/`, **block** (exit 2)
  with: "docs/ai/rules/** is machine-managed law — edit the legislator
  skill source and re-run /legislator instead."
- Guards `docs/ai/rules/**` only, **not** `docs/ai/manifest.json`: SKILL.md
  Step 3.6 writes the manifest with the Write tool, so guarding it would
  block legislator's own runs (and Step 3.6's rewrite already heals manifest
  hand-edits every run). Deliberate scope cut; revisit only if manifest
  vandalism shows up in practice.
- Non-legislated repos: no manifest found → exit 0, zero interference.
- Known gap, accepted at Gate 0: a deliberate Bash write (`sed -i`, `>`)
  bypasses the guard. Audit check 3 (byte-diff vs. source) still detects the
  drift after the fact; the guard's job is stopping the *accidental*
  mid-session hand-edit, which arrives via Edit/Write.

## Hook 2 — PostToolUse format-on-edit

- Matcher: `Edit|Write|MultiEdit`.
- `.cs` file and a `dotnet` binary on PATH: run
  `dotnet format <nearest .sln/.csproj upward> --include <file>` (whitespace
  + style). `.ts`/`.js`/`.html`/`.css` and a prettier config upward: run
  `npx prettier --write <file>`.
- **Never blocks, never fails:** every branch ends exit 0; formatter errors
  and missing toolchains are swallowed; a hard timeout (10 s) is set in
  hooks.json so a cold `dotnet format` can't stall the session.
- The backlog's consequence — deleting mechanically-enforced style rules
  from `coding-standards.md` — is **out of scope here**: that edits
  `assets/rules/**` (VERSION bump + full e2e benchmark), while this track
  validates with hook tests only. Logged as a follow-up rider for the next
  benchmarked rules cycle.

## Hook 3 — Stop-hook OKF-sync check

- On Stop: if the working tree has changes under `src/**` but none under
  `docs/okf/**`, exit 2 with "src/ changed but docs/okf/ didn't — update the
  OKF (map/log/glossary) or state why no update is needed." — the
  enforcement arm of okf.md's sync law.
- Loop-safe: if the hook input carries `stop_hook_active: true` (Claude Code
  sets it when a Stop hook already fired this stop), exit 0 — the reminder
  fires at most once per stop.
- Scope: uncommitted working-tree state only (`git status --porcelain`).
  Changes committed mid-session with the OKF update in a *different* pending
  state are out of reach without transcript parsing — accepted limitation,
  documented in the plugin README.
- Only active in legislated repos (same manifest test as hook 1); exit 0
  elsewhere, and exit 0 when not in a git repo at all.

## Testing — hook tests, not e2e benchmark (per the queue)

`evals/check_hooks.py` — seconds, no agent, same spirit as
`check_static.py`:

- **Guard:** temp legislated repo (manifest + rules file) — Edit JSON
  targeting `docs/ai/rules/core/x.md` → exit 2 with the message; targeting
  `src/a.cs` → exit 0; same rules path in a repo *without* a manifest →
  exit 0.
- **Format:** `.cs` edit JSON with no dotnet project → exit 0, no output;
  non-code file → exit 0. (Real-formatter runs are environment-dependent —
  smoke-tested manually, not asserted.)
- **OKF-sync:** temp git repo — dirty `src/` only → exit 2; dirty `src/` +
  `docs/okf/` → exit 0; `stop_hook_active: true` → exit 0; clean tree →
  exit 0.
- **hooks.json well-formed:** parses, every referenced script exists, every
  matcher names real tools.

Manual acceptance (documented in plugin README, run once): install locally,
verify a hand-edit to an owned rule is blocked and a legislator upgrade run
still completes (its `cp` path unguarded by design).

## Out of scope

- Marketplace packaging / distribution (BL-008 — extends this skeleton).
- Deleting machine-enforced style rules from `coding-standards.md` (rules
  cycle rider, see hook 2).
- Guarding Bash writes or the manifest (Gate 0 / manifest rationale above).
- Any SKILL.md change — this track touches `plugin/` and `evals/` only,
  which is exactly what makes it safe to run in a parallel worktree.

## Verification

`python3 evals/check_hooks.py` green; `check_static.py` unaffected. No e2e
benchmark: no file under `skill/` changes in this track.
