#!/usr/bin/env python3
"""The mutation manifest (BL-063, POLICY §1c): assert name -> the named,
minimal corruption of its declared artifact that must flip it to failed.

Two populations, per POLICY §8 (derive over restate):

- **Derived entries** — asserts whose names are data (fixture markers,
  fidelity sentences, preserved needles) get their mutations from the same
  fixture_meta.json that produced the assert names: remove the marker the
  assert demands, insert the marker it forbids, move the anchored marker
  out of its section. A marker added to a fixture automatically arrives
  with its mutation; one that is renamed cannot drift.
- **Named entries** — hand-written, one per named assert, in the closed
  operation vocabulary below.

An assert with no entry here is `uncovered`, and uncovered is a red finding
of the pass (R-603) — the obligation extends to every future assert by
construction.
"""
from __future__ import annotations

import json
import re
import subprocess
from pathlib import Path

SKILL = Path(__file__).resolve().parent.parent / "skill"

REPORT = {
    "audit": ("rotted-layer", "audit-report.md"),
    "audit-engine-absent": ("audit-engine-absent", "audit-report.md"),
    "legacy-migration": ("legacy-migration", "migration-report.md"),
    "legacy-migration-agents-first": ("legacy-migration-agents-first",
                                      "migration-report.md"),
    "upgrade": ("upgrade", "upgrade-report.md"),
    "upgrade-drop-stack": ("upgrade-drop-stack", "upgrade-report.md"),
    "restructure": ("restructure", "restructure-report.md"),
}


class Mutation:
    """One named corruption. `apply(ws, rev)` mutates in place, recording
    every touched path/HEAD in the Reverter; `key()` is the canonical
    identity used for duplicate detection; `probe` marks the existence
    asserts, for which removal (unmeasured elsewhere, failed on the probe)
    is the lawful kill."""

    def __init__(self, op: str, *args: str, fn, probe: bool = False) -> None:
        self.op, self.args, self.fn, self.probe = op, args, fn, probe

    def key(self) -> tuple:
        return (self.op, *self.args)

    def describe(self) -> str:
        return f"{self.op}({', '.join(self.args)})"

    def apply(self, ws: Path, rev) -> None:
        self.fn(ws, rev)


# --- the closed operation vocabulary --------------------------------------

def _write(rev, path: Path, text: str) -> None:
    rev.touch(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text)


def _edit(rev, path: Path, fn) -> None:
    rev.touch(path)
    path.write_text(fn(path.read_text()))


def _delete(rev, path: Path) -> None:
    rev.touch(path)
    if path.is_file() or path.is_symlink():
        path.unlink()


def _delete_tree(rev, root: Path) -> None:
    for p in sorted(root.rglob("*"), reverse=True):
        if p.is_file() or p.is_symlink():
            rev.touch(p)
            p.unlink()
    for p in sorted(root.rglob("*"), reverse=True):
        if p.is_dir():
            p.rmdir()
    if root.is_dir():
        root.rmdir()


def _drop_lines(text: str, needle: str) -> str:
    return "\n".join(l for l in text.splitlines() if needle not in l) + "\n"


def _replace_everywhere(rev, repo: Path, needle: str,
                        replacement: str = "REDACTED") -> None:
    """Case-insensitive, like the fidelity grep it mutates against: the law
    lawfully re-cases carried lines (table capitalization), so a literal
    redaction misses the carried copy — 'billing period' survived exactly
    that way on the first full pass."""
    pat = re.compile(re.escape(needle), re.I)
    for p in repo.rglob("*"):
        if ".git" in p.parts or not p.is_file():
            continue
        try:
            text = p.read_text()
        except (UnicodeDecodeError, OSError):
            continue
        if pat.search(text):
            rev.touch(p)
            p.write_text(pat.sub(replacement, text))


def _move_out_of_section(text: str, marker: str, heading: str) -> str:
    """Remove the marker's lines from the named ## section, append them at
    the end — the marker stays present, its anchoring breaks."""
    m = re.search(rf"^## {re.escape(heading)}\s*\n(.*?)(?=^## |\Z)", text,
                  re.S | re.M)
    if not m:
        return text
    section = m.group(1)
    moved = [l for l in section.splitlines() if marker in l]
    kept = "\n".join(l for l in section.splitlines() if marker not in l)
    return (text[:m.start(1)] + kept + "\n" + text[m.end(1):]
            + "\n" + "\n".join(moved) + "\n")


def _commit_all(rev, repo: Path) -> None:
    """add -A first: `commit -a` skips untracked files, and a fresh
    scaffold is almost entirely untracked — the first pass proved it by
    leaving nothing_committed green (survived, 2026-08-26)."""
    rev.touch_git(repo)
    subprocess.run(["git", "-C", str(repo), "add", "-A"], capture_output=True)
    subprocess.run(["git", "-C", str(repo), "-c", "user.email=m@m", "-c",
                    "user.name=m", "commit", "-qm", "mutation"],
                   capture_output=True)


def _json_edit(rev, path: Path, fn) -> None:
    rev.touch(path)
    data = json.loads(path.read_text())
    fn(data)
    path.write_text(json.dumps(data, indent=2) + "\n")


def _grep_file(root: Path, needle: str) -> Path | None:
    for p in sorted(root.rglob("*")):
        if ".git" in p.parts or not p.is_file():
            continue
        try:
            if needle in p.read_text():
                return p
        except (UnicodeDecodeError, OSError):
            continue
    return None


# --- manifest builders, one per artifact family ---------------------------

def _common(repo: Path, mani: Path) -> dict[str, Mutation]:
    """The common_checks family (scaffold/migration/upgrade scenarios)."""
    okf = repo / "docs/ai/rules/core/okf.md"
    return {
        "manifest_valid_json": Mutation(
            "corrupt-json", str(mani.name),
            fn=lambda ws, rev, p=mani: _edit(rev, p, lambda t: t + "not json")),
        "manifest_version_matches_skill_VERSION": Mutation(
            "json-set", "legislatorVersion", "1",
            fn=lambda ws, rev, p=mani: _json_edit(
                rev, p, lambda d: d.update(legislatorVersion=1))),
        "manifest_stacks_correct": Mutation(
            "json-set", "stacks", "[]",
            fn=lambda ws, rev, p=mani: _json_edit(
                rev, p, lambda d: d.update(stacks=[]))),
        "manifest_ownedFiles_exact_sorted": Mutation(
            "json-drop-first", "ownedFiles",
            fn=lambda ws, rev, p=mani: _json_edit(
                rev, p, lambda d: d["ownedFiles"].pop(0))),
        "manifest_stacks_single_line_inline": Mutation(
            "expand-stacks-array",
            fn=lambda ws, rev, p=mani: _edit(rev, p, lambda t: re.sub(
                r'^(  "stacks": )\[([^\]\n]*)\],$',
                lambda m: f'{m.group(1)}[\n    {m.group(2)}\n  ],',
                t, count=1, flags=re.M))),
        "manifest_keep_matches_expected": Mutation(
            "json-append-keep-entry",
            fn=lambda ws, rev, p=mani: _json_edit(
                rev, p, lambda d: d.setdefault("keep", []).append(
                    {"path": "docs/planted.md", "reason": "mutation"}))),
        "manifest_key_order": Mutation(
            "reorder-keys",
            fn=lambda ws, rev, p=mani: _json_edit(
                rev, p, lambda d: (v := d.pop("legislatorVersion"),
                                   d.update(legislatorVersion=v)))),
        "manifest_keep_pinned_serialization": Mutation(
            "unpin-keep",
            fn=lambda ws, rev, p=mani: _edit(rev, p, lambda t: (
                t.replace('  "keep": [],', '  "keep": [\n  ],', 1)
                if '  "keep": [],' in t
                # populated list: break the single-line-object pinning
                else t.replace('{"path": ', '{\n     "path": ', 1)))),
        "owned_files_verbatim": Mutation(
            "edit-owned", "core/okf.md",
            fn=lambda ws, rev, p=okf: _edit(rev, p, lambda t: t + "\ndrift\n")),
        "v14_model_agents_canonical_claude_symlink": Mutation(
            "delete", "CLAUDE.md",
            fn=lambda ws, rev, p=repo / "CLAUDE.md": _delete(rev, p)),
        "nothing_committed": Mutation(
            "git-commit-all",
            fn=lambda ws, rev, r=repo: _commit_all(rev, r)),
        "no_unresolved_placeholders": Mutation(
            "insert-bare-token", "CHANGELOG.md",
            fn=lambda ws, rev, p=repo / "CHANGELOG.md": _edit(
                rev, p, lambda t: t + "\n{{PLANTED_TOKEN}}\n")),
    }


def _scaffold(repo: Path) -> dict[str, Mutation]:
    gl = repo / "docs/okf/glossary.md"

    def cut_glossary(ws, rev, p=gl):
        def fn(t):
            lines, out, in_table, seen_rows = t.splitlines(), [], False, 0
            for l in lines:
                if l.lstrip().startswith("|") and "Term" in l:
                    in_table = True
                    out.append(l)
                    continue
                if in_table and l.lstrip().startswith("|"):
                    seen_rows += 1
                    if seen_rows <= 1:      # keep the separator only
                        out.append(l)
                    continue
                in_table = False
                out.append(l)
            return "\n".join(out) + "\n"
        _edit(rev, p, fn)

    def drop_one_core_import(ws, rev, p=repo / "AGENTS.md"):
        _edit(rev, p, lambda t: re.sub(
            r"^@docs/ai/rules/core/[^\n]*\n", "", t, count=1, flags=re.M))

    def drop_all_core_imports(ws, rev, p=repo / "AGENTS.md"):
        _edit(rev, p, lambda t: re.sub(
            r"^@docs/ai/rules/core/[^\n]*\n", "", t, flags=re.M))

    return {
        "scaffold_artifacts_present": Mutation(
            "delete", "docs/cases/README.md",
            fn=lambda ws, rev, p=repo / "docs/cases/README.md": _delete(rev, p)),
        "skills_stage_map_scaffolded": Mutation(
            "blank-file", ".claude/rules/skills.md",
            fn=lambda ws, rev, p=repo / ".claude/rules/skills.md": _write(
                rev, p, "# skills\n")),
        "glossary_seeded_with_terms": Mutation(
            "cut-glossary-rows", fn=cut_glossary),
        "agents_md_imports_all_core": Mutation(
            "drop-one-core-import", fn=drop_one_core_import),
        "agents_md_imports_rules": Mutation(
            "drop-all-core-imports", fn=drop_all_core_imports),
        "project_rules_dir_scaffolded": Mutation(
            "delete-tree", ".claude/rules",
            fn=lambda ws, rev, p=repo / ".claude/rules": _delete_tree(rev, p)),
    }


def _report_derived(ws: Path, scenario: str, meta: dict) -> dict[str, Mutation]:
    """The data-named report asserts: mutations derived from the same
    fixture_meta entries that name the asserts (POLICY §8)."""
    sub, fname = REPORT[scenario]
    rp = ws / sub / "outputs" / fname
    out: dict[str, Mutation] = {}
    for marker in meta.get("report_markers", []):
        out[f"report names {marker!r}"] = Mutation(
            "remove-lines", fname, marker,
            fn=lambda ws_, rev, p=rp, m=marker: _edit(
                rev, p, lambda t: _drop_lines(t, m)))
    for marker, severity in meta.get("severity_anchored_markers", []):
        out[f"report anchors {marker!r} under ## {severity}"] = Mutation(
            "move-out-of-section", fname, marker, severity,
            fn=lambda ws_, rev, p=rp, m=marker, s=severity: _edit(
                rev, p, lambda t: _move_out_of_section(t, m, s)))
    for marker in meta.get("absent_markers", []):
        out[f"report does NOT contain {marker!r}"] = Mutation(
            "insert-line", fname, marker,
            fn=lambda ws_, rev, p=rp, m=marker: _edit(
                rev, p, lambda t: t + f"\n{m}\n"))
    for marker in meta.get("candidate_absent_markers", []):
        def insert_in_candidates(ws_, rev, p=rp, m=marker):
            def fn(t):
                i = t.find("## Constitution candidates")
                if i < 0:
                    return t + f"\n## Constitution candidates\n{m}\n"
                j = t.find("\n", i) + 1
                return t[:j] + m + "\n" + t[j:]
            _edit(rev, p, fn)
        out[f"candidates section does NOT contain {marker!r}"] = Mutation(
            "insert-in-candidates", fname, marker, fn=insert_in_candidates)
    return out


def _fidelity_derived(ws: Path, repo: Path, meta: dict) -> dict[str, Mutation]:
    out: dict[str, Mutation] = {}
    for s in meta.get("fidelity_sentences", []):
        out[f"fidelity: {s[:44]!r}"] = Mutation(
            "redact-everywhere", s[:44],
            fn=lambda ws_, rev, r=repo, needle=s: _replace_everywhere(
                rev, r, needle))
    return out


# --- per-scenario assembly -------------------------------------------------

def mutations_for(ws: Path, scenario: str) -> dict[str, Mutation]:
    sub = REPORT.get(scenario, (scenario, ""))[0]
    home = ws / sub
    repo = home / "repo"
    mani = repo / "docs/ai/manifest.json"
    meta_file = home / "fixture_meta.json"
    meta = json.loads(meta_file.read_text()) if meta_file.exists() else {}
    rp = (home / "outputs" / REPORT[scenario][1]) if scenario in REPORT else None

    muts: dict[str, Mutation] = {}

    def probe_report(name: str) -> None:
        muts[name] = Mutation("delete-report", REPORT[scenario][1],
                              fn=lambda ws_, rev, p=rp: _delete(rev, p),
                              probe=True)

    def zero_writes() -> None:
        muts["zero_writes"] = Mutation(
            "create-untracked", "mutation-litter.txt",
            fn=lambda ws_, rev, p=repo / "mutation-litter.txt": _write(
                rev, p, "litter\n"))

    def authority_edit_owned() -> None:
        # audit/restructure hold owned law read-only/never-touch: an edit is
        # the minimal diff-shape violation.
        muts["mode_respects_authority"] = Mutation(
            "edit-owned-law", "core/decision-gate.md",
            fn=lambda ws_, rev,
            p=repo / "docs/ai/rules/core/decision-gate.md": _edit(
                rev, p, lambda t: t + "\nviolation\n"))

    if scenario == "fresh-scaffold-dotnet":
        muts |= _common(repo, mani) | _scaffold(repo)
        # scaffold's owned law is `replace` — the kept-path column is what a
        # scaffold must not touch; but fresh has no keep. The scaffolded-
        # artifacts column is create-if-absent: an M on a scaffolded file
        # violates it only pre-creation; simplest lawful violation: modify
        # the entry document symlink target class? Use the kept-paths-free
        # route: delete CLAUDE.md is already v14's mutation. For authority,
        # commit nothing and instead edit .claude/rules/skills.md tracked?
        # Fresh repo has ONE commit (empty seed) — every file is untracked
        # (A), and create-if-absent allows A. The one class always present
        # and never writable in installing modes is... none; use the
        # manifest × scaffold = replace too. Fallback: git-commit makes
        # every A into tracked-at-HEAD, then an edit is M on scaffolded
        # artifacts (create-if-absent forbids M).
        def scaffold_authority(ws_, rev, r=repo):
            _commit_all(rev, r)
            _edit(rev, r / "docs/cases/README.md", lambda t: t + "\nmut\n")
        muts["mode_respects_authority"] = Mutation(
            "commit-then-edit-scaffolded", "docs/cases/README.md",
            fn=scaffold_authority)

    elif scenario in ("legacy-migration", "legacy-migration-agents-first"):
        muts |= _common(repo, mani) | _scaffold(repo)
        muts |= _report_derived(ws, scenario, meta)
        wiring_target = repo / "AGENTS.md"
        muts["agents_md_v2_wiring_written_directly"] = Mutation(
            "drop-line", "AGENTS.md", "## Boundaries",
            fn=lambda ws_, rev, p=wiring_target: _edit(
                rev, p, lambda t: _drop_lines(t, "## Boundaries")))
        muts["mode_respects_authority"] = Mutation(
            "commit-then-edit-scaffolded", "docs/cases/README.md",
            fn=lambda ws_, rev, r=repo: (
                _commit_all(rev, r),
                _edit(rev, r / "docs/cases/README.md", lambda t: t + "\nmut\n")))
        if scenario == "legacy-migration":
            probe_report("step7_report_saved")
            cand = "Money values are always"
            muts["harvest_lists_decimal_money_rule"] = Mutation(
                "remove-lines", REPORT[scenario][1], cand,
                fn=lambda ws_, rev, p=rp: _edit(
                    rev, p, lambda t: _drop_lines(t, cand)))
            muts["harvest_excludes_instance_convention"] = Mutation(
                "insert-in-candidates", REPORT[scenario][1],
                "bl/NNN-short-description",
                fn=lambda ws_, rev, p=rp: _edit(rev, p, lambda t: t.replace(
                    "## Constitution candidates",
                    "## Constitution candidates\nbl/NNN-short-description",
                    1)))
            def drop_law_carve(ws_, rev, r=repo):
                f = _grep_file(r / ".claude/rules", "Money values are always")
                if f:
                    _delete(rev, f)
            muts["law_carved_to_project_rules"] = Mutation(
                "delete-carved-rule", fn=drop_law_carve)
            muts["instance_data_not_in_project_rules"] = Mutation(
                "plant-instance-rule", ".claude/rules/branching.md",
                fn=lambda ws_, rev,
                p=repo / ".claude/rules/branching.md": _write(
                    rev, p, "Branches: bl/NNN-short-description\n"))
            for needle in ("Money values are always",
                           "bl/NNN-short-description"):
                muts[f"preserved: {needle!r}"] = Mutation(
                    "redact-everywhere", needle,
                    fn=lambda ws_, rev, r=repo, n=needle: _replace_everywhere(
                        rev, r, n))
        else:
            probe_report("migration_report_saved")
            muts["agents_md_content_preserved"] = Mutation(
                "drop-line", "AGENTS.md", "bl/NNN-short-description",
                fn=lambda ws_, rev, p=repo / "AGENTS.md": _edit(
                    rev, p, lambda t: _drop_lines(
                        t, "bl/NNN-short-description")))

    elif scenario in ("upgrade", "upgrade-drop-stack"):
        muts |= _common(repo, mani)
        muts |= _report_derived(ws, scenario, meta)
        kept = meta.get("expected_keep") or []
        kept_path = kept[0]["path"] if kept else "docs/notes/special-sauce.md"
        muts["project_owned_files_untouched"] = Mutation(
            "edit-protected", kept_path,
            fn=lambda ws_, rev, p=repo / kept_path: _edit(
                rev, p, lambda t: t + "\nmut\n"))
        muts["mode_respects_authority"] = Mutation(
            "edit-entry-document-content", "AGENTS.md",
            fn=lambda ws_, rev, p=repo / "AGENTS.md": _edit(
                rev, p, lambda t: t + "\nviolation\n"))
        if scenario == "upgrade":
            probe_report("step7_report_saved")
            core = meta["withheld_core_rule"]
            stack = meta["withheld_stack_rule"]
            muts["newly_added_rule_present"] = Mutation(
                "delete", f"core/{core}",
                fn=lambda ws_, rev,
                p=repo / "docs/ai/rules/core" / core: _delete(rev, p))
            muts["newly_added_stack_rule_present"] = Mutation(
                "delete", f"stacks/dotnet/{stack}",
                fn=lambda ws_, rev,
                p=repo / "docs/ai/rules/stacks/dotnet" / stack: _delete(rev, p))
            muts["report_proposes_core_import_line"] = Mutation(
                "remove-lines", REPORT[scenario][1], f"core/{core}",
                fn=lambda ws_, rev, p=rp, n=f"core/{core}": _edit(
                    rev, p, lambda t: _drop_lines(t, n)))
            muts["report_proposes_stack_import_line"] = Mutation(
                "remove-lines", REPORT[scenario][1], f"dotnet/{stack}",
                fn=lambda ws_, rev, p=rp, n=f"dotnet/{stack}": _edit(
                    rev, p, lambda t: _drop_lines(t, n)))
            muts["retired_rule_deleted"] = Mutation(
                "recreate-retired", meta["retired_rule"],
                fn=lambda ws_, rev, p=repo / "docs/ai/rules/core" /
                meta["retired_rule"]: _write(rev, p, "# retired\n"))
            muts["upgrade_creates_missing_artifacts"] = Mutation(
                "delete", "docs/cases/README.md",
                fn=lambda ws_, rev,
                p=repo / "docs/cases/README.md": _delete(rev, p))
            muts["keep_refusal_for_owned_path"] = Mutation(
                "remove-lines", REPORT[scenario][1], "okf.md",
                fn=lambda ws_, rev, p=rp: _edit(
                    rev, p, lambda t: _drop_lines(t, "okf.md")))
        else:
            probe_report("upgrade_report_saved")
            dropped = meta["dropped_stack_files"][0]
            muts["dropped_stack_files_deleted"] = Mutation(
                "recreate-dropped", dropped,
                fn=lambda ws_, rev, p=repo / dropped: _write(
                    rev, p, "# dropped stack rule\n"))
            dotnet_rule = sorted(
                (repo / "docs/ai/rules/stacks/dotnet").glob("*.md"))[0]
            muts["kept_stack_untouched_and_refreshed"] = Mutation(
                "delete", f"stacks/dotnet/{dotnet_rule.name}",
                fn=lambda ws_, rev, p=dotnet_rule: _delete(rev, p))
            muts["report_proposes_aurelia_import_removal"] = Mutation(
                "remove-lines", REPORT[scenario][1], "stacks/aurelia",
                fn=lambda ws_, rev, p=rp: _edit(
                    rev, p, lambda t: _drop_lines(t, "stacks/aurelia")))

    elif scenario == "audit":
        muts |= _report_derived(ws, scenario, meta)
        probe_report("audit_report_saved")
        authority_edit_owned()
        zero_writes()
        muts["audit_report_outside_repo"] = Mutation(
            "plant-report-in-repo", "docs/planted-report.md",
            fn=lambda ws_, rev,
            p=repo / "docs/planted-report.md": _write(rev, p, "# report\n"))
        slug = "imports-resolve"
        muts["parity_every_check_has_a_defect"] = Mutation(
            "meta-drop-slug", slug,
            fn=lambda ws_, rev, p=meta_file: _json_edit(
                rev, p, lambda d: d["check_slugs_covered"].remove(slug)))

    elif scenario == "audit-engine-absent":
        probe_report("audit_report_saved")
        zero_writes()
        muts["fixture_state_is_bundle_without_engine"] = Mutation(
            "plant-engine", "docs/ai/engine.py",
            fn=lambda ws_, rev,
            p=repo / "docs/ai/engine.py": _write(rev, p, "# engine\n"))
        muts["check15_engine_absent_info"] = Mutation(
            "remove-lines", REPORT[scenario][1], "okf-anchors",
            fn=lambda ws_, rev, p=rp: _edit(
                rev, p, lambda t: _drop_lines(t, "okf-anchors")))
        def warn_anchor(ws_, rev, p=rp):
            def fn(t):
                if re.search(r"^## Warning", t, re.M):
                    return re.sub(r"^(## Warning\s*\n)",
                                  r"\1- [okf-anchors] planted finding\n",
                                  t, count=1, flags=re.M)
                return t + "\n## Warning\n- [okf-anchors] planted finding\n"
            _edit(rev, p, fn)
        muts["no_anchor_warning_without_an_engine"] = Mutation(
            "insert-warning-anchor", fn=warn_anchor)

    elif scenario == "case-practice":
        cases_dir = repo / "docs/cases"
        agent_case = sorted(d for d in cases_dir.glob("BL-*") if d.is_dir())
        case = agent_case[0] if agent_case else cases_dir / "BL-none"

        def edit_case_files(fn):
            def apply(ws_, rev, c=case):
                for p in sorted(c.rglob("*.md")):
                    rev.touch(p)
                    p.write_text(fn(p.read_text()))
            return apply

        muts["case_born_in_case_home"] = Mutation(
            "delete-tree", "agent-case", probe=True,
            fn=lambda ws_, rev, c=case: _delete_tree(rev, c))
        muts["tier_declared_in_case_header"] = Mutation(
            "strip", "Tier:",
            fn=edit_case_files(lambda t: t.replace("Tier:", "T:")))
        muts["ears_lines_with_ids"] = Mutation(
            "strip", "R-NNN ids",
            fn=edit_case_files(lambda t: re.sub(r"\bR-\d{3}\b", "R-X", t)))
        muts["gherkin_hurting_case_present"] = Mutation(
            "strip", "GIVEN",
            fn=edit_case_files(lambda t: re.sub(
                r"\bGIVEN\b", "G_", t, flags=re.I)))
        muts["tasks_trace_per_rnnn"] = Mutation(
            "strip", "per R-",
            fn=edit_case_files(lambda t: re.sub(
                r"per(\s+)(R-\d{3})", r"for\1\2", t)))
        muts["converge_trail_present"] = Mutation(
            "strip", "converge trail",
            fn=edit_case_files(lambda t: re.sub(
                r"Converged|\((missing|partial|contradicts|unrequested)\)",
                "X", t)))
        muts["case_home_readmark_untouched"] = Mutation(
            "edit-file", "docs/cases/README.md",
            fn=lambda ws_, rev,
            p=repo / "docs/cases/README.md": _edit(
                rev, p, lambda t: t + "\nmut\n"))
        def plant_lint_defect(ws_, rev, c=cases_dir / "BL-999-mutation"):
            _write(rev, c / "spec.md", "# BL-999 — planted\n\n"
                   "### R-001 — planted req\n\nWHEN x THEN y SHALL z.\n")
            _write(rev, c / "plan.md", "# plan\n\n1. task, per R-777\n")
        muts["delivered_engine_sdd_lint_clean"] = Mutation(
            "plant-unconverged-case-with-dangling-ref", fn=plant_lint_defect)

    elif scenario == "restructure":
        muts |= _report_derived(ws, scenario, meta)
        muts |= _fidelity_derived(ws, repo, meta)
        probe_report("restructure_report_saved")
        muts["mode_respects_authority"] = Mutation(
            "remove-lines", REPORT[scenario][1], "[heal]",
            fn=lambda ws_, rev, p=rp: _edit(
                rev, p, lambda t: _drop_lines(t, "[heal]")))
        muts["no_unresolved_placeholders"] = Mutation(
            "insert-bare-token", "CHANGELOG.md",
            fn=lambda ws_, rev, p=repo / "CHANGELOG.md": _edit(
                rev, p, lambda t: t + "\n{{PLANTED_TOKEN}}\n"))
        kept_path = repo / meta["kept_path"]
        muts["kept_file_untouched_in_place"] = Mutation(
            "edit-file", meta["kept_path"],
            fn=lambda ws_, rev, p=kept_path: _edit(
                rev, p, lambda t: t + "\nmut\n"))
        muts["conflict_not_auto_resolved"] = Mutation(
            "drop-line", "AGENTS.md", "conflict-marker",
            fn=lambda ws_, rev, p=repo / "AGENTS.md",
            m=meta["conflict_marker"]: _edit(
                rev, p, lambda t: _drop_lines(t, m)))
        muts["v14_model_canonicalized"] = Mutation(
            "delete", "CLAUDE.md",
            fn=lambda ws_, rev, p=repo / "CLAUDE.md": _delete(rev, p))
        muts["conflict_surfaced_as_decision"] = Mutation(
            "remove-lines", REPORT[scenario][1],
            "We do not maintain CHANGELOG.md",
            fn=lambda ws_, rev, p=rp: _edit(rev, p, lambda t: _drop_lines(
                t, "We do not maintain CHANGELOG.md")))
        prc = repo / meta["project_rule_conflict_path"]
        muts["project_rule_conflict_decision_gated"] = Mutation(
            "edit-file", meta["project_rule_conflict_path"],
            fn=lambda ws_, rev, p=prc: _edit(rev, p, lambda t: t + "\nmut\n"))
        gl = repo / "docs/okf/glossary.md"
        def cut_gl(ws_, rev, p=gl):
            _edit(rev, p, lambda t: "\n".join(
                l for i, l in enumerate(t.splitlines())
                if not (l.lstrip().startswith("|")
                        and "---" not in l and "Term" not in l
                        and "Meaning" not in l)) + "\n")
        muts["glossary_healed_with_terms"] = Mutation(
            "cut-glossary-rows", fn=cut_gl)
        muts["glossary_heal_in_plan"] = Mutation(
            "remove-lines", REPORT[scenario][1], "glossar",
            fn=lambda ws_, rev, p=rp: _edit(rev, p, lambda t: "\n".join(
                l for l in t.splitlines() if "glossar" not in l.lower())
                + "\n"))
        fg = repo / meta["foreign_glossary_path"]
        muts["foreign_glossary_merged_away"] = Mutation(
            "recreate", meta["foreign_glossary_path"],
            fn=lambda ws_, rev, p=fg: _write(rev, p, "# ubiquitous\n"))
        definition = meta["foreign_glossary_definition"]
        muts["foreign_definition_in_okf_glossary"] = Mutation(
            "redact-in-file", "docs/okf/glossary.md",
            fn=lambda ws_, rev, p=gl, d=definition: _edit(
                rev, p, lambda t: re.sub(re.escape(d), "REDACTED", t,
                                         flags=re.I)))
        skf = repo / meta["skills_rules_path"]
        muts["skill_binding_for_the_team_not_a_plan_item"] = Mutation(
            "edit-file", meta["skills_rules_path"],
            fn=lambda ws_, rev, p=skf: _edit(rev, p, lambda t: t + "\nmut\n"))
        stray = repo / meta["stray_rulebook_path"]
        muts["stray_rulebook_merged_away"] = Mutation(
            "recreate", meta["stray_rulebook_path"],
            fn=lambda ws_, rev, p=stray: _write(rev, p, "# stray\n"))
        def drop_stray_law(ws_, rev, r=repo):
            f = _grep_file(r / ".claude/rules", meta["stray_project_law"])
            if f:
                _delete(rev, f)
        muts["stray_law_merged_to_project_rules"] = Mutation(
            "delete-carved-rule", "stray",
            fn=drop_stray_law)
        muts["plans_relocated_to_standard_home"] = Mutation(
            "delete", "docs/superpowers/plans/2026-01-importer-plan.md",
            fn=lambda ws_, rev, p=repo /
            "docs/superpowers/plans/2026-01-importer-plan.md": _delete(rev, p))
        muts["misplaced_case_relocated_to_case_home"] = Mutation(
            "delete", "docs/cases/BL-0007/plan.md",
            fn=lambda ws_, rev,
            p=repo / "docs/cases/BL-0007/plan.md": _delete(rev, p))
        muts["cursorrules_merged_away"] = Mutation(
            "recreate", ".cursorrules",
            fn=lambda ws_, rev, p=repo / ".cursorrules": _write(
                rev, p, "rules\n"))
        muts["ghost_import_fixed"] = Mutation(
            "insert-line", "AGENTS.md", "ghost-rule import",
            fn=lambda ws_, rev, p=repo / "AGENTS.md": _edit(
                rev, p,
                lambda t: t + "\n@docs/ai/rules/core/ghost-rule.md\n"))
        for marker in ("okf-anchors", "okf-sync-debt"):
            muts[f"{marker.replace('-', '_')}_routed_to_team"] = Mutation(
                "remove-lines", REPORT[scenario][1], marker,
                fn=lambda ws_, rev, p=rp, m=marker: _edit(
                    rev, p, lambda t: _drop_lines(t, m)))
        for path in meta.get("okf_untouched", {}):
            muts[f"okf_{Path(path).stem}_unedited"] = Mutation(
                "edit-file", path,
                fn=lambda ws_, rev, p=repo / path: _edit(
                    rev, p, lambda t: t + "\nmut\n"))
        muts["owned_drift_healed"] = Mutation(
            "edit-owned", "core/okf.md",
            fn=lambda ws_, rev,
            p=repo / "docs/ai/rules/core/okf.md": _edit(
                rev, p, lambda t: t + "\ndrift\n"))
        muts["manifest_healed_keep_carried"] = Mutation(
            "json-clear-keep",
            fn=lambda ws_, rev, p=mani: _json_edit(
                rev, p, lambda d: d.update(keep=[])))
        idx = repo / "docs/okf/index.md"
        muts["orphan_linked_not_deleted"] = Mutation(
            "redact-everywhere", "orphan-notes.md",
            fn=lambda ws_, rev, r=repo: _replace_everywhere(
                rev, r, "orphan-notes.md", "gone.md"))
        def unindex_but_keep_linked(ws_, rev, p=idx, r=repo):
            # keep the `linked` gate true (a conditional assert switches
            # off, it does not fail — BL-062's converge note, met live):
            # plant a reference elsewhere, then drop the index's.
            _edit(rev, r / "docs/okf/log.md",
                  lambda t: t + "\nSee `docs/okf/orphan-notes.md`.\n")
            _edit(rev, p, lambda t: _drop_lines(t, "orphan-notes.md"))
        muts["link_post_state_in_index"] = Mutation(
            "unindex-but-keep-linked", "orphan-notes.md",
            fn=unindex_but_keep_linked)
        muts["stale_map_row_gone"] = Mutation(
            "insert-line", "docs/okf/codebase-map.md", "legacy/",
            fn=lambda ws_, rev,
            p=repo / "docs/okf/codebase-map.md": _edit(
                rev, p, lambda t: t + "\n| `legacy/` | old importer |\n"))
        muts["fidelity_line_reported"] = Mutation(
            "remove-lines", REPORT[scenario][1], "Fidelity: verified",
            fn=lambda ws_, rev, p=rp: _edit(
                rev, p, lambda t: _drop_lines(t, "Fidelity: verified")))

    return muts
