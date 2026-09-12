# Round 1 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:115
- Description: TWA0019 still builds its name set from `ReferencedAssemblySymbols`, while discovery now walks only `GetMarkedReferencedAssemblies`. A configured `IngressWebContractAssemblies` entry that is referenced but unmarked therefore skips TWA0019 and yields an empty `WebServerApiRoutePrefixes.All` with no diagnostic — a new silent-empty path this change introduces. Product `web-contracts` is stamped, so the in-repo path is fine; task scope required FastEndpoint TWE008 and only “share the filter” for ingress.
- Suggestion: Align TWA0019 with marked names (or report when a configured name is referenced but unmarked), mirroring TWE008’s unmarked case.
- Source: general
- Disposition notes: TWA0019 now uses `GetMarkedReferencedAssemblies`. Descriptor, Design region, AGENTS.md, Unshipped notes, and `Should_Report_TWA0019_When_Configured_Assembly_Is_Unmarked` added. Sourcegenerator tests 84 passed.

### M2 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/mock-response-factory-registry-generator-tests.cs:71
- Description: Comment still says the assembly name `"Test.Contracts"` “satisfies the name filter,” but the generator no longer filters on a `contracts` substring — it uses `[assembly: ApiEndpointsEmbedded]` (and the test already stamps via `MarkerStubs`).
- Suggestion: Reword to say the contract ref must carry `ApiEndpointsEmbedded` (assembly name is incidental).
- Source: general
- Disposition notes: Comment reworded to the marker filter; assembly name called incidental.

## Duplicates / conflicts

- None. Single general reviewer; two independent findings.
