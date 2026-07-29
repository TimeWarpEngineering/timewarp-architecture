# General review — round 1

**Task:** 131-003 extract shared template-smoke harness; derive rewrite-scan suffixes from props SSOT  
**Reviewer:** general  
**Scope:** `tools/dev-cli/services/template-smoke-harness.cs`, `endpoints/template-smoke-command.cs`, `endpoints/template-publish-smoke-command.cs`, `tools/dev-cli/Directory.Build.props`  
**Date:** 2026-07-29

## Summary

Shared harness extraction is clean and matches the task plan. Both smoke commands own only their orchestration (local pack/feed vs nuget.org isolation, pin rewrite vs pin assert, package-mode vendored-tree check) and delegate rewrite asserts, props SSOT suffix derivation, pin helpers, monorepo namespace pre-scan, and generate/restore/build to `TemplateSmokeHarness`. `Directory.Build.props` correctly compiles `services/**/*.cs`.

All seven verification gates pass:

| Gate | Result |
|------|--------|
| Zero hand `ForbiddenRewrittenPackageFragments` in either command | Pass — fragments come from `PlatformPackagePropsSsot.ToForbiddenRewrittenFragments` / nupkg exclude helpers only |
| Suffixes derived from `msbuild/timewarp-platform-packages.props` fail-closed | Pass — missing props, zero matches, or missing minimal set (`Analyzers`/`Attributes`/`Generators`/`TypedIds`) hard-fail with `ExitCode = 1` |
| Publish-smoke monorepo namespace pre-scan | Pass — `Harness.AssertNoUnsafePlatformNamespaceLiterals()` before network wait |
| `IsBinObjOrArtifacts` on tree walks | Pass — monorepo scan, package-id rewrite scan, and rewritten-token collect all use it (bin/obj/**artifacts**) |
| InstallTemplate nupkg filter uses derived fragments | Pass — `IsExcludedPlatformNupkgFileName` → `ToNupkgExcludeFragments` (e.g. `.Analyzers.`) |
| No duplicated `SmokeOneAsync` / assert helpers in commands | Pass — thin command wrappers call harness; assert helpers live only in harness |
| No silent matrix / pin regression | Pass — matrices `SmokeDefault`/`SmokeNoPostgres` and `PublishSmokeDefault`/`PublishSmokeNoPostgres`; `PlatformPinIncludeFragments` still covers `TwArchitecture*`, `TimeWarp.Foundation.`, `TimeWarp.Modules`, `TimeWarp.Identity` |

Props SSOT derivation aligns with current `timewarp-platform-packages.props` (first segment after `.Architecture.` yields Analyzers, Generators, Attributes, TypedIds). Pin fragments remain a deliberate single hand list (task notes), not multi-site ForbiddenRewritten arrays.

## Issues

_None._
