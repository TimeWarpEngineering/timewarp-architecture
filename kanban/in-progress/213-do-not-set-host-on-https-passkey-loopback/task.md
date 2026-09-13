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

- [x] Stop setting HTTP Host on HTTPS WebService loopback
- [x] Internal circuit-host header + accessor fallback
- [x] Rewrite 212 Host-header tests; add accessor tests
- [ ] Manual smoke: cold InteractiveAuto (Server) sign-in on `https://arch.timewarp.work` with Proton Pass — no SSL crash, 200 session

## Session

- Created: 1067753 (2026-09-13)
- Cockpit: grok 01a0964a-adbd-7ad1-8c1e-db8d962e1e45 (2026-09-13)
- Implementer: grok session 01a09895-d637-74e0-bb0f-38ace9f41bff (2026-09-13)

## Notes

Follow-up to merged PR #351 / task 212. Live repro 2026-09-13 on architecture master Aspire after that merge. Postgres / Proton Pass credential is fine; this is TLS name mismatch on loopback.

Related:

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` (`CopyCircuitHost`)
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs`
- `source/container-apps/web/projects/web-spa/services/mocks/mock-authentication-defaults.cs` (header constant home)
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs`
- `tests/container-apps/web/web-server-integration-tests/features/identity/http-request-host-accessor-tests.cs`

## Results

HTTPS InteractiveServer/Auto `WebService` loopback no longer rewrites HTTP `Host` to the public hostname. The circuit/page host is copied onto `X-TimeWarp-Circuit-Host` (port already stripped). `HttpRequestHostAccessor.GetRequestHost()` prefers that internal header when present and non-empty, else `Request.Host.Host` (YARP public path unchanged). TLS still validates `localhost` against the ASP.NET dev cert. Allowlist / `WebAuthnRelyingPartySelection` unchanged. No `UseForwardedHeaders`, no SSL-validation bypass, no HTTP loopback. The task-212 Information log `Passkey authentication verification failed: {FailureReason} (rpId {RelyingPartyId})` is unchanged.

**Files changed**

- `source/container-apps/web/projects/web-spa/services/mocks/mock-authentication-defaults.cs` — `CircuitHostHeader = "X-TimeWarp-Circuit-Host"`
- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` — `CopyCircuitHost` sets the internal header; does not set `Headers.Host`
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs` — prefer internal header, else `Request.Host.Host`
- `source/container-apps/web/platform/identity-host/i-request-host-accessor-application.cs` — Design: two-source host read
- `source/container-apps/web/features/identity/web-authn-options-application.cs` — Design: no Host rewrite on HTTPS loopback
- `source/container-apps/web/projects/web-server/program.cs` — Design: internal header, not HTTP Host
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs` — 212 Host-copy assertions rewritten
- `tests/container-apps/web/web-server-integration-tests/features/identity/http-request-host-accessor-tests.cs` — accessor present/absent/empty/X-Forwarded-Host/null
- `tests/container-apps/web/web-server-integration-tests/features/identity/passkey-host-selection-tests.cs` — Design only

**Key decisions**

- Internal header lives next to `X-TimeWarp-Mock-Principal-Id` (`MockAuthenticationDefaults.CircuitHostHeader`). Set only from `HttpContext.Request.Host.Host` of the circuit request — never from `X-Forwarded-Host`.
- If the outgoing request already has `X-TimeWarp-Circuit-Host`, it is not overwritten. `Headers.Host` is never written by this handler.

**Test outcomes**

- `IdentitySessionCookieForwarding_` — 6 passed
- `HttpRequestHostAccessor_` — 5 passed
- `PasskeyHostSelection_` — 4 passed
- `Public_host_assertion*` (timewarp-identity) — 2 passed (OriginMismatch when selected RP is localhost vs public origin; success when they match)
- `web-server` Release build — 0/0

### How to validate

**Automated**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class IdentitySessionCookieForwarding
# expect: 6 passed — circuit host on X-TimeWarp-Circuit-Host, Headers.Host unset/empty

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class HttpRequestHostAccessor
# expect: 5 passed — internal header wins; Request.Host when absent; X-Forwarded-Host ignored

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class PasskeyHostSelection
# expect: 4 passed — public Host path + X-Forwarded-Host still ignored

cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-method Public_host_assertion
# expect: 2 passed — Verify fails when selected RP is localhost vs arch.timewarp.work origin; succeeds when they match
```

**Manual smoke**

```bash
./bin/dev run
# Open a cold tab: https://arch.timewarp.work/Login (InteractiveAuto, Server first)
# Sign in with Proton Pass (existing credential bound to arch.timewarp.work)
```

**Expect:** no `AuthenticationException` / `RemoteCertificateNameMismatch` in web-server logs on the first Server-mode passkey click; `POST https://localhost:<kestrel>/api/identity/passkey/authenticate` returns 200; identity-session cookie set. A leftover 400 still logs `Passkey authentication verification failed: {FailureReason} (rpId {RelyingPartyId})` at Information — `rpId localhost` means the circuit host did not reach RP-ID selection.

**Depends on:** running Aspire AppHost from this branch; YARP `WithTransformUseOriginalHostHeader`; Proton Pass credential already bound to `arch.timewarp.work`.

**Not in scope:** live Proton Pass smoke was not run in this implementer session (needs the running Aspire volume + authenticator). Checklist item left open.
