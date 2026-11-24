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

## Goal (implementer restatement)

Every server host that composes `CommonServerModule` must stop serving the configuration dump at
`GET /api/debug-config`, including in `Development`. `ConfigureEndpoints` stays as an empty
`IAspNetModule` hook so `api-server` and `web-server` keep calling it unchanged.

Acceptance:

- `common-server-module.cs` has no `MapGet("/api/debug-config")` and no `GetDebugView()`; no unused
  locals or usings remain.
- The Design region and XML docs in `common-server-module.cs`, and the Design region in
  `open-api-discovery-coverage.cs`, no longer mention the endpoint.
- No `debug-config` or `GetDebugView` reference remains under `source/`, `tests/`, or `skills/`.
- A Docker-free Jaribu test boots a tiny `UseTestServer()` host in `Development`, calls
  `CommonServerModule.ConfigureEndpoints`, and asserts `GET /api/debug-config` is `404 Not Found`.
- `./bin/dev build` is clean, the new test passes, and `ganda repo audit` passes.

## Checklist

- [x] Remove `/api/debug-config` and `GetDebugView()` from `CommonServerModule.ConfigureEndpoints` in `source/foundation/foundation-server/common-server-module.cs`
- [x] Update comments and XML docs in `common-server-module.cs` and `open-api-discovery-coverage.cs`
- [x] Verify no other `debug-config` or `GetDebugView` endpoints remain in `source/`, `tests/`, or `skills/`
- [x] Add a Development-environment regression test asserting `GET /api/debug-config` returns `404 Not Found`
- [x] Verify `./bin/dev build` (0 warnings, 0 errors), targeted `./bin/dev test`, and `ganda repo audit`

## Session

- Created: 3163313 (2026-11-24)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 970ee484-a715-4332-9d37-9e0ed7254053, max-turns 500, Done — 2026-11-24T23:53:06Z

## Results

- `source/foundation/foundation-server/common-server-module.cs`: `ConfigureEndpoints` is now an
  empty `IAspNetModule` hook (no `MapGet`, no `GetDebugView`). Design region, class summary, and
  method summary rewritten.
- `source/foundation/foundation-server/open-api-discovery-coverage.cs`: Design region no longer
  names the route.
- `source/foundation/foundation-server/global-usings.cs`: dropped `Microsoft.Extensions.Hosting`
  (only `IsDevelopment()` used it; IDE0005 failed the build otherwise).
- New `tests/container-apps/web/web-server-integration-tests/common-server-module-endpoints-tests.cs`:
  Development `UseTestServer()` host, `ConfigureEndpoints`, `GET /api/debug-config` → 404.
- `grep -rn "debug-config\|GetDebugView" source tests skills` finds only the new test's route string.

### Gates

- `./bin/dev build`: passed, 0 warnings, 0 errors (first run failed on IDE0005 in
  foundation-server `global-usings.cs`; fixed by removing the unused `Microsoft.Extensions.Hosting`).
- `./bin/dev test --no-build --project web-server-integration-tests --filter-class CommonServerModuleEndpoints_`
  (`common-server-module-endpoints-tests.cs`, a suite file in the csproj, not a standalone runfile:
  no `#if !JARIBU_MULTI` guard): passed, 1/1.
- `ganda repo audit`: passed (reused the recorded full pass for tree 2cb2418434cd from the commit hook).

### How to validate

Smoke: `./bin/dev test --project web-server-integration-tests --filter-class CommonServerModuleEndpoints_`
Expect: 1 test passes; the log shows `GET http://localhost/api/debug-config - 404`. Also
`grep -rn GetDebugView source` prints nothing.

## Notes

- Keep `ConfigureEndpoints(WebApplication)` on `CommonServerModule` as a no-op to preserve `IAspNetModule` binary/source compatibility across hosts.
- Related: Task 458 (OpenAPI review). Do not record any environment or host-specific secret details in code, tests, commit messages, or docs.
