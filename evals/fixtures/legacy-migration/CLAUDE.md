# InvoiceApi — AI Notes

Hand-written notes for AI assistants working on this repo. Please read before making changes.

## Architecture Constraints

- All database access goes through repository interfaces in `Data/Repositories` — never call `DbContext` directly from a controller.
- Money values are always `decimal`, never `double` or `float`.
- Controllers must not contain business logic; put it in `Services/`.

## Branch naming

Use `bl/NNN-short-description` for branches tied to a backlog item, `hf/short-description` for hotfixes.

## Misc

- Run `dotnet format` before committing.
- The staging environment connection string lives in `appsettings.Staging.json` (gitignored) — ask a teammate for a copy.
