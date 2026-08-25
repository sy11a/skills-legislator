# BL-057 — the red demonstrations (POLICY §3)

Both directions of the quotation rule were shown RED against the unchanged
v21 law before the fix landed, per §3's red-before-green.

## The absent-marker, red against v21

Workspace `red57`, materialized from a worktree at commit `2dc4e86` (the
fixture-plant commit — v21 law, new fixture), targeted `rotted-layer` run,
runner=claude model=sonnet, 2026-08-25 23:50–23:56.

The v21 agent's own report, verbatim (line 12):

> - [unresolved-placeholders] `docs/okf/templating-notes.md`: contains
>   literal `{{PROJECT_NAME}}`, `{{PROJECT_OVERVIEW}}`, `{{STACK_SUMMARY}}`
>   tokens. The check's only exemption is `docs/adr/template.md`; this file
>   is not it → rewrite the example so it isn't a literal `{{TOKEN}}` match
>   (e.g. escape the braces), or get it added to the exemption list.

The assert `report does NOT contain 'templating-notes.md'` graded FAILED
("false-positive finding present") — exactly the false Critical BL-057
measures, produced by an agent obeying the v21 law to the letter. Note the
agent's own remediation ideas are the two BL-057 rejected: mangle the prose,
or grow an exemption list. The fix is the check, not the prose.

Incidental observation from the same run, out of scope but recorded: seven
adjacency report-markers (`orphan-docs] docs/okf/orphan-notes.md` etc.)
failed although every finding was present and correct — this agent wrapped
paths in backticks (`[orphan-docs] ` + backticked path), and the substring
marker assumes no typography between slug and path. Same class as defect
15's order-independence repair (v18); a flaky candidate for any benchmark
run. Grader class if it recurs.

## The whole-tree token scan, red against the run-1 artifact

The v22 baseline's restructure run 1 left the planted `{{PROJECT_OVERVIEW}}`
unfilled; the porcelain-only `no_unresolved_placeholders` graded that tree
**38/38** ("no stray {{TOKEN}}s") because the untouched file never entered
the scan; run 2 filled it and only the idempotency diff exposed the pair.
Re-graded on the reconstructed run-1 state with the whole-tree scan:
**35/38, `unfilled tokens in: ['docs/okf/overview-draft.md']`**. The same
scan leaves `templating-notes.md` (tokens in backticks and a fence, present
in the same tree) unflagged — the quotation rule holding in the grader on a
live artifact.

## The delivered-engine assert, red against v21

`delivered_engine_sdd_lint_clean` on the baseline workspace's case-practice
repo (engine delivered by v21 agents):
`FAIL — exit=2: usage: python3 engine.py {anchors|okf-debt}` → 7/8.
