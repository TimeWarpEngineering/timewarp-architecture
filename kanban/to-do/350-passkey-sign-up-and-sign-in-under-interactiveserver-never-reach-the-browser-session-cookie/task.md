# Passkey sign-up and sign-in under InteractiveServer never reach the browser session cookie

## Symptom (Steve, 2026-10-13, kind deployment)

With an empty cache and cleared local storage, the first visit renders **InteractiveServer**
(`InteractiveAuto` before WASM is cached). Creating an account with a passkey succeeds, but the user
is **not signed in**. The same flow on the WASM side (a later visit) signs in automatically.

## Root cause (cockpit, from the code; not yet reproduced in a test)

1. The ceremony completion is an **HTTP call made by whoever runs the SPA handler**.
   `SignInState.CreateAccountWithPasskey` → `PasskeyCeremonyClient.RegisterAsync` →
   `IWebServerApiService` → `CompletePasskeyRegistration`. The server's handler issues the session
   through `CookieBrowserSessionService.IssueAsync` → `HttpContext.SignInAsync`, a **`Set-Cookie` on
   that HTTP response**.
   - **WASM:** the caller is the browser's `fetch`, so the browser stores `.timewarp.identity.session`.
     Signed in.
   - **InteractiveServer:** the caller is **web-server's own named HttpClient** (the self-call,
     `https+http://web-server`, task 312). The `Set-Cookie` lands in that server-side HttpClient
     response and is **never forwarded to the browser**. `IdentitySessionCookieForwardingHandler`
     copies the browser's *inbound* Cookie onto the self-call (205-003), but nothing carries a newly
     issued cookie back.
2. Even if the cookie did reach the browser, the **circuit's auth state is fixed for its lifetime**.
   `HostedIdentitySessionAuthenticationStateProvider` prefers the circuit request's
   `HttpContext.User`, which was anonymous when the circuit started. `NotifySessionChanged` and a
   non-forceLoad `NavigateTo` don't give the circuit a new principal.
3. The same gap almost certainly affects **passkey sign-in** (`CompletePasskeyAuthentication`) and
   **add-existing-passkey / bootstrap** completions under InteractiveServer: any completion that
   *issues* the session. Entra is unaffected because it's a full-page redirect flow. Sign-out
   already has a browser-protocol endpoint (`SignOutBrowserSessionEndpoint`, form POST + redirect).
   Confirm each.

Under `dev run` this was rarely seen because WASM is usually cached. On kind, a clean browser hits
the server render first, and 312 made server mode work there at all.

## Fix direction (decide, record in Design regions)

The session must be issued **on a browser request**, and the circuit must restart to see it.

- **Option B (recommended): complete from the browser, then force-reload.**
  - The WebAuthn JS module (`WebAuthnJsModule` / `web-authn.ts`) already runs
    `navigator.credentials.*` in the browser. Have it also POST the **complete** command to the
    contract route with `fetch(..., { credentials: 'include' })`, so `Set-Cookie` lands in the
    browser. This applies in InteractiveServer, or in both modes for one path; record which.
  - The SPA handler receives the result and then navigates with **`forceLoad: true`** to the
    destination, so a fresh request (or circuit) carries the cookie.
  - The JS call stays inside the typed `[SideEffectService]` module, invoked from the handler (TWA0026).
- **Option A (alternative): one-time session ticket.**
  - The completion returns a short-lived, single-use, server-stored ticket instead of (or as well
    as) setting the cookie on the self-call.
  - The handler `NavigateTo(redeem-url?ticket=…&returnUrl=…, forceLoad: true)`.
  - A **browser-protocol endpoint** (hand-written `EndpointWithoutRequest`, allowed per AGENTS
    "browser-protocol endpoints") redeems the ticket, sets the cookie and redirects.
  - More machinery, plus replay and expiry rules, so only pick it if B is impractical.

Either way:
- The return-URL handling stays sanitized (`LoginPage.GetSafeReturnUrl`).
- No session or ticket ever appears in a log.
- The ceremony still goes through the mediator handler (no second implementation).

## Tests

- **A regression test that fails today:** drive account creation under **InteractiveServer** and
  assert the browser ends up authenticated. That means the session cookie is present in the browser
  context, and an `[Authorize]` page loads after the flow. Playwright with WASM disabled or not yet
  cached, or a forced `RenderMode=InteractiveServer` test host.
- The same for passkey **sign-in** under InteractiveServer, plus add-existing-passkey if it issues a
  session.
- WASM behaviour unchanged (existing tests stay green).

## Checklist

- [ ] Reproduce in a failing InteractiveServer test (sign-up and sign-in)
- [ ] Implement the chosen option (B recommended); record the decision
- [ ] Cover every session-issuing completion (registration, authentication, add-existing/bootstrap — confirm)
- [ ] Design regions updated (passkey-ceremony-client, hosted auth state provider, forwarding handler)
- [ ] Gates: `dev build` 0/0, `dev test` (Playwright), `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster. The maintainer verifies on kind after
  merge and redeploy, with a clean browser profile.

## Session

- Created: 2026-10-13 (cockpit, from Steve's kind report; root cause from code reading)
