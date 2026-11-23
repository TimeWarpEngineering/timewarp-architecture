# Round 2 — general
**Date:** 2026-11-24
**Scope reviewed:** fix delta `5cb923a25..aa25cba8c` (commit aa25cba8c, "fix: keep a profile draft until the loaded form is the working copy"), plus the updated `## Results` and `### How to validate` in task.md. Effort 2 (by-diff budget), roster general.

## Summary

The fix pass addresses all five round-1 findings. ProfilePage now has a `WorkingCopyReady` flag. `CopyCommandFromProfile` sets it only after it has copied a loaded profile. `OnAfterRenderAsync` returns early until the flag is set, so a draft that arrives during the fetch stays in state (M1). A new page test file (`profile-page-prefill-tests.cs`, 5 tests) covers clean apply, dirty confirm, decline, the badge, and the load race (M2). `FormPrefill.TryNormalize` returns false with an accepted-keys error when no field matches. Both callers (the WebMCP dispatcher and `CatalogAgentFunctions`) already treat false as `drafted: false` and store nothing (M3). The confirm message is neutral, and a test asserts it (M4). The unreachable not-page-bound branch is gone. `FormPrefill.SelectAsync` (form-prefill.cs:93) still skips any action that is not page-bound, so the Design region's claim holds (M5). Results record a 0-warning build, the filtered test runs (5 + 5 + 8 passed), and `ganda repo audit` exit 0.

## Prior findings re-verified

- **M1 — fixed.** ProfilePage.razor:84 guards on `WorkingCopyReady`. CopyCommandFromProfile resets the flag and sets it only on a loaded profile. In a real circuit, ComponentBase re-renders after `OnInitializedAsync` completes, so the after-render pass picks the draft up. `Keep_A_Draft_That_Arrives_While_The_Profile_Is_Loading` pumps after-render while the profile read is held. It asserts the draft is kept and no prompt is shown, then that the draft is applied once the load completes.
- **M2 — fixed.** Page-lifecycle coverage is added. The static HTML renderer does not call `OnAfterRenderAsync`, so the tests call it through reflection. The test file's Design region documents this. The browser smoke still has not run, and Results say so and name the page tests as the substitute. Round 1 accepted either remedy.
- **M3 — fixed.** form-prefill.values.cs `NoMatch` covers whitespace input, JSON null, an empty object, and all-unknown keys. Its accepted-keys list comes from `Properties`, which already excludes `UserId`. The tests check both the WebMCP path and the in-app AIFunction path, and that state stays null.
- **M4 — fixed.** The message is neutral. The design-region text and the state test match.
- **M5 — fixed.** The `pageBound` local and the `else` branch are removed. Approval waits page-bound, and the path is re-checked after the tool is re-resolved.

## Issues

None. No new defects were found in the fix delta.

No targeted command was run. Every claim above comes from reading the diff, the surrounding call sites, and the recorded Results.
