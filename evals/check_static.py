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

print("== the deterministic arm is the binary, and the law says so ==")
# BL-082 T-13 (R-8207, R-8213): parity is reached, so the Python arm leaves the
# package and the law names one command per job. These six assertions are the
# inversion of the thirteen that stood while the engine shipped - they do not
# extend that section, they replace it.
check(not (SKILL / "assets" / "engine" / "engine.py").exists(),
      "no Python engine ships in the package",
      "assets/engine/engine.py is still delivered")
check(not list((REPO / "plugin" / "hooks").glob("*.py")),
      "no Python hooks ship in the plugin",
      f"still there: {sorted(f.name for f in (REPO / 'plugin' / 'hooks').glob('*.py'))}")

rules_text = "\n".join(f.read_text() for f in sorted((SKILL / "assets" / "rules").rglob("*.md")))
check("python3 docs/ai/engine.py" not in rules_text,
      "law names the binary, not the interpreter",
      "a rule file still spells `python3 docs/ai/engine.py`")
check("assets/engine/engine.py" not in skill_md,
      "SKILL.md no longer delivers an engine source",
      "Step 3 still names assets/engine/engine.py")

# The tag-time digest record ships WITH the law rather than being delivered into
# a repo: audit already takes `--skill`, and a fleet member has no
# `evals/benchmarks/` to read (operator ruling 2026-09-04). It carries the
# edition because a bare two-column digest list cannot be pinned to one, and a
# record that outlived its edition would pass in silence.
release = SKILL / "assets" / "release" / "release.json"
check(release.is_file(), "the release record ships with the package",
      f"expected at {release.relative_to(REPO)}")
if release.is_file():
    try:
        record = json.loads(release.read_text())
    except ValueError as exc:
        record = {}
        check(False, "the release record parses as JSON", str(exc))
    edition = (SKILL / "VERSION").read_text().strip()
    check(str(record.get("edition", "")).split(".")[0] == edition,
          "the release record pins this edition",
          f"record says {record.get('edition')!r}, VERSION says {edition!r}")

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

print("== BL-051: the arm's callers state its failure and absence branches ==")
# Checks 15 and 17 are the audit's readers of the deterministic arm. Both read
# stdout lines only, so an arm that crashes (empty stdout, non-zero exit)
# reads to them as "no findings" — the audit fails open on the one instrument
# the verification rung fails closed on. v26 retired the Python engine, so the
# instrument is `legislator <job>`: the obligation moved with it rather than
# expiring, because an agent-performed audit still spawns the arm and an arm
# that is not installed is exactly the absence BL-051 named. Parse each check
# body out of SKILL.md rather than restating its text here (POLICY.md §8).
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
    check("legislator" in body and re.search(r"legislator[^.]{0,120}(absent|missing|not (?:on|available))",
                                             body, re.I | re.S) is not None,
          f"check_{num}_has_absent_arm_branch: check {num} ({slug}) states what it does when the legislator binary is absent",
          "no absent-branch sentence found")
    # Two independent signals rather than one proximity match: the body must
    # talk about the exit code AND declare a bad one not-clean. Requiring them
    # within N characters measured sentence layout, not the obligation.
    names_exit = re.search(r"\bexit(?:s|ed|ing)?\b", body, re.I) is not None
    declares_failure = re.search(
        r"(check failure|never (?:as )?a clean check|not a clean check|never clean)",
        body, re.I) is not None
    check(names_exit and declares_failure,
          f"check_{num}_names_nonzero_exit: check {num} ({slug}) states that an arm exit beyond its findings code is a check failure",
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

print("== BL-082: the .NET substrate's build discipline lives once ==")
SRC = REPO / "src"
props = SRC / "Directory.Build.props"
check(props.exists(), "src/Directory.Build.props exists")
tests_props = REPO / "tests" / "Directory.Build.props"
check(tests_props.exists() and '<Import Project="../src/Directory.Build.props" />' in tests_props.read_text(),
      "tests/Directory.Build.props imports src/Directory.Build.props",
      "MSBuild searches upward from the project dir; tests/ must import, never restate")
if props.exists():
    txt = props.read_text()
    for key, val in [("Nullable", "enable"), ("TreatWarningsAsErrors", "true"),
                     ("EnforceCodeStyleInBuild", "true"),
                     ("AnalysisLevel", "latest-recommended"),
                     ("IsAotCompatible", "true"), ("TargetFramework", "net10.0")]:
        check(f"<{key}>{val}</{key}>" in txt, f"Directory.Build.props sets {key}={val}")
    # IsAotCompatible is deliberately absent from the list below: test projects
    # (JIT) set it to false, the one permitted override.
    for csproj in sorted(list(SRC.rglob("*.csproj")) + list((REPO / "tests").rglob("*.csproj"))):
        body = csproj.read_text()
        for key in ("Nullable", "TreatWarningsAsErrors", "EnforceCodeStyleInBuild",
                    "AnalysisLevel", "TargetFramework"):
            check(f"<{key}>" not in body,
                  f"{csproj.relative_to(REPO)} does not restate {key}",
                  "build discipline is declared once, in Directory.Build.props")

print("== BL-082: no statics in the core (R-8204) ==")
# A static call is the identifier at the start of a member chain, optionally
# qualified by its namespace; `fs.File.Exists` (IFileSystem) is not a static.
FORBIDDEN_STATIC = re.compile(
    r"(?<![\w.])(?:System\.(?:IO\.|Diagnostics\.)?)?"
    r"(?:(?:File|Directory|Environment|Process)\.|Path\.GetFullPath\b|DateTime(?:Offset)?\.(?:Now|UtcNow)\b)")
for proj in ("Legislator.Core", "Legislator.Engine"):
    for cs in sorted((SRC / proj).rglob("*.cs")):
        if "/obj/" in cs.as_posix() or "/bin/" in cs.as_posix():
            continue
        hits = [n for n, line in enumerate(cs.read_text().splitlines(), 1)
                if FORBIDDEN_STATIC.search(line) and not line.strip().startswith("//")]
        check(not hits, f"{cs.relative_to(REPO)} has no static file/clock/env/process call",
              f"lines {hits}")


print("== BL-082: no path/name literal outside the options model (R-8209) ==")
# The v24 engine's constant surface as a tripwire: a quoted path, file name,
# branch/tag shape or version literal anywhere in src/ means a default escaped
# the options model. LegislatorOptions.cs is the one lawful home (C-03).
LITERAL = re.compile(r'"(docs|\.claude|\.config|CLAUDE\.md|AGENTS\.md|opencode\.json|manifest\.json|baseline\.md|glossary\.md|log\.md|CHANGELOG\.md|backlog\.md|bl/|v\d+|\.git\b)[^"]*"')
ALLOWED = {"src/Legislator.Core/Options/LegislatorOptions.cs"}
for cs in sorted(SRC.rglob("*.cs")):
    rel = cs.relative_to(REPO).as_posix()
    if "/obj/" in rel or "/bin/" in rel or rel in ALLOWED:
        continue
    hits = [n for n, line in enumerate(cs.read_text().splitlines(), 1)
            if LITERAL.search(line) and not line.strip().startswith("//")]
    check(not hits, f"{rel} carries no path/name literal",
          f"lines {hits} — add an option instead (C-03)")

print("== BL-082: the release matrix is the only home of three of the four RIDs (R-8203) ==")
# NativeAOT does not cross-compile between operating systems, so `publish-legislator.sh`
# publishes the host's RID and no more. The other three exist only if the matrix builds
# them, which makes this workflow part of the edition rather than incidental config
# (operator ruling 2026-09-04, option a).
RIDS = ("linux-x64", "win-x64", "osx-x64", "osx-arm64")
workflow = REPO / ".github" / "workflows" / "dotnet.yml"
check(workflow.is_file(), "the release workflow exists",
      f"{workflow.relative_to(REPO).as_posix()} is absent - three of the four RIDs have no builder")
if workflow.is_file():
    body = workflow.read_text()
    missing = [rid for rid in RIDS if rid not in body]
    check(not missing, "the release workflow names every released RID",
          f"absent from the matrix: {missing} - a RID nothing builds is a RID the edition cannot release")
    check("-warnaserror" in body, "the release workflow builds strict",
          "the matrix must build under the same discipline as the gate (R-8202)")

print("== BL-082: the .NET gate is invoked by a shell that can run it ==")
# The gate declares `#!/usr/bin/env bash` and uses arrays and `pipefail`.
# Invoking it as `sh <script>` overrides the shebang, and on a runner whose
# /bin/sh is dash it dies at `set -o pipefail` before the first test - which is
# what CI did on every job of the v26 workflow while a developer machine whose
# /bin/sh is bash saw nothing. A gate that cannot start is worse than one that
# fails, so the invocation is checked rather than remembered.
gate_callers = []
for path in sorted(REPO.rglob("*")):
    if not path.is_file() or ".git/" in str(path):
        continue
    if path.suffix not in {".md", ".yml", ".yaml", ".sh"}:
        continue
    rel = path.relative_to(REPO).as_posix()
    # Records say what was true when they were written; they are not callers.
    if rel.startswith(("docs/journal/", "docs/cases/", "evals/benchmarks/")):
        continue
    try:
        text = path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        continue
    for n, line in enumerate(text.splitlines(), 1):
        if re.search(r"(?<![-\w/])sh\s+\S*evals/check_dotnet\.sh", line):
            gate_callers.append(f"{rel}:{n}")
check(not gate_callers,
      "the .NET gate is never invoked through `sh` (it declares bash)",
      f"offenders: {gate_callers}")

print("== BL-082: the edition pins the tool (R-8214, C-12) ==")
# One number, two homes, and neither may move without the other: `skill/VERSION` is the
# edition and `src/Legislator.Cli/Version.props` is what `legislator version` prints back.
# `0.0.0` is the un-assigned pin - lawful only while no edition has been assigned, which
# stops being true the moment this repository carries a VERSION at all.
props = (REPO / "src" / "Legislator.Cli" / "Version.props").read_text()
pin = re.search(r"<Version>([^<]+)</Version>", props)
check(pin is not None, "Version.props declares a <Version>", f"got {props!r}")
if pin:
    major = pin.group(1).split(".")[0]
    check(major == version_text,
          "edition pins the tool major",
          f"skill/VERSION={version_text!r} but Version.props major={major!r} "
          f"(<Version>{pin.group(1)}</Version>) - bump both in one commit")

if failures:
    print(f"\n{len(failures)} check(s) FAILED")
    sys.exit(1)
print("\nall static checks passed")
