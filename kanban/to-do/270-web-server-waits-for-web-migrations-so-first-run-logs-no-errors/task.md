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

- [ ] `WaitForCompletion` re-tested on Aspire 13.6 against (a), (b) and (c); kept, or rejected
      with evidence
- [ ] If rejected: the seeder probes the table quietly, so no EF Error on first run; other startup
      readers checked
- [ ] A test that proves a first run against an empty database logs no Error from the seeder path
- [ ] AppHost Design region reconciled with the 13.6 outcome
- [ ] Gates: `dev build` 0/0, `dev test` (including aspire-tests and the closed-box suites),
      `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start the maintainer's AppHost or run `dev db nuke --yes`. Closed-box test suites
      are fine
- [ ] Implementation review; host `open-pr`

## Session

- Created: 252821 (2026-10-01)

## Notes

- Evidence: Aspire structured logs 2026-10-02, web-server logIds 65 and 76 (Error) and 77 (seeder
  Warning, attempt 1/30), immediately after `dev db nuke --yes` then `dev run`.
- Related: task 155 (removed the wait edges), task 254 (site settings seed on first read),
  task 266/269 (`dev db nuke`).
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*

Maintainer, after merge:
1. Run `dev db nuke --yes`, then `dev run`. The web-server structured logs show no Error.
2. Restart web-server from the dashboard. It comes back, with no deadlock.
