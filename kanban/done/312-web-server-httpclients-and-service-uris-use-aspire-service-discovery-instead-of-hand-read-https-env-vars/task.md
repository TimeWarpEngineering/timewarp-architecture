# web-server HttpClients and service URIs use Aspire service discovery instead of hand-read https env vars

## Problem (found 2026-10-12 on the kind deployment)

Loading the app through the Kubernetes ingress shows the error notification "An invalid request URI
was provided. Either the request URI must be an absolute URI or BaseAddress must be set."

**Root cause.** In web-server, `program.cs:157-159` sets the server-side named `HttpClient`s'
`BaseAddress` from `ServiceUriHelper.GetServiceHttpsUri(...)`
(`source/foundation/foundation-server/extensions/service-uri-helper.cs`). That helper hand-reads the
env var `services__{resource}__https__0`.
- Local `dev run` injects an https endpoint.
- Published targets do not: Compose, Kubernetes and ACA expose **http only**. The live kind
  web-server ConfigMap has `services__web-server__http__0` but no `__https__0`.
- So `BaseAddress` is null, and the first-visit **InteractiveServer** render, the only one before
  WASM loads, fails on every server-side call.

The publish safety suites inspect output and never run the app, so CI was green.

`curl <ingress>/service-discovery` on kind returns
`{"grpc-server":null,"api-server":null,"web-server":null}`. That is the same helper, exposed
**unauthenticated** by `program.cs:516-530` and consumed by the SPA's
`services/service-uri-provider.cs` (used by `superhero-grpc-service-provider.cs` /
`superhero-module.cs`).

## Rules

- **Don't reconstruct Aspire's conventions by hand** (070-006 / 284). The service defaults already
  call `AddServiceDiscovery()` and `ConfigureHttpClientDefaults(http => http.AddServiceDiscovery())`.
  Use the documented scheme: `BaseAddress = new Uri("https+http://web-server")` (prefers https,
  falls back to http), with `ServiceNames` constants composing the host.
- Fail closed and loud: a missing service at startup or first use is a clear error, never a silent
  null `BaseAddress`.

## Requirements

- **web-server named clients** (`ServiceNames.WebServiceName`, `ApiServiceName`): use
  `https+http://<ServiceNames…>` through service discovery.
  - Check that the `IdentitySessionCookieForwardingHandler` loopback, the
    `X-TimeWarp-Circuit-Host` rule (070-008) and TLS on the loopback hop still work. The loopback
    Design notes mention https and the dev cert, so reconcile them with the http fallback on
    published targets.
- **`/service-discovery` endpoint and the SPA `ServiceUriProvider`.** Decide and document:
  - The browser never needs internal service URIs. Every browser-facing call goes to the ingress
    origin; YARP routes `/api` and `/grpc`.
  - Preferred: the SPA's grpc-web client targets the ingress origin plus the `/grpc` prefix. Delete
    the `/service-discovery` endpoint and `ServiceUriProvider`. If the endpoint must stay, it must
    not expose internal URIs unauthenticated; record why.
- **Delete `ServiceUriHelper`** once it has no callers. It is a foundation package type, so check
  the package surface and `PublicAPI` / XML docs, and note the removal for the release notes.
- **Tests.** This regression must have been catchable.
  - Add a closed-box or in-proc test that boots web-server with **http-only** service-discovery
    configuration, the shape published targets produce, and exercises a server-side named-client
    call, for example a page or handler that uses the web-server client during interactive-server
    render.
  - It must fail on today's code with the null `BaseAddress`.
  - Keep the existing suites green, including the passkey/circuit-host tests and ingress smoke.
- **Docs:** Design regions in `web-server/program.cs`, the SPA services touched, the foundation
  helper's removal; `skills/tw-deploy` only if it says anything about https between services.

## Checklist

- [x] Named clients via `https+http://` service discovery; loopback/circuit-host behaviour verified
- [x] `/service-discovery` + `ServiceUriProvider` removed (grpc-web via ingress origin) or justified
- [x] `ServiceUriHelper` deleted (package surface noted)
- [x] Regression test with http-only discovery config (fails before, passes after)
- [x] Design regions reconciled
- [x] Gates: `dev build` 0/0, `dev test`, `dev publish compose|kubernetes|aca`, `dev template-smoke`,
      `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster `kind-timewarp-architecture` (live
  deployment). The maintainer redeploys (`dev deploy --target kubernetes`) and reloads the page
  after merge.
- Side observation, check and record only: Aspire's Kubernetes output sets
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` on web-server. 070-008's Design regions say "no
  global UseForwardedHeaders". Confirm what that env var turns on (X-Forwarded-For/-Proto, not
  Host, by default) and make the regions accurate.

## Session

- Created: 2026-10-12 (cockpit, per Steve's kind-deployment error report)
- 2026-10-12 implementer (ganda task work, headless): implemented; gates below.
- 2026-10-12 review oracle (claude-opus-5-5, headless): tw-implementation-review effort 3; reviewer + fix subagents via the Agent tool; disposition accepted-exceptions.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-12T09:26:31Z

## Results

### What changed

- **web-server named clients** (`program.cs`): `web-server`, `api-server` and a new `grpc-server`
  client use `https+http://{ServiceNames.*}/` through the service discovery that
  `AddServiceDefaults` already registers. https in run mode, http on Compose / Kubernetes / ACA.
  `web-server.csproj` adds `grpc` to `DefineConstants` (TWA0010) for the `#if grpc` client.
- **Self-call host forwarding.** On published targets the self-call goes to `http://web-server`,
  which is not loopback, so `X-TimeWarp-Circuit-Host` (honored only on a loopback Host) would have
  broken passkey RP-ID selection under InteractiveServer. The header is retired:
  `IdentitySessionCookieForwardingHandler` now sets **`X-Forwarded-Host`** on the self-call, and
  `HttpRequestHostAccessor.GetRequestHost()` is just `GetPublicHost` (X-Forwarded-Host, else
  Request.Host). No new trust: since 070-008 the accessor already reads X-Forwarded-Host from any
  peer as a selector only. HTTP Host is still never set (TLS on the https run-mode hop).
  `MockAuthenticationDefaults.CircuitHostHeader` is removed.
- **`/service-discovery` endpoint and `ServiceUriProvider` deleted.** The browser never gets
  internal URIs. `SuperheroGrpcServiceProvider` builds its gRPC-Web channel from the host's
  `grpc-server` named client. WASM: ingress origin + `IngressPaths.GrpcPathPrefix` (new constant in
  foundation-contracts, also used by the AppHost `/grpc` route). Server (prerender /
  InteractiveServer): `https+http://grpc-server/`. A private `GrpcBaseAddressHandler` outside the
  factory pipeline rebases each call, because GrpcChannel drops address paths and rejects
  non-http(s) schemes. The channel keeps HTTP/2 exact (h2c on the published http hop) since
  grpc-server endpoints are Http2-only. An unregistered client throws on first use.
- **`ServiceUriHelper` deleted.** **Release note:** `TimeWarp.Foundation.Extensions.ServiceUriHelper`
  (`GetServiceHttpUri` / `GetServiceHttpsUri`) is removed from the **TimeWarp.Foundation.Server**
  package. That was the only type in the `TimeWarp.Foundation.Extensions` namespace. Use
  `https+http://{name}` with Aspire service discovery. There is no PublicAPI file in this repo.
  `dev check-version`: source 2.0.0-beta.20 is already ahead of nuget beta.19, so no bump.
- **Test host.** `WebApplicationHost` accepts `configurationOverrides`. `WebTestServerApplication`
  registers `AddServiceDiscovery` plus the `ConfigureHttpClientDefaults` discovery handler, the same
  two calls `AddServiceDefaults` makes, and feeds `Services:*` endpoints (`HttpsServiceEndpoints`
  default, `HttpOnlyServiceEndpoints` for published shape) instead of `PostConfigure`d
  BaseAddresses. `HostGraphFactory.CreateWebAsync` passes `serviceEndpoints` through.
- **Regression test:** `web-server-integration-tests/features/service-discovery/
  web-server-named-client-service-discovery-tests.cs` boots web-server with http-only discovery
  config. It calls `/api/health` through the production `web-server` named client, including the
  forwarding handler.
- **Side observation (recorded in `HttpRequestHostAccessor` Design).** Aspire.Hosting 13.6 sets
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` on project resources with endpoints in **publish
  mode only** (decompiled `ProjectResourceBuilderExtensions`). It enables X-Forwarded-For and
  X-Forwarded-Proto with known proxies cleared, **not** Host. So Request.Scheme and
  RemoteIpAddress follow the ingress on published targets, and Request.Host stays the destination.

### Gate evidence

- `dev build`: 0 warnings, 0 errors.
- Regression test fails before: with the named client temporarily reverted to the old
  `services__web-server__https__0` read, the class failed with
  `InvalidOperationException: An invalid request URI was provided. Either the request URI must be
  an absolute URI or BaseAddress must be set.` It passes after (1/1).
- `dev test`: every project green except one aspire-tests case,
  `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` (401 vs 403). This
  is environmental. The maintainer's master `aspire run` holds ingress ports 63610/63620, so the
  test AppHost's ingress could not bind 63620 ("address already in use") and the test hit the
  maintainer's instance, which has no mock auth. A re-run with ports moved off
  (`Ingress__Port=64610 Ingress__HttpPort=64620 dotnet run -c Release -- --filter-class
  IngressSmoke`) passed: IngressSmoke 10/10. The maintainer's app was not touched.
- `dev publish compose|kubernetes|aca`: all production-safe, helm lint clean. The Kubernetes
  output has only `services__{api,grpc,web}-server__http__0`, which is the shape the regression
  test models.
- `dev template-smoke`: SUCCEEDED (defaults, SmokeNoApi, postgres-off; web-jaribu 270/270).
- `ganda repo audit`: passes.

### Not verified / follow-ups

- The SPA gRPC (Superhero page) path was not exercised end to end in a browser or Aspire test,
  for either the WASM path via `/grpc` or the server path via discovery. It compiles, and its
  composition is described in the provider's Design region.
- Unverified observation: on published targets the AddYarp ingress forwards `/grpc` to
  grpc-server's http endpoint. grpc-server is `Protocols: Http2` (h2c only on cleartext), and
  YARP's default request version policy (2.0 OrLower) downgrades cleartext to HTTP/1.1. Browser
  gRPC-Web through the ingress may therefore fail on Compose/Kubernetes, independent of this task.
  Worth its own task if the maintainer sees it after redeploy.

### Review disposition

- **Rounds:** 2. Effort 3 (by-diff). Roster: round 1 general + security; round 2 general re-review.
- **Final counts:** bug 1 fixed · suggestion 2 fixed · nit 3 fixed, 1 wontfix · **0 open**.
- **Disposition:** `accepted-exceptions`. M4 (nit, wontfix): the test host mirrors the `AddServiceDefaults` discovery calls because it cannot run `Main`.
- **Review fixes (140ab824f):** the self-call forwards the full public origin (`X-Forwarded-Host` with port, plus `X-Forwarded-Proto`), so mail permalinks stay correct. New tests: `SuperheroGrpcServiceProvider_` (3/3) and the yarp gRPC route ↔ `IngressPaths` check. "loopback" → "self-call" wording. `DefineConstants` appends (web-server; boyscout: web-spa, timewarp-testing). The pass-through DNS fallback is documented.
- **Verification:** `dev build` 0/0 · `dev template-smoke` SUCCEEDED · manual `--grpc false` app builds with 0 errors · targeted classes green.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,security,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`.

### How to validate

Smoke:
```bash
dev build
cd tests/container-apps/web/web-server-integration-tests
dotnet run -- --filter-class WebServerNamedClientServiceDiscovery_
dotnet run -- --filter-class IdentitySessionCookieForwarding_
dotnet run -- --filter-class HttpRequestHostAccessor_
dotnet run -- --filter-class PasskeyHostSelection_
cd - && dev publish kubernetes && grep -rhoE 'services__[a-z-]+__https?__0' artifacts/aspire-output/kubernetes | sort -u
# after merge (maintainer): dev deploy --target kubernetes, reload the ingress page
curl -s -o /dev/null -w '%{http_code}\n' <ingress>/service-discovery
```
Expect: build 0/0. Each filtered class passes (1/1 for the regression class). The Kubernetes
output lists only `__http__0` entries. On kind, the page loads with no "invalid request URI"
notification, passkey sign-in works on the first (InteractiveServer) visit, and
`/service-discovery` no longer returns the URI map: it falls through to the SPA, not JSON.

