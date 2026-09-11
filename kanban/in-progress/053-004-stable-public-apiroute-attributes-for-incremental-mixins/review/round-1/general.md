# Round 1 — general
**Date:** 2026-09-11
**Scope reviewed:** branch task/053-004-stable-public-apiroute-attributes-for-incremental vs origin/master

## Summary

The change moves `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` to public post-init types in `TimeWarp.Foundation.Features`, switches mixin discovery to `ForAttributeWithMetadataName`, and updates FastEndpoint / ingress / TWA0006 / TWA0014 call sites to FQN string matching via `HostedRouteDiscovery` (correct for per-contracts-assembly copies). Requirements check out: metadata names include the `Attribute` suffix, RootNamespace no longer drives emit, dual-mode `TimeWarp.Foundation.*` is left intact, and mixin tests cover foreign same-simple-name ignore plus existing `GetRoute` / `UserId` / OData members. Overall risk is low; remaining gaps are regression coverage on the shared discovery path and one stale design comment.

## Issues

### Issue 1 — Severity: suggestion
- File: source/analyzers/shared/hosted-route-discovery.cs:37
- Description: Mixin generator tests cover ignoring `Other.Lib.ApiRouteAttribute`, but FastEndpoint / ingress / coverage still discover routes only through `HostedRouteDiscovery.IsApiRouteAttribute`. There is no FastEndpoint or ingress (or analyzer) test that places a foreign same-simple-name `ApiRouteAttribute` on an otherwise hosted operation and asserts it is ignored. A silent revert of `IsApiRouteAttribute` to `AttributeClass?.Name == "ApiRouteAttribute"` would still pass the current FastEndpoint suite (harness stubs live only in `TimeWarp.Foundation.Features`).
- Suggestion: Add one FastEndpoint (or ingress) harness case with `Other.Lib.ApiRouteAttribute` on a nested Query/Command that also has `[ApiEndpoint]`, and assert no endpoint / prefix is emitted (and preferably TWE007 / skip as appropriate). Mirror for TWA0006 if cheap.
- Status: open

### Issue 2 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:13
- Description: Design region still says discovery uses “simple-name attribute matching” for `[ApiRoute]`, but this task switched that path to FQN via `HostedRouteDiscovery.IsApiRouteAttribute`.
- Suggestion: Update the comment to say FQN (`TimeWarp.Foundation.Features.ApiRouteAttribute`) and note ClientOnly remains simple-name.
- Status: open
