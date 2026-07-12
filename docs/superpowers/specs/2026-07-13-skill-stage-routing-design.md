# Skill stage-routing + setup automation — design (BL-024, constitution v13)

**Status:** approved 2026-07-12 (plan-mode approval: stage-mapped starter
scaffold, check 14 at Info, tutorial + link script). Historical record —
do not rewrite.

## Problem

v12 settled *which* skills are lawful; nothing says *when*. Agents don't
proactively reach for `grilling` before a plan or `tdd` during
implementation — the sanction list is stage-blind. And the skill ecosystem
itself has no setup story: kept skills are symlinks into a non-git dump
(`~/.agents/skills`), installation/pruning is undocumented tribal
knowledge, and nothing detects a sanctioned-but-uninstalled skill (the
binding rots silently, the same disease the glossary had).

## Decisions

1. **Stage routing** (`core/skills.md`, +2 bullets): the per-repo
   `.claude/rules/skills.md` maps sanctioned skills to workflow stages
   (`pre-plan`, `implement`, `debug`, `review`, `docs/research`); at a
   stage boundary, consult the map and invoke where applicable; outside
   its stage a skill is not banned, just not proactively invoked; absence
   of the file disables routing, never errors.
2. **Starter scaffold** (create-once): new `skills-rules.md.tpl` +
   Step 4 row; `{{SANCTIONED_SKILLS_BY_STAGE}}` derived from confirmed
   profiles ∩ skills actually installed, using a pinned stage-affinity
   table in the derivation rule; confirmed with the user; never invents
   uninstalled skills. Existing hand-written files are never overwritten.
3. **Audit check 14 `skill-bindings` (Info):** every skill the file names
   resolves to an installed skill or plugin; environment-relative ⇒ Info,
   never Warning; restructure routes findings to `For the team:`.
4. **Steward tooling:** `tools/link-skills.sh` (idempotent curated
   symlinker with `--check`/`--prune`) + README "Skill ecosystem setup"
   tutorial. Repo tooling, not shipped law — machine setup is the
   steward's job, not the constitution's.
5. **BL-023 riders ship in this cycle** (v12 final review): read-only-DB
   bullet exempts test-harness seeding; absent-bindings fallback defined;
   `project-rules.md` sanctions the two instance-data homes; skills.md
   self-reference exemption for legislator's own CLAUDE.md writes;
   check 12 exempts GitHub's own templates; check 9 gains the kept-path
   exemption; upgrade grader also asserts the core-rule import proposal.

## Eval plan

Fresh: structural oracle only (file exists, ≥1 stage heading, no tokens —
content is environment-relative). Rotted fixture defect 15:
`.claude/rules/skills.md` sanctioning `made-up-skill` → check-14 Info
marker; restructure leaves the file byte-unchanged and lists the finding
under `For the team:`. Upgrade withholds the actual alphabetically-last
core rule (verify generator) and now asserts both import proposals.
Benchmark → `evals/benchmarks/v13.md` vs v12's 110.

## Out of scope

- Repo-local skills (`.claude/skills/` in-repo) — machine-level linking
  covers the fleet today; revisit if a repo ever needs a private skill.
- Auto-installing packs from the network — the script links what exists
  locally; cloning/updating stays a human step (documented in README).

## Branching note

Stacked on `feature/bl-022-skill-governance-v12` (PR #4 open at cycle
start) per the pair-development stacking law; merge bottom-up.
