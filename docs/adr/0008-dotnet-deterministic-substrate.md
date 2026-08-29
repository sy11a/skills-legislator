# 0008. The deterministic substrate is .NET from v25; Python is prototype-only

## Status

accepted

*Amends the phasing of 0005; extends 0007 §6.*

## Context

Decided 2026-08-29 with the owner; case `docs/cases/BL-082-dotnet-deterministic-substrate/`.

ADR-0005 chose the end state — one machine-installed .NET binary as the
deterministic arm — and phased it after a Python patch set (BL-070), the
dependency register (BL-069) and the file-model decision (BL-071). All
three gates are closed or void: BL-070 measured the behaviour contract,
BL-069 delivered the register and the absence policy, ADR-0007 voided
BL-071 by construction. What remained was to pick the moment.

ADR-0007 then made edition v25 the largest deterministic build the
system has had — a machine registry, a two-root engine, a registry
predicate in four arms, fleet enumeration with no agent — and named
"default deterministic" as doctrine. Writing that on Python and porting
in v26 writes it twice.

The owner's requirement session (2026-08-29) added three demands: all
determinism, present and future, on .NET, built to product standard
through code-review sessions; everything about environment and
placement configurable, no literals; and an MCP surface so an agent
calls the deterministic core instead of running a script file it can
edit on the fly.

## Decision

1. **The substrate moves now.** BL-072 is absorbed into BL-082 as step
   zero of edition v25: solution skeleton, the engine port as pilot
   (red-first against `check_engine.py`), the hooks port, then BL-077's
   content in .NET. ADR-0005's phasing is amended; its end state, its
   integrity model and its opencode exception stand.
2. **One core, several hosts.** All deterministic behaviour lives in
   `Legislator.Core`/`Legislator.Engine`; the CLI, the Claude Code hooks
   and (v27) the MCP server are hosts without logic of their own. The
   MCP host ships only when the core is covered (≥ 90 % Core+Engine),
   parity with `check_engine.py` is complete and the Python engine is
   gone from law.
3. **Python is a prototype, never a product.** A Python script may be
   written inside a case to try a step; it does not ship in an edition,
   is not named by law, and is ported or deleted before the case
   converges. The eval harness is an instrument, not an arm, and is not
   covered by this rule.
4. **Environment and placement are configuration; law is not.** Every
   path, file name, branch/tag name, threshold, cadence and dependency
   version floor is a member of one options model that holds its only
   default; layers are defaults → machine file → instance file → env,
   schema-validated, loud on unknown keys, with `config show` printing
   provenance. Rule content, templates and `R-NNN` identifiers stay
   versioned text.
5. **Product standard.** Warnings are errors, analyzers on, statics
   (`File`, `DateTime`, `Environment`) injected in the core, tests at the
   boundary, every PR to `src/`/`tests/` reviewed with the owner.
6. **Parity then retirement, per job, same edition.** A Python job is
   removed in the edition its .NET twin reaches parity; the law names
   one command per job, never two.

## Consequences

- Easier: v25's new determinism is written once; the hooks gain a real
  Windows story; the MCP host is one thin project over an already-tested
  core; "which arm ran?" has one answer; a layered config makes the
  instance/sub-group model of v27 a directory of overrides, not a fork.
- Harder: edition v25 grows a port in front of its own content; CI
  gains per-RID NativeAOT publishing and release checksums (ADR-0005's
  phase-2 obligations arrive now); the static check gains two new
  finding classes (literal outside options, static call inside core);
  the repository takes its first compiled dependency — `dotnet` joins
  the register as hard, operator-side and fleet-side alike.
- Amended: ADR-0005's phasing paragraph. Extended: ADR-0007 §6
  (default deterministic now names the substrate). Absorbed: BL-072.
  New cases: BL-082 (v25), BL-083 (v26, configuration complete),
  BL-084 (v27, MCP host).
