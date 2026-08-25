# Take remaining latest NuGet pins after 195 and fix breakages

Predecessor: **195** / PR **306** (SDK 10.0.400 + 2026-08-13 Take table). Not a product release.

## Description

195 left `ganda nuget outdated` non-empty on purpose: the inventory was 12 days old, plus
known holdouts. This task takes **current latest** for those leftovers and **fixes** whatever
breaks (build, tests, template-smoke). Re-run `ganda nuget outdated` at start — versions
below are 2026-08-25 snapshots.

Do **not** bump `<Version>` or TimeWarp platform pins (`TimeWarp.Foundation.*` / Modules /
Identity / 402 / Architecture.*). Those stay until a real release PR.

Do **not** adopt .NET 11 preview packages. SDK stays **10.0.400**.

## FluentUI (channel, not Stable)

`Microsoft.FluentUI.AspNetCore.Components` is already on the **v5 prerelease** train
(`5.0.0-rc.5-26219.1`). `ganda nuget outdated` Stable **4.14.4** is a **downgrade** — never
take it.

Stay on the **v5 prerelease channel**. If a newer `5.0.0-rc.*` (or 5.0.0 stable) exists,
take that. If already latest 5.x, leave it. Icons `Microsoft.FluentUI.AspNetCore.Components.Icons`
is a different package (currently 4.14.4 and current) — do not conflate with Components.

## Take (2026-08-25 leftovers — refresh from `ganda nuget outdated`)

Lockstep families together.

| Family | From | Toward (as of 8/25) | Notes |
|--------|------|---------------------|-------|
| Aspire.Hosting.* | 13.4.6 | **13.5.2** | keep Aspire.* together |
| FastEndpoints + Swagger | 8.2.0 | **8.3.0** | keep the pair |
| OpenTelemetry.* | 1.17.0 | **1.18.0** | exporter, hosting, AspNetCore, Http, Runtime |
| Testcontainers.PostgreSql | 4.13.0 | **4.14.0** | |
| Microsoft.CodeAnalysis.* | 5.6.0 | **5.9.0** | CSharp, Analyzers, CodeStyle, Workspaces — BannedApiAnalyzers was already current |
| Roslynator.* | 4.16.0 | **5.0.0** (major) | keep the three together; fix analyzer breaks. 4.16.1 is same-major fallback only if 5.0 is ugly |
| Scalar.AspNetCore | 2.16.19 | **2.17.1** | |
| libphonenumber-csharp | 9.0.36 | **9.0.37** | |
| protobuf-net.Grpc* | 1.2.x | **1.3.14** | keep the protobuf-net.Grpc set together |
| Microsoft.OpenApi | 2.12.0 | **2.12.2** at minimum | same 2.x floor as 195 |

## Take and fix (were 195 holdouts)

These are **in scope**. Try latest, **fix the product**, do not silently revert unless a Notes
line explains a real blocker after a genuine attempt.

| Pin | Latest | Known issue | Job |
|-----|--------|-------------|-----|
| **Microsoft.TypeScript.MSBuild** | **7.0.1** | 195 tried 7.0.0; `web-spa` Release **MSB4057** (`CheckFileSystemCaseSensitive` missing). Reverted to 6.0.3. | Take 7.0.1. Repair the TS target chain / project so Release builds, or document a real remaining blocker and keep 6.0.3 with a Design comment. |
| **Microsoft.OpenApi 3.x** | **3.10.2** | Task **144**: 3.x `IOpenApiMediaType.Example` is read-only; AspNetCore.OpenApi 10.x XmlCommentGenerator assigns it → **CS0200**. | Try 3.x. If CS0200 still fires, patch our OpenApi usage **or** stay on **2.12.2** with an updated 144 comment. Do not leave 2.12.0 if 2.12.2 is free. |

## Requirements

1. Start with `ganda nuget outdated` in this worktree; Take table is a hint, **latest** wins.
2. `dotnet restore` + `dev build` **0/0**. Fix warnings/errors from the bumps (Roslynator 5, CodeAnalysis 5.9, Aspire 13.5, FastEndpoints 8.3, OTel 1.18, protobuf-net.Grpc 1.3, TS 7, OpenApi 3).
3. `dev test` (or full `dotnet run tools/dev-cli/dev.cs -- test`).
4. `dev template-smoke` — CPM pins ship in generated apps.
5. `ganda repo audit` — no new NU1903 suppressions.
6. After: `ganda nuget outdated` empty except TimeWarp platform pins, .NET 11 previews, and any **documented** remaining blocker (TS 7 / OpenApi 3 only if the attempt failed).
7. Results + How to validate; `ganda kanban done 201`; PR; STOP. Do not merge.

## Checklist

- [x] `ganda nuget outdated` snapshot at start
- [x] Take leftover minors/patches (Aspire 13.5, FastEndpoints 8.3, OTel 1.18, Testcontainers 4.14, CodeAnalysis 5.9, Scalar 2.17, libphonenumber 9.0.37, protobuf-net.Grpc 1.3, OpenApi ≥ 2.12.2)
- [x] Roslynator 5.0 (or 4.16.1 only if 5.0 is a real break after a fix attempt)
- [x] TypeScript.MSBuild 7.0.1 + fix MSB4057 (or documented revert)
- [x] OpenApi 3.x + fix CS0200 (or stay 2.12.2 with comment)
- [x] FluentUI stays v5 prerelease channel (never 4.14.4)
- [x] `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] `ganda nuget outdated` only documented holdouts
- [x] Results + How to validate; kitchen in `done/`; PR; STOP

## Session

- Created: 927093 (2026-08-25)
- Cockpit: Grok 01a0275a after merge of architecture PR 306 / task 195
- FluentUI policy: v5 prerelease channel; Stable 4.14.4 is a downgrade
- Implementation: Grok 01a039de (2026-08-26) — take leftover pins, TS 7.0.1 pipeline, OpenApi 3.x attempt

## Notes

195 kitchen: `kanban/done/195-bump-sdk-to-100400-and-outdated-nuget-pins.md` (Holdouts + post-inventory leftovers).

FluentUI Icons 4.14.4 is unrelated to Components v5 — leave Icons unless outdated on its own channel.
- Implementer launch: host=herdr profile=implementer-grok provider=grok worktree=/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-201-take-remaining-latest-nuget-pins-after-195-and-fix workspace=wY pane=wY:p1 agent=task201 (2026-08-25 UTC)

Start inventory (`ganda nuget outdated`, 2026-08-26): 27 outdated (5 major, 21 minor, 1 patch). Latest matched the Take table.

OpenApi 3.10.2 restore fails NU1608: `Microsoft.AspNetCore.OpenApi 10.0.11 requires Microsoft.OpenApi (>= 2.7.5 && < 3.0.0)`. That upper bound is new vs the task-144 floor-only comment. Stay on **2.12.2**.

TypeScript.MSBuild 7.0.1 taken. `web-spa.csproj` now runs v7 `SetTscExecutable` + `RunTscCompile` before `ResolveProjectStaticWebAssets` (task 116 SWA ordering). Release build and `-t:Rebuild` 0/0.

Aspire 13.5.2 emits ASPIRE010 (AspireUseCliBundle default false). Template hosts stay on NuGet-restored DCP/dashboard (`AspireUseCliBundle=false` + `NoWarn ASPIRE010`).

Roslynator 5.0.0: no analyzer breakages under TreatWarningsAsErrors.

## Results

Took the 2026-08-26 leftovers in `Directory.Packages.props`: Aspire.Hosting.* **13.5.2** (EntityFrameworkCore **13.5.2-preview.1.26421.6**, AppHost SDK **13.5.2**), FastEndpoints pair **8.3.0**, OpenTelemetry.* **1.18.0**, Testcontainers.PostgreSql **4.14.0**, Microsoft.CodeAnalysis.{CSharp,Analyzers,CSharp.CodeStyle,CSharp.Workspaces} **5.9.0**, Roslynator.* **5.0.0**, Scalar.AspNetCore **2.17.1**, libphonenumber-csharp **9.0.37**, protobuf-net.Grpc* **1.3.14**, Microsoft.OpenApi **2.12.2**, Microsoft.TypeScript.MSBuild **7.0.1**. Did **not** bump `<Version>` / TimeWarp platform pins. FluentUI Components stays **5.0.0-rc.5-26219.1**; Icons stays **4.14.4**.

TypeScript 7.0.1 adopted: classic `CheckFileSystemCaseSensitive` / `CompileTypeScript` / `GetTypeScriptOutputForPublishing` chain is gone; `web-spa` compiles via native tsc-go before Static Web Assets discovery. OpenApi 3.10.2 attempted and blocked at restore (NU1608); CPM comment records the AspNetCore.OpenApi 10.0.11 upper bound.

Gates: `dotnet restore`; `dotnet build timewarp-architecture.slnx -c Release` **0/0**; `dotnet run tools/dev-cli/dev.cs -- test` all projects passed (2 skipped, pre-existing); `dotnet run tools/dev-cli/dev.cs -- template-smoke` **SUCCEEDED** (SmokeDefault / SmokeNoPostgres / SmokeNoApi); `ganda repo audit` exit 0 (pre-existing `memsearch-scaffold` advisory only). `ganda nuget outdated` remainder is Microsoft.OpenApi 2.12.2 → 3.10.2 (documented holdout).

### How to validate

**Smoke**

```bash
ganda nuget outdated
# Expect: only Microsoft.OpenApi 2.12.2 → 3.10.2 (major). TimeWarp platform pins and
#         FluentUI 5.0.0-rc.5-26219.1 current. $(TwArchitecture*PackageId) "not found" is expected.

rg 'Microsoft.TypeScript.MSBuild" Version="' Directory.Packages.props
# Expect: 7.0.1

rg 'Microsoft.OpenApi" Version="' Directory.Packages.props
# Expect: 2.12.2

rg 'Aspire.AppHost.Sdk/' source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
# Expect: 13.5.2

dotnet restore
dotnet build timewarp-architecture.slnx -c Release
# Expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release -t:Rebuild
# Expect: TSFILE lines for wwwroot/js/*.js; 0 Warning(s) 0 Error(s); no MSB4057

dotnet run tools/dev-cli/dev.cs -- test
# Expect: Tests completed successfully! (2 skipped: RunForever, quarantined weather)

dotnet run tools/dev-cli/dev.cs -- template-smoke
# Expect: Template smoke SUCCEEDED; generated-app web-spa emits TSFILE under wwwroot/js

ganda repo audit
# Expect: exit 0 (blocking checks pass; memsearch-scaffold advisory only)
```

**Expect**

- `Directory.Packages.props`: Aspire 13.5.2, FastEndpoints 8.3.0, OpenTelemetry 1.18.0, Roslynator 5.0.0, TypeScript.MSBuild 7.0.1, Microsoft.OpenApi 2.12.2, FluentUI 5.0.0-rc.5-26219.1.
- `web-spa.csproj` hooks `SetTscExecutable;RunTscCompile` before `ResolveProjectStaticWebAssets` (no classic v6 target names).
- `aspire-app-host.csproj`: `Aspire.AppHost.Sdk/13.5.2`, `AspireUseCliBundle=false`, `NoWarn` includes `ASPIRE010`.
- No NU1903 suppressions added. SDK stays 10.0.400. `<Version>` / TimeWarp platform pins unchanged.
