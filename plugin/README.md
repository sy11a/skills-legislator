# legislator-hooks

The deterministic enforcement arm of the legislator constitution: CLAUDE.md
and rule files are advisory (an agent can ignore them); these hooks are
guaranteed by the harness itself. See
`docs/superpowers/specs/2026-07-09-hooks-plugin-design.md` in the legislator
repo for the full design and rationale — this README covers what ships and
how to check it works.

The enforcement ships for **two harnesses** from one repo:

- **Claude Code** — `plugin/hooks/*.py` + `hooks/hooks.json` (PreToolUse /
  PostToolUse / Stop). Install below.
- **opencode** — `plugin/opencode/legislator-guard.ts` (a global opencode
  plugin). Install with `tools/link-opencode-plugin.sh`, which symlinks it
  into `~/.config/opencode/plugins/`. It is a silent no-op outside legislated
  repos, so loading it globally is safe.

### opencode event mapping

| Claude Code hook | opencode mapping |
|---|---|
| `guard_owned_files.py` (PreToolUse, blocks) | `tool.execute.before` on edit/write/patch → `throw` (opencode surfaces the message to the agent; verified to block) |
| `guard_git_conduct.py` (PreToolUse, blocks) | `tool.execute.before` on bash → `throw`; git state read from `.git` directly (no shell needed) |
| `format_on_edit.py` (PostToolUse, best-effort) | `tool.execute.after` on edit/write — dotnet-format / prettier, never blocks |
| `okf_sync_check.py` (Stop reminder) | `event` `session.idle` — warns via `client.app.log` |

**Accepted limitation of the opencode port:** opencode's event model has no
"Stop with feedback fed back to the model" equivalent, so the OKF-sync
reminder is **logged** (`client.app.log`, warn level) rather than
force-feeding the agent the way Claude Code's Stop `exit 2` does. The
write-guard — the load-bearing hook — is a true block in both harnesses.

### opencode tests

`node evals/check_opencode_plugin.mjs` — 26 deterministic checks (no agent,
no opencode runtime): owned-rule edits blocked for edit/write/patch, new
files under `docs/ai/rules/**` blocked, non-owned paths allowed, manifest
and `.claude/rules/**` intentionally unguarded, non-edit tools ignored,
non-legislated repos no-op, relative-path resolution, malformed-args safety;
plus the git-conduct block set (merge/push onto the default branch,
attribution in commit and PR text, `gh pr merge`) with its allow-side
controls.
Manual acceptance (real `opencode run` in a legislated repo): an edit of
`docs/ai/rules/core/okf.md` is blocked and the file's hash is unchanged.

## Install

Add this repo (or wherever `plugin/` is checked out) as a plugin source and
enable `legislator-hooks`, or symlink/copy `plugin/` to wherever your Claude
Code plugin loader expects it. `hooks/hooks.json` uses
`${CLAUDE_PLUGIN_ROOT}`-relative paths, so the plugin works from any install
location.

## The four hooks

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

- **`.claude/rules/**` (project law, per `core/project-rules.md`) is
  project-owned and intentionally unguarded** — the guard protects only the
  machine-managed fleet law under `docs/ai/rules/**`.
- **`docs/ai/manifest.json` itself is not guarded.** SKILL.md Step 3.7
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
- **A symlink planted inside `docs/ai/rules/` that points outside the repo
  bypasses the guard** (`Path.resolve()` follows it out of the legislated
  repo before the manifest walk). Same class as the Bash bypass: it requires
  a deliberate act legislator never performs, and audit's byte-diff catches
  the resulting drift after the fact.

### 2. `guard_git_conduct.py` — PreToolUse git conduct guard

Matcher: `Bash` (BL-064; requirements R-641–R-649 in
`docs/cases/BL-064-git-conduct-guard/`).

Parses the agent's command line (quote-aware tokenization, compound commands
split on `&& || ; | &`) and blocks, in legislated repos only:

- `git merge` while the current branch IS the default branch, and
  `gh pr merge` — merging is the user's act, whatever the channel;
- `git push` whose effect updates the default branch: an explicit refspec
  targeting it (`master`, `HEAD:master`, `:master`, `refs/heads/master`),
  `--all`/`--mirror`, or a bare/remote-only push while ON it;
- `git commit` / `gh pr create|edit` whose message/title/body carries an
  AI-attribution marker — a `Co-Authored-By:` trailer naming
  Claude/Anthropic, or a "Generated with …" footer. A human co-author
  trailer passes.

Default branch detection: `origin/HEAD` first, else the only one of
`main`/`master` that exists locally; both or neither → undecidable → allow.
`git -C <path>` is honored; `git merge --abort/--quit` is cleanup, not
merging, and passes.

**Fail-open by contract:** malformed input, unbalanced quotes, no git,
detached HEAD, unknown default branch, any exception → exit 0. The human
path is untouched by construction — hooks fire on the agent's tool calls,
never on the user's own terminal (`!`-prefixed commands included).

**Accepted limitations (by design, not bugs):**

- **Command-string inspection only.** A commit message supplied via `-F
  <file>` or an editor is not read; a push driven by a script the command
  merely names is not seen. The guard stops the *direct* act, which is how
  an agent actually performs these operations; review still catches the
  exotic path.
- **`gh pr merge` is blocked outright** — no attempt to check which branch
  the PR targets; merging any PR is the user's act under
  `core/pair-development.md`.

### 3. `format_on_edit.py` — PostToolUse format-on-edit

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

### 4. `okf_sync_check.py` — Stop-hook OKF-sync check

On session Stop: if the working tree has uncommitted changes under `src/**`
but none under `docs/okf/**`, exits 2 with a reminder — the enforcement arm
of `okf.md`'s sync law.

- **Loop-safe:** exits 0 immediately if the hook input carries
  `stop_hook_active: true` (Claude Code sets this when a Stop hook already
  fired for this stop) — the reminder fires at most once per stop.
- **Only active in legislated repos** — `docs/ai/manifest.json` checked at
  the git toplevel only (unlike hook 1's walk-up from the file path, so a
  legislated subdirectory of a larger monorepo gets the write-guard but not
  this reminder); exits 0 outside a legislated repo, and exits 0 outside a
  git repo entirely.

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
5. In a legislated repo checked out on its default branch, ask the agent to
   run `git merge <any-branch>` → the call is blocked with the
   "user's act" message; the same command typed by hand (`!`-prefix or a
   plain terminal) still runs.

## Automated tests

`evals/check_hooks.py` in the legislator repo covers the write-guard (3
cases), the git-conduct guard (22 cases: block and allow sides of merge,
push, commit/PR attribution, `gh pr merge`, plus the fail-open set),
format-on-edit (2 cases + defensive malformed-input cases), OKF-sync (4
cases + non-legislated/non-git cases), and `hooks.json` well-formedness.
Run `python3 evals/check_hooks.py` — seconds, no agent required.
