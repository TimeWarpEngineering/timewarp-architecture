# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:51
- Description: `SignInState.Microsoft365Offered` is a `bool` that is `false` until `FetchMicrosoft365Offered` completes. On the first interactive render after prerender, Microsoft365Only and Staff mode replace the Microsoft 365 button with the false message "Microsoft 365 sign-in is not available." The message stays until the fetch returns, and it also appears when the fetch fails.
- Suggestion: Make the offer tri-state (`bool?`, `null` until fetched). Show the unavailable line only on `false`. Add a render case with the offer unknown.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:54
- Description: No test pins the choose-account page's mode behavior: "I already have an account" is hidden outside Passkey mode, and Staff mode shows `ChooseStaffNote`.
- Suggestion: Add a render test (or extract the unlinked-account body into a component) that pins Passkey, Microsoft365Only, and Staff.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/configuration/sign-in-mode.cs:8
- Description: The Design region carries provenance history ("The portal card that inspired this…") in template source that ships to every generated app.
- Suggestion: Drop the sentence, or state the rule only ("Copy is generic; no product or client name").
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
