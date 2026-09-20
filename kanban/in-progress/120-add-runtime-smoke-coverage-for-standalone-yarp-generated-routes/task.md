# Add runtime smoke coverage for standalone yarp generated routes

## Description

From 107 review finding G3 (2026-07-23): the standalone yarp project now consumes the
generated WebServerApiRoutePrefixes via a cross-provider LoadFromMemory merge (in-memory routes
→ config-defined Web.Server cluster), but that path is BUILD-VERIFIED ONLY — aspire-tests
exercise the AppHost YARP, not standalone yarp. Accepted to ship because the AppHost is the
dogfooded/verified public chain (112); this task adds an automated runtime smoke for the
standalone gateway: boot yarp + web-server (no Aspire), request /api/Hello and /api/identity
through it, assert the generated carve-outs route to Web.Server (and foreign-Host handling on
the http cluster). Also covers the Development cluster https→http change made in 107.

## Checklist

- [x] Test host that boots standalone yarp + web-server with test config
- [x] Facts: generated prefix routes reach Web.Server (200/401-not-404); foreign-Host ok
- [x] Runs in dev test (respect fixed-port discipline)

## Notes

Origin: 107 review/round-1 G3 verdict (ship + follow-up). Standalone ReverseProxy config is Development-only today; test scope matches.

In-proc lane (not Aspire): `HostGraphFactory.CreateWebYarpAsync` boots Web then standalone YARP. Web also listens on http :7001 so the Development cluster hop is http (the 107 https→http change). `YarpWebClusterHttpAddressFilter` rewrites the config `Web.Server` destination onto that listener because there is no Aspire DCP to resolve `http://_http.web-server`. Generated `LoadFromMemory` routes still target the config cluster id.

## Session

- Implementer: grok session 01a0bfca-5613-7d82-8ab4-1b60bb37b3ed (2026-09-21)
- Implementer: grok session 01a0bfda-b26b-7b90-a2bc-c638a95e568e (2026-09-21)

## Results

Standalone YARP generated-route runtime smoke is in-proc, not AppHost.

**What**
- `HostGraphFactory.CreateWebYarpAsync` boots Web.Server + standalone yarp (no Api, no Aspire).
- Web.Server binds https :7000 and http :7001. YARP :8443 forwards Web.Server over http :7001 with original Host preserved.
- Jaribu MTP suite `tests/container-apps/yarp/yarp-integration-tests` requests `/api/Hello`, `/api/identity/session`, and `/api/Roles` through the gateway.

**Files**
- `tests/common/timewarp-testing/applications/web-test-server-application.cs` — http :7001
- `tests/common/timewarp-testing/applications/yarp-test-server-application.cs` — optional Api; `IProxyConfigFilter` pins Web.Server to :7001
- `tests/common/timewarp-testing/host-graph-factory.cs` — `CreateWebYarpAsync`; preflight :7001
- `tests/container-apps/yarp/yarp-integration-tests/**` — four smoke facts
- `.template.config/template.json` — exclude the suite when `web` or `yarp` is off
- `timewarp-architecture.slnx` — project behind `#if (yarp)` / `#if (web)`
- `AGENTS.md`, `tools/dev-cli/endpoints/test-command.cs` — document web-http=7001

**Decisions**
- In-proc HostGraph, not Aspire.Hosting.Testing (task brief: standalone gateway; aspire-tests already cover AppHost).
- No Api host. Generated prefixes are Web.Server-owned; `CreateWebApiYarpAsync` stays for full graphs.
- Destination rewrite via `IProxyConfigFilter` rather than a consumer `appsettings.json`, so the http hop is SSOT with `WebHttpUrl`.

**Tests**
- `cd tests/container-apps/yarp/yarp-integration-tests && dotnet test -c Release` — 4 passed, 0 failed, 0 skipped (re-verified this session)
- `dotnet build tests/common/timewarp-testing/timewarp-testing.csproj -c Release` — 0/0
- `cd tests/common/timewarp-testing-tests && dotnet test -c Release` — 3 passed (Web+Api factory still boots with :7001; re-verified this session)

### How to validate

**Smoke**

```bash
cd tests/container-apps/yarp/yarp-integration-tests && dotnet test -c Release
```

**Expect**
- 4 passed, 0 failed, 0 skipped
- `HelloThroughStandaloneYarp_Should_ReachWebServer` — GET `/api/Hello?Name=Smoke` → 200, body contains `Hello, Smoke!`
- `HelloThroughStandaloneYarpWithForeignHost_Should_ReturnOk` — same with `Host: smoke.test` → 200, not 502
- `IdentitySessionThroughStandaloneYarp_Should_ReachWebServer` — GET `/api/identity/session` → 200, body contains `uthenticated`
- `RolesThroughStandaloneYarp_Should_ReachWebServerAndRequireAuth` — GET `/api/Roles` → 401, not 404

**Automated gate**

```bash
cd tests/container-apps/yarp/yarp-integration-tests && dotnet test -c Release
# or, from repo root (serial; this project is in the tests/ glob):
./bin/dev test
```

**Depends on**
Fixed ports free: web https 7000, web http 7001, yarp https 8443. `dev test` runs projects one at a time for that reason.

**Not in scope**
Aspire AppHost ingress (`tests/container-apps/aspire/aspire-tests`). Production ReverseProxy (standalone config is Development-only).
