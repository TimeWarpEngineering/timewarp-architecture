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

- [ ] Shared closed-box boot helper (ephemeral postgres + dynamic ingress + extras); 3 call sites use it
- [ ] No hard-coded 63610/63620 in tests; ingress URL from the app
- [ ] Guard against bypassing the helper
- [ ] Proof run with 63610/63620 occupied
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve; recurring worker collision in 312/313/316)
