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

- [x] Pending-migrations probe replaces table probe (decision recorded)
- [x] Quiet wait + bounded timeout with clear message
- [x] Design regions (AppHost, probe, seed) updated
- [x] Regression test (partially migrated DB + late migration) + existing first-run fact green
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's running `dev run` or database.

## Session

- Created: 2026-10-14 (cockpit, from the Aspire MCP diagnosis; first in the next stack per Steve)
- 2026-10-14 implementer (ganda task work, headless): pending-migrations probe, seed wait, tests, gates.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T15:41:51Z
- Review oracle (claude, claude-opus-5-5, effort 2, roster general), session not reported: 2 rounds, disposition clean.

## Results

**Decision:** `IPendingMigrationsProbe` (`platform/persistence/`, namespace
`TimeWarp.Architecture.Persistence`) with `EfPendingMigrationsProbe` (`platform/postgres/`) built
on `Database.GetPendingMigrationsAsync()`. It was picked over "is the latest migration id in
`__EFMigrationsHistory`" because it also catches an older migration that was never applied, and EF
owns the history table's name and schema. Logging: EF 11 / Npgsql reads `__EFMigrationsHistory`
**without** checking it exists, so on a fresh database EF logs "Failed executing DbCommand" at
Error (the first test run showed this). The probe therefore runs a `to_regclass` check on the
history table first, using the name and schema from the context's relational options. While the
table is missing, the probe reports every compiled migration (`GetMigrations()`, no query).

**Seed wait** (`SiteSettingsSeedHostedService`): each attempt asks the probe. While migrations are
pending it logs Information, "waiting for web-migrations: N pending migration(s) (latest X)". From
attempt 121 it logs the same at Warning. After 180 attempts it throws `InvalidOperationException`
naming every pending migration and the `ef-database-update` remedy, instead of a raw 42P01 or 42703.
The 42P01 catch stays for the probe-then-`dev db reset` race. In-memory builds register no probe,
so they don't wait.

**Changed:** probe port and adapter renamed and moved (old `ISiteSettingsTableProbe` and
`EfSiteSettingsTableProbe` removed); `PostgresDbModule` registration; Design regions in AppHost
`program.cs` (Design region and the inline web-migrations comment), the probe, the seed and the
Postgres module; `tw-aggregate-pattern` skill; `.template.config/template.json` `!postgres` excludes.

**Tests:**
- `web-infrastructure-tests/ef-pending-migrations-probe-tests.cs`: fresh DB (reports every
  migration, no Error), partially migrated to the second-to-last migration (reports only the
  latest), fully migrated (reports none), plus the existing control that a fresh-DB store read
  does log Error.
- `web-server-integration-tests/features/settings/site-settings-boot-pending-migrations-tests.cs`
  (regression): migrates a fresh DB to the second-to-last migration and boots a real Web.Server
  against it. The test asserts the boot is still blocked after 3s and the pending-migration log
  names the latest migration. It then applies the latest migration and asserts the host starts,
  `GET api/settings/assistant` returns 200, and no Error-or-above entry was logged. Before this
  change the host started at once because the table existed, so the "still blocked" assertion
  failed.

**Gates (this worktree):** `dev build` 0 warnings / 0 errors; `dev test` all suites passed,
including aspire-tests 51/51 (`FirstRunOnEmptyDatabase`), web-infrastructure-tests 78/78 and
web-server-integration-tests 341/341; `dev template-smoke` passed; `ganda repo audit` passed. The
AppHost user-secrets file was hashed before and after `dev test` and was unchanged. No `dev run` was
started, and the maintainer's database was not touched (tests use Testcontainers).

### How to validate

**Smoke:**
```bash
./bin/dev build
dotnet build tests/container-apps/web/web-infrastructure-tests && tests/container-apps/web/web-infrastructure-tests/bin/Release/net11.0/web-infrastructure-tests
tests/container-apps/web/web-server-integration-tests/bin/Release/net11.0/web-server-integration-tests --ansi off | grep -E "pending migration|total:|failed:"
```
Manual: with `dev run` up, add a trivial migration (`dev db add-migration Probe363`), restart
web-server from the dashboard before running `ef-database-update` on web-migrations, then run it.

**Expect:**
- Infrastructure suite `failed: 0`; the `Probe_Given_` cases pass with no captured EF Error.
- Web.Server integration suite `failed: 0`. Its log shows "Site settings seed is waiting for
  web-migrations: 1 pending migration(s) (latest <newest migration id>)" for a few attempts, then
  the host starts.
- Manual: web-server stays in startup, logs the Information wait lines (no `fail:` and no 42703 or
  42P01), and reaches Running within about a second of the migration finishing. Without migrating,
  it fails after 180s with "Site settings seed gave up after 180 attempts ... missing N migration(s)".

### Review disposition

- **Rounds:** 2. **Effort:** 2. **Roster:** general.
- **Final counts:** bug 0, suggestion 1 fixed, nit 0. 0 open, 0 wontfix.
- **Disposition:** `clean`.
- **M1 (suggestion, fixed):** the boot regression waited a fixed 3s before asserting the wait log,
  so a slow host build could fail it. It now polls up to 30s for the log, waits 2s more and asserts
  the boot is still blocked. After the fix, `dev build` was 0/0 and web-server-integration-tests
  passed 341/341.
- **Artifacts:** `review/review-framework.md`, `review/round-1/` and `review/round-2/` (`general.md`,
  `merged.md`), `review/disposition.md`.
