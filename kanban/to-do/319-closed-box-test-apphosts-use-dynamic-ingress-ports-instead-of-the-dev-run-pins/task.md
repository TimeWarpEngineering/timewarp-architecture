# Closed-box test AppHosts use dynamic ingress ports instead of the `dev run` pins

## Problem

Three workers in a row (312, 313, 316) had
`IngressSmoke_Given_.RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` fail
locally with 401 instead of 403, and CI passed every time.

The cause:
- The closed-box tests boot the **real AppHost** through `DistributedApplicationTestingBuilder`,
  which loads `appsettings.Development.json`. That pins the YARP ingress to `Ingress:Port` 63610 /
  `Ingress:HttpPort` 63620, the **same ports as the maintainer's `dev run`**.
- When the maintainer has the app running, the test AppHost's ingress can't bind ("address already
  in use"), and the test's requests reach the maintainer's instance, which has no mock auth.
- At best this is a false failure. At worst, a test talks to a live dev instance.

Run-mode boots:
- `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs` (`SetupOnce`);
- `tests/container-apps/web/web-spa-integration-tests/infrastructure/base-test.cs` (`StartAsync`);
- `tests/container-apps/api/api-server-integration-tests/features/open-api/open-api-document-tests.cs`
  (`SetupOnce`).

(The publish-mode boots in compose/kubernetes/aca tests don't bind ports.) The in-process lane
(`TIMEWARP_TEST_PORT_BASE`) is a different mechanism and is not affected.

## Requirements

- **Test AppHosts never bind the `dev run` pins.** In the AppHost (`program.cs` ~497–498), pins
  apply only when `Ingress:Port` / `Ingress:HttpPort` parse; otherwise Aspire assigns ports.
  Closed-box boots override both to empty, for example `--Ingress:Port=` `--Ingress:HttpPort=`,
  so the ingress gets dynamic ports. If an empty override doesn't clear the value, find the
  documented way and record it.
- **One helper, no copies.** Add a shared closed-box boot helper, for example in
  `tests/common/timewarp-testing`, that returns the standard args: ephemeral postgres
  (`--Postgres:UseDataVolume=false`, today repeated in all three files), dynamic ingress, and an
  optional `extraArgs` such as `--Authentication:UseMock=true`. All three boots use it.
- **Tests resolve the ingress URL from the running app**, for example
  `App.GetEndpoint("ingress", …)` / `CreateHttpClient`. They never hard-code 63610/63620. Find
  and fix any test that does.
- **The run-mode-only behaviour on the http pin keeps working:** the in-process binding of
  `Ingress:HttpPort`, so Caddy's `X-Forwarded-Proto` reaches the ingress. It is still configured
  for `dev run`. Tests that relied on it (if any) are adjusted to the dynamic endpoint. Record
  this in the AppHost Design region next to the pins.
- **Guard:** a test fails if any closed-box `CreateAsync<Projects.aspire_app_host>` call in run
  mode bypasses the helper. A source scan like 316's guard is fine.
- **Proof:** run `IngressSmoke` locally **while something is listening on 63610 and 63620**, for
  example two throwaway `TcpListener`s started by the worker. It must pass. Never start or touch
  the maintainer's AppHost.

## Checklist

- [x] Shared closed-box boot helper (ephemeral postgres + dynamic ingress + extras); 3 call sites use it
- [x] No hard-coded 63610/63620 in tests; ingress URL from the app
- [x] Guard against bypassing the helper
- [x] Proof run with 63610/63620 occupied
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve; recurring worker collision in 312/313/316)
- 2026-10-12: implemented (headless implementer, ganda task work).

## Results

- **Helper:** `tests/common/closed-box-app-host/closed-box-app-host.cs` (`ClosedBoxAppHost.Args` /
  `CreateAsync(params extraArgs)`): `--Postgres:UseDataVolume=false`, `--Ingress:Port=`,
  `--Ingress:HttpPort=`, then extras. It is one source file linked (`Compile Include … Link`) into
  aspire-tests, api-server-integration-tests and web-spa-integration-tests. It is not a
  timewarp-testing type, because api-server-integration-tests must not load timewarp-testing (FastEndpoints
  discovery pollution, 145-005) and timewarp-testing has no Aspire.Hosting.Testing. All three run-mode
  boots use it; IngressSmoke passes `--Authentication:UseMock=true` as an extra.
- **Empty override works:** `--Ingress:Port=` gives `""`, `int.TryParse` fails, and the pin is null.
- **Second port source found and fixed:** the yarp project's launch profile `applicationUrl` also
  carries `https://localhost:63610;http://localhost:63620`. Clearing the config alone still
  left the ingress endpoints at 63610/63620, and the new model fact caught it. Run mode now overwrites the
  launch-profile ports with the pin values: https `Port = pin` (null means dynamic), and http either pinned
  in-process (`IsProxied = false`, unchanged for `dev run`) or `Port = null` (proxied, dynamic). The
  AppHost Design region records this next to the pins, including that the in-process http binding
  (Caddy `X-Forwarded-Proto`) is a `dev run` behaviour. The smoke drives forwarded headers from a
  loopback (trusted) client, so no test relied on the pinned http port.
- **Ingress URL from the app:** every closed-box ingress request already used
  `App.CreateHttpClient("ingress", "http")` / `CreateHttpClient(YarpResourceName, "http")`. The remaining
  63610/63620 literals in tests are header/string values in in-process unit tests
  (forwarded-host, Entra redirect URIs). They open no connection, so they are left unchanged.
- **Guard:** `tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs` contains:
  - a source scan that fails any test file that calls `DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>`
    directly and also calls `.StartAsync(`;
  - a fact for the `Args` shape;
  - a model fact: helper-built ingress endpoints have no 63610/63620 port;
  - a model fact: default args keep the `dev run` pins (https 63610, http 63620 un-proxied).
- **Proof:** the maintainer's `dev run` was live on 63610 (dcp) and 63620 (yarp) the whole time.
  Nothing touched it, and the worker's own listeners were unnecessary. `IngressSmoke` passed 10/10,
  including `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`. The
  maintainer's pids were unchanged afterwards.
- **Gates:**
  - `dev build`: 0 warnings, 0 errors.
  - `dev template-smoke`: SUCCEEDED.
  - `dev test`: every Aspire closed-box suite passed (aspire-tests, api-server-integration-tests,
    web-spa-integration-tests 207/207) while the dev ports were occupied.
    timewarp-testing-tests, web-jaribu-tests and web-server-integration-tests failed only on
    in-process port 7000, which another concurrent process held (the in-process lane, unaffected by
    this task). Rerun with `TIMEWARP_TEST_PORT_BASE=47000`: 3/3, 298/298, and 323/324 with 1 skipped.
  - `ganda repo audit`: clean.

### How to validate

Smoke:

```bash
cd tests/container-apps/aspire/aspire-tests
dotnet build
dotnet run --no-build -- --filter-class ClosedBoxAppHost
# with `dev run` up (or anything listening on 63610 and 63620):
ss -ltn | grep -E ':636[12]0'
dotnet run --no-build -- --filter-class IngressSmoke
```

Expect:
- `ClosedBoxAppHost_Should_`: 4/4 passed.
- `IngressSmoke_Given_`: 10/10 passed while 63610/63620 are held by another process, including
  `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` (403, not 401).
- The process holding 63610/63620 is untouched.

