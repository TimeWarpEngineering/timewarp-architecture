# Add SignInOptions passkey Microsoft365-only staff mode

## Description

A generated app chooses how people sign in. `SignInOptions.Mode` is `Passkey`, `Microsoft365Only`, or `Staff`. The checked-in value is `Passkey`, so the default login card stays the passkey button, optional Microsoft 365, and account creation. Generated code carries no client name.

## Goal (implementer restatement)

An operator picks the login card with `SignInOptions.Mode`: `Passkey`, `Microsoft365Only`, or `Staff`. The server configuration and the WebAssembly host each bind a `SignInOptions` section. Both checked-in sections match `new SignInOptions()`, and that default is `Passkey`. A mode outside the three values fails startup validation. A missing section stays `Passkey`.

`Passkey` keeps the passkey card. The primary button is "Sign in with a passkey". "Continue with Microsoft 365" is an outline button and appears when the site offers it. "Create account" stays. The add-passkey prompt and the required-passkey gate stay on.

`Microsoft365Only` hides the passkey button and account creation. "Continue with Microsoft 365" is the primary button when the site offers it, under the lead "Use your Microsoft 365 work account." When a completed read says the site does not offer Microsoft 365, the card says it is not available and shows no passkey button. The add-passkey prompt and the required-passkey gate stay off.

`Staff` is that Microsoft 365 card plus the line "Staff only."

Microsoft 365 availability is unknown (`null`) until `FetchMicrosoft365Offered` finishes. The unavailable line appears only when that value is `false`, including a failed read. While it is `null`, the card shows neither the Microsoft 365 button nor the unavailable line. Settings shows the Microsoft 365 section only when the value is `true`.

On the choose-account page, "I already have an account" appears only in Passkey mode. Microsoft365Only shows neither that button nor the staff line. Staff shows "Staff only." Settings still lists credentials. Bootstrap stays the site-settings flag.

The style guide names the configured mode and shows the staff-only line. Render tests pin each login card, including an unknown offer, and pin the three choose-account bodies. Pure tests pin the validator, the predicates (including an unknown offer), and the two appsettings copies. The mode enum's design note states that copy is generic and names no product or client.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` has no case-insensitive client-name match.

## Checklist

- [x] `SignInOptions` and `SignInMode`, validator, and startup binding
- [x] Login card for Passkey, Microsoft365Only, and Staff
- [x] Add-passkey prompt and required gate only in Passkey mode
- [x] Choose-account page hides passkey proof outside Passkey mode
- [x] Style-guide line. Render tests. Options tests
- [x] `./bin/dev build`, `./bin/dev template-smoke`, `ganda repo audit`, no client name in the source or test diff
- [x] Microsoft 365 offer stays unknown until the fetch finishes; the unavailable line is false only
- [x] Choose-account render tests pin Passkey, Microsoft365Only, and Staff
- [x] Sign-in mode design note states the generic-copy rule

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: 2026-10-24 (implementer walk). Gates passed the same day.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-24T01:31:48Z
- Review round 1: review oracle (claude, claude-opus-5-5), session not reported. Effort 2, roster general — 2026-10-24
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-24T01:35:20Z
- Fix pass (implement-fix-2): implementer-grok (grok, model grok-4.7), 2026-10-24. Addressed M1, M2, and M3.
- Review round 2: review oracle (claude, claude-opus-5-5), session not reported. Effort 2, roster general, max-turns 120. Disposition clean — 2026-10-24

## Results

`SignInOptions.Mode` is `Passkey` (default), `Microsoft365Only`, or `Staff`. Both checked-in sections bind equal to `new SignInOptions()`. An unknown name fails binding. A numeric value outside the enum fails `IsInEnum` at startup. The login card is `SignInChoices`. Passkey keeps "Sign in with a passkey" (`appearance="primary"`), optional outline "Continue with Microsoft 365", and "Create account". Microsoft365Only and Staff hide passkey and account creation, use a primary Microsoft 365 button, and lead with "Use your Microsoft 365 work account." Staff adds "Staff only." The shell shows the add-passkey prompt and the required-passkey gate only when the mode offers a passkey.

`SignInState.Microsoft365Offered` is `bool?`. It stays `null` until `FetchMicrosoft365Offered` writes `true` or `false`. A failed read still writes `false`. `ShowMicrosoft365Unavailable` is true only for a work-account mode when the value is `false`. While the value is `null`, Microsoft365Only and Staff show the lead (and the staff line, for Staff) and neither the Microsoft 365 button nor "Microsoft 365 sign-in is not available." Settings shows the Microsoft 365 section only when the value is `true`.

The unlinked-account body is `Microsoft365AccountChoice`. Passkey shows "Create a new account" and "I already have an account". Microsoft365Only shows neither that second button nor `ChooseStaffNote`. Staff shows `ChooseStaffNote` ("Staff only.") and hides the passkey proof. The style guide names the configured mode (`data-qa="StyleGuideSignInMode"`) and the staff line (`data-qa="StyleGuideStaffNote"`). It does not embed the login component. Settings credential management and Entra bootstrap are unchanged. `SignInMode`'s design note states that copy is generic and names no product or client.

The checked-in mode stays Passkey. Microsoft365Only and Staff, including an unknown offer, are pinned by HtmlRenderer, not by a second appsettings file in the browser. This fix pass did not re-run the interactive passkey browser test; the Passkey primary button is unchanged.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class SignInOptions_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class SignInChoices_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class Microsoft365AccountChoice_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class SignInActions_Should_ --filter-method FetchMicrosoft365Offered
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class TimeWarpPage_Should_
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit
```

**Expect:**

- `SignInOptions_Should_`: 6 passed, 0 failed (67ms). The three modes validate. `(SignInMode)99` does not. `Staff` and `microsoft365only` bind. `Nope` throws `InvalidOperationException`. Both appsettings sections bind `Mode` equal to `new SignInOptions()` (`Passkey`). `ShowMicrosoft365Unavailable` is false for Passkey at `null` and `false`, and for Microsoft365Only at `null` and `true`. It is true for Microsoft365Only at `false`.
- `SignInChoices_Should_`: 3 passed, 0 failed (87ms). Passkey renders `ContinueWithPasskey` as `appearance="primary"` and, when offered, `ContinueWithMicrosoft365` as `appearance="outline"`, plus `CreatePasskey`. Microsoft365Only with the offer renders the work-account lead and a primary Microsoft 365 button, and omits passkey and create-account. With the offer still `null` it keeps the lead and omits `Microsoft365Unavailable`, the Microsoft 365 button, and the passkey button. With the offer `false` it renders `Microsoft365Unavailable` and neither button. Staff renders `SignInStaffNote` ("Staff only.") and the lead, and omits passkey and create-account. Staff with the offer still `null` keeps the staff line and omits the unavailable line and the Microsoft 365 button.
- `Microsoft365AccountChoice_Should_`: 3 passed, 0 failed (86ms). Passkey renders `CreateNewAccount` and `AlreadyHaveAccount`, and omits `ChooseStaffNote`. Microsoft365Only renders "Create a new account to continue." and omits `AlreadyHaveAccount` and `ChooseStaffNote`. Staff renders `ChooseStaffNote` ("Staff only.") and `CreateNewAccount`, and omits `AlreadyHaveAccount`.
- `FetchMicrosoft365Offered_Read_The_Server_Flag_And_Fail_Closed`: 1 passed, 0 failed (218ms). The offer starts `null`, becomes `true` when the server says offered, and becomes `false` when the read throws.
- `TimeWarpPage_Should_`: 14 passed, 0 failed (452ms). The shell test host registers `IOptions<SignInOptions>` at the code default.
- `./bin/dev build`: Build succeeded. 0 Warning(s). 0 Error(s). Time Elapsed 00:00:06.84.
- `./bin/dev template-smoke`: Template smoke SUCCEEDED. SmokeDefault OK. Standalone 5/5, 5/5, 2/2. web-jaribu-tests 405/405. api-jaribu-tests 9/9. timewarp-testing-tests 14/14. Generated build 0 Warning(s), 0 Error(s), Time Elapsed 00:00:12.14.
- `ganda repo audit`: Passed 33, Failed 0, Skipped 1. Repository passes all audit checks.
- Client-name scan of `git diff origin/master -- source tests` prints nothing.

### Review

Rounds: 2. Effort 2, roster general. Round 1 raised 1 bug (M1), 1 suggestion (M2), and 1 nit (M3). The fix pass (20c0216b5) addressed all three. Round 2 re-verified them on the fix delta and raised nothing new. In round 2 the reviewer ran `./bin/dev build` (0 errors) and `SignInOptions_Should_`, `SignInChoices_Should_`, `Microsoft365AccountChoice_Should_`, and `SignInActions_Should_` (all passed, 0 failed).

Final counts: bug 1 fixed, suggestion 1 fixed, nit 1 fixed. 0 open, 0 wontfix. **Disposition: clean.**

Framework: `review/review-framework.md`. Last merged ledger: `review/round-2/merged.md`. Disposition: `review/disposition.md`.

## Review

- Round 1: M1, M2, and M3 raised. Fixed in 20c0216b5.
- Round 2: Done
## Notes

- Source idea (read-only): the portal sign-in card is one Microsoft 365 work-account button and a staff-only line. This template keeps passkey as the default and does not copy that product name.
- Out of scope: the StyleGuidePage contract pass (379-017), changing Entra bootstrap policy, and removing passkey management from Settings.
