# Engine interchangeability — the measured state (BL-054)

**Type:** exploration. **Status:** spike record, measured 2026-08-24 against
`master` at `8d4f04c` (skill v20). Reference artifact — do not rewrite;
the cases it sizes carry the execution decisions.

## Purpose and boundary

The principle was settled by the user on 2026-08-24 and is not in question
here: every part of this resource must work under both engine profiles —
opencode and Claude Code — and the two must be **fully interchangeable**.
Where a tool behaves differently depending on which engine drives it, the
tool's design is wrong.

This spike does not decide that. It answers two questions:

1. Where do we violate it today?
2. At which layer must the guarantee live — per-invocation, or a stated
   contract with a conformance check?

**In scope:** delivery (`tools/fleet.sh`), measurement (`tools/evals-bg.sh`),
enforcement (`plugin/`), and the toolchain around them.

**Out of scope:** how the *law itself* loads into a session. That axis is
owned by BL-044 (the two-harness asymmetry study) and BL-052 (the whole
provider field); both are unrun, and this spike consumes their findings
rather than pre-empting them. Nothing here was implemented.

## Method

Every claim below is backed by a probe, not by reading either vendor's
documentation. Three kinds were used:

- **Code comparison** where the behaviour is deterministic and readable
  (formatter extension sets, guard predicates).
- **Executable probes** where reading could mislead: a throwaway Node script
  drove *both* write-guard arms over the same path set with the same repo
  fixture; a canary token in an entry document plus a four-line
  question measured what a delivery agent actually starts with.
- **Live invocation** for the facts that only a real run can settle
  (`opencode run`'s exit code on an auth failure).

The probes are reproducible from the descriptions in each finding. They cost
three cheap agent calls in total; everything else is free.

**One correction made during the run, recorded because it changes how a
reader should weigh the result.** The first ambient-context probe used a
fixture carrying only `AGENTS.md`. A legislated repo carries `CLAUDE.md` as a
symlink to it, so the first "entry document not loaded" reading was an
artifact of the stand, not a finding. The fixture was reshaped and the probe
re-run; only the second reading is reported.

## What was measured

### Axis 1 — enforcement (`plugin/`)

Two arms ship from one repo: `plugin/hooks/*.py` for Claude Code and
`plugin/opencode/legislator-guard.ts` for opencode.

**Probe.** A fake legislated repo (`docs/ai/manifest.json`, an owned rule,
`opencode.json`, `docs/ai/engine.py`); each arm driven directly over the same
three paths — the Python hook with a `PreToolUse` payload on stdin, the
opencode plugin through its `tool.execute.before` hook.

| path | Claude Code | opencode |
|---|---|---|
| `docs/ai/rules/core/okf.md` | BLOCKED | BLOCKED |
| `opencode.json` | BLOCKED | BLOCKED |
| `docs/ai/engine.py` | BLOCKED | **ALLOWED** |

The formatter arms are at parity: identical extension sets
(`.cs` → dotnet-format; `.ts .tsx .js .jsx .html .css` → prettier), identical
config-name lists, and both honour a `prettier` key in `package.json`. Both
are best-effort and neither blocks.

The OKF-sync reminder diverges — a blocking `Stop` under Claude Code, a
`session.idle` log line under opencode — but that divergence is **declared**
in `plugin/README.md` as an accepted limitation of the port.

### Axis 2 — delivery (`tools/fleet.sh`)

**Probe.** A repo shaped like a legislated one (manifest, `AGENTS.md` holding
a unique token, `CLAUDE.md` symlinked to it), driven with `fleet.sh`'s claude
profile flags verbatim, asking the agent to name its instruction sources.

| property | opencode profile | claude profile |
|---|---|---|
| Agent system prompt | the `service-fleet` agent body | none |
| Operator's global instructions | `~/.config/opencode/AGENTS.md` exists | `~/.claude/CLAUDE.md` — **measured loaded** |
| Repo entry document | — (not probeable, see below) | **measured loaded** |
| `legislator` installed as a skill | `~/.config/opencode/skills/` is empty | `~/.claude/skills/legislator` — **measured invokable** |
| Service marking | `--agent service-fleet` | none reachable |
| Permission posture | machine config (`--auto` exists, unused) | `--permission-mode bypassPermissions` |
| Prompt channel | argv | stdin |
| Write scope | `--dir <repo>` | `cd <repo>` + two `--add-dir` |
| Model vocabulary | `provider/model` | bare name |

**The opencode side of the ambient-context row could not be probed live: the
credential is still invalid.** The same `API key is invalid` that stopped the
2026-08-23 sweep stops it today, which means the fleet is currently
deliverable *only* under the claude profile — the emergency path is the only
path. One useful fact did come out of the attempt: `opencode run` exits `1`
on an auth failure, so BL-053's new FAIL branch fires correctly against the
real failure mode it was written for.

### Axis 3 — measurement (`tools/evals-bg.sh`)

`evals/README.md` states in writing that "pass rates compare *within* a
profile, never across one", and each edition's model floor is recorded
against one engine. Under the principle above that sentence is not a caveat
but the measurement-side statement of the same defect.

`--safe-mode` is what makes the claude profile a fair harness — it disables
the operator's `CLAUDE.md`, auto-memory, hooks, plugins, MCP and installed
skills. **opencode has no equivalent.** `opencode run --help` offers `--pure`
("run without external plugins"), which covers plugins only: the global
`~/.config/opencode/AGENTS.md`, global agents and global commands still load.
So the fair-harness property is currently achievable under one profile and
not the other, by vendor capability rather than by our omission.

One symmetry worth recording, because it constrains any fix: in both engines
the isolation switch also disarms our own enforcement arm — `--safe-mode`
disables hooks, `--pure` disables external plugins. Isolation and enforcement
are coupled the same way on both sides.

### Axis 4 — the rest of the toolchain

`tools/link-skills.sh` installs into `~/.claude/skills` only; the opencode
skills directory is empty. A third tool in the same directory as the two we
just made dual-profile is single-profile by construction.

## Findings, ranked

**F1 — the write-guard arms protect different sets, and the gap is the
engine itself.** `docs/ai/engine.py` is blocked under Claude Code and
allowed under opencode. It became an owned file in v20; the Claude arm was
extended in that cycle and the opencode arm was not. The consequence is worse
than an inconsistency: `core/verification.md`'s rung makes the anchor
engine's findings gate "done", so under opencode an agent may rewrite the
engine that judges it, and the guard whose whole purpose is to keep law
machine-managed does not protect the machine. *Closable here.*

**F2 — nothing compares the two arms, which is why F1 shipped.**
`evals/check_hooks.py` and `evals/check_opencode_plugin.mjs` are independent
hand-written suites; neither derives from a shared declaration of the owned
set. The Python suite asserts the `engine.py` case, the Node suite has no
counterpart, and both were green through a whole edition. F1 is the symptom;
this is the defect. *Closable here, and it is the one that stops recurrence.*

**F3 — a sweep under the claude profile cannot be marked as a service run.**
`fleet-obs` ingests Claude Code sessions but only its opencode miner records
an agent identity into bronze; its silver and gold views select service
sessions with `raw.agent_mode LIKE 'service-%'`, a predicate no Claude Code
session can match, and by its ADR-0039 an unmarked session counts as
practice. Recorded in full in BL-053. *Closable elsewhere — the fix is a
marking path in that project's Claude adapter.*

**F4 — the two delivery profiles do not start their agents from the same
state.** Measured: under the claude profile the agent loads the operator's
personal `~/.claude/CLAUDE.md`, loads the target repo's entry document, and
can invoke an installed `legislator` skill instead of reading the skill files
the prompt points at; under the opencode profile it runs beneath a dedicated
service agent whose body is a different system prompt, and no `legislator`
skill is installed at all. Two agents, two starting states, one prompt.
*Closable here.*

**F5 — the fair-harness property is not achievable under opencode today.**
No flag suppresses global instructions, agents or commands. *Declarable only,
until the vendor offers one; the declaration must be explicit rather than
implied by a README sentence about comparability.*

**F6 — permission posture differs, and asymmetrically in the dangerous
direction.** The claude profile cannot stop on a permission prompt; the
opencode one relies on machine configuration and `--auto` is available and
unused. In a headless sweep with nobody to answer, a prompt is a stall.
*Closable here.*

**F7 — model identity has no shared vocabulary.** `provider/model` strings
versus bare names, with no mapping. "The same model floor under both
profiles" is not merely unmeasured, it is currently **unexpressible** — which
is the real reason the eval suite declares pass rates non-comparable.
*Closable here for the vocabulary; whether floors can be equated at all is
a question for BL-048.*

**F8 — `tools/link-skills.sh` is single-profile by construction.** Claude
Code only. *Closable here.*

**F9 — the OKF-sync reminder blocks under one engine and logs under the
other.** Legitimate given the two event models, and already declared in
`plugin/README.md`. *No action beyond moving the declaration into the
contract, so that it is an exemption on a list rather than a paragraph of
prose.*

**Legitimate adapter differences — measured and explicitly not divergences:**
the stall oracle (`run.log` vs `run.jsonl`), the process-kill pattern, the
resume flag (`--continue` vs `-c`), and the prompt channel (argv vs stdin).
Each is a different mechanism reaching the same observable outcome. This
distinction is the one the whole case turns on: a contract that forbids these
would be unimplementable, and a contract that ignores F1 would be pointless.

## The recommendation

**The guarantee cannot live per-invocation.** Every divergence above entered
the same way — a profile was added or extended in one file, by one case,
with nothing stating what a profile owes. F1 is decisive evidence: the arms
diverged inside a single edition that touched both concerns, was reviewed,
and shipped green. Fixing the eight findings one by one leaves the ninth to
enter on the next edition.

**Recommended shape: a stated engine contract plus a mechanical conformance
check.** A short document naming the properties every profile must deliver,
one adapter per engine that satisfies it, and a check in the commit gate that
fails when a profile cannot — with a named exemption list for the properties
that are declarable-only.

Draft property list, derived from the findings rather than invented:

| # | property | closes |
|---|---|---|
| P1 | the owned-path enforcement set is identical, derived from one declaration | F1, F2 |
| P2 | isolation posture is declared per profile, with the unavailable ones named | F4, F5 |
| P3 | the agent receives exactly the composed prompt (channel is free) | keeps argv/stdin legitimate |
| P4 | the set of writable roots is the same | — |
| P5 | no interactive prompt is reachable in a headless run | F6 |
| P6 | service marking is declared per profile, with the unreachable ones named | F3 |
| P7 | model identity has one vocabulary, or a stated mapping | F7 |

This is BL-045's "one declaration, two projections" applied to invocation
instead of imports, and the two cases should share a mechanism rather than
grow two.

**Cost, honestly rough.** The contract document and the conformance check are
small — the check is mechanical, needs no agent, and belongs beside
`check_static.py` in the every-commit gate. The expensive half is deriving
both guard suites from one owned-path declaration (F2), because it means
touching two languages and two test harnesses; that is the item worth its own
case. The rest are single-file edits once the contract says what they must
satisfy.

## Classification

| finding | closable here | closable elsewhere | declarable only |
|---|---|---|---|
| F1 guard set | ✅ | | |
| F2 suites not derived | ✅ | | |
| F3 service marking | | ✅ `fleet-obs` | |
| F4 delivery start state | ✅ | | |
| F5 opencode fair harness | | | ✅ |
| F6 permission posture | ✅ | | |
| F7 model vocabulary | ✅ | | |
| F8 skill linking | ✅ | | |
| F9 OKF reminder | ✅ (as an exemption) | | |

## What this spike did not settle

- **Whether the law loads identically.** BL-044 and BL-052 own that; both
  unrun.
- **The opencode side of the ambient-context probe**, because the credential
  is invalid. The row above is inference from configuration, not measurement,
  and is marked as such.
- **Whether pass rates can ever be compared across profiles.** F7 removes one
  obstacle (vocabulary); whether the floors themselves are comparable is
  BL-048's question.
- **What the `service-fleet` agent body actually changes in a live run** —
  unmeasurable while the credential is dead.

## Cases this sizes

Recommended, not filed — each needs the user's decision on order:

1. **The engine contract and its conformance check** (P1–P7 above, plus the
   exemption list). Behavioural if it touches `assets/rules/**`; probably not.
2. **Derive both guard suites from one owned-path declaration** — closes F1
   permanently rather than by patching the Node suite once.
3. **`fleet.sh` profile parity** — F4, F6, and the skill-shadowing hazard.
4. **`link-skills.sh` dual target** — F8.
5. **A case in `fleet-obs`** — F3, that project's own backlog.
