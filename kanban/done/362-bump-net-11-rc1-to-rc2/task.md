# Bump .NET 11 RC1 to RC2

## Description

.NET 11 RC2 shipped 2026-10-13 (go-live). Move the repo from RC1 to RC2. The RC2 SDK is installed on Windows, WSL and the box.

## Requirements

- `global.json` sdk: `11.0.100-rc.1.26425.128` -> `11.0.100-rc.2.26504.105`
- `Directory.Packages.props`: every `11.0.0-rc.1.26425.128` -> `11.0.0-rc.2.26504.105`
- Review ASP.NET Core and EF Core RC2 breaking-change notes: https://github.com/dotnet/core/tree/main/release-notes/11.0

## Checklist

- [x] Update global.json SDK version (root + every `tests/**/global.json`)
- [x] Update all RC1 package versions in Directory.Packages.props (EF Core held on RC1, see Results)
- [x] Review ASP.NET Core / EF Core RC2 breaking changes and fix any fallout (none hit)
- [x] Build succeeds
- [x] Tests green
- [ ] Blazor WASM dev server runs (manual: workers must not start the AppHost; see How to validate)

## Session

- Created: 2026-10-14
- 2026-10-14: implementer (ganda task work) bumped RC1 -> RC2; build, tests, check-version, template-smoke green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T16:16:04Z
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-14T16:18:02Z

## Review

- Round 1: Done

## Notes

- PRs #494 and #495 both pin System.Security.Cryptography.Pkcs 9.0.21 in Directory.Packages.props; coordinate with them to avoid conflicts.

## Results

- `global.json` and all 22 `tests/**/global.json` SDK pins: `11.0.100-rc.2.26504.105`.
- `Directory.Packages.props`: every shared-framework `Microsoft.*` / `System.*` pin and
  `Microsoft.CodeAnalysis.NetAnalyzers` moved to RC2 (`11.0.0-rc.2.26504.105` /
  `11.0.100-rc.2.26504.105`).
- **EF Core held on RC1** (`Microsoft.EntityFrameworkCore*`,
  `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `dotnet-ef` local tool):
  `Npgsql.EntityFrameworkCore.PostgreSQL` latest is `11.0.0-rc.1.1` and it pins EF Core
  `(= 11.0.0-rc.1.26425.128)` exactly. Moving EF Core to RC2 fails restore with NU1608/NU1107.
  The reason is in the Directory.Packages.props comment. Move EF Core when Npgsql ships an RC2 build.
- RC2 breaking changes reviewed (ASP.NET Core: `AddRazorRuntimeCompilation` obsolete,
  `IValidationMessageFormatter` removed, `BasePath` component namespace move, DeviceBoundSessions
  removed; EF Core: bug fixes only). None of these APIs are used. The only `BasePath` match is the
  MSBuild attribute in `web-spa.csproj`, which is unrelated.
- Existing migration `ProductVersion` annotations stay RC1. They are history, not pins.
- Gates: `dev build --clean` 0 warnings / 0 errors; `dev test` all green; `dev check-version` OK
  (2.0.0-beta.20 ahead of nuget); `dev template-smoke` SUCCEEDED.

- Review: 1 round, effort 1, roster `general`. Final counts: 0 bug, 0 suggestion, 0 nit; 0 open,
  0 fixed, 0 wontfix. Disposition: **clean**. The reviewer re-ran `./bin/dev build` (0 warnings,
  0 errors) and confirmed the only leftover rc.1 pins are the EF Core family. Artifacts:
  `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
dotnet --version                # inside the repo
dev build --clean
dev test
dev template-smoke
dev run                         # then open the web-server endpoint from the Aspire dashboard
```

Expect:

- `dotnet --version` prints `11.0.100-rc.2.26504.105`.
- Build reports 0 warnings, 0 errors. Tests and template smoke succeed.
- `dev run` boots Aspire. The Blazor WASM SPA loads in the browser with no TypeLoad errors.
  If stale `_framework` assets break it, run `dev clean` and clear site data.
- `grep rc.1 Directory.Packages.props` shows only the EF Core family, Npgsql, and the comments.
