# 0007. Outer-only placement: the AI layer leaves the code repository

## Status

accepted

## Context

Decided 2026-08-29 with the owner; implemented by edition v25 (BL-077),
fleet migration by v26 (BL-078).

Since v1 the legislator has delivered its owned layer *into* the target
repository — `docs/ai/**`, the OKF bundle, cases, journal, project law
and the entry document all committed beside the code. Fleet discovery,
the enforcement arms, the engine's root model and the audit checks all
assume that one tree. An outer mode (BL-027, ontology §Placement modes)
was designed as an exception for repositories the operator cannot commit
to.

The owner's 2026-08-29 decision reframes the system as a developer-
experience framework of four members — legislator (laws), kbl (the
knowledge fund), fleet-obs (practice observability), and a dev-flow tool
— whose shared property is that nothing of the AI layer is mixed into
the code. Two ideas drove it: a single legislator installation per
machine configuring every project it will ever touch, and rule layering
(machine → instance → sub-group → project) that keeps agent context
small. Both are impossible while the layer is a per-repository commit.

## Decision

1. **Outer is the only placement mode.** The AI layer of a project lives
   in an external *control directory*; the code repository keeps zero
   tracked AI files. Inner mode is retired; every legislated repository
   migrates (BL-078).
2. **The instance is the git unit.** One repository per instance (a group
   of projects sharing rules), projects as subtrees, a light group node.
   Teams share the instance repository; a case is a paired MR — the
   instance half merges first.
3. **The machine registry is the single truth about placement** —
   `~/.config/legislator/instances.yaml`, generated, legislator-written,
   listing every clone of every project. Everything else (the untracked
   stub in the clone, the fleet-obs registry, an env projection later) is
   a render of it. Arms that cannot read it fail open with one warning;
   verification fails loud.
4. **Delivery is layered:** a two-file untracked stub (`CLAUDE.md` import
   line, `opencode.json` absolute instructions) restored by global
   sentinel hooks; env-attach (`--add-dir`, `OPENCODE_CONFIG_DIR`) is the
   reserved evolution once a `legislator` CLI with shell-init exists.
5. **Config is code.** What agents read is the AI layer; what the build or
   CI executes is code and travels through the code repository's MR. The
   legislator generates such files from stack law and proposes them; it
   never owns them. Native analyzers, generators and compilers adjudicate
   before any agent does.
6. **Default deterministic.** Every flow step is classed D or A; A must
   justify why determinism is impossible. Fleet upgrade runs with no
   agent.
7. **kbl and fleet-obs are modules of the legislator**, installed and
   configured by it; fleet-obs serves the whole machine and derives its
   registry from the legislator's, with a hand overlay for foreign
   sources. The legislator owns structure, mappings, templates and
   version merge; it never stores knowledge or computes metrics.

## Consequences

- Easier: one installation configures every project; rule layering
  becomes a directory tree; fleet delivery is an enumerated list, not a
  filesystem scan; the vendor-key dependency of upgrade disappears;
  member #0's depth-5 invisibility (BL-055) vanishes; a register DB and a
  binary arm (ADR-0005 phase 2) get a natural per-instance home.
- Harder: the engine needs a two-root model (`anchors` across two git
  repositories); the four arms depend on machine state; the file-
  authority matrix gains a tree axis; the entire eval corpus is re-cut
  into control+code pairs; lazy path-scoped rules are lost (imports are
  eager); an external `@import` costs one trust dialog per project.
- Superseded in part: ADR-0002 and ADR-0004 keep their principle
  (member #0 is governed by its own constitution and delivered as a
  release step) but their mechanics move to the instance that holds the
  legislator project. BL-027's design is absorbed; ontology §Placement
  modes is rewritten to a single mode.
- Cross-repository obligations: fleet-obs must accept a registry from an
  external source without crashing on unknown keys (its ADR-0041), read
  note roles from the node manifest instead of path literals, and follow
  `@import` in its adapters — sequenced before v25 ships (BL-080).
