# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/508-prefill-every-edit-form-wire-role-ai` vs merge-base with `master`, against the original task and `## Goal (implementer restatement)`. Gates were not re-run. `## Results` and `### How to validate` record `./bin/dev build` 0/0, the named web-spa test classes, `./bin/dev lint-skills`, and `ganda repo audit` (one pre-existing advisory).

## Summary

The shared store, `FormPrefillScope<TSubmit>`, and the derived opt-in match the restatement. Profile, Role create, Role edit, both AI submits, and Authentication render the scope with `Field` set to the details property. `[EditFormSubmit]` is gone. The draft schema is the `I*Details` surface, so `RoleId`, `Version`, and `UserId` are not accepted keys on the wired commands. Passkeys rename stays unwired because `RenameCredential` takes an id and a nickname and has no `Command`. The convention test pins those six submits and fails a derived submit with no scope. The recorded tool listing matches that set.

One regression: agent drafts used to die with `ProfileState.Initialize()`. They now live on `FormSuggestionState`, and the signed-out path that the auth listener actually runs does not clear that store. The test that locked the old clear was deleted instead of pointed at the new store.

Screenshots in `evidence/` are still absent. That is a misread of the original Evidence section, recorded under `## Learnings`, not filed here. The checklist item is already open, and Results name why this worker did not start the AppHost.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:44
- Description: Moving the draft off `ProfileState` drops it on the session boundary the app already uses for sign-out. `FormSuggestionState` says `Initialize` runs on sign-out so the next person never sees a draft (`form-suggestion-state.cs`). `ProfileState.SignOut` does call it (`profile-state.sign-out.cs:78`). The listener's signed-out branch does not. It dispatches `ClearProfileData` (`AuthenticationStateListener.razor:44`), and that handler only calls `ProfileState.Initialize()` (`profile-state.clear-profile-data.cs:27`). Before this change the suggestion fields lived on `ProfileState`, so that same `Initialize` cleared them. The listener is the path that runs when the principal becomes anonymous without the SignOut action, including an in-app sign-in that keeps the circuit (the listener's own comment on task 293). A draft left in the store is then applied by `FormPrefillScope` onto the next person's form. The same change deleted the assertion that locked this. `Suggestion_Should_StoreClearAndAsk` in `profile-state-tests.cs` used to send `ClearProfileData` and expect the suggestion to be gone. It now only clears by draft id. `SignOut_Should_.AssertCleared` never seeds a `FormSuggestionState` draft, so the path that does call `Initialize` is also untested. No command was run; the two handlers and the test diff are the evidence.
- Suggestion: Clear `FormSuggestionState` on the listener's signed-out branch, in `ClearProfileData` or beside the other resets there, and when the principal id changes so a different sign-in on the same circuit cannot apply the previous draft. Put back an assertion that `ClearProfileData` (or that listener reset) leaves no drafts, and extend `SignOut_Should_.AssertCleared` to seed a draft and expect it gone.
- Status: open
