# Round 2 — general
**Date:** 2026-11-23
**Scope reviewed:** fix delta 44a850b72 (product paths `AiPage.razor` and `ai-page-assistant-draft-tests.cs`, plus the task.md record), against round 1 at 6da30734e.

## Summary

The fix pass deletes `AssistantDraftLoaded` and `ByokDraftLoaded`, so each form renders when `AssistantSource` or `ByokSource` is set. It also adds two `AiPage_Should_` tests that edit both forms, invoke `SaveAssistantAsync` or `SaveByokAsync` through reflection on the renderer dispatcher, and then read the rendered version attributes. Both round-1 findings are resolved, and the delta adds no new defect.

## Re-verification of round 1

- **M1 (suggestion), now fixed.** `Move_The_Assistant_Form_To_The_Saved_Response_And_Keep_A_Byok_Edit` starts at name Akili, version 3. It sets the name to Nia and the key policy to Off, then saves the name, and the response is Nia at version 9. In `ShouldReRender`, the assistant command (Nia, 3) differs from its source, and the stored name changed from Akili to Nia, so the command keeps version 3. Only the forced `AcceptAssistant` call in `SaveAssistantAsync` (`AiPage.razor:236`) moves it to 9. The test asserts version 9 and asserts there is no version 3, so it fails if the forced call is deleted. The key-policy test does the same for `SaveByokAsync` (`AiPage.razor:246`, versions 11 and 3). Both tests also check that the other form keeps its unsaved edit and takes the new version. The recorded gate is `--filter-class AiPage_Should_`, which passed 9 tests, including both of these tests by name.
- **M2 (nit), now fixed.** Neither flag remains. The markup tests `AssistantSource is not null` (`AiPage.razor:264`) and `ByokSource is not null` (`AiPage.razor:390`). Nothing clears either source after it is set, so the render conditions match the old flags. The Purpose region and the task's acceptance list both record this.

## Issues

None.

## Notes (not findings)

- The second accept call in each save handler, `AcceptByok(stored, force: false)` after the name save and `AcceptAssistant(stored, force: false)` after the key-policy save, has no effect in practice. `ShouldReRender` has already reconciled that form, so `Same*Snapshot` returns early. It is harmless.
- The goal restatement matches the original task. The two acceptance bullets added in this pass state behavior the review asked for and do not change scope. There is no misread.
- I ran no gates, per the recorded-gates rule. The implementer's record covers the delta: build, the `AiPage_Should_` filter, and the audit.
