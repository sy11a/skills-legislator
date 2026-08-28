# BL-075 — Engine `detect`/`apply`/`verify`/`report`: the run record and the Step-7 emitter (edition v24)

**Tier: 2 (full).** Blast radius: Steps 1, 3, 6 and 7 of every installing
and maintaining run in every legislated repo — the owned layer is written
by the engine instead of the model; the Step-7 report becomes an engine
print with pinned model slots; `SKILL.md` Steps 1/3/6/7, the engine, the
grader's report asserts, five corpus scenarios and their mutations, the
harness ground rules. Novelty: the engine's first job that writes the
owned layer (ADR-0006), and the first run record — a fact file one job
writes and later jobs read.

**Spec type: feature.** Edition branch `bl/075-edition-v24` (one MR per
version). Sources: BL-047's ranked list groups 6 and 7 (sk-3…sk-13,
sk-23…sk-26), BL-049's emitter contract, BL-066's composition clarify
("Step-7/restructure emitters ride v24 with engine apply/verify").
Scope clarified 2026-08-28: **apply/verify + detect + the Step-7
emitter**; the restructure emitter and the fidelity job are BL-076 (v25).

## Boundary

**In:** four engine jobs (`detect`, `apply`, `verify`, `report`) and the
run record they share; SKILL.md Steps 1/3/6/7 rewired to invoke them;
ADR-0006; the Step-7 model-findings channel; the scaffold report as a
persisted artifact; red-first engine unit tests; new corpus asserts and
their mutations; the harness ground rule that named `cp`.

**Out:** the restructure emitter and the fidelity job (BL-076); Step 4's
template scaffolding and placeholder derivation (the model's, unchanged —
`apply` only *records* which Step-4 targets existed before the run);
Step 5; any change to the audit job; migrating or deleting existing
asserts (D4, owner-reviewed); the hooks guard (Bash is not guarded, the
engine is invoked through it).

## Requirements

### The run record

- **R-751** — WHEN `apply` runs THEN it SHALL write one run record — a
  JSON file at `--record <path>`, defaulting to
  `<tempdir>/legislator-runs/<root basename>-<sha1(resolved root)[:8]>.json`
  — carrying the mode, the version, the stack list, every owned-file event
  (created / overwritten / deleted, by repo-relative path), the keep-list
  delta (added / removed / refused with reason), the file-model actions,
  and a pre-run snapshot: which Step-4 targets existed and which
  `@docs/ai/rules/` lines the entry document carried.
- **R-752** — The run record SHALL live outside the target repository
  (never under `--root`), so a run leaves no untracked file behind and the
  idempotency contract holds by construction.

### `detect` (Step 1 + Step 2's signals, zero writes)

- **R-753** — WHEN `engine.py detect --skill <p>` runs THEN it SHALL print
  one JSON object — `mode` (`upgrade` / `migration` / `fresh`), `entry`
  (`AGENTS.md`, `CLAUDE.md` or null), `reconstructed` (the manifest-less
  already-legislated edge case), `manifest` (as read, or null), `stacks`
  (`subscribed` from the manifest — legacy `profiles` read as the same
  field — and `candidates` from the stack signals), `ownedFilesOld`
  (the manifest's list, or the on-disk reconstruction) — exactly per
  Step 1's decision order, writing nothing, exit 0.

### `apply` (Step 3 whole)

- **R-754** — WHEN `engine.py apply --skill <p> --stacks <a,b>` runs THEN
  it SHALL copy every file under `assets/rules/core/` and each named
  stack's directory to `docs/ai/rules/…`, `assets/templates/opencode.json.tpl`
  to `opencode.json`, and `assets/engine/engine.py` to `docs/ai/engine.py`,
  byte-for-byte, and compute `ownedFiles` as exactly those paths, sorted.
- **R-755** — WHEN a path in the old `ownedFiles` list is absent from the
  new one THEN `apply` SHALL delete it and remove a
  `docs/ai/rules/stacks/<stack>/` directory the deletion emptied.
- **R-756** — `apply` SHALL carry the manifest's `keep` list forward
  (default `[]`), apply `--keep-add <path>::<reason>` only when the path
  exists, is not in the new `ownedFiles`, and is not `docs/ai/baseline.md`
  — refusing otherwise with the reason recorded — dedupe by path
  (re-marking replaces the reason), and remove an entry only on
  `--keep-remove <path>`.
- **R-757** — `apply` SHALL write `docs/ai/manifest.json` in the pinned
  serialization (2-space indent, key order `legislatorVersion`, `stacks`,
  `keep`, `ownedFiles`; `stacks` single-line inline; `keep` as `[]` inline
  when empty, else one sorted single-line object per entry with keys
  `path`, `reason`; `ownedFiles` one entry per line, sorted) — byte-stable
  across runs with no change.
- **R-758** — `apply` SHALL ensure the v14 file model: a real `CLAUDE.md`
  with no `AGENTS.md` is renamed (`git mv` when tracked, else a plain
  rename), and `CLAUDE.md` ends as a symlink → `AGENTS.md`.
- **R-771** — WHERE a real `CLAUDE.md` and a real `AGENTS.md` both exist
  THEN `apply` SHALL stop with an exit outside {0, 1} naming the
  conflict, writing nothing.
- **R-759** — `apply` SHALL write nothing but the owned set, the
  manifest, the file-model wiring and the run record (ADR-0006); a
  crashed `apply` exits 3.

### `verify` (Step 6)

- **R-760** — WHEN `engine.py verify --skill <p>` runs THEN it SHALL
  byte-compare every `ownedFiles` entry against its skill source,
  re-copy a diverged file once and compare again, and confirm every
  file-target row of Step 4's table (parsed from `<skill>/SKILL.md`, the
  grader's own derivation) exists — printing each residual failure and
  exiting 1 on any, 0 when clean.
- **R-761** — `verify` SHALL append its result and the post-run Step-4
  snapshot to the run record, so `report` can print `Created` from the
  two snapshots.

### `report` (Step 7)

- **R-762** — WHEN `engine.py report --skill <p>` runs THEN it SHALL print
  the Step-7 report from the run record alone: title
  `# Legislator <Scaffold|Migration|Upgrade> — <repo>, <ISO date>`, the
  pinned `##` sections in order (`Created`, `Overwritten`, `Deleted`,
  `Needs your review`, `Keep list`, `Constitution candidates`, `Health`),
  one pinned line per item, and the emitter stamp
  `Emitted by docs/ai/engine.py report — constitution v<N>` as the last
  line — writing nothing.
- **R-763** — The `Needs your review` section SHALL carry the derivable
  proposals: an `@docs/ai/rules/…` import line to add for every owned
  rule file the entry document does not import, one to remove for every
  such import whose target is no longer owned, and — WHEN this run
  created a Step-4 artifact an existing entry document does not reference
  — the wiring for it (the codebase-map import, the `## Boundaries`
  section, the glossary pointer line).
- **R-764** — WHERE `--model-findings <json>` is passed THEN `report`
  SHALL merge its `candidates` into `## Constitution candidates` (omitted
  when empty; never in scaffold mode) and its `review` lines into
  `## Needs your review`; a malformed file is a loud exit.
- **R-765** — WHILE the mode is upgrade, `report` SHALL append `## Health`
  from audit checks 1–6 executed by the engine on the post-run tree
  (`Health: clean` when none); scaffold and migration reports carry no
  Health section.
- **R-766** — `## Keep list` SHALL appear only when the record carries a
  keep delta or a refusal, each refusal naming its reason (the path does
  not exist, or the path is an owned file — any entry of the manifest's
  `ownedFiles`).
- **R-767** — WHILE the record and the tree are identical, the printed
  report SHALL be byte-stable across runs.

### The law and the harness

- **R-768** — SKILL.md Steps 1, 3, 6 and 7 SHALL instruct the model to
  invoke the four jobs and deliver `report`'s output verbatim — the model
  keeps Step 2's confirmations, Step 4, Step 5, the keep-request parsing
  into flags, and the candidates scan; every derivation the grader and
  `check_static` parse (Step 4's table, the authority table, the Step 3.6
  keep clause, the Step 7 refusal clause, the report skeleton sentence)
  stays parsable.
- **R-769** — The scaffold run's report SHALL be a persisted, graded
  artifact (`outputs/scaffold-report.md` in the harness), like every
  other mode's.
- **R-770** — Every new engine behavior SHALL land with a red-first unit
  check in `evals/check_engine.py`.
- **R-772** — Every new corpus assert SHALL be shown red against the v23
  workspace artifacts and carry a mutation entry.

## The run record (data shape)

```json
{
  "mode": "upgrade", "version": 24, "stacks": ["dotnet"],
  "root": "<resolved path>", "entry": "AGENTS.md",
  "owned": {"created": ["docs/ai/rules/core/x.md"], "overwritten": [],
            "unchanged": ["…"], "deleted": ["docs/ai/rules/core/retired.md"]},
  "manifest": {"written": true, "changed": true},
  "keep": {"added": [{"path": "…", "reason": "…"}], "removed": [],
           "refused": [{"path": "…", "reason": "…"}]},
  "file_model": ["renamed CLAUDE.md -> AGENTS.md", "linked CLAUDE.md -> AGENTS.md"],
  "pre": {"step4": {"docs/okf/index.md": true, "…": false},
          "imports": ["docs/ai/rules/core/okf.md", "…"]},
  "post": {"step4": {"…": true}, "verify": {"clean": true, "failures": []}}
}
```

## The Step-7 model-findings channel

```json
{"candidates": ["- \"<quote>\" — <path>"], "review": ["<proposed line>"]}
```

Both keys optional; `review` lines are pinned-format proposals the model
could derive and the engine cannot (a `.gitignore` entry removed in
migration, a conflict the owner must rule on). Free prose is not a slot
(BL-049).

## Eval design (POLICY §3 — before the change)

| new assert | scenario | artifact | planted defect / bite | negative control | red means |
|---|---|---|---|---|---|
| `report_carries_engine_stamp` | fresh, migration ×2, upgrade ×2 | report | none — red against v23 by construction (no stamp exists) | stamp absent → red | law (SKILL.md didn't wire `report`) or harness |
| `scaffold_report_saved` (probe) | fresh | outputs/scaffold-report.md | absent in every v23 workspace | — | law/harness |
| `report_mechanical_lines_match_engine` | upgrade | report + run record (path derived from the repo root, R-751) | grader re-runs `engine report` on the record and requires every `Created`/`Overwritten`/`Deleted`/`Health` line verbatim | a line the re-run prints that the report lacks → red | law/model (the model edited engine output) or engine |
| `report_created_lists_new_rules` | upgrade | report | the withheld core + stack rule must sit under `## Created` as pinned lines | line under another section → red | engine (record) or model |
| `report_deleted_lists_retired_rule` | upgrade | report | the retired rule under `## Deleted` | — | engine or model |
| engine units: `detect` per mode + edge case; `apply` copy/ownedFiles/deletions/empty-dir/keep rules/manifest bytes/file model/both-exist stop/record shape/writes-nothing-else; `verify` diverged-then-recopied, missing artifact; `report` skeleton, import deltas, wiring proposals, Keep list gating, Health gating, candidates merge, malformed findings, byte-stable | check_engine | temp repos | each behavior planted small | clean tree silent | engine bug |

**Red-first deviation, stated:** `report_created_lists_new_rules` and
`report_deleted_lists_retired_rule` are expected green against the v23
reports (the v23 model already listed them lawfully) — their
falsifiability evidence is their mutation (`remove-lines`), which the v24
mutation pass must show killing them; the stamp asserts, the scaffold
probe and the re-print match go honestly red against the v23 workspace.

Existing report asserts (`report_proposes_*_import_line`,
`keep_refusal_for_owned_path`, the migration harvest pair) stay as
written — they now measure the engine's print through the model's
delivery. The candidates scan stays the model's (sk-27, class (c)).

## Clarifications

### Session 2026-08-28 (edition composition)

- **Q: which slice?** → apply/verify + detect + the Step-7 emitter; the
  restructure emitter and fidelity job are filed as BL-076 for v25.
- **Q: may the engine write the owned layer?** → Yes — ADR-0006 extends
  ADR-0003: `apply` writes exactly the owned set, the manifest, the
  file-model wiring and its run record; nothing else under any sentence.

## The hurting case

GIVEN the upgrade fixture (one version behind, one retired rule, a keep
request for an owned path), WHEN the agent runs the legislator under v24,
THEN the owned layer is byte-identical to the skill source, the retired
rule is gone, the manifest is the pinned serialization with the lawful
keep entry and the refusal, and the report is the engine's print — every
`Created`/`Overwritten`/`Deleted`/`Health` line byte-equal to a re-run of
`engine report` on the same record, the stamp present, the refusal under
`## Keep list` — AND a second run leaves a zero diff. The case that hurts
most: a report that *looks* engine-printed but was hand-composed, or a
run record left inside the repo turning idempotency red — the stamp, the
re-print match and R-752 exist to make both unfakeable-by-accident.

## Converge — 2026-08-29

Judged against R-751–R-772, the boundary and the eval-design table.
R-751/R-752: the record's shape and outside-the-repo home proven by units
and by idempotency (zero diff ×3 with the record in `/tmp`). R-753:
`detect` per mode, the legacy `profiles` read, the reconstruction edge
case, zero writes. R-754–R-759, R-771: `apply`'s copies, ownedFiles,
deletions with emptied-directory cleanup, every keep rule and refusal
reason, the byte-pinned manifest (compared as a whole string), the file
model with `git mv`, the both-real stop (exit 4, tree untouched), the
declared footprint — all unit-proven; the live corpus exercised every
path (25/25 upgrade incl. the owned-path keep refusal). R-760/R-761:
`verify`'s one re-copy, missing-artifact naming, post snapshot.
R-762–R-767: the skeleton, pinned lines, import deltas and scaffold
wiring, Keep-list gating, candidates and review merge, Health from checks
1–6, the stamp last, byte-stability, the malformed-findings loud exit.
R-768: SKILL.md rewired; `check_static` and the grader's derivation
selftest (16/16) prove every parsed clause survived. R-769: the scaffold
report probe is green in the corpus. R-770/R-772: 37 engine units red
first; the corpus asserts' red-first record is in `benchmarks/v24.md`,
including the two stated as mutation-evidenced. Benchmark: 214/214,
idempotency ×3, mutation 214/214 killed, floor sonnet. Member #0
delivered by `engine apply` and byte-verified (`.gitattributes` scaffolded
where `verify` found it missing). Gaps: none (missing / partial /
contradicts / unrequested: none). Residual, stated: `_pid_alive`-class
Windows paths in the engine (`git mv` fallback to `os.replace`) are
unexecuted on this machine.

✅ Converged
