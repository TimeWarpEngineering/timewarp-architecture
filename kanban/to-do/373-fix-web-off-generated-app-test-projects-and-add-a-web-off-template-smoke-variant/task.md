# Fix web-off generated app test projects and add a web-off template-smoke variant

## Description

A generated app made with `dotnet new timewarp-architecture ... --web false` has failing test
projects, and nothing in CI catches it.

Observed failures (found by the 335-001 worker; pre-existing, not caused by 335-001):
- `ingress-smoke-tests` references `WebServerApiRoutePrefixes`, which is generated only when `web`
  is on. The reference does not compile with web off.
- The `timewarp-testing` project fails with IDE0005 (unnecessary using) and RCS1213 (unused
  member). Warnings are errors, so the build fails.

Why it hid: `dev template-smoke` and the `template-smoke` CI job generate only the variants in
`SmokeMatrix`: `SmokeDefault` (all flags on), `SmokeNoPostgres` and `SmokeNoApi`. There is no
`--web false` variant, so web-off output is never built or tested.

## Where to look (read-only investigation already done)

- `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`: the
  `WebServerApiRoutePrefixes_Should_CoverIdentityAndHello` test (around line 418) and the header
  comment (line 8). The file has no `#if web` region today. The `aspire-tests.csproj` beside it is
  also unconditional.
- `source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs`:
  the generator that emits `WebServerApiRoutePrefixes`. Related:
  `tests/container-apps/yarp/yarp-integration-tests/standalone-yarp-ingress-smoke-tests.cs` and
  `tests/analyzers/timewarp-architecture-sourcegenerator-tests/ingress-route-prefix-generator-tests.cs`.
- `tests/common/timewarp-testing/`: already has `#if(web)`, `#if(api)`, `#if(yarp)` regions in
  `host-graph.cs` and `host-graph-factory.cs`. Also `timewarp-testing.csproj` (a
  `<!--#if(web)-->` ProjectReference to web-server) and `global-usings.cs`, `constants.cs`,
  `global-suppressions.cs`. The IDE0005 and RCS1213 sites are in this project and need to be
  located by building a web-off generation.
- `tools/dev-cli/endpoints/template-smoke-command.cs`: `SmokeMatrix` (around line 82) is the list of
  variants. It carries `ExcludedFamilies` for flag-off assertions. Supporting code is in
  `tools/dev-cli/services/template-smoke-harness.cs`.
- `.github/workflows/workflow.yml`: the `template-smoke` job (around line 395) runs
  `dotnet run tools/dev-cli/dev.cs -- template-smoke`. It does not enumerate variants itself, so
  adding a matrix entry probably needs no workflow change. Confirm.
- `.template.config/template.json`: the flag definitions and file excludes.

## Requirements

1. Make `--web false` output build with 0 warnings and 0 errors and pass its tests:
   - Wrap web-only test code in the correct `#if web` / `<!--#if (web)-->` regions, including the
     `WebServerApiRoutePrefixes` test in `ingress-smoke-tests`. If the whole file or test project
     is web-only, prefer a template exclude over scattered regions.
   - Fix the IDE0005 and RCS1213 findings in `timewarp-testing` by region placement (usings and
     members that exist only for web code go inside the web region), not by suppression.
   - Leave `<!--#if (flag)-->` and `#if flag` regions intact for the other flags (TWA0008 / TWA0010).
2. Add a web-off variant to `SmokeMatrix` (for example `SmokeNoWeb`, `["--web", "false"]`, with the
   right `ExcludedFamilies` entries if the harness has a web family) so the regression cannot
   return. Update the CI job only if it enumerates variants.
3. Investigation item: check whether the other single-flag-off variants (`grpc`, `yarp`, and
   possibly `postgres` and `api` beyond the two already covered) have the same class of bug.
   Generate each by hand and build it. If covering the matrix is cheap, add the variants. If not,
   record the findings in the task Notes and file follow-up tasks. The required gate is web-off only.

## Checklist

- [ ] Reproduce: generate a `--web false` app and capture the exact build errors (ingress-smoke-tests, timewarp-testing IDE0005 and RCS1213)
- [ ] Fix `ingress-smoke-tests` / `aspire-tests` web-only code with `#if web` or a template exclude
- [ ] Fix `timewarp-testing` IDE0005 and RCS1213 with correct web regions
- [ ] Add the web-off variant to `SmokeMatrix` in `template-smoke-command.cs` (and the harness if needed)
- [ ] Confirm the CI `template-smoke` job picks up the new variant; edit `workflow.yml` only if it enumerates variants
- [ ] Investigate grpc-off, yarp-off, postgres-off and api-off for the same bug class; add variants if cheap, else record findings and file follow-ups
- [ ] Generated `--web false` app: `dev build` 0 warnings and 0 errors, tests green
- [ ] `dev build` (monorepo) 0 warnings and 0 errors
- [ ] `dev test` passes
- [ ] `dev template-smoke` passes including the new web-off variant (run from a fresh `bin/dev`, see stale-binary footgun)
- [ ] `dev check-version` and version bump with pins if the template output changes ship (`tw-pr`)
- [ ] `ganda repo audit` clean

## Proof gates

- Generated `--web false` app builds with 0 warnings and 0 errors, and its test projects pass.
- `dev template-smoke` is green with the new web-off variant. A negative check shows the variant
  fails against the pre-fix tree.
- `dev build` 0/0 and `dev test` green in the monorepo, so default output is unchanged.

## Session

- Created: 3911527 (2026-10-16)

## Notes

Found by the 335-001 worker while checking its "`web` off still compiles" acceptance item. Not
caused by 335-001.
