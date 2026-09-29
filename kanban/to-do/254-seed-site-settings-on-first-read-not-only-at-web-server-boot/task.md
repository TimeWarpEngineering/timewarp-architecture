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

- [ ] Single read seam that seeds on empty; all four readers use it
- [ ] isDevelopment propagated; concurrent seed safe; 42P01 handled
- [ ] 503 "until seeded" path removed; Design regions reconciled
- [ ] Tests
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Origin: `dev db reset` on 2026-09-29 left site settings empty until web-server restarted.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-29)
