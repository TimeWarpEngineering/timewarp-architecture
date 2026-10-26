# Bump .NET 11 RC2 to RC3

## Description

Steve, 2026-10-26: "create task to update to dotnet 11 RC3 just installed." SDK
`11.0.100-rc.3.26522.148` is installed locally. The RC3 shared-framework, ASP.NET Core and EF Core
packages (`11.0.0-rc.3.26522.148`) are on nuget.org. Move the repo from RC2 to RC3. Task 362
(RC1 to RC2) is the precedent.

## Goal (implementer restatement)

Move this repo from the .NET 11 RC2 train onto RC3 so local builds use the installed SDK
`11.0.100-rc.3.26522.148` and the matching shared-framework packages `11.0.0-rc.3.26522.148`.

Done when:

- The root `global.json` and every `tests/**/global.json` pin that SDK.
- Every shared-framework `Microsoft.*` / `System.*` pin in `Directory.Packages.props`, and
  `Microsoft.CodeAnalysis.NetAnalyzers` when it ships on the same SDK train, is
  `11.0.0-rc.3.26522.148` or `11.0.100-rc.3.26522.148`.
- EF Core, the EF-adjacent packages, `dotnet-ef`, and `Npgsql.EntityFrameworkCore.PostgreSQL`
  move together only if Npgsql has an RC3 build that accepts the RC3 EF Core pin. Otherwise they
  stay on RC2 and Results says why.
- The shared-framework train comment names RC3 and this task, and states the EF Core / Npgsql
  position. The OpenApi comment gains a "Re-checked task 420" line. The FastEndpoints 9 major
  move stays on task 257.
- ASP.NET Core, EF Core, and runtime RC3 breaking-change notes are checked and any fallout in
  this repo is fixed. Existing migration `ProductVersion` annotations stay as history.
- `dev check-version` is followed. An already-unreleased `<Version>` does not get another bump.
- `dev build --clean` is 0/0, `dev regenerate-generated` has an empty golden diff, `dev test`
  (including the named suites), `dev template-smoke`, and `ganda repo audit` are clean.

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

- [x] `global.json` + every `tests/**/global.json` on `11.0.100-rc.3.26522.148`
- [x] Shared-framework pins on `11.0.0-rc.3.26522.148`
- [x] EF Core / Npgsql / `dotnet-ef` moved together, or held on RC2 with the reason recorded
- [x] Train comment and OpenApi re-check comment updated
- [x] RC3 breaking changes reviewed; fallout fixed
- [x] `dev build --clean` 0/0; `dev check-version`; `dev regenerate-generated` golden diff empty
- [x] `dev test` green (including web-infrastructure-tests, web-spa-integration-tests,
      aspire-tests, timewarp-identity-tests, dev-cli-tests)
- [x] `dev template-smoke` green
- [x] `ganda repo audit` clean

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-26)
- 2026-10-26: implementer moved the SDK and shared-framework pins to RC3. EF Core stayed on RC2
  because Npgsql has no RC3 build. `dev build --clean`, `dev test` (23/23), `dev template-smoke`,
  and `dev check-version` passed. `<Version>` stayed `2.0.0-beta.27`. Task 419 was already an
  ancestor (Aspire `13.6.4`); no extra merge was required for that pin.
- `ganda repo audit` exits 0. The only finding is a pre-existing `kanban-landed` warning for
  eight other claims on this machine that have no worktree (070-001, 104-024, 104-025, 104-026,
  123, 141, 198, 244). `--fix` does not land or remove those; it reports them and changes nothing.

## Results

- Root `global.json` and all 22 `tests/**/global.json` files pin SDK `11.0.100-rc.3.26522.148`.
  `dotnet --version` prints that SDK.
- `Directory.Packages.props`: the shared-framework `Microsoft.*` / `System.*` pins and
  `Microsoft.CodeAnalysis.NetAnalyzers` (`11.0.100-rc.3.26522.148`, the package exists on nuget.org)
  moved to RC3. `Microsoft.AspNetCore.Components.WebAssembly.Server` and
  `Microsoft.AspNetCore.Components.WebAssembly.DevServer` are in that set, which is what the RC3
  note asks for the Blazor debugger advisory.
- **EF Core held on RC2.** nuget.org `Npgsql.EntityFrameworkCore.PostgreSQL` latest is
  `11.0.0-rc.2` (no `11.0.0-rc.3`). Its net11.0 nuspec pins
  `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` exactly at
  `[11.0.0-rc.2.26504.105]`. The same exact pin is on
  `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` and
  `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 11.0.0-rc.2. Held together
  with `Microsoft.EntityFrameworkCore`, `.Design`, `.InMemory`, `.Tools`, and the `dotnet-ef`
  tool in `.config/dotnet-tools.json`. Moving that family alone fails restore (NU1608/NU1107).
- Train comment in `Directory.Packages.props` names RC3 and task 420, and states the hold.
- **Re-checked task 420.** `Microsoft.AspNetCore.OpenApi` 11.0.0-rc.3.26522.148 still declares
  `Microsoft.OpenApi` `[3.10.2, 4.0.0)`. `FastEndpoints.OpenApi` 8.3.0 is still the latest stable
  and still depends on `Microsoft.AspNetCore.OpenApi` 10.0.11 (`Microsoft.OpenApi` `[2.7.5, 3.0.0)`).
  8.4.0-beta.26 is net10.0 only. 9.0.0-beta.6's net11.0 group still depends on
  `Microsoft.AspNetCore.OpenApi` 11.0.0-rc.2.26504.105, not RC3. Latest `Microsoft.OpenApi` 2.x is
  still 2.13.0, so the pin stays. The FastEndpoints 9 move remains task 257.
- **RC3 notes checked** under <https://github.com/dotnet/core/tree/main/release-notes/11.0>.
  The `preview/rc3` folder has only `11.0.0-rc.3.md` and `release.json`. These pages 404:
  `aspnetcore.md`, `efcore.md`, `runtime.md`, `libraries.md`, `sdk.md`. `known-issues.md` has no
  RC3 section. The RC3 note (2026-10-25) is an out-of-band security update, not an API break:
  CVE-2026-111497 (Brotli decoder; automatic decompression is off by default; taking the RC3
  runtime and packages is the fix), CVE-2026-114148 (source-built Linux tarball ownership; does
  not apply to the Microsoft SDK install), and re-released CVE-2026-70197 / CVE-2026-102870
  (the Blazor one is covered by the WebAssembly.Server and DevServer pin move). No product
  call site changed. Existing migration `ProductVersion` annotations stay
  `11.0.0-rc.2.26504.105`.
- `dev check-version`: source `2.0.0-beta.27` is one prerelease ahead of NuGet `2.0.0-beta.26`.
  No extra version bump.
- Gates: `dev build --clean` 0 warnings / 0 errors; `dev regenerate-generated` exit 0 and
  `goldens/ingress/WebServerApiRoutePrefixes.g.cs` has an empty diff; `dev test` 23/23 passed
  (web-infrastructure-tests, web-spa-integration-tests, aspire-tests, timewarp-identity-tests,
  dev-cli-tests included); `dev template-smoke` SUCCEEDED (SmokeDefault); `ganda repo audit`
  exit 0 (advisory above).

### How to validate

Smoke:

```bash
dotnet --version
dev build --clean
dev test
dev template-smoke
dev run
```

Expect:

- `dotnet --version` prints `11.0.100-rc.3.26522.148`.
- Build reports 0 warnings and 0 errors. `dev test` reports 23 of 23 projects passed.
  `dev template-smoke` ends with `Template smoke SUCCEEDED`.
- `dev run` boots Aspire. The Blazor WASM SPA loads with no TypeLoad errors. If stale
  `_framework` assets break it, run `dev clean` and clear site data.
- `rg 11.0.0-rc.2.26504.105 Directory.Packages.props .config/dotnet-tools.json` shows only the
  EF Core family, the `dotnet-ef` tool, and the comments that record the Npgsql hold. Migration
  `ProductVersion` annotations still say RC2.

## Notes

- Workers must not start the AppHost (shared user secrets). The Blazor WASM check after merge is
  manual: `dev run`, then load the SPA. If stale `_framework` assets cause TypeLoad errors, run
  `dev clean` and clear site data.
- Task 419 (Aspire 13.6.4) also edits `Directory.Packages.props`. Different lines, but merge
  `origin/master` first if 419 has landed.
