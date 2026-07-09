---
name: legislator
description: Scaffold or upgrade the AI-development constitution (CLAUDE.md rules, OKF knowledge bundle, backlog, ADRs, dev journal, changelog, specs/plans) in the current project repo. Use when starting a new AI-first project, migrating an existing repo to the standard layout, or re-applying an updated constitution after editing the Legislator's rule files.
---

# Legislator

Run this procedure against the **current working directory** (the target project repo). Never write outside it. Never commit — leave the diff for the user to review. `<skill-path>` in the commands below means this skill's install directory (typically `~/.claude/skills/legislator`, a symlink to the skill package).

## Step 0 — Preconditions

1. Confirm the current directory looks like a project root (has a `.git` directory, or the user has explicitly confirmed this directory is the target). If neither is true, ask the user to confirm before proceeding.
2. Run `git status --porcelain`. If it reports anything, warn the user that uncommitted changes exist and ask whether to proceed (a run's diff would mix with their changes) before continuing.

## Step 1 — Detect state

Check, in this order:

1. Does `docs/ai/manifest.json` exist? → **upgrade** mode. Read it for the previously applied `legislatorVersion`, `profiles`, and `ownedFiles`.
2. Else, does `CLAUDE.md` exist at the repo root? → **legacy migration** mode.
3. Else → **fresh scaffold** mode.

**Edge case — manifest missing but CLAUDE.md already carries the import block:** before treating a manifest-less repo as legacy migration, check whether `CLAUDE.md` already contains the `@docs/ai/rules/core/okf.md` import line. If it does, the repo was already legislated at some point and the manifest was deleted or never committed — do not re-run the CLAUDE.md migration logic in Step 5 (it is not idempotent against an already-migrated file). Instead: inspect `docs/ai/rules/` for the owned files already present, reconstruct `profiles` from which `docs/ai/rules/stacks/<name>/` directories exist, reconstruct `ownedFiles` from the files actually on disk under `docs/ai/rules/`, ask the user to confirm the reconstructed profile list, then proceed as upgrade mode from Step 3 onward (skip Step 5 entirely).

## Step 2 — Determine profiles

- **Upgrade mode:** use `profiles` from the existing manifest. Do not ask again — unless the user's request explicitly asks to add or drop a stack (e.g. "we added an Aurelia frontend, pick that up"). In that case, re-run the stack-signal detection below, present the updated candidate list alongside the existing `profiles`, and ask the user to confirm the new list. Store the confirmed list as the new `profiles`; Step 3.5's deletion logic then removes any stack's owned files that are no longer in the confirmed list, exactly as it does for an edited/removed core rule.
- **Fresh scaffold / legacy migration:** inspect the repo for stack signals — any `*.slnx`/`*.sln`/`*.csproj` file → candidate profile `dotnet`; an `aurelia` entry in `package.json` dependencies, or an `aurelia_project/aurelia.json` file (Aurelia v1 convention) → candidate profile `aurelia`. Present the detected candidates to the user and ask them to confirm or adjust before proceeding. Store the confirmed list as `profiles`.

## Step 3 — Apply owned files

Owned files live in this skill package at `assets/rules/`. For each file:

1. Always copy every file under `assets/rules/core/` to `docs/ai/rules/core/` in the target repo, preserving relative path, using a byte-for-byte copy operation (e.g. `cp`) — never retype or reformat the content.
2. For each confirmed profile name in `profiles`, copy `assets/rules/stacks/<profile>/` to `docs/ai/rules/stacks/<profile>/` the same way.
3. Read `VERSION` (a single integer) — this is the `legislatorVersion` to write into the manifest.
4. Compute the new `ownedFiles` list: every path just copied, expressed relative to the target repo root (e.g. `docs/ai/rules/core/okf.md`).
5. **Deletions:** compare the new `ownedFiles` list against the "old" `ownedFiles` list — the existing manifest's list in upgrade mode, or the reconstructed list from Step 1's edge-case guard when there was no manifest to read. If the old list contains a path not present in the new list, delete that file from the target repo (it was removed from the constitution, or its stack profile was de-selected in Step 2). If deleting a file empties its containing `docs/ai/rules/stacks/<profile>/` directory, remove the now-empty directory too.
6. Write `docs/ai/manifest.json` with this exact serialization — 2-space indentation, keys in this order (`legislatorVersion`, `profiles`, `ownedFiles`), `ownedFiles` sorted — so that two runs with no actual constitution change produce a byte-identical file and no spurious diff:

```json
{
  "legislatorVersion": <int from VERSION>,
  "profiles": [<confirmed profile names>],
  "ownedFiles": [<new ownedFiles list, sorted>]
}
```

## Step 4 — Scaffold missing project-owned artifacts

For each of the following, create it **only if it does not already exist** — never overwrite. Use the templates in `assets/templates/`, filling placeholders as described below. Never invent content for a placeholder without either asking the user or deriving it from the repo (see the derivation rules below) — do not leave a placeholder token unfilled in the written file. (The `{{...}}` tokens inside `adr-template.md.tpl` itself are the one exception: that file is a reusable template for *future* ADRs the project will write, so its tokens are intentional fill-in-later guidance for humans and must be copied through unresolved — do not attempt to fill them.)

| Target path | Template | Notes |
|---|---|---|
| `CLAUDE.md` | `CLAUDE.md.tpl` | Only in fresh-scaffold mode — legacy migration mode handles this file per Step 5 instead |
| `docs/okf/index.md` | `okf-index.md.tpl` | Fresh scaffold: fill placeholders per the derivation rules below. Legacy migration: `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, and `{{STACK_SUMMARY}}` are derived the same way Step 5 derives them for CLAUDE.md (from the existing CLAUDE.md's own overview/stack content); `{{CATEGORY_MAPPING_TABLE}}` is filled by Step 5.2's table extraction; `{{TODAY_ISO}}` is mode-independent — always today's date/time in ISO 8601, per the derivation rules below, regardless of mode. Run Step 5 before scaffolding this file in migration mode, since Step 5 is what supplies the first three values. |
| `docs/okf/log.md` | `okf-log.md.tpl` | |
| `docs/backlog.md` | `backlog.md.tpl` | |
| `docs/adr/0001-record-architecture-decisions.md` | `adr-0001.md.tpl` | Used verbatim, no placeholders |
| `docs/adr/template.md` | `adr-template.md.tpl` | Copied verbatim — its `{{...}}` tokens are intentional and must NOT be filled in (see the note above the table) |
| `docs/journal/README.md` | `journal-README.md.tpl` | Used verbatim, no placeholders |
| `CHANGELOG.md` | `changelog.md.tpl` | Used verbatim, no placeholders |
| `docs/superpowers/specs/` | (empty directory) | Create the directory if absent; no file |
| `docs/superpowers/plans/` | (empty directory) | Create the directory if absent; no file |

Placeholder derivation rules (fresh-scaffold mode only, except `{{TODAY_ISO}}` and `{{TODAY_ISO_DATE}}` which are always mode-independent — legacy migration extracts the rest of these from the existing CLAUDE.md instead, per Step 5 and the `docs/okf/index.md` row above):

- `{{PROJECT_NAME}}` — ask the user, or infer from the repo directory name if unambiguous, and confirm with the user before writing.
- `{{PROJECT_OVERVIEW}}` — ask the user for a one-paragraph description.
- `{{STACK_SUMMARY}}` — derive from `profiles` (e.g. "`.NET`" for `dotnet`, "`.NET, Aurelia`" for both) plus anything the user adds.
- `{{STACK_IMPORTS}}` — one `@docs/ai/rules/stacks/<profile>/<file>.md` line per file under each confirmed profile's rule directory.
- `{{PROJECT_ARCHITECTURE_NOTES}}` — ask the user for any project-specific architecture constraints beyond the stack rules; if none, write "None beyond the stack rules imported above."
- `{{BUILD_TEST_COMMANDS}}` — ask the user for the build/test commands (e.g. `dotnet build`, `dotnet test`), or detect from solution/project files and confirm.
- `{{TODAY_ISO}}` — today's date/time in ISO 8601 (e.g. `2026-07-08T00:00:00Z`).
- `{{TODAY_ISO_DATE}}` — today's date only (e.g. `2026-07-08`).
- `{{CATEGORY_MAPPING_TABLE}}` — ask the user for the project's feature-slice-to-OKF-category mapping (mirroring the pattern: `| Change | OKF file to update |`); if the project has no slices yet, write a single row pointing everything at `docs/okf/index.md`.

## Step 5 — Legacy migration (migration mode only)

Follow `references/migration.md` in full. Summary of what it covers:

1. Split the existing CLAUDE.md into project-specific content (kept) and content now covered by an owned rule (removed, replaced by the `@docs/ai/rules/...` import block from the `CLAUDE.md.tpl` import section).
2. Extract any existing "what maps to what" / category table from the old CLAUDE.md. If `docs/okf/index.md` already exists and already has an equivalent table, leave it untouched — do not write `docs/okf/index.md` at all. Otherwise, derive `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, `{{STACK_SUMMARY}}` from the old CLAUDE.md and the extracted table as `{{CATEGORY_MAPPING_TABLE}}`, then hand these four values to Step 4 — Step 4 is the only step that actually writes `docs/okf/index.md` from `okf-index.md.tpl`; this step only supplies its placeholder values.
3. Relocate any plans/specs directory outside the standard location (e.g. `.claude/plans/`) into `docs/superpowers/plans/`, fixing any relative references inside the moved files.
4. Remove `docs/superpowers/` (or equivalent) from `.gitignore` if present.
5. If an existing CLAUDE.md section conflicts with an owned rule (e.g. contradictory branch-naming convention), do not resolve it — surface the conflict and ask, per the decision-gate rule.

Run this step before scaffolding `docs/okf/index.md` in Step 4 — Step 4's `docs/okf/index.md` row depends on the project-name/overview/stack-summary/category-table values this step derives.

## Step 6 — Verify

1. For every file in the new `ownedFiles` list, diff it against the corresponding `assets/rules/...` source file (e.g. `diff docs/ai/rules/core/okf.md <skill-path>/assets/rules/core/okf.md`) — must be byte-identical. If any diff is non-empty, re-copy and check again; if it still fails, stop and report the failure instead of continuing.
2. Confirm every artifact from Step 4's table exists (or was already present).

## Step 7 — Report

Print a summary with four sections: **Created** (new files/directories), **Overwritten** (owned files updated by this run), **Deleted** (owned files removed because they left the constitution or a de-selected stack profile), **Needs your review** (e.g. a proposed `@import` line to add/remove from CLAUDE.md when a rule file was added/removed — CLAUDE.md is project-owned, so the Legislator never edits it directly; it only proposes the exact line here for the user to add or delete themselves).

Do not run `git add` or `git commit`. The user reviews and commits.
