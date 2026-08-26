# BL-069 — Spike: the dependency register — what this system stands on, and the policy for adding more

**Tier: 1 (light).** Blast radius: none in code — no `skill/` change, no
VERSION, no benchmark. Novelty: no surface of this system declares what it
depends on; every absence behavior is folklore until measured.

**Spec type: exploration.** Branch `bl/069-dependency-register`. Backlog
entry of 2026-08-26; since ADR-0005 this spike is also **gate 1 of
phase 2**: the register is what BL-072's design reads to know what the
binary arm replaces and what it may itself depend on.

## The question

Which dependencies are already embedded in the system, how does each behave
when missing, where (if anywhere) is it declared — and by what rule may a
new one enter?

## Scope

**In:** every external tool or runtime any surface invokes — interpreters,
the VCS toolchain, formatters, OS utilities, the eval substrate, the
harnesses themselves — across all surfaces BL-068 enumerated (hooks,
opencode plugin, engine, `tools/*.sh`, eval suite, SKILL.md procedure).
Plus the policy half: a proposed adoption rule, drafted as law-shaped text,
and tested against the three known candidates (the constitution DB, the
ADR-0005 binary arm, BL-067's analyzer binding).

**Out:** BL-068's territory (where our own code breaks per OS — done;
this spike cites its axes, never re-measures them); adding or removing any
dependency; writing the policy into any law file (it is a constitution
candidate — a proposal the owner promotes via an edition, never a silent
write).

## Method

- **Register row per dependency:** name → surfaces that invoke it (named
  file/line class), **class** (`hard` — the surface cannot do its job
  without it / `best-effort` — designed to degrade gracefully /
  `operator-side` — needed on the operator's machine only), **declared
  where** (today: mostly nowhere — the known exceptions are the
  `python3`-absent branches in `core/verification.md` and audit checks
  15/17, and check 14's machine-relative skill list), and **absence
  behavior** with an evidence class: `measured` (executed here with the
  dependency hidden from PATH) or `inspected` (a named guard/branch in the
  code). No "probably fine".
- **Measurement depth** — per the clarify decision below.
- **Absence-behavior taxonomy:** `fail-open` (allows, enforcement quietly
  absent) / `fail-loud` (states it could not run) / `crash` / `silent
  false green` (the worst class — reports success while having measured
  nothing).
- **The policy draft:** law-shaped, short, checkable — what a candidate
  dependency must bring: a stated class, a defined absence behavior
  (fail-open for enforcement arms, fail-loud for verification rungs, never
  silent false green), a cross-platform story on BL-068's axes, and a
  declaration home a machine can be checked against. Generalizes the one
  dependency rule that already exists as code: `check_static.py`'s
  "engine imports only stdlib".

## Acceptance (the case it would hurt to get wrong)

GIVEN the system at v22, WHEN the register is complete, THEN every external
invocation found by the sweep appears in exactly one row with class,
declaration status and absence behavior carrying an evidence class — AND
every `hard` dependency of an enforcement arm or verification rung has its
absence behavior **measured**, not inspected — AND the policy draft is
concrete enough that applying it to the three candidates yields a verdict
per candidate. The case that hurts most: a dependency whose absence
produces silent false green and whose row says `fail-loud`.

## Deliverable

`docs/cases/BL-069-dependency-register/register.md` — the register, the
measurements, the policy draft, the three candidate verdicts; summary to
the backlog (status flip) and the day's journal.

## Stop condition

The register and the proposed policy are the deliverable. No dependency is
added or removed; no law file is written; promotion of the policy text is
the owner's act in a later edition.

## Clarifications

### Session 2026-08-26

- **Q: measurement depth — full matrix or load-bearing cells?** →
  **Load-bearing cells.** Absence behavior is *measured* (dependency hidden
  from PATH, surface executed) only where a silent failure is expensive:
  the enforcement arms and the verification rungs. Everything else is
  `inspected` with a named guard line, BL-068-style. The owner's standing
  direction rides this answer: verification cost is already too high —
  this system optimizes the cost of its checks down, never up; a spike
  that inflates eval time to confirm guards already visible in code is
  waste (the same economics that motivate BL-048 and BL-049).

## Converge — 2026-08-26

Judged against the spec and the backlog entry: every external invocation
the sweep found has exactly one register row with class, declaration
status and absence behavior carrying an evidence class; every hard
dependency of an enforcement arm or verification rung is measured (M1–M6),
not inspected; the acceptance's hurting case fired for real — a
dependency (git, under okf-debt) whose absence produces silent false
green — and is recorded as F1 with its S fix filed into BL-070, not
fixed here (stop condition holds: nothing added, removed, or written into
law). The policy draft is concrete enough that all three candidates got
verdicts. Measurement scope stayed at load-bearing cells per the clarify;
no eval-time inflation. Backlog, changelog, journal, glossary and OKF log
updated. Verification: check_static, check_engine, engine anchors,
sdd-lint all clean. Gaps: none (missing / partial / contradicts /
unrequested: none).

✅ Converged
