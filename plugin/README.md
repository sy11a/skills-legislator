# legislator-hooks

The deterministic enforcement arm of the legislator constitution: CLAUDE.md
and rule files are advisory (an agent can ignore them); these three hooks are
guaranteed by Claude Code itself. See
`docs/superpowers/specs/2026-07-09-hooks-plugin-design.md` in the legislator
repo for the full design and rationale — this README covers what ships and
how to check it works.

## Install

Add this repo (or wherever `plugin/` is checked out) as a plugin source and
enable `legislator-hooks`, or symlink/copy `plugin/` to wherever your Claude
Code plugin loader expects it. `hooks/hooks.json` uses
`${CLAUDE_PLUGIN_ROOT}`-relative paths, so the plugin works from any install
location.

## The three hooks

### 1. `guard_owned_files.py` — PreToolUse write-guard

Matcher: `Edit|Write|MultiEdit|NotebookEdit`.

Reads the tool call's file path and walks up the directory tree looking for
`docs/ai/manifest.json` (the "is this a legislated repo?" test). If found,
and the file lies under that repo's `docs/ai/rules/**`, the edit is
**blocked** (exit 2): "docs/ai/rules/** is machine-managed law — edit the
legislator skill source and re-run /legislator instead." Everywhere else
(non-legislated repos, files outside `docs/ai/rules/**`) it's a silent
no-op.

**Accepted limitations (by design, not bugs):**

- **`docs/ai/manifest.json` itself is not guarded.** SKILL.md Step 3.6
  rewrites the manifest with the `Write` tool on every legislator run, and
  that rewrite already heals hand-edits to it every run. Guarding it would
  block legislator's own upgrade runs.
- **A deliberate Bash write bypasses the guard** (`sed -i`, `>`, `cat >>`,
  …). This hook only intercepts the file-editing tools (`Edit`/`Write`/
  `MultiEdit`/`NotebookEdit`); Bash is never inspected. That asymmetry is the
  mechanism, not an oversight: legislator's own owned-file updates are a
  `cp`-based Bash operation, so guarding Bash writes would also block
  legislator itself. The guard's job is stopping the *accidental* mid-session
  hand-edit, which arrives via Edit/Write; audit mode's byte-diff check still
  detects a Bash-authored drift after the fact.

### 2. `format_on_edit.py` — PostToolUse format-on-edit

Matcher: `Edit|Write|MultiEdit`, 10s timeout (set in `hooks.json`).

Best-effort, per-file formatting after an edit:

- `.cs` file + `dotnet` on PATH + a `.sln`/`.csproj` found upward → runs
  `dotnet format <project> --include <file>`.
- `.ts`/`.tsx`/`.js`/`.jsx`/`.html`/`.css` + a prettier config found upward
  (`.prettierrc*`, `prettier.config.js`, or a `"prettier"` key in
  `package.json`) + `npx` on PATH → runs `npx prettier --write <file>`.
- Anything else, or missing toolchain/config → silent no-op.

**Never blocks, never fails.** Every branch ends in exit 0; formatter errors,
timeouts, and missing toolchains are all swallowed. This is polish, not a
gate.

**Accepted limitation:** deleting the corresponding mechanically-enforced
style rules from `coding-standards.md` (the natural next step once a
formatter runs on every edit) is explicitly **out of scope** for this hooks
track — that's a rule-content change requiring a VERSION bump and a full
e2e benchmark, logged as a follow-up rider for the next benchmarked rules
cycle.

### 3. `okf_sync_check.py` — Stop-hook OKF-sync check

On session Stop: if the working tree has uncommitted changes under `src/**`
but none under `docs/okf/**`, exits 2 with a reminder — the enforcement arm
of `okf.md`'s sync law.

- **Loop-safe:** exits 0 immediately if the hook input carries
  `stop_hook_active: true` (Claude Code sets this when a Stop hook already
  fired for this stop) — the reminder fires at most once per stop.
- **Only active in legislated repos** (same manifest-walk test as hook 1);
  exits 0 outside a legislated repo, and exits 0 outside a git repo
  entirely.

**Accepted limitation:** scope is uncommitted working-tree state only
(`git status --porcelain`). Changes already committed mid-session, with the
matching OKF update in a *different* still-pending state, are out of reach
without transcript parsing — this is a deliberate scope cut, not a bug.

## Manual acceptance checklist

Run once after installing, in a real legislated repo:

1. Hand-edit a file under `docs/ai/rules/**` with the `Edit` tool → the edit
   is blocked with the "machine-managed law" message.
2. Run a legislator upgrade (`/legislator`) in the same repo → it still
   completes; its `cp`-based owned-file writes are unaffected (Bash is
   unguarded by design).
3. Edit a `.cs` file in a repo with a `.sln`/`.csproj` and `dotnet` on PATH →
   the file comes back formatted.
4. In a legislated repo, touch a file under `src/` and end the turn without
   touching `docs/okf/**` → the Stop-hook reminder appears.

## Automated tests

`evals/check_hooks.py` in the legislator repo covers the guard (3 cases),
format-on-edit (2 cases + defensive malformed-input cases), OKF-sync (4
cases + non-legislated/non-git cases), and `hooks.json` well-formedness.
Run `python3 evals/check_hooks.py` — seconds, no agent required.
