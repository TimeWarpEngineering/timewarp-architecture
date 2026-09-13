# Round 1 — general
**Date:** 2026-09-13
**Scope reviewed:** branch task/213-do-not-set-host-on-https-passkey-loopback vs origin/master (product files)

## Summary

The change correctly stops rewriting `HttpRequestMessage.Headers.Host` on the HTTPS InteractiveServer/Auto `WebService` loopback and instead copies the circuit host (port stripped from `HttpContext.Request.Host.Host`) onto `X-TimeWarp-Circuit-Host`. `HttpRequestHostAccessor` prefers that header when non-empty, else `Request.Host.Host`; 212 Host-copy tests were rewritten; SSL validation, HTTPS loopback, allowlist selection, and the `FailureReason` log remain intact. Overall risk is low for the TLS regression itself. One trust gap remains: the accessor treats a client-supplied circuit-host header the same as the handler-set value on the public YARP path, unlike the gated mock-principal header.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs:52-55
- Description: `GetRequestHost()` unconditionally prefers any non-empty `X-TimeWarp-Circuit-Host` over `Request.Host.Host`. Nothing in AppHost YARP (`WithTransformUseOriginalHostHeader` only) or the standalone YARP config strips that header, so a client on the public `/api/identity/**` path can send it and move WebAuthn RP-ID selection among `AllowedRpIds` independently of the real Host. That is not the same trust model as `X-TimeWarp-Mock-Principal-Id`, which is ignored unless mock auth is active. Damage stays allowlist-bounded (and browser origin/rpId checks still constrain ceremonies), but Host-based selection on the public path is client-influenceable. Design text claiming the public YARP path “has no internal header” / “set only by” the forwarding handler is aspirational, not enforced. Loopback `CopyCircuitHost` skip-if-already-set has no current product setter (only tests), so that hop is not a separate finding.
- Suggestion: Ignore or strip `X-TimeWarp-Circuit-Host` unless the request is the trusted loopback hop (e.g. only honor when `Request.Host` is loopback/localhost, strip at ingress, or otherwise refuse client-supplied values on the public path). Add a test that a client-supplied circuit-host header does not override `Request.Host` on the non-loopback path.
- Status: open
