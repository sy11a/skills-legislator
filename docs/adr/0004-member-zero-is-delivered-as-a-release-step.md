# 0004. Member #0 is delivered as a release step, never swept

## Status

accepted

## Context

This repository is fleet member #0: legislated by the constitution it
produces (ADR-0002). `fleet.sh` discovery is a filesystem scan at
`-maxdepth 4`; this repository sits one level deeper and is invisible to it
(BL-055). The one-character fix — raising the depth — would decide the
delivery channel as a side effect: the sweep would edit the repository that
holds the law's source, possibly mid-edition, on a branch where VERSION is
already bumped but the edition has not shipped. The opposite risk is a
second delivery path, the divergence class BL-054 exists to stop.

Both v21 and v22 were in fact delivered to this repository by hand, on the
edition branch, byte-verified, before the fleet sweep — the release runbook
already treats self-delivery as a release step.

## Decision

Member #0 is delivered **as a release step**, on the edition branch, before
the fleet sweep — the practice v21 and v22 established. The sweep never
touches it; discovery's depth stays as it is, and that invisibility is now
declared rather than accidental: `fleet.sh status` prints an explicit
member-#0 line (computed from the tool's own location, never a hardcoded
path) so a reader cannot mistake the absence for an omission.

The member-#0 line does not participate in `status`'s exit contract. While
an edition is under development on a branch, this repository being behind is
branch-normal (its CLAUDE.md says so); the delivery guarantee is enforced
elsewhere — the release runbook's byte-verify step, and audit check 3
(owned-integrity) on the default branch, where drift is a finding.

There is exactly one delivery mechanism — SKILL.md Steps 2–3, byte-copies
plus the manifest rewrite — invoked by the sweep for members 1..N and by the
release runbook for member #0. The *channel* differs; the *mechanism* does
not, which is what keeps this outside BL-054's divergence class.

## Consequences

- No sweep can edit the law's source repository mid-edition.
- `fleet.sh status` names member #0 explicitly; its state is informative,
  never part of the exit code.
- The release runbook is the delivery guarantee for #0: an edition is not
  shippable until self-delivery is byte-verified (the v22 precedent).
- If the repository is ever relocated to sweep-visible depth, this ADR is
  the record that the invisibility was a decision — supersede it before
  changing the behavior.
