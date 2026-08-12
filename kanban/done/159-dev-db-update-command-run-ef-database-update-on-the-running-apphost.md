# dev db-update command: run ef-database-update on the running AppHost

## Description

Follow-up scope from task 155 (hybrid on-demand migrations): give the on-demand migration
surface a scriptable CLI form so operators don't need the dashboard. `dev db-update` executes
the `ef-database-update` resource command on `web-migrations` in the RUNNING AppHost via
`aspire resource … --apphost <this repo's AppHost>`. Going through the live resource graph is
deliberate: the Aspire postgres container has a dynamic port, so only the AppHost knows the
connection string — a cold `dotnet ef` invocation can't work without it.

## Checklist

- [x] New Nuru endpoint `tools/dev-cli/endpoints/db-update-command.cs` (mirrors run-command
      idiom; `--apphost` pins discovery so a second running AppHost is never targeted)
- [x] Clear failure guidance when no AppHost is running (aspire CLI error passthrough + hint
      to `dev run` first)
- [x] Validate happy path via runfile against a live AppHost (fresh-code loop, not stale AOT)
- [x] `self-install` so `./bin/dev db-update` ships; `dev --help` lists it
- [x] `dev build` 0/0

## Results

- `tools/dev-cli/endpoints/db-update-command.cs`: `[NuruRoute("db-update")]` endpoint shelling
  `aspire resource web-migrations ef-database-update --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj`
  with passthrough output. On failure prints the exit code plus "Is the AppHost running? Start
  it with `dev run` first."
- Design notes recorded in the file's Design region: resource-name string must equal
  `WebMigrationsResourceName` in the AppHost's constants.cs (agreement-by-memory — CLI can't
  reference the AppHost assembly); without the postgres template flag the aspire CLI's
  resource-not-found error is the honest failure already.
- Validated live: ran via `dotnet run tools/dev-cli/dev.cs -- db-update` against the running
  AppHost — ef tool executed through the resource and reported "No migrations were applied.
  The database is already up to date." (idempotent no-op on current schema). `self-install`
  refreshed `./bin/dev`; `dev --help` lists db-update. `dev build` 0/0.

### How to validate

**Smoke**
```bash
dev run          # in one terminal (or any running AppHost from this repo)
./bin/dev db-update
```
Expect: `Command 'ef-database-update' executed successfully on resource 'web-migrations'.`
and the web-migrations resource transitions Running → Finished in the dashboard; with an
up-to-date schema its logs end in "No migrations were applied. The database is already up to
date."

**Failure path**: with no AppHost running, `./bin/dev db-update` fails with the aspire CLI's
no-running-AppHost error plus the hint to start `dev run` first (non-zero exit code).

**Automated gate**: `./bin/dev build` → 0/0 (CLI compiles as part of the tools tree; no unit
test — the command is a thin shell over the aspire CLI).

Depends on: postgres template flag (the `web-migrations` resource); aspire CLI 13.4+ on PATH.

## Session

- 2026-08-05 claude: created from 155 follow-up scope, implemented, validated live, done.
