# BL-059 (prevention half) — reclaim the dotnet map files before they kill a run

**Tier: 1 (light).** Evals/tooling only (`tools/evals-bg.sh`); rides
`bl/055-member-zero-channel`. The detection half (the 512 MB write-probe)
shipped 2026-08-25; this closes the case.

**Spec type: bugfix.**

## Current behavior

Every eval run that exercises a dotnet fixture leaves `.XXXXXXXXXXXXXXXX-N.so`
files in `/tmp` — W^X assembly images the .NET host maps and deletes on a
clean exit, and abandons when the process is killed, which is exactly what
the stall ladder does to stuck agents. Measured 2026-08-26: **457 files,
1.88 GB**, every one older than an hour, on a 6124 MB per-user tmpfs quota.
The probe refuses a run that starts with less than 512 MB, but nothing stops
a run that starts healthy from exhausting the quota mid-corpus.

## Requirements

R-501 — WHEN `tools/evals-bg.sh` runs its stage-1 gates, THEN before the
headroom probe it SHALL reclaim every dotnet map file it can **prove**
unowned: matching `/tmp/.[0-9a-f]*-*.so`, owned by the invoking user, and
open in no process (`fuser` silent). Proof, not age: a file some process
still maps is never touched, however old.

R-502 — The reclaim SHALL be reported, never silent: one line naming how
many files and how many MB were reclaimed (zero included), so a reader of
the run log can see the guard executed.

R-503 — WHERE reclaiming fails or `fuser` is unavailable, the run SHALL
proceed to the probe unchanged — the probe stays the authority on whether
there is room; the reclaim only makes room.

## Hurting case

**GIVEN** a `/tmp` carrying unowned map files and one still open in a live
process,
**WHEN** stage 1 runs,
**THEN** the unowned files are gone, the open one survives untouched, and
the log line names the count — where today the garbage accumulates until a
mid-corpus write fails in a shape that reads as a stalled agent.

## Verification

No agent: seed fake map files (plus one held open via a live fd), run the
reclaim function standalone, assert survivors and the log line; then run it
against the real 457-file backlog and watch the probe's headroom grow.

## Converge — 2026-08-26

- **per R-501 (complete).** Stage 1 reclaims `/tmp/.[0-9a-f]*-*.so` files
  that are owned by the invoking user and open in no process. Synthetic
  test: three seeded files, one held open by a live fd — the open one
  survived, the two unowned ones were removed. Real backlog: **457 files,
  1.88 GB → 0**; the user quota went from 3.3 GB used to 1.45 GB.
- **per R-502 (complete).** One log line: `reclaimed N unowned dotnet map
  file(s), M MB` — printed even at zero.
- **per R-503 (complete).** `fuser` absent → an explicit skip line and the
  probe unchanged; a failed `rm` skips the file and keeps counting.

The detection half (the 512 MB probe) stays exactly as shipped — the probe
remains the authority on room; the reclaim only makes room. Case closed:
both halves of BL-059's done-when now hold, and the failure mode can no
longer masquerade as a stalled agent on a run that started healthy.

✅ Converged.
