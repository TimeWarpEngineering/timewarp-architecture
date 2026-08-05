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
- [ ] Live smoke: protected page bounce carries returnUrl; `/Login` renders signed-out
- [ ] Results with How to validate

## Notes

- CA1056 fires on the string `ReturnUrl` property; suppressed with justification —
  `[SupplyParameterFromQuery]` binds strings, and the raw value goes through `GetSafeReturnUrl`.
- Passkey-ceremony success paths (redirect after sign-in) are code-reviewed + symmetric with
  the sanitizer tests; a full WebAuthn ceremony needs a virtual authenticator and is covered
  by the existing passkey e2e lane, not re-automated here.

## Session

- Created: Claude (2026-08-05)
- Implementation: Claude (2026-08-05)
