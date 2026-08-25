# Constitution source discipline

The constitution's source of truth is `skill/assets/rules/**`. The copies
under `docs/ai/rules/**` in this repo and across the fleet are delivered
artifacts, never edited in place.

- Editing any file under `skill/assets/rules/` means the constitution
  changed: bump `skill/VERSION` in the same commit (see README.md).
- Rule files contain only enforceable law; how-to guidance is delegated by
  pointer — see "Content discipline for rule files" in README.md.
