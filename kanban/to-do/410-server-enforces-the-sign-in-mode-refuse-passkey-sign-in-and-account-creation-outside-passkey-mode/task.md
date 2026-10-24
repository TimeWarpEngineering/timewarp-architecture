# Server enforces the sign-in mode: refuse passkey sign-in and account creation outside Passkey mode

## Description

Task 379-016 added `SignInOptions.Mode` with three values: `Passkey` (the default), `Microsoft365Only`
and `Staff`. The mode only changes what the browser shows. The web-spa binds `SignInOptions`
(`configuration/sign-in-options.cs`) from two appsettings files, web-server and web-spa `wwwroot`,
and no server code reads it. So in `Microsoft365Only` the server still serves:

- passkey sign-in (`features/identity/start-passkey-authentication`, `complete-passkey-authentication`),
- passkey account creation (`start-passkey-registration`, `complete-passkey-registration`),
- the add-existing-passkey flow (`start-add-existing-passkey`, `complete-add-existing-passkey`).

Anyone who skips the login card can still call those endpoints.

`Staff` is not a sign-in mode. It is `Microsoft365Only` plus a hard-coded "Staff only." line, which
is app wording and does not belong in the template.

Goal: the template has two modes, the server owns the mode, and the server refuses what the mode
does not offer. The browser reads the mode from the server, so the two cannot drift (AGENTS.md:
when two things must agree, generate one from the other).

## Modes

| Mode | Offers |
|------|--------|
| `Passkey` (default) | Passkey sign-in and account creation, plus Microsoft 365 when the site offers it. Same as today. |
| `Microsoft365Only` | Only Continue with Microsoft 365. The server refuses every passkey endpoint above, including for existing passkeys. |

## Decisions

- Remove `Staff` entirely. `SignInMode` keeps `Passkey = 0` and `Microsoft365Only = 1`.
- Existing passkey credentials stop working in `Microsoft365Only`. The mode means Microsoft 365
  sign-in only. The rows stay in the database, and switching back to `Passkey` makes them work again.
- The server binds and validates the mode on start, from one checked-in section in the web-server
  appsettings. The default stays `Passkey`, so existing apps behave the same.
- The SPA gets the mode from the server, next to the existing Microsoft 365 offer read
  (`get-entra-sign-in-offered` / `SignInState.FetchMicrosoft365Offered`). Extend that contract, or
  add one alongside it, whichever is less code. Remove the SPA's own `SignInOptions` binding, its
  `wwwroot/appsettings.json` section, and the test that both files bind equal.
- In `Microsoft365Only`, the passkey start and complete endpoints return 403 with one stable problem
  type. The start endpoints refuse before any WebAuthn ceremony state is created. Use one shared
  check, not a copy per endpoint.
- The shell's required-passkey gate stays skipped in `Microsoft365Only`. That is now correct,
  because the server agrees passkeys are not offered.
- Agent keys and agent tokens are out of scope. They are not browser sign-in.

## Remove Staff

- `configuration/sign-in-mode.cs`: drop the `Staff` member and its summary.
- `configuration/sign-in-options.cs`: drop `StaffNote` and any Staff handling.
- `SignInChoices.razor`, `Microsoft365AccountChoice.razor`, `LoginPage.razor.cs`,
  `ChooseMicrosoft365Page.razor.cs`: drop the staff-line markup and any `Staff` branches. Checks
  like `Mode is Microsoft365Only or Staff` become `Mode == Microsoft365Only`.
- `StyleGuidePage.razor`: drop the Staff sign-in example and the "Staff only." text.
- Tests: `sign-in-choices-render-tests.cs`, `microsoft-365-account-choice-render-tests.cs`,
  `configuration/sign-in-options-tests.cs` (which moves server-side with the binding), and
  `style-guide-page-contract-tests.cs`: drop the Staff cases and the "Staff only." assertions.
- Keep the sample role named "Staff" in the role card, TimeWarpPage and style guide role tests.
  That is example data, not the sign-in mode.
- Update the Purpose and Design regions that mention Staff.
- After the change, `rg -n 'SignInMode\.Staff|StaffNote|Staff only' source tests skills` is empty.

## Checklist

- [ ] `Staff` removed from `SignInMode`, `SignInOptions`, components, pages, style guide, and tests
- [ ] Server-side `SignInOptions` bound and validated on the web-server from one checked-in section
- [ ] The SPA reads the mode from the server; SPA `SignInOptions` binding and `wwwroot` section removed
- [ ] Passkey start/complete authentication, start/complete registration, and add-existing-passkey
      return 403 with one problem type in `Microsoft365Only`, through one shared check
- [ ] Tests: server tests for each refused endpoint in `Microsoft365Only`, including an existing
      passkey; Passkey mode unchanged; SPA render tests updated for the server-read mode
- [ ] Remove the client name from the Design region of
      `features/identity/entra-sign-in-mode-application.cs` (the "... 008-004 replacements" sentence).
      It predates 379 and is not from this task's change.
- [ ] Gates: `./bin/dev build` 0/0, `./bin/dev template-smoke`, `ganda repo audit`,
      `git diff origin/master -- source tests | rg -i "crunchit|copic"` empty

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)

## Notes

- Found while checking PR #569 (the 379-012..017 stack) before merge. Steve: merge, file this follow-up.
- Steve 2026-10-24 on Staff: "sounds like UX to me not something we put in a template". Removed here.
- Do not rename the existing `EntraSignInMode` in `entra-sign-in-mode-application.cs`. It is an
  evaluation result (Challenge / SyncHit / BootstrapCreate / Link), not the sign-in mode.
