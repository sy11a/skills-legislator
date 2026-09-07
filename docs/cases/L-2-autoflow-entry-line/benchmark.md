# L-2 benchmark — full e2e corpus, branch `bl/l2-autoflow-entry-line`

**Result: 216/216 measured, 216 passed, 0 unmeasured — 100%. Idempotency zero-diff ×3.**

Lifecycle record of this case (see `## Record home` below): the branch's
behavioral `skill/` change (commit `89ce886` — the `{{SANCTIONED_SKILLS_BY_STAGE}}`
derivation now pins `flow-sessions` first) measured against the whole
`evals.json` corpus per `.claude/rules/evals.md`.

## Per scenario

| Scenario | This run | Reference |
|---|---|---|
| `upgrade` (smoke) | **25/25** | v24 25/25 · v25 not run |
| `legacy-migration-agents-first` | **23/23** | — |
| `fresh-scaffold-dotnet` | **21/21** | — (the changed derivation's primary surface) |
| `legacy-migration` | **28/28** | — |
| `audit` (rotted-layer) | **50/50** | v25 50/50 — equal |
| `restructure` | **38/38** | v25 38/38 — equal |
| `upgrade-drop-stack` | **17/17** | — |
| `case-practice` | **8/8** | — |
| `audit-engine-absent` | **6/6** | — |
| Idempotency (fresh, upgrade, restructure) | **zero ×3** | v24 zero ×3 · v25 not run |

## Harness

| | |
|---|---|
| Workspace | `<eval-ws-2>` (materialized from this branch's tree) |
| Law generation | `v25-89ce886-gf3b1317` (grader stamped at grade time) |
| Runner profile | `claude` (the `opencode` profile stays frozen, BL-054) |
| Model | `sonnet`, pinned — the v24 precedent |
| Wall time | ≈ 74 min (15:15:37 → 16:29:59, 2026-09-07) |
| Grader | unchanged from v25 (`evals/**` untouched by this branch — same asserts both sides of the comparison) |

## The haiku smoke reds, classified (run 1, 2026-09-07 14:55)

The first staged run used the profile default (`claude`/`haiku`, workspace
`<eval-ws-1>`); smoke `upgrade` graded 23/25 with
two reds — `mode_respects_authority` (the agent renamed/canonicalized
AGENTS.md and applied wiring directly, an upgrade-mode propose-only
overstep) and `report_mechanical_lines_match_engine` (the report lost the
engine's `Health: clean` line).

Classification evidence: a baseline run of the same scenario, same runner
and model, against the **unchanged law** (`master` @ `696fdc0`, workspace
`<eval-ws-base>`) graded **21/25**, failing
`mode_respects_authority` identically plus three further per-run reds
(`report_proposes_core_import_line`, `report_proposes_stack_import_line`,
`project_owned_files_untouched`). No red on either side names `skills.md`
or the changed derivation; the grader was identical across runs; zero
asserts unmeasured. Verdict per `POLICY.md`'s classes: **model** (haiku's
upgrade-mode behavior is unstable on both laws) — the law change is
exonerated by the discriminator. Per the README's re-run rule the
measurement moved to the pinned-sonnet profile above, which is clean.

## Record home

This file is the case's lifecycle record. The edition's own
`evals/benchmarks/v<N>.md` is written by the release runbook when the
operator cuts the edition at merge — an edition number is never reserved on
this task's own authority (DP-1 of the case spec's decision policy). The
pull request body carries these numbers and proposes the edition.
