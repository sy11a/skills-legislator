#!/usr/bin/env python3
"""Materialize eval fixture repos into a workspace directory.

Creates one git-initialized repo per scenario, ready for an agent to run the
legislator skill against:

  <workspace>/fresh-scaffold-dotnet/repo   — new repo, no CLAUDE.md
  <workspace>/legacy-migration/repo        — hand-written CLAUDE.md, no manifest
  <workspace>/upgrade/repo                 — previously legislated, one version behind
  <workspace>/rotted-layer/repo            — legislated, fifteen planted defects (audit scenario)
  <workspace>/restructure/repo            — rotted + relocatables (restructure scenario)

The upgrade repo is generated from the CURRENT skill source so this suite
never rots as the constitution evolves: it contains every current core+dotnet
rule EXCEPT the alphabetically last core rule AND the alphabetically last
dotnet stack rule (simulating one core and one stack rule added to the
constitution since the repo was last legislated), PLUS one retired rule that
no longer exists in the source (so the run must delete it — this exercises
deletion propagation). Its manifest records VERSION-1. Its manifest also
carries one `keep` entry; the eval prompt adds a second — together they
exercise keep carry-forward, prompt-driven adds, and pinned keep serialization.

Usage: python3 evals/setup_workspace.py <workspace-dir>
The workspace dir must not already exist (refuses to overwrite).
"""
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

EVALS = Path(__file__).resolve().parent
SKILL = EVALS.parent / "skill"

RETIRED_RULE = "retired-rule.md"


def git(repo: Path, *args: str) -> None:
    subprocess.run(
        ["git", "-c", "user.email=eval@local", "-c", "user.name=eval", *args],
        cwd=repo, check=True, capture_output=True,
    )


def init_commit(repo: Path, msg: str) -> None:
    git(repo, "init", "-q")
    git(repo, "add", "-A")
    git(repo, "commit", "-q", "-m", msg)


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


def materialize_upgrade(dest: Path) -> None:
    shutil.copytree(EVALS / "fixtures" / "upgrade-base", dest)
    core_src = sorted((SKILL / "assets/rules/core").glob("*.md"))
    dotnet_src = sorted((SKILL / "assets/rules/stacks/dotnet").glob("*.md"))
    if len(core_src) < 2 or len(dotnet_src) < 2:
        sys.exit("upgrade fixture needs at least 2 core and 2 dotnet rules to withhold one of each")
    withheld = core_src[-1]  # the core rule "added since this repo was legislated"
    withheld_stack = dotnet_src[-1]  # ditto for the dotnet stack (BL-017 R4)

    rules_dst = dest / "docs/ai/rules"
    (rules_dst / "core").mkdir(parents=True)
    (rules_dst / "stacks/dotnet").mkdir(parents=True)

    owned: list[str] = []
    for f in core_src:
        if f.name != withheld.name:
            shutil.copy2(f, rules_dst / "core" / f.name)
            owned.append(f"docs/ai/rules/core/{f.name}")
    for f in dotnet_src:
        if f.name != withheld_stack.name:
            shutil.copy2(f, rules_dst / "stacks/dotnet" / f.name)
            owned.append(f"docs/ai/rules/stacks/dotnet/{f.name}")

    (rules_dst / "core" / RETIRED_RULE).write_text(
        "## Retired Rule\n\n- This rule was removed from the constitution "
        "after this repo was last legislated; an upgrade run must delete it.\n"
    )
    owned.append(f"docs/ai/rules/core/{RETIRED_RULE}")

    version = int((SKILL / "VERSION").read_text().strip())
    owned_sorted = sorted(owned)
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
    (dest / "docs" / "ai" / "manifest.json").write_text(manifest)

    imports = "\n".join(f"@{p}" for p in owned_sorted)
    (dest / "CLAUDE.md").write_text(
        "# BillingApi\n\n" + imports +
        "\n\n## Project notes\n\nBillingApi handles invoice generation and "
        "payment webhooks. Legislated one constitution version ago.\n"
    )

    # Record what was withheld so grade.py doesn't have to re-derive it in a
    # way that could disagree with this script.
    meta = {
        "withheld_core_rule": withheld.name,
        "withheld_stack_rule": withheld_stack.name,
        "retired_rule": RETIRED_RULE,
        "fixture_manifest_version": version - 1,
        # Entry 0 pre-exists in the fixture manifest (carry-forward test);
        # entry 1 is added by the eval prompt (add-path test). Sorted by path.
        "expected_keep": [
            {"path": "docs/notes/deploy-runbook.md", "reason": "battle-tested deploy runbook"},
            {"path": "docs/notes/perf-tuning.md", "reason": "hand-tuned GC settings notes"},
        ],
    }
    (dest.parent / "fixture_meta.json").write_text(json.dumps(meta, indent=2) + "\n")


def materialize_case_practice(dest: Path) -> None:
    """BL-036 Wave C: a clean legislated repo (current VERSION, full
    corpus, manifest, case home) with a tiny src tree. The scenario task
    is ordinary feature work — the grader asserts the case landed per
    core/sdd.md (the only test that EXECUTES the law rather than its
    delivery)."""
    shutil.copytree(EVALS / "fixtures" / "upgrade-base", dest)
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

    # v20: the engine is an owned file. The delivered law now tells an
    # agent to run `python3 docs/ai/engine.py anchors` before reporting
    # done (core/verification.md's rung) — this scenario's feature-work
    # task must be able to obey that, so the engine ships here too.
    (dest / "docs/ai").mkdir(parents=True, exist_ok=True)
    shutil.copy2(SKILL / "assets/engine/engine.py", dest / "docs/ai/engine.py")
    owned.append("docs/ai/engine.py")

    version = int((SKILL / "VERSION").read_text().strip())
    owned_sorted = sorted(owned)
    (dest / "docs/ai/manifest.json").write_text(
        "{\n"
        f'  "legislatorVersion": {version},\n'
        '  "stacks": ["dotnet"],\n'
        '  "keep": [],\n'
        '  "ownedFiles": [\n'
        + ",\n".join(f'    "{p}"' for p in owned_sorted)
        + "\n  ]\n}\n")
    (dest / "docs/cases").mkdir(parents=True)
    shutil.copy2(SKILL / "assets/templates/cases-README.md.tpl",
                 dest / "docs/cases/README.md")

    # The engine is owned but never @-imported (SKILL.md's import block is
    # rules only), so it is excluded here even though it is in the
    # manifest's ownedFiles.
    imports = "\n".join(
        f"@{p}" for p in owned_sorted if p != "docs/ai/engine.py")
    (dest / "AGENTS.md").write_text(
        "# BillingApi\n\n" + imports +
        "\n\n## Project notes\n\nBillingApi handles invoice generation and "
        "payment webhooks. Fully legislated.\n")
    import os
    os.symlink("AGENTS.md", dest / "CLAUDE.md")


def materialize_upgrade_drop_stack(dest: Path) -> None:
    """BL-036 Wave B: a repo subscribed to dotnet+aurelia whose user asks
    to drop aurelia. Asserts the only deletion-semantics-by-stack branch:
    aurelia owned files must be deleted, dotnet untouched. Aurelia has
    exactly one rule file in the current corpus — copy it as-is."""
    shutil.copytree(EVALS / "fixtures" / "upgrade-base", dest)
    core_src = sorted((SKILL / "assets/rules/core").glob("*.md"))
    dotnet_src = sorted((SKILL / "assets/rules/stacks/dotnet").glob("*.md"))
    aurelia_src = sorted((SKILL / "assets/rules/stacks/aurelia").glob("*.md"))
    if not aurelia_src:
        sys.exit("drop-stack fixture needs at least one aurelia rule")

    rules_dst = dest / "docs/ai/rules"
    (rules_dst / "core").mkdir(parents=True)
    (rules_dst / "stacks/dotnet").mkdir(parents=True)
    (rules_dst / "stacks/aurelia").mkdir(parents=True)

    owned: list[str] = []
    for f in core_src:
        shutil.copy2(f, rules_dst / "core" / f.name)
        owned.append(f"docs/ai/rules/core/{f.name}")
    for f in dotnet_src:
        shutil.copy2(f, rules_dst / "stacks/dotnet" / f.name)
        owned.append(f"docs/ai/rules/stacks/dotnet/{f.name}")
    aurelia_names = []
    for f in aurelia_src:
        shutil.copy2(f, rules_dst / "stacks/aurelia" / f.name)
        owned.append(f"docs/ai/rules/stacks/aurelia/{f.name}")
        aurelia_names.append(f.name)

    version = int((SKILL / "VERSION").read_text().strip())
    owned_sorted = sorted(owned)
    manifest = (
        "{\n"
        f'  "legislatorVersion": {version - 1},\n'
        '  "profiles": ["dotnet", "aurelia"],\n'
        '  "keep": [],\n'
        '  "ownedFiles": [\n'
        + ",\n".join(f'    "{p}"' for p in owned_sorted)
        + "\n  ]\n}\n"
    )
    (dest / "docs" / "ai" / "manifest.json").write_text(manifest)

    imports = "\n".join(f"@{p}" for p in owned_sorted)
    (dest / "CLAUDE.md").write_text(
        "# BillingApi\n\n" + imports +
        "\n\n## Project notes\n\nBillingApi handles invoice generation and "
        "payment webhooks. Legislated one constitution version ago.\n"
    )

    meta = {
        "stacks": ["dotnet"],           # the post-drop subscription
        "dropped_stack": "aurelia",
        "dropped_stack_files": [f"docs/ai/rules/stacks/aurelia/{n}" for n in aurelia_names],
        "expected_keep": [],
    }
    (dest.parent / "fixture_meta.json").write_text(json.dumps(meta, indent=2) + "\n")


def materialize_rotted(dest: Path, restructure_extras: bool = False) -> None:
    """Legislated repo with fifteen planted defects for the audit scenario.

    Generated from the CURRENT skill source, then deliberately damaged.
    Each defect leaves a distinctive marker string an audit report must
    name verbatim; the markers are recorded in fixture_meta.json.
    With restructure_extras, plants two additional relocatables (a non-standard plans dir and a deliberate owned-rule conflict) for the restructure scenario; the audit scenario always materializes WITHOUT extras so its marker set stays version-comparable.
    """
    (dest / "src/LegacyBilling").mkdir(parents=True)
    (dest / "src/LegacyBilling/LegacyBilling.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk.Web">\n  <PropertyGroup>\n'
        '    <TargetFramework>net8.0</TargetFramework>\n'
        '  </PropertyGroup>\n</Project>\n')
    (dest / "src/LegacyBilling/Endpoints.cs").write_text(
        "// initial endpoint surface\n")

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

    # v20: the engine is an owned file. The fixture carries it (checks 15
    # and 17 need a runnable engine); defect 4 is about the manifest's
    # version field, not about which files were delivered.
    (dest / "docs/ai").mkdir(parents=True, exist_ok=True)
    shutil.copy2(SKILL / "assets/engine/engine.py", dest / "docs/ai/engine.py")
    owned.append("docs/ai/engine.py")

    # Defect 3 — owned-file drift: one appended line differs from source.
    with open(rules_dst / "core" / "okf.md", "a") as fh:
        fh.write("\n- Local tweak someone hand-added (drift!)\n")

    # Defect 4 — stale manifest: one version behind the skill source.
    version = int((SKILL / "VERSION").read_text().strip())
    (dest / "docs/ai").mkdir(parents=True, exist_ok=True)
    (dest / "docs/ai/manifest.json").write_text(
        "{\n"
        f'  "legislatorVersion": {version - 1},\n'
        '  "stacks": ["dotnet"],\n'
        '  "keep": [\n'
        '    {"path": "docs/notes/special-sauce.md", "reason": "works as-is"},\n'
        '    {"path": "docs/notes/gone-runbook.md", "reason": "deleted last sprint, entry forgot"}\n'
        "  ],\n"
        '  "ownedFiles": [\n'
        + ",\n".join(f'    "{p}"' for p in sorted(owned))
        + "\n  ]\n}\n")

    # Defect 1 — broken import (ghost-rule.md does not exist). The engine is
    # owned but never @-imported (SKILL.md's import block is rules only), so
    # it is excluded here even though it is in the manifest's ownedFiles.
    imports = "\n".join(
        f"@{p}" for p in sorted(owned) if p != "docs/ai/engine.py")
    (dest / "CLAUDE.md").write_text(
        "# LegacyBilling\n\n" + imports +
        "\n@docs/ai/rules/core/ghost-rule.md\n@docs/okf/codebase-map.md\n\n"
        "- Domain glossary: `docs/okf/glossary.md` — check it when a term is "
        "unclear; add terms as they emerge\n\n"
        "## Boundaries\n\nGenerated build output only (`bin/`, `obj/`, "
        "`node_modules/`) — do not edit generated files.\n\n"
        "## Project notes\n\nLegacyBilling processes archived invoices.\n\n"
        # Harvest bait: one law-shaped generalizable line the audit's
        # constitution-candidates appendix must quote. Phrased stack-generic
        # on purpose: the v7.4 first run rejected an earlier wording ("the
        # archive importer") as project-instance data -- defensibly, which
        # made the bait ambiguous. The marker substring is unchanged.
        "Always run data imports in dry-run mode before a real import.\n"
        # ...and one suppressed by the not-law marker, which it must NOT.
        "<!-- legislator: not-law -->\n"
        "Never delete rows from the invoices table; archive them instead.\n"
        + (
            # Restructure bait: deliberate conflict with core/changelog.md --
            # must surface as a [decision] item, never auto-resolved.
            "\nWe do not maintain CHANGELOG.md; release notes are written "
            "in the wiki at release time.\n"
            if restructure_extras else ""))

    okf = dest / "docs/okf"
    okf.mkdir(parents=True)
    # Defect 5 — stale index link (renamed-away.md does not exist).
    (okf / "index.md").write_text(
        "# OKF Index\n\n- [Log](log.md)\n- [Overview draft](overview-draft.md)\n"
        "- [Old notes](renamed-away.md)\n- [Glossary](glossary.md)\n"
        "- [Importer](importer.md)\n- [Endpoints](endpoints.md)\n")
    (okf / "log.md").write_text(
        "# OKF Log\n\n## 2026-01-10 — Initial legislation\n\nSet up.\n")
    # Defect 2 — unresolved placeholder (linked from the index, so it is
    # a token defect, not an orphan — defects stay isolated).
    (okf / "overview-draft.md").write_text(
        "# Overview\n\n{{PROJECT_OVERVIEW}}\n")
    # Defect 6 — stale map: lists legacy/ (does not exist), omits src/.
    (okf / "codebase-map.md").write_text(
        "---\ntype: System\ntitle: LegacyBilling — Codebase Map\n"
        "description: Top-level directory map — where things live in this repo.\n"
        "tags: [system, architecture, map]\ntimestamp: 2026-01-10T00:00:00Z\n"
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
        "tags: [system, glossary, domain]\ntimestamp: 2026-01-10T00:00:00Z\n"
        "status: implemented\n---\n\n# Domain Glossary\n\n"
        "| Term | Meaning in this codebase |\n|------|--------------------------|\n")
    # Defect 7 — orphan: linked from nowhere.
    (okf / "orphan-notes.md").write_text(
        "# Scratch notes\n\nNobody links to this file.\n")

    # Defect 16 (check 15, okf-anchors) — a concept doc naming code that is
    # gone: one path-anchor to a file that does not exist and one symbol
    # nowhere in the source. Linked from the index, so it is not an orphan.
    importer_md_text = (
        "# Importer\n\n"
        "The archive importer lives in `src/LegacyBilling/Removed/OldImporter.cs` "
        "and its sweep step is `ArchivedInvoiceSweeper`.\n")
    (okf / "importer.md").write_text(importer_md_text)
    # Defect 17 (check 17, okf-sync-debt) — a doc whose anchored source is
    # touched by the second commit (2026-07-01), 167 days after the doc's
    # own commit (2026-01-15). Isolated from defect 16: its anchor resolves.
    endpoints_md_text = (
        "# Endpoints\n\n"
        "The public surface is `src/LegacyBilling/Endpoints.cs`.\n")
    (okf / "endpoints.md").write_text(endpoints_md_text)

    # Defect 10 — keep-listed file that nothing references (protected but
    # orphaned). Lives under docs/notes/ so orphan check 7 (which scans only
    # docs/okf/ and docs/ top level) cannot double-report it — the finding
    # must come from keep-list check 10.
    (dest / "docs/notes").mkdir(parents=True)
    (dest / "docs/notes/special-sauce.md").write_text(
        "# Special sauce\n\nUltra-specific invoice rounding rules that work "
        "in production. Keep as-is.\n")

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

    # Defect 14 -- foreign glossary/domain store (parallel-constitution
    # artifact, check 9). Check-9 files are NOT harvest sources (check-12
    # precedence), so NEITHER line may appear as a constitution candidate;
    # the generic line is expected to surface in the finding text instead.
    # Restructure must merge it away per the routing row (definition ->
    # okf/glossary row, law line -> .claude/rules/).
    (dest / "UBIQUITOUS_LANGUAGE.md").write_text(
        "# Ubiquitous Language\n\n"
        "- Domain events are named in past tense (InvoiceSettled, "
        "never SettleInvoice).\n"
        "- A billing period in LegacyBilling always runs from the 1st to "
        "the last day of the calendar month.\n")

    # Defect 11 -- a project rule (in the standard .claude/rules/ home) that
    # contradicts owned law (core/dev-journal.md). Audit check 11 must flag
    # it; restructure must decision-gate it, never edit it.
    (dest / ".claude/rules").mkdir(parents=True)
    (dest / ".claude/rules/journal.md").write_text(
        "# Journal policy\n\n"
        "Dev journal entries are optional; skip them for small changes.\n")

    # Defect 15 -- skill-bindings rot: the repo sanctions a skill that is
    # not installed on any machine. Audit check 14 must flag it at Info;
    # restructure must route it to "For the team" and leave the file
    # byte-unchanged (machine setup, never a repo plan item).
    (dest / ".claude/rules/skills.md").write_text(
        "# Skill Law (this repo)\n\n"
        "- **pre-plan:** `grilling`\n"
        "- **implement:** `made-up-skill`\n")

    # Negative control (BL-036 Wave B): a healthy case file in the v17
    # case home. Not a defect — checks 7/12 exempt docs/cases/**; its
    # absence from the audit report is the assert (absent-marker).
    (dest / "docs/cases/BL-0042").mkdir(parents=True)
    (dest / "docs/cases/BL-0042/summary.md").write_text(
        "# BL-0042 — settled invoice export\n\n"
        "Tier: 1 (light). Converged 2026-01-20.\n")

    # Defect 12 -- stray rulebook: law-shaped review rules parked at
    # docs/superpowers/ top level (exempt from orphan check 7, invisible to
    # every session). Audit check 12 must flag it under its slug; the
    # harvest must propose the generic line and NOT the project-specific
    # one; restructure must merge its law into .claude/rules/ and remove it.
    (dest / "docs/superpowers").mkdir(parents=True, exist_ok=True)
    (dest / "docs/superpowers/review-checklist.md").write_text(
        "# LegacyBilling — Review Checklist\n\n"
        "- Every database migration must be reversible; write the Down() "
        "step before merging.\n"
        "- Invoice PDFs are rendered only through the PdfRenderer service; "
        "never call wkhtmltopdf directly.\n")

    if restructure_extras:
        # Restructure bait: plans in a non-standard location (a `move`).
        (dest / ".claude/plans").mkdir(parents=True, exist_ok=True)
        (dest / ".claude/plans/2026-01-importer-plan.md").write_text(
            "# Importer split plan\n\n"
            "Planned: split the importer into reader and writer stages.\n")
        # BL-036 Wave B bait: a case directory misplaced in the legacy home
        # — §1's cases row sends it to docs/cases/BL-0007/ (a `move`).
        (dest / "docs/superpowers/BL-0007").mkdir(parents=True)
        (dest / "docs/superpowers/BL-0007/plan.md").write_text(
            "# BL-0007 — invoice numbering\n\n"
            "Tier: 1 (light). Plan: sequential per tenant.\n")

    git(dest, "init", "-q")
    commit_dated(dest, "fixture: rotted-layer at 2026-01-15", "2026-01-15T12:00:00")
    # Defect (check 9): agent-tooling working-dir debris — foreign dump.
    (dest / ".superpowers/sdd").mkdir(parents=True)
    (dest / ".superpowers/sdd/task-9-report.md").write_text(
        "# task 9 report\n\nDone. Diff attached elsewhere.\n")

    # Second commit: code change months after the last journal entry.
    with open(dest / "src/LegacyBilling/Endpoints.cs", "w") as fh:
        fh.write("// new endpoint added long after the journal went quiet\n")
    # Defect (check 16): a spec BORN in the legacy home after legislation.
    (dest / "docs/superpowers/specs").mkdir(parents=True, exist_ok=True)
    (dest / "docs/superpowers/specs/late-feature-spec.md").write_text(
        "# Late feature spec\n\nWritten after legislation, parked in the legacy home.\n")
    commit_dated(dest, "Add endpoints (no journal entry)", "2026-07-01T12:00:00")

    meta = {
        "report_markers": [
            "ghost-rule.md",          # defect 1: broken import
            "overview-draft.md",      # defect 2: unresolved token file
            "docs/ai/rules/core/okf.md",  # defect 3: owned drift
            f"(skill source: v{version}) — behind",  # defect 4: stale manifest (header wording)
            "renamed-away.md",        # defect 5: stale index link
            "legacy/",                # defect 6: stale map row
            "orphan-docs] docs/okf/orphan-notes.md",  # defect 7: orphan doc
            "2026-01-15",             # defect 8: dead journal (entry date cited)
            ".cursorrules",           # defect 9: foreign structure
            "keep-list] docs/notes/special-sauce.md",  # defect 10: kept but referenced nowhere
            "project-rules] .claude/rules/journal.md",  # defect 11: project rule vs owned law
            "stray-rulebooks] docs/superpowers/review-checklist.md",  # defect 12
            "foreign-structures] UBIQUITOUS_LANGUAGE.md",  # defect 14: foreign glossary store
            "glossary-vitality] docs/okf/glossary.md",  # defect 13: empty glossary, src/ exists
            # Defect 15 (sanctioned but uninstalled). Two order-independent
            # markers, not one `<slug>] <name>` string: the law pins the slug
            # and requires the finding to "name the offending path, date, or
            # entry verbatim" — it never pins their ORDER on the line. The
            # single adjacency marker passed only when a model happened to
            # write the name right after the slug; a lawful
            # "[skill-bindings] .claude/rules/skills.md: `made-up-skill`"
            # failed it (found 2026-08-23, v18 benchmark).
            "skill-bindings]",        # defect 15a: reported under its pinned slug
            "made-up-skill",          # defect 15b: the offending entry named verbatim
            ".superpowers/",          # defect (check 9): working-dir debris
            "gone-runbook.md",        # defect (check 10a): dangling keep entry
            "legacy-home-violation] docs/superpowers/specs/late-feature-spec.md",  # defect (check 16)
            "okf-anchors]",                  # defect 16a: pinned slug
            "OldImporter.cs",                # defect 16b: the dead path named
            "ArchivedInvoiceSweeper",        # defect 16c: the dead symbol named
            "okf-sync-debt]",                # defect 17a: pinned slug
            "docs/okf/endpoints.md",         # defect 17b: the document named
            "dry-run mode before a real import",  # harvest: candidate quoted
            "must be reversible",  # harvest: stray-rulebook generic line quoted
            "## Constitution candidates",  # harvest appendix present with pinned heading
        ],
        # Parity (BL-036 Wave B, repaired 2026-08-22): which pinned check
        # slug each planted defect exercises. Measured against the slugs
        # derived from SKILL.md — a check added to the law without a defect
        # here (or a defect for no check) is red at grade time.
        "check_slugs_covered": [
            "imports-resolve",          # ghost-rule.md
            "unresolved-placeholders",  # overview-draft.md
            "owned-integrity",          # drifted docs/ai/rules/core/okf.md
            "staleness",                # manifest one version behind
            "okf-index-links",          # renamed-away.md
            "codebase-map",             # stale legacy/ row
            "orphan-docs",              # orphan-notes.md
            "journal-recency",          # dead since 2026-01-15
            "foreign-structures",       # .cursorrules, .superpowers/, UBIQUITOUS_LANGUAGE.md
            "keep-list",                # special-sauce.md unlinked + gone-runbook.md missing
            "project-rules",            # .claude/rules/journal.md vs owned law
            "stray-rulebooks",          # docs/superpowers/review-checklist.md
            "glossary-vitality",        # empty glossary with src/ present
            "skill-bindings",           # sanctioned but uninstalled made-up-skill
            "legacy-home-violation",    # late-feature-spec.md born into a legacy home
            "okf-anchors",              # importer.md names a dead path and symbol
            "okf-sync-debt",            # endpoints.md's source moved on 167 days later
        ],
        # BL-025 item 2: Critical findings must sit under the Critical
        # severity heading, not merely appear somewhere in the report
        # (severity-anchored presence; section = ## <Severity> ... next ##).
        "severity_anchored_markers": [
            ["ghost-rule.md", "Critical"],
            ["overview-draft.md", "Critical"],
            ["docs/ai/rules/core/okf.md", "Critical"],
        ],
        # BL-011 regression lock: the audit must NOT flag the constitution's
        # hub files as orphans (they are referenced by inline-code mention).
        "absent_markers": [
            "orphan-docs] docs/okf/index.md",
            "orphan-docs] docs/okf/glossary.md",
            # the healthy case file is exempt (docs/cases/** scan-set):
            "BL-0042",
            "stray-rulebooks] docs/cases",
            # not-law suppression: the marked statement must not be proposed
            "Never delete rows from the invoices table",
        ],
        # Scoped to the report's "### Constitution candidates" section only
        # (findings may legitimately name these files/statements):
        "candidate_absent_markers": [
            # project-instance law from the stray rulebook — merges to
            # .claude/rules/, never a fleet candidate
            "never call wkhtmltopdf directly",
            # foreign-glossary lines — check-9 territory is not a harvest
            # source (check-12 precedence): NEITHER line may be proposed
            "billing period",
            "named in past tense",
            # BL-015 rider 1 lock: a statement contradicting an owned rule
            # is covered by that rule — decision-gate material, not a
            # candidate (defect 11's planted line)
            "Dev journal entries are optional",
        ],
        "fixture_commit_count": 2,
        "fixture_head": subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=dest, check=True,
            capture_output=True, text=True).stdout.strip(),
        "expected_manifest_version": version - 1,
        # File authority (BL-038): the two classes only a fixture can name.
        "authority_foreign_structures": [".cursorrules", "UBIQUITOUS_LANGUAGE.md"],
        "authority_relocated_owner_content": [],
        # v20 fix round 1: the two okf-anchors/okf-sync-debt documents are
        # owner prose — the closed `fix` scope forbids restructure from
        # touching them. Recorded here (the exact text written above, not
        # retyped and not read back) so the grader can assert byte-identity
        # rather than a substring match.
        "okf_untouched": {
            "docs/okf/importer.md": importer_md_text,
            "docs/okf/endpoints.md": endpoints_md_text,
        },
    }
    if restructure_extras:
        meta["fidelity_sentences"] = [
            "Planned: split the importer into reader and writer stages.",
            "Always write tests first.",
            "Nobody links to this file.",
            "Ultra-specific invoice rounding rules",
            "We do not maintain CHANGELOG.md",
            "Every database migration must be reversible",
            "never call wkhtmltopdf directly",
            "named in past tense",
            "billing period",
        ]
        meta["foreign_glossary_path"] = "UBIQUITOUS_LANGUAGE.md"
        # case-insensitive: the law says "definitions become glossary rows" —
        # agents lawfully reformat "A billing period …" into
        # "| Billing period | … |" (observed 2026-08-21), so the grader
        # matches the concept, not the casing
        meta["foreign_glossary_definition"] = "billing period"
        meta["foreign_glossary_definition_ci"] = True
        meta["stray_rulebook_path"] = "docs/superpowers/review-checklist.md"
        meta["stray_project_law"] = "never call wkhtmltopdf directly"
        meta["conflict_marker"] = (
            "We do not maintain CHANGELOG.md; release notes are written "
            "in the wiki at release time.")
        meta["kept_path"] = "docs/notes/special-sauce.md"
        meta["kept_content"] = (dest / "docs/notes/special-sauce.md").read_text()
        meta["skills_rules_path"] = ".claude/rules/skills.md"
        meta["skills_rules_content"] = (
            dest / ".claude/rules/skills.md").read_text()
        meta["project_rule_conflict_path"] = ".claude/rules/journal.md"
        meta["project_rule_conflict_content"] = (
            dest / ".claude/rules/journal.md").read_text()
        meta["authority_relocated_owner_content"] = [
            ".claude/plans/2026-01-importer-plan.md",
            "docs/superpowers/BL-0007/plan.md",
            "docs/superpowers/review-checklist.md",
        ]

    (dest.parent / "fixture_meta.json").write_text(
        json.dumps(meta, indent=2) + "\n")


def main() -> None:
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    ws = Path(sys.argv[1]).resolve()
    if ws.exists():
        sys.exit(f"refusing to overwrite existing workspace: {ws}")

    for name in ("fresh-scaffold-dotnet", "legacy-migration",
                 "legacy-migration-agents-first"):
        repo = ws / name / "repo"
        shutil.copytree(EVALS / "fixtures" / name, repo)
        init_commit(repo, f"fixture: {name}")

    repo = ws / "upgrade" / "repo"
    materialize_upgrade(repo)
    init_commit(repo, "fixture: upgrade (one version behind, one retired rule)")

    repo = ws / "upgrade-drop-stack" / "repo"
    materialize_upgrade_drop_stack(repo)
    init_commit(repo, "fixture: upgrade-drop-stack (dotnet+aurelia, dropping aurelia)")

    repo = ws / "case-practice" / "repo"
    materialize_case_practice(repo)
    init_commit(repo, "fixture: case-practice (clean legislated repo)")

    repo = ws / "rotted-layer" / "repo"
    materialize_rotted(repo)

    repo = ws / "restructure" / "repo"
    materialize_rotted(repo, restructure_extras=True)

    # Every fixture gets an `eval-base` tag at its materialized HEAD — the
    # one anchor a full reset can trust. Resetting the working tree alone is
    # not enough (the idempotency stage commits "run 1" into a fixture on
    # purpose, and a later rerun inherited that commit), and resetting to the
    # ROOT commit is wrong for the two-commit fixtures: rotted-layer and
    # restructure plant a dated second commit as defect 8's evidence.
    for repo in sorted(p for p in ws.glob("*/repo") if (p / ".git").exists()):
        git(repo, "tag", "-f", "eval-base")

    print(f"workspace ready: {ws}")
    for p in sorted(ws.glob("*/repo")):
        print(f"  {p}")


if __name__ == "__main__":
    main()
