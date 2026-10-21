# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** `master...HEAD` (3ac5aef2b), `source/foundation/timewarp-contracts/timewarp-contracts.csproj`, and the reference in `source/Directory.Build.props:36-38` that it updates

## Summary

The diagnosis holds. `SetTargetFramework=TargetFramework=net11.0` on the net10.0 inner build sent the analyzer csproj a `TargetFramework` global property. The solution build and the net11.0 inner build send none. MSBuild therefore had two project instances that both wrote `bin/Release/net11.0/…convention-analyzers.dll`, and the SDK's own comment in `Microsoft.Common.CurrentVersion.targets` (`_GetProjectReferenceTargetFrameworkProperties`) calls this case out as a parallel-build conflict. The new metadata uses the documented hooks. `SkipGetTargetFrameworkProperties` still skips the upward TFM check, and `GlobalPropertiesToRemove` flows into `RemoveProperties` on the `Build` / `GetTargetPath` MSBuild calls, so the net10.0 reference resolves to the same instance as every other consumer. This is the only `SetTargetFramework` / multi-target site in the repo. There is no retry and no `continue-on-error`, and the change silences no diagnostic. No issues raised.

## Verification (re-run by reviewer, not taken from the implementer's report)

- `dotnet msbuild timewarp-contracts.csproj -t:ResolveProjectReferences -getItem:Analyzer` for net10.0 and for net11.0: both resolve the same `bin/Release/net11.0/timewarp-architecture-convention-analyzers.dll`, so net10.0 still runs the convention analyzers.
- `dotnet build timewarp-contracts.csproj -c Release -m -v:n`: one analyzer project instance and one `convention-analyzers ->` line. 0 warnings, 0 errors.
- `./bin/dev build` (full solution): exit 0, exactly one `convention-analyzers ->` line, 0 warnings, 0 errors, no CS8034.
- Not re-run: `dev template-smoke` and `ganda repo audit`. The implementer reports both passing. The change does not touch template symbols or `#if` regions.

## Notes (not findings)

- With `SkipGetTargetFrameworkProperties`, `RuntimeIdentifier` / `SelfContained` are not removed the way the SDK's RID-agnostic path removes them. This only matters if `timewarp-contracts` itself is built with `-r`, which no job does. Even then the analyzer output path would get a RID segment and would not collide. The old `SetTargetFramework` path behaved the same way, so this is not a regression.
- PR CI re-runs of `ci (publish)` / `ci (playwright)` are still open on the checklist. The host proves them after it opens the PR.

## Issues

None.
