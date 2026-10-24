# Round 2 — general
**Date:** 2026-10-24
**Scope reviewed:** fix-pass delta `0d1792e7a..HEAD` (commit 20c0216b5, source + tests; b564a8f23 is kitchen only). Re-verified round-1 M1, M2, M3.

## Summary

The fix pass makes `SignInState.Microsoft365Offered` a `bool?` that is `null` until `FetchMicrosoft365Offered` writes true or false. Every consumer was checked: `SignInChoices` renders the button only for `true`, `ShowMicrosoft365Unavailable` is true only for `false`, Login passes the nullable value through, and Settings gates on `is true`. No other source consumer exists. The choose-account body moved into `Microsoft365AccountChoice` with the same markup and callbacks, and render tests pin all three modes. The provenance sentence is gone from the `SignInMode` Design region. Risk is low.

Verification run by this reviewer: `./bin/dev build` (0 errors, build succeeded); `SignInOptions_Should_`, `SignInChoices_Should_`, `Microsoft365AccountChoice_Should_`, and `SignInActions_Should_` all passed, 0 failed.

## Resolved prior

- M1 (bug) — fixed. Verified at `SignInChoices.razor:43` (`is true`), `sign-in-options.cs:43` (`offered is false`), `sign-in-state.cs:39,56` (`bool?`, `Initialize` sets null), `SettingsPage.razor:18` (`is true`). Unknown-offer render cases added for Microsoft365Only and Staff; the state test asserts the initial `null`.
- M2 (suggestion) — fixed. `Microsoft365AccountChoice.razor` holds the body; `microsoft-365-account-choice-render-tests.cs` pins Passkey, Microsoft365Only, and Staff.
- M3 (nit) — fixed. `sign-in-mode.cs:8` states the generic-copy rule only.

## Issues

None new on the fix delta.

## Plan alignment

The Goal restatement matches the task Description and extends it consistently (tri-state offer, choose-account behavior). No misread.
