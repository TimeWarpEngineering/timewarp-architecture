# web-server waits for web-migrations so first run logs no errors

## Description

On a first run against an empty database (after `dev db nuke`, on a new machine, or in a freshly
generated app), web-server starts before `web-migrations` has created the schema. The
`SiteSettingsSeedHostedService` first query fails with `42P01 relation "identity.site_settings"
does not exist`. The seeder recovers: it logs a Warning, "attempt 1/30; retrying", and the next
attempt succeeds. But EF Core first logs **two Error-level entries**, "Failed executing DbCommand"
and "An exception occurred while iterating over the results of a query". The dashboard shows
2 errors on a clean start. The maintainer observed this on 2026-10-02, right after the first
`dev db nuke --yes`.

Goal: a first run against an empty database logs **no errors**.

## Constraint: read the AppHost Design region first

`aspire-app-host/program.cs` (Design region, task 155) records that **both wait edges were tried
and removed**:

- `WaitFor(web-migrations)` deadlocks every dashboard restart or rebuild of web-server.
  Run-mode `DefaultWaitBehavior` only continues past Running, and the one-time migration
  resource never re-enters Running (Finished is terminal).
- `WaitForCompletion(web-migrations)` resolved restarts, but reproducibly broke DCP
  service-producer endpoint creation for web-server under Aspire.Hosting.Testing: "Could not
  create Endpoint object(s): information about the port to expose the service is missing;
  service-producer annotation is invalid". That was verified on 2026-08-05 with
  Aspire.Hosting.EntityFrameworkCore 13.4.6-preview.

The accepted trade-off was a brief first-boot window. That window is what produces these errors.

## Requirements

1. **Preferred: re-validate `WaitForCompletion` on Aspire 13.6.** The repo is now on Aspire 13.6
   and EF preview 13.6.0-preview.1.26479.8. Add `webServer.WaitForCompletion(webMigrations)` (or
   the 13.6-correct equivalent) and prove all three of the following. Keep it only if all three
   hold:
   - **(a) The Aspire.Hosting.Testing suites are green** (aspire-tests and every closed-box
     suite), with no DCP "service-producer annotation is invalid" error.
   - **(b) Restart and rebuild don't deadlock.** Verify through the AppHost model or the
     closed-box tests, not by starting the maintainer's AppHost. Show that after migrations
     finish, the wait is satisfied for a restarted web-server.
   - **(c) First run against an empty database logs no Error** from the seeder path. Prove it in
     a closed-box test with a fresh volume (`UseDataVolume=false`) if one is feasible.
2. **Fallback, only if (1) fails on 13.6:** keep the AppHost unchanged. Make the seeder probe
   quietly.
   - Before querying through EF, check that the table exists with a cheap catalog query, such as
     `to_regclass('identity.site_settings')` or `information_schema`. Wait with the existing
     retry and backoff if the table is missing, without running the failing EF query, so EF
     never logs an Error.
   - Keep the existing 30-attempt budget and its Warning. Consider lowering a "migrations not
     applied yet" probe to Information or Debug on early attempts.
   - Check other startup readers of not-yet-migrated tables, such as the read-path seeding
     wrapper from task 254, for the same pattern.
3. **Record the outcome in the AppHost Design region**, either way. Reconcile it with the 13.6
   result: kept with evidence, or still broken, with the 13.6 error text and the fallback used.
   The existing paragraph must not go stale.
4. **Do not regress:** `dev db reset` (drop and migrate in a running server), the
   `ef-database-update` dashboard command, production's script or bundle migration path, or
   `dev db nuke`.

## Checklist

- [x] `WaitForCompletion` re-tested on Aspire 13.6 against (a), (b) and (c); kept, or rejected
      with evidence
- [x] If rejected: the seeder probes the table quietly, so no EF Error on first run; other startup
      readers checked
- [x] A test that proves a first run against an empty database logs no Error from the seeder path
- [x] AppHost Design region reconciled with the 13.6 outcome
- [x] Gates: `dev build` 0/0, `dev test` (including aspire-tests and the closed-box suites),
      `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start the maintainer's AppHost or run `dev db nuke --yes`. Closed-box test suites
      are fine
- [ ] Implementation review; host `open-pr`

## Session

- Created: 252821 (2026-10-01)
- 2026-10-02 implementer (Claude Opus 5.5, ganda task work): re-tested WaitForCompletion on 13.6
  and rejected it, then implemented the quiet-probe fallback, the tests and the region updates. All gates green.
- 2026-10-02 review oracle (Claude Opus 5.5, ganda task work): implementation review, effort 2,
  roster general, 1 round — disposition clean.

## Notes

- Evidence: Aspire structured logs 2026-10-02, web-server logIds 65 and 76 (Error) and 77 (seeder
  Warning, attempt 1/30), immediately after `dev db nuke --yes` then `dev run`.
- Related: task 155 (removed the wait edges), task 254 (site settings seed on first read),
  task 266/269 (`dev db nuke`).
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

**WaitForCompletion on Aspire 13.6: rejected.** Re-tested with Aspire 13.6.0 and
Aspire.Hosting.EntityFrameworkCore 13.6.0-preview.1.26479.8 by adding
`webServer.WaitForCompletion(webMigrations)`.

- The wait itself worked. web-server logged "Waiting for resource 'web-migrations' to complete",
  then started after "Successfully executed command 'ef-database-update'".
- **(a) failed.** The dotnet-ef tool resource inherits web-server's environment, and DCP rejected
  its substitution: `Could not perform substitution for environment variable ASPNETCORE_URLS ...
  service '/web-server-https' referenced by Executable '/ef-tool-web-migrations-…' specification
  is not produced by this Executable`. web-server then listened on the Kestrel default
  `http://localhost:5000` instead of its allocated port, and the ingress health check timed out.
- aspire-tests failed 6 of 11 with the edge ("Resource 'ingress' failed to become healthy").
  The same suite passed 11 of 11 without it.
- (b) and (c) are moot: the AppHost keeps no wait edge, so a restart has nothing to deadlock on.

**Fallback: quiet seeder probe.**

- New port `ISiteSettingsTableProbe` in Identity.Application, with Postgres implementation
  `EfSiteSettingsTableProbe` in Identity.Infrastructure. It runs
  `SELECT to_regclass("identity"."site_settings") IS NOT NULL`, and takes the schema and table
  names from the EF model. `PostgresDbModule` registers it. The file is postgres-gated in
  template.json, so in-memory builds have no probe and seed in a single pass.
- `SiteSettingsSeedHostedService` asks the probe before each EF read and waits (1s, same
  30-attempt budget) while the table is missing. Attempts 1–5 log Information ("waiting for
  web-migrations…"); later attempts log Warning. The last attempt skips the probe, so an exhausted
  budget still fails the host with the real 42P01. The 42P01 catch and its Warning stay for a
  table removed between probe and query.
- Other startup readers: the seed hosted service is the only boot-time reader of a migrated
  table. `EntraSchemeRegistrationLogHostedService` reads options only, and the environment check
  only calls CanConnect. The task-254 read decorator is left unchanged and its Design region says
  why: the seed runs before Kestrel starts, so no request reads before migrations on a first run.
- Design regions reconciled: AppHost `program.cs` (13.6 error text and evidence, plus the inline
  comment), `postgres-db-module-server.cs`, `site-settings-seed-hosted-service-server.cs`,
  `seed-on-read-site-settings-store-application.cs`, and the ingress-smoke Design region.
  Skill `tw-aggregate-pattern` now has the startup-reader probe rule.

**Tests.**

- aspire-tests `FirstRunOnEmptyDatabase_Should_LogNoErrorFromWebServer` watches web-server's
  console from creation, against the suite's ephemeral Postgres. It asserts that the boot logged
  no `fail:`, `crit:` or `42P01` lines.
  - Red before the fix: six 42P01 lines.
  - Green after.
  - A diagnostic run confirmed the race happened: "Site settings seed is waiting for
    web-migrations… (attempt 1/30)" at Information, then no Error.
- web-infrastructure-tests `ef-site-settings-table-probe-tests.cs`, against real Postgres:
  - The probe reports false on an unmigrated database and EF logs nothing at Error.
  - It reports true after Migrate.
  - A control case proves that the plain store read does log Error in the same state.

**Gates:**

- `dev build` 0/0.
- `dev test`: every suite passed, including aspire-tests 12/12 and web-infrastructure-tests
  63/63; one existing skip in web-server-integration-tests.
- `dev template-smoke` SUCCEEDED, after adding the probe file to the `!postgres` exclude.
- `ganda repo audit` passes.
- The maintainer's AppHost was not started, and `dev db nuke` was not run.

**No regressions:** `dev db reset`, the `ef-database-update` command, the production script and
bundle path, and `dev db nuke` are untouched. The AppHost graph is unchanged; only comments were
edited.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release -- --filter-method FirstRunOnEmptyDatabase
cd tests/container-apps/web/web-infrastructure-tests && dotnet test -c Release -- --filter-class Unmigrated_Database
```

**Expect:** both pass: 1/1, then 2/2. The first test boots the full AppHost with an ephemeral
Postgres, so it is a real first run against an empty database. To check the same thing on a dev
volume, use the maintainer steps below.

Maintainer, after merge:
1. Run `dev db nuke --yes`, then `dev run`. The web-server structured logs show no Error. At most
   one or more Information entries read "Site settings seed is waiting for web-migrations to
   create identity.site_settings (attempt n/30)".
2. Restart web-server from the dashboard. It comes back with no deadlock, because there is still
   no wait edge.

### Review disposition

- **Rounds:** 1 · **Effort:** 2 · **Roster:** general
- **Final counts:** bug 0 · suggestion 0 · nit 0 (0 open, 0 fixed, 0 wontfix)
- **Disposition:** clean — no findings; retry-loop edge cases, in-memory template exclusion,
  first-run closed-box test and Design-region reconciliation verified against the code.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`
