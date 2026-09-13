# Round 1 — merged findings
**Date:** 2026-09-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs:52-55
- Description: `GetRequestHost()` unconditionally prefers any non-empty `X-TimeWarp-Circuit-Host` over `Request.Host.Host`. Nothing in AppHost YARP (`WithTransformUseOriginalHostHeader` only) or the standalone YARP config strips that header, so a client on the public `/api/identity/**` path can send it and move WebAuthn RP-ID selection among `AllowedRpIds` independently of the real Host. That is not the same trust model as `X-TimeWarp-Mock-Principal-Id`, which is ignored unless mock auth is active. Damage stays allowlist-bounded (and browser origin/rpId checks still constrain ceremonies), but Host-based selection on the public path is client-influenceable. Design text claiming the public YARP path “has no internal header” / “set only by” the forwarding handler is aspirational, not enforced. Loopback `CopyCircuitHost` skip-if-already-set has no current product setter (only tests), so that hop is not a separate finding.
- Suggestion: Ignore or strip `X-TimeWarp-Circuit-Host` unless the request is the trusted loopback hop (e.g. only honor when `Request.Host` is loopback/localhost, strip at ingress, or otherwise refuse client-supplied values on the public path). Add a test that a client-supplied circuit-host header does not override `Request.Host` on the non-loopback path.
- Source: general
- Disposition notes: Fixed in `HttpRequestHostAccessor`: honor `X-TimeWarp-Circuit-Host` only when `Request.Host.Host` is loopback (`localhost` case-insensitive, or an IP that `IPAddress.IsLoopback` accepts). On the public path the header is ignored even if present; `Request.Host.Host` wins. Accessor remains SSOT (direct-to-Kestrel and tests bypass YARP). Covered by unit tests (non-loopback ignore + loopback IP honor) and an e2e passkey Start registration spoof case.

## Duplicates / conflicts

- None (single reviewer).
