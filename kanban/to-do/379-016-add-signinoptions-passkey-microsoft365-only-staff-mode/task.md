# Add SignInOptions passkey Microsoft365-only staff mode

## Description

A generated app chooses how people sign in. `SignInOptions.Mode` is `Passkey`, `Microsoft365Only`, or `Staff`. The checked-in value is `Passkey`, so the default login card stays the passkey button, optional Microsoft 365, and account creation. Generated code carries no client name.

## Goal (implementer restatement)

An operator sets the sign-in surface in configuration. The login card follows that mode. The default app still signs in with a passkey.

`SignInOptions` is one section on the server configuration and on the WebAssembly host. Both checked-in sections bind equal to `new SignInOptions()`. A mode that is not one of the three values fails startup validation. When the section is missing, the code default is `Passkey`.

`Passkey` keeps today's card. The primary button is "Sign in with a passkey". "Continue with Microsoft 365" stays an outline button and appears only when the site offers it. "Create account" stays. The add-passkey prompt and the required-passkey gate stay.

`Microsoft365Only` hides the passkey button and account creation. "Continue with Microsoft 365" is the primary button when the site offers it. The lead line is "Use your Microsoft 365 work account." When the site does not offer Microsoft 365, the card says it is not available and does not fall back to a passkey. The add-passkey prompt and the required-passkey gate stay off.

`Staff` is that same Microsoft 365 card plus the line "Staff only." The choose-account page, in either non-passkey mode, no longer offers "I already have an account" (that path proves a passkey). Settings still lists credentials. Bootstrap stays the existing site-settings flag.

The style guide names the configured mode and shows the staff-only line. Render tests pin each card. Pure tests pin the validator, the predicates, and the two appsettings copies.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` has no case-insensitive client-name match.

## Checklist

- [x] `SignInOptions` and `SignInMode`, validator, and startup binding
- [x] Login card for Passkey, Microsoft365Only, and Staff
- [x] Add-passkey prompt and required gate only in Passkey mode
- [x] Choose-account page hides passkey proof outside Passkey mode
- [x] Style-guide line. Render tests. Options tests
- [x] `./bin/dev build`, `./bin/dev template-smoke`, `ganda repo audit`, no client name in the source or test diff

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: 2026-10-24 (implementer walk). Gates passed the same day.

## Results

`SignInOptions.Mode` is `Passkey` (default), `Microsoft365Only`, or `Staff`. Both checked-in sections bind equal to `new SignInOptions()`. An unknown name fails binding. A numeric value outside the enum fails `IsInEnum` at startup. The login card is `SignInChoices`. Passkey keeps "Sign in with a passkey" (`appearance="primary"`), optional outline "Continue with Microsoft 365", and "Create account". Microsoft365Only and Staff hide passkey and account creation, use a primary Microsoft 365 button, and lead with "Use your Microsoft 365 work account." When Microsoft 365 is not offered, those modes say "Microsoft 365 sign-in is not available." and do not show a passkey button. Staff adds "Staff only." The shell shows the add-passkey prompt and the required-passkey gate only when the mode offers a passkey. The choose-account page hides "I already have an account" unless the mode is Passkey, and shows "Staff only." in Staff mode. The style guide names the configured mode (`data-qa="StyleGuideSignInMode"`) and the staff line (`data-qa="StyleGuideStaffNote"`). It does not embed the login component. Settings credential management and Entra bootstrap are unchanged.

The checked-in mode stays Passkey, so a live browser pass covered that card. Microsoft365Only and Staff are pinned by HtmlRenderer, not by a second appsettings file in the browser.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class SignInOptions_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class SignInChoices_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class TimeWarpPage_Should_
cd tests/container-apps/web/web-spa-playwright-tests && dotnet run -c Release -- --filter-class PasskeySession_Given_InteractiveServer
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit
```

**Expect:**

- `SignInOptions_Should_`: 6 passed, 0 failed (69ms). The three modes validate. `(SignInMode)99` does not. `Staff` and `microsoft365only` bind. `Nope` throws `InvalidOperationException`. Both appsettings sections bind `Mode` equal to `new SignInOptions()` (`Passkey`).
- `SignInChoices_Should_`: 3 passed, 0 failed (97ms). Passkey renders `ContinueWithPasskey` as `appearance="primary"` and, when offered, `ContinueWithMicrosoft365` as `appearance="outline"`, plus `CreatePasskey`. Microsoft365Only with the offer renders the work-account lead and a primary Microsoft 365 button, and omits passkey and create-account. Without the offer it renders `Microsoft365Unavailable` and neither button. Staff renders `SignInStaffNote` ("Staff only.") and the lead, and omits passkey and create-account.
- `TimeWarpPage_Should_`: 14 passed, 0 failed (453ms). The shell test host registers `IOptions<SignInOptions>` at the code default.
- `PasskeySession_Given_InteractiveServer`: 2 passed, 0 failed (3s 571ms). The prerendered `/Login` still exposes `ContinueWithPasskey`, and clicking it signs in.
- `./bin/dev build`: Build succeeded. 0 Warning(s). 0 Error(s). Time Elapsed 00:00:05.85.
- `./bin/dev template-smoke`: Template smoke SUCCEEDED. SmokeDefault OK. Standalone 5/5, 5/5, 2/2. web-jaribu-tests 405/405. api-jaribu-tests 9/9. timewarp-testing-tests 14/14. Generated build 0 Warning(s), 0 Error(s), Time Elapsed 00:00:12.22.
- `ganda repo audit`: Passed 33, Failed 0, Skipped 1. Repository passes all audit checks.
- Client-name scan of `git diff origin/master -- source tests` prints nothing.

## Notes

- Source idea (read-only): the portal sign-in card is one Microsoft 365 work-account button and a staff-only line. This template keeps passkey as the default and does not copy that product name.
- Out of scope: the StyleGuidePage contract pass (379-017), changing Entra bootstrap policy, and removing passkey management from Settings.
