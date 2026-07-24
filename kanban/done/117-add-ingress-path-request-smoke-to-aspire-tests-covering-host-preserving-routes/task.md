# Add ingress-path request smoke to aspire-tests covering host-preserving routes

## Description

Close review gap G2i (104-031 round-1) for real: the ingress→backend hop had NO automated
coverage — health checks hit backends directly — so the RemoteCertificateNameMismatch 502
(original-Host preservation vs localhost dev cert; fixed by forwarding web routes over the http
endpoint) shipped invisible and was found live 2026-07-22 despite green dashboards and a
'manual live-chain check' that never ran. Add request-level coverage THROUGH the YARP ingress
to aspire-tests: plain request (200), request with a foreign Host header on a web route (200 —
proves the http-endpoint forwarding), and an api-route request (200, default https hop). Keep
it cheap: the AppHost already boots in aspire-tests; three requests against the ingress
endpoint.

## Checklist

- [ ] aspire-tests: resolve the ingress endpoint from the testing AppHost and issue the three
      requests (plain / foreign-Host web route / api route), asserting 200s
- [ ] Assert the web-route response actually traversed Web.Server (e.g. a known endpoint body),
      not just any 200
- [ ] Note in the AppHost Design region that this test guards the host-preservation forwarding

## Notes

Origin: 104-031 G2i + the 2026-07-22 502 incident. Lesson recorded: 'looks green' meant 'backends healthy', not 'requests flow' — this test makes the ingress path load-bearing in CI.

### Implementation plan (Phase 2, 2026-07-22)

- NEW tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs: IAsyncLifetime class
  fixture boots the AppHost ONCE (["--Postgres:UseDataVolume=false"]), waits
  WaitForResourceHealthyAsync for web-server/api-server/ingress (2-min CTS), three xUnit facts
  all via App.CreateHttpClient("ingress", "http"):
  1. GET / → 200 + body contains "_framework/blazor.web.js" (Web.Server shell proof)
  2. GET /api/Hello?Name=Smoke with request.Headers.Host="smoke.test" → 200 + "Hello, Smoke!"
     — THE regression guard (old https hop 502'd this exact request); Hello is
     EndpointAllowAnonymous, no allowlist dependency
  3. GET /api/weatherforecast?Days=10 → 200 (api-server catch-all, default https hop)
- PINNED PORTS: proven non-issue by decompilation (13.4.6): testing builder sets
  DcpPublisher:RandomizePorts=true; DcpExecutor.PrepareServices nulls pinned ports on proxied
  endpoints → no collision with live dev run; CreateHttpClient resolves actual ports.
- All requests over the ingress "http" endpoint (no test-client cert validation; internal
  api hop stays https — deliberately exercised by fact 3).
- AppHost Design region gains one sentence pointing at this test as the forwarding guard.
- Parallel AppHosts (IntegrationTest1 + fixture) safe under randomized ports + ephemeral
  postgres; single-collection fallback only if CI flakes. xUnit asserts (project is xUnit).

- Plan: 2026-07-22 (plan agent, evidence-based via decompilation)

## Results

**Delivered (commits `09ea4162`, review-fix follow-up, 2026-07-22/23):**
`ingress-smoke-tests.cs` — single-boot AppHost fixture (ephemeral postgres, randomized ports —
decompilation-proven no collision with live dev run) + three facts through the real YARP
ingress: `/` proves the Web.Server SPA shell; **foreign-Host `/api/Hello` is the regression
guard** (negative-proof verified: restoring the https hop fails it with the incident's exact
502); `/api/weatherforecast` exercises the api-server catch-all https hop. Fixture includes a
bounded readiness gate for a discovered **DCP-proxy/YARP warm-up race** (ingress reports
Healthy before accepting requests) — retries only connection-level failures; reviewer proved
three ways it cannot mask the guarded regression; exhaustion throws with attempt count + last
exception (review L1).

**Verification:** dev build 0/0; aspire-tests 4/4 (19–28s) across implementer, orchestrator,
and post-fix runs; negative proof documented. AppHost Design region points at the test.

**Review:** 1 round, effort 1 — 0 blocking/medium, L1 fixed, L2/I1/I2 accepted-noted;
disposition **clean**. G2i (104-031) is now closed by automation instead of a manual check.

## Session

- Orchestrated 2026-07-22/23: plan (decompilation-evidenced) + build (negative proof) + review.
