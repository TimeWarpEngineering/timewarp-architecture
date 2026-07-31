# Update outdated NuGet packages (patch and minor wave; evaluate majors)

## Description

`ganda nuget outdated` (2026-07-31) reports 36 outdated of 117: 22 patch (dominated by the
.NET 10.0.9 → 10.0.10 servicing train), 11 minor (OpenTelemetry 1.16 → 1.17,
ServiceDiscovery/Http.Resilience 10.7 → 10.8, Identity.Web 4.14.2, NET.Test.Sdk 18.8.1,
FluentUI Icons 4.14.4, Scalar 2.16.17, libphonenumber 9.0.35, timewarp-simple-icons 16.27.1,
NetAnalyzers 10.0.302), 3 major (MessagePack 2.5.302 → 3.1.8, Microsoft.OpenApi 2.7.5 → 3.9.0,
Microsoft.TypeScript.MSBuild 6.0.3 → 7.0.0).

## Requirements

1. **Patch + minor**: bump all in `Directory.Packages.props`. Exceptions/traps:
   - `Microsoft.FluentUI.AspNetCore.Components` STAYS `5.0.0-rc.4-26180.1` (deliberate v5 RC,
     epic 059) — only the `.Icons` package takes 4.14.4.
   - Ignore the report's "Prerelease" column entirely (11.0.0-preview = .NET 11; stay on 10.x).
2. **Majors — verify before bumping, one commit each; defer with documented reason if broken**
   (never backward-pin; check for package splits per house rule):
   - `MessagePack` 2 → 3: find actual consumers first (grep csproj usage; check whether
     TimeWarp.State / SignalR pull it transitively vs direct use); breaking changes in v3
     (analyzer, source gen). Bump only if consumers compile + tests pass.
   - `Microsoft.OpenApi` 2 → 3.9: check `FastEndpoints.OpenApi` 8.2.0's dependency range —
     if it caps at 2.x, bumping breaks restore; defer and note upstream expectation.
   - `Microsoft.TypeScript.MSBuild` 6 → 7: web-spa TS pipeline must keep compiling TS →
     `wwwroot/js` (history: a broken TS pipeline silently killed counter JS interop). Verify
     compiled JS output exists and is unchanged/equivalent post-bump.
3. **Gates**: `dev build` 0/0; full `dev test` (serialized, fixed ports); `dev template-smoke`
   (all three matrices); `ganda repo audit` clean.
4. Branch off dev; ships as next release after PR #294 merges (do not push into #294's head).

## Checklist

- [x] Patch + minor bumps applied (33; FluentUI main verified untouched at 5.0.0-rc.4)
- [x] MessagePack 3.1.8 BUMPED — sole consumer is transitive-lift pin for StreamJsonRpc (floor-only range); aspire-tests 7/7 against it
- [x] Microsoft.OpenApi DEFERRED — floor-only ranges restore, but Microsoft.AspNetCore.OpenApi 10.0.x XmlCommentGenerator emits CS0200 against 3.x (IOpenApiMediaType.Example read-only); evidence in props comment
- [x] TypeScript.MSBuild DEFERRED — 7.0.0 rewrite removes target chain web-spa hooks (MSB4057); revert verified byte-identical JS emission (4 files md5-matched)
- [x] Gates: build 0/0; full dev test ~652 tests 0 failed; template-smoke ×3 SUCCEEDED; audit 23/23
- [x] Kanban mutations committed

## Results

Branch `Claude/2026-07-31/task-144-nuget-update-wave` (4 commits) fast-forwarded into dev;
version bumped to 2.0.0-beta.12 (54441158). 33 patch/minor bumps + MessagePack 2.5.302→3.1.8.
Two majors deferred with verification evidence recorded as comments beside the pins in
Directory.Packages.props: Microsoft.OpenApi (blocked by ASP.NET 10.0.x's own generated code —
revisit when Microsoft.AspNetCore.OpenApi supports OpenApi 3.x) and Microsoft.TypeScript.MSBuild
(v7 tsc-go rewrite removes the classic target chain; web-spa PrepareForBuildDependsOn needs
reworking first). Orchestrator verification: diff confirmed pins-only, no VersionOverride,
FluentUI main untouched; post-merge dev build 0/0, audit 23/23, check-version safe.
Follow-up filed in ganda: `nuget outdated` fails to surface newer patches within the current
major when the latest version is a different major (OpenApi 2.7.5→2.11.0 not shown).

## Session

- Created: fe3c947a-a536-495b-88dd-794216a1fa8e (2026-07-31)
