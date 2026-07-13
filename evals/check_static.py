#!/usr/bin/env python3
"""Static checks on the legislator skill package — the "unit test" layer.

No agent involved; runs in seconds. Verifies internal consistency of the
skill package so that broken references or malformed files are caught on
every commit, before spending any tokens on e2e runs.

Usage: python3 evals/check_static.py
Exit code 0 = all checks pass; 1 = at least one failure (printed).
"""
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SKILL = REPO / "skill"

failures: list[str] = []


def check(ok: bool, label: str, detail: str = "") -> None:
    if ok:
        print(f"  ok    {label}")
    else:
        print(f"  FAIL  {label}" + (f" — {detail}" if detail else ""))
        failures.append(label)


print("== VERSION ==")
version_text = (SKILL / "VERSION").read_text().strip()
check(version_text.isdigit(), "VERSION is a bare integer", f"got {version_text!r}")

print("== SKILL.md structure ==")
skill_md = (SKILL / "SKILL.md").read_text()
check(skill_md.startswith("---\nname: legislator\n"), "frontmatter starts with name: legislator")
check("description:" in skill_md.split("---")[1], "frontmatter has a description")

print("== templates referenced <-> present ==")
templates_dir = SKILL / "assets" / "templates"
present = {p.name for p in templates_dir.glob("*.tpl")}
referenced = set(re.findall(r"`([\w.-]+\.tpl)`", skill_md))
for name in sorted(referenced - present):
    check(False, f"template referenced in SKILL.md exists: {name}", "missing from assets/templates/")
for name in sorted(present - referenced):
    check(False, f"template file is referenced in SKILL.md: {name}", "orphaned in assets/templates/")
if referenced == present:
    check(True, f"all {len(present)} templates referenced and present, no orphans")

print("== template placeholders documented ==")
# Every {{TOKEN}} used by a template must appear in SKILL.md (its derivation
# rules or table notes). adr-template.md.tpl is the documented carve-out: its
# tokens are intentional fill-in-later guidance and are exempt.
documented = set(re.findall(r"\{\{([A-Z_]+)\}\}", skill_md))
for tpl in sorted(templates_dir.glob("*.tpl")):
    if tpl.name == "adr-template.md.tpl":
        continue
    tokens = set(re.findall(r"\{\{([A-Z_]+)\}\}", tpl.read_text()))
    undocumented = tokens - documented
    check(not undocumented, f"{tpl.name} placeholders all documented in SKILL.md",
          f"undocumented: {sorted(undocumented)}")

print("== references/ files exist ==")
for ref in sorted(set(re.findall(r"`references/([\w.-]+)`", skill_md))):
    check((SKILL / "references" / ref).exists(), f"references/{ref} exists")

print("== rule files well-formed ==")
rule_files = sorted((SKILL / "assets" / "rules").rglob("*.md"))
check(len(rule_files) > 0, "at least one rule file exists")
for rf in rule_files:
    rel = rf.relative_to(SKILL / "assets" / "rules")
    text = rf.read_text()
    check(text.startswith("## "), f"{rel} starts with a '## ' heading")
    check(len(text.strip()) > 0, f"{rel} is non-empty")

print("== CLAUDE.md.tpl imports every core rule ==")
tpl_text = (SKILL / "assets" / "templates" / "CLAUDE.md.tpl").read_text()
for rf in sorted((SKILL / "assets" / "rules" / "core").glob("*.md")):
    check(f"@docs/ai/rules/core/{rf.name}" in tpl_text,
          f"CLAUDE.md.tpl imports core/{rf.name}",
          "missing from the tpl core import block")

print("== stack rule-file naming (README content discipline) ==")
allowed = {"architecture.md", "coding-standards.md", "data-access.md"}
for rf in sorted((SKILL / "assets" / "rules" / "stacks").rglob("*.md")):
    check(rf.name in allowed, f"stacks/{rf.parent.name}/{rf.name} uses a concern-based filename",
          f"allowed: {sorted(allowed)}")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall static checks passed")
