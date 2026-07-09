# Legislator

The Legislator is a Claude Code skill that scaffolds, migrates, or upgrades the
AI-development "constitution" in a project repo: CLAUDE.md rules, the OKF
knowledge bundle, backlog, ADRs, dev journal, changelog, and specs/plans.

It is designed to be re-run: edit a rule file here, bump `skill/VERSION`, then
run `/legislator` in each downstream project. Owned rule files
(`docs/ai/rules/**` in the target repo) are overwritten byte-for-byte on every
run; project-owned files (CLAUDE.md body, OKF content, ADRs, journal entries,
backlog, changelog, specs/plans) are created once and never touched again.

## Install

```bash
ln -s /home/admin/Repository/custom_skills/legislator/skill ~/.claude/skills/legislator
```

The symlink name (`legislator`) is what gives the skill its `/legislator`
invocation.

## Use

`cd` into any project repo and run `/legislator`. See `skill/SKILL.md` for the
full procedure and `skill/references/migration.md` for legacy-repo migration
specifics.

## Content discipline for rule files

Rule files (core and stacks alike) contain only **law** — short, enforceable
constraints a reviewer can check any diff against. How-to **guidance** (build
this component like so, optimize that query this way) is never inlined; it is
delegated by a pointer to where it actually lives (a skill such as
`aurelia-developer` or the `dotnet-*` family, project docs, or — later —
project-local agents). Guidance inlined into a rule file is a second copy that
rots; a pointer stays true. When tempted to paste a tutorial into a rule file,
that's the test to apply.

Stack rule files are named by concern — `architecture.md`,
`coding-standards.md` — and a stack ships only the concerns it actually has
law for (no empty placeholder files).

## Update the constitution

1. Edit a file under `skill/assets/rules/`.
2. Bump `skill/VERSION`.
3. Run the eval suite (see below) — required before the change is done.
4. `cd` into each downstream project and run `/legislator`.
5. Review the `git diff` — only the changed owned file(s) and the manifest
   should appear — then commit.

## Test and benchmark

`evals/` is the skill's regression suite — see [evals/README.md](evals/README.md)
for the full explanation of what the evals do and how benchmarking works.
Short version:

```bash
# unit layer — every commit, seconds, no agent
python3 evals/check_static.py

# e2e layer — required after ANY behavioral change to skill/
python3 evals/setup_workspace.py /tmp/legislator-eval-vN
#   → run one fresh agent per scenario (prompts in evals/evals.json)
python3 evals/grade.py /tmp/legislator-eval-vN
#   → commit run 1 in one repo, re-run the agent, then:
python3 evals/grade.py /tmp/legislator-eval-vN idempotency:fresh-scaffold-dotnet
```

Record each version's results in `evals/benchmarks/v<N>.md`; the comparison
against the previous version's file (pass rate = correctness, tokens/time =
cost, idempotency = diff noise) is the productivity verdict for the change.
This requirement is also stated in this repo's `CLAUDE.md` so any agent
working here sees it.
