# Round 3 — merged findings
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
- Description: Round-3 re-verify on HEAD through `d2f33a9c`. FastEndpoint and ingress still ignore `Other.Lib.ApiRouteAttribute` (TWE007 `missing ApiRoute`, no `api/collided`). No leftover simple-name `ApiRouteAttribute` match in product analyzers/generators.
- Suggestion: (implemented)
- Source: general
- Disposition notes: Carried from round 1; remains `fixed`.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs:13
- Description: Round-3 re-verify. Ingress Design region still states FQN match via `IsApiRouteAttribute`; ClientOnly stays simple-name.
- Suggestion: (implemented)
- Source: general
- Disposition notes: Carried from round 1; remains `fixed`.

## Resolved prior

- M1, M2 carried from rounds 1–2; both remain `fixed`. No new IDs.
- Post-disposition `eval.yaml` grader hyphen (`invokes-web-api-contracts`) reviewed; no finding.

## Duplicates / conflicts

- None. Single reviewer; zero new issues.
