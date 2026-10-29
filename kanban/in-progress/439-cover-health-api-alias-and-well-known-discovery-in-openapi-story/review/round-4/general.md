# Round 4 — general
**Date:** 2026-10-29
**Scope reviewed:** delta since round 3 (round-3 base `3f596ae40`): commit `2ebd4893d` (`fix: route /api/health and the /api tip alias to web-server at ingress`). Product files: `source/container-apps/aspire/projects/aspire-app-host/program.cs`, `source/container-apps/web/projects/web-server/program.cs`, `source/container-apps/yarp/program.cs`, `source/foundation/foundation-contracts/configuration/ingress-paths.cs`, `source/foundation/foundation-server/open-api-discovery-coverage.cs`, `tests/container-apps/yarp/yarp-integration-tests/standalone-yarp-ingress-smoke-tests.cs`.

## Summary

Fix pass 3 (implement-fix-4, claude-opus-5-5) is the first fix pass with a product change. It adds `IngressPaths.WebServerHealthPath` (`/api/health`) and `IngressPaths.TipDiscoveryAliasPath` (`/api`) and reads them from web-server's `MapHealthChecks`, `OpenApiDiscoveryCoverage`, the standalone yarp in-memory routes, and the AppHost `#if web` block. The standalone yarp builder replaces the duplicate `TipDiscoveryAlias-api-slash` route with a `WebHealth-api-health` route. The AppHost drops its `"/api/"` route and adds a `/api/health` route. Two gateway tests request `/api/health`, `/api`, and `/api/` through YARP. M1 and M2 are fixed. No new findings.

## Verification

- `./bin/dev build`: 0 warnings, 0 errors.
- `tests/container-apps/yarp/yarp-integration-tests`: 19 of 19 passed. `ApiHealthThroughStandaloneYarp_Should_ReachWebServer` and `TipAliasThroughStandaloneYarp_Should_MatchWebServerTip` each pass when run alone by `--filter-method`.
- `tests/container-apps/web/web-server-integration-tests` `--filter-class OpenApiDiscoveryCoverage_`: 6 of 6 passed.
- `source/container-apps/web/features/tip/submit-tip/submit-tip-tests.cs` `--filter-class SubmitTipEndpoint_Given_`: 7 of 7 passed, including `Api Health_ Should_ Return Plaintext Status`.
- `TipDiscoveryAlias.IsAliasPath` accepts both `/api` and `/api/`, so one exact `/api` ingress route (a route template ignores a trailing slash) reaches the rewrite for both spellings. `SubmitTip` and `SubmitTipPost` are always mapped on `api/tip` (402 when tip is enabled, 503 when not), so the alias test's equal-status assertion cannot be satisfied by a 404 or a routing 500.
- In both builders the exact literals outrank the Api.Server `/api/{**catch-all}` by ASP.NET route precedence, so order does not matter. The AppHost route is inside `#if web`, which is where web-server exists.
- `ganda repo audit`: only `kanban-landed` fails, because this claim's kitchen is ahead of origin-home. That is the in-flight state; the host land step publishes it. No product audit failure.

## Issues

### Issue 1 — Severity: bug (carried M1)
- File: source/container-apps/yarp/program.cs:118-124; source/container-apps/aspire/projects/aspire-app-host/program.cs:863-864
- Description: Round-1 M1. OpenAPI advertised `GET /api/health` on the ingress origin, but ingress sent it to Api.Server's catch-all (404).
- Resolution: Both builders add an exact `/api/health` route to web-server from `IngressPaths.WebServerHealthPath`; web-server's `MapHealthChecks` reads the same constant. `ApiHealthThroughStandaloneYarp_Should_ReachWebServer` gets `text/plain` with a status name through the gateway.
- Status: fixed

### Issue 2 — Severity: bug (carried M2)
- File: source/container-apps/yarp/program.cs:111-117; source/container-apps/aspire/projects/aspire-app-host/program.cs:857-861
- Description: Round-1 M2. Separate `"/api"` and `"/api/"` ingress routes tied and returned 500 `AmbiguousMatchException` for the documented `GET|POST /api` alias.
- Resolution: One `/api` route per builder from `IngressPaths.TipDiscoveryAliasPath`. `TipAliasThroughStandaloneYarp_Should_MatchWebServerTip` checks `/api` and `/api/` through the gateway return the same status as web-server's `/api/tip`.
- Status: fixed

## Notes (not findings)

- The AppHost route table has no in-proc test in this suite; `aspire-tests` boots the AppHost and was not run here. The AppHost change mirrors the standalone builder line for line and compiles in `dev build`.
- Round-1 out-of-scope items (developer exception page on production YARP 500, `agent-skills/index.json` digests) are unchanged and remain out of scope.
