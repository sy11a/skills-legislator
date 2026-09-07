# 0010. Info does not raise the audit's exit code

## Status

accepted

## Context

The audit exits 1 when it found something and 0 when it did not, and an exit
outside that pair means the audit itself failed — a contract `verify`, the
verification ladder and every future host read.

Edition v26 added check 20, `arm-integrity`, which asks the installed binary
what it is and compares the answer to the edition's release record. An edition
that has not been tagged has released no digests, and the check says so rather
than inventing a fault: it adds one **Info** line, *"edition <N> records no
released digests yet"*. That line is true, unactionable, and present in every
audit of every repository until the edition is tagged.

With the old reading — any finding raises the exit code — a clean repository on
an untagged edition exits 1. The eval ruler caught it as R-661's
`audit_clean_repo_clean_report` going red, and the shape generalises past this
one check: an Info line is by construction not a finding to act on
(`core/artifact-lifecycle.md`: every surfaced worklist item is an action), so a
contract that treats it as one teaches callers to stop reading the exit code —
which is the one signal the ladder rests on.

Raised as a decision-gate stop during BL-082 T-13 and ruled by the operator on
2026-09-07. Three options were weighed: (a) Info does not raise the exit code;
(b) the eval fixture pins released digests so the line never appears; (c) check
20 stays silent until an edition is tagged. (b) and (c) both keep the old
contract by hiding a true statement — one from the corpus, one from every user —
which is the failure mode `no silent caps` names.

## Decision

**Only Warning and above raise the audit's exit code. Info alone exits 0.**

The report is unchanged: Info findings are printed, under their own heading,
exactly as before. What changes is the arithmetic behind the exit code —
`AuditJob` counts findings whose severity is not `Info`.

A consequence taken with it, recorded because it is not obvious: the pinned
*clean shape* is asserted as **the absence of the Critical and Warning
sections plus an exit of 0**, not as the presence of the line `No findings.`.
A repository can be clean while the machine it is audited on has something
worth saying about itself, and on an untagged edition that is the normal case.

## Consequences

- A caller can read the exit code again: non-zero means there is something to
  act on. That is what `verify` and the ladder need, and what a future MCP host
  will need without knowing anything about severities.
- An Info-only audit is invisible to a script that reads only the exit code, so
  a genuinely useful Info line reaches a human only through the report. That is
  the intended cost: Info is for a reader, not for a gate.
- Both arms must spell the rule identically — the audit's check set and its exit
  contract are law, not implementation. There is one arm today (`AuditJob`); the
  opencode plugin does not run audits.
- The eval corpus asserts the new shape in three places (the ruler's R-661 pair,
  `AuditTwins`, `AuditReportTests`), so a regression to the old arithmetic is
  red in seconds rather than discovered on a fleet member.
