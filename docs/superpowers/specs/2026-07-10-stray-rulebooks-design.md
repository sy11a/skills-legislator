# Stray rulebooks + .NET refactoring law — design (BL-016, constitution v9)

**Status:** approved 2026-07-10 (plan-mode approval; law home = concern-named
files, scope = full loop including CareerPlatform validation). Historical
record — do not rewrite.

## Problem

CareerPlatform carries `docs/superpowers/refactoring-checklist.md` — seven
sections of review/refactoring law referenced from nowhere. Legislator is
structurally blind to it: audit check 7 exempts `docs/superpowers/**`,
check 9's foreign-structures list names only known tool configs, and the
harvest scan skips `docs/superpowers/**`. A rulebook parked in an unorthodox
folder is law no session ever loads. Separately, the checklist's *generic*
rules (async hygiene, EF data-access discipline) belong in the constitution,
and the generic `dotnet-refactoring` skill — the pointer target of
`stacks/dotnet/architecture.md`'s how-to line — is contaminated with
CareerPlatform-specific rules (`ManagerLinks`, `CareerPlatform.Domain`).

## Decisions

1. **Generic law → concern-named files (VERSION 8 → 9).**
   `stacks/dotnet/coding-standards.md` gains async/cleanliness bullets
   (fire-and-forget, sync-over-async, sleep-as-sync, CancellationToken,
   dead code); `stacks/dotnet/architecture.md` gains DI bullets
   (constructor-injection-only, no static mutable state); new
   `stacks/dotnet/data-access.md` holds EF/data law (N+1, AsNoTracking,
   unbounded queries, SaveChanges-in-loops, parameterized raw SQL,
   deterministic disposal). Checklist *procedure* (severity tiers, report
   format, build/test gate) stays in the dotnet-refactoring skill — law
   points, skills teach (content discipline).
2. **Audit check 12, slug `stray-rulebooks` (Warning).** Flags an `.md`
   whose content is predominantly law-shaped rule/checklist items (same
   law-shaped test as the harvest scan) that no session loads: scanned
   scope is `docs/**` minus `docs/ai/rules/**`, `docs/adr/**`,
   `docs/journal/**`, `docs/backlog.md`, `docs/superpowers/specs/**`,
   `docs/superpowers/plans/**`, plus root-level `.md` other than CLAUDE.md,
   README.md, CHANGELOG.md. Exempt when CLAUDE.md or any `.md` under
   `.claude/rules/` references it (check 7's referenced definition
   restricted to the session-visible surfaces). Remedy points at
   restructure.
3. **Harvest scans stray rulebooks.** The constitution-candidates scan
   (migration/upgrade/audit) runs the check-12 scan and includes what it
   finds — the one carve-out to the `docs/superpowers/**` exclusion.
   Suppression marker + silent-skip law apply unchanged.
4. **Restructure routing.** `references/restructure.md` §1 gains a stray-
   rulebooks row: law-shaped lines merge into `.claude/rules/<topic>.md`
   (three-way carve per §3), file removed after the merge. Check-12
   findings become plan items through the existing diagnose step; conflicts
   with owned law stay decision-gated.
5. **BL-015 riders ship in this cycle:** harvest test 2 explicitly counts
   an owned-law contradiction as covered (never a candidate) + grader lock;
   check 11 gains an Info note when the manifest is current but
   `.claude/rules/` is absent; restructure.md §1 "AI rules prose" →
   "Narrative AI rules prose"; check 10(b) exempts `.claude/rules/**`
   (auto-loading is the wiring).
6. **Import-replacement under restructure stays untested this cycle** —
   the fixture's stray rulebook carries a generic line and a project line,
   both of which must survive the merge into `.claude/rules/`; §3's
   restatement-replacement path (and its tension with the fidelity pass's
   exemption list) is a known gap, logged as a rider.

## Eval plan

Rotted fixture (both variants) plants defect 12:
`docs/superpowers/review-checklist.md` with a generic law line ("Every
database migration must be reversible…" — harvest candidate) and a project
law line ("Invoice PDFs … PdfRenderer … never call wkhtmltopdf directly" —
must merge to `.claude/rules/`, never be proposed as a candidate). Audit
asserts the check-12 marker + candidate quote + candidates-section-scoped
absence of the project line and of the check-11 contradiction line (rider 1
lock). Restructure asserts the file is merged away and the project law
lands under `.claude/rules/`. Fresh/upgrade auto-track the new owned file
via generated fixtures/`expected_owned()`. Full benchmark → 
`evals/benchmarks/v9.md` vs `v8.md`.

## Out of scope

- Guarding `.claude/rules/**` (project-owned by design, BL-014 decision).
- A `security.md` concern file (AntiForgeryToken/Authorize law) — future
  candidate cycle.
- Restructure import-replacement fidelity mechanics (Decision 6 rider).

## Downstream (phases 2–3, outside this repo)

Genericize `~/.claude/skills/dotnet-refactoring` (drop `CareerPlatform.*`,
`ManagerLinks` → generic centralized-links rule); then run upgrade +
restructure in CareerPlatform: expect check 12 to flag
`refactoring-checklist.md`, the merge to carve project law into
`.claude/rules/refactoring.md`, and v9 law to arrive via the normal owned
copy. User reviews and commits there.
