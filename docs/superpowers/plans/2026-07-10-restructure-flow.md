# Restructure Flow (BL-004 + BL-005b + BL-012 rider) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Approval-gated restructure (diagnose → propose → approve → apply) with grep-verifiable content fidelity, immovable kept paths, and decision-gated conflicts — plus the eval scenario that proves all of it, landing in the same cycle.

**Architecture:** Law in a new SKILL.md section (protocol, pinned plan format, closed action set, fidelity/keep/decision rules); mechanics in a new `references/restructure.md` (mirrors the migration.md pattern). Eval: the rotted-layer generator gains a `restructure_extras` flag so the audit fixture stays byte-comparable; a new `restructure` grader scenario asserts fidelity, the untouched kept file, the unresolved conflict, and the standard layout. Spec: `docs/superpowers/specs/2026-07-10-restructure-flow-design.md` — binding.

**Tech Stack:** Markdown skill prose; Python 3 stdlib eval scripts.

## Global Constraints

- **NEVER add any `Co-Authored-By` / AI-attribution trailer to any commit.** Absolute repo rule.
- `skill/VERSION` stays `7` — nothing under `skill/assets/rules/` may change.
- Every commit: `python3 evals/check_static.py` must pass first.
- Behavioral `skill/` changes are not "done" until Task 4's e2e benchmark is recorded as `evals/benchmarks/v7.4.md`.
- Pinned strings (the eval greps them): plan header `# AI-Layer Restructure Plan — <repo name>, <ISO date>`; action slugs `[move]` `[merge]` `[link]` `[fix]` `[heal]` `[decision]`; `Kept (immovable):`; outcome suffixes `— applied` / `— skipped` / `— blocked: <why>` / `— decision: open`; `Fidelity: verified (<N> lines tracked)`.
- `decision` items are NEVER applied, even under blanket approval; content is NEVER deleted except by `merge` after verified relocation; content is NEVER invented.
- The audit fixture (rotted-layer, `restructure_extras=False`) must materialize byte-identical to v7.3's — extras appear only in the new `restructure` scenario.

---

### Task 1: SKILL.md restructure section + references/restructure.md + BL-012 rider

**Files:**
- Modify: `skill/SKILL.md` (two one-line rider edits in Step 7; new section at end of file)
- Create: `skill/references/restructure.md`

**Interfaces:**
- Produces: the pinned plan/report format and action slugs listed in Global Constraints — Tasks 2 and 3 grep for them verbatim.

- [ ] **Step 1: BL-012 rider — pin the Keep list heading**

In Step 7's first paragraph, replace:

```
include a **Keep list** section: each entry added or removed
```

with:

```
include a section headed exactly `### Keep list`: each entry added or removed
```

- [ ] **Step 2: BL-012 rider — candidates placement fallback**

In Step 7's harvest paragraph, replace:

```
— after the Keep list section, before Health:
```

with:

```
— after the Keep list section, before Health (when those sections are absent, make it the report's last section):
```

- [ ] **Step 3: Append the Restructure section to skill/SKILL.md** (after the Audit section, at end of file):

````markdown
## Restructure — approval-gated repair

Run this section INSTEAD of Steps 0–7 when the user asks to restructure, repair, or reorganize the AI layer. Detailed mechanics live in `references/restructure.md` — read it before proposing. The protocol is strict: **diagnose → propose → approve → apply**, with a hard stop at approve.

1. **Diagnose (zero writes):** run the Audit section in full. The plan is built from its findings.
2. **Propose (still zero writes):** print a plan headed exactly `# AI-Layer Restructure Plan — <repo name>, <ISO date>`, then numbered items, one line each: `N. [action] <current> → <target>: <one-line what/why>`. `[action]` is one of the closed set — `move`, `merge`, `link`, `fix`, `heal`, `decision` — defined in `references/restructure.md`. Anything conflicting with an owned rule, and any call this skill cannot make alone, is a `decision` item. Findings that would require inventing project content (e.g. a missing journal entry) are never plan items — list them at the end under `For the team:` instead. Close the plan with `Kept (immovable): <path> — <reason>` for every manifest `keep` entry (`Kept (immovable): none` when the list is empty). A kept path must never be the source of a `move`, `merge`, or `fix`; the only action allowed to touch a kept file is linking to it.
3. **Approve — hard stop:** apply nothing until the user approves, item-by-item or all at once. Blanket approval still excludes every `decision` item: ask each one individually; unanswered ones stay open, change nothing, and are re-surfaced by every future run.
4. **Apply + verify:** execute the approved items per `references/restructure.md`, then run the **fidelity pass**: every content line of every file this run moved, merged, or edited must still be greppable somewhere in the repo. If applying an item would lose a line, do not apply it. Print the final report: the plan repeated with an outcome appended to each item (`— applied`, `— skipped`, `— blocked: <why>`, `— decision: open`), then exactly `Fidelity: verified (<N> lines tracked)` — or an explicit failure line naming what was lost and where it came from. Do not commit. A repo already in standard shape yields a plan with no numbered items (open `decision` items excepted) and zero writes.
````

- [ ] **Step 4: Create `skill/references/restructure.md`** with exactly this content:

````markdown
# AI-Layer Restructure Guide

Detailed mechanics for SKILL.md's "Restructure — approval-gated repair" section. The law (protocol, plan format, fidelity/keep/decision rules) lives in SKILL.md; this file is the how-to.

## 1. Standard layout — where each artifact type belongs

| Artifact type | Standard home |
|---|---|
| Implementation plans | `docs/superpowers/plans/` |
| Design specs | `docs/superpowers/specs/` |
| ADRs (any `adrs/`, `doc/adr/` variant) | `docs/adr/` |
| Knowledge docs, overviews, glossaries, maps | `docs/okf/` |
| Backlog / task lists | `docs/backlog.md` |
| Dev journal entries | `docs/journal/` |
| Changelog | `CHANGELOG.md` |
| Foreign AI configs (`.cursorrules`, `.cursor/`, `AGENTS.md`, `.github/copilot-instructions.md`) | prose merged into CLAUDE.md's project sections; the file removed after the merge |
| AI rules prose | CLAUDE.md project sections — **never** `docs/ai/rules/**` (machine-managed law; only `heal` touches it, via Steps 2–3) |

## 2. The action set

- **move** — relocate a file to its standard home, preserving the filename. Grep the repo for references to the old path and update them. Remove the source directory if the move emptied it.
- **merge** — fold each content line of the source into its target home (fit the target's existing sections; add a section only when nothing fits). Verify every content line is present at the target, then — and only then — delete the source file.
- **link** — wire an orphan into the layer: add a markdown link from `docs/okf/index.md` (or a pointer from CLAUDE.md when it is clearly a CLAUDE-level concern). Linking never rewrites the linked file.
- **fix** — repair in place: dangling `@import`/link lines removed (or retargeted when the file moved elsewhere in this plan); unresolved `{{TOKEN}}`s filled per SKILL.md Step 4's derivation rules; stale codebase-map rows corrected from the actual tree.
- **heal** — owned-layer drift or staleness: run SKILL.md Steps 2–3 as-is (byte-for-byte Bash copy, deletions, manifest rewrite with `keep` carried forward). Never hand-edit anything under `docs/ai/`.
- **decision** — presented, never executed. Typical: project text contradicting an owned rule (e.g. a "we don't keep a changelog" note vs `core/changelog.md`), two plausible homes for the same content, or a foreign structure whose removal would lose semantics a merge cannot carry.

## 3. Content carve-outs

When merging foreign or misplaced prose, apply the same classification discipline as migration (`references/migration.md` §1): project-specific facts are kept verbatim; boilerplate that merely restates an owned rule is replaced by the import that covers it. Under restructure, that replacement is allowed **only** for text that restates an owned rule — anything differing in substance from the owned rule is a `decision` item, never a silent deletion.

## 4. The fidelity pass

Before applying, inventory every content line of each file a plan item will move, merge, or edit — non-blank lines, counting the text of headings, bullets, and table cells, but skipping pure markup (fence markers, table rules, horizontal rules). After applying, grep the repo (excluding `.git/`) for each inventoried line. Every miss blocks its item: revert that item, mark it `— blocked: <the lost line>`. Close the report with `Fidelity: verified (<N> lines tracked)` only when every line survived.

## 5. What restructure never does

- Delete project content — `merge` removes a file only after its content verifiably lives elsewhere; `link` deletes nothing; no plan item may propose deleting content (if the user wants something gone, that is their explicit call, not a proposal).
- Invent content — journal entries, overviews the derivation rules cannot produce, or any prose the team must author.
- Resolve owned-rule conflicts on its own authority.
- Touch source code or anything outside the AI layer (CLAUDE.md, `docs/**`, root-level foreign AI configs).
- Commit.
````

- [ ] **Step 5: Verify and commit**

```bash
python3 evals/check_static.py
grep -c "\[decision\]" skill/SKILL.md
grep -c "^## Restructure" skill/SKILL.md
```

Expected: static checks all pass (note: `check_static.py` only asserts `references/migration.md` exists — the new reference file doesn't need registration); first grep prints at least `1`; second prints `1`.

```bash
git add skill/SKILL.md skill/references/restructure.md
git commit -m "SKILL.md: restructure protocol (diagnose/propose/approve/apply) + references/restructure.md; BL-012 rider (Keep list heading, candidates placement fallback)"
```

---

### Task 2: Fixture — restructure_extras, evals.json, README

**Files:**
- Modify: `evals/setup_workspace.py`
- Modify: `evals/evals.json`
- Modify: `evals/README.md`

**Interfaces:**
- Consumes: pinned slugs/strings from Task 1 (`[decision]`, report path convention).
- Produces: `materialize_rotted(dest, restructure_extras=False)`; a `restructure/repo` scenario whose `fixture_meta.json` gains `fidelity_sentences` (5 strings), `conflict_marker`, `kept_path`, `kept_content` — Task 3's grader reads exactly these keys, and reads the report at `<ws>/restructure/outputs/restructure-report.md`.

- [ ] **Step 1: Parameterize the generator**

Change the signature `def materialize_rotted(dest: Path) -> None:` to `def materialize_rotted(dest: Path, restructure_extras: bool = False) -> None:` and append to its docstring: `With restructure_extras, plants two additional relocatables (a non-standard plans dir and a deliberate owned-rule conflict) for the restructure scenario; the audit scenario always materializes WITHOUT extras so its marker set stays version-comparable.`

- [ ] **Step 2: Plant the conflict line in CLAUDE.md**

In the CLAUDE.md `write_text` call, replace the final string fragment:

```python
        "Never delete rows from the invoices table; archive them instead.\n")
```

with:

```python
        "Never delete rows from the invoices table; archive them instead.\n"
        + (
            # Restructure bait: deliberate conflict with core/changelog.md --
            # must surface as a [decision] item, never auto-resolved.
            "\nWe do not maintain CHANGELOG.md; release notes are written "
            "in the wiki at release time.\n"
            if restructure_extras else ""))
```

- [ ] **Step 3: Plant the non-standard plans dir**

After the `.cursorrules` write (Defect 9), add:

```python
    if restructure_extras:
        # Restructure bait: plans in a non-standard location (a `move`).
        (dest / ".claude/plans").mkdir(parents=True)
        (dest / ".claude/plans/2026-01-importer-plan.md").write_text(
            "# Importer split plan\n\n"
            "Planned: split the importer into reader and writer stages.\n")
```

- [ ] **Step 4: Extend meta**

Immediately before the `(dest.parent / "fixture_meta.json").write_text(` line, add:

```python
    if restructure_extras:
        meta["fidelity_sentences"] = [
            "Planned: split the importer into reader and writer stages.",
            "Always write tests first.",
            "Nobody links to this file.",
            "Ultra-specific invoice rounding rules",
            "We do not maintain CHANGELOG.md",
        ]
        meta["conflict_marker"] = (
            "We do not maintain CHANGELOG.md; release notes are written "
            "in the wiki at release time.")
        meta["kept_path"] = "docs/notes/special-sauce.md"
        meta["kept_content"] = (dest / "docs/notes/special-sauce.md").read_text()
```

- [ ] **Step 5: Materialize the new scenario in main()**

After the `rotted-layer` block in `main()`, add:

```python
    repo = ws / "restructure" / "repo"
    materialize_rotted(repo, restructure_extras=True)
```

Also add to the module docstring's scenario list: `  <workspace>/restructure/repo            — rotted + relocatables (restructure scenario)`.

- [ ] **Step 6: evals.json — new entry + idempotency note**

Append to the `evals` array (id 5):

```json
    {
      "id": 5,
      "name": "restructure",
      "prompt": "The AI docs in this repo have rotted and there's leftover config from other AI tools in the way. Restructure the AI layer into the standard shape. I pre-approve applying everything you propose except items that need my decision — leave those open and show them in the report.",
      "expected_output": "Diagnose (full audit, zero writes) -> plan in the pinned format -> apply approved items -> fidelity pass. Zero content loss (every planted sentence still greppable), the kept file byte-identical in place, the CHANGELOG conflict surfaced as an open [decision] item and NOT auto-resolved, .claude/plans relocated to docs/superpowers/plans, .cursorrules merged into CLAUDE.md and removed, ghost-rule import fixed, owned drift healed via Steps 2-3 (manifest at current VERSION, keep list carried). The report ends with 'Fidelity: verified (<N> lines tracked)' and is saved by the eval harness to <ws>/restructure/outputs/restructure-report.md.",
      "assertions": "see grade.py grade_restructure — fidelity_sentences/conflict_marker/kept_* from fixture_meta.json"
    }
```

And append to the idempotency entry's (id 3) `expected_output`: ` Also run on the restructure repo after run 1 is committed — an already-standard layer must yield a no-item plan (open decision items excepted) and zero writes.`

- [ ] **Step 7: evals/README.md**

- E2E step 2: add the sentence `The restructure agent gets the restructure prompt (blanket approval minus decision items is part of it) and must save its final report to <ws>/restructure/outputs/restructure-report.md.`
- E2E step 4: add `python3 evals/grade.py /tmp/legislator-eval-vN idempotency:restructure` beside the existing idempotency commands.
- Scenario descriptions: add `- **restructure** — approval-gated repair: zero content loss (fidelity greps), kept path immovable, owned-rule conflict decision-gated (never auto-resolved), foreign/misplaced structures reach the standard layout; second run is a zero-diff no-op.`

- [ ] **Step 8: Materialize and verify**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/rstest-ws
python3 - <<'EOF'
import json
from pathlib import Path
ws = Path("/tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/rstest-ws")
rot = json.loads((ws / "rotted-layer/fixture_meta.json").read_text())
assert "fidelity_sentences" not in rot, "audit fixture must NOT carry extras"
assert not (ws / "rotted-layer/repo/.claude").exists()
assert "We do not maintain CHANGELOG.md" not in (ws / "rotted-layer/repo/CLAUDE.md").read_text()
rst = json.loads((ws / "restructure/fixture_meta.json").read_text())
assert len(rst["fidelity_sentences"]) == 5 and rst["kept_path"] == "docs/notes/special-sauce.md"
assert rst["conflict_marker"] in (ws / "restructure/repo/CLAUDE.md").read_text()
assert (ws / "restructure/repo/.claude/plans/2026-01-importer-plan.md").exists()
assert rst["kept_content"] == (ws / "restructure/repo/docs/notes/special-sauce.md").read_text()
print("restructure fixture assertions OK")
EOF
rm -rf /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/rstest-ws
```

Expected: `restructure fixture assertions OK`.

- [ ] **Step 9: Commit**

```bash
python3 evals/check_static.py
git add evals/setup_workspace.py evals/evals.json evals/README.md
git commit -m "Fixtures: restructure scenario (rotted + relocatables via restructure_extras), fidelity/conflict/kept meta; eval docs"
```

---

### Task 3: grade.py — grade_restructure + routing

**Files:**
- Modify: `evals/grade.py`

**Interfaces:**
- Consumes: Task 2's meta keys and report path; Task 1's pinned strings (`[decision]`, `Fidelity: verified`).
- Produces: scenario name `restructure` in the default list; `idempotency:restructure` works via the existing generic `grade_idempotency`.

- [ ] **Step 1: Add grade_restructure** (insert after `grade_audit`):

```python
def grade_restructure(ws: Path) -> Grader:
    repo = ws / "restructure" / "repo"
    meta = json.loads((ws / "restructure" / "fixture_meta.json").read_text())
    report_path = ws / "restructure" / "outputs" / "restructure-report.md"
    g = Grader()

    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("restructure_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")

    for s in meta["fidelity_sentences"]:
        hits = subprocess.run(
            ["grep", "-rl", "--exclude-dir=.git", s, str(repo)],
            capture_output=True, text=True).stdout.strip()
        g.check(f"fidelity: {s[:44]!r}", bool(hits),
                f"survives in {hits.splitlines()}" if hits
                else "lost — appears nowhere in the repo")

    kept = repo / meta["kept_path"]
    kept_ok = kept.exists() and kept.read_text() == meta["kept_content"]
    g.check("kept_file_untouched_in_place", kept_ok,
            "byte-identical at original path" if kept_ok
            else "kept file moved, edited, or deleted")

    claude = (repo / "CLAUDE.md").read_text() if (repo / "CLAUDE.md").exists() else ""
    g.check("conflict_not_auto_resolved", meta["conflict_marker"] in claude,
            "conflicting line still in CLAUDE.md" if meta["conflict_marker"] in claude
            else "conflict line gone — auto-resolved without the user")
    decision_open = "[decision]" in report and "We do not maintain CHANGELOG.md" in report
    g.check("conflict_surfaced_as_decision", decision_open,
            "[decision] item names the conflict" if decision_open
            else "report lacks a [decision] item naming the conflict")

    moved_ok = (not (repo / ".claude/plans").exists()
                and (repo / "docs/superpowers/plans/2026-01-importer-plan.md").exists())
    g.check("plans_relocated_to_standard_home", moved_ok,
            ".claude/plans/ gone, file at docs/superpowers/plans/" if moved_ok
            else "plan file not moved (or old dir left behind)")
    g.check("cursorrules_merged_away", not (repo / ".cursorrules").exists(),
            ".cursorrules removed after merge" if not (repo / ".cursorrules").exists()
            else ".cursorrules still present")
    g.check("ghost_import_fixed", "ghost-rule.md" not in claude,
            "dangling import gone" if "ghost-rule.md" not in claude
            else "ghost-rule import still in CLAUDE.md")

    src = SKILL / "assets/rules/core/okf.md"
    okf = repo / "docs/ai/rules/core/okf.md"
    healed = okf.exists() and okf.read_bytes() == src.read_bytes()
    g.check("owned_drift_healed", healed,
            "okf.md byte-identical to skill source" if healed
            else "owned drift not healed via Steps 2-3")

    version = int((SKILL / "VERSION").read_text().strip())
    mpath = repo / "docs/ai/manifest.json"
    manifest = json.loads(mpath.read_text()) if mpath.exists() else None
    heal_ok = bool(manifest and manifest.get("legislatorVersion") == version
                   and {"path": meta["kept_path"], "reason": "works as-is"}
                   in (manifest.get("keep") or []))
    g.check("manifest_healed_keep_carried", heal_ok,
            f"manifest at v{version}, keep entry carried" if heal_ok
            else "manifest missing, stale, or keep entry dropped")

    orphan = repo / "docs/okf/orphan-notes.md"
    refs = subprocess.run(
        ["grep", "-rl", "--exclude-dir=.git", "--include=*.md",
         "orphan-notes.md", str(repo)],
        capture_output=True, text=True).stdout.strip().splitlines()
    linked = orphan.exists() and any(Path(r) != orphan for r in map(Path, refs))
    g.check("orphan_linked_not_deleted", linked,
            "orphan still exists and is now referenced" if linked
            else "orphan deleted or still unreferenced")

    fid = "Fidelity: verified" in report
    g.check("fidelity_line_reported", fid,
            "report carries the pinned fidelity line" if fid
            else "no 'Fidelity: verified' line in the report")
    return g
```

- [ ] **Step 2: Routing + defaults + docstring**

- In `main()`, change the defaults line to:

```python
    names = sys.argv[2:] or ["fresh-scaffold-dotnet", "legacy-migration", "upgrade", "audit", "restructure"]
```

- Add the routing branch after the `audit` branch:

```python
        elif name == "restructure":
            g, outdir = grade_restructure(ws), ws / "restructure"
```

- Module docstring: change "(default: the first four)" to "(default: the first five)" and add the scenario line `  restructure              grade <ws>/restructure/repo + the report saved at restructure/outputs/`.

- [ ] **Step 3: Dry-run for tracebacks**

```bash
python3 evals/setup_workspace.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/rstest-ws2
python3 evals/grade.py /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/rstest-ws2 restructure 2>&1 | tail -22
rm -rf /tmp/claude-1000/-home-admin-Repository-custom-skills-legislator/f3af1a4c-da95-4f54-bc61-8efd5dd7819d/scratchpad/rstest-ws2
```

Expected: exit 1 (un-run fixture — correct), 16 checks listed, no tracebacks. Sanity-read the pre-state results: the five fidelity greps, `kept_file_untouched_in_place`, and `conflict_not_auto_resolved` PASS on the un-run fixture (their content is planted and untouched); `restructure_report_saved`, `conflict_surfaced_as_decision`, `plans_relocated_to_standard_home`, `cursorrules_merged_away`, `ghost_import_fixed`, `owned_drift_healed`, `manifest_healed_keep_carried`, `orphan_linked_not_deleted`, `fidelity_line_reported` FAIL with readable evidence — those are exactly the assertions only a real restructure run can satisfy.

- [ ] **Step 4: Commit**

```bash
python3 evals/check_static.py
git add evals/grade.py
git commit -m "Grader: restructure scenario (fidelity greps, kept-path immutability, decision-gated conflict, standard-layout assertions)"
```

---

### Task 4: Full e2e benchmark v7.4 + record + backlog

**Files:**
- Create: `evals/benchmarks/v7.4.md`
- Modify: `docs/backlog.md` (BL-004 + BL-005b → done; queue Wave 3 complete)

**Interfaces:**
- Consumes: everything above. Run by the main session (it dispatches eval agents), not a subagent.

- [ ] **Step 1:** `python3 evals/setup_workspace.py /tmp/legislator-eval-v7.4`
- [ ] **Step 2:** Run the five scenario agents per `evals/README.md` (prompts from `evals.json`; audit saves to `rotted-layer/outputs/audit-report.md`, migration to `legacy-migration/outputs/step7-report.md`, restructure to `restructure/outputs/restructure-report.md`). Record tokens + wall time.
- [ ] **Step 3:** `python3 evals/grade.py /tmp/legislator-eval-v7.4` — expected exit 0: fresh 13, migration 19, upgrade 13, audit 16, restructure 16.
- [ ] **Step 4:** Commit run-1 results in the fresh, upgrade, AND restructure repos; run the three idempotency agents; `python3 evals/grade.py /tmp/legislator-eval-v7.4 idempotency:fresh-scaffold-dotnet idempotency:upgrade idempotency:restructure` — all 1/1.
- [ ] **Step 5:** Record `evals/benchmarks/v7.4.md` vs v7.3 (same table format; the audit scenario must match v7.3's marker set exactly since its fixture is untouched; any regression stops the cycle — investigate or surface, never commit over it).
- [ ] **Step 6:** Update `docs/backlog.md` (BL-004 done, BL-005b done inside the BL-005 entry, roadmap + queue Wave 3 complete); `python3 evals/check_static.py`; commit: `"Record v7.4 benchmark: restructure live; BL-004 + BL-005b done — queue complete"`.

---

## Deviations from spec (documented)

1. The spec's `For the team:` escape hatch for content-requiring findings (journal recency) is spelled out in SKILL.md's Propose step — the spec implied it under "never invent content" but gave it no report slot.
2. The grader adds `fidelity_line_reported` and `manifest_healed_keep_carried` beyond the spec's five listed assertion groups — direct consequences of the pinned report format and the heal-carries-keep rule, made explicit so they can't silently regress.
