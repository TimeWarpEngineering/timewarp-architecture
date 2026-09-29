# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** branch vs master

## Summary
I found no correctness bugs. The DI lifetimes are sound. The decorator is scoped. Every consumer is scoped or resolves through a scope: `SiteSettingsEntraSignInPolicy`, `EntraTicketProcessor`, the handlers, and the hosted service, which calls `CreateScope`. No singleton captures `ISiteSettingsStore`.

Other items I checked by reading the code:
- The keyed move keeps the lifetime and shape (instance, factory, or type) for the in-memory singleton and for the scoped `EfSiteSettingsStore`.
- The registration call sits after the `#if postgres` block, so both template branches get the same wiring.
- The seeder takes the keyed inner store, so there is no recursion.
- The EF Add race detaches the entity and throws `InvalidOperationException`, which the seeder catches before it re-Gets.
- The 42P01 check is the one shared string check.

The findings below are accuracy and test-coverage suggestions.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/features/identity/seed-on-read-site-settings-store-application.cs:10
- Description: The Design region, and task.md Results ("resolved at registration"), say `IsDevelopment` is "resolved once at registration". The code does not do that. In `site-settings-seed-registration-server.cs:43` the scoped factory calls `GetRequiredService<IHostEnvironment>().IsDevelopment()` each time it builds a decorator, which is once per scope. The behavior is correct, but the region describes a different mechanism.
- Suggestion: Reword to "resolved from IHostEnvironment when the scoped decorator is built", or capture the value once if that was the intent.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/identity/seed-on-read-site-settings-store-tests.cs (Concurrent_Empty_Reads_With_Development_Reseed_Should_Not_Conflict)
- Description: This test is meant to cover the new `catch (ConcurrencyConflictException)` in `SiteSettingsSeeder` (site-settings-seeder-application.cs:120). It never forces that path. After the forced Add race, the winner may finish its reseed Update before the loser re-Gets. The loser then reads Version 1 and updates without a conflict. The test passes on both interleavings and asserts only non-null reads, so the new catch branch is never deterministically executed.
- Suggestion: Add a store whose `UpdateAsync` throws `ConcurrencyConflictException` once, or gate the two Updates the way AddRace gates Adds. Assert that the conflict was raised and swallowed, and that the result is the re-Get row.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/settings/update-site-settings/update-site-settings-handler-application.cs:7
- Description: The Design region says an update after the row was deleted "applies against the freshly seeded Version 0". In Development with `ReseedSiteSettings=true`, the lazy seed also runs the reseed Update, so the row lands at Version 1. The unit test `Development_Reseed_Flag_Should_Reach_Lazy_Seed` asserts exactly that. The wording is only true outside that case.
- Suggestion: Say "the freshly seeded row" instead of pinning Version 0.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-server/program.cs:195
- Description: The comment "Scoped: ISiteSettingsStore is scoped under postgres (EfSiteSettingsStore)" is now incomplete. The registered `ISiteSettingsStore` is always the scoped `SeedOnReadSiteSettingsStore`, with or without postgres.
- Suggestion: Update to "ISiteSettingsStore is scoped (SeedOnReadSiteSettingsStore decorator, task 254)".
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/features/identity/site-settings-seed-registration-server.cs:26
- Description: `ConfigureServices` cannot safely be called twice. A second call would treat the decorator factory as the "current" store, move it to `InnerStoreKey`, and wrap it again. That decorator factory resolves `InnerStoreKey`, and the last keyed registration is now itself, so resolution becomes circular. Nothing calls it twice today.
- Suggestion: Optionally guard at the top: throw or return if a keyed `InnerStoreKey` registration already exists.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/features/identity/site-settings-entra-sign-in-policy-application.cs:9; source/container-apps/web/features/settings/update-site-settings/update-site-settings-handler-application.cs:9
- Description: After the edits, two Design-region lines are much longer than the surrounding wrap: 112 and 145 characters.
- Suggestion: Re-wrap to match the rest of the region.
- Status: open
