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
