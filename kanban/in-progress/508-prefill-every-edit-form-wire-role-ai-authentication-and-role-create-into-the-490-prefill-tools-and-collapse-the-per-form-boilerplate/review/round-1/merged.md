# Round 1 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:44
- Description: Agent drafts moved from `ProfileState` to `FormSuggestionState`, and the signed-out path no longer drops them. `ProfileState.SignOut` calls `FormSuggestionState.Initialize()` (`profile-state.sign-out.cs:78`). The auth listener's signed-out branch dispatches `ClearProfileData` (`AuthenticationStateListener.razor:44`), whose handler only calls `ProfileState.Initialize()` (`profile-state.clear-profile-data.cs:27`). Those suggestion fields used to live on `ProfileState`, so that `Initialize` was what cleared them. A draft that survives is applied by `FormPrefillScope` when the next person uses the same circuit. `Suggestion_Should_StoreClearAndAsk` (`profile-state-tests.cs`) used to send `ClearProfileData` and expect the suggestion gone; that assertion was removed instead of retargeted. `SignOut_Should_.AssertCleared` does not seed or check a form draft.
- Suggestion: Clear `FormSuggestionState` on the listener signed-out path and when the principal id changes. Assert that this reset leaves no drafts, and extend `SignOut_Should_.AssertCleared` to seed a draft and expect it gone.
- Source: general
- Disposition notes: Fixed. New `FormSuggestionState.SessionChangedActionSet` drops every draft when the principal id changes; `AuthenticationStateListener` dispatches it on every auth change (signed out sends an empty id). `Initialize` also resets the principal. `FetchAndUpdate_Should_.Suggestion_Should_Drop_When_PrincipalChanges` covers same-principal keep, sign-out drop and a different sign-in; `SignOut_Should_` seeds a draft and `AssertCleared` expects none.

## Duplicates / conflicts

- None. One reviewer.
