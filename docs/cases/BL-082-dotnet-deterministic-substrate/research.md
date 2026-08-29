# BL-082 — Research

Local decisions of the case (the ones that outlive it are ADR-0008).

## 1. Why the substrate moves now, not after v27

**Decision:** BL-072 (ADR-0005 phase 2) is pulled forward to step zero of
edition v25.

**Rationale:** ADR-0005 phased the port after the Python patch set so the
deterministic surface would be measured before it was rebuilt. BL-070
delivered that measurement; BL-069 delivered the register; BL-071 was
voided by ADR-0007. The remaining reason to wait — "know the scope" —
is satisfied. Meanwhile BL-077 is the largest deterministic addition the
system has had (registry, two-root engine, four-arm predicate, fleet
enumeration): building it on Python means writing it twice. The owner's
2026-08-29 requirement makes the choice explicit: all determinism,
present and future, on .NET.

**Alternatives:** (B) Python v25, .NET from v26 — faster to the pivot,
every v25 line rewritten in v26. (C) Python spikes inside cases, only
.NET in editions — this is kept as the *rule* for prototypes (ADR-0008
§Python) but not as the v25 plan.

## 2. Toolchain

**Decision:** .NET 10 (LTS), xUnit v3 on Microsoft.Testing.Platform,
NativeAOT per RID for the CLI, `Directory.Build.props` as the one home
of build discipline, `.editorconfig` alongside.

**Rationale:** the fleet's stack is .NET and its SDK is the one toolchain
every fleet machine already guarantees (ADR-0005). NativeAOT is what
makes a hook viable inside a PreToolUse budget (measured in ADR-0005's
context: ~10–30 ms). xUnit v3 is native MTP — one runner, no VSTest
adapter, `dotnet test` and the platform's exit-code contract straight
from the box. The build properties are the same ones the constitution's
own dotnet stack law asks of the fleet — member #0 obeys its own stack
law for the first time.

**Alternatives:** .NET file-based apps (`dotnet run file.cs`) for the
engine — BL-068's port candidate — rejected: JIT + script host startup
is the latency ADR-0005 already ruled out for hooks, and a solution with
tests is what the review discipline needs. MSTest/NUnit — no advantage
over xUnit v3 here; the dotnet plugin toolchain on this machine is
xUnit-fluent either way.

## 3. Shape: one core, several hosts

**Decision:** `Legislator.Core` (file model, registry, options,
provenance), `Legislator.Engine` (jobs), `Legislator.Hooks` (hooks as
commands), `Legislator.Cli` (entry point). MCP (`Legislator.Mcp`,
BL-084) is a fourth host, added when the gate opens.

**Rationale:** the failure mode the owner named — "editing the Python
file on the fly" — is only closed if the thing an agent calls has no
logic of its own to edit. A host that parses stdin and calls the core is
that. The same shape is what makes the MCP host cheap later: its tools
are the engine's jobs, one method each.

**Alternatives:** one project with folders — simpler, but the host/core
boundary is then a convention, and the static check for R-8204 needs a
project boundary to be enforceable.

## 4. Configuration

**Decision:** an options model in Core as the only default source; four
layers (defaults, machine file, instance file, env), each optional;
schema validation at start, loud on unknown keys; `config show` with
provenance.

**Rationale:** "no constants" read literally would make law
configurable, which contradicts the one-way law stratum. The line the
owner drew (clarification 2): environment and placement are options,
law is text. Provenance is what makes a layered config debuggable —
every effective value names the file it came from; without it a
layered config is a guessing game.

**Alternatives:** `Microsoft.Extensions.Configuration` as the layer
engine — likely the implementation, but the contract (R-8209–R-8213) is
stated independent of it so the tests do not couple to the library.
JSON instead of YAML for the machine/instance files — the registry is
already YAML (BL-077, R-7701); one format.

## 5. Parity and retirement

**Decision:** `check_engine.py` and `check_hooks.py` become parity
rulers: each assertion gets a named .NET twin, shown red first; a job's
Python form is removed in the same edition its parity is reached; law
text switches command names at that moment, not before.

**Rationale:** the eval discipline (`evals/POLICY.md`) already says a new
assert is shown red before green; a port is the same discipline applied
to a whole surface. Removing the Python job in the same edition prevents
two arms coexisting across an edition boundary — the dual-arm state
BL-068 named as a silent killer (which one ran?).

**Alternatives:** keep the Python engine as a fallback where the binary
is absent — rejected by BL-069's policy: verification fails loud, never
falls back to a different implementation.

## 6. What stays Python

The eval harness (`grade.py`, `setup_workspace.py`, `mutate.py`,
`dashboard.py`, `mutations.py`, `proc.py`) and `tools/evals-bg.sh`: an
operator-side instrument that measures the arm, not the arm. The
opencode guard stays TypeScript (ADR-0005). Their future is a separate
question, not part of this pivot; the register (BL-069) already classes
them operator-side.

## 7. Gates for the MCP host (BL-084)

Coverage of `Legislator.Core` + `Legislator.Engine` ≥ 90 % line, measured
by the coverage-analysis job in CI; every `check_engine.py` assertion
twinned; `python3 docs/ai/engine.py` absent from every law file; the
edition it ships in records an MCP-transport eval scenario (an agent
calling `anchors` through MCP and never through Bash).
