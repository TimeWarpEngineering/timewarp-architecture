# Round 2 — general
**Date:** 2026-11-26
**Scope reviewed:** commit `fd3552530` (fix for round 1 M1, M2, M3) against its parent, plus the updated `## Results` and `### How to validate` record

## Summary

The fix lists `GET /alive` from `HealthEndpointPaths.Alive` on the shared exclusion list and checks `MapDefaultEndpoints` on a Production host against the real web-server document. `info.description` now names the families the route walk skips, through `OpenApiDiscoveryCoverage.UnlistedFamilies`. The dictation path has a `GET` entry and a `CONNECT` entry, and the walk compares method and path when the endpoint declares methods. The recorded build, the discovery-coverage tests (8/8), the served-route tests (3/3), and `ganda repo audit` cover that delta. Those gates were not re-run. No new defect on the fix.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:80
- Description: `MapDefaultEndpoints` maps `HealthEndpointPaths.Alive` (`GET /alive`) in every environment and maps `HealthEndpointPaths.Health` only in Development (`aspire-service-defaults/extensions.cs` lines 115–128). The shared list now has a `GET` entry for `HealthEndpointPaths.Alive` beside `IngressPaths.WebServerAlivePath`. `Default_Endpoints_Should_Be_Listed` builds a Production host, calls `MapDefaultEndpoints`, and fails when those routes are absent from the real document. The ingress health route rewrites `/web-server/alive` to `/alive`, and the catch-all forwards an unmatched public path to web-server (`ingress-resource.cs` lines 347–361), which matches the entry's reason. The health description still names liveness from `IngressPaths.WebServerAlivePath`.
- Suggestion: none
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:54
- Description: `UnlistedFamilies` names Razor pages, static files, Blazor framework routes (`/_framework`, `/_blazor`), `/openapi/v1.json`, `/scalar/v1`, and Development-only routes. `Description()` appends that constant after "Not listed: ". Both Design regions point at the constant. `Document_Given_.Unlisted_Families_Should_Name_What_The_Route_Walk_Skips` requires each of those phrases in `info.description`. The walk's skip list is the same set: component metadata, static-asset metadata, the `{**path:file}` fallback, the `/_blazor`, `/_framework`, `/openapi/`, and `/scalar` prefixes, and the two Development-only paths.
- Suggestion: none
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/web-server-open-api-excluded-routes.cs:43
- Description: `TranscribeAgentAudioEndpoint.Map` registers `HttpMethods.Get` and `HttpMethods.Connect` on `TranscribeAgentAudio.StreamPath`. The exclusion list has both methods with one shared reason. `Unlisted` compares `method + path` when `IHttpMethodMetadata` is present, and compares the path alone when it is absent. `Dictation_Should_List_Every_Mapped_Method` requires both methods in the real document. The discovery-test diff adds assertions and the method check. It does not drop or loosen an existing one.
- Suggestion: none
- Status: fixed

## Issues

<!-- No new issues. -->
