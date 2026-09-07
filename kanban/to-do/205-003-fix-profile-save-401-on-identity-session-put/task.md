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

- [ ] Reproduce PUT 401 with evidence (status, scheme challenge, cookie present/absent)
- [ ] Signed-in identity-session PUT succeeds and persists
- [ ] Anonymous PUT 401; empty 401/403 toasted as Unauthorized/Forbidden, not Unhandled Error
- [ ] HTTP cookie integration test + any HttpApiService mapping tests
- [ ] Results + How to validate (include live `/Profile` Save after sign-in)

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
