# BL-055 — `fleet.sh` cannot see member #0, and the channel decision behind it

**Tier: 1 (light).** Tooling + ADR; no `skill/` change, no VERSION, no
benchmark. Branch `bl/055-member-zero-channel`; BL-059's prevention half
rides it (case: `docs/cases/BL-059-tmp-quota-prevention/`).

**Spec type: bugfix** (of an undeclared behavior, resolved by declaring it).

## The question, and the ruling

Discovery (`-maxdepth 4`) cannot see this repository at depth 5. Raising the
depth would decide the permanent delivery channel as a side effect. Owner's
ruling (2026-08-26, clarify session): **release-step** — self-delivery on
the edition branch, byte-verified, before the sweep; the sweep never touches
member #0. Recorded as ADR-0004.

## Requirements

R-401 — `fleet.sh status` SHALL print an explicit member-#0 line, computed
from the tool's own repository (never a hardcoded path), naming its manifest
version and the channel (`release-step, never swept — ADR-0004`).

R-402 — The member-#0 line SHALL NOT participate in the exit contract:
mid-edition skew is branch-normal, and the delivery guarantee lives in the
release runbook's byte-verify step and audit check 3 on the default branch.

R-403 — Discovery SHALL stay at its current depth; the invisibility is
declared, not repaired.

## Hurting case

**GIVEN** this repository legislated at the current skill VERSION,
**WHEN** `fleet.sh status` runs,
**THEN** the output carries a `member #0` line naming v<current> and
ADR-0004, and the exit code is unchanged by it — where today the repository
simply does not appear and the absence reads as an omission.

## Verification

The stub harness (`tools/fleet-harness.sh`) plus a live `status` run: the
line appears, names the version, and a fabricated behind-state changes the
wording but not the exit code.

## Converge — 2026-08-26

- **per R-401 (complete).** `status` prints the member-#0 line, computed from
  `$HERE/..` — live: `member #0 (this repo): v22 — delivered as a release
  step, never swept (ADR-0004)`.
- **per R-402 (complete).** The line is informative only; `status` exit
  stayed 1 (8 fleet repos pending review) with the line present, and the
  behind-wording names branch-normal skew without touching the code path
  that computes the exit.
- **per R-403 (complete).** Discovery untouched; the invisibility is now a
  recorded decision (ADR-0004), not an accident.

✅ Converged.
