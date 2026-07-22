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
