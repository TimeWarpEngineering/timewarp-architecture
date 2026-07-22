# Round 1 — merged (single general reviewer)

APPROVE: 0 blocking / 0 medium / 2 low / 2 info. Critical focus (readiness gate) CLEAN with a
three-way proof it cannot mask the guarded regression: (1) 502 is an HTTP response, never
HttpRequestException; (2) the gate probes with the DEFAULT Host which matches the dev cert, so
it exits promptly even under regression; (3) the gate covers the shared client→ingress layer,
backends covered by the health waits. Body strings, routes, fixture lifetime, conventions all
verified clean.

| id | sev | status | disposition |
|----|-----|--------|-------------|
| L1 | low | fixed | gate exhaustion now throws TimeoutException with attempt count + last HttpRequestException as inner (orchestrator-applied; suite re-run 4/4) |
| L2 | low | accepted | fact-level HttpClient/response disposal — consistent with IntegrationTest1 precedent, negligible under single boot |
| I1 | info | noted | parallel AppHosts residual mitigated by DCP unique names; single-collection fallback documented |
| I2 | info | noted | gate rationale in inline comments (TWA0004 requires Purpose only) |
