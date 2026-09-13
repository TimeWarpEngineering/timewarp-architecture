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
- [x] Implementation review (effort 1) — disposition clean
- [ ] Manual smoke: cold InteractiveAuto (Server) sign-in on `https://arch.timewarp.work` with Proton Pass — no SSL crash, 200 session

## Session

- Created: 1067753 (2026-09-13)
- Cockpit: grok 01a0964a-adbd-7ad1-8c1e-db8d962e1e45 (2026-09-13)
- Implementer: grok session 01a09895-d637-74e0-bb0f-38ace9f41bff (2026-09-13)
- Review oracle: grok session 01a098a1-9f6b-7d72-a42a-bb4aa75567ab (2026-09-13)

## Notes

Follow-up to merged PR #351 / task 212. Live repro 2026-09-13 on architecture master Aspire after that merge. Postgres / Proton Pass credential is fine; this is TLS name mismatch on loopback.

Related:

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` (`CopyCircuitHost`)
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs`
- `source/container-apps/web/projects/web-spa/services/mocks/mock-authentication-defaults.cs` (header constant home)
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs`
- `tests/container-apps/web/web-server-integration-tests/features/identity/http-request-host-accessor-tests.cs`

Implementation review (effort 1, general only) lives under `review/`:

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/round-2/general.md`
- `review/round-2/merged.md`
- `review/disposition.md`

## Results

HTTPS InteractiveServer/Auto `WebService` loopback no longer rewrites HTTP `Host` to the public hostname. The circuit/page host is copied onto `X-TimeWarp-Circuit-Host` (port already stripped). `HttpRequestHostAccessor.GetRequestHost()` honors that header only when `Request.Host` is loopback; otherwise `Request.Host.Host` wins (a client-supplied copy on the public YARP path is ignored). TLS still validates `localhost` against the ASP.NET dev cert. Allowlist / `WebAuthnRelyingPartySelection` unchanged. No `UseForwardedHeaders`, no SSL-validation bypass, no HTTP loopback. The task-212 Information log `Passkey authentication verification failed: {FailureReason} (rpId {RelyingPartyId})` is unchanged.

**Files changed**

- `source/container-apps/web/projects/web-spa/services/mocks/mock-authentication-defaults.cs` — `CircuitHostHeader = "X-TimeWarp-Circuit-Host"`
- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` — `CopyCircuitHost` sets the internal header; does not set `Headers.Host`
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs` — honor internal header only on loopback Host, else `Request.Host.Host`
- `source/container-apps/web/platform/identity-host/i-request-host-accessor-application.cs` — Design: two-source host read; loopback gate
- `source/container-apps/web/features/identity/web-authn-options-application.cs` — Design: no Host rewrite on HTTPS loopback; header ignored on public path
- `source/container-apps/web/projects/web-server/program.cs` — Design: internal header, not HTTP Host; loopback-only honor
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs` — 212 Host-copy assertions rewritten
- `tests/container-apps/web/web-server-integration-tests/features/identity/http-request-host-accessor-tests.cs` — loopback honor, non-loopback ignore, empty/absent/X-Forwarded-Host/null
- `tests/container-apps/web/web-server-integration-tests/features/identity/passkey-host-selection-tests.cs` — circuit-host spoof on non-loopback Host ignored

**Key decisions**

- Internal header lives next to `X-TimeWarp-Mock-Principal-Id` (`MockAuthenticationDefaults.CircuitHostHeader`). Set only from `HttpContext.Request.Host.Host` of the circuit request — never from `X-Forwarded-Host`.
- If the outgoing request already has `X-TimeWarp-Circuit-Host`, it is not overwritten. `Headers.Host` is never written by this handler.
- Review M1: `HttpRequestHostAccessor` honors `X-TimeWarp-Circuit-Host` only when `Request.Host.Host` is loopback (`localhost` or `IPAddress.IsLoopback`). A client-supplied copy on the public path is ignored.

**Test outcomes**

- `IdentitySessionCookieForwarding_` — 6 passed
- `HttpRequestHostAccessor_` — 7 passed (loopback honor, non-loopback ignore, loopback IP)
- `PasskeyHostSelection_` — 5 passed (includes spoofed circuit-host on non-loopback Host)
- `Public_host_assertion*` (timewarp-identity) — 2 passed (OriginMismatch when selected RP is localhost vs public origin; success when they match)
- `web-server` Release build — 0/0

**Review** (effort 1, general only; 2 rounds)

- Roster: general (`review/round-1/general.md`, `review/round-2/general.md`)
- Final counts: bug 0 open / 1 fixed / 0 wontfix; suggestion 0; nit 0
- Disposition: **clean** (`review/disposition.md`) — M1 fixed on this task (`cd1f5b56`); no wontfix; no escalation
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`

### How to validate

**Automated**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class IdentitySessionCookieForwarding
# expect: 6 passed — circuit host on X-TimeWarp-Circuit-Host, Headers.Host unset/empty

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class HttpRequestHostAccessor
# expect: 7 passed — header wins on loopback Host; Request.Host on public Host even if header set; X-Forwarded-Host ignored

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class PasskeyHostSelection
# expect: 5 passed — public Host path + X-Forwarded-Host ignored + circuit-host spoof ignored on non-loopback Host

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
