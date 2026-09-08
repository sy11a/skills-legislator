# L-1 — Research: the mode-reading law, check 21, and its fixture

Tier 1 (light), stage 2 of the feature flow. Establishes the facts the design will rest on;
chooses no variant (that is stage 3). The frozen contract, the case spec and its decision
policy (DP-1…DP-5) are the authority; the Architector checkout was read only.

## 1. History-first sweep (receipt)

Searched: `git log --follow` over the touched area (`skill/assets/rules/core/pair-development.md`,
`skill/assets/templates/AGENTS.md.tpl`, `src/Legislator.Engine/Audit/`,
`evals/setup_workspace.py`); `git grep -i waterflow` across **all** refs; `gh pr list --state all
--search "waterflow"` and `--search "mode"` (PR history).

Found:

- **The concept is settled, the implementation is greenfield.** `waterflow` appears in exactly
  three places, all non-code: `docs/backlog.md` row `## BL-088` (status PROPOSED, hold
  discharged 2026-09-08), Architector `docs/adr/0007-waterflow-mode.md` (status proposed;
  "the ruling and the edition are legislator's"), and Architector `docs/okf/release-cycle.md`
  (the concept the mode serves). Nothing in `skill/`, `src/`, `evals/` or any branch
  implements it. No prior attempt and no rejection: the only waterflow PRs are paperwork
  (PR #40 filed the backlog row, merged 2026-09-06).
- **`pair-development.md` is a 10-line file, last touched by L-2** (f368256, the `/autoflow`
  entry line — the ADR-0009 precedent this case's own ADR question follows). The "one task at
  a time / never merge yourself / no next task without approval" trio dates to v10 (2dbf933).
- **`AGENTS.md.tpl` carries the `Task tracker:` line at its line 13** — the declared home of
  the mode line ("beside the `Task tracker:` line", spec in-scope 2).
- **The audit check set is v26 .NET** (`src/Legislator.Engine/Audit/`, all four commits from
  BL-082); check 20 `arm-integrity` is the newest and the only machine-subject check.
- **BL-088's "What lands here" says the check requires a `waterflow` repo to name "its
  release-branch convention **and its kernel**"** — the frozen contract (A-5.2.2) and the spec
  carry only the release-branch-convention half. Divergence resolved by DP-2 below; recorded
  as a finding against the kernel per the release's own practice.

## 2. Reuse-first census

**The quartet to copy is check 18 `tracker-drift`, in full.** Every mechanism it uses is
directly reusable:

- *Entry-document read.* `EntryDocument.Of(fs, layout, options)` answers which document is the
  entry (canonical `AGENTS.md`, else the real-file alias `CLAUDE.md` for pre-v14 repos — the
  important case for the fixture, whose entry **is** CLAUDE.md, `setup_workspace.py` ≈299).
  Check 18 then reads the entry text with `Read(entry)` and tests a closed marker by
  substring: `Read(...).Contains("Task tracker:", ...)`. Check 21 needs exactly this: the same
  read, one marker for the mode declaration, one for the convention line.
- *Registration is `AuditChecks.Order` and nothing else.* `Place(slug)` reads the `places`
  dictionary built from `Order`; `AuditReport` orders findings by `Place` and prints clean
  checks from `Order`; `HealthChecks` derives from `Order.Take(6)` (so check 21, at position
  21, never enters the Health section); `ModelChecks = {project-rules, stray-rulebooks}` is
  the model boundary — check 21 is a repository-fact check, mechanical. The new slug is
  **21, `waterflow-mode`** (the pinned-name decision is recorded in the spec's clarifications:
  appended to `Order` and spelled in `SKILL.md` § Audit identically).
- *Finding shape.* A finding is `new(Severity.Warning, "<slug>", "<entry>: <fact> → <remedy>")`
  — severity Warning like checks 18/19/20; the text names the offending document verbatim
  (SKILL.md § Audit: "a finding names the offending path, date, or entry verbatim").
- *The fixture triplet.* `evals/setup_workspace.py` `meta` holds `report_markers` (≈501), the
  parity-driven `check_slugs_covered` (≈546), `severity_anchored_markers`, `absent_markers`;
  grade.py enforces the parity — `law_slugs` parsed from SKILL.md's pinned-slug line vs
  `check_slugs_covered`; `ENVIRONMENTAL = {"arm-integrity"}` is **closed**: a repository-fact
  check is not machine-subject, so check 21 must carry a planted defect, a slug-coverage row
  and a report marker (the spec's clarification already rules this).
- *The quiet case.* Check 18's quiet scenario lives at the **twin boundary**, not in a second
  fixture repo: `evals/check_engine.py` asserts `audit_check18_quiet_without_tracker` ("not in
  out") and `tests/Legislator.Parity.Tests/Engine/AuditTwins.cs` carries the `[Parity("engine",
  label)]` twin (≈383–415 region; the second twin asserts condition (b), the third the quiet
  case). The label ledger ratchet (`LabelCoverageTests`) pins the debt at 0 — a new ruler
  label without a twin is red immediately, which **is** the red-first mechanism for the new
  check's labels. The check-20 unit set (`ArmIntegrityCheckTests`) is the boundary-test
  precedent for a check that cannot be fully e2e-planted.
- *Options model.* All name literals live in `src/Legislator.Core/Options/LegislatorOptions.cs`
  (`OptionValue<string>` + key map + composer). The two new key phrases (the mode-declaration
  marker and the convention marker) are new options members — R-8209 forbids literals in the
  check.
- *Twin fixture shape.* `AuditTwins.AuditRepo(...)` builds a minimal legislated repo in a
  `MockFileSystem` with a scripted git; the ruler's `audit_repo` is the Python half. The new
  check's twins add files to that map, exactly like check 18's.

## 3. Constraints census

- **`.claude/rules/dotnet-substrate.md` (ADR-0008).** The check is engine logic in
  `src/Legislator.Engine/Audit/`; no literal outside the options model; no statics in core;
  suite via `sh evals/check_dotnet.sh`, never `dotnet test` (zero-test discovery on this SDK).
- **`.claude/rules/evals.md`.** The change is behavioral (`skill/assets/rules/**` law text +
  `SKILL.md` § Audit both change) ⇒ the full e2e benchmark is **warranted** — materialize a
  workspace, run the scenario agents, grade, idempotency ×3 — and each new assert must be
  shown RED against the unchanged law before green (the new ruler labels are red while the
  check does not exist; green after). Confirms the handoff's open-question default; the
  "212/212" wording and cost are re-confirmed at plan.
- **`.claude/rules/constitution-source.md`.** Editing `skill/assets/rules/core/pair-development.md`
  is a constitution change ⇒ `skill/VERSION` bump in the same commit — **overridden here by
  DP-1**: the edition number is the operator's; this run proposes v27 in the PR and never
  decides. Noted for the PR body.
- **`.claude/rules/records.md` + `check_static.py`.** Tracked files carry no absolute local
  paths and no fleet repo names (the check reads the decoding key outside the repo). This
  document complies; the Architector references below name documents, not local paths.
- **`SKILL.md` § Audit is the one finding namespace.** grade.py parses the pinned slugs from
  it; check_static parses the audit body (checks 15/17 expression). Check 21's slug and
  finding lines must be spelled there, and `AuditChecks.Order` must hold the same slug — both
  arms agree by construction of the parity law.
- **Ownership.** Solo project; everything touched (`skill/`, `src/`, `evals/`, the case home)
  is the operator's; the Architector checkout is read-only (DP-3) and was only read.
- **The fixture entry is pre-v14.** The rotted-layer repo's entry is `CLAUDE.md` (written at
  `setup_workspace.py` ≈299); the mode plant goes into that text. The mode line must not trip
  check 11 (a model check scanning the entry's prose for contradictions with owned law): with
  the law amended to state both modes, a `waterflow` declaration contradicts nothing — verify
  at build.

## 4. Local decisions

Every question the spec left open, with Decision / Rationale / Alternatives. No variant of
wording is chosen — stage 3 decides the lines' exact text.

- **D-1 — the check reads the entry document text through the existing `EntryDocument.Of` +
  `Read` path; no new I/O, no parsed model.** Rationale: check 18 demonstrates the identical
  read; `EntryDocument.Of` already answers "which document is the entry" for both v14
  (AGENTS.md) and pre-v14 (real-file CLAUDE.md) repos — the fixture is the latter. Alternatives:
  a parsed entry-document model (over-engineering for a line scan); reading `AGENTS.md` by
  name (misses pre-v14 entries); the manifest as the declaration home (contradicts the frozen
  contract — the entry document declares the mode).
- **D-2 — the detection frame is closed-token substring matching, mirroring check 18's
  `Task tracker:` match.** The check matches a mode-declaration marker (pair|waterflow) and a
  convention marker in the entry text; the marker strings are the template's own lines and
  live in the options model. Rationale: "what counts is closed, so the engine can execute the
  check" is the convention check 18 and check 14 (skill-name token) already hold; the check
  set's split is mechanical-engine vs model-judgement, and a repository-fact check is
  mechanical. Alternatives: LLM judgement of the declaration (out of the split; would run
  through the model-findings channel, which carries no repo-fact checks); open-ended regex
  (cannot be executed deterministically).
- **D-3 — severity is Warning, and the finding names the entry document verbatim.** Rationale:
  repo-fact drift checks (18/19/20) are all Warning; SKILL.md's finding rule demands the
  offending path/entry named.
- **D-4 — the "and its kernel" half of BL-088 is out of this case.** DP-2: the frozen
  contract beats the task text, and A-5.2.2 names only the release-branch convention; the
  spec's in-scope (3) carries the same narrow reading. The check reports a `waterflow` repo
  that names no release-branch convention, and nothing for one that names one; the kernel
  requirement is recorded as a contract-vs-backlog divergence (finding against the kernel)
  for the operator's release ledger, not implemented here. Alternative: implement both
  halves — exceeds the frozen acceptance, needs a second planted omission no scenario
  asserts; rejected.
- **D-5 — the fixture plant is the rotted-layer entry declaring `waterflow` with no
  convention line; the quiet case (with a convention → silent) is asserted at the twin
  boundary, exactly like check 18's quiet scenario, not in a second fixture repo.**
  Rationale: the parity law (grade.py `parity_every_check_has_a_defect`) demands a planted
  defect per law slug; `ENVIRONMENTAL` is closed for a repo-fact check. One rotted repo
  already carries one defect per check; a second repo doubles benchmark cost for a boundary
  the twins already own. Alternatives: a second fixture variant (heavy, redundant);
  exempting the check (explicitly forbidden by the spec's clarification).
- **D-6 — the test quartet is check_engine.py labels + `[Parity]` twins + the ledger at 0.,
  red-first by the ledger ratchet.** Rotted case → finding; quiet case (convention named) →
  silent; pair/nothing → silent (R-005). The e2e fixture triplet is the fourth piece.
- **D-7 — the options model gains two members** (mode-declaration marker, convention
  marker); naming is stage 3's.
- **ADR candidates (flagged for stage 3, never written here).** The cross-repo decision is
  already recorded by Architector ADR 0007 (proposed; "the ruling and the edition are
  legislator's"). Precedent L-2 wrote ADR-0009 in this repo for a law-line decision (`/autoflow`
  entry); stage 3 decides whether the mode case warrants its own ADR here (the entry-line
  wording is arguably law/instance data, not ADR material; the outliving decision may be the
  check's contract itself, or nothing beyond Architector 0007 — the operators' ledger half
  already exists).

## 5. Unknowns that need an experiment

**None.** Every mechanism check 21 needs has an in-tree precedent exercised in this stage:
the entry read (check 18), the registration (Order/Place), the twin scaffolding
(AuditTwins.AuditRepo), the fixture mechanics (setup_workspace meta + grade parity), the
options home. The genuinely open items — the exact template wording of the mode and
convention lines, the finding text, the options member names — are design variants (stage 3),
not unknowns; no probe would settle them cheaper than the design decision they belong to.
Per the stash conventions (`legislator - Sources` § Flow methods: no `git stash`; Python
spikes live in the case directory), nothing is parked.

## 6. Context package for stage 3 (design)

- Reuse map: the check-18 quartet (entry read → Order registration → fixture triplet → quiet
  twin) is the whole scaffold; nothing new is invented.
- Constraints: engine-side .NET in `src/Legislator.Engine/Audit/`; markers in the options
  model (R-8209); slug pinned 21 `waterflow-mode` in `Order` + SKILL.md § Audit; fixture
  triplet mandatory; quiet case at the twin boundary; severity Warning; full e2e benchmark
  warranted; no VERSION bump on this run's authority (proposed in the PR).
- Open for design variants: the exact mode/convention line wording in `AGENTS.md.tpl` (beside
  the `Task tracker:` line, line 13 → the new line after it), the finding text, the options
  member names, whether an in-repo ADR is written (candidate).
- Finding against the kernel, carried: BL-088's "and its kernel" vs the frozen contract's
  convention-only wording (D-4) — for the release ledger, not for this case's acceptance.