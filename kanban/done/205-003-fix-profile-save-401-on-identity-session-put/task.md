# Fix Profile Save 401 on identity-session PUT

## Description

Human demo of `/Profile` after **205-002** (PR #329, `fe508f5d`): **Save** fails.

Browser shows: `An unhandled error occurred while processing the request.`

Aspire `web-server` (`https://localhost:63611`) logged **PUT** `api/Users/Current/Profile`
**401**, empty body, twice (2026-09-07 ~09:06:13 and 09:06:55). Challenge:

- `DenyAnonymousAuthorizationRequirement`
- `PermissionRequirement` (`profile.write`)
- schemes `identity-session` and `mock-identity-session` both challenged

**GET** `api/Users/Current/Profile` is `[EndpointAllowAnonymous]` (dual-mode demo vs
store-backed). The form still loads. **PUT** `UpdateProfile` is `[EndpointAuthorize]`
`profile.write` with those two schemes. Page gate is `profile.read`.

This is **not** a catalog bug. 205-001/205-002 stay closed.

## Requirements

### Diagnose, then fix (do not lock to one hypothesis)

Prove the actual 401 with a signed-in human session. Candidates (any may be wrong):

1. **Cookie not on the PUT.** WASM `HttpClient` `BaseAddress` is the SPA origin
   (`program.cs`). Aspire ingress was `:63610`, web-server `:63611`. Task 183 notes
   prerender `HttpContext.User` vs WASM loopback that cannot forward the identity-session
   cookie (`IdentitySessionAuthenticationStateProvider` / hosted subclass).
2. **Cookie sent, schemes fail.** Cookie name/path/SameSite/Secure vs the host that
   received the PUT.
3. **Authenticated but missing `profile.write`.** Usually **403**, not 401 — still check
   `RolePermissionSeed.SelfServicePermissions` and the live principal's grants.
4. **GET AllowAnonymous masks unsigned-in chrome.** Page `[Authorize(ProfileRead)]`
   should block, but prerender/mock/claim projection can still show a form whose Save
   has no session.

Use Aspire logs + browser Network (Cookie / `Set-Cookie` on login vs Cookie on PUT vs GET
session). Handler-only tests are **not** this bug: `UpdateProfileHandler_Given_.Anonymous_Should_Return401`
injects `ICurrentPrincipalAccessor` and never hits FastEndpoints auth.

### Product: signed-in Save must persist

- A real **identity-session** cookie (passkey register + authenticate, same pattern as
  `tests/container-apps/web/web-server-integration-tests/features/profile/get-profile-session-tests.cs`)
  must **authorize PUT** `api/Users/Current/Profile` and persist `IProfileDetails`.
- Anonymous PUT stays **401** (not 200). Insufficient permission stays **403**.
- `Authentication:UseMock` is **false** in Development; local dogfood is passkey cookie.
  Do not “fix” Save by turning mock on. If mock is in play, `mock-identity-session`
  needs `X-TimeWarp-Mock-Principal-Id` — SPA mock claims without that header would 401.

### UX: 401 must not look like a crash

Empty 401 body is mapped in `HttpApiService.HandleProblemResponse` to:

- Title: `Unhandled Error`
- Detail: `An unhandled error occurred while processing the request.`

That is the string on Save. `DefaultApiHandler.HandleError` toasts `AddProblemDetails`.
`ApiHandler` still `throw`s unexpected exceptions — do not leave a path that yellow-bars.

- Empty **401/403** (and other non-JSON error bodies) must become honest `SharedProblemDetails`
  (`Unauthorized` / `Forbidden`, status 401/403), toasted — **not** “Unhandled Error”.
- GET AllowAnonymous must not hide that Save needs a session: unsigned-in Save → sign-in
  toast / redirect, not a generic crash.

### Tests

- **HTTP cookie PUT** integration test next to `get-profile-session-tests.cs` (isolated
  cookie jar; do not rely on the shared `HttpClient` jar). Expect **200** + persisted
  fields for a valid session; **401** without a cookie.
- Cover empty-body 401 → `SharedProblemDetails` Status 401 in
  `tests/foundation/foundation-contracts-tests/http-api-service-tests.cs` if that path
  changes (existing `Synthesizes_problem_when_error_body_is_not_json` is 500/text).
- Bump template-smoke web-jaribu expected count if the aggregator suite grows
  (`tools/dev-cli/services/template-smoke-harness.cs`). Aggregator `PropertyName` may be
  camelCase (205-001 CI).

### Locked (do not regress)

- Passkey / agent-key / session / token never take `IProfileStore` (104/205).
- Catalogs stay full ISO (205-002). Theme stays `system|light|dark`.
- Do not fold profile into TimeWarp.Identity (132-001).

## Checklist

- [x] Reproduce PUT 401 with evidence (status, scheme challenge, cookie present/absent)
- [x] Signed-in identity-session PUT succeeds and persists
- [x] Anonymous PUT 401; empty 401/403 toasted as Unauthorized/Forbidden, not Unhandled Error
- [x] HTTP cookie integration test + any HttpApiService mapping tests
- [x] Results + How to validate (include live `/Profile` Save after sign-in)
- [x] Implementation review disposition (`review/`)

## Notes

- Parent **205** (done). Immediate predecessors **205-001** PR #328, **205-002** PR #329.
- Cockpit: timewarp-flow Grok session `01a03d38-9611-7620-aae5-848e15dafa94` (2026-09-07).
  Do **not** implement in cockpit; this kitchen is the brief.
- Live Aspire at diagnosis: apphost pid 3506146, dashboard `https://localhost:17304`,
  ingress `https://localhost:63610`, web-server `https://localhost:63611`.
- Files to start: `update-profile-contracts.cs`, `get-profile-contracts.cs`,
  `http-api-service.cs`, `api-handler.cs` / `default-api-handler.cs`,
  `profile-state.update-profile.cs`, `ProfilePage.razor.cs`,
  `get-profile-session-tests.cs`, `identity-session-cookie-challenge-server.cs`,
  `web-spa/program.cs` HttpClient BaseAddress, hosted auth-state provider (task 183).

## Session

- Created: 3527185 (2026-09-07)
- Cockpit: Grok `01a03d38-9611-7620-aae5-848e15dafa94` — `/Profile` Save 401; dispatch implementer
- Implementer: Grok session `01a079ad-0377-7e21-899c-5e6ad32b1bab` (2026-09-07)
- Review: Grok session `01a079be-a8fe-7fb3-850d-72380e467e32` (2026-09-07); round-1 general `01a079c1-a9f7-7c80-ac8f-a2569f1bfc01`

## Results

**Diagnosis (not a catalog / `profile.write` seed bug).** Live Aspire still serving the pre-fix bits (`dcp` pid 3506266; ingress `https://localhost:63610`, web-server `https://localhost:63611`):

```bash
curl -sk -D - -o /tmp/put-body -X PUT \
  'https://localhost:63611/api/Users/Current/Profile' \
  -H 'Content-Type: application/json' \
  -d '{"alias":"Ada","language":"en-US","region":"US","theme":"system","notifications":false}'
```

- PUT (web-server and ingress): **HTTP/2 401**, `content-length: 0`, empty body. Kitchen challenge (`DenyAnonymousAuthorizationRequirement` + `PermissionRequirement(profile.write)` + both `identity-session` and `mock-identity-session` challenged) is **unauthenticated**, not missing grants (that would be 403). `RolePermissionSeed.SelfServicePermissions` already includes `profile.write`.
- GET same route, no cookie: **200** mock `alias: "alias"` because GetProfile is `[EndpointAllowAnonymous]`.

Default BlazorSettings is **InteractiveAuto** (server first). Named `WebService` HttpClient loopbacks to `ServiceUriHelper.GetServiceHttpsUri` **without** the browser `.timewarp.identity.session` cookie (task 183 already documented this hole for prerender auth state). GET still loads the form; PUT is `[EndpointAuthorize]` `profile.write` → 401. WASM after Auto-switch had the same hole: fetch credentials were not `SameOrigin`. Empty 401 was mapped in `HttpApiService` to Title `Unhandled Error` / Detail `An unhandled error occurred while processing the request.` — the Save toast.

**Fix (cookie on the PUT + honest 401/403).** Did not turn `Authentication:UseMock` on.

- Server loopback: `IdentitySessionCookieForwardingHandler` copies inbound `Cookie` and `X-TimeWarp-Mock-Principal-Id` onto the named WebService HttpClient.
- WASM: `BrowserRequestCredentialsHandler` sets fetch credentials `SameOrigin` on named HttpClients (`BaseAddress` is the SPA origin).
- Empty/non-JSON 401 → `SharedProblemDetails` Title `Unauthorized`, Detail `Authentication is required.`; 403 → `Forbidden`. 500/text stays `Unhandled Error`.
- Unsigned-in Save: UpdateProfile handler toasts then navigates to `/Login?returnUrl=/Profile`. 403 stays toast-only.

**Files**

- `source/container-apps/web/platform/identity-host/identity-session-cookie-forwarding-server.cs` (new)
- `source/container-apps/web/projects/web-spa/services/browser-request-credentials-handler.cs` (new)
- `source/container-apps/web/projects/web-server/program.cs`
- `source/container-apps/web/projects/web-spa/program.cs`
- `source/container-apps/web/projects/web-spa/features/profiles/profile-state/profile-state.update-profile.cs`
- `source/foundation/foundation-contracts/services/http-api-service.cs`
- `source/container-apps/web/projects/web-server/hosted-identity-session-authentication-state-provider-server.cs` (Design)
- `tests/container-apps/web/web-server-integration-tests/features/profile/update-profile-session-tests.cs` (new)
- `tests/container-apps/web/web-server-integration-tests/features/identity/identity-session-cookie-forwarding-tests.cs` (new)
- `tests/foundation/foundation-contracts-tests/http-api-service-tests.cs`

No template-smoke web-jaribu bump (aggregator suite did not grow). Catalogs / theme / identity kernel untouched.

**Tests**

- `dev build` (via `dotnet run tools/dev-cli/dev.cs -- build`): **0/0**
- HttpApiService: 13 passed (incl. empty 401/403)
- Cookie forwarding handler: 2 passed
- UpdateProfile session HTTP: 2 passed (200 persist + 401 anonymous empty body)
- GetProfile session: 2 passed
- Co-located `update-profile-tests.cs`: 15 passed

Live `/Profile` Save was **not** re-run in the browser on this branch: the diagnosis Aspire process is still the pre-fix bits; this implementer did not recycle it.

### How to validate

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded, 0 Warning(s), 0 Error(s)

cd tests/foundation/foundation-contracts-tests && dotnet test -c Release -- --filter-class HttpApiService
# expect: 13 passed; Synthesizes_unauthorized_when_401_body_is_empty Title=Unauthorized Status=401

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class UpdateProfileSession
# expect: 2 passed — Persists_Given_Authorized_Session HTTP 200 + GET alias "Ada Lovelace";
#          Unauthorized_Given_No_Session HTTP 401 empty body

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class Copies_
# expect: 2 passed — Cookie and mock-principal header copied from HttpContext
```

Restart Aspire on this branch (`dotnet run tools/dev-cli/dev.cs -- run`, or stop the diagnosis apphost and start again), then:

1. Sign in with passkey (`/Login`). Confirm `.timewarp.identity.session` Set-Cookie on the login response.
2. Open `/Profile`, change display name, Save.
3. Expect HTTP **200** PUT `api/Users/Current/Profile` with the session Cookie; form shows the saved alias; no “Unhandled Error” toast.
4. Unsigned-in PUT (or Save if the form is reachable): toast **Authentication is required.** and redirect to `/Login?returnUrl=/Profile` — not “An unhandled error occurred while processing the request.”

**Expect**

- Signed-in identity-session PUT: 200 and persisted `IProfileDetails`.
- Anonymous PUT: 401 empty body at FastEndpoints; SPA maps that to Unauthorized, not Unhandled Error.
- Insufficient permission: 403 Forbidden (not Login redirect).
- `Authentication:UseMock` stays false in Development.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class UpdateProfileSession
cd tests/foundation/foundation-contracts-tests && dotnet test -c Release -- --filter-class HttpApiService
```

**Depends on:** passkey cookie (not mock). Live `/Profile` Save needs Aspire restarted onto this branch.

**Not in scope:** live WebAuthn hardware in this implementer session; recycling the diagnosis Aspire process.

### Review

| Field | Value |
|-------|--------|
| Effort / roster | 1 · general only |
| Rounds | 1 |
| Final counts | open 0 · fixed 0 · wontfix 0 (all severities) |
| Disposition | **clean** |
| Paths | `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md` |

Round 1 raised no issues. Cookie forwarding on server loopback, WASM SameOrigin credentials, honest 401/403 mapping, unsigned-in Save → Login, and HTTP cookie PUT coverage hold. No exceptions.
