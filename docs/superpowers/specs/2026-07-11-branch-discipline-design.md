# Branch discipline + BL-017 riders — design (BL-018, constitution v10)

**Status:** approved 2026-07-11 (plan-mode approval: WIP-limit-1 + stacking,
both layers, triage). Historical record — do not rewrite.

## Problem

`core/pair-development.md` mandates branch-per-task and user-only merges
but sets no integration deadline. At AI-assisted pace, tasks are produced
faster than PRs merge: CareerPlatform accumulated 30+ local branches (most
of them ghosts of squash-merged PRs — GitHub squash-merge never marks the
local branch merged) plus genuinely stale work, and every new branch cut
from an old checkout made the next merge worse. The fix is the trunk-based
one: integrating is part of the task (WIP limit 1), dependent work stacks,
micro-changes batch.

## Decisions

1. **Constitution amendment (VERSION 9 → 10), `core/pair-development.md`.**
   One new bullet after "Each task gets its own branch": integrating is
   part of the task — a task is not done until its branch is merged or
   explicitly parked; never cut a new task branch while an unmerged one
   exists (surface the pending merge as the blocking next step — merging
   stays the user's act); stack dependent work and merge bottom-up; batch
   micro-changes into the open branch; cut new branches from freshly-pulled
   main. Branch-per-task and never-merge-yourself stay unchanged — control
   is preserved by gates, not branch count.
2. **Project layer** (outside this repo): CareerPlatform gets
   `.claude/rules/branching.md` with the concrete mechanics (WIP limit,
   `git rebase --update-refs` stacking, 2-working-day cap, `chore/`
   batching, delete-after-merge). Written 2026-07-11; re-landing pending —
   the first commit (0dd0ebd) was orphaned by a concurrent branch cleanup
   in that repo.
3. **BL-017 riders ship in this cycle** (check 12 hardening + fixture):
   - R1: kept paths (manifest `keep`) exempt from check 12 — the keep
     entry is the user's ruling; the audit must not prescribe an action
     restructure refuses to execute.
   - R2: `docs/okf/index.md` joins the recognized referrer set (it is a
     surface sessions load via `core/okf.md`); finding text reworded to
     "not wired into the AI layer"; precedence pinned: a file flagged by
     check 12 is not also listed by check 7, and files listed by check 9
     (foreign structures) are reported there only.
   - R3: root-level exemption list extended with the conventional-doc set
     (`CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `LICENSE.md`,
     `SUPPORT.md`, `GOVERNANCE.md`) — fleet audits must not tell users to
     dissolve their GitHub community files.
   - R4: the upgrade fixture also withholds the alphabetically last
     `stacks/dotnet` rule (today: `data-access.md` — exactly v9's fleet
     delta, never before exercised) and the grader asserts both the
     delivered file and the Step 7 report's proposed `@…` import line;
     the upgrade eval now saves its report to `outputs/step7-report.md`
     like migration does.
   - R5 (BL-017 item 5, law): captive-dependency bullet added to
     `stacks/dotnet/architecture.md` — no singleton service may capture a
     scoped or transient dependency (incl. DbContext); this rides the
     VERSION bump this cycle already pays for.

## Eval plan

Upgrade scenario gains 3 assertions (withheld stack rule delivered, report
saved, proposed import line for it present). No audit/restructure fixture
changes — the planted stray rulebook is unkept, lives under
`docs/superpowers/`, and triggers none of the new exemptions, so v9's
markers stay comparable. Full benchmark per `evals/README.md` recorded as
`evals/benchmarks/v10.md` vs `v9.md`.

## Out of scope

- Enforcing the WIP limit mechanically (a hooks-plugin candidate — PreToolUse
  on `git switch -c`/`checkout -b` warning when an unmerged branch exists);
  logged as a rider for a future hooks cycle.
- The §3 restatement-path fixture lock (BL-016 rider) — unchanged, still open.
