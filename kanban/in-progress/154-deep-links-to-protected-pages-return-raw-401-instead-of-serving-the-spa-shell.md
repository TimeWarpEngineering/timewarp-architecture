# Deep links to protected pages return raw 401 instead of serving the SPA shell

## Description

Found during task 153's live smoke (2026-08-05): a **direct browser hit** to a protected page
URL while signed out — e.g. typing `https://…/Settings` into the address bar — returns a bare
HTTP 401 from web-server (Chrome shows "This page isn't working"). The SPA shell never loads,
so the client router, `AuthorizeRouteView`, and the task-153 `RedirectToLogin` →
`/Login?returnUrl=…` flow never get a chance to run.

Client-side navigation is unaffected (in-app links bounce to `/Login?returnUrl=…` correctly).
Verified against the running Aspire instance: `curl -w "%{http_code}" http://…/` → 200 for `/`,
401 for `/Settings` signed out.

Likely cause: the server-side prerender of the protected page challenges the identity-session
cookie scheme, whose challenge behavior returns 401 for the HTML request instead of either
(a) redirecting to `/Login?returnUrl=…` for interactive/HTML requests, or (b) serving the SPA
shell and letting the client router show the login redirect. API/XHR requests must keep
getting 401 (no HTML redirects on the contract seam).

## Requirements

- A signed-out direct hit to any protected page URL ends up on
  `/Login?returnUrl=<that page>` (server-side redirect or SPA-shell fallback — decide and
  document which in the Design region of the touched host config).
- API/fetch requests keep their 401/403 semantics — content-negotiation or endpoint-class
  distinction, no blanket redirect.
- Signed-in direct hits with sufficient policy render the page as today; insufficient policy
  still yields the Forbidden experience, not a redirect loop.
- Regression coverage in the in-proc host lane (web-server-integration-tests): HTML request to
  a protected page signed out asserts redirect-to-login (or shell-serve) behavior; API request
  still 401.

## Checklist

- [ ] Locate the challenge path (identity-session cookie scheme options / prerender pipeline
      in web-server) and pick redirect-vs-shell strategy
- [ ] Implement; reconcile Design regions
- [ ] Regression tests (HTML deep link signed out; API 401 unchanged; signed-in deep link OK)
- [ ] `dev build` 0/0; suite green
- [ ] Live smoke: address-bar hit to /Settings signed out lands on /Login?returnUrl=%2FSettings
- [ ] Results with How to validate

## Notes

- Task 153 owns the client-side flow (returnUrl capture, sanitizer, redirect-when-
  authenticated) — done; this task only makes deep links reach that flow.
- Origin note for smoke: WebAuthn ceremonies require the https endpoint (RP selection accepts
  https origins only).

### Implementation plan (Phase 2, 2026-08-05)

**Root cause:** `program.cs` `ConfigureAuthentication` identity-session cookie events
unconditionally set `OnRedirectToLogin` → 401 and `OnRedirectToAccessDenied` → 403. Correct
for JSON ceremony/API; wrong for HTML deep links to `[Authorize]` Blazor pages (challenge
fires before SPA shell / client `RedirectToLogin` can run).

**Strategy: server-side HTML redirect** (not SPA-shell fallback).

| Request class | Unauthenticated challenge | Authenticated forbid |
|---|---|---|
| `/api/…` (contract seam) | **401** unchanged | **403** |
| Non-API (page deep links) | **302** → `/Login?returnUrl=<path+query>` | **403** (never Login — no task-153 loop) |

Why redirect over shell-serve: cookie events already own the challenge response; shell-serve
would require dropping page `[Authorize]` endpoint metadata (blast radius). Login query
contract matches task 153 (`returnUrl` lowercase, `GetSafeReturnUrl`).

**Classification (`ShouldRedirectToLogin`):** hard stop if path starts with `/api`; else
redirect when `Sec-Fetch-Dest: document` or Accept contains `text/html`; non-api fallback
redirect when neither signal present (bare curl smoke).

**Files:**

1. **Create** `source/container-apps/web/platform/identity-host/identity-session-cookie-challenge-server.cs`
   — pure helper: `ShouldRedirectToLogin`, `BuildLoginRedirectTarget`, Design region SSOT;
   constants `LoginPath` / `ReturnUrlQueryParameter` (or on `IdentitySessionDefaults`).
2. **Edit** `source/container-apps/web/projects/web-server/program.cs` — dual-mode
   `OnRedirectToLogin`; keep forbid 403; fix outdated comment (“login page that does not
   exist”); Design pointer to helper.
3. **Optional** constants on `identity-session-defaults-server.cs`.
4. **Create** `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs`
   — real HTTP, isolated clients, `AllowAutoRedirect = false`; cases: HTML signed-out 302
   Location `/Login?returnUrl=%2FSettings`; curl-like fallback; API anonymous 401; signed-in
   Member `/Settings` 200; Member `/Admin/Roles` 403 not Login.
5. **Optional** pure helper unit tests (co-located `-tests.cs` or suite).

**Non-goals:** task 153 client changes; removing page `[Authorize]`; HTML forbid → Forbidden
page; Entra; agent bearer challenge polish.

**Order:** helper + Design → wire events → integration tests → `dev build` 0/0 → suite green
→ live smoke → Results / How to validate.

**Validate sketch:**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release
curl -sI -H 'Accept: text/html' 'https://localhost:<web>/Settings'   # 302 + Login?returnUrl
curl -sI 'https://localhost:<web>/api/Roles'                           # 401, no Login
```

## Session

- Created: Claude (2026-08-05, during task 153 verification)
- Orchestration: Grok (2026-08-05) — Phase 1–3; plan = HTML redirect dual-mode cookie challenge
