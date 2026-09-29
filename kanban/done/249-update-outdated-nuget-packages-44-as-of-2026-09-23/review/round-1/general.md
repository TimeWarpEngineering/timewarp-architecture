# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** same as framework

## Summary

Pin-only CPM refresh plus a small forward migration to TimeWarp.State 12.0.0-beta.5. Platform
release pins (`TimeWarp.Foundation.*`, `TimeWarp.Modules`, `TimeWarp.Identity`, `TimeWarp.402`,
`$(TwArchitecture*PackageId)`) have no diff lines; no `VersionOverride`. The `SSH.NET` CPM pin and
both direct references were removed together (no orphan `PackageVersion`); `TestCaller` and
`MemberNameToCamelCase` have no remaining source references. `JsonNamingPolicy.CamelCase.ConvertName`
produces the same keys as the old helper for the PascalCase member names used (`Guid`, `Name`,
`Count`, `WeatherForecasts`). Design regions in all four debug seeders were reconciled with the
change. OpenApi deferral comment is accurate and follow-up task 257 exists on `origin/master`
(`kanban/to-do/257-…`). Re-verified: `dotnet build source/container-apps/web/projects/web-spa -c Release`
→ 0 warnings / 0 errors. Low risk.

## Issues

None.
