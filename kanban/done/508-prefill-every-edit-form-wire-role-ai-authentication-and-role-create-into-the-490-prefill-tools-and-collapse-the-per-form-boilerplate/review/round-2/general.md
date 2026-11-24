# Round 2 — general
**Date:** 2026-11-24
**Scope reviewed:** Fix commit `a4d911915` (review round 1 M1) against its parent. Not the whole task diff. Gates were not re-run. `## Results` records the fix pass: `./bin/dev build` 0/0, `FetchAndUpdate_Should_` 6 tests, `SignOut_Should_` 2 tests, and `ganda repo audit` with the same pre-existing advisory.

## Summary

The fix adds `FormSuggestionState.SessionChanged`, dispatched from `AuthenticationStateListener` on every authentication change with `AskReferenceState.PrincipalIdOf` (empty when signed out). A different principal replaces the draft map and stores the new id. The same principal keeps the drafts. `Initialize`, still called from `ProfileState.SignOut`, also forgets the principal. `FormPrefillScope` reads the store again at apply time, so a cleared draft is not applied on the next render.

M1 is fixed. The delta does not loosen a test or silence a check. No new issues.

## Prior findings

### M1 — fixed
- The listener calls `NoSubFormSuggestionState.SessionChanged` before the signed-in branch (`AuthenticationStateListener.razor:37`). The signed-out path no longer depends on `ClearProfileData`.
- The handler drops every draft when the id changes (`form-suggestion-state.session-changed.cs:37-43`), including a change to the empty id and a change from one person to another. An equal id returns without clearing.
- `Suggestion_Should_Drop_When_PrincipalChanges` covers keep, signed-out clear, and a different sign-in. `SignOut_Should_` seeds a draft and `AssertCleared` expects none. `Seed` asserts the draft is present before sign-out, so the later empty check is not vacuous.
- The fix commit adds those assertions. It does not delete or loosen an existing one.
