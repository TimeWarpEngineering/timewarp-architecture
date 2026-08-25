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
| **Microsoft.OpenApi 3.x** (3.10.2 on 2026-08-25) | Task 144: 3.x makes `IOpenApiMediaType.Example` read-only; `Microsoft.AspNetCore.OpenApi` 10.x XmlCommentGenerator still assigns it → **CS0200**. Stay on 2.x. Took **2.12.0** (same major). Revisit only if AspNetCore.OpenApi is built against the 3.x object model. |
| **Microsoft.TypeScript.MSBuild 7.x** | Attempted **7.0.0** (restore OK; `web-spa` Release build **MSB4057** `CheckFileSystemCaseSensitive` missing). Reverted to **6.0.3**. Latest on 2026-08-25 is 7.0.1 (same rewrite). Do not block the rest of the bump. |
| **TimeWarp.Foundation.\* / Modules / Identity / 402 / Architecture.\*** | Release-coupled (task 124). Not this task. Pins already `2.0.0-beta.16`. |
| **.NET 11 preview packages** | Out of scope. |
| **FluentUI 4.14.4** | Downgrade off the v5 RC. Icons stay 4.14.4. |

**Post-inventory leftovers (2026-08-25 vs 2026-08-13 Take table)** — not in the original Take list; left for a later bump:

Aspire 13.4.6→13.5.2, FastEndpoints 8.2.0→8.3.0, OpenTelemetry 1.17.0→1.18.0, Testcontainers.PostgreSql 4.13.0→4.14.0, protobuf-net.Grpc 1.2.x→1.3.14, Microsoft.CodeAnalysis.* 5.6.0→5.9.0, Roslynator 4.16.0→5.0.0 (major) / 4.16.1 (patch), Scalar.AspNetCore 2.16.19→2.17.1, libphonenumber-csharp 9.0.36→9.0.37, Microsoft.OpenApi 2.12.0→2.12.2 (same-minor of what we took).

`$(TwArchitecture*PackageId)` "not found" from `ganda nuget outdated` is expected (sourceName-safe
composed IDs) — ignore.

## Checklist

- [x] Confirm installed SDK is 10.0.400 (`dotnet --list-sdks`)
- [x] Bump root `global.json` and every `tests/**/global.json` (and template copies) to 10.0.400
- [x] Bump `Directory.Packages.props` for the Take table (Microsoft 10.0.11 set in one commit-worth of edits)
- [x] OpenApi → 2.12.0 only (refresh the task-144 comment if the floor moves)
- [x] FluentUI → 5.0.0-rc.5-26219.1
- [x] Attempt TypeScript.MSBuild 7.0.0; keep or revert with a Notes line
- [x] `dotnet restore` + `dev build` 0/0
- [x] `dev test` (or family suites if some families have zero package graph change — still run if SDK pin changed)
- [x] `dev template-smoke`
- [x] `ganda nuget outdated` — only Holdouts remain
- [x] `ganda repo audit`
- [x] Reconcile any `#region Design` that names the old OpenApi / FluentUI / SDK pin

## Notes

- Inventory date: 2026-08-13 (`ganda nuget outdated` pasted into the creating session).
- SSH.NET 2026.0.0 and Testcontainers.PostgreSql 4.13.0 are already current (task 194).
- Aspire 13.4.6, Npgsql.EF 10.0.3, FastEndpoints 8.2.0, TimeWarp.State 12.0.0-beta.1 — already current at inventory; some have newer versions on 2026-08-25 (see Holdouts post-inventory leftovers).
- TypeScript.MSBuild 7.0.0 restore succeeded; `dotnet build web-spa -c Release` failed MSB4057 (`CheckFileSystemCaseSensitive` does not exist). Reverted to 6.0.3. Comment in `Directory.Packages.props` records the re-attempt.

## Results

Pinned SDK **10.0.400** (`rollForward: latestFeature`) on root `global.json` and all 19 `tests/**/global.json` copies (template output is that root file — generated SmokeDefault `global.json` is 10.0.400). Took the 2026-08-13 Take table in `Directory.Packages.props`: Microsoft 10.0.10→10.0.11 lockstep, Resilience/ServiceDiscovery 10.9.0, Grpc 2.83.0, Roslynator 4.16.0, Scalar 2.16.19, Blazilla 2.4.1, libphonenumber-csharp 9.0.36, timewarp-heroicons 2.2.0, FluentUI **5.0.0-rc.5-26219.1**, OpenApi **2.12.0**, NetAnalyzers 10.0.400. Did **not** bump `<Version>` / TimeWarp platform pins.

TypeScript.MSBuild 7.0.0 reverted after MSB4057. OpenApi CPM comment refreshed from 10.0.9 / 2.7.5 floor to 10.0.x / 2.12.0. Identity Design regions that named `App.Ref` / `NETCore.App.Ref` 10.0.10 now say 10.0.11.

Gates: `dotnet restore`; `dotnet build timewarp-architecture.slnx -c Release` **0/0**; `dotnet run tools/dev-cli/dev.cs -- test` all projects passed (2 skipped, pre-existing); `dotnet run tools/dev-cli/dev.cs -- template-smoke` **SUCCEEDED** (SmokeDefault / SmokeNoPostgres / SmokeNoApi); `ganda repo audit` exit 0 (pre-existing `memsearch-scaffold` advisory only). `ganda nuget outdated` remainder is the Holdouts table plus post-inventory leftovers listed there.

### How to validate

**Smoke**

```bash
dotnet --list-sdks
# Expect: 10.0.400 is present (also 10.0.301/302 and 11 previews may be installed)

rg '"version": "10.0.301"' --glob '**/global.json'
# Expect: no matches

rg '"version": "10.0.400"' --glob '**/global.json'
# Expect: root + every tests/**/global.json

dotnet restore
dotnet build timewarp-architecture.slnx -c Release
# Expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run tools/dev-cli/dev.cs -- test
# Expect: Tests completed successfully!

dotnet run tools/dev-cli/dev.cs -- template-smoke
# Expect: Template smoke SUCCEEDED; generated-app global.json sdk.version 10.0.400

ganda nuget outdated
# Expect: leftover rows are Holdouts (OpenApi 3.x, TypeScript.MSBuild 7.x, TimeWarp platform,
#         FluentUI 4.14.4, .NET 11) plus post-inventory leftovers documented on this task

ganda repo audit
# Expect: exit 0 (blocking checks pass)
```

**Expect**

- `dotnet --version` from repo root reports 10.0.400 (global.json pin).
- Microsoft.* 10.0.11, FluentUI 5.0.0-rc.5-26219.1, Microsoft.OpenApi 2.12.0, Microsoft.TypeScript.MSBuild 6.0.3 in `Directory.Packages.props`.
- No NU1903 suppressions added.

## Session

- Created: grok 2026-08-13 (after PR #301 green; user asked for a bump task + new SDK)
- Implementer launch: host=herdr profile=implementer-grok provider=grok worktree=/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-195-bump-sdk-to-100400-and-outdated-nuget-pins workspace=wX pane=wX:p1 agent=task195 (2026-08-25 UTC)
- Implementation: grok 2026-08-25 (SDK 10.0.400 + Take-table pins; TS 7.0.0 reverted)
