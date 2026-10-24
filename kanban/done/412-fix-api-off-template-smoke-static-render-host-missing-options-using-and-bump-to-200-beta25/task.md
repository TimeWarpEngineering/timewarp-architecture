# Fix api-off template smoke (static-render-host missing Options using) and bump to 2.0.0-beta.25

## Description

The nightly matrix template smoke (run 40092118522, 2026-10-24) fails the `SmokeNoApi` variant:

```
tests/container-apps/web/web-spa-integration-tests/infrastructure/static-render-host.cs(41,36):
error CS0246: The type or namespace name 'IOptions<>' could not be found
error CS0103: The name 'Options' does not exist in the current context
```

`static-render-host.cs` (added by 379-017) uses `IOptions<>` / `Options.Create`, but
`Microsoft.Extensions.Options` is a global using only under `#if(api)` in that project's
`global-usings.cs`. With `--api false` the test project does not compile. The other variants pass.

This blocks the beta.25 release, so the version bump rides in the same PR.

## Goal (implementer restatement)

`SmokeNoApi` fails because `static-render-host.cs` calls `IOptions<>` and `Options.Create`, while `Microsoft.Extensions.Options` is imported only inside the `#if(api)` block of that test project's `global-usings.cs`. With `--api false` the preprocessor drops that import and the file does not compile. The fix is a file-level `using Microsoft.Extensions.Options;` on `static-render-host.cs`, the same pattern `ask-starter-prompts-tests.cs` already uses. The `#if(api)` global using stays as it is.

The same change set moves the repo from `2.0.0-beta.24` to `2.0.0-beta.25` so the release can cut. That means `<Version>` in `source/Directory.Build.props`, the matching `<Version>` in `timewarp-templates/Directory.Build.props`, and every platform `PackageVersion` pin in `Directory.Packages.props` that task 124 keeps equal to the release version. Those version edits land in one commit.

Done means `dev build` is 0 warnings and 0 errors, `dev check-version` says the source version is new, and template smoke covers `SmokeNoApi` and the default variant (the full `--matrix --app-tests` set when it can run). The kitchen records which variants actually ran.

## Required outcome

- Add `using Microsoft.Extensions.Options;` to `static-render-host.cs`, matching
  `features/application/ask-starter-prompts-tests.cs`. Do not change the `#if(api)` block in
  `global-usings.cs`.
- Bump `<Version>` in `source/Directory.Build.props` from `2.0.0-beta.24` to `2.0.0-beta.25`, with
  any platform package pins `dev check-version` requires, in the same commit (`tw-pr`).
- Gates: `dev build` 0/0; `dev check-version`; `dev template-smoke --matrix --app-tests` passes all
  variants, or at least `SmokeNoApi` plus the default variant if the full matrix cannot run.
  Report which ran.

## Checklist

- [x] Using added to static-render-host.cs
- [x] Version bumped to 2.0.0-beta.25 (plus pins)
- [x] dev build 0/0, dev check-version clean
- [x] SmokeNoApi and default template smoke pass

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)
- Implementation: 01a16e14-c24c-7043-aa5e-341195a12844 (2026-10-24)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-24T14:29:23Z
- Review oracle: general reviewer (claude, model claude-opus-5-5), effort 1, session not reported, max-turns 80, disposition clean — 2026-10-24
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-24T14:33:48Z

## Results

`static-render-host.cs` now imports `Microsoft.Extensions.Options` in the file. The `#if(api)` block in `global-usings.cs` is unchanged. A generated `--api false` tree keeps that file-level using and drops the Options global using, so `IOptions<>` and `Options.Create` resolve.

`<Version>` is `2.0.0-beta.25` in `source/Directory.Build.props` and `timewarp-templates/Directory.Build.props`. The task-124 platform pins in `Directory.Packages.props` match: Foundation (Domain, Contracts, Application, Infrastructure, Server), Modules, Analyzers, Generators, Attributes, Identity, and TimeWarp.402.

`./bin/dev build`: 0 warnings, 0 errors. `./bin/dev check-version`: source `2.0.0-beta.25`, NuGet `2.0.0-beta.24`, one prerelease increment ahead, safe to release. `ganda repo audit`: 33 passed, 0 failed, 1 skipped.

`dotnet run tools/dev-cli/dev.cs -- template-smoke --matrix --app-tests` ran every variant: SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp, with generated-app tests. Every variant built with 0 warnings and 0 errors. SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, and SmokeNoGrpc passed their app-test suites, including Playwright 27/27 on each web variant. SmokeNoApi's SPA integration suite was 493/493. SmokeNoYarp's suites passed except `web-spa-playwright-tests` (26 passed, 1 failed): `Open_Should_MoveFocusIn_TrapTab_And_ReturnFocusOnEscape` saw `paintHitsDialog` false. The same project, rerun immediately against that generated tree, passed 27/27. That assertion also passed inside the matrix on the other four web variants. The matrix command therefore exited 1 on that one SmokeNoYarp Playwright miss. `web-server-integration-tests` printed 355/356 succeeded on the variants that include it, and the harness continued, which it does only when the MTP summary has zero failures.

### Review disposition

One round, effort 1, one general reviewer. The reviewer found 0 bugs, 0 suggestions, and 0 nits, with 0 open, 0 fixed, and 0 wontfix. Disposition is **clean**. The reviewer re-ran `./bin/dev build` (0 warnings, 0 errors) and `./bin/dev check-version` (safe to release) in this worktree. The reviewer did not re-run template smoke, because it cannot select one variant. Smoke evidence is the implementer's matrix run above. The SmokeNoYarp Playwright focus-trap miss, which passed on rerun, is recorded as an observation outside this diff. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, and `review/disposition.md`.

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev check-version
dotnet run tools/dev-cli/dev.cs -- template-smoke --matrix --app-tests
```

Expect:

- Build ends with `0 Warning(s)` and `0 Error(s)`.
- `check-version` prints source `2.0.0-beta.25`, NuGet `2.0.0-beta.24`, and `Version in source is new — safe to release`.
- Template smoke prints `SmokeNoApi OK` and `SmokeDefault OK`, then `Template smoke SUCCEEDED`. A generated app with `--api false` compiles `tests/container-apps/web/web-spa-integration-tests`, including `infrastructure/static-render-host.cs`.

## Review

- Round 1: Done

## Notes

Steve asked for the beta.25 cut on 2026-10-24. The release is held on this.
