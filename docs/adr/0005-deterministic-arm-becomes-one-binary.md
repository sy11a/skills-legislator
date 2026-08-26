# 0005. The deterministic arm becomes one distributed binary; the law stays text

## Status

accepted

## Context

BL-047 measured the constitution: 97 of 176 units are enforceable by code
that does not exist yet, and the engine-growth path (audit, apply/verify,
case-shape lints) is where v23+ investment goes. BL-068 then audited the
existing deterministic surface — a scatter of bash, Python and TypeScript —
against macOS and Windows-native: the operator scripts break off-Linux, the
Windows story has two silent killers, and the deadliest cells live in the
environment contract (interpreter resolution, git symlink semantics,
line-ending config), which no patch to any single script removes for good.

The owner's stated end state (2026-08-26): the deterministic part of the
product should be a universally executable binary, not a set of scripts —
which requires choosing a platform to base it on. The fleet's stack is
.NET, so its SDK/runtime is the one toolchain the fleet already guarantees;
`python3` is not guaranteed off-Linux. The original objections were
examined and resolved: the per-call latency concern applied to `dotnet run`
(JIT + script host), not to NativeAOT binaries (~10–30 ms startup — viable
for PreToolUse hooks); and the owned-file byte-diff law fits text, not
binaries — but byte-verify is the mechanism, not the goal. The goal is
"the arm running in a repo is exactly the arm the edition shipped", which a
version-pinned, checksum-verified installed tool achieves equally.

## Decision

- **End state:** the deterministic arm — the engine's jobs, the Claude Code
  hooks, the operator tools — is **one .NET binary**, published NativeAOT
  per platform (win/linux/macos), installed **per machine** as a versioned
  tool (the model the hooks plugin already uses), never delivered per repo
  as a binary file.
- **The law stratum does not change:** rules, templates and every text
  artifact stay byte-identical delivered copies of `skill/assets/**`.
- **Integrity model for the executable arm:** the edition pins the tool
  version; verification checks the installed tool's reported version and
  its per-platform release checksum recorded at tag time. Byte-diff remains
  the mechanism for text law only.
- **Accepted permanent exception:** the opencode guard stays TypeScript —
  opencode's plugin model requires it; single-language purity is not a goal
  the port can reach.
- **Phasing (the owner's, 2026-08-26):** first **finish the existing
  fixes on the current substrate** — the BL-068 patch set (BL-070) and the
  file-model fork (BL-071) — so the true scope of the deterministic surface
  is known and the platform work can be sized against it; the BL-069
  dependency register precedes the design; only then the binary arm
  (BL-072) is designed and migrated on top, the engine first as the pilot,
  each step red-first under the eval discipline.

## Consequences

- One platform replaces the bash+python zoo on both fleet and operator
  sides; the Windows-native story becomes real instead of accidental.
- New infrastructure obligations arrive with Phase 2: CI cross-compilation
  per RID, release checksums recorded at tag time, and a reproducibility
  story for the builds — the audit's integrity check splits into
  text-law byte-diff and arm version+checksum.
- The environment-contract findings of BL-068 (symlink checkout, autocrlf)
  are *not* solved by this decision — BL-071 owns them regardless of
  platform.
- Migration is L+, multi-edition; until Phase 2 completes, the patched
  scripts remain the arm, and every Phase-1 patch is honest work, not
  throwaway: it defines the behavior the binary must reproduce.
