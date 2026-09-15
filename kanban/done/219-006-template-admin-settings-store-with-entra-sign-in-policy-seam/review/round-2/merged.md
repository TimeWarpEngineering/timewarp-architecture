# Round 2 — merged findings
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
- File: source/container-apps/web/features/settings/get-site-settings/get-site-settings-handler-application.cs
- Description: `GetOrCreateDefaultsAsync` persisted factory defaults on an empty store and raced `SiteSettingsSeeder`, so first-run copy from `Authentication:Entra` could be swallowed.
- Suggestion: Stop inserting from Get/Update; seed only via `SiteSettingsSeeder` in `StartingAsync`.
- Source: general (round 1); re-verified round 2
- Disposition notes: `GetOrCreateDefaultsAsync` removed. Empty Get/Update return 503 `Site settings not initialized` without inserting. Seed runs in `IHostedLifecycleService.StartingAsync`. Tests: empty Get/Update do not insert; seeder still copies config after empty Get.

## Duplicates / conflicts

- None. Round 2 raised no new findings. Prior M1 carried with updated status.

## Resolved prior

- M1 fixed on this task id (round-1 open → round-2 fixed).
