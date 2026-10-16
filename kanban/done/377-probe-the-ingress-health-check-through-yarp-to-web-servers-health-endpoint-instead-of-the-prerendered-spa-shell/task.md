# Probe the ingress health check through YARP to web-server's health endpoint instead of the prerendered SPA shell

## Goal (implementer restatement)

The ingress `WithHttpHealthCheck` probes a cheap health path that YARP routes to web-server, not
`/`. Ingress Healthy still means "the proxy is wired AND YARP routed a request to a live
web-server", but a probe no longer prerenders the Blazor app.

## Description

`source/container-apps/aspire/projects/aspire-app-host/program.cs` calls
`yarpProject.WithHttpHealthCheck(endpointName: "http")` (around line 582, run mode) and
`yarp.WithHttpHealthCheck(endpointName: "http")` (around line 671, publish path). Both probe `/`
through YARP about every 30 seconds. `/` is the catch-all SPA shell route, so web-server
prerenders the full Blazor app for every probe and creates a new TimeWarp.State prerender store
each time. In the State DevTools inspector (wired by task 335-001) the Store list floods with
dozens of `pre <guid>` stores that are not user page loads. Evidence: web-server traces `GET /`
exactly 30 seconds apart on the ingress http port, no user-agent, a new store id each time.
The publish path makes the same call, so deployed environments may also pay a full prerender per
probe (see the first note under Where to look: confirm whether publish output executes it).

**Decision (Steve, 2026-10-16):** probe a cheap health path routed THROUGH YARP to web-server.
The comments at around lines 577 to 579 (run mode) and 666 to 675 (publish path) explain `/` was
chosen so ingress readiness means the web route answers. Preserve that intent: the probe must
still prove ingress to web-server connectivity, just without prerendering.

## Where to look

- `source/container-apps/aspire/projects/aspire-app-host/program.cs`: both `WithHttpHealthCheck`
  calls, both inside `#if web` regions. The publish-path comment says AppHost health checks never
  execute in publish mode. The task statement says deployed environments also pay for the probe.
  Reconcile by checking what the publish output emits (compose healthcheck, k8s probe, ACA probe)
  and fix whatever actually probes.
- `source/container-apps/yarp/appsettings.Development.json`: the run-mode routes (`ApiRoute`,
  `WebRoute` catch-all, `WebSwaggerRoute`, `GrpcRoute`). The publish path builds its routes in
  `WithConfiguration` in the same AppHost file (`ForwardPublicHost(...)` per
  `WebServerApiRoutePrefixes.All` prefix, then the web catch-all). A health route needs to exist in
  both, and the two must agree. Per the repo rule "when two things must agree, generate one from
  the other or add a build check", prefer a shared constant (compare `IngressPaths.GrpcPathPrefix`)
  or an aspire-tests assertion over two literals.
- `source/container-apps/aspire/projects/aspire-service-defaults/extensions.cs`
  (`MapDefaultEndpoints`, line 105): maps `/health` and `/alive` ONLY when
  `app.Environment.IsDevelopment()`. web-server calls it at
  `source/container-apps/web/projects/web-server/program.cs` line 121. In Production those
  endpoints do not exist, so a YARP route to `/alive` 404s when published unless the exposure is
  chosen deliberately.
- `source/container-apps/yarp/program.cs` line 60 also calls `MapDefaultEndpoints`, so the YARP
  container's own `/health` and `/alive` are Development-only too.
- Tests: `tests/container-apps/aspire/aspire-tests/` (`compose-publish-tests.cs`,
  `kubernetes-publish-tests.cs`, `aca-publish-tests.cs`, `ingress-smoke-tests.cs`,
  `closed-box-app-host-guard-tests.cs`). The publish suites contain no health-path assertions
  today; check the generated output for the ingress probe and assert the new path there.
- Task 058-001 introduced the ingress health check (readiness, not liveness); read its record
  for the connection-EOF race the check closes. Task 335-001 wired the State DevTools inspector.

## Requirements

1. Choose the probe path and document it in the Design region of each touched file. A web-server
   health/alive endpoint exposed through a YARP route is the expected shape. Route precedence:
   a literal health route outranks the `{**catch-all}` web route in both run and publish
   configuration; the route targets web-server only, behind `#if web`.
2. Production exposure is deliberate and safe. Either keep health mapping Development-only and
   make the probe path exist in every environment some other deliberate way, or map a minimal
   anonymous liveness endpoint in all environments. Never expose detailed `/health` output
   publicly: no check names, descriptions, exception text, or dependency state in the response
   body. Record the choice and the security reasoning in the Design region of
   `extensions.cs` (or wherever the mapping lands), replacing the current "see aka.ms" caveat.
3. Change both the run-mode and the publish-mode probe. Update the comments at both sites; the
   existing text says `/` is the SPA shell and that "/health" is Development-only, both of which
   change.
4. Keep `#if web`, `#if yarp`, and the other feature-flag regions intact (TWA0008 / TWA0010). With
   web excluded the probe and route must not be emitted.
5. Update any publish-output tests (compose, Kubernetes, ACA suites) for the new probe path. If
   none can assert the probe today, add one assertion that fails if the ingress probe path
   returns to `/`.
6. Aspire-tests ingress facts still pass: ingress reaches Healthy through the new path, and the
   `ingress-smoke-tests.cs` requests do not race the startup window (the reason 058-001 exists).
7. Template output changes (AppHost, service defaults, YARP config). Run `dev check-version` and
   bump `<Version>` and the platform package pins in the same commit if it says so (`tw-pr`).
   Run `dev template-smoke` since template output changes.

## Dependencies

- Serial with task 376 in a work-set stack (same repo, one stack, no parallel workers).

## Checklist

- [x] Confirm what the publish path emits for the ingress health check and who executes it
- [x] Choose the probe path and production exposure; record both in Design regions
- [x] Add the health route to the run-mode YARP config and the publish-mode `WithConfiguration`
- [x] Change both `WithHttpHealthCheck` calls and update both comments
- [x] Update publish-output tests for the new probe path
- [x] Add an aspire-test assertion on the ingress health-check URL path
- [x] `dev check-version` (bump version and pins if required)
- [x] `dev build` 0 warnings 0 errors
- [x] `dev test` passes
- [x] `dev template-smoke` passes
- [x] `ganda repo audit` clean

## Proof gates

- `dev build`: 0 warnings, 0 errors.
- `dev test` passes, including the aspire-tests ingress facts: ingress reaches Healthy via the
  new path under the AppHost test harness.
- No periodic `GET /` from the health probe. Verify with an aspire test that asserts the
  ingress health-check URL path is not `/` (and, for publish suites, that the generated probe
  path is the new one). Do NOT verify by running `dev run` in the worker: worktree AppHosts share
  user secrets and a headless worker run can rewrite the postgres password.
- `ganda repo audit` clean.

## Session

- Created: 228576 (2026-10-16)
- Specified from Steve's approved direction (2026-10-16); read-only investigation of the AppHost,
  YARP config, and service defaults.
- 2026-10-16 implement oracle (ganda task work): implemented, all gates green; see Results.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T10:12:54Z
- 2026-10-16 review oracle (claude-opus-5-5, session not reported): tw-implementation-review effort 2, roster general, 1 round, disposition clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-16T10:15:44Z

## Review

- Round 1: Done

## Notes

- No time estimates. Scope is the two probe sites, one route in each YARP configuration, the
  health-endpoint exposure decision, and tests.
- Workers must not run the AppHost via `dev run`.

## Results

### What changed

- **Probe path:** `IngressPaths.WebServerAlivePath` = `/web-server/alive` (new constant in
  foundation-contracts). Both `WithHttpHealthCheck` calls (run mode yarp project, publish mode
  `AddYarp`) now pass it; comments at both sites rewritten.
- **Routes:** run mode `WebAliveRoute` in `source/container-apps/yarp/appsettings.Development.json`
  (literal match, `PathSet: /alive`, cluster `Web.Server`); publish mode `AddRoute(WebServerAlivePath,
  webServerHttp).WithTransformPathSet(HealthEndpointPaths.Alive)` in `WithConfiguration`. Both inside
  the existing web flag regions; the literal outranks `{**catch-all}`.
- **Production exposure:** `MapDefaultEndpoints` now maps `/alive` in every environment, anonymous,
  liveness checks only, default writer (status text only). `/health` stays Development-only. The
  Design region of `aspire-service-defaults/extensions.cs` records the reasoning in place of the
  aka.ms caveat. New `HealthEndpointPaths` constants (`Alive`, `Health`).
- **Publish output:** confirmed by decompiling Aspire.Hosting 13.6.1 that `WithHttpHealthCheck`
  only adds a `HealthCheckAnnotation` plus an AppHost-process health check, and no
  `EndpointProbeAnnotation`. Compose, Kubernetes, and ACA emit probes only from
  `EndpointProbeAnnotation`, so deployed environments never ran the `/` probe. The publish-path
  change keeps the model consistent.
- **Tests:**
  - `aspire-tests/ingress-health-check-model-tests.cs` checks the ingress health-check key
    (`ingress_http_/web-server/alive_200_check`) and that the path is never `/`. It covers run mode
    and the compose, kubernetes, and aca publish models. It tolerates the yarp and web flags being
    off.
  - `ingress-smoke-tests.cs` adds `WebServerAliveThroughIngress_Should_ReturnHealthy`, the
    closed-box check that the route reaches web-server.
  - `yarp-integration-tests/web-alive-route-ingress-path-tests.cs` checks that the JSON route
    matches the constants.

### Gates

- `dev check-version`: source 2.0.0-beta.20 is already ahead of nuget beta.19, so no bump was
  needed.
- `dev build`: 0 warnings, 0 errors.
- `dev test`: 23 of 23 projects passed. aspire-tests: 62 of 62, including the new facts. The
  ingress reached Healthy through the new path.
- `dev template-smoke` (default variant): SUCCEEDED. I did not run `--matrix`. No new `#if` regions
  were added (edits sit inside existing web regions) and the new aspire model test is
  flag-tolerant at runtime, so the nightly matrix covers the flag-off variants.
- `ganda repo audit`: passes.

### How to validate

- **Smoke:** `cd tests/container-apps/aspire/aspire-tests && dotnet test` and
  `cd tests/container-apps/yarp/yarp-integration-tests && dotnet test`.
- **Expect:** both pass. `IngressHealthCheck_Given_` (4 facts),
  `WebServerAliveThroughIngress_Should_ReturnHealthy` (body `Healthy`), and
  `WebAliveRouteIngressPath_.DevelopmentConfig_` pass. Under `dev run`, web-server traces show
  `GET /alive` every ~30s and no `GET /`, and the State DevTools Store list stops growing with
  `pre <guid>` stores. Don't run `dev run` in a worker; this is for a human check.

### Review disposition

- **Rounds:** 1. **Effort:** 2. **Roster:** general.
- **Final counts:** bug 0, suggestion 0, nit 0. No findings were raised, so none are open, fixed, or wontfix.
- **Disposition:** clean.
- **Reviewer re-ran the gates:** `./bin/dev build` green, yarp-integration-tests 17/17, aspire-tests 62/62.
- **Goal restatement vs task:** they match. No misread found.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.
