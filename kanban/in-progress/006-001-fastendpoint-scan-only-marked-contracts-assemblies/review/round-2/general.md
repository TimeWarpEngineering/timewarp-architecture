# Round 2 — general
**Date:** 2026-09-12
**Scope reviewed:** post-fix delta for M1/M2 on task/006-001-fastendpoint-scan-only-marked-contracts-assemblies

## Summary

Both prior findings hold as fixed. TWA0019 now builds its name set from `GetMarkedReferencedAssemblies`, so configured-but-unmarked and typo cases both diagnose; discovery still walks only marked refs. The mock-registry test comment correctly describes the `ApiEndpointsEmbedded` participate filter. Fix-delta scan found no new defects.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs
- Description: Verified. TWA0019 uses `HostedRouteDiscovery.GetMarkedReferencedAssemblies` for the configured-name check; descriptor / Design region / AGENTS.md / AnalyzerReleases.Unshipped.md all mention marked refs and missing `[assembly: ApiEndpointsEmbedded]`. Discovery foreach also uses marked refs only. `Should_Report_TWA0019_When_Configured_Assembly_Is_Unmarked` compiles with `stampMarker: false` and asserts TWA0019 plus empty `All`; typo test `Should_Report_TWA0019_When_Configured_Assembly_Not_Found` remains.

### M2 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/mock-response-factory-registry-generator-tests.cs
- Description: Verified. Comment no longer claims `"Test.Contracts"` satisfies a name filter; it states the generator scans assemblies stamped `[assembly: ApiEndpointsEmbedded]` and that the assembly name is incidental.

## Issues
