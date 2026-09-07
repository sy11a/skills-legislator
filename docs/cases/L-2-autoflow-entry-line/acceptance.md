# L-2 — Acceptance protocol & converge record (stage 8, cycle 1)

**Verdict: ✅ Converged** — stage 8 closed 2026-09-07; delivery (stage 9) may open.
Mode autoflow, tier 0 (no EARS R-lines, no plan tasks — the promise set below is the
inventory; findings and their disposition are recorded here and in the journal, per the
tier-0 reading recorded in the case spec's route deviations).

## Frozen acceptance set — version: cycle 1 (frozen 2026-09-07)

Carried from the spec (`## Acceptance`), not re-derived. No amendment candidates were
accumulated by any stage; nothing added at freeze.

| # | Scenario | Origin tag | What it demands |
|---|----------|-----------|-----------------|
| S-1 | Law line present and correctly worded in the source | stage 0/1 (spec, case's own exit) | one line in `skill/assets/rules/core/pair-development.md` stating that a task is entered by the unattended route unless the operator names the attended one |
| S-2 | Instance class exists | stage 1 (spec, case's own exit; ADR-0009 decision 2) | this repository's `.claude/rules/skills.md` carries the `flow-sessions` class, `autoflow` first |
| S-3 | Generated stage map pins the same shape | stage 6 (ruling 11's second half; ADR-0009 decision 2, amended) | the `{{SANCTIONED_SKILLS_BY_STAGE}}` derivation in `skill/SKILL.md` pins `flow-sessions` — `autoflow` first, include-only-when-installed |
| S-4 | Edition posture | stage 1 (spec per DP-1; ADR-0009 decision 3) | no `skill/VERSION` bump on this task's authority; the edition question is a proposal in the pull request, never a decision |
| S-5 | A-5.1.1 structural exit | stage 0 (Architector contract, carried) | GIVEN a governed repository re-legislated after the edition lands, WHEN its delivered law is read, THEN the line is present — read at the *delivery target*, not this repository |

## Protocol — scenario → run mode → verdict → evidence

Run modes: scripted (the repository's own gates) and structural read of the artifacts at the
judged version (commit `89ce886` plus the acceptance commits — clean tree verified before
judging). No reproduction canon exists (tier 0); the scenario steps above are the execution
steps. No stand exists (sources note rulings 2026-08-30: no live stands/environments);
preconditions = clean tree at the judged version, verified.

| # | Verdict | Evidence |
|---|---------|----------|
| S-1 | **PASS** | `git show 89ce886:skill/assets/rules/core/pair-development.md` line 3: "Task entry defaults to the unattended route — a task is opened via `/autoflow` unless the operator names `/flow` explicitly for an attended session; applies wherever that skill pair is installed, and is silent where it is not" — matches A-5.1.1's demand (unattended default, attended named by the operator) and ADR-0009 decision 1 (conditioned on install, silent where absent) |
| S-2 | **PASS** | `.claude/rules/skills.md` diff (+2): `flow-sessions` — `autoflow` first, above every per-work-stage class; direct instance-data edit per `core/project-rules.md` (ADR-0009 decision 2) |
| S-3 | **PASS** | `skill/SKILL.md` diff: the derivation bullet pins `flow-sessions` — `autoflow` — as the first stage affinity; exercised green in the benchmark's scaffold/migration surfaces (`fresh-scaffold-dotnet` 21/21, `legacy-migration` 28/28, `legacy-migration-agents-first` 23/23, `upgrade` 25/25 — the generated `.claude/rules/skills.md` is produced and asserted in each) |
| S-4 | **PASS** | `skill/VERSION` = 25, unchanged by the branch; the edition question is staged for the PR body (stage 9). The `constitution-source.md` bump-in-same-commit tie is deliberately deferred by the operator's recorded ruling (DP-1; ADR-0009 decision 3) and is honored at the merge boundary, where the edition is assigned — a recorded deviation, not a violation |
| S-5 | **CARRIED** | Not runnable in this repository by design: the spec scopes this case's own exit to S-1..S-4 and assigns delivery into `docs/ai/rules/**` (here and fleet-wide) to the release runbook after the operator's edition ruling. Recorded as the structural authority the PR body must carry forward; unblocks with the edition question |

## Sibling regression

The benchmark ran the whole corpus, not a slice (no scoping ruling exists for L-2): all
nine scenarios green, 216/216 measured, 0 unmeasured, idempotency zero-diff ×3 — the
unchanged surfaces (`audit`, `restructure`, `case-practice`, `audit-engine-absent`,
`upgrade-drop-stack`) are regression-clean next to the changed derivation's surfaces.
Record: `docs/cases/L-2-autoflow-entry-line/benchmark.md` (committed by this stage — see
findings). The haiku smoke reds were classified MODEL via the unchanged-law baseline
discriminator (21/25 on `master` @ `696fdc0` vs 23/25 on the branch, failing
`mode_respects_authority` identically); the law change is exonerated; no red names
`skills.md` or the derivation.

## Converge — promise inventory and verdicts

Judged against the artifacts at the judged version, never against diffs alone.

| Promise (source) | Verdict |
|------------------|---------|
| Spec, case's own exit (S-1..S-4 above) | met — see protocol |
| ADR-0009 decision 1 (law line, wording + inert-where-absent) | met |
| ADR-0009 decision 2 (instance class + generated-half amendment) | met — both halves present; the amendment is recorded in the ADR itself |
| ADR-0009 decision 3 (no VERSION bump; edition proposed in the PR) | met — `skill/VERSION` untouched |
| Ruling 11, both halves (fleet law line + generated stage map) | met |
| `core/okf.md` checklist (glossary rows `task entry route`, `flow-sessions`; log entries ×2; timestamp 2026-09-07; cross-links) | met |
| `core/changelog.md` (Unreleased carries the L-2 clause) | met |
| `core/dev-journal.md` (2026-09-06 and 2026-09-07 day files, one section per session) | met |
| `.claude/rules/records.md` (no fleet repo names, no absolute local paths in tracked files) | met after remediation — see F-1 |
| `core/verification.md` ladder (static rung: `python3 docs/ai/engine.py anchors` silent; every-commit gates; behavioral change → full e2e benchmark, 100% + idempotency ×3) | met — gates re-run green at the judged tree during this stage; benchmark green and recorded |
| Nothing unrequested (DP-1's sanctioned set vs the branch's 11-file inventory) | met — every touched file is in DP-1's set or required by the frozen contract's second half (ruling 11) |

Gate outputs at the judged tree (this stage's own runs, not inherited claims):
`evals/check_static.py` — all static checks passed; `evals/check_engine.py` — all engine
checks passed; `evals/check_hooks.py` — all hook checks passed;
`evals/check_opencode_plugin.mjs` — all 29 checks passed; `docs/ai/engine.py anchors` —
silent; `docs/ai/engine.py sdd-lint` — silent (exit 0).

## Converge findings (append-only)

- **F-1 per spec acceptance / `.claude/rules/records.md` (missing)** — the benchmark record
  `docs/cases/L-2-autoflow-entry-line/benchmark.md` existed only untracked in the working
  tree; the delivered branch did not carry it, and it held three absolute `/tmp` workspace
  paths that `records.md` forbids in tracked files (the static check passed only because
  untracked files are not scanned). Fixed in-stage: paths masked to `<eval-ws-N>`
  placeholders, record committed to the task branch. Not CRITICAL: no delivered artifact
  ever violated the rule.
- **F-2 per `docs/backlog.md` register row L-2 (contradicts)** — the row's status line
  claimed "draft PR pushed 2026-09-06"; `gh pr list --head bl/l2-autoflow-entry-line`
  returns `[]` — no PR exists (stage 9 opens it). The register misstated a delivery act.
  Fixed in-stage: status line corrected to the true state (branch carried, PR opens at
  delivery). Not CRITICAL: a register-accuracy defect, not a constitutional breach.

Both findings were fixed within this stage's sanctioned writes (task-branch commits; the
backlog row is DP-1-sanctioned) rather than routed to a stage-6 build cycle: they are
register/record corrections touching no law text, no code, no templates — the tier-0
remediation home this case's route deviations name. Converge re-ran after the fixes:
gates green, sdd-lint and anchors silent. No open converge rows remain.

## Stand cleanup and the deployed build's fate

Nothing was deployed: no edition was cut and no governed repository was re-legislated (DP-3
keeps `docs/ai/**` and sibling repositories out of this run's reach). The deliverable is the
task branch itself, kept, with its push belonging to stage 9 as the run's one sanctioned
push. Receipt carried forward: the baseline comparison worktree `<eval-ws-base-tree>`
(detached `696fdc0`, registered in this repository's `.git/worktrees`), whose removal at run
end belongs to the orchestrator (tracked in the handoff's `stand_receipts`).
