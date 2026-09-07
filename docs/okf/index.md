---
type: System
title: Legislator — System Overview
description: Root of the OKF knowledge bundle — architecture, tech stack, project layout, and links to every category.
tags: [system, architecture, index]
timestamp: 2026-09-07T00:00:00Z
status: implemented
---

# Legislator

This repo develops the `legislator` Claude Code skill: a constitution for
AI-assisted development, delivered verbatim and versioned into every
repository that subscribes to it. `skill/` is the shipped package, `evals/` is
its regression suite, `tools/` holds the fleet and eval runners, `plugin/`
holds the two deterministic enforcement arms, and `docs/` holds this repo's
own backlog, cases and historical specs and plans.

The repository is **fleet member #0**: it is legislated by the constitution it
produces. The law under `docs/ai/rules/**` is a delivered copy of
`skill/assets/rules/**` and is never edited in place — changes flow only
through edit → bump `skill/VERSION` → benchmark → deliver.

## Tech stack

Markdown for the law itself; C# under `src/` and `tests/` for the
deterministic substrate every enforcement arm now is (BL-082, ADR-0008 —
from v26 the engine and the four hooks are one NativeAOT binary and the law
names `legislator <job>`); and Python and Bash for the eval harness and the
operator scripts, which are instruments and ship in no edition
(`.claude/rules/dotnet-substrate.md`). The
manifest's `stacks` list is still empty: it is machine-managed and changes only
on a `/legislator` run, so the `dotnet` stack the constitution ships does not
yet reach this repository's own law even though it now describes its code.

## What maps to what

| Change | OKF file to update |
|--------|---------------------|
| A top-level directory added, removed, or repurposed | [codebase-map.md](codebase-map.md) |
| A new domain term, or a changed meaning for an existing one | [glossary.md](glossary.md) |
| What this system is, or how its parts relate | this file |
| Any of the above | append the entry to [log.md](log.md) |

Deeper narrative lives outside the bundle and is linked, not duplicated:
`docs/philosophy.md` (why the system is shaped this way), `docs/ontology.md`
(the concept model and naming rules), `docs/backlog.md` (the case queue and
register).

## Changelog

All bundle changes are recorded in [log.md](log.md).
