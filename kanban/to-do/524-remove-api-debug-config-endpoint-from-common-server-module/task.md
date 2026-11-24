# Remove /api/debug-config endpoint from CommonServerModule

## Description

`CommonServerModule.ConfigureEndpoints` in `source/foundation/foundation-server/common-server-module.cs` currently maps an unauthenticated `GET /api/debug-config` endpoint whenever `webApplication.Environment.IsDevelopment()` is true. That endpoint calls `IConfigurationRoot.GetDebugView()`, which dumps the full configuration tree including all process environment variables and secret values as plain text.

Because `TimeWarp.Foundation.Server` is shared by every server host (`api-server`, `web-server`, and generated template apps), any host running in `Development` automatically exposes process secrets over HTTP with no authentication.

Remove the `/api/debug-config` endpoint completely from `CommonServerModule` and add a regression test asserting that `GET /api/debug-config` returns `404 Not Found` even in `Development`.

## Scope

1. **Remove `/api/debug-config` in `source/foundation/foundation-server/common-server-module.cs`:**
   - Delete the `MapGet("/api/debug-config", ...)` registration and `configurationRoot.GetDebugView()` call from `CommonServerModule.ConfigureEndpoints(WebApplication)`.
   - Keep `public static void ConfigureEndpoints(WebApplication webApplication) { }` as an explicit no-op hook satisfying `IAspNetModule` (matching `ConfigureConfiguration` and `ConfigureMiddleware`), so existing host callers in `api-server` and `web-server` remain unchanged.
   - Remove any unused local variables or `using` directives (zero warnings).
2. **Update docs and comments:**
   - Update `#region Design`, class XML `<summary>`, and `ConfigureEndpoints` XML `<summary>` in `source/foundation/foundation-server/common-server-module.cs` to remove references to `/api/debug-config` and "optional debug config".
   - Update `#region Design` in `source/foundation/foundation-server/open-api-discovery-coverage.cs` where it references `/api/debug-config`.
   - Search `source/`, `tests/`, and `skills/` for any remaining references to `debug-config` or `GetDebugView` and clean up or verify none remain.
3. **Add a Docker-free regression test:**
   - Add a Jaribu test (following the tiny `WebApplication` + `UseTestServer()` pattern in `tests/container-apps/web/web-server-integration-tests/open-api-discovery-coverage-tests.cs`) that builds a host with `EnvironmentName = Environments.Development`, invokes `CommonServerModule.ConfigureEndpoints(app)`, starts the test server, and asserts `GET /api/debug-config` returns `HttpStatusCode.NotFound`.
   - Keep the test completely Docker-free so it runs reliably in `ci (fast)` and `template-smoke`.

## Checklist

- [ ] Remove `/api/debug-config` and `GetDebugView()` from `CommonServerModule.ConfigureEndpoints` in `source/foundation/foundation-server/common-server-module.cs`
- [ ] Update comments and XML docs in `common-server-module.cs` and `open-api-discovery-coverage.cs`
- [ ] Verify no other `debug-config` or `GetDebugView` endpoints remain in `source/`, `tests/`, or `skills/`
- [ ] Add a Development-environment regression test asserting `GET /api/debug-config` returns `404 Not Found`
- [ ] Verify `./bin/dev build` (0 warnings, 0 errors), targeted `./bin/dev test`, and `ganda repo audit`

## Session

- Created: 3163313 (2026-11-24)

## Notes

- Keep `ConfigureEndpoints(WebApplication)` on `CommonServerModule` as a no-op to preserve `IAspNetModule` binary/source compatibility across hosts.
- Related: Task 458 (OpenAPI review). Do not record any environment or host-specific secret details in code, tests, commit messages, or docs.
