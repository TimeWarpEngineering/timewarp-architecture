# web-server boot waits for no pending migrations, instead of only the site-settings table existing

## Incident (2026-10-14, maintainer's `dev run` / arch.timewarp.work)

After #501 merged (task 361 added the `SiteSettings.AssistantName` column), `dev run` crashed
web-server at boot:

```
Npgsql.PostgresException: 42703: column s.AssistantName does not exist
web-server: start 14:38:27, exit code 1 at 14:38:32
web-migrations (ef-tool): finished 14:38:38
```

The ingress went Unhealthy as a result. The cockpit recovered by starting web-server again after
migrations finished.

## Root cause

- By design, the AppHost has **no wait edge** from web-server to `web-migrations` (AppHost
  `program.cs` Design region, tasks 155 and 270). `WaitFor` deadlocks dashboard restarts, and
  `WaitForCompletion` breaks DCP endpoint wiring (the dotnet-ef tool inherits web-server's env).
  **Keep it that way.**
- web-server tolerates the first-boot window instead: `SiteSettingsSeedHostedService` asks
  `ISiteSettingsTableProbe` / `EfSiteSettingsTableProbe` (`to_regclass`) whether
  **`identity.site_settings` exists**, and waits until it does.
- That covers an **empty** database, but not a **new column or table on an existing schema**. The
  table exists, so the probe passes and the read fails. Every future migration that touches
  something read at boot reintroduces this crash.

## Requirements

- Replace the table-existence probe with a **"no pending migrations"** check. The seed (and any
  other boot-time reader) waits until the database contains every migration this web-server build
  was compiled with. For example:
  - `DbContext.Database.GetPendingMigrationsAsync()` is empty; or
  - the assembly's latest migration id is present in `__EFMigrationsHistory`.

  Choose one, and record why (cost, logging behaviour).
- **No Error-level logs** while waiting. A missing `__EFMigrationsHistory` (fresh database) means
  "everything pending", using a query that can't fail and log at Error, the same goal as task 270's
  `to_regclass` probe. If the EF API logs failures, guard with a catalog check first.
- Bounded wait with clear Information/Warning progress logs, for example "waiting for N pending
  migrations (latest X)". On timeout, fail with a clear message naming the pending migrations;
  don't crash on a raw 42703 or 42P01.
- **Keep the probe abstraction:** only backends that can be unmigrated register it
  (`PostgresDbModule`). In-memory / no-Postgres apps don't wait.
- Rename the port and type to match the new meaning (for example `IPendingMigrationsProbe`), and
  update the Design regions:
  - AppHost `program.cs`: the "first-boot window ... web-server is what tolerates it" paragraph;
  - the probe;
  - `SiteSettingsSeedHostedService`.
- **Regression tests:**
  - **New:** start web-server against a database migrated to the **second-to-last** migration,
    apply the latest migration after a delay, and assert web-server waits (no Error logs, no crash)
    and then serves. This would fail today, exactly like the incident.
  - Keep aspire-tests' `FirstRunOnEmptyDatabase` fact green.
  - Unit tests for the probe: fresh DB, partially migrated, fully migrated.

## Checklist

- [ ] Pending-migrations probe replaces table probe (decision recorded)
- [ ] Quiet wait + bounded timeout with clear message
- [ ] Design regions (AppHost, probe, seed) updated
- [ ] Regression test (partially migrated DB + late migration) + existing first-run fact green
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's running `dev run` or database.

## Session

- Created: 2026-10-14 (cockpit, from the Aspire MCP diagnosis; first in the next stack per Steve)
