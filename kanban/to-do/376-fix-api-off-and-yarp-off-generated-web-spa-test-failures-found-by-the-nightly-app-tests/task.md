# Fix api-off and yarp-off generated web-spa test failures found by the nightly app tests

## Description

`dev template-smoke --matrix --app-tests` (task 375, the nightly CI run) runs each generated
app's test projects. Two flag-off variants fail in web-spa test projects that the per-PR smoke
never runs. The cause is the same as task 374 (tests that assume a flag is on), but in different
projects. The nightly stays red until these are fixed.

| Variant | Project | Result | Failure |
|---------|---------|--------|---------|
| SmokeNoApi | web-spa-integration-tests | 281/285 | `CatalogAgent_Should.RememberConversationAsync` (`catalog-agent-parity-tests.cs:493`) gets a NullReferenceException: the authentication state has no `ClaimTypes.NameIdentifier` claim with api off. Four catalog-agent tests fail. |
| SmokeNoApi | web-spa-playwright-tests | FAILED locally, 17/17 on CI | `CtrlK_Should_ShowAsk_And_TheKeyState` timed out after 30 s in the local run and passed on the CI nightly (run 38860602411). Check whether it is flaky or tied to the catalog-agent failure. |
| SmokeNoYarp | web-spa-integration-tests | FAILED | `SpaIntegrationHost.StartAsync` (`infrastructure/base-test.cs:59`) waits for the `ingress` resource to become healthy. It does not exist with yarp off, so every test fails in fixture setup. |

Every other variant passes every generated test project. Run of 2026-10-16: SmokeDefault 9/9
projects, SmokeNoPostgres 8/8, SmokeNoWeb 2/2, SmokeNoGrpc 9/9, locally and on the CI nightly
(https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/38860602411).

## Requirements

- Guard or adapt the flag-assuming code in the template source (`tests/container-apps/web/...`),
  following task 374's pattern (`#if yarp` / `#if api` guards, DefineConstants per TWA0010, no
  double blank lines in any flag combination).
- Determine whether the api-off missing NameIdentifier claim is a test assumption or a product
  defect (mock-auth principal under api off). Fix the product if it is a product defect.
- `dev template-smoke --matrix --app-tests` passes for every variant.
- Template output changes: run `dev check-version` and follow `tw-pr`.

## Checklist

- [ ] SmokeNoYarp web-spa-integration-tests pass
- [ ] SmokeNoApi web-spa-integration-tests pass
- [ ] SmokeNoApi web-spa-playwright-tests pass (or the local timeout is shown to be flaky and fixed)
- [ ] `dev build` 0 warnings and 0 errors; `dev test` passes
- [ ] `dev template-smoke --matrix --app-tests` passes (Docker running)
- [ ] Dispatched nightly (`gh workflow run workflow.yml --ref <branch> -f mode=nightly`) passes
- [ ] `ganda repo audit` clean

## Notes

- Repro: `dev template-smoke --matrix --app-tests`, then rerun one suite with
  `cd artifacts/template-smoke/work/SmokeNoYarp/tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release`.
- Found by task 375's first full nightly run.
