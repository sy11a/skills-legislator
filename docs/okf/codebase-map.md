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
| `evals/` | The regression suite — `POLICY.md` (the bar), `evals.json`, fixtures, `grade.py`, `setup_workspace.py`, the four static checks, `check_dotnet.sh` and `parity_labels.py` (the port's instruments), and the per-edition benchmark records. |
| `tools/` | Operator scripts — `fleet.sh` (discover and upgrade legislated repos), `evals-bg.sh` (the staged eval runner), and the skill/plugin linkers. |
| `plugin/` | The deterministic enforcement arms — `hooks/**` for Claude Code and `opencode/legislator-guard.ts` for opencode. |
| `src/` | The .NET deterministic substrate (from v25, ADR-0008) — `Legislator.Core` (abstractions, the options model), `Legislator.Engine` (the jobs), `Legislator.Hooks`, `Legislator.Cli` (the host), and the build discipline declared once in `Directory.Build.props`. |
| `tests/` | The .NET suite — one test project per production project plus `Legislator.Parity.Tests` (the twins of the Python rulers) and `Legislator.TestSupport` (the fakes every test project shares). |
| `artifacts/` | Generated (git-ignored, `.gitignore`): the published NativeAOT binaries, one directory per RID, plus `SHA256SUMS` — the digests `tools/publish-legislator.sh` records and `legislator version --json` answers with. Do not edit; republish. |
| `docs/` | This repo's own AI layer and records — the delivered law under `ai/`, the OKF bundle, `cases/`, `backlog.md`, `philosophy.md`, `ontology.md`, and `superpowers/` as retired history. |
