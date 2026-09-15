# Round 1 — general
**Date:** 2026-09-15
**Scope reviewed:** branch `task/219-006-template-admin-settings-store-with-entra-sign-in-p` vs `origin/master` (commits `bf2abe32` feat + `03d093ea` Results). Product: site-settings aggregate/store, `IEntraSignInPolicy`, Get/Update site-settings contracts, anonymous Entra offered flag, Settings page Authentication section, challenge/ticket processor wiring, tests, identity guide.

## Summary

The change correctly splits deploy-time Entra configuration (scheme registration / secrets) from runtime admin policy via a singleton `SiteSettings` store, `IEntraSignInPolicy`, SettingsRead/SettingsWrite endpoints, and an anonymous offered boolean for the login CTA. Challenge and ticket processor no longer consult `AllowBootstrap` / `TrustedTenants` from options; policy and offered paths fail closed on an empty store; EF maps `identity.site_settings` with a Version concurrency token; Required passkey mode gates `TimeWarpPage` body; docs and Design regions cover configured-vs-enabled. Dominant residual risk is a second empty-store insert path in the Settings handlers that can permanently swallow the configuration first-run seed.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/settings/get-site-settings/get-site-settings-handler-application.cs:39
- Description: `GetOrCreateDefaultsAsync` persists factory defaults (`EntraSignInEnabled=false`, `EntraAllowBootstrap=false`, empty trusted tenants, Soft) whenever the store is empty. `UpdateSiteSettings` reuses the same helper (`update-site-settings-handler-application.cs:31`). That insert races `SiteSettingsSeeder` / `SiteSettingsSeedHostedService`, which is supposed to copy `Authentication:Entra:Enabled` / `AllowBootstrap` / `TrustedTenants` once so existing `dev entra setup` keeps working. The Get handler Design region admits the failure mode: if this path wins, the seeder Add no-ops and configuration is never consulted again. Concrete case: empty store + `UpdateSiteSettings` with a non-zero `Version` inserts factory defaults, then returns 409 — side effect is a persisted all-false row that blocks config seed. `GetSiteSettingsHandler_Given_.Empty_Store_Should_Create_Defaults` locks the anti-requirement behavior in. Policy/offered paths correctly avoid inventing a row; Settings handlers should too (or must go through the seeder).
- Suggestion: Stop persisting from Get/Update when empty. Prefer return-a-problem / rely on boot seed for Get; for Update, either refuse until seeded or insert from the command only after a successful config seed path. Keep a single empty-store writer (`SiteSettingsSeeder`) so factory defaults cannot beat configuration. Update the handler Design comments and replace `Empty_Store_Should_Create_Defaults` with coverage that empty Get does not block a later config seed.
- Status: open
