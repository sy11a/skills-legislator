# BL-051 — summary

**✅ Converged** 2026-08-25. Judged against every promise in `spec.md`, not
against the diff. Full measurement record: `evals/benchmarks/v21.md`.

## Requirements

| | verdict |
|---|---|
| R-001 `status: removed` produces no finding in either job | ✅ `removed_doc_not_anchored`, `removed_doc_no_debt`, both red first |
| R-002 `core/okf.md` states the exemption at the class | ✅ beside the human class, with the reason |
| R-003 nested build output stays unresolved | ✅ `nested_build_output_ignored` — red was `exit=0 out=''` |
| R-004 real source still resolves | ✅ control, green throughout |
| R-005 unhandled exception exits 3 to stderr | ✅ `crash_exits_distinctly` — red was `exit=1 out=''` |
| R-006 exit codes 0/1/2 unchanged | ✅ regression assert |
| R-007 checks 15/17 name the non-zero exit rule | ✅ |
| R-008 checks 15/17 carry the `python3`-absent branch | ✅ |
| R-009 keep refusal covers the whole owned set | ✅ both places |
| R-010 a fixture reaches check 15's engine-absent branch | ✅ `audit-engine-absent` 5/5 |
| R-011 VERSION 21, benchmark recorded against the baseline | ✅ `evals/benchmarks/v21.md` |

## Hurting cases

**H-1 — a repository following the checklist must not wedge. ✅**
`docs/okf/payments.md` at `status: removed` naming deleted code produces no
finding, so the rung does not block unrelated work.

**H-2 — a broken engine must never read as a clean audit. ✅**
Exit 3 on an unhandled exception; checks 15/17 report any exit outside `{0,1}`
as a check failure. The red that proved it was one line: `exit=1 out=''` — a
crash prints nothing to stdout, and stdout was all those checks read.

## What the cycle cost, and what it bought

Six full corpus attempts; one reported. Two were killed by tmpfs quota
exhaustion (BL-059) and three found law defects that had nothing to do with
this case's five items — the corpus was measuring the law, and the law had
five latent ambiguities in it. Each fix reset the law generation, so the
counter restarted five times before a clean pass.

The five are chronicled in `evals/benchmarks/v21.md`. Four are
scope-or-completion defects; one had been filed model-class in v18, left
unfixed, and returned three editions later.

## Amendments made during the case

**R-008 was written too broadly** — it forbade every reference to
`docs/glossary.md`, which this case's own record must make. Amended with the
reason in `## Clarifications`; the requirement was fixed rather than the record
reworded around it. (That amendment belongs to BL-034, whose spec carries it;
noted here because the same discipline applied twice this cycle.)

**One assert was itself defective and was repaired, not accommodated.**
`check_15_names_nonzero_exit` required "exit" and "check failure" within 120
characters — it measured sentence layout, not the obligation. Rewritten as two
independent signals and re-proved red against the v20 text, which carries
neither.

## Filed, not fixed

- **BL-055** `fleet.sh` cannot see this repo at depth 5.
- **BL-056** `fleet.sh status` reads the working tree, not HEAD.
- **BL-057** audit check 2 cannot tell a quoted token from an unfilled one.
- **BL-059** the dotnet `.so` leak; detection shipped, prevention open.
- **BL-060** the eval suite's false green — a third of the audit scenario
  passes with no report at all.

## Delivered to this repository

Release step 4, its first execution: `skill/VERSION` 21 copied onto this
repo's owned layer, 13 files byte-verified identical to `skill/assets`, engine
clean, all four static suites green.
