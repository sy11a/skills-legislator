#!/usr/bin/env python3
"""Materialize eval fixture repos into a workspace directory.

Creates one git-initialized repo per scenario, ready for an agent to run the
legislator skill against:

  <workspace>/fresh-scaffold-dotnet/repo   — new repo, no CLAUDE.md
  <workspace>/legacy-migration/repo        — hand-written CLAUDE.md, no manifest
  <workspace>/upgrade/repo                 — previously legislated, one version behind
  <workspace>/rotted-layer/repo            — legislated, nine planted defects (audit scenario)

The upgrade repo is generated from the CURRENT skill source so this suite
never rots as the constitution evolves: it contains every current core+dotnet
rule EXCEPT the alphabetically last core rule (simulating a rule added to the
constitution since the repo was last legislated), PLUS one retired rule that
no longer exists in the source (so the run must delete it — this exercises
deletion propagation). Its manifest records VERSION-1.

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
    if len(core_src) < 2:
        sys.exit("upgrade fixture needs at least 2 core rules to withhold one")
    withheld = core_src[-1]  # the rule "added since this repo was legislated"

    rules_dst = dest / "docs/ai/rules"
    (rules_dst / "core").mkdir(parents=True)
    (rules_dst / "stacks/dotnet").mkdir(parents=True)

    owned: list[str] = []
    for f in core_src:
        if f.name != withheld.name:
            shutil.copy2(f, rules_dst / "core" / f.name)
            owned.append(f"docs/ai/rules/core/{f.name}")
    for f in dotnet_src:
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
        "retired_rule": RETIRED_RULE,
        "fixture_manifest_version": version - 1,
    }
    (dest.parent / "fixture_meta.json").write_text(json.dumps(meta, indent=2) + "\n")


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


def main() -> None:
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    ws = Path(sys.argv[1]).resolve()
    if ws.exists():
        sys.exit(f"refusing to overwrite existing workspace: {ws}")

    for name in ("fresh-scaffold-dotnet", "legacy-migration"):
        repo = ws / name / "repo"
        shutil.copytree(EVALS / "fixtures" / name, repo)
        init_commit(repo, f"fixture: {name}")

    repo = ws / "upgrade" / "repo"
    materialize_upgrade(repo)
    init_commit(repo, "fixture: upgrade (one version behind, one retired rule)")

    repo = ws / "rotted-layer" / "repo"
    materialize_rotted(repo)

    print(f"workspace ready: {ws}")
    for p in sorted(ws.glob("*/repo")):
        print(f"  {p}")


if __name__ == "__main__":
    main()
