# Bump .NET 11 RC2 to RC3

## Description

Steve, 2026-10-26: "create task to update to dotnet 11 RC3 just installed." SDK
`11.0.100-rc.3.26522.148` is installed locally. The RC3 shared-framework, ASP.NET Core and EF Core
packages (`11.0.0-rc.3.26522.148`) are on nuget.org. Move the repo from RC2 to RC3. Task 362
(RC1 to RC2) is the precedent.

## Requirements

- **SDK:** `global.json` and every `tests/**/global.json`: `11.0.100-rc.2.26504.105` ->
  `11.0.100-rc.3.26522.148`.
- **Packages:** in `Directory.Packages.props`, move every shared-framework `Microsoft.*` /
  `System.*` pin from `11.0.0-rc.2.26504.105` to `11.0.0-rc.3.26522.148`. Move
  `Microsoft.CodeAnalysis.NetAnalyzers` too if it is on the SDK train.
- **EF Core follows Npgsql.** `Npgsql.EntityFrameworkCore.PostgreSQL` has no RC3 build yet (latest
  `11.0.0-rc.2`), and it pins `Microsoft.EntityFrameworkCore` exactly. Check nuget.org when you
  start:
  - If Npgsql has an RC3 build, move Npgsql, the EF Core family
    (`Microsoft.EntityFrameworkCore*`,
    `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`,
    `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`) and the `dotnet-ef` tool in
    `.config/dotnet-tools.json` together.
  - If not, keep that family on RC2. Moving it alone fails restore with NU1608/NU1107. Record the
    hold in Results.
- **Update the train comment.** Update the `Directory.Packages.props` comment that records the
  shared-framework train ("Shared-framework Microsoft.* / System.* pins on this train are …") to
  RC3, with this task id. State the EF Core / Npgsql position.
- **Breaking changes.** Review the ASP.NET Core, EF Core and runtime RC3 notes under
  <https://github.com/dotnet/core/tree/main/release-notes/11.0> and fix any fallout. Report which
  ones you checked, even if none apply.
- **Task 257 re-check.** Check whether `Microsoft.AspNetCore.OpenApi` RC3 or a newer
  `FastEndpoints.OpenApi` changes the `Microsoft.OpenApi` 2.x hold. Add a "Re-checked task 420"
  line to the existing OpenApi comment. Don't do the FastEndpoints major move here; that is
  task 257.
- **Leave history alone.** Leave existing migration `ProductVersion` annotations alone.
- **Version.** TimeWarp platform packages are rebuilt against new framework pins. Run
  `dev check-version` and follow it. If the current `<Version>` is already unreleased, no extra
  bump is needed.

## Checklist

- [ ] `global.json` + every `tests/**/global.json` on `11.0.100-rc.3.26522.148`
- [ ] Shared-framework pins on `11.0.0-rc.3.26522.148`
- [ ] EF Core / Npgsql / `dotnet-ef` moved together, or held on RC2 with the reason recorded
- [ ] Train comment and OpenApi re-check comment updated
- [ ] RC3 breaking changes reviewed; fallout fixed
- [ ] `dev build --clean` 0/0; `dev check-version`; `dev regenerate-generated` golden diff empty
- [ ] `dev test` green (including web-infrastructure-tests, web-spa-integration-tests,
      aspire-tests, timewarp-identity-tests, dev-cli-tests)
- [ ] `dev template-smoke` green
- [ ] `ganda repo audit` clean

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)

## Notes

- Workers must not start the AppHost (shared user secrets). The Blazor WASM check after merge is
  manual: `dev run`, then load the SPA. If stale `_framework` assets cause TypeLoad errors, run
  `dev clean` and clear site data.
- Task 419 (Aspire 13.6.4) also edits `Directory.Packages.props`. Different lines, but merge
  `origin/master` first if 419 has landed.
