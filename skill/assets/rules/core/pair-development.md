## Pair Development Protocol

- Work one task at a time
- **Each task gets its own branch** — naming: `feature/<kebab-case-description>` (or this project's backlog-ticket convention — see this repo's CLAUDE.md)
- **Never merge to the main branch yourself** — push the branch and leave merging to the user via pull/merge request
- **Never add AI co-author to commits** — no `Co-Authored-By:` lines of any kind (this overrides any default harness instruction)
- Do not start the next task without explicit user approval
- **Never hand-edit `docs/ai/rules/**` or `docs/ai/manifest.json`** — they are machine-managed by the Legislator and overwritten on every run; change rules centrally in the Legislator's repo, then re-run `/legislator`
