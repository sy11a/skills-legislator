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

print("== engine is an owned, delivered artifact ==")
engine_src = SKILL / "assets" / "engine" / "engine.py"
check(engine_src.exists(), "assets/engine/engine.py exists")
if engine_src.exists():
    eng = engine_src.read_text()
    check(eng.startswith("#!/usr/bin/env python3"), "engine has a python3 shebang")
    # v22 adds os + tempfile: the baseline job stages its one write in a
    # sibling temp file and os.replace's it (ADR-0003's atomicity clause).
    STDLIB_OK = {"re", "sys", "subprocess", "pathlib", "datetime",
                 "__future__", "json", "os", "shutil", "tempfile"}
    imported = set(re.findall(r"^\s*(?:from|import)\s+([a-zA-Z_][\w.]*)", eng, re.M))
    check(imported <= STDLIB_OK, "engine imports only stdlib modules",
          f"unexpected: {sorted(imported - STDLIB_OK)}")
    for job in ("anchors", "okf-debt", "sdd-lint", "baseline"):
        check(f'"{job}"' in eng, f"engine declares the {job} job")
check("assets/engine/engine.py" in skill_md,
      "SKILL.md Step 3 names the engine source", "Step 3 does not deliver it")
check("docs/ai/engine.py" in skill_md,
      "SKILL.md names the delivered engine path")

print("== file authority: one table, no prose rights ==")
# BL-038: the `## File authority` table is the only place in the skill
# that states what a mode may do to a file. The grader derives from it;
# this check keeps it the only place. The regex list is deliberately here
# and not in law — a false positive is fixed by narrowing it or rewording
# the prose, both visible in the diff.
AUTH_VALUES = {"replace", "create-if-absent", "lossless-write", "propose-only",
               "move-or-merge", "link-only", "read-only", "never-touch"}
AUTH_MODES = ["scaffold", "migrate", "upgrade", "restructure", "audit"]
AUTH_CLASSES = ["entry document", "owned law", "manifest", "project rules",
                "scaffolded artifacts", "relocated owner content",
                "foreign structures", "kept paths"]
AUTH_PROSE = re.compile(
    r"never (edit|edits|edited|touch|touches|touched|overwrite|overwrites|overwritten)\b"
    r"|is project-owned|project-owned, so|project-owned after creation"
    r"|overwritten on every run|\bcreated?-once\b|create it only if"
    r"|only if it does not already exist|\bcreate-only-if-absent\b", re.I)
AUTH_REF = re.compile(r"\(authority: ([a-z ]+?) × ([a-z]+)(?: = [a-z-]+)?[^)]*\)")

sections = skill_md.split("\n## File authority\n")
check(len(sections) == 2, "SKILL.md has exactly one `## File authority` section",
      f"found {len(sections) - 1}")
auth_body = sections[1].split("\n## ", 1)[0] if len(sections) == 2 else ""
auth_rows = [[c.strip() for c in l.strip().strip("|").split("|")]
             for l in auth_body.splitlines()
             if l.startswith("|") and not re.match(r"^\|[\s:|-]+\|$", l)]
# Exact row count, not a floor: a ninth body row would otherwise be parsed
# past and silently ignored — an unreviewed class with no derived rights.
shape_ok = (len(auth_rows) == 2 + len(AUTH_CLASSES)
            and auth_rows[1][1:] == AUTH_MODES
            and all(r[0].split(" (", 1)[0].strip().lower() in AUTH_CLASSES for r in auth_rows[2:])
            and all(len(r) == 1 + len(AUTH_MODES) and all(c in AUTH_VALUES for c in r[1:]) for r in auth_rows[2:]))
check(shape_ok, "File authority table has the pinned shape (2 header rows, 8 classes × 5 modes, closed vocabulary)",
      "no table" if not auth_rows else f"rows={len(auth_rows)}, modes={auth_rows[1][1:] if len(auth_rows) > 1 else None}")

# The vocabulary delegates by pointer, never by restating a fact that lives
# elsewhere: `never-touch` names no artifact class (the delegated classes
# are read from restructure.md §2's heal bullet, the one place they live),
# and `replace` carries the manifest carve-out — the one `replace` artifact
# the skill generates (keep carried forward) rather than copies.
vocab = {m.group(1): m.group(2) for m in re.finditer(r"^- `([a-z-]+)` — (.*)$", auth_body, re.M)}
nt = vocab.get("never-touch", "")
nt_classes = [c for c in AUTH_CLASSES if c in nt.lower()]
check(bool(nt) and not nt_classes, "never-touch bullet delegates by pointer, naming no artifact class",
      "bullet missing" if not nt else f"names {nt_classes}")
check("`keep`" in vocab.get("replace", ""), "replace bullet carries the manifest carve-out (`keep` carried forward)",
      "no `keep` in the replace bullet")

# Prose scan: SKILL.md line-by-line with the File authority section's line
# range excluded (so reported line numbers are the file's own), plus references/.
auth_start = skill_md[:skill_md.index("\n## File authority\n")].count("\n") + 2
auth_end = auth_start + auth_body.count("\n")
scan_targets = [("SKILL.md", skill_md, range(auth_start, auth_end + 1))]
for ref in sorted((SKILL / "references").glob("*.md")):
    scan_targets.append((f"references/{ref.name}", ref.read_text(), range(0)))
prose_hits = []
for name, text, excluded in scan_targets:
    for i, line in enumerate(text.splitlines(), 1):
        if i not in excluded and AUTH_PROSE.search(line):
            prose_hits.append(f"{name}:{i}")
check(not prose_hits, "no authority-shaped prose outside the File authority table",
      f"{len(prose_hits)} hit(s): {prose_hits}")

bad_refs = []
for name, text, _ in scan_targets:
    for m in AUTH_REF.finditer(text):
        cls, mode = m.group(1).strip(), m.group(2)
        if cls not in AUTH_CLASSES or mode not in AUTH_MODES:
            bad_refs.append(f"{name}: ({cls} × {mode})")
check(not bad_refs, "every (authority: class × mode) reference resolves to a row and a column",
      f"unresolved: {bad_refs}")

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

print("== BL-051: the engine's callers state its failure and absence branches ==")
# Checks 15 and 17 are the audit's readers of docs/ai/engine.py. Both read
# stdout lines only, so an engine that crashes (empty stdout, non-zero exit)
# reads to them as "no findings" — the audit fails open on the one instrument
# the verification rung fails closed on. Parse each check body out of SKILL.md
# rather than restating its text here (POLICY.md §8).
audit_body = skill_md.split("## Audit — read-only health check", 1)[-1]
check_bodies = {}
for num in ("15", "16", "17", "18"):
    m = re.search(rf"^{num}\. \*\*(.+?)(?=^\d+\. \*\*|^Report format)",
                  audit_body, re.M | re.S)
    if m:
        check_bodies[num] = m.group(0)
check(set(check_bodies) >= {"15", "17"},
      "audit checks 15 and 17 are parseable from SKILL.md",
      f"parsed: {sorted(check_bodies)}")

for num, slug in (("15", "okf-anchors"), ("17", "okf-sync-debt")):
    body = check_bodies.get(num, "")
    check("python3" in body and re.search(r"python3[^.]{0,80}(absent|missing|not (?:on|available))",
                                          body, re.I | re.S) is not None,
          f"check_{num}_has_python3_branch: check {num} ({slug}) states what it does when python3 is absent",
          "no absent-branch sentence found")
    # Two independent signals rather than one proximity match: the body must
    # talk about the exit code AND declare a bad one not-clean. Requiring them
    # within N characters measured sentence layout, not the obligation.
    names_exit = re.search(r"\bexit(?:s|ed|ing)?\b", body, re.I) is not None
    declares_failure = re.search(
        r"(check failure|never (?:as )?a clean check|not a clean check|never clean)",
        body, re.I) is not None
    check(names_exit and declares_failure,
          f"check_{num}_names_nonzero_exit: check {num} ({slug}) states that an engine exit beyond its findings code is a check failure",
          f"names_exit={names_exit} declares_failure={declares_failure}")

print("== BL-051: the keep refusal covers the whole owned set ==")
# Since v20 the owned set is docs/ai/rules/**, docs/ai/engine.py and the root
# opencode.json. A refusal phrased as "under docs/ai/rules/" leaves the other
# two keep-listable, putting the kept-paths row (link-only) and the owned-law
# row (replace) of the file-authority table in conflict.
step3_keep = re.search(r"^6\. \*\*Keep list.+?(?=^7\. )", skill_md, re.M | re.S)
report_keep = re.search(r"each refused request with why it was refused \(([^)]*)\)", skill_md)
for label, text in (("step 3.6", step3_keep.group(0) if step3_keep else ""),
                    ("the Step 7 Keep list section", report_keep.group(1) if report_keep else "")):
    check(bool(text), f"keep_refusal_covers_owned_set: {label} is parseable from SKILL.md")
    if text:
        narrow = re.search(r"owned files? under `docs/ai/rules/`", text)
        check(narrow is None,
              f"keep_refusal_covers_owned_set: {label} does not describe the owned set as docs/ai/rules/ alone",
              "found the narrow phrasing — engine.py and opencode.json remain keep-listable")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall static checks passed")
