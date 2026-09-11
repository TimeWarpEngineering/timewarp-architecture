# Round 2 — merged findings
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
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-more-tests.cs; tests/analyzers/timewarp-architecture-sourcegenerator-tests/ingress-route-prefix-generator-tests.cs; source/analyzers/shared/hosted-route-discovery.cs:37
- Description: FastEndpoint and ingress now have `Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name`. Re-review confirmed a simple-name revert of `IsApiRouteAttribute` would fail both tests (TWE007 message would not be `missing ApiRoute`; ingress would emit `api/collided`).
- Suggestion: (implemented)
- Source: general
- Disposition notes: Round-2 re-verified. No TWA0006 mirror (not required).

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:13
- Description: Ingress Design region now states FQN match for `[ApiRoute]` via `HostedRouteDiscovery.IsApiRouteAttribute`; ClientOnly stays simple-name.
- Suggestion: (implemented)
- Source: general
- Disposition notes: Round-2 re-verified. No leftover “simple-name attribute matching” for ApiRoute.

## Resolved prior

- M1, M2 carried from round 1; both remain `fixed`. No new IDs.

## Duplicates / conflicts

- None.
