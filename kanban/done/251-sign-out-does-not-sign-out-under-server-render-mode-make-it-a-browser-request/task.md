# Sign-out does not sign out under Server render mode: make it a browser request

## Description

Reported 2026-09-24: with the footer showing **Server** render mode, clicking Sign out leaves the
user signed in. Cause (from `source/container-apps/web/projects/web-spa/features/profiles/profile-state/profile-state.sign-out.cs`):

1. The action POSTs `EndBrowserSession` through `IWebServerApiService`. Under WebAssembly that is a
   browser request and the `Set-Cookie` deletion reaches the browser. Under InteractiveServer the
   component runs in the circuit, so the call is a server→server loopback request carrying the
   forwarded cookie (`identity-session-cookie-forwarding-server.cs`); the cookie deletion lands on
   that `HttpClient` response and never reaches the browser.
2. `NotifySessionChanged` runs only when the provider is `IdentitySessionAuthenticationStateProvider`
   (WASM). Under Server the provider is `HostedIdentitySessionAuthenticationStateProvider`, so the
   circuit's auth state is never re-evaluated.
3. `NavigateTo("/Login")` is a soft navigation — no new browser request re-reads the (still valid)
   cookie, and the circuit keeps the signed-in principal.

Same class as tasks 212/213: cookie-mutating identity actions cannot ride the server's loopback
`HttpClient`.

## Requirements

- **Server endpoint.** A browser-facing sign-out endpoint on web-server (e.g. `POST /identity/sign-out`,
  antiforgery-protected, same session-end logic as `EndBrowserSession.Handler`) that clears the
  identity-session cookie and redirects to `/Login` (303). Reuse the handler — no second session-end
  implementation. Decide whether `EndBrowserSession` stays for API/agent callers; record it.
- **SPA.** `SignOutActionSet` clears client state (Profile, Authorization, Credentials, AgentLinks)
  then performs a **full browser navigation** to that endpoint (form POST via a small JS helper or
  `NavigateTo(..., forceLoad: true)` to a GET-safe variant if antiforgery makes POST awkward — pick
  one, justify it in the Design region). Same path in WebAssembly, Server and Auto. Remove the
  now-unneeded loopback POST + `NotifySessionChanged` branch if the full navigation supersedes them.
- **Tests.**
  - web-server integration: POST the sign-out endpoint with a session cookie → response clears the
    cookie (expired `Set-Cookie`) and redirects to `/Login`; a protected page afterwards redirects
    to login; missing antiforgery token is rejected.
  - A test that exercises the Server-render-mode path (prerender/InteractiveServer host in the
    existing web-server integration suite, as the passkey-loopback tests 212/213 did) proving the
    browser-visible cookie is cleared — not only the loopback response.
  - SPA state test: sign-out clears the four states and issues the full-navigation call.
- Reconcile Design regions (`profile-state.sign-out.cs`, the endpoint, the cookie-forwarding handler
  if it names sign-out).
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`.
- **Do not start an AppHost** (`dev run`, `aspire run`, `dotnet run` of aspire-app-host) — task
  worktrees share the master user-secrets id. Record the manual browser check (Server + WASM modes)
  as not performed.

## Checklist

- [x] Browser-facing sign-out endpoint reusing the session-end handler; antiforgery; 303 to /Login
- [x] SPA sign-out = clear state + full navigation, identical across render modes
- [x] Loopback POST / NotifySessionChanged branch removed or justified
- [x] Tests: endpoint, Server-mode browser cookie cleared, antiforgery rejection, SPA state
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Related: 212 (forward browser Host on InteractiveServer passkey loopback), 213 (do not set Host
  on HTTPS passkey loopback) — same loopback-vs-browser boundary.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
- Implement (ganda task work, headless implementer): 2026-09-24
- Review (ganda task work review oracle, headless Claude; effort 1, general): 2026-09-24

## Results

**Server.** Two hand-written FastEndpoints under
`source/container-apps/web/features/identity/sign-out-browser-session/` (same precedent as
`ChallengeEntraEndpoint` — the response is a redirect, not mediator JSON):

- `GET /identity/sign-out/antiforgery-token` → `{ formFieldName, requestToken }` plus the
  antiforgery cookie (`IAntiforgery.GetAndStoreTokens`).
- `POST /identity/sign-out` (form) → `IAntiforgery.IsRequestValidAsync` (400 with nothing cleared
  when the token is missing or invalid) → `ISender.Send(new EndBrowserSession.Command())` (reuses
  the existing handler, so there is still only one session-end implementation) → 303 `Location: /Login`.
- Paths and the token record are in `sign-out-browser-session-contracts.cs` (`SignOutBrowserSession`),
  which the server and the SPA both read.
- Antiforgery is checked explicitly in the endpoint. Blazor's `UseAntiforgery` only acts on
  endpoints that carry antiforgery metadata, and FastEndpoints antiforgery is not enabled on this host.

**SPA.** `ProfileState.SignOutActionSet` clears Profile, Authorization, Credentials and AgentLinks,
then calls `SignOutJsModule` (on-demand `import("./js/features/sign-out.js")`). The browser fetches
the token and submits a hidden form POST, so the 303 is a full page load. The path is the same in
WebAssembly, Server and Auto. The loopback `IWebServerApiService` POST, the
`NotifySessionChanged` branch and the soft `NavigateTo` are removed; the Design region explains
why the full navigation replaces all three. If the JS step throws `JSException`, the handler does
`NavigateTo("/Login", forceLoad: true)`. `JSDisconnectedException` (a Server circuit dropped by
the unload) is ignored.

**Why the browser fetches a token, not form POST from a prerendered token or a GET sign-out.**
`AntiforgeryStateProvider` only has a token when the page was prerendered, and `Prerender` is a
setting. A GET sign-out could not be antiforgery-protected. Fetching the token from the browser
works in every mode, prerendered or not.

**Decision: `EndBrowserSession` stays.** Its handler is the one the new endpoint dispatches. The
JSON `[ApiEndpoint]` stays for non-browser cookie-jar clients (scripted clients, integration
tests). Agents use bearer tokens and have no session to end. The SPA no longer calls it. This is
recorded in the contract's Design region.

**Tests.**
- `tests/container-apps/web/web-server-integration-tests/features/identity/sign-out-browser-session-tests.cs`
  (6): the browser jar is cleared, with an expired Set-Cookie and a 303 to /Login; `/Settings`
  afterwards redirects to /Login; a missing token gets 400 and the session stays; a token replayed
  from another jar gets 400; an anonymous browser gets an idempotent 303; **Server mode**: the real
  `IdentitySessionCookieForwardingHandler` loopback POST of EndBrowserSession returns the deletion
  only on the loopback response, the browser jar is still signed in (the bug), and the browser
  form POST then clears it.
- `tests/container-apps/web/web-spa-integration-tests/features/profiles/sign-out-state-tests.cs` (2):
  the four states are cleared; `import` + `SignOut(tokenPath, signOutPath)` are called; the BFF
  client is never touched and there is no soft navigation; on `JSException` the fallback is
  forceLoad /Login.

**Gates.** `dev build` (via `dotnet run tools/dev-cli/dev.cs`) 0 warnings / 0 errors. `dev test`
all green: web-server-integration 260/261 plus 1 pre-existing skip; web-spa-integration 70/70.
`dev template-smoke` succeeded. `ganda repo audit` reports only `bin-dev` / `dev-cli-capabilities`
because the AOT `bin/dev` binary is not present in this worktree. That comes from the
environment, not this change.

**Not performed:** the manual browser check in Server and WASM render modes. No AppHost was
started, as the brief requires.

### How to validate

Smoke:

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class SignOut_
cd ../web-spa-integration-tests && dotnet test -c Release -- --filter-class SignOut_Should_
```

Expect: 6/6 and 2/2 pass. `Server_Mode_Loopback_Leaves_Browser_Signed_In_Browser_Post_Clears_It`
shows that the loopback leaves the browser jar signed in and the browser POST clears it. Manual,
in any render mode (footer Server / WebAssembly / Auto): sign in, then Profile menu → Sign out.
The browser does a full load of `/Login`, the identity-session cookie is gone in devtools, and
`/Settings` redirects to `/Login`.

### Review disposition

- **Rounds:** 1. **Effort:** 1. **Roster:** general.
- **Final counts:** bug 0, suggestion 0, nit 1. All fixed. 0 open, 0 wontfix.
- **Disposition:** clean. M1 was a mis-wrapped Design comment in `sign-out-js-module.cs`, re-wrapped in the review commit.
- **Re-verified by the reviewer:** web-server `SignOut_` 6/6 and web-spa `SignOut_Should_` 2/2.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
