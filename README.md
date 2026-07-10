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
git clone <this-repo-url> legislator
ln -s "$(pwd)/legislator/skill" ~/.claude/skills/legislator
```

The symlink name (`legislator`) is what gives the skill its `/legislator`
invocation in Claude Code. Keeping it a symlink (not a copy) means a
`git pull` here updates the skill everywhere at once.

## Tutorial — your first legislated repo

### 1. Run it

`cd` into any project repo (it should be a git repo with a clean working
tree) and ask Claude Code in plain language — `/legislator` or just:

> Set up the AI-dev constitution here — rules, OKF docs, backlog, the works.

The skill detects the repo's state and picks its mode by itself:

| Your repo | Mode | What happens |
|---|---|---|
| No `CLAUDE.md` yet | **fresh scaffold** | Everything created from templates, with a short interview (project name, overview, build/test commands, no-touch boundaries) |
| Hand-written `CLAUDE.md`, never legislated | **migration** | Your CLAUDE.md is rewritten around the rule imports — every project-specific sentence you wrote survives, carved into the right place |
| Already legislated (`docs/ai/manifest.json` exists) | **upgrade** | Newest rules copied in, retired rules deleted, everything you own left untouched |

It also detects your stack (`.sln`/`.csproj` → dotnet, Aurelia signals →
aurelia) and asks you to confirm before importing stack rules.

### 2. What you get

```
CLAUDE.md                  ← imports the rules; your project prose lives here
.claude/rules/             ← YOUR project law — one topic per file, auto-loaded
docs/ai/manifest.json      ← machine-managed: version, profiles, keep list
docs/ai/rules/             ← THE LAW — copied byte-for-byte, never edit by hand
docs/okf/                  ← knowledge: index, codebase map, glossary, log
docs/adr/  docs/journal/   ← decisions and dev journal
docs/backlog.md  CHANGELOG.md
docs/superpowers/{specs,plans}/
```

The split is the whole design: **owned** files (`docs/ai/rules/**`) are
overwritten on every run so all your repos share one versioned constitution;
**project-owned** files (everything else) are created once and never touched
again. The skill never commits — review the diff and commit it yourself.

### 3. Keep it healthy

Once legislated, three more requests matter:

- **"Audit the AI layer here"** — read-only health check (12 checks: broken
  imports, stale maps, orphan docs, dead journal, hand-edited law, leftover
  `.cursorrules`, stray rulebooks parked where no session loads them, …).
  Zero writes, severity-ranked report.
- **"Restructure the AI layer"** — the repair mode for rotted or foreign
  layouts. It audits, proposes a numbered plan, and **stops for your
  approval**; conflicts with the law are decision items it will never resolve
  on its own, and every sentence of your content provably survives the moves.
- **"Keep `docs/notes/runbook.md` as-is: battle-tested"** — marks a file
  untouchable in the manifest. Restructuring must leave it in place, and
  audits warn if it ever becomes unlinked.
- **"Add a project rule: every feature ships behind a feature toggle"** — the agent creates `.claude/rules/feature-toggles.md` per the project-rules law (auto-loaded, no CLAUDE.md bloat); harvest will later propose promoting it if it generalizes.

### 4. The fleet loop

Migration, upgrade, and audit reports end with **Constitution candidates** —
project rules you wrote that read like general law ("Money values are always
`decimal`"). To adopt one fleet-wide: add it to `skill/assets/rules/`, bump
`skill/VERSION`, run the eval suite, then `/legislator` in each repo. To
reject one: put `<!-- legislator: not-law -->` on the line above it in the
source repo. Law flows down automatically; it flows up only through your
hands.

Two habits: re-run the skill after any rule edit (that *is* the update
mechanism), and never hand-edit `docs/ai/rules/**` in a project — audits flag
the drift, and the optional [`plugin/`](plugin/) (`legislator-hooks`) blocks
such edits outright, auto-formats on edit, and reminds you when code changes
ship without an OKF update.

See [skill/SKILL.md](skill/SKILL.md) for the full procedure,
[skill/references/migration.md](skill/references/migration.md) for migration
specifics, and [skill/references/restructure.md](skill/references/restructure.md)
for the repair mechanics.

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

## Steward duties

The constitution has one steward (the DRI for the fleet's AI layer). Standing
duties, on a cadence of **every 3–6 months and after every major model
release**:

1. **Review each rule asking: preference or compensation?** Preferences (e.g.
   the decision gate, decimal-for-money) are durable — keep them.
   Compensations — instructions that padded over a limitation models no longer
   have — become actively constraining on newer models: delete them centrally
   and bump VERSION.
2. **Re-run the benchmark on the new model, unchanged.** Record results as
   `evals/benchmarks/v<N>-<model>.md`; pass-rate and token deltas against the
   previous model's file measure empirically whether the constitution helps or
   hinders the newer model — don't guess, measure.
3. **The deletion habit.** A rule that hasn't changed a single review outcome
   in months is either universally internalized (delete it — it's context
   noise) or universally ignored (delete it or start enforcing it — decide,
   don't let it linger). Deletion propagation makes removal a one-commit
   operation.
4. **Harvest candidates:** migration/upgrade/audit runs propose "Constitution candidates" in their reports (proposals only — nothing is written anywhere). Review them; promote worthy ones into `assets/rules/**` (bump `VERSION`), then re-run `/legislator` across repos. Reject by adding `<!-- legislator: not-law -->` above (or on) the source line in the target repo, or by rewording it so it stops reading as law. Candidates are re-derived every run, so an unread proposal is never lost — and a rejected one stays silenced.
