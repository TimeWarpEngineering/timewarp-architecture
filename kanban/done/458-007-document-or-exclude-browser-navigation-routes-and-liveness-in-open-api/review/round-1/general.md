# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-007-document-or-exclude-browser-navigation-routes-and` vs `master`, against task 458-007 and `## Goal (implementer restatement)`

## Summary

The three identity operations now state the wire contract the task asked for: sign-out is 303 with `Location` and 400 with the host antiforgery field, the token route is 200 JSON, and the Entra challenge has the `mode` enum, optional `returnUrl`, and 302. Liveness `GET /web-server/alive` is generated from `IngressPaths.WebServerAlivePath` and the health sentence uses that same constant. The recorded build, OpenAPI and identity tests, and audit cover that work; those gates were not re-run. The new route walk does not see production `GET /alive`, and the description's carve-out is narrower than the routes the walk ignores. `sha256sum` on `host-discovery/SKILL.md` was run because a stale skill digest was a suspected bug; the file hash is `5d57dd1bd6f37ce461ded33ec1033773f68fa79adddc36f3a72d88584dac0da9`, which matches `index.json`.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-server/program.cs:101
- Description: `Program.Main` calls `MapDefaultEndpoints`, which maps `HealthEndpointPaths.Alive` (`GET /alive`) in every environment (`aspire-service-defaults/extensions.cs` lines 115–122). That path is not an OpenAPI operation and not an `x-timewarp-excluded-routes` entry. The shared list names only `IngressPaths.WebServerAlivePath` (`GET /web-server/alive`) and mentions `/alive` as the ingress rewrite target (`open-api-discovery-coverage.cs` lines 38 and 61–70). The ingress catch-all forwards an unmatched public path, including `/alive`, to web-server (`ingress-resource.cs` line 361), so the public origin serves it. The new `info.description` still says a missing path is an exclusion, and `/alive` is not in the Razor, static-file, or Blazor carve-out. `WebServer_Given_` never sees the route: `WebApplicationHost` runs `ConfigureMiddleware` and `ConfigureEndpoints` and does not call `MapDefaultEndpoints` (`web-application-host.cs` lines 123–124). The tiny-host liveness test only asserts `/web-server/alive`. The task requires each served non-operation route to be listed. The Goal restatement says the real host walk checks every served route outside those families; `/alive` is outside them and is not listed. Results records the hole instead of closing it.
- Suggestion: Add `GET` `HealthEndpointPaths.Alive` to the shared exclusion list, with a reason that it is the same self check as `IngressPaths.WebServerAlivePath`. Extend the route walk so a host that maps `MapDefaultEndpoints` fails when `/alive` is absent (call the same mapping production uses, or assert the constant directly). Keep the health description on `IngressPaths.WebServerAlivePath`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:220
- Description: `Description()` says a missing path is an exclusion, except Razor pages, static files, and Blazor framework routes. `WebServer_Given_.IsUnlistedFamily` also skips `/openapi/`, `/scalar`, and the Development-only paths (`open-api-discovery-coverage-tests.cs` lines 335–347 and 417–424). `WebServerOpenApiExcludedRoutes`'s Design region says the description names the OpenAPI document, Scalar, and Development-only routes. It does not. Served `/openapi/v1.json` and `/scalar/v1` are therefore missing paths the note does not carve out.
- Suggestion: Name those families in `info.description` with the same words the test uses, or stop skipping them and list the routes. Make the Design comment match the sentence.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/web-server-open-api-excluded-routes.cs:37
- Description: The dictation exclusion's method is `GET`. `TranscribeAgentAudioEndpoint.Map` registers `HttpMethods.Get` and `HttpMethods.Connect` on `TranscribeAgentAudio.StreamPath` (`transcribe-agent-audio-endpoint-server.cs` line 36). The reason prose mentions CONNECT; the `method` field does not. `Every_Served_Route_Should_Be_An_Operation_Or_Listed` compares paths only (`open-api-discovery-coverage-tests.cs` lines 380–404), so a missing method stays green.
- Suggestion: Add a second excluded entry with method `CONNECT` (same path and reason). Assert both methods for that path in the discovery test, not only the path.
- Status: open
