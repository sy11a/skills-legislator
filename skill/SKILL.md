---
name: legislator
description: Scaffold or upgrade the AI-development constitution (CLAUDE.md rules, OKF knowledge bundle, backlog, ADRs, dev journal, changelog, specs/plans) in the current project repo. Use when starting a new AI-first project, migrating an existing repo to the standard layout, or re-applying an updated constitution after editing the Legislator's rule files.
---

# Legislator

Run this procedure against the **current working directory** (the target project repo). Never write outside it. Never commit — leave the diff for the user to review. `<skill-path>` in the commands below means this skill's install directory (typically `~/.claude/skills/legislator`, a symlink to the skill package).

If the user's request is to **audit** or health-check the existing AI layer (no scaffolding or upgrading intent), do not run Steps 0–7 — follow the **Audit — read-only health check** section at the end of this file instead.

## Step 0 — Preconditions

1. Confirm the current directory looks like a project root (has a `.git` directory, or the user has explicitly confirmed this directory is the target). If neither is true, ask the user to confirm before proceeding.
2. Run `git status --porcelain`. If it reports anything, warn the user that uncommitted changes exist and ask whether to proceed (a run's diff would mix with their changes) before continuing.

## Step 1 — Detect state

Check, in this order:

1. Does `docs/ai/manifest.json` exist? → **upgrade** mode. Read it for the previously applied `legislatorVersion`, `profiles`, `keep`, and `ownedFiles`.
2. Else, does `CLAUDE.md` exist at the repo root? → **legacy migration** mode.
3. Else → **fresh scaffold** mode.

**Edge case — manifest missing but CLAUDE.md already carries the import block:** before treating a manifest-less repo as legacy migration, check whether `CLAUDE.md` already contains the `@docs/ai/rules/core/okf.md` import line. If it does, the repo was already legislated at some point and the manifest was deleted or never committed — do not re-run the CLAUDE.md migration logic in Step 5 (it is not idempotent against an already-migrated file). Instead: inspect `docs/ai/rules/` for the owned files already present, reconstruct `profiles` from which `docs/ai/rules/stacks/<name>/` directories exist, reconstruct `ownedFiles` from the files actually on disk under `docs/ai/rules/`, default `keep` to `[]` (there is no manifest to carry it from), ask the user to confirm the reconstructed profile list, then proceed as upgrade mode from Step 3 onward (skip Step 5 entirely).

## Step 2 — Determine profiles

- **Upgrade mode:** use `profiles` from the existing manifest. Do not ask again — unless the user's request explicitly asks to add or drop a stack (e.g. "we added an Aurelia frontend, pick that up"). In that case, re-run the stack-signal detection below, present the updated candidate list alongside the existing `profiles`, and ask the user to confirm the new list. Store the confirmed list as the new `profiles`; Step 3.5's deletion logic then removes any stack's owned files that are no longer in the confirmed list, exactly as it does for an edited/removed core rule.
- **Fresh scaffold / legacy migration:** inspect the repo for stack signals — any `*.slnx`/`*.sln`/`*.csproj` file → candidate profile `dotnet`; an `aurelia` entry in `package.json` dependencies, or an `aurelia_project/aurelia.json` file (Aurelia v1 convention) → candidate profile `aurelia`. Present the detected candidates to the user and ask them to confirm or adjust before proceeding. Store the confirmed list as `profiles`.

## Step 3 — Apply owned files

Owned files live in this skill package at `assets/rules/`. For each file:

1. Always copy every file under `assets/rules/core/` to `docs/ai/rules/core/` in the target repo, preserving relative path, using a byte-for-byte Bash copy (`cp`) — never the Write or Edit tools, and never retype or reformat the content. (The legislator-hooks write-guard blocks Edit/Write on `docs/ai/rules/**` in legislated repos; the Bash copy is the sanctioned path.)
2. For each confirmed profile name in `profiles`, copy `assets/rules/stacks/<profile>/` to `docs/ai/rules/stacks/<profile>/` the same way.
3. Read `VERSION` (a single integer) — this is the `legislatorVersion` to write into the manifest.
4. Compute the new `ownedFiles` list: every path just copied, expressed relative to the target repo root (e.g. `docs/ai/rules/core/okf.md`).
5. **Deletions:** compare the new `ownedFiles` list against the "old" `ownedFiles` list — the existing manifest's list in upgrade mode, or the reconstructed list from Step 1's edge-case guard when there was no manifest to read. If the old list contains a path not present in the new list, delete that file from the target repo (it was removed from the constitution, or its stack profile was de-selected in Step 2). If deleting a file empties its containing `docs/ai/rules/stacks/<profile>/` directory, remove the now-empty directory too.
6. **Keep list — protected project artifacts.** Carry forward the existing manifest's `keep` list (default to `[]` when the key is absent — older manifests predate it). Then apply any keep requests in the user's prompt: **adding** requires the path to exist in the repo (if it doesn't, skip it and report the refusal in Step 7) and to not be an owned file under `docs/ai/rules/` (owned files are machine-managed, not keepable); entries dedupe by path — re-marking an already-kept path replaces its reason. **Remove** an entry only when the user explicitly asks. Never add or remove keep entries on your own initiative. Kept files are project-owned content — the keep list is manifest metadata about them; it changes nothing about how this skill treats the file (project-owned files are never touched anyway). Its meaning is downstream: audit check 10 verifies kept files stay wired into the layer, and any future restructuring must leave kept paths in place.
7. Write `docs/ai/manifest.json` with this exact serialization — 2-space indentation, keys in this order (`legislatorVersion`, `profiles`, `keep`, `ownedFiles`), `profiles` as a single-line inline array (e.g. `["dotnet"]` or `["dotnet", "aurelia"]` — never expanded across multiple lines, regardless of how many entries it has), `keep` written as `[]` on the same line as its key when empty and otherwise one entry per line sorted by `path`, each entry a single-line inline object with keys in the order `path`, `reason`, and `ownedFiles` sorted with each entry on its own line as shown below — so that two runs with no actual change produce a byte-identical file and no spurious diff:

```json
{
  "legislatorVersion": <int from VERSION>,
  "profiles": [<confirmed profile names>],
  "keep": [
    {"path": "<repo-relative path>", "reason": "<why this artifact must stay as-is>"}
  ],
  "ownedFiles": [<new ownedFiles list, sorted>]
}
```

With no kept files that line is exactly `  "keep": [],` — never an expanded empty array.

## Step 4 — Scaffold missing project-owned artifacts

For each of the following, create it **only if it does not already exist** — never overwrite. Use the templates in `assets/templates/`, filling placeholders as described below. Never invent content for a placeholder without either asking the user or deriving it from the repo (see the derivation rules below) — do not leave a placeholder token unfilled in the written file. (The `{{...}}` tokens inside `adr-template.md.tpl` itself are the one exception: that file is a reusable template for *future* ADRs the project will write, so its tokens are intentional fill-in-later guidance for humans and must be copied through unresolved — do not attempt to fill them.)

| Target path | Template | Notes |
|---|---|---|
| `CLAUDE.md` | `CLAUDE.md.tpl` | Only in fresh-scaffold mode — legacy migration mode handles this file per Step 5 instead |
| `docs/okf/index.md` | `okf-index.md.tpl` | Fresh scaffold: fill placeholders per the derivation rules below. Legacy migration: `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, and `{{STACK_SUMMARY}}` are derived the same way Step 5 derives them for CLAUDE.md (from the existing CLAUDE.md's own overview/stack content); `{{CATEGORY_MAPPING_TABLE}}` is filled by Step 5.2's table extraction; `{{TODAY_ISO}}` is mode-independent — always today's date/time in ISO 8601, per the derivation rules below, regardless of mode. Run Step 5 before scaffolding this file in migration mode, since Step 5 is what supplies the first three values. |
| `docs/okf/log.md` | `okf-log.md.tpl` | |
| `docs/okf/codebase-map.md` | `codebase-map.md.tpl` | `{{CODEBASE_MAP_TABLE}}` per the derivation rules below; `{{PROJECT_NAME}}` and `{{TODAY_ISO}}` as usual |
| `docs/okf/glossary.md` | `glossary.md.tpl` | Seeded with an intentionally empty table — terms grow under the okf.md sync rule; `{{PROJECT_NAME}}` and `{{TODAY_ISO}}` as usual |
| `docs/backlog.md` | `backlog.md.tpl` | |
| `docs/adr/0001-record-architecture-decisions.md` | `adr-0001.md.tpl` | Used verbatim, no placeholders |
| `docs/adr/template.md` | `adr-template.md.tpl` | Copied verbatim — its `{{...}}` tokens are intentional and must NOT be filled in (see the note above the table) |
| `docs/journal/README.md` | `journal-README.md.tpl` | Used verbatim, no placeholders |
| `CHANGELOG.md` | `changelog.md.tpl` | Used verbatim, no placeholders |
| `docs/superpowers/specs/` | (empty directory) | Create the directory if absent; no file |
| `docs/superpowers/plans/` | (empty directory) | Create the directory if absent; no file |

Placeholder derivation rules (fresh-scaffold mode only, except `{{TODAY_ISO}}`, `{{TODAY_ISO_DATE}}`, `{{CODEBASE_MAP_TABLE}}`, and `{{BOUNDARIES}}`, which are mode-independent — legacy migration extracts the rest of these from the existing CLAUDE.md instead, per Step 5 and the `docs/okf/index.md` row above; the mode-independent four are always derived the same way regardless of mode):

- `{{PROJECT_NAME}}` — ask the user, or infer from the repo directory name if unambiguous, and confirm with the user before writing.
- `{{PROJECT_OVERVIEW}}` — ask the user for a one-paragraph description.
- `{{STACK_SUMMARY}}` — derive from `profiles` (e.g. "`.NET`" for `dotnet`, "`.NET, Aurelia`" for both) plus anything the user adds.
- `{{STACK_IMPORTS}}` — one `@docs/ai/rules/stacks/<profile>/<file>.md` line per file under each confirmed profile's rule directory.
- `{{PROJECT_ARCHITECTURE_NOTES}}` — ask the user for any project-specific architecture constraints beyond the stack rules; if none, write "None beyond the stack rules imported above."
- `{{BUILD_TEST_COMMANDS}}` — ask the user for the build/test commands (e.g. `dotnet build`, `dotnet test`), or detect from solution/project files and confirm.
- `{{TODAY_ISO}}` — today's date/time in ISO 8601 (e.g. `2026-07-08T00:00:00Z`).
- `{{TODAY_ISO_DATE}}` — today's date only (e.g. `2026-07-08`).
- `{{CATEGORY_MAPPING_TABLE}}` — ask the user for the project's feature-slice-to-OKF-category mapping (mirroring the pattern: `| Change | OKF file to update |`); if the project has no slices yet, write a single row pointing everything at `docs/okf/index.md`.
- `{{CODEBASE_MAP_TABLE}}` — list the repo's actual top-level directories, draft a one-line description of each from its contents, and confirm the table with the user before writing. One row per directory, formatted `| ` + backtick-quoted `dir/` + ` | description |`. Mode-independent: always derived from the tree, never from an existing CLAUDE.md.
- `{{BOUNDARIES}}` — detect no-touch candidates from the repo (`bin/`, `obj/`, `node_modules/`, `dist/`, `*.Designer.cs`, database-migration output, vendored code), present them and ask the user for repo-specific additions (legacy areas, generated projects). If nothing exists beyond generated output, write exactly: "Generated build output only (`bin/`, `obj/`, `node_modules/`) — do not edit generated files."

## Step 5 — Legacy migration (migration mode only)

Follow `references/migration.md` in full. Summary of what it covers:

1. Split the existing CLAUDE.md into project-specific content (kept) and content now covered by an owned rule (removed, replaced by the full `CLAUDE.md.tpl` v2 wiring: the `@docs/ai/rules/...` import block, the `@docs/okf/codebase-map.md` import, the `## Boundaries` section, and the glossary pointer line — see `references/migration.md` §1; migration writes these directly rather than proposing them in Step 7).
2. Extract any existing "what maps to what" / category table from the old CLAUDE.md. If `docs/okf/index.md` already exists and already has an equivalent table, leave it untouched — do not write `docs/okf/index.md` at all. Otherwise, derive `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, `{{STACK_SUMMARY}}` from the old CLAUDE.md and the extracted table as `{{CATEGORY_MAPPING_TABLE}}`, then hand these four values to Step 4 — Step 4 is the only step that actually writes `docs/okf/index.md` from `okf-index.md.tpl`; this step only supplies its placeholder values.
3. Relocate any plans/specs directory outside the standard location (e.g. `.claude/plans/`) into `docs/superpowers/plans/`, fixing any relative references inside the moved files.
4. Remove `docs/superpowers/` (or equivalent) from `.gitignore` if present.
5. If an existing CLAUDE.md section conflicts with an owned rule (e.g. contradictory branch-naming convention), do not resolve it — surface the conflict and ask, per the decision-gate rule.

Run this step before scaffolding `docs/okf/index.md` in Step 4 — Step 4's `docs/okf/index.md` row depends on the project-name/overview/stack-summary/category-table values this step derives.

## Step 6 — Verify

1. For every file in the new `ownedFiles` list, diff it against the corresponding `assets/rules/...` source file (e.g. `diff docs/ai/rules/core/okf.md <skill-path>/assets/rules/core/okf.md`) — must be byte-identical. If any diff is non-empty, re-copy and check again; if it still fails, stop and report the failure instead of continuing.
2. Confirm every artifact from Step 4's table exists (or was already present).

## Step 7 — Report

Print a summary with these sections: **Created** (new files/directories), **Overwritten** (owned files updated by this run), **Deleted** (owned files removed because they left the constitution or a de-selected stack profile), **Needs your review** (CLAUDE.md is project-owned, so the Legislator never edits it directly; it only proposes exact lines here for the user to apply themselves — e.g. an `@import` line to add/remove when a rule file was added/removed, and, when this run scaffolded an artifact an existing CLAUDE.md doesn't reference yet, the wiring for it: the `@docs/okf/codebase-map.md` import, a `## Boundaries` section, the glossary pointer line). Additionally, only when this run changed the `keep` list or refused a keep request, include a **Keep list** section: each entry added or removed (path + reason) and each refused request with why it was refused (the path does not exist, or the path is an owned file under `docs/ai/rules/`).

In **upgrade mode only**, append a final section, `### Health`, running the cheap audit checks (1–6 in the Audit section below) against the post-run state: list findings in the Audit section's line format; if there are none, print exactly `Health: clean`. Fresh-scaffold and migration runs skip this section — everything they just created is definitionally fresh.

In **migration and upgrade modes** (never fresh scaffold — everything there was just written by this skill), also scan project-owned prose for **constitution candidates** and, when at least one qualifies, append a section (after the Keep list section, before Health):

```
### Constitution candidates
- "<verbatim quote>" — <repo-relative path>
```

A candidate is a statement passing all three tests: (1) **law-shaped** — imperative and diff-checkable ("always …", "never …", "must …", "… before every commit"), not description or narration; (2) **not already covered** — no rule under `docs/ai/rules/**` states it (judge by meaning, not wording); (3) **generalizable** — it would make sense verbatim in another repo of the same stack. Project-instance data is an instantiation of law, not law: a concrete path, this project's branch pattern, a named contact or environment stays put and is never proposed. Scan CLAUDE.md's project-specific sections (skip the `@import` block and the template wiring lines) and prose under `docs/okf/`; do not scan `docs/ai/rules/**`, `docs/adr/**`, `docs/journal/**`, `docs/backlog.md`, or `docs/superpowers/**`. Skip any line carrying `<!-- legislator: not-law -->` on it or as the entire immediately preceding line — the user has already ruled that statement out. Candidates are **proposals only**: never write them to any file; the user promotes worthy ones into the skill's central `assets/rules/**` (bumping VERSION) and re-runs `/legislator` fleet-wide. If nothing qualifies, omit the section entirely.

Do not run `git add` or `git commit`. The user reviews and commits.

## Audit — read-only health check

Run this section INSTEAD of Steps 0–7 when the user asks to audit or health-check the AI layer. It performs **zero writes**: record `git status --porcelain` and `git rev-parse HEAD` before starting, and verify both are byte-identical after producing the report — if either changed, the run has a bug; say so explicitly in the report.

Perform these checks (severity in parentheses; a finding names the offending path, date, or entry verbatim):

In findings, `[check-name]` is the check's pinned slug — use exactly these: 1 `imports-resolve`, 2 `unresolved-placeholders`, 3 `owned-integrity`, 4 `staleness`, 5 `okf-index-links`, 6 `codebase-map`, 7 `orphan-docs`, 8 `journal-recency`, 9 `foreign-structures`, 10 `keep-list`.

1. **Imports resolve (Critical):** every `@<path>` line in CLAUDE.md points to an existing file.
2. **Unresolved placeholders (Critical):** no `{{TOKEN}}` pattern in CLAUDE.md or any `.md` under `docs/`, except `docs/adr/template.md` (its tokens are intentional).
3. **Owned-layer integrity (Critical):** `docs/ai/manifest.json` parses; every `ownedFiles` entry exists on disk; every owned file is byte-identical to its `<skill-path>/assets/rules/...` source (diff each one).
4. **Constitution staleness (Info):** manifest `legislatorVersion` vs. `<skill-path>/VERSION`; if behind, note "re-run /legislator to upgrade".
5. **OKF index links (Warning):** every markdown link in `docs/okf/index.md` resolves to an existing file.
6. **Codebase-map freshness (Warning):** skip if `docs/okf/codebase-map.md` is absent. Otherwise: every directory named in the map's table exists, and every non-generated top-level directory (ignore hidden directories and `bin/`, `obj/`, `node_modules/`, `dist/`) has a row.
7. **Orphan docs (Warning):** an `.md` under `docs/okf/` or directly in `docs/` that no other `.md` file (or CLAUDE.md) **references**. A file counts as referenced when any other markdown file mentions it by markdown link, by `@import`, or by inline-code path mention — its repo-relative path (or a relative path resolving to it) inside backticks; rule text naming `docs/okf/index.md` in backticks wires that file in. Exempt by directory convention: `docs/ai/rules/**`, `docs/adr/**`, `docs/journal/**`, `docs/superpowers/**`, and `docs/backlog.md`.
8. **Journal recency (Warning):** newest entry date in `docs/journal/` (from filenames or entry content) vs. the date of the last commit touching paths outside `docs/`; flag if the code commit is newer by more than 30 days, citing the newest entry's date.
9. **Foreign AI-layer structures (Info):** list any of `.cursorrules`, `.cursor/`, `AGENTS.md`/`agents.md`, `.github/copilot-instructions.md`, `wiki/`, and ADR/plans directories outside the standard layout (`adrs/`, `doc/adr/`, `.claude/plans/`).
10. **Keep-list integrity (Warning):** for every entry in the manifest's `keep` list: (a) the kept path exists on disk — otherwise finding `<path>: kept path missing from disk → restore it or remove the keep entry`; (b) the kept file is referenced from somewhere — same definition as check 7, searched across CLAUDE.md and every `.md` in the repo, with no directory exemptions — otherwise finding `<path>: kept but referenced from nowhere → link it from docs/okf/index.md or CLAUDE.md`. If the manifest has no `keep` key at all, add to the **Info** section exactly `- [keep-list] docs/ai/manifest.json: no keep key (pre-keep-schema manifest) → re-run /legislator to refresh` and skip the check.

Report format — print exactly this structure (omit empty severity sections; a fully clean audit prints the header, `No findings.`, and the clean-checks line):

```
# AI-Layer Audit — <repo name>, <ISO date>

Constitution: v<manifest version> (skill source: v<VERSION>) — <up to date | behind>

## Critical
- [check-name] <path>: <one-line finding> → <one-line remedy>

## Warning
...

## Info
...

Clean checks: <comma-separated names of checks that passed>
```

**Constitution candidates appendix:** after the Info section and before the `Clean checks:` line, apply Step 7's constitution-candidates scan — same three tests, same scanned sources, same suppression marker, same section format. It is an appendix of proposals, not a numbered check: no severity, no slug, and it changes nothing about the zero-writes contract. Omit it when nothing qualifies.
