#!/usr/bin/env python3
"""Static checks on the legislator skill package — the "unit test" layer.

No agent involved; runs in seconds. Verifies internal consistency of the
skill package so that broken references or malformed files are caught on
every commit, before spending any tokens on e2e runs.

Usage: python3 evals/check_static.py
Exit code 0 = all checks pass; 1 = at least one failure (printed).
"""
import json
import re
import subprocess

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

print("== AGENTS.md.tpl imports every core rule ==")
tpl_text = (SKILL / "assets" / "templates" / "AGENTS.md.tpl").read_text()
for rf in sorted((SKILL / "assets" / "rules" / "core").glob("*.md")):
    check(f"@docs/ai/rules/core/{rf.name}" in tpl_text,
          f"AGENTS.md.tpl imports core/{rf.name}",
          "missing from the tpl core import block")

print("== opencode.json.tpl well-formed owned wiring ==")
oc_text = (SKILL / "assets" / "templates" / "opencode.json.tpl").read_text()
try:
    oc = json.loads(oc_text)
    oc_ok = isinstance(oc, dict) and isinstance(oc.get("instructions"), list) and len(oc["instructions"]) >= 3
except Exception as e:
    oc, oc_ok = None, False
check(oc_ok, "opencode.json.tpl is valid JSON with an instructions array",
      f"instructions={oc.get('instructions') if oc else 'parse error'}")

print("== stack rule-file naming (README content discipline) ==")
allowed = {"architecture.md", "coding-standards.md", "data-access.md"}
for rf in sorted((SKILL / "assets" / "rules" / "stacks").rglob("*.md")):
    check(rf.name in allowed, f"stacks/{rf.parent.name}/{rf.name} uses a concern-based filename",
          f"allowed: {sorted(allowed)}")

print("== philosophy Horizon names only open cases ==")
# The Horizon section states what is designed but not built. An edition that
# closes one of those cases must remove its item in the same cycle, or the
# manifest starts claiming a gap that no longer exists. Mechanical bond
# instead of discipline: the section is checked against the backlog's own
# status lines.
CLOSED_STATUSES = ("DONE", "GREEN", "ABSORBED", "REVISED")
philosophy = (REPO / "docs" / "philosophy.md").read_text()
horizon = re.search(r"^## \d+\. Horizon.*?(?=^## )", philosophy, re.M | re.S)
check(horizon is not None, "philosophy.md has a Horizon section")
if horizon:
    backlog = (REPO / "docs" / "backlog.md").read_text()
    statuses = dict(re.findall(r"^## (BL-\d+).*?\n+\*\*Status:\s*([A-Za-z]+)",
                               backlog, re.M | re.S))
    for case in sorted(set(re.findall(r"BL-\d+", horizon.group(0)))):
        status = statuses.get(case)
        check(status is not None, f"Horizon's {case} exists in the backlog")
        if status is not None:
            check(status.upper() not in CLOSED_STATUSES,
                  f"Horizon's {case} is still open",
                  f"backlog says {status} — the closing edition must drop it from the Horizon")

print("== tracked files carry no local paths or fleet repo names ==")
# Redacting the working tree once is a patch; a check is a wall. Absolute
# home paths are caught generically. Fleet repo names cannot be listed here
# without reintroducing them, so the list is read from the decoding key kept
# OUTSIDE any repository — the check is strongest on the machine that has
# the key and degrades to the path check elsewhere, which is honest.
tracked = subprocess.run(["git", "ls-files"], cwd=REPO,
                         capture_output=True, text=True).stdout.split()
path_re = re.compile(r"/home/[a-z]|/Users/[A-Za-z]")
key = Path.home() / ".claude" / "legislator-fleet-aliases.md"
names = []
if key.exists():
    names = re.findall(r"^\| `[^`]+` \| ([^|]+?)\s*\|", key.read_text(), re.M)
    names = [n.split(" (")[0].strip() for n in names]
name_re = re.compile("|".join(rf"\b{re.escape(n)}\b" for n in names)) if names else None
offenders = []
for rel in tracked:
    f = REPO / rel
    try:
        text = f.read_text(errors="ignore")
    except OSError:
        continue
    for i, line in enumerate(text.splitlines(), 1):
        if "KBO_" in line:
            continue          # an env var name is an integration contract
        if path_re.search(line) or (name_re and name_re.search(line)):
            offenders.append(f"{rel}:{i}")
check(not offenders, "no absolute local paths or fleet repo names in tracked files",
      f"offenders: {offenders[:5]}")
if not names:
    print("  note  fleet-name check skipped — decoding key not on this machine")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall static checks passed")
