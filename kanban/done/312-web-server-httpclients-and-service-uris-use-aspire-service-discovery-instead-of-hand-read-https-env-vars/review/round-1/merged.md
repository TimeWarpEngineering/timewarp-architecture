# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general, security

## Counts (at merge)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 4 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs:78
- Description: The self-call forwards a port-stripped `X-Forwarded-Host` and no `X-Forwarded-Proto`. `HttpAppBaseUrlAccessor.GetRequestOrigin` (mail permalinks) also reads `X-Forwarded-Host`, so it keeps the port and takes the scheme from proto. Feedback mail sent under InteractiveServer therefore gets `https://localhost/...` in run mode (port lost) and `http://<public-host>` on published targets (scheme downgrade). The Design claim that the accessor is the "only reader" is false.
- Suggestion: Forward the full public origin: `X-Forwarded-Host` = authority with port, plus `X-Forwarded-Proto`, each only when absent. Add a handler test. Correct both Design regions.
- Source: general (bug), security (suggestion). Collapsed to bug.
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/services/superhero-grpc-service-provider.cs:46
- Description: Nothing tests the gRPC channel construction: URI rebasing, `/grpc/` prefix, HTTP/2 exact policy, and the error for an unregistered client.
- Suggestion: Add a host-free Jaribu test with a capturing primary handler.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/foundation/foundation-contracts/configuration/ingress-paths.cs:9
- Description: The yarp `appsettings.Development.json` still hard-codes `/grpc`, which contradicts the "cannot drift" claim.
- Suggestion: Add a build/test check that ties the yarp route to `IngressPaths.GrpcPathPrefix`, or generate the route from the constant.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/common/timewarp-testing/applications/web-test-server-application.cs:88
- Description: The test host registers service discovery itself, so the regression test does not prove that production `Main` wires it.
- Suggestion: Optional check that the production `AddServiceDefaults` wires discovery.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs:6 (and others listed in general.md Issue 5)
- Description: Design regions still call the self-call a "loopback".
- Suggestion: Reword to "self-call".
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-server/web-server.csproj:18
- Description: `DefineConstants` overwrites instead of appending, which drops TRACE/DEBUG.
- Suggestion: `$(DefineConstants);postgres;grpc`.
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-server/program.cs:165
- Description: With no `Services:web-server` config, the pass-through resolver falls back to DNS `https://web-server` and the cookie-forwarding handler still runs. Master made no request at all in that case. TLS name validation mitigates this.
- Suggestion: Harden the handler or disable pass-through for the client. Otherwise document the behaviour in the program.cs Design note.
- Source: security
- Disposition notes:

## Duplicates / conflicts

- general Issue 1 and security Issue 1 describe the same defect. They are merged as M1 at the stronger severity (bug).
