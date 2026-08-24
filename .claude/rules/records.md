# Records and identifiers

- Historical specs/plans under `docs/superpowers/` record decisions already
  executed — never rewrite them. **Carve-out:** redacting *identifiers* is
  not rewriting a decision. Replacing a fleet repo's name with its alias, or
  an absolute local path with `<repo>`/`<fleet>`, leaves every claim, date
  and conclusion untouched and is permitted — indeed required by the rule
  below. Changing what a record *says* remains forbidden.
- **Tracked files carry no fleet repository names and no absolute local
  paths.** Fleet repos are referred to by stable alias (`fleet-api`,
  `fleet-platform`, `fleet-agent`, `fleet-obs`); the decoding key lives
  outside every repository, at `~/.claude/legislator-fleet-aliases.md`.
  Paths are `<repo>` / `<fleet>/<alias>`. Aliases are stable identifiers —
  never reused for another repo, never renamed, so cross-references between
  documents keep resolving. `check_static.py` enforces this on every commit
  (the name half of the check needs the decoding key, so it is strongest on
  a machine that has it). The one deliberate exception is the environment
  variable `KBO_EVALS_NO_BROWSER`: an integration contract, not prose.
  Note that this governs the working tree only — git history still contains
  what was committed before (BL-040).
