# BL-360 — the scaffold records its own day

**Tier: 0 (direct)** · Gate item `sy11a/Architector#364`.

## The defect

**A freshly scaffolded repository fails its own audit on the day it is created.**
The scaffold commits `.gitattributes`, `AGENTS.md`, `CLAUDE.md`, `opencode.json`
— paths outside `docs/` — and writes `docs/journal/README.md` but no dated entry.
`journal-recency` then fires, correctly, on the scaffold's own commit.

It was found independently by **two unattended runs on two subscriptions**,
legislating two repositories from briefs that both said *every check the skill's
own audit runs passes, or each finding it leaves is named in your delivery*.
Neither closed it. **When two independent agents make the same omission, the
instruction is not the defect — the shape is.**

## Why the finding matters more than the warning

`core/artifact-lifecycle.md`: *"A class of items that systematically yields no
action is excluded mechanically … A worklist that is mostly noise gets ignored,
which kills the ritual it exists to serve."* A finding that fires on **every
scaffold in every repository** is that class exactly — and it fires on the
constitution's own act, which is the wrong first impression for the artifact
whose entire purpose is that a repository's law is trustworthy.

## The choice, and why this one

The issue offered two. **Taken: the scaffold writes the day's entry.** The
alternative — exempting the scaffold's own commit from check 8 — is cheaper and
leaves a new repository with an empty journal looking clean.

The entry is better because **it is true rather than filler**: a constitution was
laid, at a named edition, with a named stack subscription, and that is a real
working day. It also seeds the ritual, which is the harder half of a journal's
adoption — a rule nobody has yet obeyed is a rule the first busy day drops.

The template says what a scaffold day honestly does *not* have — no real gate
beyond `legislator anchors`, no ADR but the one recording that ADRs are kept, no
case, no dead ends — so the entry does not pretend to be a work entry.

**Fresh-scaffold mode only.** An upgrade writes no entry: the day's work belongs
to the repository being upgraded, and inventing an entry for it would be the
filler this avoided.

## What has no control, said plainly

**Step 4 is performed by the skill, not the engine** — no `apply` code writes any
of these templates, so no test in this repository can assert that a scaffolded
repository passes its own audit. The engine's own `journal-recency` tests are
unchanged and still cover the check.

That gap is the honest limit: this repairs the instruction, and the instruction
is followed by an agent. The two runs that found the defect were following an
instruction too.

## Verification

| Gate | Result |
|---|---|
| `python3 evals/check_static.py` | passes — **after it caught a violation this case did not introduce** |
| `bash evals/check_dotnet.sh` | 705 tests pass, unchanged |

## What the static gate caught on the way

`check_static.py` failed on entry with five **absolute local paths** in
`docs/cases/BL-357-bindings-form/`'s refutation files — committed by BL-357 and
merged, because that case ran `check_dotnet.sh` on its last commit and not
`check_static.py`. This repository's `records.md` forbids absolute paths and
fleet repository names in tracked files, **and provides the carve-out that makes
the repair lawful**: redacting identifiers is not rewriting a decision. Paths are
now `<repo>` and `<fleet>/<alias>`; every claim, date and conclusion is
untouched.

A gate is only as good as the last commit it ran on.

## The refutation round

Run on `bailian-cli/deepseek-v4-pro` in its own checkout against the whole diff
(`refutation-brief.md` is what it was given; `refutation.md` is what it returned,
both redacted of absolute local paths per `records.md`'s carve-out). Five claims
and four named weaknesses were put to it; it confirmed three claims and returned
two SERIOUS findings, both acted on.

| Finding | Verdict after my own check | Act |
|---|---|---|
| SERIOUS — *"no verification bindings that name a real gate beyond `legislator anchors`"* parses as "there are no verification bindings" | Upheld. A fresh scaffold writes `.claude/rules/verification.md` with one row, so the sentence a reader takes away is false about the file they will open | Reworded with the verb positive: *"verification bindings that name no gate beyond `legislator anchors`"* |
| SERIOUS — `{{STACKS}}` is underspecified in format, in its `none` literal, and duplicates `{{STACK_SUMMARY}}` | Upheld, and checked against the file: `{{STACK_SUMMARY}}` (SKILL.md's derivation rules) already defines the human-readable form, and `{{TODAY}}` is `{{TODAY_ISO_DATE}}` under another name | Both placeholders replaced by the existing tokens; the bolded-`none` field replaced by an instruction to drop the sentence when no stack is confirmed |
| MINOR — `{{EDITION}}` is declared only in the journal row's notes cell | Upheld: it is the one genuinely new token, and the derivation-rules list is where a future editor looks | Declared in the derivation rules — the `VERSION` file's value, verified to be `26` in `skill/VERSION` |
| Claim 1, the audit mechanism | Confirmed independently, with the `lastCode is null` and the older-code edge cases both walked | — |
| Weakness C, the computed filename | No finding: `JournalLint`'s `^\d{4}-\d{2}-\d{2}\.md$`, check 8's reader and `RenderJob`'s writer all agree on the shape | — |
| Weakness D, BL-357's redaction | Lawful and claim-preserving, checked line by line against `records.md`'s carve-out | — |

What it did **not** overturn is the choice itself: it argued the other side of
option 2 (exempt the scaffold's commit from check 8) as asked, and concluded the
split is defensible, calling the upgrade-writes-no-entry exclusion *"a difference
of degree, not kind"* while granting the difference is real. That is the weakest
point of this case and it stays stated rather than resolved.
