# BL-056 — `fleet.sh status` reports uncommitted work as delivered

**Tier: 1 (light).** Tooling only; rides `bl/061-fleet-false-fail` (one MR).

**Spec type: bugfix.**

## Current behavior

`status` reads `docs/ai/manifest.json` from the working tree. On 2026-08-24
it reported three repositories at v20 whose committed HEAD said v16/v16/v14 —
an unreviewed sweep diff counted as delivery. Second-order: those dirty repos
are silently skipped by the next sweep, so the tool parks them in
"upgraded but nowhere" indefinitely.

## Requirements

R-311 — WHEN `status` reports a repository, THEN the version column SHALL be
the **committed** one (`HEAD:docs/ai/manifest.json`); WHERE the path is not a
git repository or has no commit, the working-tree value SHALL be used and the
state SHALL say so (`no-git`).

R-312 — WHILE the working-tree manifest version differs from the committed
one, the state SHALL read `pending review (worktree vX, uncommitted)` and the
repository SHALL count as behind — visible, never `ok`.

R-313 — The exit contract SHALL stay: 0 iff every repository's **committed**
version is current.

## Unchanged

- Discovery (scan roots, depth — BL-055's territory, untouched here).
- The table's shape; `upgrade`'s behavior.

## Hurting case

**GIVEN** a repo whose HEAD manifest says v21 and whose working tree carries
an uncommitted v22 upgrade,
**WHEN** `fleet.sh status` runs,
**THEN** the row reads `v21  pending review (worktree v22, uncommitted)` and
the exit code is 1 — where today it reads `v22  ok` and can exit 0.

## Verification

Same stub harness: fake repos with committed vs worktree manifest divergence;
asserted before (red) and after.
