# 0006. The engine writes the owned layer, and the model only invokes it

## Status

accepted

## Context

ADR-0003 narrowed the engine's read-only guarantee to one sentence: check
jobs write nothing; `baseline` writes exactly its declared target. Since
then every installing and maintaining run has still performed Step 3 by
hand — a model issuing `cp` commands, computing `ownedFiles`, diffing the
old list for deletions, serializing the manifest to a byte-pinned shape,
and canonicalizing the file model — and Step 6 by re-diffing its own
work. BL-047 classified all of it bucket (b): mechanical, every-run, the
highest-cadence unenforced mechanics left in SKILL.md. BL-049 measured
the downstream cost: 86% of every report line is a fact the run already
holds, and 22% of the system's classified defect history is report
composition.

## Decision

Edition v24 gives the engine four jobs. `detect` and `report` are check
jobs (they write nothing but stdout). `verify` writes nothing except a
single re-copy of an owned file it found diverged. `apply` writes
**exactly four things**: the owned set (`docs/ai/rules/**`,
`docs/ai/engine.py`, `opencode.json`) byte-for-byte from the skill
source, `docs/ai/manifest.json` in its pinned serialization, the v14
file-model wiring (`CLAUDE.md` → `AGENTS.md` rename and symlink), and its
own run record — which lives outside the repository. ADR-0003's sentence
gains a clause and keeps its shape: *check jobs write nothing; `baseline`
writes its declared target; `apply` writes the owned layer and its run
record; nothing else, under any sentence.*

The model's Step 3 becomes one invocation; its Step 6 another; its
Step 7 delivers `report`'s print verbatim, filling only the pinned model
slots (constitution candidates, derivable-by-model review lines) through
the model-findings channel BL-066 introduced.

## Consequences

- The file-authority table is unchanged: `apply` exercises the `replace`
  cells of owned law and the manifest, and the wiring clause of the entry
  document's `propose-only` right — the same rights, now executed by the
  engine under the model's invocation.
- The hooks write-guard is unchanged: Bash is not guarded, and the engine
  is invoked through it; a model that edits an owned file by hand is
  still blocked.
- The engine's docstring lists the writing jobs by name; a future job
  that writes extends the list explicitly. Silence stays a guarantee.
- A crashed `apply` exits 3 with a partial tree possible — `verify` is
  the repair (re-copy), and the run record says what was touched.
- The restructure emitter and the fidelity job (BL-076) will read the
  same record; `heal` is already defined as "Steps 2–3 as-is", which now
  means `apply`.
