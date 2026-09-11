# Round 3 — general
**Date:** 2026-09-11
**Scope reviewed:** branch vs origin/master through d2f33a9c; re-verify M1/M2; eval.yaml grader hyphen

## Summary

Product commits move `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` to public post-init types in `TimeWarp.Foundation.Features`, switch mixin discovery to `ForAttributeWithMetadataName` (metadata names include the `Attribute` suffix), and match FastEndpoint / ingress / TWA0006 / TWA0014 by FQN string via `HostedRouteDiscovery`. Dual-mode is covered: attribute emit ignores consumer `RootNamespace`, and docs/skill/release notes agree. Post-disposition `eval.yaml` hyphenates the grader name and ends with a newline. Overall risk is low; M1/M2 still hold; no new defects found.

## Prior findings

### M1 — Severity: suggestion — Status: fixed
- File: `tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-more-tests.cs` (`Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name`); `tests/analyzers/timewarp-architecture-sourcegenerator-tests/ingress-route-prefix-generator-tests.cs` (same); `source/analyzers/shared/hosted-route-discovery.cs` (`IsApiRouteAttribute`)
- Description: Re-verified on HEAD. Both suites still place `Other.Lib.ApiRouteAttribute` on an `[ApiEndpoint]` nested Query and assert ignore behavior (FastEndpoint: TWE007 message contains `missing ApiRoute`, zero sources, no `api/collided`; ingress: no `api/collided`, empty `All`). Call sites still use `IsApiRouteAttribute` / FQN constants; no leftover `ApiRouteAttributeSimpleName` or `AttributeClass?.Name == "ApiRouteAttribute"` in product analyzers/generators.
- Status: fixed

### M2 — Severity: nit — Status: fixed
- File: `source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs` Design region (lines 13–14)
- Description: Re-verified on HEAD. Design still states nested `[ApiRoute]` is matched by FQN `TimeWarp.Foundation.Features.ApiRouteAttribute` via `HostedRouteDiscovery.IsApiRouteAttribute`; ClientOnly stays simple-name. No leftover “simple-name attribute matching” wording for ApiRoute.
- Status: fixed

## Issues

<!-- none -->
