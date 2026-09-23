# Round 2 — general
**Date:** 2026-09-23
**Scope reviewed:** merge resolution 3aeed9b1 (origin/master task 246 → task/248-001) + branch delta vs origin/master for `CredentialList.razor`, `SettingsPage.razor`/`.razor.cs`, `PasskeysPage.razor`/`.razor.cs`, `credential-list-render-tests.cs`, `credentials-spa-test-application.cs`, and related callers/tests

## Summary

The hand-resolved merge lands both sides cleanly: task 246's `Revoke*` parameter rename, `RevokePasskey` data-qa default, `CanRevoke`/`CanUnlink(ActiveCredentialCount)` disable-with-hint, and the "Credential revoked." status text are all present and unchanged in behavior; task 248-001's nickname title, context line, fingerprint, inline rename (auto-open + M2/M3 ownership and draft-retention rules), and the two-step restating revoke confirmation are all present and wired to the renamed `OnRevoke`/`RevokeDisabled` surface. `RevokeDisabled` correctly gates both the first-step button (`Disabled="@(!CanClickRevoke || ConfirmingId == credential.Id.Value)"`) and the Confirm button (`Disabled="@(!CanClickRevoke)"`) in `CredentialList.razor:184,207`. The updated 9-arg `CredentialSummary` construction in `ScriptedCredentialsApiService` and the render-test `Summary()` helper both match the current contract's parameter order exactly. No conflict-marker residue, no leftover `Delete*`/`OnDelete` identifiers outside one intentional `ShouldNotContain` negative assertion. Risk is low.

## Prior findings re-verified

| ID | Round-1 status | Still holds after merge? | Note |
|----|----------------|--------------------------|------|
| M1 | fixed | yes | Migration data step (`UPDATE identity.credentials SET "Nickname" = LEFT("Label", 64), "Label" = NULL WHERE "Type" = 2 …`) unchanged at `source/container-apps/web/platform/postgres/migrations/20260923063306_AddCredentialNicknameAndRegisteredWith.cs:22-27`; file was not touched by the merge. |
| M2 | fixed | yes | `PendingNicknameOwnedByPrompt` + computed `PendingListRenameCredentialId` still in `credentials-state.cs:75-79`; both `SettingsPage.razor:131` and `PasskeysPage.razor:143` bind `PendingRenameCredentialId="CredentialsState.PendingListRenameCredentialId"` post-merge. |
| M3 | fixed | yes | `SaveRenameAsync` in `CredentialList.razor:110-121` still only clears `EditingId` when `AutoOpenedId != id`; auto-opened editor stays open until the pending id clears on success. Untouched by the merge conflict hunks. |
| M4 | fixed | yes | `credential-list-render-tests.cs` re-resolved with `RevokePasskey` (not `DeletePasskey`) in the title/context assertions, plus a new fact `Revoke_Disabled_Disables_First_Step_And_Shows_Hint` covering 246's guard on the same component. |
| M5 | fixed | yes | `Nickname_and_registered_with_round_trip_through_update` still present in `tests/common/timewarp-testing/principal-store-contract-tests.cs:284`; not part of the conflict set. |
| M6 | fixed | yes | Wire-break Design note unaffected — file not part of the merge conflict. |
| M7 | wontfix | n/a | Not re-opened; the merge did not add an interactive component test harness. Click paths (Save/Cancel/two-step revoke raising `OnRevoke`) remain uncovered by an automated interactive test, consistent with the round-1 disposition. |

## Issues

No new issues.

Verification performed (falsifiable checks, all passed):
- `grep -rn "OnDelete\|DeleteLabel\|DeleteDataQa\|DeletePasskey\b" source tests` → only hit is a `ShouldNotContain("data-qa=\"DeletePasskey\"")` negative assertion in `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs:251`.
- `grep -rn "^<<<<<<<\|^>>>>>>>" source tests kanban` → no hits.
- `CredentialsState.CanUnlink(CredentialsState.ActiveCredentialCount)` bound identically as `CanRevoke` on both `SettingsPage.razor:19` (two `CredentialList` instances, lines 126 and 166) and `PasskeysPage.razor:17,138`.
- `RevokeCredentialActionSet.Handler.HandleSuccess` still sets `StatusMessage = "Credential revoked."` (`credentials-state.revoke-credential.cs:59`), matching the hint/status text pinned by `tests/.../protected-page-deep-link-tests.cs:302` (`LastCredentialHint`) and `tests/.../credentials-state-revoke-guard-tests.cs` (`state.StatusMessage.ShouldBe("Credential revoked.")`).
- `get-credentials-contracts.cs:98-109` parameter order (`Id, Type, Label, Nickname, CreatedAt, RevokedAt, IsActive, RegisteredWith, Fingerprint`) matches both the merged `ScriptedCredentialsApiService.Active/Revoked`/`RevokeCredential.Command` branch construction and the render test's `Summary()` helper.
- `credentials-state-revoke-guard-tests.cs` (not itself conflicted) continues to compile against the merged `ScriptedCredentialsApiService` signature and still asserts the same `RevokeDisabled(scope)` predicate.
