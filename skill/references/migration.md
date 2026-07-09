# Legacy Repo Migration Guide

Detailed guidance for SKILL.md Step 5 (legacy migration mode) — when `CLAUDE.md` exists but `docs/ai/manifest.json` does not.

## 1. Splitting CLAUDE.md

Read the existing `CLAUDE.md` top to bottom and classify each section:

- **Project-specific — keep verbatim in the new CLAUDE.md:** project overview, tech stack description, project-specific architecture instances (e.g. "CareerPlatform.Domain has zero NuGet dependencies" — this is an *instance* of the generic `stacks/dotnet/architecture.md` rule and stays as a concrete callout), build/test commands, CI notes.
- **Now covered by an owned rule — remove and replace with an import:** the OKF Documentation Rule section, the Pair Development Protocol section, the Decision gate section. If the existing text differs from the owned rule's wording, that's expected — the owned rule supersedes it. Do not try to preserve project-specific *phrasing* of these sections; the import replaces the prose entirely.
- **Concrete project-specific facts embedded in a replaced section — carve out and keep as a short callout, even though the surrounding section is removed.** A section being "covered by an owned rule" means its boilerplate prose is superseded — it does not mean every fact inside it is disposable. Before deleting a section, scan it for concrete instance data the owned rule itself expects to find in CLAUDE.md (the owned rules are written assuming this): a branch-naming pattern (e.g. `bl/NNN-short-description` — `pair-development.md` explicitly says "or this project's backlog-ticket convention — see this repo's CLAUDE.md", so if you delete this without a replacement, that rule's cross-reference goes dangling), a specific escalation contact or channel, or any other fact that isn't restating the rule but instantiating it for this project. Keep these as one or two lines near the import block (a short "Project Conventions" callout works well), the same way an architecture instance like "Domain has zero NuGet dependencies" survives even though the general rule lives in `stacks/dotnet/architecture.md`.
- **New sections to add:** none go in CLAUDE.md itself — ADR, dev journal, and changelog disciplines are covered by their own owned rule files, imported via the same `@docs/ai/rules/core/...` block.

Rewrite CLAUDE.md as: kept project-specific content, followed by the six core `@import` lines plus one import line per file in each confirmed stack profile's rule directory (mirroring `CLAUDE.md.tpl`'s import block).

## 2. OKF "what maps to what" table

If the existing CLAUDE.md has a "What maps to what" or category-mapping table (mapping change types to `docs/okf/<category>/<concept>.md` paths), and `docs/okf/index.md` does not already contain an equivalent table, move that table into `docs/okf/index.md` under a `## What maps to what` heading — this is exactly the `{{CATEGORY_MAPPING_TABLE}}` placeholder from the `okf-index.md.tpl` template. If `docs/okf/index.md` already exists with its own mapping table (common — most legacy repos already have an OKF bundle), leave it as-is; do not duplicate or merge. An "equivalent table" means a Change → OKF-file mapping a future agent can act on directly — a general "Key Concepts" table of contents or category index that merely *links* to OKF documents, without telling the reader which file to update for which kind of change, is not equivalent; move the real mapping table in that case too.

## 3. Relocating plans and specs

Some legacy repos keep implementation plans somewhere other than `docs/superpowers/plans/` — for example `.claude/plans/`. If such a directory exists:

1. Create `docs/superpowers/plans/` and `docs/superpowers/specs/` if they don't exist.
2. Move every file from the legacy location into `docs/superpowers/plans/`, preserving filenames.
3. Grep the moved files (and any other docs that reference them) for the old path and update references to the new path.
4. Leave the old directory empty (or remove it if nothing else uses it) — do not leave a stale duplicate.

## 4. Gitignore cleanup

Check `.gitignore` for an entry matching `docs/superpowers/` (or a more specific `docs/superpowers/plans/` / `docs/superpowers/specs/`). If found, remove the entry — this project's git policy commits everything the Legislator manages. Note the removal in the run's report (Step 7 of SKILL.md) since it changes what future commits include.

## 5. Conflicting sections

If a section of the existing CLAUDE.md contradicts an owned rule — for example, a branch-naming convention that differs from `pair-development.md`, or a decision-gate list with fewer/different triggers — this is exactly the kind of trade-off the decision-gate rule exists for. Do not silently pick one side. Stop, describe the conflict (quote both versions), and ask the user which should win before writing anything.

## 6. Known-repo notes

These are examples of what to expect, not an exhaustive list — always re-read the actual repo rather than assuming these apply unchanged.

- A repo with a **gitignored** `docs/superpowers/` (specs/plans kept local-only): this is precisely the pattern Section 4 above corrects — un-gitignore it and note in the report that history for those files starts now (existing local-only content should be committed as part of this migration, not lost).
- A repo with plans under **`.claude/plans/`** instead of `docs/superpowers/plans/`: apply Section 3 above.
- A repo whose CLAUDE.md already has an OKF "what maps to what" table with several project-specific slice categories (tenancy, applications, pipeline, etc.): leave that table in `docs/okf/index.md` untouched per Section 2 — it is project knowledge, not something the Legislator owns or regenerates.
