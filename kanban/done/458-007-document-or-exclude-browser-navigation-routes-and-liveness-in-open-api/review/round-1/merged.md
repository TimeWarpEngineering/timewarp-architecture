# Round 1 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:101
- Description: `Program.Main` calls `MapDefaultEndpoints`, which maps `HealthEndpointPaths.Alive` (`GET /alive`) in every environment (`aspire-service-defaults/extensions.cs` lines 115–122). That path is not an OpenAPI operation and not an `x-timewarp-excluded-routes` entry. The shared list names only `IngressPaths.WebServerAlivePath` (`GET /web-server/alive`) and mentions `/alive` as the ingress rewrite target (`open-api-discovery-coverage.cs` lines 38 and 61–70). The ingress catch-all forwards an unmatched public path, including `/alive`, to web-server (`ingress-resource.cs` line 361), so the public origin serves it. The new `info.description` still says a missing path is an exclusion, and `/alive` is not in the Razor, static-file, or Blazor carve-out. `WebServer_Given_` never sees the route: `WebApplicationHost` runs `ConfigureMiddleware` and `ConfigureEndpoints` and does not call `MapDefaultEndpoints` (`web-application-host.cs` lines 123–124). The tiny-host liveness test only asserts `/web-server/alive`. The task requires each served non-operation route to be listed. The Goal restatement says the real host walk checks every served route outside those families; `/alive` is outside them and is not listed. Results records the hole instead of closing it.
- Suggestion: Add `GET` `HealthEndpointPaths.Alive` to the shared exclusion list, with a reason that it is the same self check as `IngressPaths.WebServerAlivePath`. Extend the route walk so a host that maps `MapDefaultEndpoints` fails when `/alive` is absent (call the same mapping production uses, or assert the constant directly). Keep the health description on `IngressPaths.WebServerAlivePath`.
- Source: general
- Disposition notes: Fixed. `OpenApiDiscoveryCoverage` lists `GET` `HealthEndpointPaths.Alive` beside `IngressPaths.WebServerAlivePath` (same self check). `WebServer_Given_.Default_Endpoints_Should_Be_Listed` maps `MapDefaultEndpoints` (the call `Program.Main` makes) on a Production host and checks its routes against the real web-server document; removing the entry fails it.

### M2 — Severity: suggestion — Status: fixed
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:220
- Description: `Description()` says a missing path is an exclusion, except Razor pages, static files, and Blazor framework routes. `WebServer_Given_.IsUnlistedFamily` also skips `/openapi/`, `/scalar`, and the Development-only paths (`open-api-discovery-coverage-tests.cs` lines 335–347 and 417–424). `WebServerOpenApiExcludedRoutes`'s Design region says the description names the OpenAPI document, Scalar, and Development-only routes. It does not. Served `/openapi/v1.json` and `/scalar/v1` are therefore missing paths the note does not carve out.
- Suggestion: Name those families in `info.description` with the same words the test uses, or stop skipping them and list the routes. Make the Design comment match the sentence.
- Source: general
- Disposition notes: Fixed. `info.description` reads "Not listed: " + `OpenApiDiscoveryCoverage.UnlistedFamilies` (Razor pages, static files, Blazor framework routes, `/openapi/v1.json`, `/scalar/v1`, Development-only routes). Both Design regions name that constant; `Document_Given_.Unlisted_Families_Should_Name_What_The_Route_Walk_Skips` checks each family the walk skips.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/web-server-open-api-excluded-routes.cs:37
- Description: The dictation exclusion's method is `GET`. `TranscribeAgentAudioEndpoint.Map` registers `HttpMethods.Get` and `HttpMethods.Connect` on `TranscribeAgentAudio.StreamPath` (`transcribe-agent-audio-endpoint-server.cs` line 36). The reason prose mentions CONNECT; the `method` field does not. `Every_Served_Route_Should_Be_An_Operation_Or_Listed` compares paths only (`open-api-discovery-coverage-tests.cs` lines 380–404), so a missing method stays green.
- Suggestion: Add a second excluded entry with method `CONNECT` (same path and reason). Assert both methods for that path in the discovery test, not only the path.
- Source: general
- Disposition notes: Fixed. The dictation path has a `GET` and a `CONNECT` entry (shared reason). The route walk now compares method and path for endpoints with method metadata; `Dictation_Should_List_Every_Mapped_Method` asserts both, and dropping CONNECT fails the walk naming `CONNECT /api/agent-chat/transcriptions/stream`.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
