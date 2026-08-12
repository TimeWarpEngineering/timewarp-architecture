# Login page redirect flow: returnUrl capture, local-only validation, redirect when authenticated

## Description

Real-app login navigation (requested 2026-08-05). Before this task, `/Login` had no redirect
flow at all: a successful passkey sign-in/registration just showed a status message and left
the user stranded on the login page; `RedirectToLogin` (the unauthorized-route bounce in
`Routes.razor`) dropped the origin URL; and an already-signed-in visitor to `/Login` saw the
sign-in page — whose "Create a passkey" button mints a NEW Principal, i.e. a second account.

Target behavior:

1. `RedirectToLogin` forwards the blocked page as `/Login?returnUrl=<escaped local path>`.
2. Successful sign-in (or registration) navigates to `returnUrl`, else home.
3. Already-authenticated visitors to `/Login` are redirected the same way immediately.
4. `returnUrl` is honored only when **local**: must start with `/`, must not be
   protocol-relative (`//…`) or backslash-tricked (`/\…`), and must not target `/Login`
   itself (loop guard). Anything else collapses to `/` — open-redirect protection.

Design decision (confirmed by Steve): the old note "signed-in users may still open /Login to
add credentials later" was wrong — credential management belongs to a Settings/Security
surface (progressive profile, task 104-024), never the login page.

## Checklist

- [x] `RedirectToLogin.razor` captures the current relative URL into `?returnUrl`
- [x] `LoginPage` reads `returnUrl` via `[SupplyParameterFromQuery]`
- [x] `GetSafeReturnUrl` local-only sanitizer (open-redirect + `/Login` loop guards)
- [x] Redirect when already authenticated on init; navigate on ceremony success
      (StatusMessage removed — navigation replaces it)
- [x] Purpose/Design regions reconciled (stale "add credentials later" note corrected)
- [x] Unit tests for the sanitizer
      (`tests/container-apps/web/web-spa-integration-tests/features/account/login-return-url-tests.cs`)
- [x] `dev build` 0/0; sanitizer tests green (4/4)
- [x] Live smoke: full round-trip verified in Chromium against the running Aspire instance
      (bounce with returnUrl → passkey registration via CDP virtual authenticator → landed on
      /Settings; signed-in visit to /Login → bounced home)
- [x] Results with How to validate

## Notes

- CA1056 fires on the string `ReturnUrl` property; suppressed with justification —
  `[SupplyParameterFromQuery]` binds strings, and the raw value goes through `GetSafeReturnUrl`.
- Passkey-ceremony success paths (redirect after sign-in) are code-reviewed + symmetric with
  the sanitizer tests; a full WebAuthn ceremony needs a virtual authenticator and is covered
  by the existing passkey e2e lane, not re-automated here.

## Results

**What changed (commit `06a5e7b8`):**
- `LoginPage.razor.cs` — `?returnUrl` bound via `[SupplyParameterFromQuery]`; already-
  authenticated visitors redirected on init; ceremony success navigates via `NavigateOnward()`;
  `GetSafeReturnUrl` sanitizer (local paths only, `/Login` loop guard, everything else → `/`);
  StatusMessage limbo state removed; Design region corrects the wrong "add credentials later"
  note (CreatePasskey mints a NEW Principal — credential management is 104-024 Settings
  territory). CA1056 suppressed with justification (query binding is string-typed).
- `LoginPage.razor` — success message bar removed (navigation replaces it).
- `RedirectToLogin.razor` — forwards the blocked page as escaped `?returnUrl`.
- NEW `tests/container-apps/web/web-spa-integration-tests/features/account/login-return-url-tests.cs`
  — 4 pure-function tests: local pass-through, missing → home, absolute/protocol-relative/
  backslash/scheme rejection, `/Login` loop rejection (exact path only).

**Gates:** `dev build` 0/0; sanitizer tests 4/4
(`dotnet test -c Release -- --filter-class GetSafeReturnUrl`).

**Live verification (Chromium via playwright-cli against the running Aspire instance,
https origin, CDP virtual WebAuthn authenticator):**
1. Signed out, client-side navigation to `/Settings` → landed on `/Login?returnUrl=%2FSettings`.
2. "Create a passkey" ceremony completed → signed in → **navigated to `/Settings`**, page
   rendered (policy satisfied).
3. Signed in, navigated to `/Login` → immediately bounced to `/` (home).

**Found during smoke (out of scope, pre-existing):** a DIRECT browser hit to a protected URL
(e.g. typing `/Settings` signed out) returns a raw HTTP 401 from web-server instead of serving
the SPA shell — the client router (and thus this redirect flow) never loads on deep links to
protected pages. Client-side navigation is unaffected. Needs its own task.

### How to validate

**Smoke (UI):**
1. `dev run`; open the web app root (https web-server URL from the Aspire dashboard).
2. Signed out, click any protected nav destination or in-app link to `/Settings`.
3. Expect: URL becomes `/Login?returnUrl=%2FSettings`, Sign in page shown.
4. Complete a passkey sign-in (or registration). Expect: you land on `/Settings`, not `/Login`.
5. While signed in, navigate in-app to `/Login`. Expect: immediate bounce to home.

**Automated gates:**
```bash
dev build   # expect 0 warnings / 0 errors
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class GetSafeReturnUrl   # expect 4/4
```

**Not in scope:** deep-link 401 on direct protected-URL hits (pre-existing, filed separately);
WebAuthn over plain http origins (RP selection accepts https only — dev smoke must use the
https endpoint).

## Session

- Created: Claude (2026-08-05)
- Implementation: Claude (2026-08-05)
- Live verification: Claude (2026-08-05), Chromium + CDP virtual authenticator
