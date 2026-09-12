# Round 1 — general
**Date:** 2026-09-12
**Scope reviewed:** branch task/006-001-fastendpoint-scan-only-marked-contracts-assemblies vs origin/master

## Summary

The branch lands a TypedId-style `[assembly: ApiEndpointsEmbedded]` participate filter shared by FastEndpoint, ingress, and mock-registry; makes `ApiEndpointContractAssemblies` a required AssemblyName allow-list with TWE008 fail-closed; and adds equatable `GenerationOptions` plus content-equal `EndpointEmitModel.Tags` so `.Collect()` does not rebuild on identical content. Product stamps and `api-server`/`web-server` allow-lists match the brief; docs no longer recommend the fictional `TimeWarp.Architecture.Web.Contracts` MSBuild value. Overall risk is low — two follow-ups below, neither blocking the product path.

## Issues

### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:115
- Description: TWA0019 still builds its name set from `ReferencedAssemblySymbols`, while discovery now walks only `GetMarkedReferencedAssemblies`. A configured `IngressWebContractAssemblies` entry that is referenced but unmarked therefore skips TWA0019 and yields an empty `WebServerApiRoutePrefixes.All` with no diagnostic — a new silent-empty path this change introduces. Product `web-contracts` is stamped, so the in-repo path is fine; task scope required FastEndpoint TWE008 and only “share the filter” for ingress.
- Suggestion: Align TWA0019 with marked names (or report when a configured name is referenced but unmarked), mirroring TWE008’s unmarked case, if ingress fail-closed parity is desired.
- Status: open

### Issue 2 — Severity: nit
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/mock-response-factory-registry-generator-tests.cs:71
- Description: Comment still says the assembly name `"Test.Contracts"` “satisfies the name filter,” but the generator no longer filters on a `contracts` substring — it uses `[assembly: ApiEndpointsEmbedded]` (and the test already stamps via `MarkerStubs`).
- Suggestion: Reword to say the contract ref must carry `ApiEndpointsEmbedded` (assembly name is incidental).
- Status: open
