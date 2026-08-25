---
type: System
title: Legislator — Codebase Map
description: Top-level directory map — where things live in this repo.
tags: [system, architecture, map]
timestamp: 2026-08-24T00:00:00Z
status: implemented
---

# Codebase Map

One line per top-level directory. Keep this table in sync with the actual tree (the okf.md sync rule applies): update it when directories are added, removed, or repurposed.

| Directory | What lives there |
|-----------|------------------|
| `skill/` | The shipped package — `SKILL.md`, `assets/rules/**` (the law's only source), `assets/templates/**`, `assets/engine/`, `references/**`, `VERSION`. Symlinked into `~/.claude/skills/legislator`. |
| `evals/` | The regression suite — `POLICY.md` (the bar), `evals.json`, fixtures, `grade.py`, `setup_workspace.py`, the four static checks, and the per-edition benchmark records. |
| `tools/` | Operator scripts — `fleet.sh` (discover and upgrade legislated repos), `evals-bg.sh` (the staged eval runner), and the skill/plugin linkers. |
| `plugin/` | The deterministic enforcement arms — `hooks/**` for Claude Code and `opencode/legislator-guard.ts` for opencode. |
| `docs/` | This repo's own AI layer and records — the delivered law under `ai/`, the OKF bundle, `cases/`, `backlog.md`, `philosophy.md`, `ontology.md`, and `superpowers/` as retired history. |
