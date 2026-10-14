# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** `global.json`, 22 `tests/**/global.json`, `Directory.Packages.props`, `.config/dotnet-tools.json`, task.md

## Summary

This is a version bump only. The root SDK pin and every test `global.json` now say `11.0.100-rc.2.26504.105`. Every shared-framework `Microsoft.*` / `System.*` pin and `Microsoft.CodeAnalysis.NetAnalyzers` now say RC2.

EF Core is held on RC1: `Microsoft.EntityFrameworkCore*`, `HealthChecks.EntityFrameworkCore`, and the `dotnet-ef` tool. The reason is that `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0-rc.1.1 pins EF Core to exactly rc.1. This hold is explained in a `Directory.Packages.props` comment and in task Results.

What I re-checked:

- `git grep` finds no other non-EF RC1 pin. The leftover `rc.1` strings are historical "verified against" notes or migration `ProductVersion` annotations, which are history and not pins.
- `dotnet --version` prints `11.0.100-rc.2.26504.105`.
- `./bin/dev build` gave 0 warnings and 0 errors.

Risk is low.

## Issues

None.
