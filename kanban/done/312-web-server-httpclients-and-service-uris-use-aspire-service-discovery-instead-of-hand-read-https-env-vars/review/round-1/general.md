# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** `git diff master...HEAD` (commit 64eb82d29), 31 files. Covered: web-server named clients
(`https+http://` discovery), the `IdentitySessionCookieForwardingHandler` / `HttpRequestHostAccessor`
switch from `X-TimeWarp-Circuit-Host` to `X-Forwarded-Host`, deletion of `/service-discovery`,
`ServiceUriProvider` and `ServiceUriHelper`, `SuperheroGrpcServiceProvider` / `GrpcBaseAddressHandler`,
`IngressPaths`, AppHost route, test-host changes and the new regression test. Also checked against
task.md Requirements and Results, the Kubernetes/Compose/ACA publish output under `artifacts/aspire-output/`,
and the decompiled `Grpc.Net.Client.Web` 2.84.0 `GrpcWebHandler.SendAsyncCore`. Ran
`--filter-class WebServerNamedClientServiceDiscovery_` (1/1 passed).

## Summary
The core fix is correct. Web-server gets its own `services__web-server__http__0` in every publish target
(verified in k8s `config.yaml`, compose and ACA bicep), so `https+http://web-server` resolves, and the
regression test exercises the production registration with an http-only `Services:*` shape. I found no
leftover references to the deleted types in source, tests, skills or `.template.config`, and the `#if grpc`
regions are correct. The gRPC rebasing is sound: `GrpcWebHandler` relaxes the version policy only for an
https request URI, which is the property the http-scheme channel address relies on. One real regression:
the self-call now forwards a port-stripped host and no scheme on `X-Forwarded-Host`, and the mail base-URL
accessor reads that header as a URL authority. The other findings are coverage and drift.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs:78
- Description: `ForwardPublicHost` writes `HttpRequestHostAccessor.GetPublicHost(...)` onto the self-call's
  `X-Forwarded-Host`. That value has the port stripped (an RP ID). `HttpRequestHostAccessor` is not the only
  reader of `X-Forwarded-Host` on web-server. `HttpAppBaseUrlAccessor.GetRequestOrigin`
  (`source/container-apps/web/platform/mail/http-app-base-url-accessor-server.cs:57-70`) builds the absolute
  origin for outbound mail from the first `X-Forwarded-Host` (port **kept**) plus `X-Forwarded-Proto`, else
  `Request.Scheme`. `SubmitFeedback` uses it for the permalink in the email body, and no appsettings sets
  `Mail:PublicBaseUrl`. On the InteractiveServer self-call (SPA `FeedbackState.SubmitFeedback` →
  `WebServerApiService` → `web-server` named client):
  - Run mode: the circuit request arrives through the ingress with `X-Forwarded-Host: localhost:63610`. The
    self-call now carries `X-Forwarded-Host: localhost` and no proto. The mailed link is `https://localhost/...`,
    which has lost the port and does not resolve. Before this change it was `https://localhost:<web-server port>`,
    which did resolve.
  - Published (Compose/Kubernetes): the self-call is `http://web-server`. No `X-Forwarded-Proto` is sent, and the
    publish-mode `ASPNETCORE_FORWARDEDHEADERS_ENABLED` middleware has already consumed the ingress's proto on the
    circuit request. The mailed link is `http://<public-host>` (wrong scheme, and any non-default public port is dropped).
  
  The Design comments in this file and in `HttpRequestHostAccessor` say the accessor is the "only reader" of
  `X-Forwarded-Host`. That is not true, and it is why the self-call value can be lossy without anyone noticing.
- Suggestion: Forward the circuit request's full public origin instead of the RP-ID host. Take
  `HttpAppBaseUrlAccessor.GetRequestOrigin(httpContext.Request)`, which is the same first-value rule: forwarded
  proto, else `Request.Scheme`, and forwarded host with the port kept, else `Request.Host`. Set
  `X-Forwarded-Host = origin.Authority` and `X-Forwarded-Proto = origin.Scheme`, each only when not already
  present. `HttpRequestHostAccessor` strips the port itself, so RP-ID selection is unchanged. Add a case to
  `identity-session-cookie-forwarding-tests.cs` asserting that the port and proto survive, for example
  `X-Forwarded-Host: arch.timewarp.work:8443` in and the same value out. Correct the "only reader" sentence in
  both Design regions. Note that forwarding `X-Forwarded-Proto` changes `Request.Scheme` on the published
  self-call because the forwarded-headers middleware is on. That is the intended outcome, but record it.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/services/superhero-grpc-service-provider.cs:46
- Description: Nothing tests the new gRPC channel construction, and the implementer's "Not verified" list
  says so. Its correctness depends on several non-obvious facts:
  - The channel address must be http so that `GrpcWebHandler` keeps `RequestVersionExact` 2.0. I verified this
    against the decompiled 2.84.0 handler: it relaxes the policy only for an https request URI.
  - `GrpcBaseAddressHandler` must sit outside the factory pipeline, so that the discovery handler sees
    `https+http://grpc-server/...`.
  - Path rebasing must keep the `/grpc/` prefix on WASM.
  
  A future `Grpc.Net.Client.Web` change or a refactor that moves the rebasing handler into the factory
  pipeline would break the published h2c hop silently.
- Suggestion: Add a host-free Jaribu test. Build a `ServiceCollection` that registers the `grpc-server`
  named client with a capturing primary handler, once with `https+http://grpc-server/` and once with
  `https://host/grpc/`. Resolve `SuperheroGrpcServiceProvider` and call `GetSuperheroAsync`. Assert the
  captured `RequestUri` (`https://host/grpc/<Service>/<Method>` for the WASM shape), `Version == 2.0`, and
  `VersionPolicy == RequestVersionExact` for the server shape. Also assert the fail-loud
  `InvalidOperationException` when the client is unregistered.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/foundation/foundation-contracts/configuration/ingress-paths.cs:9
- Description: `IngressPaths` says the AppHost route and the WASM `BaseAddress` "cannot drift". The
  run-mode ingress is the yarp project, which routes from `source/container-apps/yarp/appsettings.Development.json`
  (`"Path": "/grpc/{**catch-all}"`, `"PathRemovePrefix": "/grpc"`). That is the route `dev run` actually serves,
  and it still spells the literal. The repo rule is: when two things must agree, generate one from the other or
  add a build check.
- Suggestion: Add a check that the yarp Development config's `GrpcRoute` path and transform equal
  `IngressPaths.GrpcPathPrefix`. A small test in the yarp integration tests that reads the JSON would do. An
  alternative is to register the grpc route in yarp `program.cs` code from the constant, as the generated web
  prefixes already are.
- Status: open

### Issue 4 — Severity: nit
- File: tests/common/timewarp-testing/applications/web-test-server-application.cs:88
- Description: The test host registers `AddServiceDiscovery()` and `ConfigureHttpClientDefaults(... AddServiceDiscovery())`
  itself, because `WebApplicationHost` does not run `Main`/`AddServiceDefaults`. The regression test therefore
  proves the client registration plus discovery config shape. It does not prove that production `Main` wires
  discovery. The comment says so, but removing `AddServiceDefaults` (or its discovery lines) from production
  would leave this test green.
- Suggestion: Acceptable as is. If cheap, add an assertion or a separate check that
  `aspire-service-defaults/extensions.cs` `AddServiceDefaults` calls both. Alternatively, route the test host
  through a shared `AddServiceDiscoveryDefaults` helper that production also calls.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs:6
- Description: The Design regions still call the self-call a "loopback". On published targets it is now
  `http://web-server`, which is not loopback, and this task's own Design text makes that distinction. Stale
  occurrences:
  - `identity-session-cookie-forwarding-server.cs` lines 6, 19 and 53
  - `i-request-user-agent-accessor-application.cs:12`
  - `http-request-user-agent-accessor-server.cs:9`
  - `profile-state.sign-out.cs:15`
  - `sign-out-browser-session-contracts.cs:7`
  - `end-browser-session-contracts.cs:9`
  - `hosted-identity-session-authentication-state-provider-server.cs:8,16`
- Suggestion: Reword to "self-call" (or "server→server hop") where the text means the named-client hop, so a
  reader does not infer a loopback trust rule that no longer exists.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-server/web-server.csproj:18
- Description: `<DefineConstants>postgres;grpc</DefineConstants>` overwrites, where it should append. The SDK's
  `Microsoft.NET.Sdk.CSharp.props` sets `TRACE` (and `DEBUG` for Debug builds) before the project body, so
  web-server builds without them. web-server has no `#if DEBUG` today, so nothing breaks yet. The pattern predates
  this change, but this change touches the line. Every other project uses `$(DefineConstants);...`, for example
  `web-infrastructure.csproj` and `yarp.csproj`.
- Suggestion: `<DefineConstants>$(DefineConstants);postgres;grpc</DefineConstants>`.
- Status: open
