# Round 2 — general
**Date:** 2026-09-11
**Scope reviewed:** post-fix uncommitted delta for M1/M2; HostedRouteDiscovery FQN helper

## Summary

Both claimed fixes hold. The new FastEndpoint and ingress `Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name` cases exercise `HostedRouteDiscovery.IsApiRouteAttribute` with `Other.Lib.ApiRouteAttribute` and would fail a silent revert to `AttributeClass?.Name == "ApiRouteAttribute"`. The ingress Design region now states FQN match for ApiRoute and simple-name only for ClientOnly. No new defects on the fix delta.

## Prior findings

### M1 — Severity: suggestion — Status: fixed
- File: `tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-more-tests.cs` (`Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name`); `tests/analyzers/timewarp-architecture-sourcegenerator-tests/ingress-route-prefix-generator-tests.cs` (same); `source/analyzers/shared/hosted-route-discovery.cs:37` (`IsApiRouteAttribute`)
- Description: Re-verified. FastEndpoint path: `EndpointEmitModel.FromSymbol` uses `IsApiRouteAttribute`; when the foreign attribute is ignored, the missing-ApiRoute branch sets `VerbUnresolved` / `"<missing ApiRoute>"` and `ProcessBatch` reports **TWE007** with zero sources — matching the new assertions. A simple-name revert would accept `Other.Lib.ApiRouteAttribute`, resolve verb `1` as fail-closed (`unresolvedVerbDisplay` `"1"`, not `"missing ApiRoute"`), and fail the message assertion. Ingress path: `TryGetHostedOperation` would accept the foreign route under simple-name and emit `api/collided`, failing `ShouldNotContain("api/collided")` and the empty-`All` check.
- Status: fixed

### M2 — Severity: nit — Status: fixed
- File: `source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs` Design region (lines 13–16)
- Description: Re-verified. Design now says nested `[ApiRoute]` is matched by FQN `TimeWarp.Foundation.Features.ApiRouteAttribute` via `HostedRouteDiscovery.IsApiRouteAttribute`; ClientOnly stays simple-name. No leftover “simple-name attribute matching” wording for ApiRoute.
- Status: fixed

## Issues

<!-- none -->
