# Round 2 — general (re-verify fix delta)
**Date:** 2026-10-01
**Scope reviewed:** fix delta on top of 219fe923 (analyzer check order + Design region, analyzer tests, spa.ts / web.spa.lib.module.ts Design regions); carried M1–M8.

## Summary

M1–M5 verified fixed: `Analyze` returns on `IsInComponent` before receiver/side-effect matching (behavior unchanged: all guards are conjunctive); new HttpClient Post/Put/Patch/Send and local-storage lifecycle Remove cases are asserted and pass (14/14 `Direct_Side_Effects`); TS Design regions now name the JS onclick caller. M6–M8 remain wontfix with rationale. Full `dotnet build timewarp-architecture.slnx -c Release --no-incremental` 0 warnings / 0 errors. No new defects in the delta.

## Issues

<!-- none -->
