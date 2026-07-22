# Task 117 — General Review (round 1)

Reviewer: general (Opus). Commit `09ea4162`. Files:
`tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`,
`source/container-apps/aspire/aspire-app-host/program.cs` (Design note only).

Verification performed: read both files + AppHost YARP config + Hello/weatherforecast contracts +
Hello handler + App.razor + constants + csproj/GlobalUsings; grepped asserted body strings; built
`aspire-tests.csproj` (**succeeded, 0 warnings / 0 errors**). Did not run the suite (docker/postgres/
aspire boot, ~minutes; would contend with a live dev run) — everything below is static + build-verified.

## Summary count

- Blocking (High): 0
- Medium: 0
- Low: 2
- Info: 2
- Clean statements: 8

No blocking issues. Recommend **approve**; the two Low items are optional polish.

---

## CRITICAL FOCUS #1 — Readiness gate cannot retry-away the guarded regression — CLEAN

id: G1 · severity: none (clean) · status: verified · file: ingress-smoke-tests.cs:40-63

Adversarial question answered three ways; the gate is sound:

1. **A 502 is an HTTP response, not an exception.** YARP terminates the client connection at the
   ingress edge and, when the *backend* TLS handshake fails (RemoteCertificateNameMismatch), returns
   **502 to the caller** — the client→ingress socket is healthy. `HttpClient.GetAsync`/`SendAsync` do
   NOT throw for 5xx (no `EnsureSuccessStatusCode` here), so a 502 returns normally. The gate's
   `catch (HttpRequestException)` only fires on connection-level failure (the warm-up EOF). It is
   structurally impossible for the guarded regression to be swallowed by that catch.

2. **The gate probes a Host that MATCHES the dev cert, so it can't 502 even under regression.**
   `WaitForIngressReachableAsync` issues `GET /` with the default Host (the ingress host, e.g.
   `localhost:<port>`). If someone regressed the web cluster back to the https hop, `GET /`'s cert
   validation target would be `localhost` — which the dev cert satisfies — so the gate still gets a
   prompt 200 and returns. The regression then surfaces **cleanly and only** in fact 2
   (`GetWebRouteThroughIngressWithForeignHostReturnsOk`): the foreign `Host: smoke.test` moves the
   cert-name target to `smoke.test`, mismatch → 502 → `Assert.Equal(OK, 502)` fails loudly. The gate
   neither hides nor delays that failure.

3. **The single `GET /` probe is the RIGHT scope.** The race the gate guards is the Aspire DCP
   proxy fronting the ingress replica (client→ingress wiring lag after YARP reports Healthy) — a
   layer shared by all three facts. Backend readiness (ingress→web, ingress→api) is covered
   separately by the three `WaitForResourceHealthyAsync` calls. So `GET /` warming the shared
   ingress-proxy layer is sufficient for fact 3 (api cluster) too; there is no per-route gap despite
   only `/` being probed. (Confirmed `GET /` and `/api/Hello` share the `web-server-http` cluster;
   `/api/weatherforecast` uses the api catch-all but the same ingress proxy.)

Conclusion: the gate cannot convert the guarded regression into a 2-minute hang, and cannot retry it
away. (The hang-then-fixture-failure mode exists only for a *different*, connection-level ingress-edge
failure — see L1, Low severity precisely because it is not the guarded regression.)

---

## L1 — Gate failure produces an opaque OperationCanceledException — LOW

severity: Low · status: open · file: ingress-smoke-tests.cs:50-62

If the ingress edge genuinely never becomes reachable (a real infra/config failure that keeps
resetting the client→ingress connection — NOT the guarded 502 regression, which exits the loop
immediately), the `while (true)` spins the full 2-minute budget and then throws
`OperationCanceledException` from `InitializeAsync` with no diagnostic: no attempt count, no last
`HttpRequestException`. All three facts then fail with a bare fixture-init cancellation that obscures
the cause.

Suggested fix (optional): capture the last caught `HttpRequestException` and, on
`OperationCanceledException`/budget exhaustion, rethrow with it as `innerException` (or a short message
like "ingress never became reachable within 2m; last: {ex.Message}"). Low because it only degrades
diagnosability of an unrelated failure mode; the happy path and the guarded regression are unaffected.

## L2 — HttpClients / HttpResponseMessages not disposed in facts — LOW

severity: Low · status: open · file: ingress-smoke-tests.cs:75-121 (and 51)

Each fact does `HttpClient httpClient = Fixture.App.CreateHttpClient(...)` and
`HttpResponseMessage response = await ...` without `using`; `WaitForIngressReachableAsync` likewise
creates an undisposed `HttpClient` (though it does `using` its response). Negligible under a single
boot, and consistent with the existing `IntegrationTest1.cs`, but it is inconsistent with the gate's
own `using HttpResponseMessage` two lines away. Suggested fix (optional): `using` on the clients and
responses, or hoist one client onto the fixture. Not a leak that matters here — flagged only for
consistency.

## INFO-1 — Parallel AppHosts (IntegrationTest1 + fixture) — acknowledged residual

severity: Info · status: acknowledged · file: aspire-tests assembly

`IntegrationTest1` and `IngressSmokeTests` are different classes in the same assembly, so xUnit runs
them **in parallel** by default → two concurrent `aspire_app_host` boots. Mitigations in place and
verified: both pass `--Postgres:UseDataVolume=false` (ephemeral postgres, no shared WAL), and the
testing builder sets `DcpPublisher:RandomizePorts=true` (plan verified via 13.4.6 decompilation that
`PrepareServices` nulls pinned ports on proxied endpoints — so the pinned ingress 63610/63620 don't
collide, and `CreateHttpClient` resolves the actual randomized ports). Residual risk is limited to DCP
giving unique container/network names per app instance (true in 13.4.x) — no shared deterministic name
remains after postgres was made ephemeral. Plan documents a single-collection fallback if CI flakes.
Not a blocker; watch item only.

## INFO-2 — Fixture design decision lives in inline comments, not a Design region — INFO

severity: Info · status: open · file: ingress-smoke-tests.cs:1-5

The file carries `#region Purpose` (honest, satisfies TWA0004) but the readiness-gate rationale — a
genuine design decision — is captured as inline comments rather than a `#region Design`. AGENTS.md:
"files with design decisions also carry `#region Design`." Not a violation (TWA0004 requires only
Purpose) and the inline comments are excellent; optional to promote the gate rationale into a Design
region.

---

## Clean statements (verified)

1. **Body strings exist where asserted.** `"Hello, Smoke!"` ← `hello-handler-application.cs:24`
   (`Message = $"Hello, {query.Name}!"`) with `?Name=Smoke`; the value survives JSON property-name
   casing so `Assert.Contains` is stable. `"_framework/blazor.web.js"` ← `web-server/components/App.razor:51`
   (`<script src="_framework/blazor.web.js">`), served for `GET /` as the Blazor Web App root. Both
   `StringComparison.Ordinal`.
2. **Routes + auth reachable with zero setup.** `/api/Hello` is `[ApiEndpoint][EndpointAllowAnonymous]`,
   GET, `Name` query-bound (validator NotEmpty — `Smoke` passes); YARP routes `/api/Hello` →
   `web-server-http` over the **http** hop with `WithTransformUseOriginalHostHeader(true)`.
   `/api/weatherforecast` is `[EndpointAllowAnonymous]`, GET → api catch-all `/api/{**catch-all}` over
   the default https hop (fact 3 deliberately exercises that hop). No auth/allowlist dependency in any
   fact.
3. **Resource + endpoint names match.** `constants.cs`: `YarpResourceName = "ingress"`,
   `WebServiceName = "web-server"`, `ApiServiceName = "api-server"` — all three `WaitForResourceHealthy`
   targets and every `CreateHttpClient("ingress","http")` resolve. AddYarp provides a default named
   `http` endpoint independent of the `Ingress:HttpPort` config pin, so `("ingress","http")` resolves
   even when that config is absent under the testing environment.
4. **Fixture lifetime correct.** Single boot per class via `IClassFixture<IngressAppFixture>` +
   `IAsyncLifetime`; `DisposeAsync` null-guards then `await App.DisposeAsync()`; the `using var cts`
   is scoped to `InitializeAsync` and its token is fully consumed before the method returns (gate is
   awaited), so no use-after-dispose.
5. **xUnit signatures match the referenced package.** csproj references the `xunit` meta-package (v2);
   `IAsyncLifetime.InitializeAsync/DisposeAsync` return `Task` (v2 shape) — matches the code. `[Fact]`,
   `IClassFixture`, `Assert.Equal/Contains` consistent with sibling `IntegrationTest1.cs`.
6. **Compiles clean.** `dotnet build aspire-tests.csproj` → Build succeeded, 0 Warning(s), 0 Error(s)
   (warnings-are-errors repo, so 0/0 is the bar). No stray diagnostics.
7. **Conventions.** kebab-case filename; `namespace Aspire.Tests` matches the sibling; `#region Purpose`
   present and honest; ephemeral-postgres comment mirrors `IntegrationTest1.cs`; AppHost Design region
   gains one accurate sentence (program.cs:41-43) pointing at this test as the forwarding guard, next
   to the task-104-031 original-Host rationale it protects.
8. **Guard is load-bearing (reasoned).** The commit's negative-proof (sabotage the route to https →
   502) is consistent with the topology: foreign-Host + https hop → cert-name target `smoke.test` →
   `RemoteCertificateNameMismatch` → YARP 502 → fact 2 assertion fails. I did not re-run the sabotage,
   but the mechanism is confirmed against the YARP config and the .NET cert-validation behavior the
   AppHost Design region documents.
