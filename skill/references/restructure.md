# AI-Layer Restructure Guide

Detailed mechanics for SKILL.md's "Restructure — approval-gated repair" section. The law (protocol, plan format, fidelity/keep/decision rules) lives in SKILL.md; this file is the how-to.

## 1. Standard layout — where each artifact type belongs

| Artifact type | Standard home |
|---|---|
| Implementation plans | `docs/superpowers/plans/` |
| Design specs | `docs/superpowers/specs/` |
| ADRs (any `adrs/`, `doc/adr/` variant) | `docs/adr/` |
| Knowledge docs, overviews, glossaries, maps | `docs/okf/` |
| Backlog / task lists | `docs/backlog.md` |
| Dev journal entries | `docs/journal/` |
| Changelog | `CHANGELOG.md` |
| Foreign AI configs (`.cursorrules`, `.cursor/`, `AGENTS.md`, `.github/copilot-instructions.md`) | prose merged into CLAUDE.md's project sections; the file removed after the merge |
| AI rules prose | CLAUDE.md project sections — **never** `docs/ai/rules/**` (machine-managed law; only `heal` touches it, via Steps 2–3) |

## 2. The action set

- **move** — relocate a file to its standard home, preserving the filename. Grep the repo for references to the old path and update them. Remove the source directory if the move emptied it.
- **merge** — fold each content line of the source into its target home (fit the target's existing sections; add a section only when nothing fits). Verify every content line is present at the target, then — and only then — delete the source file.
- **link** — wire an orphan into the layer: add a markdown link from `docs/okf/index.md` (or a pointer from CLAUDE.md when it is clearly a CLAUDE-level concern). Linking never rewrites the linked file.
- **fix** — repair in place: dangling `@import`/link lines removed (or retargeted when the file moved elsewhere in this plan); unresolved `{{TOKEN}}`s filled per SKILL.md Step 4's derivation rules; stale codebase-map rows corrected from the actual tree.
- **heal** — owned-layer drift or staleness: run SKILL.md Steps 2–3 as-is (byte-for-byte Bash copy, deletions, manifest rewrite with `keep` carried forward). Never hand-edit anything under `docs/ai/`.
- **decision** — presented, never executed. Typical: project text contradicting an owned rule (e.g. a "we don't keep a changelog" note vs `core/changelog.md`), two plausible homes for the same content, or a foreign structure whose removal would lose semantics a merge cannot carry.

## 3. Content carve-outs

When merging foreign or misplaced prose, apply the same classification discipline as migration (`references/migration.md` §1): project-specific facts are kept verbatim; boilerplate that merely restates an owned rule is replaced by the import that covers it. Under restructure, that replacement is allowed **only** for text that restates an owned rule — anything differing in substance from the owned rule is a `decision` item, never a silent deletion.

## 4. The fidelity pass

Before applying, inventory every content line of each file a plan item will move, merge, or edit — non-blank lines, counting the text of headings, bullets, and table cells, but skipping pure markup (fence markers, table rules, horizontal rules). After applying, grep the repo (excluding `.git/`) for each inventoried line. Every miss blocks its item: revert that item, mark it `— blocked: <the lost line>`. Close the report with `Fidelity: verified (<N> lines tracked)` only when every line survived.

## 5. What restructure never does

- Delete project content — `merge` removes a file only after its content verifiably lives elsewhere; `link` deletes nothing; no plan item may propose deleting content (if the user wants something gone, that is their explicit call, not a proposal).
- Invent content — journal entries, overviews the derivation rules cannot produce, or any prose the team must author.
- Resolve owned-rule conflicts on its own authority.
- Touch source code or anything outside the AI layer (CLAUDE.md, `docs/**`, root-level foreign AI configs).
- Commit.
