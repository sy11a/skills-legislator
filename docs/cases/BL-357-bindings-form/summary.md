# BL-357 — the bindings file gains a form, a template and a check

**Tier: 0 (direct)** · Gate item `sy11a/Architector#398`, the law and scaffold
half. Cross-repo case; the record lives in `sy11a/Architector`.

## The defect this closes half of

`.claude/rules/verification.md` is parsed by foundry's merge queue into gate
commands. **The law that creates the file never said it had a form.** So two of
Release 2's products wrote their gates as prose — honestly, carefully, and
unreadably — and the kernel read **zero gates** from both.

Zero is not nothing: the release end refuses on zero rows, and the merge queue
does not. Every task in a zero-row repository merges with its gate stage passing.

## What landed here

- **`core/verification.md` states the form** — a gate is one row of a
  three-column table; exactly three cells between four pipes; the `Gate` header
  and a dashes-and-colons separator are skipped and a header is optional; a `|`
  inside a command breaks its row; prose declares nothing. With the sentence the
  two products needed: **a repository with no real gates still writes a table**,
  because every repository carrying this constitution has at least one gate —
  `legislator anchors`.
- **A template**, `verification-rules.md.tpl`, scaffolded only when the file is
  absent. The skill had told every repository to create this file and shipped no
  shape for it.
- **Audit check 22, `bindings-form`** — a file that declares no readable row is a
  Warning, in every repository, on every audit.

## Why the check mirrors the kernel's parse exactly

It splits on `|`, demands five pieces, skips `Gate` and the separator — the same
rules, deliberately. **A check kinder than the reader it stands in for reports a
green that reader will not honour**, which is the defect one level up rather than
a repair of it. So a four-column table is a finding here, because it is invisible
to the kernel there.

The other way on **absence**: an absent file is silent. `core/verification.md`
declares absence a lawful fallback (*"the ladder still applies with repo
defaults"*), and that the merge queue handles absence *worse* than zero rows —
it throws rather than parking — is a kernel defect, filed, and not this check's
to report.

## Verified rather than assumed

Run against the two products as they stand today:

```
=== aidispatcher ===
- [bindings-form] .claude/rules/verification.md: declares no gate row a machine can read …
=== runpool ===
- [bindings-form] .claude/rules/verification.md: declares no gate row a machine can read …
```

It fires on both — the exact repositories that shipped a release in that state.
And it is silent on Architector, foundry and dev-flow, which declare real tables.

## Verification

| Gate | Result |
|---|---|
| `bash evals/check_dotnet.sh` | **702 tests pass** — 6 added |
| `python3 evals/check_static.py` | all static checks pass |
| `legislator audit` over this repository | `bindings-form` among the clean checks |

The six tests are the shapes that matter: prose is a finding; a three-column
table is silent; **a header-and-separator-only file is a finding** (the case a
kinder check would miss, because the file *looks* like a table); a four-column
table is a finding; a headerless table is silent; and an absent file says
nothing.

## What is left, and it is the half that bites

**The kernel.** `MergeQueueDriver` still reports green over zero rows where
`ReleaseEndDriver` refuses, and still throws on an absent file instead of
parking. That is foundry's case, and `#398` stays open for it.
