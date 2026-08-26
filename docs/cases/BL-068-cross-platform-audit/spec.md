# BL-068 — Spike: cross-platform portability — where does the system break outside Linux?

**Tier: 1 (light).** Blast radius: none in code — no `skill/` change, no
VERSION, no benchmark. Novelty: the system has never been examined as a
deployment onto another OS; every portability property today is accidental.

**Spec type: exploration.** Branch `bl/068-cross-platform-audit`. Source:
the BL-064 review question (2026-08-26); backlog entry of the same date.
The deliverable is an answer, not code.

## The question

Which executable surfaces of this system are OS-portable, which degrade
silently, and which break an invariant — and is the right fix a set of
small patches or a migration of the logic to one cross-platform runtime?

## Scope

**In — every executable surface, per the backlog entry:**

- Claude Code hooks (`plugin/hooks/*.py` + `hooks.json` invocation lines)
- the opencode plugin (`plugin/opencode/legislator-guard.ts`)
- the delivered engine (`skill/assets/engine/engine.py` → `docs/ai/engine.py`)
  and every place the law names its invocation
- operator tools (`tools/*.sh`)
- the eval suite (`evals/*.py`, `evals/check_opencode_plugin.mjs`,
  fixture builds)
- the file model and git configuration: the `CLAUDE.md → AGENTS.md`
  symlink, `core.autocrlf` vs the owned-file byte-diff, the Bash steps
  inside `SKILL.md`'s own procedure (`ln -s`, `cp`, `mv`)

**Out:** writing any fix (each fix class becomes its own case, sized from
the table); re-running BL-069's territory (which external tools we depend
on and the adoption policy — this spike asks where *our own code* breaks);
BL-044/BL-052's territory (harness support, not OS support).

## Method

- **Axes:** Linux (the baseline — the whole fleet today), macOS,
  Windows-native (with Git Bash present, since Claude Code's Bash tool
  requires it), WSL is out of scope as an answer (clarified: native only) and appears only as an informational note.
- **Verdict per surface per axis:** `fine` / `degrades` (keeps working but
  quietly stops doing part of its job — a missed block, a skipped check) /
  `breaks` (an invariant is violated or a run fails).
- **Evidence class per row, stated honestly:** `verified` (executed on this
  machine), `inspected` (read from the code — a named line/construct), or
  `reasoned` (documented OS semantics; no execution). No row without a
  named mechanism — "probably fine" is not a verdict.
- **The verdict half:** the patch list (each degradation/break → cheapest
  fix, cost S/M/L) set against the port option — with the constraint the
  backlog entry pins: only a port that keeps the engine a byte-verifiable
  delivered *text* respects the owned-file law (.NET file-based apps are
  the candidate; compiled per-platform binaries are ruled out), and the
  opencode arm stays TS regardless.

## Acceptance (the case it would hurt to get wrong)

GIVEN the system at v22, WHEN the audit is complete, THEN every surface in
scope appears in the table with a verdict per axis, a named mechanism, and
an evidence class — AND every `degrades`/`breaks` cell names its cheapest
fix with a cost — AND the patch-vs-port recommendation is stated with its
decision criterion, so the owner can overrule it by disagreeing with the
criterion, not by redoing the audit. The case that hurts most: a `fine`
verdict that is actually a silent `degrades` — enforcement that looks
installed on Windows while blocking nothing.

## Deliverable

`docs/cases/BL-068-cross-platform-audit/audit.md` — the classified table,
the patch list, the recommendation; summary to the backlog entry (status
flip) and the day's journal.

## Stop condition

The table and the recommendation are the deliverable. No fix is written;
no dependency is added; the port decision itself stays the owner's.

## Clarifications

### Session 2026-08-26

- **Q: which Windows story is the target?** → **Native only.** WSL is not an
  answer: the system must work in a plain Windows environment without
  subsystems. (Git Bash is not a subsystem for this purpose — the system
  already requires Git, and Git for Windows ships it; Claude Code's Bash
  tool on Windows requires it anyway.) WSL appears in the table only as an
  informational note, never as a fix.
- **Q: operator side too?** → **Yes, everything portable.** `tools/*.sh`
  and the eval runners are audited on the same footing as the fleet-side
  surfaces — the operator may sit on Windows. "Linux-only by declaration"
  is off the table; a surface that cannot be made portable must say so in
  the verdict, not hide behind a declaration.

## Converge — 2026-08-26

Judged against the spec and the backlog entry: every surface in scope
appears in `audit.md` with a per-axis verdict, a named mechanism (file and
line where inspected), and an evidence class; every non-fine cell names
its cheapest fix with cost; the totals are a per-row recount (21 surfaces
× 3 axes); the patch-vs-port recommendation states its criterion so the
owner can overrule the criterion rather than redo the audit. WSL appears
only as an informational note and operator surfaces were judged on the
same footing, per the clarifications. No fix written, no dependency
added, no VERSION moved. The OKF bundle is untouched by design: no
concept changed, no new domain term — backlog, changelog and journal
carry the record. Verification: check_static, check_engine, engine
anchors, sdd-lint all clean. Gaps: none (missing / partial / contradicts
/ unrequested: none).

✅ Converged
