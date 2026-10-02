# BL-487 — edition 29: verification back to always, the journal rides the fragment trigger

**Tier: 2** (full). Closes `sy11a/skills-legislator#75`. It also completes BL-484's follow-ups F-3,
F-6 and F-7.

## Result

Edition 28 put `core/verification.md` and `core/dev-journal.md` on demand. Measured in a consuming
repository afterwards, their pointers were read 0 of 4 and 0 of 2 times, while pointers tied to a
file act were read 5 of 5. The operator ruled to fix it, and edition 29 does:

- **`core/verification.md` is always-tier again.** It is an `@` import in `AGENTS.md.tpl` and an
  explicit `opencode.json` instruction.
- **The changelog pointer now names `core/dev-journal.md` too.** The journal's own pointer is gone,
  so the journal is read on the fragment trigger, which does fire.
- **The upgrade report brings an edition-28 entry document along.**
  - When a rule has turned always-tier, the report proposes removing its old pointer line.
  - When an on-demand rule's template line has changed, the report proposes replacing the old
    pointer (remove the old line, add the template's line).
  - When several old lines are replaced by one merged template line, the add is proposed once.
  - Both proposals fire only for a line in the pointer grammar that names the rule alone. Owner
    prose that mentions the path is left alone.
- **`legislator verify` no longer demands the day's journal file after an upgrade.** That row is
  excluded only when a run record says the mode was not fresh scaffold. With no record, the row
  stays required.
- **`core/skills.md` "Law beats skills" covers a third case:** a project rule reached only by its own
  pointer line.
- **This repository now has its own `.claude/rules/verification.md` and `docs/changes/README.md`**,
  and its `AGENTS.md` is at the edition-29 shape.

## How it was verified

- **.NET suite:** `evals/check_dotnet.sh` passed 878/878. New tests pin each behaviour:
  - `ReportJobTierTests.cs`: the edition-28 and edition-29 shapes, owner prose, two removes with
    one add, and the merged add proposed once.
  - `ReportJobLayoutTests.cs`: the non-default layout run through the report (BL-484 F-3).
  - `VerifyJobTests.cs`: upgrade, fresh and no-record runs.
  - `Step4TargetsTests.cs`: the filter matches by path, so the `AGENTS.md` row is not caught, and
    the snapshot stays unfiltered.
- **Static and engine checks:** `check_static`, `check_engine`, `check_hooks` and
  `check_opencode_plugin` are all green. `check_engine` showed two arm-integrity reds before the
  edition bump, which was expected, and is green after it.
- **e2e benchmark:** 235/235 across nine scenarios, with idempotency zero-diff
  (`evals/benchmarks/v29.md`).
  - The new `upgrade-tier-flip` scenario upgrades an edition-28-shaped repository and scored 19/19.
  - Run 1 showed the scenario was not wired into the harness; `c461b45` fixed that.
  - Two candidate-harvest reds were classified model-class by control runs on master.
- **Self-delivery** used this repository as fleet member #0.
  - Before `AGENTS.md` was edited, the report proposed exactly the five lines the spec's hurting
    case predicts: the merged add once, three pointer removes, and the `verification.md` `@` add.
  - After the edit, `legislator report` proposed nothing, and `verify`, `anchors` and `sdd-lint`
    exited 0.
- **Converge:** `plan.md` § Converge judged every requirement met.

## Open

- **Release digests:** `release.json` digests are empty until tag time, as with every edition.
- **Placeholder in fresh mode:** `Step4Targets` never resolves `<today>`, so a fresh-mode `verify`
  still checks the literal path. This was out of scope.
- **Remove proposals:** they name the rule, not the line text, so they could quote the line.
- **Still open from BL-484:**
  - F-1: stage skills in other repositories should name their trigger file.
  - F-2: stack rules and `.claude/rules/*.md` have no tier split yet.
