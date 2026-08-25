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

- [ ] `ganda nuget outdated` snapshot at start
- [ ] Take leftover minors/patches (Aspire 13.5, FastEndpoints 8.3, OTel 1.18, Testcontainers 4.14, CodeAnalysis 5.9, Scalar 2.17, libphonenumber 9.0.37, protobuf-net.Grpc 1.3, OpenApi ≥ 2.12.2)
- [ ] Roslynator 5.0 (or 4.16.1 only if 5.0 is a real break after a fix attempt)
- [ ] TypeScript.MSBuild 7.0.1 + fix MSB4057 (or documented revert)
- [ ] OpenApi 3.x + fix CS0200 (or stay 2.12.2 with comment)
- [ ] FluentUI stays v5 prerelease channel (never 4.14.4)
- [ ] `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] `ganda nuget outdated` only documented holdouts
- [ ] Results + How to validate; kitchen in `done/`; PR; STOP

## Session

- Created: 927093 (2026-08-25)
- Cockpit: Grok 01a0275a after merge of architecture PR 306 / task 195
- FluentUI policy: v5 prerelease channel; Stable 4.14.4 is a downgrade

## Notes

195 kitchen: `kanban/done/195-bump-sdk-to-100400-and-outdated-nuget-pins.md` (Holdouts + post-inventory leftovers).

FluentUI Icons 4.14.4 is unrelated to Components v5 — leave Icons unless outdated on its own channel.
