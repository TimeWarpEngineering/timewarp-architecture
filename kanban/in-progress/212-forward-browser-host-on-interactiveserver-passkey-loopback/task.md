# Forward browser Host on InteractiveServer passkey loopback

## Description

Passkey login against the single Postgres principal is flaky on `https://arch.timewarp.work` because InteractiveServer/Auto loopback mints WebAuthn options with RP ID `localhost` instead of the browser host. The Proton Pass credential in the DB is bound to `arch.timewarp.work`. Same credential: loopback Host → 400, public Host → 200.

Postgres is not the bug. Do not reset or re-seed the DB.

## Requirements

- InteractiveServer/Auto named `WebService` HttpClient loopback must present the **inbound browser Host** (the YARP-preserved public host, port stripped) so `HttpRequestHostAccessor` / `WebAuthnRelyingPartySelection` selects the same RP ID the authenticator used.
- Reuse the existing loopback pattern: `IdentitySessionCookieForwardingHandler` already copies `Cookie` and the mock-principal header from `IHttpContextAccessor.HttpContext` (the circuit/page request). Forward `Host` the same way — do **not** start consuming spoofable `X-Forwarded-Host` from the client. Design regions on `HttpRequestHostAccessor` and `WebAuthnOptions` explicitly reject that.
- A passkey registered on `arch.timewarp.work` must authenticate on that host whether the SPA circuit is InteractiveServer (loopback to Kestrel) or InteractiveWebAssembly (browser origin).
- Log `WebAuthnAssertionResult.FailureReason` on authenticate 400 (Information is fine). Today the handler returns a generic `Authentication failed` with no reason, which made this look like a missing user.
- Tests: extend `identity-session-cookie-forwarding-tests` (or sibling) to prove Host is copied onto the outgoing loopback request and is not overwritten when already set. Add a ceremony-level test that Verify fails when RP ID is `localhost` but clientData origin / authenticator rpIdHash is `arch.timewarp.work`, and succeeds when they match.

## Checklist

- [x] Forward inbound Host on InteractiveServer/Auto WebService loopback
- [x] Keep RP ID selection allowlist-only (`localhost` + `arch.timewarp.work`); no X-Forwarded-Host
- [x] Log WebAuthn verify failure reason on authenticate 400
- [x] Tests for Host forwarding + rpId/origin mismatch
- [ ] Manual smoke: Sign in with Proton Pass on `https://arch.timewarp.work` from a cold InteractiveAuto load (Server first), then again after WASM switch — both 200

## Session

- Created: 504061 (2026-09-12)
- Cockpit: grok 01a0964a-adbd-7ad1-8c1e-db8d962e1e45 (2026-09-12)
- Implementer: grok session 01a0965f-0717-7082-aabf-de4f4356ea3c (2026-09-12)

## Notes

Observed 2026-09-12 on running Aspire (`timewarp-architecture` master AppHost). Postgres volume `aspire-app-host-bbe41f2e49-postgres-data` created 2026-09-02; not reset. One principal (Steve / Steve.Cramer@TimeWarp.Enterprises) + one Proton Pass credential created 2026-09-02, not revoked.

web-server logs (same credential handle lookup all three times):

```
22:57:04  POST https://localhost:63611/api/identity/passkey/authenticate  → 400
22:58:20  POST https://localhost:63611/api/identity/passkey/authenticate  → 400
22:58:28  POST http://arch.timewarp.work/api/identity/passkey/authenticate → 200
          AuthenticationScheme: identity-session signed in.
```

Root cause chain:

1. Default `BlazorSettings.RenderMode` is `InteractiveAuto`.
2. Server-side named HttpClient uses `ServiceUriHelper.GetServiceHttpsUri` → `https://localhost:63611`.
3. `HttpRequestHostAccessor` reads `HttpContext.Request.Host.Host` of **that** API request → `localhost`.
4. `StartPasskeyAuthentication` / `CompletePasskeyAuthentication` select RP ID from that host.
5. Browser origin is `https://arch.timewarp.work`; Proton Pass assertion is for that RP. Verify returns OriginMismatch or RpIdHashMismatch. Handler maps both to generic 400.
6. After Auto switches to WASM, HttpClient BaseAddress is the page origin; YARP `WithTransformUseOriginalHostHeader` preserves `arch.timewarp.work` → success.

Related code (do not drive-by refactor):

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs`
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs`
- `source/container-apps/web/features/identity/complete-passkey-authentication/complete-passkey-authentication-handler-application.cs`
- `source/container-apps/web/projects/web-server/program.cs` (task 205-003 comment on the cookie handler)
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs`

Not in scope: new users, DB seed, changing AllowedRpIds, forcing Login to WASM-only as the only fix (WASM-only is a band-aid; loopback Host is the hole).

## Results

InteractiveServer/Auto named `WebService` loopback now forwards the circuit request Host (port stripped via `HostString.Host`) so passkey RP-ID selection matches the YARP-preserved browser host. Authenticate 400 still returns the generic "Authentication failed" body; `WebAuthnAssertionResult.FailureReason` and selected RP ID are logged at Information.

**Files changed**

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` — copy Host; skip if already set
- `source/container-apps/web/platform/identity-host/http-request-host-accessor-server.cs` — Design: loopback Host is circuit Host, not X-Forwarded-Host
- `source/container-apps/web/features/identity/web-authn-options-application.cs` — Design: X-Forwarded-Host never consulted
- `source/container-apps/web/features/identity/complete-passkey-authentication/complete-passkey-authentication-handler-application.cs` — log verify FailureReason
- `source/container-apps/web/projects/web-server/program.cs` / hosted identity-session provider Design comments
- `source/container-apps/web/projects/web-application/web-application.csproj` + `Directory.Packages.props` — `Microsoft.Extensions.Logging.Abstractions` 10.0.11
- Tests: Host copy / no-overwrite; Verify fails when selected RP is `localhost` and origin/rpIdHash are `arch.timewarp.work`, succeeds when they match

**Decisions**

- Host comes from `HttpContext.Request.Host.Host` (same source as Cookie; HTTP/2 `:authority`-safe; port stripped). Not `X-Forwarded-Host`.
- Allowlist unchanged (`localhost` + `arch.timewarp.work` via binder append). Existing `PasskeyHostSelection` still ignores spoofed `X-Forwarded-Host`.
- Generic 400 body kept (no-enumeration). Log line includes `{FailureReason}` and `{RelyingPartyId}`.

**Tests** (implementer session, Release)

- `Copies_`: 5 passed (cookie + 3 Host cases)
- `WebAuthnAuthentication_.Verify` (filter-class Verify): 40 passed
- `PasskeyHostSelection`: 4 passed (second host + unlisted + X-Forwarded-Host ignored)

Live Proton Pass smoke on `https://arch.timewarp.work` was not run here (needs the running Aspire volume + authenticator). Checklist item left open.

### How to validate

**Automated**

```bash
cd tests/libraries/timewarp-identity-tests && dotnet test -c Release -- --filter-class Verify
# expect: all passed (includes Public_host_assertion_fails_when_selected_rp_is_localhost
#         OriginMismatch, and Public_host_assertion_succeeds_when_selected_rp_matches)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Copies_
# expect: 5 passed; Host_From_HttpContext_Is_Copied_Port_Stripped sets Host to
#         arch.timewarp.work (port stripped); Host_Already_Set_Is_Not_Overwritten keeps
#         already.set.test

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class PasskeyHostSelection
# expect: 4 passed; spoofed X-Forwarded-Host does not change rp.id
```

**Smoke**

1. Leave Postgres volume `aspire-app-host-bbe41f2e49-postgres-data` as-is (do not reset).
2. `dev run` (Aspire AppHost). Open `https://arch.timewarp.work/Login` in a cold tab (InteractiveAuto, Server first).
3. Sign in with Proton Pass against the existing Steve principal.
4. Wait for Auto to switch to WASM, sign out, sign in again with the same passkey.

**Expect**

- Both ceremonies: `POST /api/identity/passkey/authenticate` → **200** and `AuthenticationScheme: identity-session signed in`.
- On a leftover 400, web-server Information log: `Passkey authentication verification failed: {FailureReason} (rpId {RelyingPartyId})` — not a silent generic fail. FailureReason `OriginMismatch` / `RpIdHashMismatch` with `rpId localhost` means Host was not forwarded.

**Depends on:** running Aspire + YARP `WithTransformUseOriginalHostHeader`; Proton Pass credential already bound to `arch.timewarp.work`.

**Not in scope:** DB re-seed, new users, changing `AllowedRpIds`, WASM-only Login.
