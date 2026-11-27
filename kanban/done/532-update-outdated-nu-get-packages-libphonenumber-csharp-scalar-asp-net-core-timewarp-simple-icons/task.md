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

## Goal (implementer restatement)

Bring the three stale literal NuGet pins in root `Directory.Packages.props` up to current so
`ganda nuget outdated` is clean before the `v2.0.0-beta.32` cut.

Acceptance:

- `libphonenumber-csharp` 9.0.44, `Scalar.AspNetCore` 3.3.0, `timewarp-simple-icons` 16.38.1;
  the Scalar comment names 3.3.0.
- Any API or icon-name break from the bumps is fixed in the call sites.
- `./bin/dev build` is 0 warnings / 0 errors; the tests that cover the touched packages pass;
  `ganda nuget outdated` reports 0; `ganda repo audit` passes.
- `<Version>` in `source/Directory.Build.props` is not touched.

## Checklist

- [x] Bump `libphonenumber-csharp` (`9.0.44`), `Scalar.AspNetCore` (`3.3.0`), and `timewarp-simple-icons` (`16.38.1`) in `Directory.Packages.props` (and update the `Scalar.AspNetCore` comment)
- [x] Verify `./bin/dev build` (0 warnings, 0 errors), affected test suites, and `./bin/dev template-smoke`
- [x] Verify `ganda nuget outdated` reports 0 outdated packages and `ganda repo audit` passes

## Session

- Created: 2026-11-27
- 2026-11-27 implementer: bumped the three pins and rewrapped the Scalar comment for 3.3.0.
  No call site changed: the Scalar options calls in `common-server-module.cs` compile against
  3.3.0, and web-spa only references `timewarp-simple-icons` as a package (no icon names in code).
  No test file covers `PhoneNumberValidator` directly; `foundation-contracts-tests` was run as the
  project the spec names.
- 2026-11-27 boy-scout: the first `ganda repo audit` failed `package-pin-currency` because
  TimeWarp.Nuru 4.0.0-beta.18 shipped during the walk. Bumped `TimeWarp.Nuru` and
  `TimeWarp.Nuru.DevCli` to 4.0.0-beta.18 (and the two comments that name the Nuru version).
  Its nuspec dependencies (Build.Tasks 2.0.1-beta.1, Mediator 15.0.0-beta.10, Amuru 3.0.0-beta.10,
  Contracts 2.0.0-beta.34, OpenTelemetry 1.21.0) match the existing pins.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session d1237655-0403-44df-809b-dbb0618f0b09, max-turns 500, Done — 2026-11-27T05:47:17Z
- Review oracle: Grok 4.7, tw-implementation-review effort 1, roster general, 1 round, disposition clean, session not reported, turn cap uncapped (cursor). The pin goal matches the task. — 2026-11-27
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 80), artifacts general.md, merged.md, disposition.md — 2026-11-27T05:53:08Z

## Results

Product change: `Directory.Packages.props` pins `libphonenumber-csharp` 9.0.44,
`Scalar.AspNetCore` 3.3.0, `timewarp-simple-icons` 16.38.1; Scalar comment updated.
Boy-scout audit fix: `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` 4.0.0-beta.17 -> 4.0.0-beta.18.
`source/Directory.Build.props` `<Version>` untouched.

Gates:

- `./bin/dev build`: passed, 0 warnings, 0 errors (rerun after the Nuru bump: passed).
- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: dev CLI publishes on Nuru 4.0.0-beta.18.
- `./bin/dev test --no-build tests/container-apps/web/web-server-integration-tests/open-api-browser-navigation-tests.cs`: passed 3/3 (covers `UseScalarApiReference`; not a standalone runfile, no `#if !JARIBU_MULTI` guard, so run through the dev CLI).
- `./bin/dev test --no-build tests/container-apps/api/api-server-integration-tests/features/open-api/open-api-document-tests.cs`: passed 2/2 (hits `/scalar/v1`; not a standalone runfile).
- `./bin/dev test --no-build --project foundation-contracts-tests`: passed (libphonenumber consumer project).
- `./bin/dev test --no-build --project web-spa-integration-tests`: passed (timewarp-simple-icons consumer).
- `./bin/dev template-smoke`: Template smoke SUCCEEDED (default variant).
- `ganda nuget outdated`: All packages are up to date.
- `./bin/dev test --no-build --project agent-identity-cli-tests`: passed (Nuru consumer).
- `./bin/dev test --no-build --project dev-cli-tests`: passed (Nuru consumer).
- `ganda repo audit`: first run failed `package-pin-currency` (Nuru beta.18); after the bump, 43 passed, 0 failed, 1 warning (`vscode-window-icon`, warning only).

### Review

- Rounds: 1. Effort 1, roster: general. Reviewer: Grok 4.7. Session not reported. Turn cap uncapped (cursor); budget ladder for this diff is 80.
- Final counts: 0 bug, 0 suggestion, 0 nit. 0 open, 0 fixed, 0 wontfix.
- Disposition: **clean**. No wontfix and no escalations.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

- Smoke: `./bin/dev build && ganda nuget outdated && ./bin/dev test tests/container-apps/api/api-server-integration-tests/features/open-api/open-api-document-tests.cs`
- Expect: build has 0 warnings and 0 errors, `ganda nuget outdated` prints "All packages are up to date!", and the OpenAPI document tests pass 2/2 including the `/scalar/v1` request.

## Review

- Round 1: Done

## Notes

- Do **not** bump `<Version>` in `source/Directory.Build.props` — source is already at `2.0.0-beta.32` ahead of NuGet `2.0.0-beta.31`.
- Pre-release task ahead of task 533 (DevCli artifact-promotion release pipeline) and the `v2.0.0-beta.32` release cut.

## Learnings

- The goal restatement matches the task on the three pins, call-site fixes, and leaving `<Version>` alone.
- Misread: the restatement's acceptance line says "the tests that cover the touched packages" and leaves out `./bin/dev template-smoke` and `web-server-integration-tests --filter-class OpenApi`. Template-smoke was still run. The OpenApi filter was recorded as `open-api-browser-navigation-tests.cs` (3/3) plus the api-server `/scalar/v1` test. The recorded `./bin/dev build` compiles `UseScalarApiReference`, and `Scalar_Ui_Should_Point_At_The_Document` requests `/scalar/v1`.
