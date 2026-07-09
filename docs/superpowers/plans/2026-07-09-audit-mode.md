# Audit Mode Implementation Plan (BL-001 + BL-005a + BL-010)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a read-only audit mode (full report on request, cheap Health section in upgrade runs), a rotted-layer eval fixture with nine planted defects, and the BL-010 migration-wiring/wording fixes.

**Architecture:** SKILL.md gains an Audit section (10 checks, fixed report format, zero-writes verification) and routes audit-intent requests to it; `references/migration.md` §1 now has migration write the full CLAUDE.md.tpl v2 wiring directly; `setup_workspace.py` generates a rotted fixture from current skill source with greppable defects recorded in `fixture_meta.json`; `grade.py` gains an `audit` scenario plus a migration v2-wiring check. Spec: `docs/superpowers/specs/2026-07-09-audit-mode-design.md`.

**Tech Stack:** Markdown prompt procedure (SKILL.md), Python eval scripts.

## Global Constraints

- Repo root: `/home/admin/Repository/custom_skills/legislator`; all paths relative to it.
- `python3 evals/check_static.py` must pass before every commit.
- `skill/VERSION` stays `7` — this cycle changes procedure text only, no `assets/rules/**` content.
- Never rewrite files under `docs/superpowers/` from earlier dates.
- Commit messages end with: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Task 2 and Task 3 share `fixture_meta.json` key names: `report_markers` (list of strings) and `fixture_commit_count` (int) — exact spelling.
- The full e2e benchmark (Task 4) is mandatory before reporting complete; record as `evals/benchmarks/v7.1.md`.

---

### Task 1: SKILL.md audit section + routing + BL-010 wording, and migration.md §1 rewrite

**Files:**
- Modify: `skill/SKILL.md`
- Modify: `skill/references/migration.md`
- Test: `evals/check_static.py` (existing, no changes)

**Interfaces:**
- Produces: the Audit section's report format (header line `Constitution: v<manifest> (skill source: v<VERSION>) — <up to date | behind>`, findings as `- [check-name] <path>: <finding> → <remedy>`) — Task 2's planted-defect markers and Task 3's grader greps rely on findings naming the offending path/date verbatim.
- Produces: migration mode writing `@docs/okf/codebase-map.md` and `## Boundaries` into CLAUDE.md directly — Task 3 adds a grader check asserting exactly that.

- [ ] **Step 1: Confirm green baseline**

Run: `python3 evals/check_static.py`
Expected: exit 0, `all static checks passed`.

- [ ] **Step 2: Add audit routing to `skill/SKILL.md`'s intro**

After the intro paragraph (the one beginning `Run this procedure against the **current working directory**`), add a new paragraph:

```markdown
If the user's request is to **audit** or health-check the existing AI layer (no scaffolding or upgrading intent), do not run Steps 0–7 — follow the **Audit — read-only health check** section at the end of this file instead.
```

- [ ] **Step 3: BL-010 glossary-row touch-up in Step 4's table**

Replace:

```markdown
| `docs/okf/glossary.md` | `glossary.md.tpl` | Seeded with an intentionally empty table — terms grow under the okf.md sync rule |
```

with:

```markdown
| `docs/okf/glossary.md` | `glossary.md.tpl` | Seeded with an intentionally empty table — terms grow under the okf.md sync rule; `{{PROJECT_NAME}}` and `{{TODAY_ISO}}` as usual |
```

- [ ] **Step 4: Update Step 5's summary item 1**

Replace:

```markdown
1. Split the existing CLAUDE.md into project-specific content (kept) and content now covered by an owned rule (removed, replaced by the `@docs/ai/rules/...` import block from the `CLAUDE.md.tpl` import section).
```

with:

```markdown
1. Split the existing CLAUDE.md into project-specific content (kept) and content now covered by an owned rule (removed, replaced by the full `CLAUDE.md.tpl` v2 wiring: the `@docs/ai/rules/...` import block, the `@docs/okf/codebase-map.md` import, the `## Boundaries` section, and the glossary pointer line — see `references/migration.md` §1; migration writes these directly rather than proposing them in Step 7).
```

- [ ] **Step 5: Add the Health subsection instruction to Step 7**

After the paragraph beginning `Print a summary with four sections:` (and before the `Do not run \`git add\`` line), add:

```markdown
In **upgrade mode only**, append a fifth section, `### Health`, running the cheap audit checks (1–6 in the Audit section below) against the post-run state: list findings in the Audit section's line format; if there are none, print exactly `Health: clean`. Fresh-scaffold and migration runs skip this section — everything they just created is definitionally fresh.
```

- [ ] **Step 6: Append the Audit section to `skill/SKILL.md`**

Add at the end of the file (after Step 7's `Do not run \`git add\` or \`git commit\`. The user reviews and commits.` line):

````markdown
## Audit — read-only health check

Run this section INSTEAD of Steps 0–7 when the user asks to audit or health-check the AI layer. It performs **zero writes**: record `git status --porcelain` and `git rev-parse HEAD` before starting, and verify both are byte-identical after producing the report — if either changed, the run has a bug; say so explicitly in the report.

Perform these checks (severity in parentheses; a finding names the offending path, date, or entry verbatim):

1. **Imports resolve (Critical):** every `@<path>` line in CLAUDE.md points to an existing file.
2. **Unresolved placeholders (Critical):** no `{{TOKEN}}` pattern in CLAUDE.md or any `.md` under `docs/`, except `docs/adr/template.md` (its tokens are intentional).
3. **Owned-layer integrity (Critical):** `docs/ai/manifest.json` parses; every `ownedFiles` entry exists on disk; every owned file is byte-identical to its `<skill-path>/assets/rules/...` source (diff each one).
4. **Constitution staleness (Info):** manifest `legislatorVersion` vs. `<skill-path>/VERSION`; if behind, note "re-run /legislator to upgrade".
5. **OKF index links (Warning):** every markdown link in `docs/okf/index.md` resolves to an existing file.
6. **Codebase-map freshness (Warning):** skip if `docs/okf/codebase-map.md` is absent. Otherwise: every directory named in the map's table exists, and every non-generated top-level directory (ignore hidden directories and `bin/`, `obj/`, `node_modules/`, `dist/`) has a row.
7. **Orphan docs (Warning):** an `.md` under `docs/okf/` or directly in `docs/` that no other `.md` file (or CLAUDE.md) references by markdown link or `@import`. Exempt by directory convention: `docs/ai/rules/**`, `docs/adr/**`, `docs/journal/**`, `docs/superpowers/**`, and `docs/backlog.md`.
8. **Journal recency (Warning):** newest entry date in `docs/journal/` (from filenames or entry content) vs. the date of the last commit touching paths outside `docs/`; flag if the code commit is newer by more than 30 days, citing the newest entry's date.
9. **Foreign AI-layer structures (Info):** list any of `.cursorrules`, `.cursor/`, `AGENTS.md`/`agents.md`, `.github/copilot-instructions.md`, `wiki/`, and ADR/plans directories outside the standard layout (`adrs/`, `doc/adr/`, `.claude/plans/`).
10. **Keep-list integrity (Warning):** if the manifest has a `keep` key, every kept path must exist and be referenced from somewhere; if the key is absent, report `keep-list: not present (pre-BL-002 manifest)` and skip.

Report format — print exactly this structure (omit empty severity sections; a fully clean audit prints the header, `No findings.`, and the clean-checks line):

```
# AI-Layer Audit — <repo name>, <ISO date>

Constitution: v<manifest version> (skill source: v<VERSION>) — <up to date | behind>

## Critical
- [check-name] <path>: <one-line finding> → <one-line remedy>

## Warning
...

## Info
...

Clean checks: <comma-separated names of checks that passed>
```
````

- [ ] **Step 7: Rewrite `skill/references/migration.md` §1's final paragraph**

Replace:

```markdown
Rewrite CLAUDE.md as: kept project-specific content, followed by the six core `@import` lines plus one import line per file in each confirmed stack profile's rule directory (mirroring `CLAUDE.md.tpl`'s import block).
```

with:

```markdown
Rewrite CLAUDE.md with the full `CLAUDE.md.tpl` v2 structure, in this order: kept project-specific content; the core `@import` lines (one per file under `assets/rules/core/`) plus one import line per file in each confirmed stack profile's rule directory; the `@docs/okf/codebase-map.md` import; a `## Boundaries` section (derived per SKILL.md Step 4's `{{BOUNDARIES}}` rule — detect no-touch zones and confirm with the user); and the glossary pointer line (`Domain glossary: docs/okf/glossary.md — check it when a term is unclear; add terms as they emerge`). Write all of this directly — do not defer any of it to Step 7 proposals; migration mode is already rewriting the file. (Upgrade mode, which never edits CLAUDE.md, proposes instead.)
```

- [ ] **Step 8: Run static checks**

Run: `python3 evals/check_static.py`
Expected: exit 0.

- [ ] **Step 9: Commit**

```bash
git add skill/SKILL.md skill/references/migration.md
git commit -m "Add audit mode (10 read-only checks + Health section in upgrade runs); migration writes full CLAUDE.md v2 wiring directly (BL-010)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 2: Rotted-layer fixture generation in `setup_workspace.py`

**Files:**
- Modify: `evals/setup_workspace.py`
- Test: run the script against a temp dir and inspect the generated fixture (commands below)

**Interfaces:**
- Consumes: the Audit report format from Task 1 (markers must be strings that appear verbatim in findings).
- Produces: `<ws>/rotted-layer/repo` (a 2-commit git repo with 9 planted defects) and `<ws>/rotted-layer/fixture_meta.json` with keys `report_markers` (list[str]) and `fixture_commit_count` (int, = 2) — Task 3's grader reads exactly these.

- [ ] **Step 1: Add imports and a dated-commit helper**

In `evals/setup_workspace.py`, add `import os` to the imports, and below the existing `init_commit` function add:

```python
def commit_dated(repo: Path, msg: str, iso_date: str) -> None:
    env = {**os.environ,
           "GIT_AUTHOR_DATE": iso_date, "GIT_COMMITTER_DATE": iso_date}
    subprocess.run(
        ["git", "-c", "user.email=eval@local", "-c", "user.name=eval",
         "add", "-A"], cwd=repo, check=True, capture_output=True)
    subprocess.run(
        ["git", "-c", "user.email=eval@local", "-c", "user.name=eval",
         "commit", "-q", "-m", msg], cwd=repo, check=True,
        capture_output=True, env=env)
```

- [ ] **Step 2: Add the rotted-layer generator**

Below `materialize_upgrade`, add:

```python
def materialize_rotted(dest: Path) -> None:
    """Legislated repo with nine planted defects for the audit scenario.

    Generated from the CURRENT skill source, then deliberately damaged.
    Each defect leaves a distinctive marker string an audit report must
    name verbatim; the markers are recorded in fixture_meta.json.
    """
    (dest / "src/LegacyBilling").mkdir(parents=True)
    (dest / "src/LegacyBilling/LegacyBilling.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk.Web">\n  <PropertyGroup>\n'
        '    <TargetFramework>net8.0</TargetFramework>\n'
        '  </PropertyGroup>\n</Project>\n')

    rules_dst = dest / "docs/ai/rules"
    (rules_dst / "core").mkdir(parents=True)
    (rules_dst / "stacks/dotnet").mkdir(parents=True)
    owned: list[str] = []
    for f in sorted((SKILL / "assets/rules/core").glob("*.md")):
        shutil.copy2(f, rules_dst / "core" / f.name)
        owned.append(f"docs/ai/rules/core/{f.name}")
    for f in sorted((SKILL / "assets/rules/stacks/dotnet").glob("*.md")):
        shutil.copy2(f, rules_dst / "stacks/dotnet" / f.name)
        owned.append(f"docs/ai/rules/stacks/dotnet/{f.name}")

    # Defect 3 — owned-file drift: one appended line differs from source.
    with open(rules_dst / "core" / "okf.md", "a") as fh:
        fh.write("\n- Local tweak someone hand-added (drift!)\n")

    # Defect 4 — stale manifest: one version behind the skill source.
    version = int((SKILL / "VERSION").read_text().strip())
    (dest / "docs/ai").mkdir(parents=True, exist_ok=True)
    (dest / "docs/ai/manifest.json").write_text(
        "{\n"
        f'  "legislatorVersion": {version - 1},\n'
        '  "profiles": ["dotnet"],\n'
        '  "ownedFiles": [\n'
        + ",\n".join(f'    "{p}"' for p in sorted(owned))
        + "\n  ]\n}\n")

    # Defect 1 — broken import (ghost-rule.md does not exist).
    imports = "\n".join(f"@{p}" for p in sorted(owned))
    (dest / "CLAUDE.md").write_text(
        "# LegacyBilling\n\n" + imports +
        "\n@docs/ai/rules/core/ghost-rule.md\n@docs/okf/codebase-map.md\n\n"
        "- Domain glossary: `docs/okf/glossary.md` — check it when a term is "
        "unclear; add terms as they emerge\n\n"
        "## Boundaries\n\nGenerated build output only (`bin/`, `obj/`, "
        "`node_modules/`) — do not edit generated files.\n\n"
        "## Project notes\n\nLegacyBilling processes archived invoices.\n")

    okf = dest / "docs/okf"
    okf.mkdir(parents=True)
    # Defect 5 — stale index link (renamed-away.md does not exist).
    (okf / "index.md").write_text(
        "# OKF Index\n\n- [Log](log.md)\n- [Overview draft](overview-draft.md)\n"
        "- [Old notes](renamed-away.md)\n- [Glossary](glossary.md)\n")
    (okf / "log.md").write_text(
        "# OKF Log\n\n## 2026-01-15 — Initial legislation\n\nSet up.\n")
    # Defect 2 — unresolved placeholder (linked from the index, so it is
    # a token defect, not an orphan — defects stay isolated).
    (okf / "overview-draft.md").write_text(
        "# Overview\n\n{{PROJECT_OVERVIEW}}\n")
    # Defect 6 — stale map: lists legacy/ (does not exist), omits src/.
    (okf / "codebase-map.md").write_text(
        "---\ntype: System\ntitle: LegacyBilling — Codebase Map\n"
        "description: Top-level directory map — where things live in this repo.\n"
        "tags: [system, architecture, map]\ntimestamp: 2026-01-15T00:00:00Z\n"
        "status: implemented\n---\n\n# Codebase Map\n\n"
        "One line per top-level directory. Keep this table in sync with the "
        "actual tree (the okf.md sync rule applies): update it when "
        "directories are added, removed, or repurposed.\n\n"
        "| Directory | What lives there |\n|-----------|------------------|\n"
        "| `docs/` | Documentation |\n"
        "| `legacy/` | Old importer kept for reference |\n")
    (okf / "glossary.md").write_text(
        "---\ntype: System\ntitle: LegacyBilling — Domain Glossary\n"
        "description: Domain terms mapped to their meaning in this codebase.\n"
        "tags: [system, glossary, domain]\ntimestamp: 2026-01-15T00:00:00Z\n"
        "status: implemented\n---\n\n# Domain Glossary\n\n"
        "| Term | Meaning in this codebase |\n|------|--------------------------|\n")
    # Defect 7 — orphan: linked from nowhere.
    (okf / "orphan-notes.md").write_text(
        "# Scratch notes\n\nNobody links to this file.\n")

    (dest / "docs/journal").mkdir(parents=True)
    (dest / "docs/journal/README.md").write_text(
        "# Dev Journal\n\nEntries go here, one per working session.\n")
    # Defect 8 — dead journal: newest entry 2026-01-15; a later commit
    # touches src/ dated 2026-07-01 (see the two-commit history below).
    (dest / "docs/journal/2026-01-15-setup.md").write_text(
        "# 2026-01-15 — Initial setup\n\nLegislated the repo.\n")
    (dest / "docs/backlog.md").write_text(
        "# LegacyBilling — Backlog\n\n- BL-001 — Archive importer cleanup.\n")
    (dest / "CHANGELOG.md").write_text(
        "# Changelog\n\n## Unreleased\n\n- Initial legislation.\n")
    # Defect 9 — foreign AI-layer structure.
    (dest / ".cursorrules").write_text("Always write tests first.\n")

    git(dest, "init", "-q")
    commit_dated(dest, "fixture: rotted-layer at 2026-01-15", "2026-01-15T12:00:00")
    # Second commit: code change months after the last journal entry.
    with open(dest / "src/LegacyBilling/Endpoints.cs", "w") as fh:
        fh.write("// new endpoint added long after the journal went quiet\n")
    commit_dated(dest, "Add endpoints (no journal entry)", "2026-07-01T12:00:00")

    meta = {
        "report_markers": [
            "ghost-rule.md",          # defect 1: broken import
            "overview-draft.md",      # defect 2: unresolved token file
            "docs/ai/rules/core/okf.md",  # defect 3: owned drift
            "behind",                 # defect 4: stale manifest (header wording)
            "renamed-away.md",        # defect 5: stale index link
            "legacy/",                # defect 6: stale map row
            "orphan-notes.md",        # defect 7: orphan doc
            "2026-01-15",             # defect 8: dead journal (entry date cited)
            ".cursorrules",           # defect 9: foreign structure
        ],
        "fixture_commit_count": 2,
        "expected_manifest_version": version - 1,
    }
    (dest.parent / "fixture_meta.json").write_text(
        json.dumps(meta, indent=2) + "\n")
```

- [ ] **Step 3: Wire the scenario into `main()`**

In `main()`, after the upgrade block (`repo = ws / "upgrade" / "repo"` … `init_commit(...)`), add:

```python
    repo = ws / "rotted-layer" / "repo"
    materialize_rotted(repo)
```

(No `init_commit` call — `materialize_rotted` makes its own two dated commits.)

- [ ] **Step 4: Generate and inspect a workspace**

```bash
python3 -m py_compile evals/setup_workspace.py && echo OK
rm -rf /tmp/rotted-check && python3 evals/setup_workspace.py /tmp/rotted-check
git -C /tmp/rotted-check/rotted-layer/repo log --format='%h %ad %s' --date=short
cat /tmp/rotted-check/rotted-layer/fixture_meta.json
git -C /tmp/rotted-check/rotted-layer/repo status --porcelain
```

Expected: `OK`; four repos listed by the setup script; the rotted repo shows exactly 2 commits dated 2026-01-15 and 2026-07-01; the meta file has 9 `report_markers` and `fixture_commit_count: 2`; `status --porcelain` is empty (clean tree). Then `rm -rf /tmp/rotted-check`.

- [ ] **Step 5: Commit**

```bash
git add evals/setup_workspace.py
git commit -m "Eval fixture: rotted-layer repo with nine planted, greppable defects (BL-005a)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 3: Grader `audit` scenario + migration v2-wiring check + eval docs

**Files:**
- Modify: `evals/grade.py`
- Modify: `evals/evals.json`
- Modify: `evals/README.md`
- Test: `python3 -m py_compile evals/grade.py` (behavioral proof lands in Task 4)

**Interfaces:**
- Consumes: `<ws>/rotted-layer/fixture_meta.json` keys `report_markers`, `fixture_commit_count` (Task 2); the audit report saved by the eval agent at `<ws>/rotted-layer/outputs/audit-report.md`; migration mode writing `@docs/okf/codebase-map.md` + `## Boundaries` into CLAUDE.md (Task 1).

- [ ] **Step 1: Add `grade_audit` to `evals/grade.py`**

Below `grade_upgrade`, add:

```python
def grade_audit(ws: Path) -> Grader:
    repo = ws / "rotted-layer" / "repo"
    meta = json.loads((ws / "rotted-layer" / "fixture_meta.json").read_text())
    report_path = ws / "rotted-layer" / "outputs" / "audit-report.md"
    g = Grader()

    has_report = report_path.exists()
    report = report_path.read_text() if has_report else ""
    g.check("audit_report_saved", has_report,
            str(report_path) if has_report else f"missing: {report_path}")

    for marker in meta["report_markers"]:
        g.check(f"report names {marker!r}", marker in report,
                "named in report" if marker in report else "absent from report")

    status = git(repo, "status", "--porcelain").strip()
    commits = len(git(repo, "log", "--oneline").strip().splitlines())
    clean = not status and commits == meta["fixture_commit_count"]
    g.check("zero_writes", clean,
            "working tree untouched, no new commits" if clean
            else f"status={status[:200]!r}, commits={commits} (expected {meta['fixture_commit_count']})")
    return g
```

- [ ] **Step 2: Add the migration v2-wiring check**

In `grade_migration`, after the `claude_md_imports_rules`-producing `g.scaffold_checks(repo)` call and before the `MIGRATION_PRESERVED` loop, add:

```python
    claude = (repo / "CLAUDE.md").read_text() if (repo / "CLAUDE.md").exists() else ""
    v2_wired = "@docs/okf/codebase-map.md" in claude and "## Boundaries" in claude
    g.check("claude_md_v2_wiring_written_directly", v2_wired,
            "map import + Boundaries section present in rewritten CLAUDE.md" if v2_wired
            else "migration left v2 wiring as Step 7 proposals instead of writing it")
```

- [ ] **Step 3: Register the scenario in `main()` and the docstring**

In `main()`, change the default scenario list to:

```python
    names = sys.argv[2:] or ["fresh-scaffold-dotnet", "legacy-migration", "upgrade", "audit"]
```

and add a dispatch branch after the `upgrade` branch:

```python
        elif name == "audit":
            g, outdir = grade_audit(ws), ws / "rotted-layer"
```

In the module docstring's scenario list, add the line:

```
  audit                    grade the audit report saved by the eval agent at
                           <ws>/rotted-layer/outputs/audit-report.md against
                           the fixture's planted defects; asserts zero writes.
```

- [ ] **Step 4: Add the audit eval to `evals/evals.json`**

Append to the `evals` array (after id 3):

```json
    {
      "id": 4,
      "name": "audit",
      "prompt": "Something feels off with the AI docs in this repo — audit the AI layer here and tell me what's rotted. Don't change anything, just report.",
      "expected_output": "Full read-only audit: a severity-ranked report naming all nine planted defects (broken ghost-rule import, unresolved {{PROJECT_OVERVIEW}} token, drifted okf.md owned file, manifest one version behind, stale renamed-away.md index link, stale legacy/ map row, orphan-notes.md orphan, journal dead since 2026-01-15 while code moved on, .cursorrules foreign structure). Zero writes: git status and HEAD identical before and after. In evals the agent saves the report to <ws>/rotted-layer/outputs/audit-report.md — outside the target repo.",
      "assertions": "see grade.py grade_audit — markers from fixture_meta.json"
    }
```

- [ ] **Step 5: Update `evals/README.md`**

Three edits:

1. In the layer table, change the E2E row's cost cell from `~4 agent runs (~60k tokens each)` to `~5 agent runs (~40–60k tokens each)`.
2. In the "E2E procedure" section's step 2, change `Scenarios: \`fresh-scaffold-dotnet\`, \`legacy-migration\`, \`upgrade\`.` to `Scenarios: \`fresh-scaffold-dotnet\`, \`legacy-migration\`, \`upgrade\`, \`audit\` (the audit agent must be told to save its report to \`<ws>/rotted-layer/outputs/audit-report.md\` — outside the target repo, which the audit must not touch).`
3. In "What each scenario protects", append:

```markdown
- **audit** — read-only rot detection: the report must name every planted
  defect in the rotted fixture (see `setup_workspace.py:materialize_rotted`
  for the nine defects), and the repo must be byte-untouched afterwards
  (zero-writes contract).
```

- [ ] **Step 6: Verify and commit**

```bash
python3 -m py_compile evals/grade.py && echo OK
python3 evals/check_static.py > /dev/null && echo STATIC_OK
git add evals/grade.py evals/evals.json evals/README.md
git commit -m "Grader: audit scenario (planted-defect markers, zero-writes) + migration v2-wiring check

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 4: Full e2e benchmark (v7.1) — run by the controller

This task follows `evals/README.md` and dispatches agents; the controller session executes it directly.

**Files:**
- Create: `evals/benchmarks/v7.1.md`

**Interfaces:**
- Consumes: everything from Tasks 1–3.
- Produces: the recorded verdict; the regression gate for reporting this cycle complete.

- [ ] **Step 1: Materialize a fresh workspace**

Run: `python3 evals/setup_workspace.py <scratch>/eval-v7.1`
Expected: four repos listed (fresh, migration, upgrade, rotted-layer).

- [ ] **Step 2: Dispatch one fresh agent per scenario (parallel, 4 agents)**

Prompts from `evals/evals.json`. Standard eval-agent framing (skill path, target repo, follow SKILL.md exactly, confirmations pre-approved, never commit). The audit agent additionally: save the report to `<ws>/rotted-layer/outputs/audit-report.md`; do not write anything inside the target repo.

- [ ] **Step 3: Grade all four scenarios**

Run: `python3 evals/grade.py <scratch>/eval-v7.1`
Expected: fresh 10/10; migration 13/13 (new `claude_md_v2_wiring_written_directly` check); upgrade 10/10 (its Step 7 report should also show the new `### Health` section — verify by reading the upgrade agent's summary, expected `Health: clean` or minor notes); audit 11/11 (report saved + 9 markers + zero_writes). Any failure: investigate per `evals/README.md` before concluding.

- [ ] **Step 4: Idempotency pass**

Commit run 1 in the fresh-scaffold repo, re-dispatch the same fresh-scaffold prompt, then:

Run: `python3 evals/grade.py <scratch>/eval-v7.1 idempotency:fresh-scaffold-dotnet`
Expected: `1/1`, zero diff.

- [ ] **Step 5: Record `evals/benchmarks/v7.1.md`**

Same table format as v7.md, compared against v7 (same constitution version — this row measures the procedure change): per-scenario pass counts, tokens, wall time; note the audit scenario is new (no prior baseline) and whether the upgrade run's Health section behaved as designed.

- [ ] **Step 6: Commit**

```bash
git add evals/benchmarks/v7.1.md
git commit -m "Record v7.1 benchmark: audit mode + migration v2 wiring, all scenarios passing

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
