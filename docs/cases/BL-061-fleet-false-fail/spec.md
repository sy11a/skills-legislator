# BL-061 — `fleet.sh`'s FAIL branch trusts the exit code and never checks the version

**Tier: 1 (light).** Tooling only (`tools/fleet.sh`): no `skill/` change, no
VERSION, no benchmark. Branch `bl/061-fleet-false-fail`; BL-056 rides it
(case: `docs/cases/BL-056-status-committed/`). Ordered ahead of the v22 fleet
sweep by the owner (2026-08-26): the sweep uses the very instrument these two
cases repair.

**Spec type: bugfix.**

## Current behavior

The upgrade loop's success branch re-reads the manifest (a runner can exit 0
having achieved nothing — BL-053's `WARN`); the failure branch has no such
scepticism: any non-zero exit prints `FAIL` without asking whether the
repository advanced. Measured on the 2026-08-25 v21 sweep: the session limit
hit mid-run, `fleet-obs` was reported `FAIL — claude exited non-zero`, and its
owned layer was afterwards verified 16/16 byte-identical to v21. The work was
complete and the tool said it had failed.

## Requirements

R-301 — WHEN a runner invocation returns, THEN the loop SHALL re-read the
target's manifest in **both** branches and decide the repo's outcome on the
version, never on the exit code alone.

R-302 — WHILE the manifest reads the current version, the repo SHALL be
counted `ok` whatever the runner's exit code; a non-zero exit is noted on the
line as evidence (`runner exited N after delivery`), not as a verdict.

R-303 — WHILE the manifest does not read the current version, a zero exit
SHALL stay `WARN still behind` and a non-zero exit SHALL stay `FAIL`, exactly
as today.

## Unchanged

- The sweep's exit contract (BL-053): non-zero iff any repo did not reach the
  current version; exclusions don't fail the sweep.
- Dirty-tree skip, `--only`/`--exclude`/`--dry-run`, both runner profiles.

## Hurting case

**GIVEN** a legislated repo one version behind and a runner that performs the
full delivery and then exits 1 (the session-limit shape),
**WHEN** `fleet.sh upgrade` processes it,
**THEN** the line reads `ok … (runner exited 1 after delivery)`, the sweep
counts it delivered, and the sweep's exit code is 0 — where today it prints
`FAIL` and exits 1 over a completed delivery.

## Verification

A stub-runner harness (no agent, no tokens): a fake `claude` on PATH that
byte-copies the delivery and exits with a chosen code; four scenarios —
delivered+0, delivered+1, undelivered+0, undelivered+1 — asserted against the
printed verdicts and the sweep exit code, before (red) and after the fix.

## Converge — 2026-08-26

- **per R-301 (complete).** Both branches re-read the manifest; the verdict
  is the version. Harness: `delivered-crash` (manifest current, runner exit
  1) reads `ok … (runner exited 1 after delivery — the version is the
  verdict…)` where the unchanged tool printed `FAIL`.
- **per R-302 (complete).** Counted `ok`; the odd exit and the possibly
  truncated report are noted on the line.
- **per R-303 (complete).** `behind-clean` stays WARN, `behind-crash` stays
  FAIL (now naming the version *and* the code).
- **Unchanged verified:** sweep exit contract (2 misses → exit 1 in the same
  harness run), dirty-skip, flags, both profiles untouched.

Verification is `tools/fleet-harness.sh` — a stub runner, no agent, no
tokens; committed red in `a1de5a8` against the unchanged tool. The harness
found one bonus fact on its first run: repos seeded one level too deep were
silently invisible to discovery — BL-055 biting its own test harness.

✅ Converged.
