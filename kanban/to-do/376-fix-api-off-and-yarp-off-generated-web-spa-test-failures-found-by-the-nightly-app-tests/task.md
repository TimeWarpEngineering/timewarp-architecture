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

## Goal (implementer restatement)

Make the generated web-spa test projects pass in the api-off and yarp-off variants, so the
nightly `dev template-smoke --matrix --app-tests` run goes green.

- yarp off: the SPA integration host must not wait for or resolve the `ingress` resource. It
  waits for the resources that exist and points the api-server client straight at api-server.
- api off: the catalog-agent tests read a `NameIdentifier` claim from the session. The product
  ships mock auth in every variant; the test host's api-off stand-in provider is the gap. Decide
  test vs product defect and fix the right side.
- Playwright `CtrlK_Should_ShowAsk_And_TheKeyState` api-off timeout: confirm flaky or related.

Acceptance: SmokeNoYarp and SmokeNoApi web-spa-integration-tests pass; SmokeNoApi playwright
passes; `dev build` clean, `dev test` passes; `dev template-smoke --matrix --app-tests` passes;
`ganda repo audit` clean; flag regions stay intact with no double blank lines in any variant.

## Requirements

- Guard or adapt the flag-assuming code in the template source (`tests/container-apps/web/...`),
  following task 374's pattern (`#if yarp` / `#if api` guards, DefineConstants per TWA0010, no
  double blank lines in any flag combination).
- Determine whether the api-off missing NameIdentifier claim is a test assumption or a product
  defect (mock-auth principal under api off). Fix the product if it is a product defect.
- `dev template-smoke --matrix --app-tests` passes for every variant.
- Template output changes: run `dev check-version` and follow `tw-pr`.

## Checklist

- [x] SmokeNoYarp web-spa-integration-tests pass
- [x] SmokeNoApi web-spa-integration-tests pass
- [x] SmokeNoApi web-spa-playwright-tests pass (or the local timeout is shown to be flaky and fixed)
- [x] `dev build` 0 warnings and 0 errors; `dev test` passes
- [x] `dev template-smoke --matrix --app-tests` passes (Docker running)
- [x] Dispatched nightly (`gh workflow run workflow.yml --ref <branch> -f mode=nightly`) passes
- [x] `ganda repo audit` clean

## Review

- Round 1: Done

## Notes

- Repro: `dev template-smoke --matrix --app-tests`, then rerun one suite with
  `cd artifacts/template-smoke/work/SmokeNoYarp/tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release`.
- Found by task 375's first full nightly run.

## Results

### What changed

All edits are in `tests/container-apps/web/web-spa-integration-tests/` (template content).

- **yarp off.** `infrastructure/base-test.cs`: the `ingress` health wait is inside `#if(yarp)`.
  `infrastructure/aspire-spa-test-application.cs`: the api-server client's base address
  comes from `BaseUrlResourceName`, which is `ingress` under yarp, `api-server` under `!yarp && api`
  (api-server serves `api/...` routes itself), and `web-server` when both flags are off (nothing
  calls that client then). The csproj defines `yarp` next to `api` (TWA0010).
- **api off.** The api-off missing NameIdentifier was a **test assumption, not a product defect**.
  The web-spa mock auth (`MockAuthenticationRegistration`, `MockAuthenticationStateProvider` with
  `NameIdentifier`) ships in every variant. The test host skipped it under `!api` and used its own
  `PermittedAuthenticationStateProvider`, which had permission claims only. The host now uses the
  product mock registration in every variant, and the stand-in is deleted. `global-usings.cs`
  makes `Microsoft.Extensions.Configuration` and `TimeWarp.Architecture.Services` unconditional.
- **Playwright Ctrl+K (api off).** Not related: that test boots its own in-process web host
  and does not use the integration suite's auth stand-in. It passed 17/17 for SmokeNoApi on
  the local run below and on the earlier CI nightly. The earlier 30 s local timeout looks like a
  flake under load. No change.

### Gates

- `dev build`: 0 warnings, 0 errors. `dev test`: exit 0. `dev check-version`: source 2.0.0-beta.20
  is ahead of nuget 2.0.0-beta.19, so no bump is needed. `ganda repo audit`: pass.
- `dev template-smoke --matrix --app-tests` (local, Docker): every project passed in SmokeDefault,
  SmokeNoPostgres, SmokeNoApi (web-spa-integration 285/285, playwright 17/17), SmokeNoWeb, and
  SmokeNoYarp (web-spa-integration 286/286, playwright 17/17).
  One unrelated flake: SmokeNoGrpc aspire-tests
  `FirstRunOnEmptyDatabase_Should_LogNoErrorFromWebServer` saw one EF `Database.Connection[20004]`
  error. web-server's first postgres connect timed out after about 21 s while the machine was
  loaded (that suite took 113 s, against about 57 s in the other variants). web-server already
  `WaitFor`s postgres. Rerun alone, the same generated suite passed 57/57. This change does not
  touch aspire-tests. If it recurs on the nightly, it needs its own task.
- No generated variant has a double blank line in the three changed files.
- The nightly dispatched on this branch passed (every variant, including SmokeNoGrpc):
  https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/38877166403

### How to validate

Smoke:

```bash
dev template-smoke --matrix --app-tests
cd artifacts/template-smoke/work/SmokeNoYarp/tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release
cd ../../../../../SmokeNoApi/tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release
```

Expect: every variant ends with its projects green. SmokeNoYarp web-spa-integration-tests
pass (no `ingress` wait; weather fetch reaches api-server directly). SmokeNoApi
web-spa-integration-tests pass, including the four `CatalogAgent_Should` conversation tests that
threw NullReferenceException before. In SmokeNoYarp, `aspire-spa-test-application.cs` has
`BaseUrlResourceName = "api-server"`. In SmokeDefault it is `"ingress"`.

### Review disposition

- Rounds: 1. Effort 1, roster: general.
- Final counts: bug 0, suggestion 1 fixed, nit 0. Open: 0.
- Disposition: **clean**. M1 (comments still described an ingress-only base URL in `aspire-spa-test-application.cs`) was fixed on this task. It was a comment-only edit.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Learnings

- Goal restatement vs original task: consistent; no misread.

## Session

- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T09:45:20Z
- Review oracle: tw-implementation-review by claude (claude-opus-5-5), effort 1, clean — 2026-10-16
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-16T09:47:14Z
