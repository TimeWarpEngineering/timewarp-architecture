# Do not set Host on HTTPS passkey loopback

## Description

Regression from task 212. InteractiveServer/Auto named `WebService` HttpClient loopbacks to `https://localhost:<kestrel>` (Aspire service discovery). 212 set `HttpRequestMessage.Headers.Host` to the browser host (`arch.timewarp.work`) so WebAuthn RP ID selection would match the passkey.

HttpClient uses that Host for TLS SNI / certificate name validation. The loopback cert is the ASP.NET dev cert for `localhost`, not `arch.timewarp.work`. Result on first Server-mode passkey click:

```
System.Security.Authentication.AuthenticationException: The remote certificate is invalid according to the validation procedure: RemoteCertificateNameMismatch
```

at `SslStream` / `ConnectHelper.EstablishSslConnectionAsync`. Confirmed in live web-server logs after PR #351.

Keep the 212 *goal* (RP ID = browser host on Server loopback). Stop achieving it by rewriting the HTTP `Host` header on an HTTPS request whose URI host is `localhost`.

## Requirements

- **Do not** set `request.Headers.Host` to the public hostname on the HTTPS loopback client. TLS must keep validating `localhost` against the dev cert.
- Carry the circuit/page host on an **internal** header set only by `IdentitySessionCookieForwardingHandler` (same trust as `X-TimeWarp-Mock-Principal-Id` — never read a client-supplied `X-Forwarded-Host`). Suggested name: `X-TimeWarp-Circuit-Host` (constant next to the mock header).
- `HttpRequestHostAccessor.GetRequestHost()`: if that internal header is present and non-empty, use it (port already stripped at copy time); else `Request.Host.Host` (YARP public path unchanged).
- Allowlist / `WebAuthnRelyingPartySelection` unchanged. Still no `UseForwardedHeaders`.
- **Do not** disable SSL validation, **do not** switch loopback to HTTP as the fix, **do not** revert 212’s RP-ID goal.
- Tests:
  - Forwarding handler copies circuit host into the internal header and leaves `Headers.Host` unset/empty so the URI host (`localhost`) is used for TLS.
  - Accessor returns the internal-header host when present; returns `Request.Host` when absent.
  - Existing Host-copy tests from 212 that asserted `Headers.Host == arch.timewarp.work` must be rewritten — those assertions *are* the regression.
- Log line from 212 (verify `FailureReason`) stays.

## Checklist

- [ ] Stop setting HTTP Host on HTTPS WebService loopback
- [ ] Internal circuit-host header + accessor fallback
- [ ] Rewrite 212 Host-header tests; add accessor tests
- [ ] Manual smoke: cold InteractiveAuto (Server) sign-in on `https://arch.timewarp.work` with Proton Pass — no SSL crash, 200 session

## Session

- Created: 1067753 (2026-09-13)
- Cockpit: grok 01a0964a-adbd-7ad1-8c1e-db8d962e1e45 (2026-09-13)

## Notes

Follow-up to merged PR #351 / task 212. Live repro 2026-09-13 on architecture master Aspire after that merge. Postgres / Proton Pass credential is fine; this is TLS name mismatch on loopback.

Related:

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` (`CopyHost`)
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs`
- `source/container-apps/web/projects/web-spa/services/mocks/mock-authentication-defaults.cs` (header constant home)
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs`
