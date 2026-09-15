# Round 2 — general
**Date:** 2026-09-15
**Scope reviewed:** re-verify M1 + uncommitted fix delta (Get/Update empty-store 503, StartingAsync seed)

## Summary

Prior M1 is fixed. `GetOrCreateDefaultsAsync` is gone from product code; Get and Update both `GetAsync` and return `SiteSettingsProblems.NotInitialized()` (503) without inserting when the store is empty. `SiteSettingsSeedHostedService` is now `IHostedLifecycleService` and seeds in `StartingAsync` (before Kestrel accepts requests), and `SiteSettingsSeeder.AddAsync` remains the only product empty-store writer. Tests cover empty Get/Update no-insert and that an empty Get still allows a later config seed. No new defects on the fix delta.

## Issues

