# Round 4 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/yarp/program.cs:118-124; source/container-apps/aspire/projects/aspire-app-host/program.cs:863-864; source/foundation/foundation-contracts/configuration/ingress-paths.cs:46
- Description: Carried from round 1. OpenAPI advertised `GET /api/health` on the ingress origin, but ingress sent `/api/health` to Api.Server's catch-all (404).
- Suggestion: See round-1 merged M1.
- Source: general
- Disposition notes: Fixed in `2ebd4893d`. Exact `/api/health` route to web-server in both ingress builders, read from `IngressPaths.WebServerHealthPath`, which `MapHealthChecks` and `OpenApiDiscoveryCoverage` also read. Verified by `ApiHealthThroughStandaloneYarp_Should_ReachWebServer` (yarp suite 19 of 19) and `dev build` 0 warnings.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/yarp/program.cs:111-117; source/container-apps/aspire/projects/aspire-app-host/program.cs:857-861; source/foundation/foundation-contracts/configuration/ingress-paths.cs:52
- Description: Carried from round 1. Duplicate `"/api"` and `"/api/"` ingress routes tied and returned 500 `AmbiguousMatchException` for the documented `GET|POST /api` alias.
- Suggestion: See round-1 merged M2.
- Source: general
- Disposition notes: Fixed in `2ebd4893d`. One `/api` route per builder from `IngressPaths.TipDiscoveryAliasPath`; the template also matches `/api/`, and web-server's `TipDiscoveryAlias.IsAliasPath` accepts both spellings. Verified by `TipAliasThroughStandaloneYarp_Should_MatchWebServerTip` (`/api` and `/api/` through the gateway return web-server's `/api/tip` status).

## Duplicates / conflicts

- None.

## Process note

- Fix pass 3 ran on claude-opus-5-5 (implement-fix-4, 26 turns) after two implementer-grok passes that each ended after one model call with no change. This is the first fix pass that changed product code.
