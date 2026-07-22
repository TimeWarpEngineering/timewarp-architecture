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
