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
| Foreign AI configs (`.cursorrules`, `.cursor/`, `.github/copilot-instructions.md`) | law-shaped rules merged into `.claude/rules/<topic>.md`; narrative prose into AGENTS.md's project sections; the file removed after the merge. (`AGENTS.md` is the canonical constitution, never foreign; `CLAUDE.md` is its symlink, never foreign.) |
| Foreign glossary/domain stores (`CONTEXT.md`, `CONTEXT-MAP.md`, `UBIQUITOUS_LANGUAGE.md`) | term definitions merged into `docs/okf/glossary.md` rows; law-shaped lines into `.claude/rules/<topic>.md` per §3; the file removed after the merge |
| Project-specific rules (law for this repo only) | `.claude/rules/<topic>.md` — see `core/project-rules.md` |
| Stray rulebooks (review/refactoring checklists or rule lists parked outside the law homes — audit check 12) | law-shaped lines merged into `.claude/rules/<topic>.md` per the §3 carve-outs; the file removed after the merge. Conflicts with owned law are `decision` items, never merged silently |
| Narrative AI rules prose | AGENTS.md project sections — **never** `docs/ai/rules/**` (machine-managed law; only `heal` touches it, via Steps 2–3) |

## 2. The action set

- **move** — relocate a file to its standard home, preserving the filename. Grep the repo for references to the old path and update them. Remove the source directory if the move emptied it.
- **merge** — fold each content line of the source into its target home (fit the target's existing sections; add a section only when nothing fits). **The plan item quotes every line it carries** (the `fix`-glossary precedent: terms live in the item itself) — the inventory is durable on disk, so an interrupted or resumed run re-reads what it is carrying instead of recalling it from context. **Idempotent insert:** a line is added only when the target does not already contain it verbatim — re-applying a merge after an interruption neither duplicates nor fears a retry. **Deferred deletion:** the source file survives until the fidelity pass of §4 has verified every inventoried line of the whole apply phase; deleting confirmed sources is the last act of the phase. Until then the source stays — a killed run always leaves the truth recoverable.
- **link** — wire an orphan into the layer: add a markdown link from `docs/okf/index.md` (or a pointer from AGENTS.md when it is clearly an AGENTS-level concern). Linking never rewrites the linked file.
- **fix** — repair in place: dangling `@import`/link lines removed (or retargeted when the file moved elsewhere in this plan); unresolved `{{TOKEN}}`s filled per SKILL.md Step 4's derivation rules; stale codebase-map rows corrected from the actual tree; an empty glossary (check 13) seeded per SKILL.md Step 4's `{{GLOSSARY_TABLE}}` derivation rule — terms proposed in the plan item itself so approving the item approves the terms.
- **heal** — owned-layer drift or staleness: run SKILL.md Steps 2–3 as-is (byte-for-byte Bash copy, deletions, manifest rewrite with `keep` carried forward). Never hand-edit anything under `docs/ai/`.
- **decision** — presented, never executed. Typical: project text contradicting an owned rule (e.g. a "we don't keep a changelog" note vs `core/changelog.md`), two plausible homes for the same content, or a foreign structure whose removal would lose semantics a merge cannot carry. **Deletion proposals live here and only here:** a plan may propose deleting a file solely as a `[decision]` item — the run never executes a deletion; removing content is the owner's hands alone (blanket approval must never be able to destroy anything). A check-7 orphan is a `[link]` item by default; when the orphan looks genuinely dead, the honest form is a `[decision]` item saying so — an owner's call, not a "safe to delete" aside. In the final report each item is repeated **verbatim as printed in the plan** — same wording, same `<where> ↔ <owned rule>` line — with only the outcome suffix appended.

## 3. Content carve-outs

When merging foreign or misplaced prose, apply the same three-way classification as migration (`references/migration.md` §1): law-shaped project rules go to `.claude/rules/<topic>.md`; project-specific instance data is kept verbatim in AGENTS.md; boilerplate that merely restates an owned rule is replaced by the import that covers it. Under restructure, that replacement is allowed **only** for text that restates an owned rule — anything differing in substance from the owned rule is a `decision` item, never a silent deletion.

## 4. The fidelity pass

Before applying, inventory every content line of each file a plan item will move or merge (edits by `fix` items are exempt — their stated purpose is removing specific dead lines) — non-blank lines, counting the text of headings, bullets, and table cells, but skipping pure markup (fence markers, table rules, horizontal rules). The inventory is part of the plan (per §2's merge rule) — it survives interruption. After applying, grep the repo (excluding `.git/` **and the still-undeleted merge sources** — a line that matches only inside its own source has not been carried anywhere yet; excluding the sources is what makes the check honest while deferred deletion keeps them alive) for each inventoried line. Every miss blocks its item: revert that item, mark it `— blocked: <the lost line>`. Only when every line is verified do the confirmed merge sources get deleted (§2's last act). Close the report with `Fidelity: verified (<N> lines tracked)` only when every line survived.

## 5. What restructure never does

- Delete content on its own authority — `merge` removes a file only after its content verifiably lives elsewhere and the fidelity pass confirms it; `link` deletes nothing; a deletion proposal exists only as a `[decision]` item the owner executes (if the user wants something gone, the decision item is the proposal, and their hands are the executor).
- Invent content — journal entries, overviews the derivation rules cannot produce, or any prose the team must author.
- Resolve owned-rule conflicts on its own authority.
- Touch source code or anything outside the AI layer (the AI layer being: AGENTS.md (+ the `CLAUDE.md` symlink), `.claude/rules/**`, `docs/**`, and root-level foreign AI configs).
- Commit.
