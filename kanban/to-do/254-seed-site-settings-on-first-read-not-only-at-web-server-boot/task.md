# Seed site settings on first read, not only at web-server boot

## Description

The site-settings row (Entra enable / AllowBootstrap, PasskeyPromptMode) is written only by
`SiteSettingsSeedHostedService` (`site-settings-seed-hosted-service-server.cs`) during web-server
startup. `dev db reset` drops and re-migrates the database while web-server keeps running, so the
row stays missing until web-server restarts: Settings Get/Update return 503 and the Entra
sign-in policy reads an empty store. Everything else a reset needs comes back through migrations
(role permissions are `InsertData`). Decision (Steve, 2026-09-29): seed on first read so a missing
row is recreated on demand — removes the boot-ordering dependency for every path that empties the
table, not just the dev command.

## Requirements

- Every reader of site settings goes through `SiteSettingsSeeder.GetOrSeedAsync` (or an equivalent
  single seam) instead of `ISiteSettingsStore.GetAsync` directly. Current direct readers:
  - `features/settings/get-site-settings/get-site-settings-handler-application.cs`
  - `features/settings/update-site-settings/update-site-settings-handler-application.cs`
  - `features/identity/get-entra-sign-in-offered/get-entra-sign-in-offered-handler-application.cs`
  - `features/identity/site-settings-entra-sign-in-policy-application.cs`
  Consider making the seam the store's own read (a decorator over `ISiteSettingsStore`) so a new
  reader cannot bypass it — pick one and record why in the Design region.
- `isDevelopment` must reach the lazy path with the same meaning the hosted service passes today
  (`IHostEnvironment.IsDevelopment()`).
- Concurrency: the seeder already handles "concurrent first-boot Add races re-Get"; keep that for
  lazy seeding under concurrent requests (test it).
- Keep the boot-time hosted service (it still makes the first request fast and keeps the
  drift-warning log on startup), but the 503 "until seeded" path goes away — remove or reword it and
  the Design regions that describe it (`site-settings-seed-hosted-service-server.cs`,
  `site-settings-seeder-application.cs`, the Get/Update handlers).
- Postgres: the lazy path must also ride out 42P01 (table not migrated yet) the same way the hosted
  service does, or fail with a clear problem, never a 500.
- Tests: store emptied while the host runs ⇒ next Settings Get returns the seeded values (not 503);
  Entra sign-in-offered reads the seeded policy after the row is deleted; two concurrent reads on an
  empty store produce one row; drift warning still logged at boot.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host). Record the
  manual `dev db reset` → Settings check as not performed.

## Checklist

- [x] Single read seam that seeds on empty; all four readers use it
- [x] isDevelopment propagated; concurrent seed safe; 42P01 handled
- [x] 503 "until seeded" path removed; Design regions reconciled
- [x] Tests
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Origin: `dev db reset` on 2026-09-29 left site settings empty until web-server restarted.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Results

**Seam — decorator over `ISiteSettingsStore`** (`features/identity/seed-on-read-site-settings-store-application.cs`,
`SeedOnReadSiteSettingsStore`). Chosen over "every reader calls `SiteSettingsSeeder`" because a
new reader taking `ISiteSettingsStore` cannot bypass it, and the Settings handlers keep their
`ISiteSettingsStore` dependency (no TWA0009 edge into Identity.Application). All four readers
(Get/Update settings, GetEntraSignInOffered, SiteSettingsEntraSignInPolicy) get it by DI with no
code change beyond problem naming.

- `GetAsync`: inner read; on null → `SiteSettingsSeeder.GetOrSeedAsync(isDevelopment)` — the
  same call the boot hosted service makes, so an emptied store is treated like a first boot.
  `isDevelopment` = `IHostEnvironment.IsDevelopment()` resolved at registration.
- Concurrency: seeder's Add-race re-Get kept; reseed `UpdateAsync` losing the same race now
  re-Gets instead of surfacing `ConcurrencyConflictException`.
- 42P01: read returns null + warning log (no per-request retry). Readers' null path is now
  "unavailable": Settings Get/Update → 503 **"Site settings unavailable"** (table not migrated;
  replaces "not initialized / until seeded"); offered → false; policy → fail-closed refuse.
  Hosted service keeps its bounded boot retry (shared `SiteSettingsSeeder.IsUndefinedTable`).
- Wiring: `SiteSettingsSeedRegistration.ConfigureServices` (server layer) called in
  `program.cs` after `PostgresDbModule` — moves the backend registration to keyed
  `SeedOnReadSiteSettingsStore.InnerStoreKey` (lifetime/shape preserved), registers the seeder
  over the inner store, the scoped decorator as `ISiteSettingsStore`, and the hosted service.
- Boot hosted service kept (fast first request, Development reseed once per boot, drift warnings).
- Design regions reconciled: seeder, hosted service, Get/Update handlers, problems, offered
  handler, policy.

**Tests**
- New `features/identity/seed-on-read-site-settings-store-tests.cs` (10): empty read seeds;
  existing row unchanged; emptied store re-seeds; two concurrent empty reads → one row (Add
  race forced by a gate); concurrent + Development reseed does not conflict; 42P01 → null +
  warning; other failures propagate; isDevelopment reaches reseed (and is ignored outside Dev);
  Update passes through.
- Get/Update/offered runfiles: empty-store 503 tests replaced — emptied store returns seeded
  values; unmigrated table → 503 "Site settings unavailable"; update on emptied store seeds then
  updates; deleted row → offered reads configuration-seeded policy.
- New in-proc HTTP suite `tests/.../web-server-integration-tests/features/settings/site-settings-seed-on-read-tests.cs`
  (3): boot logs AllowBootstrap drift warning; store emptied while host runs → `GET api/settings`
  200 with seeded values (not 503); `GET api/identity/entra/offered` re-seeds the deleted row.

**Gates**: `dev build` 0 warnings / 0 errors · `dev test` all suites passed (0 failed) ·
`dev template-smoke` SUCCEEDED · `ganda repo audit` passes (after `--fix --checks bin-dev` built
the gitignored `bin/dev`). **No AppHost started.** Manual `dev db reset` → Settings check: **not
performed** (would require a running AppHost).

### How to validate

**Smoke**

```bash
dotnet run source/container-apps/web/features/identity/seed-on-read-site-settings-store-tests.cs
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Given_Emptied_Store_
```

Manual (with an AppHost running, operator only): `dev db reset`, then open /Admin/Authentication
(or `GET /api/settings` as an admin) without restarting web-server.

**Expect**

- Runfile: 10/10 passed; integration filter: 3/3 passed.
- Manual: Settings load with configuration-seeded values (Entra enable/AllowBootstrap from
  `Authentication:Entra`, PasskeyPromptMode Soft, Version 0) — no 503; the Microsoft 365 sign-in
  offer follows the seeded policy.

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-29)
