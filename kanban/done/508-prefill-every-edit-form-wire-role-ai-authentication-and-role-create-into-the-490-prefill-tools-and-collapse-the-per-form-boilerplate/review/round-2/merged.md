# Round 2 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:37
- Description: Re-checked on the fix delta. `FormSuggestionState.SessionChanged` drops every draft when the principal id changes, and the authentication listener dispatches it on every auth change, signed out included (empty id). `ProfileState.SignOut` still calls `FormSuggestionState.Initialize`, which also clears the principal id. `FetchAndUpdate_Should_.Suggestion_Should_Drop_When_PrincipalChanges` covers same-principal keep, signed-out drop, and a different sign-in. `SignOut_Should_` seeds a draft and expects it gone.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `a4d911915`. Round 2 re-read the handler, the listener call, sign-out `Initialize`, and the two tests. No regression on this delta.

## Issues

None.

## Duplicates / conflicts

- None. One reviewer. M1 is carried from round 1; no new id.
