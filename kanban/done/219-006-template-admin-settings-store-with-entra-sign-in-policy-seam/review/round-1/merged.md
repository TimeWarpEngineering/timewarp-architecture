# Round 1 — merged findings
**Date:** 2026-09-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/settings/get-site-settings/get-site-settings-handler-application.cs:39
- Description: `GetOrCreateDefaultsAsync` persists factory defaults (`EntraSignInEnabled=false`, `EntraAllowBootstrap=false`, empty trusted tenants, Soft) whenever the store is empty. `UpdateSiteSettings` reuses the same helper. That insert races `SiteSettingsSeeder` / `SiteSettingsSeedHostedService`, which is supposed to copy `Authentication:Entra:Enabled` / `AllowBootstrap` / `TrustedTenants` once so existing `dev entra setup` keeps working. If this path wins, the seeder Add no-ops and configuration is never consulted again. Concrete case: empty store + `UpdateSiteSettings` with a non-zero `Version` inserts factory defaults, then returns 409 — side effect is a persisted all-false row that blocks config seed. `GetSiteSettingsHandler_Given_.Empty_Store_Should_Create_Defaults` locks the anti-requirement behavior in. Policy/offered paths correctly avoid inventing a row.
- Suggestion: Stop persisting from Get/Update when empty. Prefer return-a-problem / rely on boot seed for Get; for Update, refuse until seeded. Keep a single empty-store writer (`SiteSettingsSeeder`) so factory defaults cannot beat configuration. Seed before Kestrel accepts requests (`IHostedLifecycleService.StartingAsync`). Update handler Design comments and replace `Empty_Store_Should_Create_Defaults` with coverage that empty Get does not block a later config seed.
- Source: general
- Disposition notes: Deleted `GetOrCreateDefaultsAsync`. Get/Update return 503 `Site settings not initialized` without inserting. `SiteSettingsSeedHostedService` now `IHostedLifecycleService.StartingAsync`. Tests: empty Get/Update do not insert; seeder still copies config after empty Get.

## Duplicates / conflicts

- None (single reviewer).
