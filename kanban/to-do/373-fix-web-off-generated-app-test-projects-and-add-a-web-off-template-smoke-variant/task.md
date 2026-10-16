# Fix web-off generated app test projects and add a web-off template-smoke variant

## Goal (implementer restatement)

An app generated with `--web false` must build 0/0 and pass its own tests. Today it fails on
web-only test code (`WebServerApiRoutePrefixes` in ingress-smoke-tests) and on analyzer findings
(IDE0005, RCS1213) in `timewarp-testing`. Fix these with template excludes and flag regions,
not suppressions. Add a web-off entry to the template-smoke matrix so CI catches a regression.
Check grpc-off and yarp-off the same way. Add each variant that is cheap to cover; for the rest,
record the finding and file a follow-up task.

Acceptance: `SmokeNoWeb` is in `SmokeMatrix` and fails against the pre-fix tree. The generated
web-off app's test projects pass. The monorepo passes `dev build` 0/0, `dev test`,
`dev template-smoke` and `ganda repo audit`.

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

- [x] Reproduce: generate a `--web false` app and capture the exact build errors (ingress-smoke-tests, timewarp-testing IDE0005 and RCS1213)
- [x] Fix `ingress-smoke-tests` / `aspire-tests` web-only code with `#if web` or a template exclude
- [x] Fix `timewarp-testing` IDE0005 and RCS1213 with correct web regions
- [x] Add the web-off variant to `SmokeMatrix` in `template-smoke-command.cs` (and the harness if needed)
- [x] Confirm the CI `template-smoke` job picks up the new variant; edit `workflow.yml` only if it enumerates variants
- [x] Investigate grpc-off, yarp-off, postgres-off and api-off for the same bug class; add variants if cheap, else record findings and file follow-ups
- [x] Generated `--web false` app: `dev build` 0 warnings and 0 errors, tests green
- [x] `dev build` (monorepo) 0 warnings and 0 errors
- [x] `dev test` passes
- [x] `dev template-smoke` passes including the new web-off variant (run from a fresh `bin/dev`, see stale-binary footgun)
- [x] `dev check-version` and version bump with pins if the template output changes ship (`tw-pr`)
- [x] `ganda repo audit` clean

## Proof gates

- Generated `--web false` app builds with 0 warnings and 0 errors, and its test projects pass.
- `dev template-smoke` is green with the new web-off variant. A negative check shows the variant
  fails against the pre-fix tree.
- `dev build` 0/0 and `dev test` green in the monorepo, so default output is unchanged.

## Session

- Created: 3911527 (2026-10-16)
- 2026-10-16: implementer (ganda task work, Claude Opus 5.5). Fixed, smoke matrix extended, follow-up 374 filed.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T04:37:58Z

- Review oracle: tw-implementation-review round 1 (claude, model claude-opus-5-5), general reviewer, disposition clean — 2026-10-16

## Learnings

- Goal restatement matched the task; no misread.

## Results

- `ingress-smoke-tests.cs` is web-only: every fact targets Web.Server and it uses the web-only
  generated `WebServerApiRoutePrefixes`. `.template.config` now excludes the whole file under
  `!web`, and its Design region records why. In `aspire-tests/global-usings.cs`, the usings only
  that file needs are now inside `#if(web)` (DI, `System.Collections.Concurrent`, `System.Net`).
  `aspire-tests.csproj` defines `web` in the monorepo (TWA0010).
- `timewarp-testing`: `HostGraphFactory.EnsureYarpPortsAreFree` is now guarded `web && yarp`,
  because its only callers need web (RCS1213). Each guarded block now carries its own leading
  blank line, so stripped regions never leave two (IDE2000, which the IDE0005/RCS1213 errors had
  hidden). In `yarp-test-server-application.cs`, the `Yarp.ReverseProxy.Configuration` using
  moves inside `#if(web)`, beside its only consumer `YarpWebClusterHttpAddressFilter` (IDE0005).
- `SmokeMatrix` now adds `SmokeNoWeb` (`ExcludedFamilies ["web"]`), `SmokeNoGrpc` and
  `SmokeNoYarp`. The web family's mock-auth surface check is skipped when web is excluded. The
  CI `template-smoke` job runs `dev template-smoke` and does not list variants, so
  `workflow.yml` needs no change.
- Negative check: the first smoke run on the pre-fix tree, with `SmokeNoWeb` added, failed with
  `Build failed for SmokeNoWeb`. Errors: CS0103 `WebServerApiRoutePrefixes` x3 in
  ingress-smoke-tests, IDE0005 in yarp-test-server-application.cs, RCS1213 and IDE2000 in
  host-graph-factory.cs.
- Investigation: grpc-off builds 0/0 and its aspire-tests pass (57/57), so it was added.
  yarp-off builds 0/0, so it was added as a compile guard. Its aspire-tests fail at runtime,
  13 of 57: ingress-smoke-tests and two closed-box guard facts assume the `ingress` resource.
  Template-smoke does not run aspire-tests, so this is a separate bug class. Filed as task 374.
  Postgres-off and api-off were already in the matrix and pass.
- Gates: `dev build` 0/0; `dev test` 23/23 projects passed; `dev template-smoke` SUCCEEDED for
  all six variants, each 0 Warning(s) / 0 Error(s); `dev check-version`: source 2.0.0-beta.20 is
  already ahead of NuGet beta.19, so no bump is needed. In the generated SmokeNoWeb app:
  api-server-integration-tests 1/1, aspire-tests 42/42, api-jaribu-tests 9/9.

- Review: 1 round, roster general (effort 2), 0 findings (bug 0 / suggestion 0 / nit 0).
  Disposition **clean**. Artifacts: `review/review-framework.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate

- Smoke: `dotnet run tools/dev-cli/dev.cs -- template-smoke`
- Expect: `Template smoke SUCCEEDED`, with SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb,
  SmokeNoGrpc and SmokeNoYarp each building 0 Warning(s) / 0 Error(s).
- Smoke: `cd artifacts/template-smoke/work/SmokeNoWeb/tests/container-apps/aspire/aspire-tests && dotnet test -c Release`
- Expect: `Test run summary: Passed!` (42 tests).

## Notes

Found by the 335-001 worker while checking its "`web` off still compiles" acceptance item. Not
caused by 335-001.
