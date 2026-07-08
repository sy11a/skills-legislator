# Legislator — AI-Setup Scaffolding & Upgrade Skill

**Date:** 2026-07-08
**Status:** approved design
**Scope:** cycle 1 of 2 — the Legislator skill only. The master-agent (request router that reuses or creates specialized project-local mini-agents) is a separate follow-up design; this cycle only installs the folder convention it will live in.

## Problem

All of the user's projects (CareerPlatform, RKruiterApi, future ones) are AI-development-first, built on the .NET stack (sometimes with Aurelia), and share the same creation flow: CLAUDE.md + OKF knowledge bundle + backlog + specs/plans. Three artifacts were forgotten in existing projects and must become standard: ADR, dev journal, changelog.

Today the common rules are copy-pasted into each project's CLAUDE.md, so improving a rule means manually editing every repo. The two existing repos have also drifted from each other (CareerPlatform gitignores `docs/superpowers/`; RKruiterApi keeps plans in `.claude/plans`).

## Goal

A single skill, `/legislator`, that:

1. Scaffolds the full AI-setup into a fresh repo.
2. Migrates a legacy repo (the two existing projects) to the standard layout.
3. Re-applies an updated constitution to an already-legislated repo idempotently — the update loop is: edit rule in the legislator repo → bump VERSION → run `/legislator` in each project → review `git diff` → commit.

Non-goal: code scaffolding (dotnet new, docker-compose, solution layout). That stays with stack tools and stack skills. The Legislator owns only the AI/docs layer.

## Core mechanism: owned-file split with verbatim copies

- **Legislator-owned files** live in `docs/ai/rules/` in each target repo. They are byte-for-byte copies of the canonical files in the skill's `assets/rules/`. Re-runs overwrite them freely and delete owned files that no longer exist in the constitution. The skill must never rewrite, reformat, or "improve" them during copy.
- **Project-owned files** (CLAUDE.md body, OKF bundle, ADRs, journal, backlog, changelog, specs/plans) are scaffolded from templates only when absent and never overwritten afterward.
- **CLAUDE.md** stays project-owned and pulls the common rules in via `@import` lines (`@docs/ai/rules/core/okf.md` etc.).
- **Manifest** (`docs/ai/manifest.json`) records: applied Legislator VERSION, the list of owned files, and the selected stack profiles. It is how a re-run knows what it owns (including deletions) and lets the skill skip questions already answered.

Rules are tiered:

- `core/` — stack-agnostic process rules, always applied.
- `stacks/dotnet/`, `stacks/aurelia/` — applied per project profile (asked once, stored in manifest).

## Skill package layout (`custom_skills/legislator/`)

```
legislator/                          # repo root
├── skill/                           # the installable skill (symlink target)
│   ├── SKILL.md                     # frontmatter + procedure: detect → apply → migrate → report
│   ├── VERSION                      # constitution version, integer, bumped on every rule change
│   ├── assets/
│   │   ├── rules/
│   │   │   ├── core/
│   │   │   │   ├── okf.md               # OKF mandate + generic update checklist
│   │   │   │   ├── pair-development.md  # branch-per-task, never self-merge, no AI co-author lines
│   │   │   │   ├── decision-gate.md     # stop-and-ask triggers
│   │   │   │   ├── adr.md               # when/how to record an ADR
│   │   │   │   ├── dev-journal.md       # journal discipline (one file per working day)
│   │   │   │   └── changelog.md         # Keep-a-Changelog discipline
│   │   │   └── stacks/
│   │   │       ├── dotnet/
│   │   │       │   ├── architecture.md  # zero-NuGet Domain, layer reference graph, tenant-context pattern
│   │   │       │   └── coding-standards.md
│   │   │       └── aurelia/
│   │   │           └── conventions.md
│   │   └── templates/               # project-owned scaffolds, created once, never overwritten
│   │       ├── CLAUDE.md.tpl        # overview stub + @import block
│   │       ├── okf-index.md.tpl
│   │       ├── okf-log.md.tpl
│   │       ├── backlog.md.tpl
│   │       ├── adr-0001.md.tpl      # "Record architecture decisions" bootstrap ADR
│   │       ├── adr-template.md.tpl
│   │       ├── journal-README.md.tpl
│   │       ├── changelog.md.tpl
│   │       └── agents-README.md.tpl # mini-agent space convention (cycle-2 hook)
│   └── references/
│       └── migration.md             # legacy-repo migration guide, incl. CareerPlatform/RKruiterApi specifics
└── README.md                        # repo purpose + install instructions
```

Install: `ln -s /home/admin/Repository/custom_skills/legislator/skill ~/.claude/skills/legislator` (the symlink name gives the skill its `/legislator` invocation).

### Rule content sources

The canonical rule files are extracted from the intersection/union of the two existing CLAUDE.md files:

- **okf.md** — the "OKF Documentation Rule (MANDATORY)" + "OKF is non-negotiable" sections, *minus* the project-specific "what maps to what" table. That table is project knowledge and lives in the project's `docs/okf/index.md` (the rule file points there).
- **pair-development.md** — the Pair Development Protocol section (one task at a time, branch per task, never merge to master, never add AI co-author lines, wait for approval between tasks).
- **decision-gate.md** — the "Decision gate — stop and ask before proceeding" section verbatim (it is already identical in both repos).
- **adr.md** — new: what warrants an ADR (any decision-gate resolution, any architecture invariant, any accepted antipattern), MADR-style format, numbering `NNNN-kebab-title.md` in `docs/adr/`.
- **dev-journal.md** — new: `docs/journal/YYYY-MM-DD.md`, append-only during the day; what was worked on, decisions taken, dead ends, open questions for tomorrow. Written at task boundaries, not as a running log.
- **changelog.md** — new: root `CHANGELOG.md`, Keep-a-Changelog format, `[Unreleased]` section updated as part of every task completion checklist.
- **stacks/dotnet/architecture.md** — zero-NuGet-dependency Domain project, enforced layer reference graph, business logic never touches HttpContext (tenant-context abstraction), all AI/LLM calls behind a provider abstraction, EF query-filter/tenant-safety expectations. Written generically (no CareerPlatform/RKruiter class names); project-specific instances of these rules stay in the project CLAUDE.md.
- **stacks/dotnet/coding-standards.md** — merged superset of both repos' standards: meaningful names, explicit types over var, braces on all ifs, no comments unless the WHY is non-obvious, no empty catch blocks / swallowed exceptions, zero-warnings policy.
- **stacks/aurelia/conventions.md** — seeded thin (pointer to the aurelia-developer skill + minimal conventions); grows over time via the normal update loop.

## Target-repo layout after a run

```
target-repo/
├── CLAUDE.md                        # project-owned: overview, stack facts, project constraints, @imports
├── CHANGELOG.md                     # project-owned, Keep-a-Changelog
├── docs/
│   ├── ai/
│   │   ├── manifest.json            # { "legislatorVersion": N, "profiles": ["dotnet"], "ownedFiles": [...] }
│   │   └── rules/
│   │       ├── core/…               # always
│   │       └── stacks/dotnet/…      # per profile
│   ├── okf/                         # index.md + log.md (+ existing bundle untouched)
│   ├── adr/                         # 0001-record-architecture-decisions.md + template.md
│   ├── journal/                     # README.md + YYYY-MM-DD.md entries
│   ├── backlog.md
│   └── superpowers/
│       ├── specs/
│       └── plans/
└── .claude/agents/README.md         # mini-agent convention doc (folder may be otherwise empty)
```

**Git policy: everything committed.** No gitignore entries for any of these paths; migration removes an existing `docs/superpowers/` gitignore entry.

## Run procedure (what SKILL.md instructs)

1. **Detect state** of the current working directory:
   - `docs/ai/manifest.json` exists → **upgrade** mode.
   - `CLAUDE.md` exists, no manifest → **legacy migration** mode.
   - Neither → **fresh scaffold** mode.
2. **Determine profiles.** From manifest if present; otherwise inspect the repo (`*.slnx`/`*.csproj` → dotnet; aurelia in package.json → aurelia) and confirm with the user.
3. **Apply owned files.** Copy `rules/core/*` and selected `rules/stacks/*` verbatim (`cp`, not retyping) into `docs/ai/rules/`. Delete files listed in the old manifest's `ownedFiles` that are absent from the new constitution. Write the new manifest.
4. **Scaffold project-owned artifacts** from templates — each only if absent: CLAUDE.md, CHANGELOG.md, okf index/log, backlog, ADR folder (0001 + template), journal folder, superpowers specs/plans folders, `.claude/agents/README.md`.
5. **Legacy migration** (migration mode only, guided by `references/migration.md`):
   - Rewrite CLAUDE.md: keep project-specific content (overview, stack facts, architecture instances, build/test commands), remove sections now covered by owned rules, add the `@import` block.
   - Move the project's OKF "what maps to what" table into `docs/okf/index.md` if not already represented there.
   - Relocate stray plans (`.claude/plans/*` → `docs/superpowers/plans/`), fix any references to moved files.
   - Remove `docs/superpowers/` (or equivalent) from `.gitignore`.
   - Known-repo specifics for CareerPlatform and RKruiterApi are spelled out in `references/migration.md`.
6. **Verify.** `diff -r` owned files against the skill's assets — must be byte-identical. Confirm every expected artifact exists.
7. **Report.** Print a summary: created / overwritten / deleted / migrated / needs-your-review items. **Never commit** — the user reviews the diff and commits themselves (consistent with the pair-development rule).

Idempotency contract: running `/legislator` twice in a row produces zero diff on the second run.

## Adaptability model

- Edit/add/remove a rule file in `legislator/skill/assets/rules/`, bump `VERSION`, re-run `/legislator` in each project. Only the changed owned files (+ manifest) appear in the diff.
- Because CLAUDE.md is project-owned, the Legislator never edits it during upgrades. When a rule file is added or removed, the run proposes the corresponding `@import` line addition/removal as a **needs-your-review** item in the report (with the exact line to add or delete) instead of editing CLAUDE.md itself.
- New artifact type later (as ADR/journal/changelog were forgotten before): add a template + a line in the SKILL.md scaffold list; the next run creates it in every project.
- New stack: add `assets/rules/stacks/<name>/`; projects opt in via profile.

## Agent space (cycle-2 hook)

`agents-README.md.tpl` establishes only the convention: project-local mini-agents live as `.claude/agents/<name>.md` with frontmatter declaring description, model, and allowed tools/MCP servers — fine-grained, single-purpose agents. The master-agent (routing incoming requests to existing agents or deciding to create a new specialized one) is the next design cycle and nothing in this cycle constrains it beyond this folder convention.

## Error handling

- Target repo has uncommitted changes → warn and ask before proceeding (the run's diff would mix with user changes).
- Owned-file verify step fails (copy not byte-identical) → re-copy with `cp`; if still failing, abort and report.
- Legacy CLAUDE.md contains a section that conflicts with an owned rule (e.g., contradictory branch naming) → decision-gate behavior: surface it and ask, don't resolve unilaterally.
- Running outside a project (no repo markers at all, empty dir) → treat as fresh scaffold but confirm the directory with the user first.

## Testing / verification plan

Built-skill acceptance, run against scratch copies (not the real repos):

1. **Fresh scaffold:** empty dir → run → all artifacts exist, manifest correct, second run yields zero diff.
2. **Legacy migration:** scratch copy of RKruiterApi (the harder case: `.claude/plans`, embedded rules) → run → CLAUDE.md keeps project content, imports resolve, plans relocated, second run yields zero diff.
3. **Upgrade propagation:** edit one rule file in the constitution, bump VERSION, re-run on the scratch copy → only that owned file (+ manifest) changes.
4. **Rule removal:** delete a rule from the constitution, re-run → the owned copy is deleted in the target, import line flagged for removal.

Real-repo migration of CareerPlatform and RKruiterApi happens after acceptance, one repo at a time, reviewed via git diff.
