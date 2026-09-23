# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** branch task/248-001 vs e3bb24b7 (55 files)

## Summary

The branch splits the credential's immutable provider `Label` from a new user-editable `Nickname` and adds a `RenameCredential` endpoint. It also captures a `RegisteredWith` context at registration (attachment plus browser and OS family; the raw User-Agent is never stored), exposes a display-only `Fingerprint` (last 8 hex of SHA-256(handle)), and rebuilds the `CredentialList` row with inline rename and a two-step revoke that restates the row. The server side checks out against the claims. The rename handler matches `RevokeCredential` on policy and schemes, the ownership check, the 404 shape and the retry loop. The validator, the domain `Rename` and the SPA agree on 1–64 characters after trim. The EF mapping matches the migration and the snapshot. The UA classifier caps its input and returns only family names. I also re-ran both co-located runfiles: 9/9 each. The remaining findings are about legacy data, UI state edges, and test coverage — no correctness or security defect in the new server paths.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/platform/postgres/migrations/20260923063306_AddCredentialNicknameAndRegisteredWith.cs:13
- Description: Before this change, a caller-supplied name was written into `Label`. That happened in `AddPasskey` (when `Label` was set), `AddAgentKey` and `CompleteAgentKeyRegistration`. The migration only adds columns, so existing rows keep a user name in `Label` with `Nickname = NULL`. Under the new semantics that user name is shown as the *provider*: the first segment of `CredentialRowPresenter.ContextLine` and the "Provider" part of the revoke confirmation. For agent keys every existing non-null `Label` is by definition a user name, because agent keys have no provider.
- Suggestion: Add a data step to the migration. At minimum: `UPDATE identity.credentials SET "Nickname" = LEFT("Label", 64), "Label" = NULL WHERE "Type" = <AgentKey> AND "Label" IS NOT NULL`. Passkey rows cannot be told apart, so leave them and record that decision in the Credential Design region. Also check whether the in-memory seed or demo data needs the same treatment.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor:67
- Description: `AddPasskeyPrompt` is rendered by `TimeWarpPage` on every page, including Settings. On Settings, the passkey `CredentialList` also receives `PendingRenameCredentialId` (`SettingsPage.razor:129`). An Entra-only user who starts the ceremony from the prompt's CTA while on Settings therefore sees two nickname editors for the same credential: the prompt's "Name this passkey" form, and the row's auto-opened inline editor. The Design text says `ShowNicknameForm` is component-local so that *Settings'* Create button does not open the prompt form. The reverse overlap (a prompt-started ceremony opening the row editor) is not handled.
- Suggestion: Pick one surface per pending nickname. For example, record the originating surface in `CredentialsState` (or a flag passed through `SetPendingNickname`/`AddPasskey`) and have `CredentialList` skip auto-open when the prompt owns it. Alternatively, suppress the prompt form when a `CredentialList` for passkeys is on the page.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/components/CredentialList.razor:105
- Description: `SaveRenameAsync` clears `AutoOpenedId` before awaiting `OnRename`, but `PendingNicknameCredentialId` is cleared only in `RenameCredential`'s `HandleSuccess`. If the rename fails (a problem-details response), pending stays set. The caller's following `FetchCredentials` changes `Credentials`, `OnParametersSet` (line 58) reopens the editor, and `Draft` is reset to `PendingRenameDefault` (the provider name), so the user's typed nickname is discarded. The same reopen can flicker during an in-flight save if the parent re-renders before success clears pending. Verified by reading the code, not by running the UI.
- Suggestion: Keep `AutoOpenedId` set (or keep a "submitted" marker) until pending actually clears, and leave `Draft` alone when reopening for the same id.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-row-presenter-tests.cs:1
- Description: The task asks for an "SPA test for the row content and inline rename". The branch adds only pure `CredentialRowPresenter` string tests. Nothing exercises `CredentialList`'s inline-rename behaviour: auto-open for the pending id, prefill, Cancel raising `OnRenameCancel` only for an auto-opened editor, Save trimming and raising `OnRename`, the two-step revoke raising `OnDelete` only on Confirm. Nothing covers `RenameCredentialActionSet` clearing pending state or posting to `ToastNotificationState` either. The Results section's claim that the Requirements' Tests bullet is complete overstates the coverage.
- Suggestion: Add a component-level test for `CredentialList` (and ideally the state handler) in web-spa-integration-tests, or record the gap in task.md Results as a follow-up.
- Status: open

### Issue 5 — Severity: suggestion
- File: tests/common/timewarp-testing/principal-store-contract-tests.cs:1
- Description: The new private-property EF binding is only checked at model level (`identity-model-mapping-tests.cs`). That binding covers `RegisteredAttachment`/`RegisteredBrowser`/`RegisteredOs` (getter-only properties bound through the private constructor) and `Nickname` written via `PersistReplacementAsync`. The shared `IPrincipalStore` contract suite, which runs against both InMemory and ephemeral Postgres, has no case that writes a credential with a `RegisteredWith` and a nickname, then reads it back after `UpdateCredentialAsync`. The rename integration tests run on the in-process host's store, so the EF write and rehydrate path for the new columns is untested.
- Suggestion: Add a contract case: Create with nickname + `RegisteredWith` → Add → Get (fields equal) → `Rename` → `UpdateCredentialAsync` → Get (new nickname, `RegisteredWith` unchanged, Version+1).
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/features/identity/complete-agent-key-registration/complete-agent-key-registration-contracts.cs:39
- Description: Renaming the wire property `Label` → `Nickname` on `CompleteAgentKeyRegistration` and `AddAgentKey` is a silent break for any already-built agent client. System.Text.Json ignores the unknown `label` member, so the request still succeeds but the name is dropped with no error. The in-repo agent CLI was updated (`tools/agent-identity-cli/services/agent-wire-dtos.cs:25`). This is acceptable pre-1.0, but it is not called out anywhere.
- Suggestion: Note the break in the contract Design region / release notes. Alternatively, accept `label` as a deprecated alias for one release (for example, a `[JsonPropertyName("label")]` shim property mapped onto `Nickname`).
- Status: open
