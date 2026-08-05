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

## Session

- Created: Claude (2026-08-05, during task 153 verification)
