# Round 1 — merged findings
**Date:** 2026-09-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/shared/hosted-route-discovery.cs:37
- Description: Mixin generator tests cover ignoring `Other.Lib.ApiRouteAttribute`, but FastEndpoint / ingress / coverage still discover routes only through `HostedRouteDiscovery.IsApiRouteAttribute`. There is no FastEndpoint or ingress (or analyzer) test that places a foreign same-simple-name `ApiRouteAttribute` on an otherwise hosted operation and asserts it is ignored. A silent revert of `IsApiRouteAttribute` to `AttributeClass?.Name == "ApiRouteAttribute"` would still pass the current FastEndpoint suite (harness stubs live only in `TimeWarp.Foundation.Features`).
- Suggestion: Add one FastEndpoint (or ingress) harness case with `Other.Lib.ApiRouteAttribute` on a nested Query/Command that also has `[ApiEndpoint]`, and assert no endpoint / prefix is emitted (and preferably TWE007 / skip as appropriate). Mirror for TWA0006 if cheap.
- Source: general
- Disposition notes: Added `Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name` on FastEndpoint (TWE007 missing ApiRoute, 0 sources) and ingress (no `api/collided`, empty All). Filter-method 2 passed.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:13
- Description: Design region still says discovery uses “simple-name attribute matching” for `[ApiRoute]`, but this task switched that path to FQN via `HostedRouteDiscovery.IsApiRouteAttribute`.
- Suggestion: Update the comment to say FQN (`TimeWarp.Foundation.Features.ApiRouteAttribute`) and note ClientOnly remains simple-name.
- Source: general
- Disposition notes: Design region now states FQN match via `IsApiRouteAttribute`; ClientOnly stays simple-name.

## Duplicates / conflicts

- None. Single reviewer; both findings kept.
