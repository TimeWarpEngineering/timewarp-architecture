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

- [x] Reproduce in a failing InteractiveServer test (sign-up and sign-in)
- [x] Implement the chosen option (B recommended); record the decision
- [x] Cover every session-issuing completion (registration, authentication, add-existing/bootstrap — confirm)
- [x] Design regions updated (passkey-ceremony-client, hosted auth state provider, forwarding handler)
- [x] Gates: `dev build` 0/0, `dev test` (Playwright), `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster. The maintainer verifies on kind after
  merge and redeploy, with a clean browser profile.

## Results

**Decision: Option B, one path in both render modes** (recorded in `PasskeyCeremonyClient`'s Design
region). Every completion that issues the session is sent by the browser, and each handler then
navigates with `forceLoad: true`.

- `web-authn.ts` gains a `SendJson(method, url, bodyJson)` export: `fetch` with
  `credentials: "include"`, which returns `{ status, contentType, body }`. It holds no routes or
  contract shapes.
- `BrowserFetchHttpMessageHandler` (new, `[SideEffectService]`) is an `HttpMessageHandler` that
  sends through that export. `PasskeyCeremonyClient.CompleteInBrowserAsync` puts it under
  `HttpApiService` with `BaseAddress = NavigationManager.BaseUri`. Route, JSON body and
  problem/204 mapping stay in the one shared transport. Start calls and `GetCurrentSession` still go
  through `IWebServerApiService`, because they issue nothing and the challenge is stored server-side.
- The ceremony still runs through the mediator handler behind the contract route, so no second
  server implementation exists.
- Option A (a one-time ticket) was rejected: it needs more machinery plus replay and expiry rules
  to get the same outcome.

**Session-issuing completions (confirmed by grepping for `IssueAsync`):**
- `CompletePasskeyRegistration` (Create account)
- `CompletePasskeyAuthentication` (Sign in)
- `CompleteEntraBootstrapExisting` (Microsoft 365 "already have an account")
- `CompleteEntraBootstrapCreate` (Microsoft 365 "create"). This one also deletes the parked choice
  cookie. It now runs through the new `PasskeyCeremonyClient.CompleteEntraChoiceCreateAsync` and
  catches `JSException` like the other ceremonies.

All four handlers navigate with `forceLoad: true`. `GetSafeReturnUrl` is unchanged.
`CompleteAddExistingPasskey` does **not** issue a session (it merges into the signed-in account),
so it was left unchanged.

**Known limit (recorded in Design):** the Developer Passkeys demo uses `StayOnPage` and does not
navigate. Under a Server circuit it keeps the circuit's starting principal until the next full load.
Under WebAssembly, `NotifySessionChanged` still refreshes it. Login, Settings and the Microsoft 365
choice page all navigate, so they are not affected.

**Design regions updated:**
- `passkey-ceremony-client.cs`
- `web-authn-js-module.cs`
- `web-authn.ts`
- `browser-fetch-http-message-handler.cs` (new)
- `hosted-identity-session-authentication-state-provider-server.cs`
- `identity-session-cookie-forwarding-server.cs`
- the four sign-in-state handlers

**Tests:**
- New file `tests/container-apps/web/web-spa-playwright-tests/passkey-session-interactive-server-playwright-tests.cs`
  (forced `InteractiveServer`, prerender off, real cookie auth (`Authentication:UseMock=false`),
  virtual authenticator):
  - `CreateAccount_Should_SignInTheBrowser`
  - `SignIn_Should_SignInTheBrowser`

  Each asserts that the browser holds `.timewarp.identity.session` and that a fresh load of
  `/Settings` stays on `/Settings`. **Both fail on the old code**: create-account has only the
  antiforgery cookie, and sign-in times out. Both pass on the new code.
- `web-spa-integration-tests/features/identity/sign-in-state-tests.cs`:
  - The IJSRuntime fake is now a scripted browser (`BrowserFetchScript`).
  - The two Microsoft 365 create tests now script the browser leg and expect `forceLoad: true`.
  - New tests: passkey sign-in and create both complete in the browser and force-load; a problem
    from the browser completion is reported without navigating.
  - Result: 16/16.
- WebAssembly behaviour: the existing Ask sign-in Playwright tests (WASM Create account → /Settings)
  stay green. The new test was also run once under `InteractiveWebAssembly` and passed.

**Gates (2026-10-13, this worktree):**
- `dev build`: 0 warnings, 0 errors
- `dev test`: exit 0, every project passed; the Playwright project ran 13/13, including the 2 new
  tests
- `dev template-smoke`: SUCCEEDED. The first run caught `HttpStatusCode` missing under
  `--api false`; it is now fully qualified.
- `ganda repo audit`: passes
- `dev check-version`: source 2.0.0-beta.20 is ahead of nuget.org beta.19, so no bump is needed

### How to validate

**Smoke**
```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet build -v q
dotnet run --no-build -- --filter-class PasskeySession
cd ../web-spa-integration-tests
dotnet build -v q
dotnet run --no-build -- --filter-class SignInActions
```
Maintainer check on kind after merge and redeploy:
1. Open a clean browser profile, so WASM is not cached and the first render is Server.
2. Go to /Login and choose **Create account**.
3. Then sign out and choose **Sign in with a passkey**.

**Expect**
- Playwright: `total: 2, failed: 0`.
- SPA integration: `total: 16, failed: 0`.
- On kind, after each passkey flow:
  - the page fully reloads onto /Settings (or the return URL) signed in, with "Signed in · TimeWarp
    account · …" visible;
  - DevTools → Application → Cookies shows `.timewarp.identity.session`;
  - a manual reload stays signed in.

### Review disposition

- Rounds: 1. Effort 2, roster: general.
- Final counts: bug 0, suggestion 0, nit 1 fixed (M1, Design-region rewrap). 0 open.
- Disposition: **clean**.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Created: 2026-10-13 (cockpit, from Steve's kind report; root cause from code reading)
- 2026-10-13 implementer (ganda task work): reproduced with the Playwright test, implemented
  Option B, and all gates are green. The kind cluster was not touched.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T13:13:23Z
- Review oracle (claude-opus-5-5, 2026-10-13): 1 round, general, disposition clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-13T13:14:38Z
