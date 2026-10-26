# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** same as framework (branch vs merge-base `0c42c3c59`)

## Summary

The change moves the root `global.json` and all 22 `tests/**/global.json` files to SDK
`11.0.100-rc.3.26522.148`. It moves every shared-framework `Microsoft.*` / `System.*` pin and
`Microsoft.CodeAnalysis.NetAnalyzers` to the RC3 train. The EF Core family, Npgsql and `dotnet-ef`
stay on RC2, and the props comment records why. Risk is low. It is a version-only change with no
product code edits and no suppressions added.

## Verification (re-run by the reviewer, not taken from Results)

- Remaining RC2 strings (`rg`, kanban and bin/obj excluded) are only the EF hold family
  (`Directory.Packages.props:113-119`), `dotnet-ef` (`.config/dotnet-tools.json:13`), the
  comments that record the hold, and migration `ProductVersion` annotations (left as history,
  as required).
- All 23 `global.json` files pin `11.0.100-rc.3.26522.148`. `dotnet --version` prints that SDK.
- nuget.org: Npgsql.EntityFrameworkCore.PostgreSQL latest is `11.0.0-rc.2` (no RC3), so the hold is
  correct. `Microsoft.CodeAnalysis.NetAnalyzers` `11.0.100-rc.3.26522.148` exists.
  `Microsoft.AspNetCore.OpenApi` RC3 declares `Microsoft.OpenApi [3.10.2, 4.0.0)`.
  FastEndpoints.OpenApi `9.0.0-beta.6` net11.0 still depends on AspNetCore.OpenApi
  `11.0.0-rc.2.26504.105`. Latest Microsoft.OpenApi 2.x is `2.13.0`. Extensions monthly
  (`Http.Resilience`) latest is still `10.11.0`. Every "Re-checked task 420" claim holds.
- CI `setup-dotnet` uses `11.0.x` + `dotnet-quality: preview`, so it picks up RC3. No workflow
  edit is needed.
- Gates re-run in this worktree: `dev build --clean` 0 warnings / 0 errors; `dev test` 23 of 23
  projects passed; `dev template-smoke` SUCCEEDED. The tree is clean afterwards (only `review/`
  is untracked).

## Goal restatement vs task

The restatement matches the task's Description and Requirements (SDK, packages, the
EF-follows-Npgsql rule, the comments, the breaking-change review, leaving history alone, version,
gates). No misread.

## Issues

None.
