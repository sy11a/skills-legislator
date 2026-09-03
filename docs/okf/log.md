---
type: Changelog
title: OKF Bundle Changelog
description: Chronological record of significant changes to the OKF knowledge bundle.
tags: [changelog, okf]
timestamp: 2026-09-03T00:00:00Z
---

# OKF Bundle Changelog

## 2026-09-03 — BL-082 T-10: the write path, and a ruler measured with a dead arm

`apply`, `verify` and `report` exist in .NET, and with them the run record, the
keep rules, the owned set and the v14 file model. The engine ruler now reads
142 ok / 0 FAIL on the published binary and the label ledger fell from 96 to 60.
`glossary.md` gained **owned set**, **case collision**, **verify job** and
**report job**, and re-homed **run record** and **apply job** onto their .NET
files.

The entry worth recording is what the green number does not say. Re-run with
`PARITY_ENGINE_CMD=/bin/false` — an arm that answers nothing — nineteen labels
stay green. Two are the known `usage` pair that still hardcodes the interpreter
and belongs to the CLI task; the other seventeen are assertions of ABSENCE (no
finding, no write, no section, two runs equal) which hold vacuously when nothing
runs. So "142 ok" is 123 measured labels and nineteen a dead engine also
satisfies. Four of the nineteen are this task's, and each has a twin that
asserts more than the ruler does — the run exited 0 and printed a report beside
the absence — so the port is measured even where the instrument is not. The
number was written down rather than inherited, which is the only reason it is
now a known quantity instead of a comfortable one.

The second entry is a fake that lied. `MockFileSystem.CreateSymbolicLink`
resolves a relative target against the process's current directory and demands
it exist, where the real `System.IO` does not resolve it at creation at all. The
product's link is relative on purpose — an absolute one breaks when the
repository moves, and the ruler reads `readlink` — so the fake was pointed at
the repository root rather than the product bent to the fake. It is the second
place where this fake's behaviour and the real one's part company; the first was
reading a directory (T-08).

## 2026-09-01 — BL-082 T-07: the pilot port, and a namespace that was not ours

The first engine job exists in .NET: `anchors` is registered, and the ruler
measures it byte for byte on the published binary — all seventeen labels the
job answers are `ok`, and the label ledger fell from 196 to 179 in the same
commit as the twins that lowered it. `glossary.md` gained **source root** (the
directories a symbol-anchor may resolve against, and the two exclusions that
make the check honest) and its **arm** row now names the renamed variables.

The entry worth recording is why they were renamed. The environment layer
admits every `LEGISLATOR_*` name as the option key it spells and refuses an
unknown one by name — so while the binary is the arm, the ruler's own
`LEGISLATOR_ENGINE_CMD` made it exit 2 before reaching a job, and the ruler
measured the collision instead of the port. T-06 could not have seen it: the
registry was empty, every job exited 2 anyway, and the fault hid inside the
expected failure — the same hazard that made the ledger a ratchet rather than a
test left red for five tasks. The rulers moved out of the namespace
(`PARITY_ENGINE_CMD`, `PARITY_HOOK_CMD`); whether an unrecognised `LEGISLATOR_*`
variable should be fatal at all is BL-089.

Two smaller findings, both worth carrying: the options census caught
`max_file_bytes` present in the key map but missing from the composer's
hand-written switch — the guard doing exactly its job; and
`MockFileSystem.File.ReadAllText` returns empty text for a directory where
`System.IO` throws, so a document the process may not read cannot be modelled
in the fake at all. The crash fixtures moved to a real temporary directory.

## 2026-08-29 — BL-082: the deterministic substrate becomes .NET

The glossary gains `deterministic substrate`, `host (substrate)`,
`options model`, `configuration layer`, `provenance (config)` and
`parity ruler`, linking ADR-0008. The codebase map does not change yet:
`src/` and `tests/` exist only in the spec — the map's rows for them
land with the v25 implementation, when `anchors` can resolve them. The
tech-stack line in `index.md` is likewise deferred to that commit.

## 2026-08-29 — BL-077: the outer-only pivot is decided and specified

The glossary gains `control directory`, `instance`, `machine registry`,
`stub`, `sentinel`, `two-root engine`, `paired MR`, `D / A step class`,
`config is code` and `module (legislator)`, all linking ADR-0007. No
concept document changed yet: the pivot is specified (case BL-077), not
built — the codebase map, the engine and the arms change in the v25
implementation, and their documents with them. `docs/ontology.md`
§Placement modes and `docs/philosophy.md` §Horizon carry the decision.

## 2026-08-28 — BL-075: edition v24, the engine writes the owned layer

The glossary gains `run record` and `apply job`, the latter linking
ADR-0006. No concept document changed: the engine grew four jobs in
place, and the codebase map's rows still hold.

## 2026-08-28 — BL-073: the eval workspace lock

The glossary gains `workspace lock`. No concept document changed: the
map's `tools/` row names no filenames, and the harness gained a
primitive in place (`tools/proc.py`), not a new surface.

## 2026-08-28 — BL-048: the per-job model floor

The glossary gains `per-job model floor`. No concept document changed:
the probe and its results are lifecycle artifacts in the case home, and
no code changed.

## 2026-08-27 — edition v23: the audit engine and the case-shape lints

The glossary gains `audit job` and `model-findings channel`. The map's
rows still hold (the engine grew in place; `tools/` gained `proc.py`
under the existing description). Benchmark and defect chronicle:
`evals/benchmarks/v23.md`.

## 2026-08-26 — BL-049: report derivability

The glossary gains `report emitter` — the concept v23's composition will
build against. No concept document changed: the classification is a
lifecycle artifact in its case home, and no code changed in this spike.

## 2026-08-26 — BL-069: the dependency register

The glossary gains `dependency register` (with its absence-behavior
taxonomy — fail-open / fail-loud / crash / silent false green). No concept
document changed: the register is a lifecycle artifact in its case home,
and no code changed in this spike.

## 2026-08-26 — BL-064: the git conduct guard

The glossary gains `git conduct guard`. The codebase map's `plugin/` row
already covers the new hook ("the deterministic enforcement arms"), so no
map change; the plugin's own README carries the concept in depth. The
regenerated `docs/ai/baseline.md` picks up R-641–R-649 alongside the
R-3xx/4xx/5xx/6xx rows the earlier cases had defined since its last
regeneration.

## 2026-08-26 — BL-047: the decision inventory

The glossary gains `decision inventory` — the bucket vocabulary (a/b/c) the
repo will now reason in when sizing engine growth. No concept document
changed: the inventory is a lifecycle artifact living in its case home
(`docs/cases/BL-047-decision-inventory/`), and no code changed in this
spike.

## 2026-08-25 — edition v22: baseline, sdd-lint, and the quotation rule

BL-043 populated the `generated` class: `docs/ai/baseline.md` exists, written
by the engine's third job. The glossary's `baseline` and `generated` rows
drop their "arrives with BL-043" tense; a new `annotated test` row pins the
marker form. BL-057's quotation rule (a backticked token is quotation, not a
placeholder) now governs audit check 2 and sdd-lint's placeholder pass alike.

## 2026-08-25 — `unmeasured` and `declared artifact` enter the glossary

BL-062 gave the eval grader a third verdict. Two terms were minted with it:
**unmeasured**, the verdict an assert carries when the artifact it declared
cannot be read, and **declared artifact**, the source an assert names as data.
Both are `coin` — the field has assert/pass/fail and no word for "this assert
scored a point against nothing", which is precisely the failure BL-060
measured at 32% of one scenario. No concept document changed: the bundle has
none for the eval suite, and `codebase-map.md`'s `evals/` row still describes
it correctly.

## 2026-08-24 — Bundle initialized

Initial OKF bundle scaffolded by the Legislator, during BL-034
(self-legislation). The glossary was not seeded from scratch: the repo's
existing 48-term register at `docs/glossary.md` was migrated forward into
`glossary.md`, keeping its `Status` and `Lives` columns, and the old path was
removed.


## 2026-08-31 — edition v25, the tracker slots

The constitution learns that a repository's work items may live outside it.
Three law sentences (`skills.md` output redirection, `sdd.md` register row,
`pair-development.md` branch convention) name two homes instead of one; the
entry-document and backlog templates gain the pointer shapes; audit check 18
`tracker-drift` guards the seam from inside the repository only. The tracker
itself stays a companion's territory — the constitution never reads one,
never names a vendor, and never migrates a repository. Answers the BL-085 and
BL-087 spikes; the initiating case lives in the companion's repository
(clerk BL-016).

## 2026-09-06 — L-2: task entry defaults to `/autoflow`

The glossary gains `task entry route` and `flow-sessions`, linking
ADR-0009. `core/pair-development.md` states the default (`/autoflow` unless
the operator names `/flow`, conditioned on the skill pair being installed);
this repository's own `.claude/rules/skills.md` gains the `flow-sessions`
class, `autoflow` first. The codebase map does not change. Initiating case:
Architector's Release 0 kernel (`docs/cases/BL-008-release-cycle/`, epic
E-5, story S-5.1); this repository's own case is
`docs/cases/L-2-autoflow-entry-line/`.

## 2026-09-07 — L-2: the generated stage map pins `flow-sessions`

The `flow-sessions` glossary row names its second home: the
`{{SANCTIONED_SKILLS_BY_STAGE}}` derivation bullet in `skill/SKILL.md` pins
`flow-sessions` — `autoflow` — as the first stage affinity, so a fresh
legislated repository's generated `.claude/rules/skills.md` emits the class
wherever the skill is installed. ADR-0009's decision 2 records the
generated half beside the instance half; `skills-rules.md.tpl` needed no
change (it interpolates the derived list verbatim). Case
`docs/cases/L-2-autoflow-entry-line/`.
## 2026-08-31 — `job (engine)` enters the glossary; the map names `src/` and `tests/`

BL-082 T-05 turned the job from a shape inside `engine.py` into a declared
contract — `IJob`, `JobContext`, `JobResult`, `JobRegistry` in
`Legislator.Engine`, dispatched by the CLI host and, from BL-084, exposed one
tool per job by the MCP server. That made **job (engine)** a term the bundle
had used (inside `deterministic substrate`'s own definition) without ever
defining: `coin`, since the field's "task"/"command"/"handler" all carry the
wrong half of the meaning. `codebase-map.md` gained the `src/` and `tests/`
rows it has owed since T-01 created those directories — the table promises one
line per top-level directory, and for four tasks it was two lines short.


## 2026-08-31 — the label ledger, and why it is a ratchet rather than a red test

BL-082 T-06 built the instrument that measures the port instead of trusting it.
Three terms entered the glossary together, because none of them names itself:
**label ledger** (what the rulers assert against what the .NET suite twins),
**arm (under test)** (which implementation a ruler is measuring on this run —
now printed before the first check, since the same output means different
things under each), and **ledger ratchet** (the ledger's form while the port is
under way).

The ratchet is the decision worth recording. The plan had the coverage test
stay red from T-06 to T-11, excluded from CI by a trait. A gate that is red on
every commit for five tasks is not a gate: the next genuine failure arrives
inside the expected one and nobody sees it. So the red R-8206 demands was shown
once against the empty implementation and recorded in the case as
`parity-red.txt`, and the living test asserts the debt equals its written
number — 196 today, zero at T-11 — which fails the moment the debt grows and
also when it falls without the record following. `codebase-map.md`'s `evals/`
row gained the two instruments the port added.

## 2026-09-01 — BL-082 T-08: the git seam, and the second job on the shared OKF rule

`okf-debt` is ported. The job itself is a transliteration; the two decisions
underneath it are not. First, **absent git**: the Python asks `shutil.which`,
which the substrate has no lawful way to imitate — `IProcessRunner` now raises
`ProcessStartException` when the executable cannot be started, `GitLog` turns
that into `GitAvailable = false`, and the job throws where the Python raises, so
the host renders exit 3 with the reason on stderr and stdout stays empty. That
is what the ruler actually asserts; the contract had said "a finding, exit 1"
and was amended in place. Second, **the shared rule**: `anchors` and `okf-debt`
read the same anchored class and the same path-anchor targets, so
`OkfDocuments` and `AnchorTarget` now hold that rule once — two jobs that
disagreed about which file a token means would measure two different
repositories. The glossary gained **git seam**.

## 2026-09-01 — BL-082 T-08: sdd-lint, and where a lint's directories come from

The analyze gate's mechanical passes now run from the binary. The reading rules
the lint shares with the baseline live in `Legislator.Engine.Sdd` —
`CaseModel` for the three definition forms and the line-anchored converge
marker, `Prose` for the rule that a token inside a fence or backticks is
quotation. `RepoLayout` gained `Adr`, `Journal` and `Changelog`: every
directory the lint judges is now named by the options model, so a repository
that calls its cases something else is linted correctly instead of silently not
at all.

## 2026-09-01 — BL-082 T-08: baseline, and a register that claimed coverage nobody wrote

The baseline job is ported, and regenerating the document over this repository
found the thing the port was always going to find: the annotation rule counts a
literal `per R-NNN` wherever it appears in a file whose path says "test", so the
port's own fixtures entered the register as annotations for requirements they do
not test. The fixtures now assemble the marker instead of spelling it, and the
rule's hole is **BL-090** — an annotation and a quotation of one must be
distinguishable, which is the rule `prose_only` already applies everywhere else
in this engine. `SourceTree` in `Legislator.Engine.Anchors` now holds the source
walk that the symbol index and the annotated-test scan share.

## 2026-09-01 — BL-082 T-09: detect ported

The `detect` job is ported: Step 1's decision tree and Step 2's stack signals,
answered off a tree alone and printed as JSON. Three glossary rows were minted —
**detect job**, **skill package** and **reconstructed upgrade**, the last being
the manifest-less repository whose entry document still imports the constitution,
where the owned set is read back off disk rather than assumed absent. The
manifest is modelled as data and never as a typed record, because its shape grows
between editions and detect echoes the whole object back.

## 2026-09-02 — BL-082 T-09: audit ported, and the ruler that measured Python

The `audit` job is ported: fourteen mechanical checks and the pinned report,
byte-stable and writing nothing, failing loud where git cannot run at all.
`glossary.md` gained **audit job**, **model-findings channel** and **clean-checks
line**. The audit is the second caller of the anchors and okf-debt jobs — the
port extracted `AnchorsJob.Unresolved`, `OkfDebtJob.Stale`, `EntryDocument.Of`
and `Prose.Placeholder()` for it rather than deriving any of them twice. Four
`check_engine.py` checks that hardcoded the interpreter now follow
`PARITY_ENGINE_CMD`; the two `usage` labels keep the fault until T-12 by the
T-07 ruling.
