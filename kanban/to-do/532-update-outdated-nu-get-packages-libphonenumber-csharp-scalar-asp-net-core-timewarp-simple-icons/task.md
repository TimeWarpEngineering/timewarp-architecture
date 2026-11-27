# Update outdated NuGet packages (libphonenumber-csharp, Scalar.AspNetCore, timewarp-simple-icons)

## Description

Pre-release dependency refresh before cutting `v2.0.0-beta.32`. `ganda nuget outdated` reports 3 outdated literal package pins in root `Directory.Packages.props` (all TimeWarp packages are already current):

- `libphonenumber-csharp`: `9.0.43` -> `9.0.44` (patch; `foundation-contracts`)
- `Scalar.AspNetCore`: `3.1.1` -> `3.3.0` (minor; `foundation-server`)
- `timewarp-simple-icons`: `16.37.0` -> `16.38.1` (minor; `web-spa`)

## Scope

1. Bump the three `<PackageVersion>` pins in root `Directory.Packages.props` and update the comment above `Scalar.AspNetCore` (`line 169`) so it matches `3.3.0`.
2. Fix any breaking API/warning changes if `Scalar.AspNetCore 3.3.0` or `timewarp-simple-icons 16.38.1` alters any call site or icon name (expected clean, but verify).
3. Verify `./bin/dev build` (0 warnings, 0 errors), targeted tests (`./bin/dev test --project foundation-contracts-tests`, `./bin/dev test --project web-server-integration-tests --filter-class OpenApi`, `./bin/dev test --project web-spa-integration-tests`), `./bin/dev template-smoke`, `ganda nuget outdated` (0 outdated), and `ganda repo audit`.

## Checklist

- [ ] Bump `libphonenumber-csharp` (`9.0.44`), `Scalar.AspNetCore` (`3.3.0`), and `timewarp-simple-icons` (`16.38.1`) in `Directory.Packages.props` (and update the `Scalar.AspNetCore` comment)
- [ ] Verify `./bin/dev build` (0 warnings, 0 errors), affected test suites, and `./bin/dev template-smoke`
- [ ] Verify `ganda nuget outdated` reports 0 outdated packages and `ganda repo audit` passes

## Session

- Created: 2026-11-27

## Notes

- Do **not** bump `<Version>` in `source/Directory.Build.props` — source is already at `2.0.0-beta.32` ahead of NuGet `2.0.0-beta.31`.
- Pre-release task ahead of task 533 (DevCli artifact-promotion release pipeline) and the `v2.0.0-beta.32` release cut.
