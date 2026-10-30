# Round 3 — general
**Date:** 2026-10-29
**Scope reviewed:** branch `task/438-document-402503-on-tip-and-metered-capability` at `aa9c7b76e`,
task-438 product commits `3b31d56f3` and `0264769f3` (diff `ce32e80af..HEAD` outside `kanban/`), plus
the CI failure recorded on `task.md` (PR #600, `ci (integration)`, run 40803144337). Tasks 435 and 436
share the merge-base diff and are out of scope.

## Summary

The product tree has not changed since the round-2 fix commit (`git diff 0264769f3..HEAD -- . ':!kanban'`
is empty). M1, M2, and M3 stay fixed. The OpenAPI transformer, the web-server registration through the
new `CommonServerModule.AddOpenApi` overload, and the `llms.txt` / `auth.md` lines still match the Goal
restatement and the original task. The restatement is consistent with the task, so there is no misread.

The change is not mergeable as it stands. The payment OpenAPI test declares four public FastEndpoints
stub classes at namespace scope in the shared `web-server-integration-tests` assembly. `api-server`
(`source/container-apps/api/projects/api-server/program.cs:101-109`) calls `AddFastEndpoints` with
`Assemblies` but without `DisableAutoDiscovery`, so every in-process api-server host in that test
process scans the test assembly and registers the stubs beside the real endpoints. I reproduced it
locally with one api-server-backed class:

```
dotnet test web-server-integration-tests.csproj -- --filter-class AgentProtectedEndpoint_
fail: FastEndpoints.DuplicateHandlerRegistration[2]
      The route [POST:api/tip] has 2 endpoints registered to handle requests!
      The route [GET:api/tip] has 2 endpoints registered to handle requests!
      The route [GET:/api/hello] has 2 endpoints registered to handle requests!
      The route [GET:api/demo/metered-capability] has 2 endpoints registered to handle requests!
Test run summary: Failed!  total: 5  failed: 5
```

That is the same four routes and the same `Duplicate routes detected!` CI reported twice (199 of 403
failed in `web-server-integration-tests`). It is deterministic. Every `HostGraphFactory.CreateWebWithApiAsync`
class in the assembly (about twenty files) fails at startup. The `OpenApiPaymentResponses_` host itself
is isolated correctly (`DisableAutoDiscovery = true` plus a namespace filter); the leak is in the
other direction.

Why task 436's stubs did not trip this: its `MeteredEndpoint` uses `"/api/demo/metered-capability"`
with a leading slash, which FastEndpoints treats as a different route string from the contract
`api/demo/metered-capability`, and nothing else in the assembly declared `api/tip`. The round-1 M2 fix
correctly moved the 438 stubs onto the contract `RouteTemplate` constants, which is exactly what makes
them collide with the real endpoints. The `GET:/api/hello` duplicate is the 438 `HelloEndpoint` against
the 436 `PublicHelloEndpoint`, both `"/api/hello"`.

Process note: this walk resumed after the CI failure was recorded on `task.md` (commit `3ebc8b917`,
"CI failure (open, must fix)"). The resumed implement oracle returned `Done` in 26 ms (progress log)
and landed only one Session line (`aa9c7b76e`). No product change addressed the recorded failure.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:236
- Description: `TipGetEndpoint`, `TipPostEndpoint`, `MeteredCapabilityEndpoint`, and `HelloEndpoint`
  are public namespace-scope FastEndpoints types in the shared integration-test assembly. api-server's
  `AddFastEndpoints` (api-server/program.cs:101) leaves auto-discovery on, so every in-process api-server
  host registers them next to the real `api/tip`, `api/demo/metered-capability`, and the 436 `/api/hello`
  stub, and fails with `Duplicate routes detected!`. 199 tests fail in CI and the PR cannot merge.
- Suggestion: Stop the stubs reaching any other host's discovery. The smallest product-side fix mirrors
  web-server (`web-server/program.cs:346-352`): set `options.DisableAutoDiscovery = true` in api-server's
  `AddFastEndpoints`, keeping its explicit `Assemblies`. Check first that api-server does not rely on
  auto-discovery for endpoints outside `Api.Server` / `Api.Contracts`. Keep the stubs on the contract
  `RouteTemplate` constants (M2), because the transformer matches on `RelativePath`. If a test-only fix
  is preferred instead, the stubs must be types FastEndpoints discovery cannot see from api-server.
  Proof gate per `task.md`: the whole integration group
  (`dotnet run tools/dev-cli/dev.cs -- test --group integration`), not only the payment class; at minimum
  `--filter-class AgentProtectedEndpoint_` must pass alongside `--filter-class OpenApi`.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:41
- Description: `FreeRoutesNever402` spells `/api/tip` and `/api/demo/metered-capability` as literals,
  while the Design region says routes come from the contract `RouteTemplate` constants and `TipRoute` /
  `MeteredRoute` already hold them. The test `Info_Should_SayFreeRoutesNeverReturn402` asserts the
  description contains the constant-derived paths, so a route rename would be caught, but the two copies
  still have to agree by hand.
- Suggestion: Build the sentence from `TipRoute` and `MeteredRoute` (prefixed with `/`), or leave as-is
  because the test pins it. Optional; do not let it delay the M4 fix.
- Status: open
