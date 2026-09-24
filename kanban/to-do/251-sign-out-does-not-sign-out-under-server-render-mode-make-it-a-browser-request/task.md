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

- [ ] Browser-facing sign-out endpoint reusing the session-end handler; antiforgery; 303 to /Login
- [ ] SPA sign-out = clear state + full navigation, identical across render modes
- [ ] Loopback POST / NotifySessionChanged branch removed or justified
- [ ] Tests: endpoint, Server-mode browser cookie cleared, antiforgery rejection, SPA state
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`; no AppHost started

## Notes

- Related: 212 (forward browser Host on InteractiveServer passkey loopback), 213 (do not set Host
  on HTTPS passkey loopback) — same loopback-vs-browser boundary.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-24)
