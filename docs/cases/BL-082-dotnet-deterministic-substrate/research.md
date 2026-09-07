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

**Amended 2026-08-30 (stage 2 audit, operator ruling A):** the sentence
"the same ones the constitution's own dotnet stack law asks of the fleet"
overstates the law — `stacks/dotnet/coding-standards.md` asks only for
warnings-as-errors where a project enables it. R-8202's set (nullable,
warnings as errors, code style in build, analyzers at Recommended, AOT
compatibility, all in `Directory.Build.props`) is a bar ABOVE fleet law;
`fleet-obs` is precedent for the props file's existence only (its props
set `TreatWarningsAsErrors` alone; nullable sits per csproj; tests are
xUnit v2 on VSTest with coverlet; YamlDotNet in reflection mode — none of
it AOT-viable). Kept: xUnit v3 on MTP, the strict props, NativeAOT for the
CLI — chosen once, no v2→v3 migration later; the coverage gate of §7 runs
on MTP's code-coverage extension, not coverlet. Follow-up candidate, not
this case: promote the R-8202 bar into the dotnet stack law.

**Measured 2026-09-04 (T-12, plan Step 4) — the startup budget, and what the
port's growth cost.** Twenty runs of `legislator version` on the published
`linux-x64` binary: **median 3.3 ms**, fastest 3.1, slowest 4.1, against
ADR-0005's PreToolUse budget of 50 ms. Fifteen times inside it. This closes the
open question the handoff has carried since T-11 — the artifact grew from
3 445 624 bytes at T-07 to 5 158 216 here, and that growth costs nothing
measurable at start: NativeAOT's cost is image size, which is paid once on disk,
not per invocation, and a hook runs on every edit. The number is a property of
the reference machine, which is why `STARTUP_BUDGET_SKIP=1` excuses a shared CI
runner rather than lowering the bar for everyone.

**Measured 2026-09-04 (T-12) — the four RIDs are not one machine's work.** The
AOT toolchain answers `error : Cross-OS native compilation is not supported`, so
`tools/publish-legislator.sh` publishes the host's RID and refuses the rest by
name before reaching the SDK; `.github/workflows/dotnet.yml` builds all four.
C-12's "in a loop" sentence is amended accordingly (operator ruling 2026-09-04,
option a). A second finding rode along: `RuntimeInformation.RuntimeIdentifier`
answers the machine's SPECIFIC rid under the test host (`fedora.43-x64` here),
which is never a rid an edition releases — the published binary reports the
portable `linux-x64` it was built for, and only a test fixture asking the
runtime about itself gets the other answer.

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

## 8. History sweep and reuse map (receipted 2026-08-30, dev-flow stage 2 audit)

Appended by the audit entry; §1–§7 are the 2026-08-29 record and stand unchanged.

**Searched:** `git log -- docs/ai/engine.py skill/assets/engine plugin/hooks`
(engine lineage); `git log --all --grep 'dotnet|.NET|port|native'` (prior
port attempts); `docs/backlog.md` §§ BL-068–BL-072; `gh pr list --state all`
(PRs #1–#36); `docs/adr/0005`, `0007`, `0008`; the fleet's .NET precedent
`<fleet>/fleet-obs/Directory.Build.props`.

**Found:** the engine is 18 commits old (BL-033 `345edc1` 2026-08-23 →
BL-075 `5166fb0` 2026-08-29), nine jobs in `JOBS` (`docs/ai/engine.py`),
four hooks in `plugin/hooks/*.py` behind `hooks.json`; no prior .NET code
in this repository — the port pre-history is PR #25 (BL-068 audit), #26
(BL-069 register), #27 (BL-070 patch set), ADR-0005 (`b543659`), and PR #35
(this case). No feature was rejected: ADR-0005 chose the end state and
only the phasing is amended (ADR-0008). Rulers: `evals/check_engine.py`
141 checks, `evals/check_hooks.py` 63 checks — the parity contract of §5.

**Reuse map:** (a) the two rulers run unchanged against the binary via an
env-var command override (plan T-05); (b) `evals/POLICY.md`'s red-first
discipline is the R-8206 procedure; (c) the fleet stack law
`skill/assets/rules/stacks/` states the build properties R-8202 repeats —
member #0 obeys it; (d) `fleet-obs`'s `Directory.Build.props` is precedent
for the file's existence only — it sets `TreatWarningsAsErrors` alone,
weaker than R-8202, so it is not a template; (e) `dotnet` 10.0.106 is on
PATH — the reference machine of R-8203/plan; (f) the registry YAML of
BL-077 (R-7701) fixes the config file format (§4).

**Constraints census (change placement):** CLAUDE.md § Architecture
Constraints (one-way law stratum) and § Boundaries; `.claude/rules/
constitution-source.md` (rule edit ⇒ VERSION bump), `dotnet-substrate.md`
(ADR-0008 as project law), `records.md` (no fleet names / absolute paths in
tracked files), `evals.md` (behavioural `skill/` change ⇒ full e2e
benchmark, red-before-green asserts). Ownership: solo; `src/`, `tests/`
are new roots.

**Unknowns needing an experiment:** none open at research time — the 50 ms
start budget (R-8203) is measured at the NativeAOT task, not probed ahead.
Prototype convention on this project: a Python spike inside the case
directory (ADR-0008 §3), never `git stash` (stash is unused here).

## 9. Design supplement (2026-08-30, dev-flow stage 3 audit)

Case-local design facts the ADR does not carry (ADR-0008 holds the decision;
per `core/sdd.md` the local half lives here). Appended by the audit; §1–§8
stand unchanged.

**Variant scoring against the R-lines** (the feature flow's criteria;
no prototypes existed, so no stash pointers):

| Variant | R-lines satisfied | Blast radius | Cost | Verdict |
|---|---|---|---|---|
| A — .NET from v25 step zero, one core / thin hosts, options model, NativeAOT CLI (ADR-0008) | all R-8201–R-8217 | `src/`, `tests/` new; 5 law files + SKILL.md + audit-checks + templates; `plugin/hooks/*`; 6 eval files; 2 tools; README, OKF | port of 9 jobs + 4 hooks before any v25 content | **chosen** |
| B — Python v25, .NET from v26 | R-8201–R-8217 deferred one edition; R-8207's "same edition" unmet at v25 | v25 small; v26 = A's radius plus a rewrite of v25's Python | v25 content written twice | rejected (§1) |
| C — Python spikes in cases, .NET only in editions | kept as the prototype RULE (ADR-0008 §3), not a v25 plan | — | — | rule, not variant |
| Toolchain: file-based apps for the engine | fails R-8203 (JIT start) and the test discipline of R-8206 | — | — | rejected (§2) |
| Shape: one project with folders | R-8204's static check has no project boundary to enforce | smaller tree | cheaper skeleton | rejected (§3) |
| Config: keep a Python fallback where the binary is absent | contradicts R-8215 (BL-069: fail loud, never a second implementation) | two arms | — | rejected (§5) |

**Feature boundaries:** the spec's Out list (BL-083 finishes the literal
migration; BL-084 the MCP host; opencode guard stays TS; eval harness stays
Python; `fleet.sh` untouched beyond BL-077's needs; law content unchanged
beyond command names). Follow-up plan: BL-083 (v26), BL-084 (v27), BL-085
follow-ups on this substrate; candidate: promote the R-8202 bar into the
dotnet stack law (§2 amendment).

**Blast radius stage 7 refactors inside and never beyond:** `src/`,
`tests/`, `evals/check_dotnet.sh`, `tools/publish-legislator.sh`,
`tools/install-legislator.sh`. Everything else the case touches (law text,
SKILL.md, hooks.json, the eval rulers) changes only as the port's parity
switch demands (R-8207), never as refactoring.

**Consumer census — who reads the engine and hooks today** (the sibling
list stage 8's regression reads): law text `core/{sdd,verification,okf,
artifact-lifecycle}.md` and `skill/SKILL.md` (name `python3 docs/ai/
engine.py <job>`); `plugin/hooks/hooks.json` (python shim per hook);
`plugin/hooks/guard_owned_files.py` (owns `docs/ai/engine.py`);
`evals/check_engine.py`, `check_hooks.py`, `check_static.py`,
`mutations.py`, `grade.py`, `setup_workspace.py`, `tools/evals-bg.sh`;
the opencode guard (`plugin/opencode/legislator-guard.ts`): 2 engine/hook
references; templates/references naming engine.py: none; every
legislated fleet member carries a delivered `docs/ai/engine.py` — removed at
its next sweep (release runbook, out of the flows by ruling 2026-08-30).

**Contracts / data model:** no separate file — the plan's per-task
`Interfaces` blocks are the contracts (`plan.md` Tasks 2–6, 11, 12: `IEnvironment`,
`IProcessRunner`, `ProcessResult`, `LegislatorOptions`, `OptionsLayer`,
`OptionValue<T>`, `IJob`/`JobContext`/`JobResult`, `IHook`/`HookPayload`/`HookResult`,
the exit-code contract 0/1/2/3/4, `legislator version --json` shape,
`artifacts/<rid>/` + `SHA256SUMS`). Tasks cite them by task number;
**Superseded the same day:** operator ruling 2026-08-30 (option 2) — the
contracts moved to `contracts.md` (C-01–C-12, each with R-lines, producing
task and consumers); plan tasks cite `C-NN` and no longer restate them.

**Design constraint — AOT-first (operator ruling 2026-08-30, stage 3 audit):**
NativeAOT is the target state of every host, not a property of the CLI's
publish step. Consequences the plan must carry (stage 4 findings):
(1) the AOT proof moves from Task 12 to Task 1 — `evals/check_dotnet.sh`
publishes `linux-x64` from the first commit, so trim/AOT warnings surface in
the task that causes them; (2) every regex in Core/Engine is
`[GeneratedRegex]` (source-generated), never a runtime `new Regex` — the
AOT fallback interpreter is a silent latency hole against R-8203; (3) the
CLI and parity tests exercise the PUBLISHED binary, not `dotnet run`, so what
is measured is what ships. Tests remain JIT (`IsAotCompatible=false` is the
one permitted override). The MCP host (BL-084) inherits the constraint.
