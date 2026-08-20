# Round 2 — general
**Date:** 2026-08-20
**Scope reviewed:** M1 re-verify + test-fix delta 403022cd

## Summary

M1 is fixed. `Given_Named_Role_Types_Are_Not_Skipped` is a real do-not-skip lock: six expected TWA0023s whose identifiers do not `EndsWith` the stem (`log`/`Logger`, `dt`/`DateTime`, `id`/`Guid`, `ts`/`TimeSpan`, `ct`/`CancellationToken`, `code`/`HttpStatusCode`). Skipping any of those types (or `TypeKind.Enum` in `ShouldSkipType`) drops a diagnostic and the test fails; spans match the raw-string source after indent strip. `Given_Enum_Members_Are_Skipped` stays clean for `Color { Red, Blue }` — a diagnostic on either member fails it. Commit `403022cd` is test-only (`test(analyzers): lock TWA0023 do-not-skip types`); no new product defects.

## Issues

None.
