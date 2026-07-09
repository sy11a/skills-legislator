#!/usr/bin/env python3
"""Materialize eval fixture repos into a workspace directory.

Creates one git-initialized repo per scenario, ready for an agent to run the
legislator skill against:

  <workspace>/fresh-scaffold-dotnet/repo   — new repo, no CLAUDE.md
  <workspace>/legacy-migration/repo        — hand-written CLAUDE.md, no manifest
  <workspace>/upgrade/repo                 — previously legislated, one version behind

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

    print(f"workspace ready: {ws}")
    for p in sorted(ws.glob("*/repo")):
        print(f"  {p}")


if __name__ == "__main__":
    main()
