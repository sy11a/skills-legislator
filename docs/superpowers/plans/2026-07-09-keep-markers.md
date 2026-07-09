# Keep-Markers (BL-002 + BL-011) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Manifest gains a pinned-serialization `keep` list of protected project artifacts; audit check 10 becomes real enforcement; the "referenced" definition is fixed so hub files stop flagging as orphans (BL-011).

**Architecture:** All behavior lives in `skill/SKILL.md` prose (the skill is a procedure, not code) plus one line in `skill/references/migration.md`; the eval layer (`evals/setup_workspace.py`, `evals/grade.py`, fixtures, `evals/evals.json`, `evals/README.md`) grows fixtures and assertions that lock the behavior in. Spec: `docs/superpowers/specs/2026-07-09-keep-markers-design.md` — binding.

**Tech Stack:** Markdown skill prose; Python 3 stdlib eval scripts; git fixtures.

## Global Constraints

- **NEVER add any `Co-Authored-By` / AI-attribution trailer to any commit.** Absolute repo rule.
- `skill/VERSION` stays `7` — no `assets/rules/**` file changes in this plan. If you find yourself editing anything under `skill/assets/rules/`, STOP: out of scope.
- Every commit: `python3 evals/check_static.py` must pass first.
- Behavioral `skill/` changes are not "done" until Task 5's full e2e benchmark passes and is recorded as `evals/benchmarks/v7.2.md`.
- Do not edit anything under `docs/superpowers/specs/` or `plans/` other than checking checkboxes in this plan.
- Keep-entry serialization (user-settled, Gate 0): array of single-line inline objects `{"path": ..., "reason": ...}`, sorted by `path`; `[]` inline when empty; key order `legislatorVersion`, `profiles`, `keep`, `ownedFiles`.
- The main session runs concurrent background work in a separate worktree touching `plugin/` and `evals/check_hooks.py` — do not create or modify those paths.

---

### Task 1: SKILL.md — keep-list handling, manifest serialization, Step 7 reporting

**Files:**
- Modify: `skill/SKILL.md` (Step 3 items 6→6+7, JSON example, Step 7)

**Interfaces:**
- Produces: manifest schema with `keep` between `profiles` and `ownedFiles` — Tasks 3 and 4 encode exactly this serialization in fixtures and grader regexes. The manifest-write item becomes **Step 3.7** (Task 4 updates grade.py's "per Step 3.6" evidence string to match).

- [ ] **Step 1: Replace Step 3 item 6 and the JSON block**

In `skill/SKILL.md`, replace item 6 of Step 3 (the line beginning `6. Write \`docs/ai/manifest.json\` with this exact serialization`) and the fenced JSON block that follows it with:

````markdown
6. **Keep list — protected project artifacts.** Carry forward the existing manifest's `keep` list (default to `[]` when the key is absent — older manifests predate it). Then apply any keep requests in the user's prompt: **adding** requires the path to exist in the repo (if it doesn't, skip it and report the refusal in Step 7) and to not be an owned file under `docs/ai/rules/` (owned files are machine-managed, not keepable); entries dedupe by path — re-marking an already-kept path replaces its reason. **Remove** an entry only when the user explicitly asks. Never add or remove keep entries on your own initiative. Kept files are project-owned content — the keep list is manifest metadata about them; it changes nothing about how this skill treats the file (project-owned files are never touched anyway). Its meaning is downstream: audit check 10 verifies kept files stay wired into the layer, and any future restructuring must leave kept paths in place.
7. Write `docs/ai/manifest.json` with this exact serialization — 2-space indentation, keys in this order (`legislatorVersion`, `profiles`, `keep`, `ownedFiles`), `profiles` as a single-line inline array (e.g. `["dotnet"]` or `["dotnet", "aurelia"]` — never expanded across multiple lines, regardless of how many entries it has), `keep` written as `[]` on the same line as its key when empty and otherwise one entry per line sorted by `path`, each entry a single-line inline object with keys in the order `path`, `reason`, and `ownedFiles` sorted with each entry on its own line as shown below — so that two runs with no actual change produce a byte-identical file and no spurious diff:

```json
{
  "legislatorVersion": <int from VERSION>,
  "profiles": [<confirmed profile names>],
  "keep": [
    {"path": "<repo-relative path>", "reason": "<why this artifact must stay as-is>"}
  ],
  "ownedFiles": [<new ownedFiles list, sorted>]
}
```

With no kept files that line is exactly `  "keep": [],` — never an expanded empty array.
````

- [ ] **Step 2: Add the Keep-list section to Step 7 and fix Health's ordinal**

In Step 7's first paragraph (line starting `Print a summary with these sections:`), after the closing parenthesis of the **Needs your review** description (`...the glossary pointer line).`), append this sentence to the same paragraph:

```markdown
 Additionally, only when this run changed the `keep` list, include a **Keep list** section: each entry added or removed (path + reason) and any refused keep request (the path did not exist).
```

In the next paragraph, change `append a fifth section, \`### Health\`` to `append a final section, \`### Health\`` (the section count is no longer fixed).

- [ ] **Step 3: Verify no stale internal references and run static checks**

```bash
grep -n "Step 3\.[0-9]" skill/SKILL.md skill/references/*.md evals/README.md README.md
python3 evals/check_static.py
```

Expected: no reference to "Step 3.6" meaning the manifest write remains in `skill/` or `README.md`/`evals/README.md` prose (grade.py's evidence string is Task 4's job — leave it). Static checks: all pass.

- [ ] **Step 4: Commit**

```bash
git add skill/SKILL.md
git commit -m "SKILL.md Step 3: keep list (carry-forward, user-driven add/remove) + pinned manifest serialization with keep; Step 7 Keep-list section"
```

---

### Task 2: SKILL.md audit section — check 7/10 rewrite, pinned slugs, Info slot; migration.md quote fix

**Files:**
- Modify: `skill/SKILL.md` (Audit section: preamble, checks 7 and 10)
- Modify: `skill/references/migration.md` (§1 final paragraph, glossary pointer quote)

**Interfaces:**
- Produces: pinned finding slugs (`orphan-docs`, `keep-list`, …) — Task 3's `absent_markers` and marker strings grep for them; the shared "referenced" definition both checks cite.

- [ ] **Step 1: Pin the check-name slugs**

In the Audit section, after the sentence `Perform these checks (severity in parentheses; a finding names the offending path, date, or entry verbatim):`, insert this paragraph before the numbered list:

```markdown
In findings, `[check-name]` is the check's pinned slug — use exactly these: 1 `imports-resolve`, 2 `unresolved-placeholders`, 3 `owned-integrity`, 4 `staleness`, 5 `okf-index-links`, 6 `codebase-map`, 7 `orphan-docs`, 8 `journal-recency`, 9 `foreign-structures`, 10 `keep-list`.
```

- [ ] **Step 2: Rewrite check 7 with the shared "referenced" definition**

Replace the current check 7 line:

```markdown
7. **Orphan docs (Warning):** an `.md` under `docs/okf/` or directly in `docs/` that no other `.md` file (or CLAUDE.md) references by markdown link or `@import`. Exempt by directory convention: `docs/ai/rules/**`, `docs/adr/**`, `docs/journal/**`, `docs/superpowers/**`, and `docs/backlog.md`.
```

with:

```markdown
7. **Orphan docs (Warning):** an `.md` under `docs/okf/` or directly in `docs/` that no other `.md` file (or CLAUDE.md) **references**. A file counts as referenced when any other markdown file mentions it by markdown link, by `@import`, or by inline-code path mention — its repo-relative path (or a relative path resolving to it) inside backticks; rule text naming `docs/okf/index.md` in backticks wires that file in. Exempt by directory convention: `docs/ai/rules/**`, `docs/adr/**`, `docs/journal/**`, `docs/superpowers/**`, and `docs/backlog.md`.
```

- [ ] **Step 3: Rewrite check 10**

Replace the current check 10 line:

```markdown
10. **Keep-list integrity (Warning):** if the manifest has a `keep` key, every kept path must exist and be referenced from somewhere; if the key is absent, report `keep-list: not present (pre-BL-002 manifest)` and skip.
```

with:

```markdown
10. **Keep-list integrity (Warning):** for every entry in the manifest's `keep` list: (a) the kept path exists on disk — otherwise finding `<path>: kept path missing from disk → restore it or remove the keep entry`; (b) the kept file is referenced from somewhere — same definition as check 7, searched across CLAUDE.md and every `.md` in the repo, with no directory exemptions — otherwise finding `<path>: kept but referenced from nowhere → link it from docs/okf/index.md or CLAUDE.md`. If the manifest has no `keep` key at all, add to the **Info** section exactly `- [keep-list] docs/ai/manifest.json: no keep key (pre-keep-schema manifest) → re-run /legislator to refresh` and skip the check.
```

- [ ] **Step 4: Align migration.md's glossary pointer quote (BL-011 item 3)**

In `skill/references/migration.md` §1's final paragraph, replace:

```
and the glossary pointer line (`Domain glossary: docs/okf/glossary.md — check it when a term is unclear; add terms as they emerge`)
```

with (note the double-backtick quoting so the bullet's own backticks survive):

```
and the glossary pointer bullet exactly as `CLAUDE.md.tpl` carries it: ``- Domain glossary: `docs/okf/glossary.md` — check it when a term is unclear; add terms as they emerge``
```

Verify the quoted text matches the template byte-for-byte:

```bash
grep -F 'Domain glossary' skill/assets/templates/CLAUDE.md.tpl skill/references/migration.md
```

Expected: the bullet text after the tpl's `- ` prefix is identical in both files.

- [ ] **Step 5: Static checks and commit**

```bash
python3 evals/check_static.py
git add skill/SKILL.md skill/references/migration.md
git commit -m "Audit: pinned finding slugs, inline-code mentions count as references (checks 7+10), keep-list check enforced; migration.md glossary quote aligned to tpl"
```

---

### Task 3: Fixtures — upgrade keep entries, rotted 10th defect, prompts

**Files:**
- Create: `evals/fixtures/upgrade-base/docs/notes/deploy-runbook.md`
- Create: `evals/fixtures/upgrade-base/docs/notes/perf-tuning.md`
- Modify: `evals/fixtures/upgrade-base/docs/okf/index.md`
- Modify: `evals/setup_workspace.py` (`materialize_upgrade`, `materialize_rotted`)
- Modify: `evals/evals.json` (upgrade + audit entries)

**Interfaces:**
- Consumes: pinned slugs from Task 2 (`orphan-docs`, `keep-list` appear in marker strings).
- Produces: `fixture_meta.json` keys Task 4's grader reads — upgrade meta gains `"expected_keep"` (list of `{path, reason}`), rotted meta gains a 10th `report_markers` entry and `"absent_markers"` (list of strings).

- [ ] **Step 1: Create the two kept files and link them from the OKF index**

`evals/fixtures/upgrade-base/docs/notes/deploy-runbook.md`:

```markdown
# Deploy runbook

Battle-tested deployment steps for BillingApi. Ordering matters; do not
restructure this document.

1. Drain the webhook queue.
2. `dotnet publish -c Release` and swap the app-service slot.
3. Re-enable the queue and watch the dead-letter count for 10 minutes.
```

`evals/fixtures/upgrade-base/docs/notes/perf-tuning.md`:

```markdown
# GC tuning notes

Hand-tuned server GC settings that took a quarter to get right. Values are
load-tested against the 2025 Black Friday traffic capture.

- `ServerGarbageCollection: true`, `ConcurrentGarbageCollection: false`
- Gen0 budget pinned via `GCgen0size` — see commit history before changing.
```

Replace the full contents of `evals/fixtures/upgrade-base/docs/okf/index.md` with:

```markdown
# OKF Index

- [Log](log.md)
- [Deploy runbook](../notes/deploy-runbook.md)
- [GC tuning notes](../notes/perf-tuning.md)
```

- [ ] **Step 2: Fixture manifest carries one keep entry; record expected_keep in meta**

In `evals/setup_workspace.py` `materialize_upgrade`, replace the `manifest = (...)` expression with:

```python
    manifest = (
        "{\n"
        f'  "legislatorVersion": {version - 1},\n'
        '  "profiles": ["dotnet"],\n'
        '  "keep": [\n'
        '    {"path": "docs/notes/deploy-runbook.md", "reason": "battle-tested deploy runbook"}\n'
        "  ],\n"
        '  "ownedFiles": [\n'
        + ",\n".join(f'    "{p}"' for p in owned_sorted)
        + "\n  ]\n}\n"
    )
```

and replace the `meta = {...}` dict with:

```python
    meta = {
        "withheld_core_rule": withheld.name,
        "retired_rule": RETIRED_RULE,
        "fixture_manifest_version": version - 1,
        # Entry 0 pre-exists in the fixture manifest (carry-forward test);
        # entry 1 is added by the eval prompt (add-path test). Sorted by path.
        "expected_keep": [
            {"path": "docs/notes/deploy-runbook.md", "reason": "battle-tested deploy runbook"},
            {"path": "docs/notes/perf-tuning.md", "reason": "hand-tuned GC settings notes"},
        ],
    }
```

Also update the module docstring's upgrade paragraph: after "Its manifest records VERSION-1." append "Its manifest also carries one `keep` entry; the eval prompt adds a second — together they exercise keep carry-forward, prompt-driven adds, and pinned keep serialization."

- [ ] **Step 3: Rotted fixture — 10th defect (unlinked kept file) + absent markers**

In `materialize_rotted`:

(a) After the Defect 7 block (`orphan-notes.md`), add:

```python
    # Defect 10 — keep-listed file that nothing references (protected but
    # orphaned). Lives under docs/notes/ so orphan check 7 (which scans only
    # docs/okf/ and docs/ top level) cannot double-report it — the finding
    # must come from keep-list check 10.
    (dest / "docs/notes").mkdir(parents=True)
    (dest / "docs/notes/special-sauce.md").write_text(
        "# Special sauce\n\nUltra-specific invoice rounding rules that work "
        "in production. Keep as-is.\n")
```

(b) In the manifest string, insert the keep block between the `profiles` line and the `ownedFiles` line:

```python
        '  "keep": [\n'
        '    {"path": "docs/notes/special-sauce.md", "reason": "works as-is"}\n'
        "  ],\n"
```

(c) In `meta`, append to `report_markers`:

```python
            "special-sauce.md",       # defect 10: kept but referenced nowhere
```

and add after `report_markers`:

```python
        # BL-011 regression lock: the audit must NOT flag the constitution's
        # hub files as orphans (they are referenced by inline-code mention).
        "absent_markers": [
            "orphan-docs] docs/okf/index.md",
            "orphan-docs] docs/okf/glossary.md",
        ],
```

(d) Update `materialize_rotted`'s docstring first line from "nine planted defects" to "ten planted defects", and the module docstring's `rotted-layer` line the same way.

- [ ] **Step 4: evals.json prompt/expected updates**

In `evals/evals.json`:

- Upgrade entry (`id: 2`): append to `prompt`: ` Also mark docs/notes/perf-tuning.md as protected: add it to the manifest keep list with reason exactly "hand-tuned GC settings notes".` Append to `expected_output`: ` The manifest's existing keep entry (deploy-runbook) survives byte-identically and the new perf-tuning entry is added in sorted position, pinned single-line serialization.`
- Audit entry (`id: 4`): in `expected_output`, change "all nine planted defects" to "all ten planted defects" and extend the parenthetical list with `, keep-listed special-sauce.md referenced nowhere`; append the sentence ` The report must not flag docs/okf/index.md or docs/okf/glossary.md as orphans.`
- Idempotency entry (`id: 3`): append to `expected_output`: ` Run on both the fresh-scaffold repo and the upgrade repo — the upgrade repo's populated keep list must re-serialize byte-identically.`

- [ ] **Step 5: Materialize and inspect a workspace**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/kmtest-ws
python3 - <<'EOF'
import json
from pathlib import Path
ws = Path("/tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/kmtest-ws")
up = json.loads((ws / "upgrade/fixture_meta.json").read_text())
assert [e["path"] for e in up["expected_keep"]] == sorted(e["path"] for e in up["expected_keep"])
m = json.loads((ws / "upgrade/repo/docs/ai/manifest.json").read_text())
assert m["keep"] == [up["expected_keep"][0]], m["keep"]
rot = json.loads((ws / "rotted-layer/fixture_meta.json").read_text())
assert len(rot["report_markers"]) == 10 and "special-sauce.md" in rot["report_markers"]
assert len(rot["absent_markers"]) == 2
rm = json.loads((ws / "rotted-layer/repo/docs/ai/manifest.json").read_text())
assert rm["keep"][0]["path"] == "docs/notes/special-sauce.md"
assert (ws / "upgrade/repo/docs/notes/perf-tuning.md").exists()
print("fixture assertions OK")
EOF
```

Expected: `fixture assertions OK`.

- [ ] **Step 6: Commit**

```bash
git add evals/fixtures/upgrade-base evals/setup_workspace.py evals/evals.json
git commit -m "Fixtures: keep entries in upgrade fixture (carry-forward + prompt-add), rotted 10th defect (unlinked kept file), absent-marker regression lock"
```

---

### Task 4: grade.py — keep assertions, absent markers, docs alignment

**Files:**
- Modify: `evals/grade.py`
- Modify: `evals/README.md`

**Interfaces:**
- Consumes: `expected_keep` / `absent_markers` from Task 3's meta; the pinned serialization from Task 1.
- Produces: `Grader.common_checks(repo, expected_keep=[])` — `grade_upgrade` passes `meta["expected_keep"]`; other scenarios rely on the `[]` default.

- [ ] **Step 1: Extend common_checks with keep assertions**

In `evals/grade.py`, change the signature `def common_checks(self, repo: Path) -> None:` to `def common_checks(self, repo: Path, expected_keep: list | None = None) -> None:` and insert after the `manifest_profiles_single_line_inline` check:

```python
        expected_keep = expected_keep or []
        keep = manifest.get("keep") if manifest else None
        self.check("manifest_keep_matches_expected", keep == expected_keep,
                   f"expected {expected_keep}, got {keep}")

        idx = [raw.find(f'"{k}"') for k in
               ("legislatorVersion", "profiles", "keep", "ownedFiles")]
        order_ok = all(i >= 0 for i in idx) and idx == sorted(idx)
        self.check("manifest_key_order", order_ok,
                   "legislatorVersion, profiles, keep, ownedFiles" if order_ok
                   else "keys missing or out of order")

        if isinstance(keep, list) and keep:
            block = re.search(
                r'^  "keep": \[\n((?:    \{"path": "[^"]*", "reason": "[^"]*"\},?\n)+)  \],$',
                raw, re.M)
            pinned = bool(block) and [e["path"] for e in keep] == sorted(e["path"] for e in keep)
            evidence = ("one entry per line, single-line objects, sorted by path" if pinned
                        else "keep block not in pinned form (expanded objects, unsorted, or wrong indent)")
        else:
            pinned = bool(re.search(r'^  "keep": \[\],$', raw, re.M))
            evidence = ('empty keep inline as \'"keep": [],\'' if pinned
                        else "empty keep not serialized inline on one line")
        self.check("manifest_keep_pinned_serialization", pinned, evidence)
```

Update the `manifest_profiles_single_line_inline` evidence string `"profiles array on one line per Step 3.6"` to `"profiles array on one line per Step 3.7"`.

- [ ] **Step 2: Wire expected_keep into grade_upgrade; absent markers into grade_audit**

In `grade_upgrade`, change `g.common_checks(repo)` to:

```python
    g.common_checks(repo, expected_keep=meta.get("expected_keep", []))
```

In `grade_audit`, after the `report_markers` loop, add:

```python
    for marker in meta.get("absent_markers", []):
        g.check(f"report does NOT contain {marker!r}", marker not in report,
                "correctly absent" if marker not in report else "false-positive finding present")
```

- [ ] **Step 3: Self-test the serialization regex both ways**

```bash
python3 - <<'EOF'
import re
PIN = r'^  "keep": \[\n((?:    \{"path": "[^"]*", "reason": "[^"]*"\},?\n)+)  \],$'
good = '{\n  "legislatorVersion": 7,\n  "profiles": ["dotnet"],\n  "keep": [\n    {"path": "a.md", "reason": "x"},\n    {"path": "b.md", "reason": "y"}\n  ],\n  "ownedFiles": []\n}\n'
bad = '{\n  "legislatorVersion": 7,\n  "profiles": ["dotnet"],\n  "keep": [\n    {\n      "path": "a.md",\n      "reason": "x"\n    }\n  ],\n  "ownedFiles": []\n}\n'
empty_good = '  "keep": [],\n'
assert re.search(PIN, good, re.M), "pinned form must match"
assert not re.search(PIN, bad, re.M), "expanded form must NOT match"
assert re.search(r'^  "keep": \[\],$', empty_good, re.M)
print("regex self-test OK")
EOF
```

Expected: `regex self-test OK`.

- [ ] **Step 4: Dry-run the grader against the un-run workspace from Task 3**

```bash
python3 evals/grade.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/kmtest-ws 2>&1 | tail -40
```

Expected: exit 1 (fixtures haven't been run by an agent — that's correct). Verify per scenario: the new `manifest_keep_matches_expected`, `manifest_key_order`, `manifest_keep_pinned_serialization` checks appear and FAIL with readable evidence for fresh/migration (no manifest yet); the **upgrade** scenario's keep check fails showing `expected [both entries], got [deploy-runbook only]` (fixture pre-state — proves the assertion will bite); no Python tracebacks anywhere.

- [ ] **Step 5: Update evals docs and grade.py docstring**

- `evals/grade.py` docstring: in the scenario list, change the idempotency line to `idempotency:<scenario>   ... (benchmarks run it for fresh-scaffold-dotnet and upgrade)`.
- `evals/README.md`: in the scenario descriptions, extend **upgrade** with "keep-list carry-forward + prompt-driven add, pinned keep serialization"; extend **audit** with "ten planted defects, including an unlinked keep-listed file; hub files must not be flagged (BL-011 regression lock)"; extend **idempotency** with "run against both fresh-scaffold-dotnet and upgrade (the latter proves a populated keep list re-serializes byte-identically)"; in the run-procedure section add the command line `python3 evals/grade.py <ws> idempotency:upgrade` next to the existing idempotency command.

- [ ] **Step 6: Static checks and commit**

```bash
python3 evals/check_static.py
git add evals/grade.py evals/README.md
git commit -m "Grader: keep-list assertions (expected entries, key order, pinned serialization), audit absent-markers; docs"
```

---

### Task 5: Full e2e benchmark v7.2 + record + backlog

**Files:**
- Create: `evals/benchmarks/v7.2.md`
- Modify: `docs/backlog.md` (BL-002 → done, BL-011 → done, queue Track A note)

**Interfaces:**
- Consumes: everything above. This task is run by the main session (it dispatches eval agents), not a subagent implementer.

- [ ] **Step 1: Materialize a fresh benchmark workspace**

```bash
python3 evals/setup_workspace.py /tmp/legislator-eval-v7.2
```

- [ ] **Step 2: Run the four scenario agents** per `evals/README.md`: one fresh agent per scenario (`fresh-scaffold-dotnet`, `legacy-migration`, `upgrade`, `audit`) with the exact prompt from `evals/evals.json`, cwd = that scenario's `repo/` (audit agent additionally told to save its report to `/tmp/legislator-eval-v7.2/rotted-layer/outputs/audit-report.md`). Record tokens + wall time per scenario.

- [ ] **Step 3: Grade**

```bash
python3 evals/grade.py /tmp/legislator-eval-v7.2
```

Expected: exit 0, all scenarios full marks (fresh 13, migration 16, upgrade 13, audit 14 — counts grew by the three new manifest checks per common-checks scenario, +1 marker and +2 absent-markers on audit).

- [ ] **Step 4: Idempotency passes — fresh AND upgrade**

Commit run-1 results inside both eval repos, dispatch a fresh agent per repo with the idempotency prompt, then:

```bash
python3 evals/grade.py /tmp/legislator-eval-v7.2 idempotency:fresh-scaffold-dotnet idempotency:upgrade
```

Expected: both `second_run_zero_diff` PASS — the upgrade one proves populated-keep byte-stability.

- [ ] **Step 5: Record `evals/benchmarks/v7.2.md`** in the same table format as `v7.1.md`, compared against v7.1's numbers (pass counts, tokens, wall time per scenario), with notes on: what changed (BL-002 + BL-011), the new assertions, the new permanent `idempotency:upgrade` scenario, and any pass-rate or idempotency regression (**a regression stops the cycle — investigate or surface, never commit over it**).

- [ ] **Step 6: Backlog + commit**

Update `docs/backlog.md`: BL-002 and BL-011 marked done with commit range; queue's Wave 1 Track A marked complete.

```bash
python3 evals/check_static.py
git add evals/benchmarks/v7.2.md docs/backlog.md
git commit -m "Record v7.2 benchmark: keep-markers live, idempotency:upgrade added; BL-002 + BL-011 done"
```

---

## Deviations from spec (documented, additive)

1. The spec says keep changes are reported "in Step 7's *Applied* section" — Step 7 has no such section; the plan adds a conditional **Keep list** section instead (Task 1 Step 2).
2. The spec's upgrade fixture carried the keep entry only; the plan additionally exercises the prompt-driven **add** path (second entry via the eval prompt) — covers Decision 2's add semantics, which the spec's eval plan left untested.
3. Pinned check-name slugs in the audit report (Task 2 Step 1) — required to make the spec's negative markers greppable without false hits from check 5 naming `docs/okf/index.md` as a finding *location*.
