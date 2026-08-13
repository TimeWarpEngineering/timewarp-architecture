# Bump SDK to 10.0.400 and outdated NuGet pins

## Description

Machine now has **.NET SDK 10.0.400** installed (also 10.0.302; previews 11.0.100-preview.5/6 present).
Repo still pins **10.0.301** in root `global.json` and every test-project `global.json` (Jaribu MTP
mirrors — timewarp-jaribu#20). CPM is one servicing band behind (`10.0.10` packages vs **10.0.11**).

Inventory from `ganda nuget outdated` on 2026-08-13: **42** outdated pins (2 major, 12 minor, 28
patch). This task is a maintenance bump, **not** a release — do not change `<Version>` /
platform `TimeWarp.*` pins (those stay `2.0.0-beta.15` until a real release PR).

CI already uses `actions/setup-dotnet` `10.0.x`, so the runner will pick 10.0.400 once the pin
moves. Generated-app `global.json` in the template tree must move with the root pin.

## Requirements

- Pin SDK **10.0.400** everywhere the 10.0.301 pin lives (root + all `tests/**/global.json` + any
  template-output copy). Keep `rollForward: latestFeature`. Do **not** adopt .NET 11 previews.
- Take every **safe** outdated pin listed below. Hold or same-major-only where called out.
- `ganda nuget outdated` after the bump should be empty except documented holdouts.
- `dev build` 0/0. `dev test` (or targeted suites if a family is untouched). `dev template-smoke`
  because CPM + SDK pin ship in generated apps.
- `ganda repo audit` clean. No NU1903 suppressions — lift if a new advisory appears.

## Take (aligned with `ganda nuget outdated` Stable, unless noted)

**SDK + analyzers**

| Pin | From | To |
|-----|------|-----|
| `global.json` sdk.version (root + every test/template copy) | 10.0.301 | 10.0.400 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.302 | 10.0.400 |

**Microsoft 10.0.10 → 10.0.11** (one servicing band; keep the set in lockstep)

Microsoft.Extensions.{Configuration.Abstractions, DependencyInjection, DependencyInjection.Abstractions, Http, Options, Options.ConfigurationExtensions, Logging.Configuration, Diagnostics.HealthChecks.EntityFrameworkCore}
Microsoft.AspNetCore.{Authorization, Components.QuickGrid, Components.WebAssembly, Components.WebAssembly.Authentication, Components.WebAssembly.DevServer, Components.WebAssembly.Server, Mvc.Testing, SignalR.Client}
Microsoft.Authentication.WebAssembly.Msal
Microsoft.EntityFrameworkCore{, .Design, .InMemory, .Tools}
System.Formats.Cbor
System.Security.Cryptography.Xml

**Microsoft extensions minor**

| Pin | From | To |
|-----|------|-----|
| Microsoft.Extensions.Http.Resilience | 10.8.0 | 10.9.0 |
| Microsoft.Extensions.ServiceDiscovery | 10.8.0 | 10.9.0 |
| Microsoft.Extensions.ServiceDiscovery.Yarp | 10.8.0 | 10.9.0 |

**Other minors / patches**

| Pin | From | To | Notes |
|-----|------|-----|-------|
| Grpc.AspNetCore + Server.Reflection + Web | 2.80.0 | 2.83.0 | keep the five Grpc.* pins together |
| Grpc.Net.Client + Web | 2.80.0 | 2.83.0 | |
| Roslynator.{Analyzers, CodeAnalysis.Analyzers, Formatting.Analyzers} | 4.15.0 | 4.16.0 | keep the three together |
| Scalar.AspNetCore | 2.16.17 | 2.16.19 | |
| Blazilla | 2.4.0 | 2.4.1 | |
| libphonenumber-csharp | 9.0.35 | 9.0.36 | |
| timewarp-heroicons | 2.0.19 | 2.2.0 | minor; template SPA icons |
| Microsoft.FluentUI.AspNetCore.Components | 5.0.0-rc.4-26180.1 | **5.0.0-rc.5-26219.1** | `Stable` is 4.14.4 — that is a **downgrade**. Stay on the v5 RC train. |
| Microsoft.OpenApi | 2.7.5 | **2.12.0** (same major) | see Holdouts |

## Holdouts (do not take blindly)

| Pin | Why |
|-----|-----|
| **Microsoft.OpenApi 3.10.0** | Task 144: 3.x makes `IOpenApiMediaType.Example` read-only; `Microsoft.AspNetCore.OpenApi` 10.x XmlCommentGenerator still assigns it → **CS0200**. Stay on 2.x. Prefer 2.12.0 over 2.7.6. Revisit only if AspNetCore.OpenApi is built against the 3.x object model. |
| **Microsoft.TypeScript.MSBuild 7.0.0** | Major. Try restore + `dev build`; revert this pin alone if TS targets break. Do not block the rest of the bump. |
| **TimeWarp.Foundation.\* / Modules / Identity / 402 / Architecture.\*** | Release-coupled (task 124). Not this task. |
| **.NET 11 preview packages** (`11.0.0-preview.7.*`) | Out of scope. |
| **FluentUI 4.14.4** | Downgrade off the v5 RC. |

`$(TwArchitecture*PackageId)` "not found" from `ganda nuget outdated` is expected (sourceName-safe
composed IDs) — ignore.

## Checklist

- [ ] Confirm installed SDK is 10.0.400 (`dotnet --list-sdks`)
- [ ] Bump root `global.json` and every `tests/**/global.json` (and template copies) to 10.0.400
- [ ] Bump `Directory.Packages.props` for the Take table (Microsoft 10.0.11 set in one commit-worth of edits)
- [ ] OpenApi → 2.12.0 only (refresh the task-144 comment if the floor moves)
- [ ] FluentUI → 5.0.0-rc.5-26219.1
- [ ] Attempt TypeScript.MSBuild 7.0.0; keep or revert with a Notes line
- [ ] `dotnet restore` + `dev build` 0/0
- [ ] `dev test` (or family suites if some families have zero package graph change — still run if SDK pin changed)
- [ ] `dev template-smoke`
- [ ] `ganda nuget outdated` — only Holdouts remain
- [ ] `ganda repo audit`
- [ ] Reconcile any `#region Design` that names the old OpenApi / FluentUI / SDK pin

## Notes

- Inventory date: 2026-08-13 (`ganda nuget outdated` pasted into the creating session).
- SSH.NET 2026.0.0 and Testcontainers.PostgreSql 4.13.0 are already current (task 194).
- Aspire 13.4.6, Npgsql.EF 10.0.3, FastEndpoints 8.2.0, TimeWarp.State 12.0.0-beta.1 — already current.

## Session

- Created: grok 2026-08-13 (after PR #301 green; user asked for a bump task + new SDK)
