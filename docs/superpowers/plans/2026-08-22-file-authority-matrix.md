# File-Authority Matrix (BL-038, edition v18) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace every prose statement of "may this mode write this file?" in the skill with one table in `skill/SKILL.md`, make `evals/grade.py` derive its protected/writable expectations from that table, and fence the table with a static check so prose rights cannot grow back — shipping as edition v18.

**Architecture:** The table (`## File authority`, 8 artifact classes × 5 modes, 8-value closed vocabulary) becomes the law; `grade.py` parses it into `authority_matrix()` and checks every scenario's diff against the cell for its mode; `check_static.py` asserts the table's shape and fails on authority-shaped prose anywhere else in `SKILL.md`/`references/**`. Evals are written and shown RED against the untouched v17 law **before** the table exists (Tasks 1–4), then the law changes (Tasks 5–7), then the full e2e benchmark (Task 8).

**Tech Stack:** Python 3 (stdlib only — `re`, `json`, `subprocess`, `pathlib`), git, the existing eval harness (`evals/setup_workspace.py`, `evals/grade.py`, `evals/check_static.py`, `tools/evals-bg.sh`).

**Spec:** `docs/superpowers/specs/2026-08-22-file-authority-matrix-design.md` — read it first; every task below cites the section it implements.

## Global Constraints

- **Evals first (spec §6, `evals/POLICY.md`):** no file under `skill/` changes until Tasks 1–4 are committed with their RED output recorded. A new assert that is green before the law change is measuring nothing.
- **Branch:** all work on `feature/v18-file-authority` (already holds the spec). Never commit to master; never merge yourself.
- **No AI attribution in commits** — no `Co-Authored-By` trailers, no generated-with footers.
- **Table bytes are pinned (spec §3):** heading exactly `## File authority`; header row 1 = `| artifact class | installing | installing | maintaining | maintaining | inspecting |`; header row 2 = `| | scaffold | migrate | upgrade | restructure | audit |`; 8 body rows; every cell one of `replace`, `create-if-absent`, `lossless-write`, `propose-only`, `move-or-merge`, `link-only`, `read-only`, `never-touch`.
- **Cell-reference form (spec §5):** the only sanctioned way to mention a right in prose is `(authority: <class> × <mode>)` — the `×` is U+00D7, the class name is the row label before its parenthesis, lowercase.
- **VERSION 17 → 18** in the same commit as the first `skill/assets/rules/**` or behavioral SKILL.md change (Task 5).
- **Fleet repo names and absolute local paths never enter tracked files** (`check_static.py` enforces; use `<ws>` in docs).
- **Every commit:** `python3 evals/check_static.py` must pass — except Tasks 3–4, where the new check is *expected* red against v17 law and the commit message says so.
- **Python style:** match `grade.py` — module-level helper functions in the contract-derivation block, `g.check(name, passed, evidence)` asserts, evidence strings that name the offending path.

---

## File structure

| File | Responsibility in this plan |
|---|---|
| `evals/grade.py` | **Modify.** Contract-derivation block (after `scaffold_artifacts`, before `expected_stacks`): add `AUTHORITY_VALUES`, `authority_matrix()`, `authority_states()`, `class_paths()`, `check_mode_authority()`; rewrite `protected_project_files()`; wire `check_mode_authority` into seven graders; replace one selftest assert, add three. |
| `evals/setup_workspace.py` | **Modify.** Restructure and audit fixture metas gain two lists: `authority_foreign_structures`, `authority_relocated_owner_content` (spec §4 `class_paths`). |
| `evals/check_static.py` | **Modify.** New section `== file authority: one table, no prose rights ==` (spec §5). |
| `skill/SKILL.md` | **Modify.** New `## File authority` section after Step 3; 13 prose phrases become cell references or are deleted (spec §7); BL-028/030 residue. |
| `skill/references/migration.md` | **Modify.** Line 27 parenthesis deleted; "stack profile" → "stack". |
| `skill/references/restructure.md` | **Modify.** §2 heal/link bullets and §5 gain cell references; line 17 "canonical constitution" → "canonical entry document". |
| `skill/VERSION` | **Modify.** 17 → 18. |
| `evals/benchmarks/v18.md` | **Create.** Red baseline (Tasks 1–4 output), green corpus, idempotency, model floor, comparison to v17. |
| `docs/backlog.md` | **Modify.** BL-038 status; BL-028/030 statuses corrected; BL-031 re-homed. |
| `docs/philosophy.md` | **Modify.** § Horizon drops BL-038 (the static check fails otherwise). |
| `docs/glossary.md` | **Modify.** Row `file authority` (coin). |

---

### Task 1: Parse the matrix — `authority_matrix()` and the selftest shape asserts (RED)

Implements spec §4 (parser) and the first two selftest asserts. Against v17 law there is no `## File authority` section, so the parser must raise and the selftest must go red with that exact reason.

**Files:**
- Modify: `evals/grade.py:72` (after `SCAFFOLD_ARTIFACTS = scaffold_artifacts()`)
- Modify: `evals/grade.py:785-827` (`grade_derivation_selftest`)

**Interfaces:**
- Produces: `AUTHORITY_VALUES: frozenset[str]`; `AUTHORITY_MODES: tuple[str, ...] = ("scaffold", "migrate", "upgrade", "restructure", "audit")`; `authority_matrix() -> dict[tuple[str, str], str]` keyed `(class, mode)` with class = row label before the first ` (`, lowercased and stripped; `authority_states() -> dict[str, str]` mode → state; both raise `ValueError` with a message starting `File authority:` on any shape defect.

- [ ] **Step 1: Add the parser to the contract-derivation block**

Insert directly after line 72 (`SCAFFOLD_ARTIFACTS = scaffold_artifacts()`):

```python
# File authority (BL-038, edition v18): the ONE table in SKILL.md that
# states what each invocation mode may do to each artifact class. The
# grader derives its protected/writable expectations from it and never
# restates a right by hand. A malformed table raises — grading leniently
# against a broken matrix would be the v17 incident in reverse.
AUTHORITY_VALUES = frozenset({
    "replace", "create-if-absent", "lossless-write", "propose-only",
    "move-or-merge", "link-only", "read-only", "never-touch",
})
AUTHORITY_MODES = ("scaffold", "migrate", "upgrade", "restructure", "audit")
AUTHORITY_CLASSES = (
    "entry document", "owned law", "manifest", "project rules",
    "scaffolded artifacts", "relocated owner content",
    "foreign structures", "kept paths",
)


def _authority_rows() -> list[list[str]]:
    """The pipe-table rows of SKILL.md's `## File authority` section, each
    as a list of stripped cells (outer pipes removed)."""
    text = _skill_md()
    if "\n## File authority\n" not in text:
        raise ValueError("File authority: no `## File authority` section in SKILL.md")
    section = text.split("\n## File authority\n", 1)[1].split("\n## ", 1)[0]
    rows = []
    for line in section.splitlines():
        if line.startswith("|") and not re.match(r"^\|[\s:|-]+\|$", line):
            rows.append([c.strip() for c in line.strip().strip("|").split("|")])
    return rows


def authority_matrix() -> dict[tuple[str, str], str]:
    """(class, mode) -> right, parsed from the pinned two-header table."""
    rows = _authority_rows()
    if len(rows) < 2 + len(AUTHORITY_CLASSES):
        raise ValueError(f"File authority: expected 2 header rows + {len(AUTHORITY_CLASSES)} body rows, got {len(rows)}")
    modes = tuple(c for c in rows[1][1:])
    if modes != AUTHORITY_MODES:
        raise ValueError(f"File authority: mode row must be {AUTHORITY_MODES}, got {modes}")
    out: dict[tuple[str, str], str] = {}
    for row in rows[2:2 + len(AUTHORITY_CLASSES)]:
        cls = row[0].split(" (", 1)[0].strip().lower()
        if cls not in AUTHORITY_CLASSES:
            raise ValueError(f"File authority: unknown artifact class {cls!r}")
        cells = row[1:1 + len(AUTHORITY_MODES)]
        for mode, cell in zip(AUTHORITY_MODES, cells):
            if cell not in AUTHORITY_VALUES:
                raise ValueError(f"File authority: cell ({cls} × {mode}) = {cell!r} is not one of {sorted(AUTHORITY_VALUES)}")
            out[(cls, mode)] = cell
    missing = [c for c in AUTHORITY_CLASSES if (c, "scaffold") not in out]
    if missing:
        raise ValueError(f"File authority: rows missing for {missing}")
    return out


def authority_states() -> dict[str, str]:
    """mode -> state (installing / maintaining / inspecting), read from the
    state header row directly above each mode."""
    rows = _authority_rows()
    states = rows[0][1:1 + len(AUTHORITY_MODES)]
    return dict(zip(AUTHORITY_MODES, states))
```

- [ ] **Step 2: Add the two shape asserts to `grade_derivation_selftest`**

Insert after the `scaffold_artifacts_include_cases_home` check (line ~793):

```python
    # File authority (BL-038): the table parses to the pinned shape, and
    # the state header says which repo state each mode assumes.
    try:
        matrix = authority_matrix()
        states = authority_states()
        shape_err = ""
    except ValueError as e:
        matrix, states, shape_err = {}, {}, str(e)
    g.check("authority_matrix_shape",
            not shape_err and len(matrix) == len(AUTHORITY_CLASSES) * len(AUTHORITY_MODES),
            f"{len(matrix)} cells, all in the closed vocabulary" if not shape_err else shape_err)
    g.check("authority_states_pinned",
            states == {"scaffold": "installing", "migrate": "installing",
                       "upgrade": "maintaining", "restructure": "maintaining",
                       "audit": "inspecting"},
            f"states: {states}" if states else shape_err)
```

- [ ] **Step 3: Run the selftest and confirm it is RED for the right reason**

Run: `python3 evals/grade.py /tmp/legislator-selftest-scratch selftest:derivation` (the directory is created by the grader for its `outputs/`; it is outside the repo).

Expected output contains:
```
  FAIL  authority_matrix_shape — File authority: no `## File authority` section in SKILL.md
  FAIL  authority_states_pinned — File authority: no `## File authority` section in SKILL.md
```
and every pre-existing selftest assert still `ok`. If `authority_matrix_shape` is green, stop: the table does not exist yet, so a green here means the parser is reading something else.

- [ ] **Step 4: Record the red output**

Create `evals/benchmarks/v18.md` with this header and paste the two FAIL lines verbatim:

```markdown
# Eval results — v18 (2026-08-22, constitution VERSION 17 → 18)

Change under test: **edition v18 — rights and names.** The file-authority
matrix (BL-038): one `## File authority` table in SKILL.md replaces every
prose statement of a mode's file rights; `grade.py` derives its protected
set and a per-scenario `mode_respects_authority` assert from it;
`check_static.py` fences the table. Riders: BL-028/BL-030 prose residue.

## Red baseline — new asserts against the unchanged v17 law

Per `evals/POLICY.md` §4: every assert below was written first and shown
failing before any file under `skill/` changed.

### selftest:derivation (Task 1)

<paste the two FAIL lines>
```

- [ ] **Step 5: Commit**

```bash
git add evals/grade.py evals/benchmarks/v18.md
git commit -m "evals: parse the file-authority matrix; selftest red against v17 (no table yet)"
```

---

### Task 2: Derive the protected set from cells — `class_paths()`, rewritten `protected_project_files()`, the two-direction selftest (RED)

Implements spec §4 (`class_paths`, `protected_project_files`, `protected_set_derived_from_cells`). Against v17 law, the rewritten `protected_project_files()` must raise (no table), so `grade_upgrade`'s existing `project_owned_files_untouched` assert cannot be computed — it goes red with the parser's message, and the new selftest assert goes red the same way.

**Files:**
- Modify: `evals/grade.py` — replace `protected_project_files()` (lines 75–81); add `class_paths()` after `authority_states()`; update the call site in `grade_upgrade` (line ~486); replace `protected_excludes_entry_document_pair` in the selftest.
- Modify: `evals/setup_workspace.py` — add the two authority lists to the restructure meta (after `meta["project_rule_conflict_content"]`, line ~556) and to the rotted/audit meta (the `meta = {...}` literal at line ~519).

**Interfaces:**
- Consumes: `authority_matrix()`, `AUTHORITY_CLASSES`, `SCAFFOLD_ARTIFACTS`, `expected_owned()` (existing, line 183).
- Produces: `class_paths(repo: Path, cls: str, fixture_meta: dict | None) -> list[str]` (repo-relative, sorted, existence NOT required — the caller decides); `protected_project_files(repo: Path, fixture_meta: dict | None = None, matrix: dict | None = None) -> list[str]` — paths tracked at HEAD whose `upgrade` cell ∈ {`create-if-absent`, `propose-only`, `read-only`, `link-only`, `never-touch`}; the optional `matrix` parameter exists so the selftest can pass a patched copy.
- Fixture meta keys (both rotted and restructure): `"authority_foreign_structures": [".cursorrules", "UBIQUITOUS_LANGUAGE.md"]`, `"authority_relocated_owner_content": [".claude/plans/2026-01-importer-plan.md", "docs/superpowers/BL-0007/plan.md", "docs/superpowers/review-checklist.md"]` (restructure only — the rotted/audit fixture lists `[]` for relocated content because audit never writes).

- [ ] **Step 1: Add `class_paths()` after `authority_states()`**

```python
def class_paths(repo: Path, cls: str, fixture_meta: dict | None = None) -> list[str]:
    """Concrete repo-relative paths of one artifact class. Skill-derived
    where the skill defines the class; fixture-declared for the two
    classes only a fixture can know (what is foreign, what was relocated)."""
    meta = fixture_meta or {}
    if cls == "entry document":
        return ["AGENTS.md", "CLAUDE.md"]
    if cls == "owned law":
        return sorted(expected_owned())
    if cls == "manifest":
        return ["docs/ai/manifest.json"]
    if cls == "project rules":
        tracked = git(repo, "ls-files", ".claude/rules").split()
        return sorted(set(tracked) | {p for p in SCAFFOLD_ARTIFACTS if p.startswith(".claude/rules/")})
    if cls == "scaffolded artifacts":
        return [p for p in SCAFFOLD_ARTIFACTS if not p.startswith(".claude/rules/")
                and p not in ("AGENTS.md", "CLAUDE.md")]
    if cls == "relocated owner content":
        return sorted(meta.get("authority_relocated_owner_content", []))
    if cls == "foreign structures":
        return sorted(meta.get("authority_foreign_structures", []))
    if cls == "kept paths":
        return sorted(k["path"] for k in meta.get("expected_keep", []))
    raise ValueError(f"File authority: unknown class {cls!r}")
```

Note the two exclusions inside `scaffolded artifacts`: the entry document and project rules are their own rows, so Step 4's table entries for `AGENTS.md`/`CLAUDE.md`/`.claude/rules/skills.md` are classified by their own rows, not twice.

- [ ] **Step 2: Replace `protected_project_files()` (lines 75–81) entirely**

```python
PROTECTING_RIGHTS = frozenset({"create-if-absent", "propose-only", "read-only",
                               "link-only", "never-touch"})


def protected_project_files(repo: Path, fixture_meta: dict | None = None,
                            matrix: dict | None = None) -> list[str]:
    """Tracked files an upgrade run must leave byte-unchanged, derived from
    the matrix: every path of every class whose `upgrade` cell is a
    protecting right, restricted to what existed at HEAD. No hand-written
    exclusions — AGENTS.md drops out because its cell says propose-only,
    not because someone listed it."""
    m = matrix if matrix is not None else authority_matrix()
    tracked = set(git(repo, "ls-files").split())
    out: set[str] = set()
    for cls in AUTHORITY_CLASSES:
        if m[(cls, "upgrade")] in PROTECTING_RIGHTS:
            out |= {p for p in class_paths(repo, cls, fixture_meta) if p in tracked}
    return sorted(out)
```

- [ ] **Step 3: Update the call site in `grade_upgrade`**

Replace lines ~481–489 (the comment block + `protected = protected_project_files()` + the check) with:

```python
    # Project-owned files must be untouched — the set is derived from the
    # file-authority matrix (BL-038): every class whose upgrade cell is a
    # protecting right. AGENTS.md is absent because its cell is
    # propose-only, not because it is listed here.
    try:
        protected = protected_project_files(repo, fixture_meta=meta)
        touched = [p for p in git(repo, "diff", "HEAD", "--name-only").splitlines() if p in protected]
        g.check("project_owned_files_untouched", not touched,
                "no tracked project-owned file modified" if not touched else f"modified: {touched}")
    except ValueError as e:
        g.check("project_owned_files_untouched", False, str(e))
```

- [ ] **Step 4: Replace the selftest assert `protected_excludes_entry_document_pair`**

Replace lines ~795–798 with the block below. It needs a git-tracked repo to derive from (the function filters by `git ls-files`), so it copies the `upgrade-base` fixture into a temporary directory and stages it — `git ls-files` lists staged files, no commit needed.

```python
    # Two directions: AGENTS.md is out of the protected set BECAUSE its
    # cell is propose-only — flip the cell to read-only on a patched copy
    # of the matrix and AGENTS.md must come back in. If either direction
    # fails, the set is not being derived from the table.
    if matrix:
        flipped = dict(matrix)
        flipped[("entry document", "upgrade")] = "read-only"
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td) / "repo"
            shutil.copytree(EVALS / "fixtures" / "upgrade-base", scratch)
            (scratch / "AGENTS.md").write_text("# stub\n")
            subprocess.run(["git", "-C", str(scratch), "init", "-q"], check=True)
            subprocess.run(["git", "-C", str(scratch), "add", "-A"], check=True)
            real = protected_project_files(scratch, None, matrix)
            patched = protected_project_files(scratch, None, flipped)
        derived = ("AGENTS.md" not in real
                   and matrix[("entry document", "upgrade")] == "propose-only"
                   and "AGENTS.md" in patched)
        g.check("protected_set_derived_from_cells", derived,
                f"real={'AGENTS.md' in real}, flipped={'AGENTS.md' in patched}")
    else:
        g.check("protected_set_derived_from_cells", False, shape_err)
```

Add `import shutil` and `import tempfile` to the module imports at the top of `grade.py` (alphabetical, after `import re` / before `import subprocess` for `shutil`; after `import subprocess` for `tempfile`). `matrix` and `shape_err` are the variables Task 1 Step 2 introduced a few lines above in the same function.

- [ ] **Step 5: Add the authority lists to both fixture metas in `setup_workspace.py`**

In the rotted/audit `meta = {` literal (line ~519), add after `"expected_manifest_version": version - 1,`:

```python
        # File authority (BL-038): the two classes only a fixture can name.
        "authority_foreign_structures": [".cursorrules", "UBIQUITOUS_LANGUAGE.md"],
        "authority_relocated_owner_content": [],
```

In the `if restructure_extras:` block, after `meta["project_rule_conflict_content"] = (...)`:

```python
        meta["authority_relocated_owner_content"] = [
            ".claude/plans/2026-01-importer-plan.md",
            "docs/superpowers/BL-0007/plan.md",
            "docs/superpowers/review-checklist.md",
        ]
```

- [ ] **Step 6: Run the selftest — RED**

Run: `python3 evals/grade.py /tmp/legislator-selftest-scratch selftest:derivation`

Expected: `FAIL  protected_set_derived_from_cells — File authority: no `## File authority` section in SKILL.md`; the old `protected_excludes_entry_document_pair` line is gone.

- [ ] **Step 7: Run the upgrade grader on the v17 workspace — RED**

If `<ws>` from the v17 benchmark is gone, materialize: `python3 evals/setup_workspace.py /tmp/legislator-eval-v18-red`. The upgrade scenario needs no agent run for this check to be red — the assert fails on the parser before looking at the diff.

Run: `python3 evals/grade.py /tmp/legislator-eval-v18-red upgrade`

Expected: `FAIL  project_owned_files_untouched — File authority: no `## File authority` section in SKILL.md` (other asserts fail too because no agent ran — that is noise here; only this line is the evidence).

- [ ] **Step 8: Append both FAIL lines to `evals/benchmarks/v18.md` under a `### protected set (Task 2)` heading; commit**

```bash
git add evals/grade.py evals/setup_workspace.py evals/benchmarks/v18.md
git commit -m "evals: derive the protected set from matrix cells; two-direction selftest; red against v17"
```

---

### Task 3: The generic authority assert — `check_mode_authority()` in seven graders (RED)

Implements spec §4 `mode_respects_authority`. One function, one assert name, wired into every scenario grader that has a repo diff to judge.

**Files:**
- Modify: `evals/grade.py` — add `check_mode_authority()` after `protected_project_files()`; call it in `grade_fresh`, `grade_migration`, `grade_migration_agents_first`, `grade_upgrade`, `grade_upgrade_drop_stack`, `grade_restructure`, `grade_audit`.

**Interfaces:**
- Consumes: `authority_matrix()`, `class_paths()`, `git()`.
- Produces: `check_mode_authority(g: Grader, repo: Path, mode: str, fixture_meta: dict | None = None) -> None` — adds exactly one assert named `mode_respects_authority`.

- [ ] **Step 1: Add the function**

```python
def check_mode_authority(g: "Grader", repo: Path, mode: str,
                         fixture_meta: dict | None = None) -> None:
    """One assert per scenario: the run's tracked-file diff, restricted to
    each artifact class, satisfies that class's cell for this mode.
    Content-level proof for lossless-write / move-or-merge stays with the
    scenario's fidelity asserts; this checks the SHAPE of the diff.
      replace, lossless-write, move-or-merge -> any change
      create-if-absent                       -> additions only
      propose-only, read-only, never-touch   -> no change
      link-only                              -> no change to the path itself"""
    try:
        m = authority_matrix()
    except ValueError as e:
        g.check("mode_respects_authority", False, str(e))
        return
    status = {}
    for line in git(repo, "status", "--porcelain", "--untracked-files=all").splitlines():
        code, path = line[:2].strip(), line[3:]
        if " -> " in path:                      # rename: both sides count
            old, new = path.split(" -> ", 1)
            status[old] = "D"; status[new] = "A"
        else:
            status[path] = "A" if code in ("??", "A") else ("D" if code == "D" else "M")
    violations = []
    for cls in AUTHORITY_CLASSES:
        right = m[(cls, mode)]
        for p in class_paths(repo, cls, fixture_meta):
            change = status.get(p)
            if change is None:
                continue
            if right in ("replace", "lossless-write", "move-or-merge"):
                continue
            if right == "create-if-absent" and change == "A":
                continue
            violations.append(f"{cls} × {mode} = {right}, but {p} {change}")
    g.check("mode_respects_authority", not violations,
            f"diff shape lawful for all {len(AUTHORITY_CLASSES)} classes in {mode} mode"
            if not violations else "; ".join(violations[:4]))
```

Path subtlety handled here: `class_paths` returns directory-less paths for classes like `project rules` (`git ls-files .claude/rules`), so the status lookup is exact-path. For `owned law`, upgrade's `replace` covers deletion of retired rules (`D`) — lawful by the vocabulary.

- [ ] **Step 2: Wire it into the seven graders**

Add one line to each, right after `g.common_checks(...)` (or after `g = Grader()` where there is no `common_checks`):

```python
    check_mode_authority(g, repo, "scaffold")                       # grade_fresh
    check_mode_authority(g, repo, "migrate")                        # grade_migration
    check_mode_authority(g, repo, "migrate")                        # grade_migration_agents_first
    check_mode_authority(g, repo, "upgrade", meta)                  # grade_upgrade
    check_mode_authority(g, repo, "upgrade", meta)                  # grade_upgrade_drop_stack
    check_mode_authority(g, repo, "restructure", meta)              # grade_restructure
    check_mode_authority(g, repo, "audit", meta)                    # grade_audit
```

- [ ] **Step 3: Grade the red workspace — every wired scenario RED on the same line**

Run: `python3 evals/grade.py /tmp/legislator-eval-v18-red fresh-scaffold-dotnet legacy-migration legacy-migration-agents-first upgrade upgrade-drop-stack restructure audit 2>&1 | grep mode_respects_authority`

Expected: seven lines, each `FAIL  mode_respects_authority — File authority: no `## File authority` section in SKILL.md`.

- [ ] **Step 4: Record under `### mode_respects_authority (Task 3)` in `evals/benchmarks/v18.md`; commit**

```bash
git add evals/grade.py evals/benchmarks/v18.md
git commit -m "evals: mode_respects_authority in seven graders; red against v17"
```

---

### Task 4: The wall — `check_static.py` section (RED with the 14-hit worklist)

Implements spec §5. Three checks; against v17 law the first fails (no section) and the second fails with 14 hits — that hit list is Task 6's worklist and is recorded verbatim.

**Files:**
- Modify: `evals/check_static.py` — insert before `print("== tracked files carry no local paths or fleet repo names ==")`.

**Interfaces:**
- Consumes: nothing from `grade.py` (the static check is self-contained by design — it runs where grade.py's workspace does not exist).
- Produces: three check labels — `SKILL.md has one File authority table of pinned shape`, `no authority-shaped prose outside the File authority table`, `every (authority: class × mode) reference resolves`.

- [ ] **Step 1: Add the section**

```python
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
    r"|only if it does not already exist", re.I)
AUTH_REF = re.compile(r"\(authority: ([a-z ]+?) × ([a-z]+)(?: = [a-z-]+)?[^)]*\)")

sections = skill_md.split("\n## File authority\n")
check(len(sections) == 2, "SKILL.md has exactly one `## File authority` section",
      f"found {len(sections) - 1}")
auth_body = sections[1].split("\n## ", 1)[0] if len(sections) == 2 else ""
auth_rows = [[c.strip() for c in l.strip().strip("|").split("|")]
             for l in auth_body.splitlines()
             if l.startswith("|") and not re.match(r"^\|[\s:|-]+\|$", l)]
shape_ok = (len(auth_rows) >= 2 + len(AUTH_CLASSES)
            and auth_rows[1][1:] == AUTH_MODES
            and all(r[0].split(" (", 1)[0].strip().lower() in AUTH_CLASSES for r in auth_rows[2:2 + len(AUTH_CLASSES)])
            and all(c in AUTH_VALUES for r in auth_rows[2:2 + len(AUTH_CLASSES)] for c in r[1:1 + len(AUTH_MODES)]))
check(shape_ok, "File authority table has the pinned shape (2 header rows, 8 classes × 5 modes, closed vocabulary)",
      "no table" if not auth_rows else f"rows={len(auth_rows)}, modes={auth_rows[1][1:] if len(auth_rows) > 1 else None}")

# Prose scan: SKILL.md minus the File authority section, plus references/.
scan_targets = [("SKILL.md", sections[0] + ("\n## " + sections[1].split("\n## ", 1)[1] if len(sections) == 2 and "\n## " in sections[1] else ""))]
for ref in sorted((SKILL / "references").glob("*.md")):
    scan_targets.append((f"references/{ref.name}", ref.read_text()))
prose_hits = []
for name, text in scan_targets:
    for i, line in enumerate(text.splitlines(), 1):
        if AUTH_PROSE.search(line):
            prose_hits.append(f"{name}:{i}")
check(not prose_hits, "no authority-shaped prose outside the File authority table",
      f"{len(prose_hits)} hit(s): {prose_hits}")

bad_refs = []
for name, text in scan_targets:
    for m in AUTH_REF.finditer(text):
        cls, mode = m.group(1).strip(), m.group(2)
        if cls not in AUTH_CLASSES or mode not in AUTH_MODES:
            bad_refs.append(f"{name}: ({cls} × {mode})")
check(not bad_refs, "every (authority: class × mode) reference resolves to a row and a column",
      f"unresolved: {bad_refs}")
```

Line-number subtlety: `sections[0]` is SKILL.md up to the section, so its line numbers are real; the tail after the section is appended with one heading line, so tail line numbers are offset — acceptable for a worklist (the hit text is greppable), and once the section exists the offset is a constant. Do not spend time making them exact.

- [ ] **Step 2: Run — RED with the worklist**

Run: `python3 evals/check_static.py | sed -n '/file authority/,/tracked files/p'`

Expected:
```
  FAIL  SKILL.md has exactly one `## File authority` section — found 0
  FAIL  File authority table has the pinned shape ... — no table
  FAIL  no authority-shaped prose outside the File authority table — 14 hit(s): ['SKILL.md:42', 'SKILL.md:58', 'SKILL.md:64', 'SKILL.md:79', 'SKILL.md:81', 'SKILL.md:95', 'SKILL.md:120', ... 'references/migration.md:27']
  ok    every (authority: class × mode) reference resolves ...
```
The count may differ from 14 by a few (the regex is line-based and line 58 alone has three phrases; spec §1 counted phrases, the check counts lines). **Record the actual list.** For each hit, open the line and classify it: *authority statement* (→ Task 6 worklist) or *false positive* (→ narrow the regex now, in this task, and say which alternation was narrowed and why in the commit message). A plausible false positive: `never overwrite the loser` in Step 5's decision-gate sentence (line ~103) — that is a decision-gate rule, not a file right; if it hits, exclude it by rewording in Task 6 to "never silently pick the loser" rather than by weakening the regex, because "never overwrite" is exactly the phrase the wall must catch.

- [ ] **Step 3: Record the hit list verbatim in `evals/benchmarks/v18.md` under `### check_static wall (Task 4) — migration worklist`; commit**

```bash
git add evals/check_static.py evals/benchmarks/v18.md
git commit -m "evals: static wall for the file-authority table; red against v17 with the prose worklist"
```

(`check_static.py` is expected red on this commit — the message says so; the pre-commit expectation in CLAUDE.md is satisfied by Task 7.)

---

### Task 5: The table — `## File authority` in SKILL.md, VERSION 18

Implements spec §3. Pure addition — the prose migration is Task 6 so that this commit's diff is the table alone and reviewable as such.

**Files:**
- Modify: `skill/SKILL.md` — insert the section after Step 3's last numbered item and before `## Step 4`; the **Entry-document authority** paragraph (line 58) stays in place until Task 6.
- Modify: `skill/VERSION` — `17` → `18`.

- [ ] **Step 1: Insert the section**

Place this block immediately before the line `## Step 4 — Scaffold missing project-owned artifacts`:

````markdown
## File authority

This table is the only statement of what an invocation mode may do to a file in the target repo. Every other mention of a right in this skill is a reference to a cell, written `(authority: <class> × <mode>)`; a right stated anywhere else in prose is a defect the static check rejects.

| artifact class | installing | installing | maintaining | maintaining | inspecting |
| | scaffold | migrate | upgrade | restructure | audit |
|---|---|---|---|---|---|
| entry document (`AGENTS.md`; `CLAUDE.md` is its symlink) | replace | lossless-write | propose-only | lossless-write | read-only |
| owned law (`docs/ai/rules/**`, `opencode.json`) | replace | replace | replace | never-touch | read-only |
| manifest (`docs/ai/manifest.json`) | replace | replace | replace | never-touch | read-only |
| project rules (`.claude/rules/**`) | create-if-absent | lossless-write | create-if-absent | move-or-merge | read-only |
| scaffolded artifacts (Step 4's table, the OKF bundle included) | create-if-absent | create-if-absent | create-if-absent | move-or-merge | read-only |
| relocated owner content (glossary rows, the OKF mapping table, legacy plans/specs, `BL-NNN` directories) | read-only | lossless-write | read-only | move-or-merge | read-only |
| foreign structures (`.cursorrules`, stray rulebooks, non-standard AI dirs) | read-only | read-only | read-only | move-or-merge | read-only |
| kept paths (manifest `keep`) | link-only | link-only | link-only | link-only | read-only |

**State rule.** `docs/ai/manifest.json` is the boundary: absent, the layer is being *installed*; present, it is being *maintained* or *inspected*. A mode's column is fixed; its state header names the repo state the mode assumes. Harvest is a report section of migrate, upgrade and audit and writes nothing; steward is a human duty performed on the skill's own repository. Neither acts on a legislated repo, so neither has a column.

**Vocabulary (closed).**

- `replace` — the content comes whole from the skill; whatever exists is replaced byte-for-byte (Bash `cp`), never merged, never edited.
- `create-if-absent` — created from a template when missing; an existing file is left as it is, whatever its content.
- `lossless-write` — the run writes owner content (into the file, or out of it into its home) such that every sentence survives; the fidelity pass is the proof. Removing machine wiring that points at nothing (a dangling `@import`, a stale map row) is inside this right — such a line is not owner content.
- `propose-only` — not written; exact lines are printed under `## Needs your review`, and the owner applies them.
- `move-or-merge` — relocated or folded whole under an approved plan item (`references/restructure.md` §2); content is carried, not edited.
- `link-only` — a link *to* the path may be added elsewhere; the path itself is not moved, merged, fixed, or rewritten.
- `read-only` — read to judge and report; zero writes.
- `never-touch` — outside the mode's jurisdiction: not written, not proposed about; another mode owns the repair (drifted owned law is healed by running the upgrade column, not by restructure's own hands).
````

The vocabulary bullets use phrases the wall's regex would catch ("never edited"). That is fine by construction: Task 4's scan excludes the `## File authority` section — inside it the words are the law, outside it they are the defect.

- [ ] **Step 2: Bump VERSION**

```bash
printf '18\n' > skill/VERSION
```

- [ ] **Step 3: Run the static check — the two table checks go green, the prose check stays red**

Run: `python3 evals/check_static.py | sed -n '/file authority/,/tracked files/p'`

Expected: first two `ok`, the prose check still `FAIL` with the Task 4 list (now shifted by the inserted lines — fine), references `ok`.

- [ ] **Step 4: Run the selftest — the three Task 1–2 asserts go green**

Run: `python3 evals/grade.py /tmp/legislator-selftest-scratch selftest:derivation`

Expected: `ok    authority_matrix_shape — 40 cells, all in the closed vocabulary`, `ok    authority_states_pinned`, `ok    protected_set_derived_from_cells — real=False, flipped=True`. This is the green half of Tasks 1–2's red→green.

- [ ] **Step 5: Commit**

```bash
git add skill/SKILL.md skill/VERSION
git commit -m "v18 law: the File authority table — one statement of every mode's file rights"
```

---

### Task 6: Prose migration — 14 phrases become references or vanish; BL-028/030 residue

Implements spec §7. Work the Task 4 hit list top to bottom. After this task the wall is green.

**Files:**
- Modify: `skill/SKILL.md` (lines cited are v17 numbers; find by text, not number, since Task 5 shifted them)
- Modify: `skill/references/migration.md:27`
- Modify: `skill/references/restructure.md` §2, §5, line 17

- [ ] **Step 1: Delete the Entry-document authority paragraph**

Remove the whole paragraph beginning `**Entry-document authority (every mode — the one place this is stated).**` (v17 line 58). Nothing replaces it in place — the table and the state rule are its replacement.

- [ ] **Step 2: Step 3 keep-list item (v17 line 42)**

Replace `Kept files are project-owned content — the keep list is manifest metadata about them; it changes nothing about how this skill treats the file (project-owned files are never touched anyway).` with `Kept files are project-owned content — the keep list is manifest metadata about them; it changes nothing about how this run treats the file (authority: kept paths × upgrade).`

- [ ] **Step 3: Step 4 header (v17 line 64)**

Replace `For each of the following, create it **only if it does not already exist** — never overwrite.` with `For each of the following the right is \`create-if-absent\` (authority: scaffolded artifacts × scaffold, and the same cell in every installing and maintaining column).`

- [ ] **Step 4: Step 4 table rows (v17 lines 79, 81, 95)**

- `docs/cases/README.md` row: delete `; create-once, project-owned after creation` — the row ends at `per \`core/sdd.md\`)`.
- `.claude/rules/skills.md` row: replace `Create-once starter: ... ; never overwrite an existing file (project-owned after creation)` with `Starter file: \`{{SANCTIONED_SKILLS_BY_STAGE}}\` per the derivation rules below (authority: project rules × scaffold)`.
- `{{SANCTIONED_SKILLS_BY_STAGE}}` derivation rule: replace `Mode-independent, create-once: skip this artifact entirely when \`.claude/rules/skills.md\` already exists.` with `Mode-independent: skip this artifact entirely when \`.claude/rules/skills.md\` already exists (authority: project rules × upgrade).`

- [ ] **Step 5: Step 7 report (v17 line 120)**

Replace `(per **Entry-document authority** above: a run against an existing manifest never edits \`AGENTS.md\` — it only proposes exact lines here for the user to apply themselves — e.g.` with `(authority: entry document × upgrade = propose-only — exact lines are proposed here for the user to apply themselves, e.g.`.

- [ ] **Step 6: Restructure section (v17 line 186)**

Replace `**Entry-document authority** names stale wiring as the one thing a run against an existing manifest may delete there, because a dangling \`@import\` is machine wiring, not owner content.` with `a dangling \`@import\` is machine wiring, not owner content (authority: entry document × restructure = lossless-write).` and replace `the only action allowed to touch a kept file is linking to it` with `a kept path is link-only (authority: kept paths × restructure)`.

- [ ] **Step 7: Any remaining hits from the Task 4 list**

For each hit not covered above (the decision-gate `never overwrite the loser` on the Step 5 line, if it hit): reword to `never silently pick the loser`. Re-run `python3 evals/check_static.py` after each edit until the prose check reads `ok`.

- [ ] **Step 8: `references/migration.md` line 27**

Delete the parenthesis `(Upgrade mode, which never edits \`AGENTS.md\`, proposes instead.)` and, on the same line, replace `each confirmed stack profile's rule directory` with `each confirmed stack's rule directory`.

- [ ] **Step 9: `references/restructure.md`**

- §2 `heal` bullet: append ` (authority: owned law × upgrade — heal is a delegated upgrade run, not a restructure write).`
- §2 `link` bullet, after `Linking never rewrites the linked file.`: append ` (authority: kept paths × restructure — link-only).`
- §5 first bullet: unchanged (it is about deletion by plan, already cell-consistent).
- Line 17: `\`AGENTS.md\` is the canonical constitution, never foreign` → `\`AGENTS.md\` is the canonical entry document, never foreign`.

- [ ] **Step 10: BL-030 residue in SKILL.md**

Find `\`AGENTS.md\` is the canonical constitution and \`CLAUDE.md\` is a symlink to it` (v17 line 60) → `\`AGENTS.md\` is the canonical entry document and \`CLAUDE.md\` is a symlink to it`. Find `\`AGENTS.md\` is the canonical constitution (never foreign)` (v17 line 151) → `\`AGENTS.md\` is the canonical entry document (never foreign)`.

- [ ] **Step 11: BL-028 residue in SKILL.md**

Lines 31 and 41 (v17): every `stack profile` → `stack`; a bare `profile` that means the subscription → `stack`. Do **not** touch the literal manifest key `profiles` in the legacy-read sentence of Step 1 (line 21) — that names the old key on purpose.

- [ ] **Step 12: Static check fully green**

Run: `python3 evals/check_static.py`
Expected: `all static checks passed`, including `ok    no authority-shaped prose outside the File authority table` and `ok    every (authority: class × mode) reference resolves`.

- [ ] **Step 13: Commit**

```bash
git add skill/SKILL.md skill/references/migration.md skill/references/restructure.md
git commit -m "v18 law: prose rights become cell references; BL-028/030 residue closed"
```

---

### Task 7: Docs — backlog statuses, Horizon, glossary

**Files:**
- Modify: `docs/backlog.md` (BL-028, BL-030, BL-031, BL-038 status lines; the Edition plan block)
- Modify: `docs/philosophy.md` § 6 Horizon
- Modify: `docs/glossary.md`

- [ ] **Step 1: Horizon drops BL-038**

In `docs/philosophy.md` § 6, delete the bullet beginning `- **The file-authority matrix** (BL-038)`. Run `python3 evals/check_static.py` — the Horizon check must still be green (BL-038 is no longer named there; the other three remain open).

- [ ] **Step 2: Backlog statuses**

- BL-038: `**Status: IN PROGRESS 2026-08-22 — edition v18 branch \`feature/v18-file-authority\`; spec \`docs/superpowers/specs/2026-08-22-file-authority-matrix-design.md\`, plan \`docs/superpowers/plans/2026-08-22-file-authority-matrix.md\`. Benchmark pending (Task 8).**` — Task 8 rewrites it to GREEN/DONE with the numbers.
- BL-028: `**Status: DONE in v17 (92d1e3d — stacks key + legacy fallback, upgrade fixture carries a \`profiles\` manifest); prose residue ("stack profile") closed in v18.**`
- BL-030: `**Status: DONE in v17 (92d1e3d — sweep); residue ("AGENTS.md is the canonical constitution" ×3) closed in v18.**`
- BL-031: `**Status: queued (docs-only — \`backlog.md.tpl\` carries no queue/register structure, so the split concerns this repo's \`docs/backlog.md\` only; no VERSION, no benchmark; any time). Left the v18 cycle 2026-08-22.**`
- Edition plan block: BL-028/030 bullets get ` — shipped in v17, residue only` ; BL-031 bullet → `leaves the cycle (docs-only)`.

- [ ] **Step 3: Glossary row**

Insert alphabetically (after `fleet`):

```
| file authority | The one table (SKILL.md `## File authority`) stating what each invocation mode may do to each artifact class, in a closed eight-value vocabulary; prose elsewhere only references a cell. The grader derives its protected set from it; the static check keeps it the only place. | coin | `skill/SKILL.md`; `evals/grade.py` `authority_matrix()` |
```

- [ ] **Step 4: Static check green; commit**

```bash
python3 evals/check_static.py
git add docs/backlog.md docs/philosophy.md docs/glossary.md
git commit -m "docs: v18 statuses — BL-038 in progress, BL-028/030 residue closed, BL-031 re-homed; Horizon drops BL-038"
```

---

### Task 8: Full e2e benchmark — green corpus, idempotency ×3, model floor, `v18.md`

Implements spec §6 step 5 and §9. Per `evals/POLICY.md` §5, the baseline is run from the `v17` tag worktree on the same runner/model first.

**Files:**
- Modify: `evals/benchmarks/v18.md` (complete it)
- Modify: `docs/backlog.md` (BL-038 final status)

- [ ] **Step 1: Baseline on the v17 tag**

```bash
git worktree add /tmp/legislator-baseline-v17 v17
cd /tmp/legislator-baseline-v17
python3 evals/setup_workspace.py /tmp/legislator-eval-baseline
NO_BROWSER=1 tools/evals-bg.sh /tmp/legislator-eval-baseline --runner claude --model <model>
```
Wait for `status.md` to report the run complete; copy the per-scenario pass counts into `v18.md` § Baseline (v17 law, same instrument). Then `cd` back and `git worktree remove /tmp/legislator-baseline-v17`.

- [ ] **Step 2: Run the v18 corpus**

```bash
python3 evals/setup_workspace.py /tmp/legislator-eval-v18
NO_BROWSER=1 tools/evals-bg.sh /tmp/legislator-eval-v18 --runner claude --model <same model>
```
The runner stages: `check_static` → smoke (upgrade) → full corpus → idempotency ×3. Poll `<ws>/status.md`; do not poll the process.

- [ ] **Step 3: Read every red, classify before fixing**

For each failing assert: law / grader / harness / model (POLICY §3). Expected hot spots, from spec §8:
- `mode_respects_authority` red in a scenario → first check whether the **cell** is wrong (e.g. a lawful write the matrix forbids) — that is a *law* defect in the table and is fixed in SKILL.md with a note in `v18.md`; only if the cell is right is it a model defect.
- A red `project_owned_files_untouched` naming a path that the v17 grader did not protect → the derived set is wider than the hand list was; check the path's class and cell before deciding.
Record each classification in `v18.md` § Defect chronicle, as v17 did.

- [ ] **Step 4: Green bar**

All scenarios 100%; `idempotency:fresh-scaffold-dotnet`, `idempotency:upgrade`, `idempotency:restructure` each zero-diff; `selftest:derivation` 100%. If any fix touched `skill/`, re-run the affected scenario and the idempotency pass after it.

- [ ] **Step 5: Model floor**

Re-run the corpus with the cheapest model that reached 100% in v17 (sonnet). Record whether v18 holds there. If it does not, record the cheapest that does — the floor may move; what matters is that it is measured.

- [ ] **Step 6: Complete `v18.md`**

Sections, in order: header (from Task 1) → Red baseline (Tasks 1–4) → Baseline (v17 law, same instrument) → Results table (scenario | v18 pass | v17 pass), with the new asserts counted (`+1 mode_respects_authority` per wired scenario, `+2 net` in selftest) → Idempotency → Model floor → Confounds → Defect chronicle → Cost (tokens/wall if the runner tracked them).

- [ ] **Step 7: Final backlog status and commit**

BL-038 status → `**Status: GREEN <date> — corpus N/N and idempotency ×3 zero-diff; model floor <model>. Edition v18 closes at merge; tag \`v18\`.**`

```bash
git add evals/benchmarks/v18.md docs/backlog.md
git commit -m "benchmark v18: <N>/<N> corpus, idempotency x3 zero-diff, model floor <model>"
git push -u origin feature/v18-file-authority
```

Open the PR (title `v18: the file-authority matrix (BL-038) + BL-028/030 residue`), body from `v18.md`'s results + the red baseline, no generated-with footer. Merging and tagging `v18` are the user's acts.

---

## Self-review against the spec

- **§3 matrix** → Task 5 (bytes pinned, eight values, state rule, harvest/steward line). ✔
- **§3 cell notes** (pair as one class; heal delegated; kept link-only; restructure lossless-write) → Task 2 `class_paths` (pair), Task 6 Step 9 (heal, link), Task 3 (the generic check treats `lossless-write` as any-change so the v14 rename passes). ✔
- **§4 grader** → `authority_matrix` (T1), `class_paths` + `protected_project_files` (T2), `check_mode_authority` in seven graders (T3), three selftest asserts: `authority_matrix_shape`, `authority_states_pinned` (T1), `protected_set_derived_from_cells` replacing the old one (T2). ✔
- **§5 wall** → Task 4, three checks; delivered rules and templates not scanned. ✔
- **§6 evals first** → Tasks 1–4 commit RED output before Task 5 touches `skill/`; each red recorded in `v18.md`. ✔
- **§7 prose table** → Task 6 Steps 1–9, one step per row; BL-028/030 residue Steps 8, 10, 11; BL-031 re-homed in Task 7. ✔
- **§9 done-when** → T6 (only place + wall), T1–3 + T5 (derivation proven both directions), T8 (VERSION 18 — bumped in T5 — benchmark, floor), T7 (statuses, Horizon). ✔
- **Type consistency:** `protected_project_files(repo, fixture_meta=None, matrix=None)` — T2 definition, T2 call site (`repo, fixture_meta=meta`), T2 selftest (`scratch, None, matrix`). `check_mode_authority(g, repo, mode, fixture_meta=None)` — T3 definition and seven call sites. `class_paths(repo, cls, fixture_meta=None)` — T2 definition, used in T2 and T3. ✔
- **Placeholder scan:** `<model>`, `<N>`, `<date>` in Task 8 are run-time values, not plan gaps. No TBD/TODO. ✔
