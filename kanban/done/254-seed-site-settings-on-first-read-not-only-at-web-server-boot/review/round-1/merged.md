# Round 1 — merged findings
**Date:** 2026-09-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 4 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/seed-on-read-site-settings-store-application.cs:10
- Description: Design region (and task.md Results) said IsDevelopment is "resolved once at registration"; it is read from IHostEnvironment each time the scoped decorator is built.
- Suggestion: Reword.
- Source: general
- Disposition notes: Reworded to "read when the scoped decorator is built"; task.md Results updated to match.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/identity/seed-on-read-site-settings-store-tests.cs
- Description: Concurrent Development-reseed test does not deterministically exercise the seeder's new `catch (ConcurrencyConflictException)`.
- Suggestion: Add a store whose UpdateAsync conflicts once and assert the conflict is swallowed.
- Source: general
- Disposition notes: Added `Development_Reseed_Losing_Update_Race_Should_Reget_Instead_Of_Throwing` with `ConflictOnceUpdateSiteSettingsStore` (asserts one conflict raised, seeded row returned). Runfile 11/11.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/settings/update-site-settings/update-site-settings-handler-application.cs:7
- Description: Design region pinned "Version 0"; Development reseed lands at Version 1.
- Suggestion: Say "the freshly seeded row".
- Source: general
- Disposition notes: Reworded.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:195
- Description: Comment said ISiteSettingsStore is scoped "under postgres"; now always the scoped decorator.
- Suggestion: Update comment.
- Source: general
- Disposition notes: Comment now names the scoped SeedOnReadSiteSettingsStore decorator.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/site-settings-seed-registration-server.cs:26
- Description: Calling ConfigureServices twice would wrap the decorator in itself (circular resolution).
- Suggestion: Guard.
- Source: general
- Disposition notes: Throws InvalidOperationException if a keyed InnerStoreKey store is already registered; Design region notes it. Integration suite (registers its keyed store after) still 3/3.

### M6 — Severity: nit — Status: fixed
- File: site-settings-entra-sign-in-policy-application.cs:9; update-site-settings-handler-application.cs:9
- Description: Over-long Design-region lines after edits.
- Suggestion: Re-wrap.
- Source: general
- Disposition notes: Re-wrapped.

## Duplicates / conflicts

- None (single reviewer).
