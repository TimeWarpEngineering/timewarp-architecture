# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:51
- Description: `SignInState.Microsoft365Offered` is a `bool` that is `false` until `FetchMicrosoft365Offered` completes. On the first interactive render after prerender, Microsoft365Only and Staff mode replace the Microsoft 365 button with the false message "Microsoft 365 sign-in is not available." The message stays until the fetch returns, and it also appears when the fetch fails.
- Suggestion: Make the offer tri-state (`bool?`, `null` until fetched). Show the unavailable line only on `false`. Add a render case with the offer unknown.
- Source: general
- Disposition notes: Fixed. `Microsoft365Offered` is `bool?` and `Initialize` sets null. The fetch still writes true or false, including false on a failed read. `ShowMicrosoft365Unavailable` is true only when the value is false. `SignInChoices` renders the button only for true and the unavailable line only for false. Microsoft365Only and Staff render cases cover the unknown offer. Settings shows the Microsoft 365 section only when the value is true.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:54
- Description: No test pins the choose-account page's mode behavior: "I already have an account" is hidden outside Passkey mode, and Staff mode shows `ChooseStaffNote`.
- Suggestion: Add a render test (or extract the unlinked-account body into a component) that pins Passkey, Microsoft365Only, and Staff.
- Source: general
- Disposition notes: Fixed. The unlinked-account body is `Microsoft365AccountChoice`, and the page renders it for a valid parked sign-in. `Microsoft365AccountChoice_Should_` pins Passkey (both buttons, no staff note), Microsoft365Only (no `AlreadyHaveAccount`, no staff note), and Staff (`ChooseStaffNote`, no passkey proof).

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/configuration/sign-in-mode.cs:8
- Description: The Design region carries provenance history ("The portal card that inspired this…") in template source that ships to every generated app.
- Suggestion: Drop the sentence, or state the rule only ("Copy is generic; no product or client name").
- Source: general
- Disposition notes: Fixed. The Design region now says "Copy is generic; no product or client name." The provenance sentence is gone.

## Duplicates / conflicts

- None (single reviewer).
