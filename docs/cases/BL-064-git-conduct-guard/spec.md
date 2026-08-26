# BL-064 — The git conduct guard: the highest-frequency unenforced law

**Tier: 1 (light).** Blast radius: every Bash `git`/`gh` invocation by an
agent in every legislated repo — but the arm is fail-open by the hooks
family's contract, so the failure mode of a guard bug is a missed block,
never stopped work. Novelty: first PreToolUse hook on the `Bash` matcher
(the existing guard matches file-editing tools); command-string parsing is
new territory for the plugin.

**Spec type: feature.** Branch `bl/064-git-conduct-guard`. Source: BL-047's
ranked list, group 1 (`docs/cases/BL-047-decision-inventory/inventory.md`).
No `skill/` change, no VERSION, no benchmark — enforcement arms only
(`plugin/`), verified by `evals/check_hooks.py` and
`evals/check_opencode_plugin.mjs`.

## The question it closes

pair-6 (never merge to the main branch yourself), pair-7 (no AI attribution
anywhere in the VCS record) and skl-3 (no skill commits/pushes/merges on its
own authority) are every-commit-cadence law with no deterministic arm — the
inventory's cheapest (b)→(a) move. This case gives them one.

## Boundary

**In:** a new PreToolUse hook on the `Bash` matcher
(`plugin/hooks/guard_git_conduct.py` + `hooks.json` registration), its
opencode port in `legislator-guard.ts`, and their checks in the two eval
harnesses.

**Out:** the immutability riders (adr-5 — never renumber/delete a past ADR;
sdd-12 — permanent `R-NNN` ids; life-4 — converged case files never
rewritten). They inspect the *staged diff*, not the command string — a
different mechanism with `-a`/`-A` blind spots and its own false-block
budget; they are sized as their own case once this arm has field history.
Also out: any change to law text (`skill/assets/rules/**`), any benchmark.

## Requirements

- **R-641** — WHEN the agent invokes Bash with a `git merge` command WHILE
  the current branch is the repository's default branch, in a legislated
  repo, the guard SHALL block the call (exit 2) with a message naming the
  pair-development rule.
- **R-642** — WHEN the agent invokes Bash with a `git push` command whose
  effect updates the default branch (an explicit `<remote> <default>` /
  `HEAD:<default>` refspec, or a bare/`-u` push while the current branch IS
  the default), in a legislated repo, the guard SHALL block the call
  (exit 2).
- **R-643** — WHEN a `git commit` (or `git commit --amend`) command's
  message arguments carry an AI-attribution marker — a `Co-Authored-By:`
  trailer naming Claude/Anthropic, or a "Generated with" attribution line —
  the guard SHALL block the call (exit 2). A `Co-Authored-By:` trailer
  naming a human co-author SHALL pass.
- **R-644** — WHEN a `gh pr create` / `gh pr edit` command's title/body
  arguments carry the same attribution markers, the guard SHALL block the
  call (exit 2).
- **R-645** — WHEN the agent invokes Bash with a `gh pr merge` command, in
  a legislated repo, the guard SHALL block the call (exit 2) — merging is
  the user's act whatever the channel; the user's own terminal (`! gh pr
  merge`) and the GitHub UI stay open.
- **R-646** — WHILE the input is malformed, the command is not a git/gh
  invocation, the repo state is undecidable (no git, detached HEAD, unknown
  default branch), or any exception occurs, the guard SHALL exit 0 — every
  "can't tell" allows; a hook bug must never stop the user's work (the
  hooks family contract).
- **R-647** — WHILE the working directory is not inside a legislated repo
  (no `docs/ai/manifest.json` up the tree), the guard SHALL exit 0 without
  further inspection.
- **R-648** — The opencode plugin SHALL mirror R-641–R-645 for its bash
  tool via `tool.execute.before` (throw = block), with the same fail-open
  contract, verified by `check_opencode_plugin.mjs`.
- **R-649** — Every new check in the two harnesses SHALL be shown failing
  against the unchanged plugin before it is shown passing (red-first,
  `evals/POLICY.md`'s discipline applied to the arm's own tests).

## The hurting case

GIVEN a legislated repo whose default branch is `master`, checked out on
`master`, WHEN the agent runs `git merge bl/064-git-conduct-guard`, THEN the
call is blocked with exit 2 and stderr names the rule — AND the same merge
command run from a feature branch (merging master *into* the feature branch)
passes, AND `git commit -m "fix: ordinary message"` passes untouched.
The case that hurts most is the false block: an over-eager pattern that
stops every commit would get the whole plugin uninstalled.

## Deliverable

`plugin/hooks/guard_git_conduct.py`, the `hooks.json` Bash matcher entry,
the `legislator-guard.ts` port, new checks in `evals/check_hooks.py` and
`evals/check_opencode_plugin.mjs`, README rows for the new hook, and the
usual case artifacts (backlog flip, changelog, journal, OKF).

## Clarifications

### Session 2026-08-26

- **Q: immutability riders (adr-5, life-4, sdd-12) here or separate?** →
  Deferred. This case is the command-string guard only; the riders inspect
  the staged diff — a different mechanism with `-a`/`-A` blind spots and its
  own false-block budget — and are sized as their own case once this arm has
  field history.
- **Q: attribution guard scope — legislated repos or everywhere?** →
  Legislated repos only, like the whole hooks family (manifest-gated). The
  plugin's "silent no-op outside legislated repos" contract stands; outside
  the fleet the owner's global instructions remain behavioral.
- **Q: block `gh pr merge` too?** → Yes (R-645). "Never merge to the main
  branch yourself" covers the GitHub channel as well; otherwise the guard is
  bypassed by one command. The user merges via the UI or their own terminal.

## Converge — 2026-08-26

Judged against R-641–R-649 and the boundary: every requirement has both its
block side and its allow-side control in `evals/check_hooks.py` (22 checks)
and, mirrored per R-648, in `evals/check_opencode_plugin.mjs` (14 checks);
the riders stayed out per the clarification; no law text, no VERSION, no
benchmark. R-649 (red-first) held with one honest caveat: the exit-2
asserts were incidentally green against the missing script (python exits 2
on a file it cannot open), so the discriminating red evidence is the
allow-side and message-content asserts — 13 red in check_hooks.py, 6 in
the mjs harness, all green only after the arm existed. Verification:
check_static, check_engine, check_hooks, check_opencode_plugin, engine
anchors and sdd-lint all clean; live smoke in this repo (member #0, on the
task branch): `git push origin master` blocked, task-branch push and an
ordinary commit untouched. Gaps: none (missing / partial / contradicts /
unrequested: none).

✅ Converged
