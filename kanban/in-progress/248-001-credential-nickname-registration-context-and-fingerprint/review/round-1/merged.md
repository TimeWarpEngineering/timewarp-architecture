# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 2 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/postgres/migrations/20260923063306_AddCredentialNicknameAndRegisteredWith.cs:13
- Description: Pre-248-001 rows hold a caller-supplied name in `Label`; the migration only added columns, so on existing agent-key rows (and some passkeys) that user name now renders as the *provider*.
- Suggestion: Data step for agent keys (`Type = 2`): move `Label` → `Nickname` (capped 64), null `Label`; leave passkey rows (indistinguishable); record in the Credential Design region.
- Source: general
- Disposition notes: `Up` now runs `UPDATE identity.credentials SET "Nickname" = LEFT("Label", 64), "Label" = NULL WHERE "Type" = 2 AND "Label" IS NOT NULL`; `Down` restores `Label = COALESCE(Label, Nickname)` for agent keys before dropping the column. Passkey-row decision recorded in `credential.cs` Design. Exercised against real Postgres by `web-infrastructure-tests` (`Migrate()`), 57/57.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/AddPasskeyPrompt.razor:67
- Description: The prompt renders on every page via `TimeWarpPage`; on Settings a ceremony started from the prompt's CTA opened BOTH the prompt's "Name this passkey" form and the row's auto-opened inline editor for the same credential.
- Suggestion: One owner per pending nickname.
- Source: general
- Disposition notes: `CredentialsState.PendingNicknameOwnedByPrompt` + computed `PendingListRenameCredentialId` (null while the prompt owns it). New action `ClaimPendingNicknameForPrompt` (parameterless, mirrors `ClearPendingNickname`) is dispatched by `AddPasskeyPrompt` after its CTA succeeds and BEFORE `FetchCredentials`, so a list on the page never sees the id. Settings and Passkeys pages bind `PendingListRenameCredentialId`. Flag resets on rename success, `ClearPendingNickname`, `SetPendingNickname`, `Initialize`. Design regions reconciled on state, prompt, and SettingsPage.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/components/CredentialList.razor:105
- Description: `SaveRenameAsync` cleared `AutoOpenedId`; on a failed rename the still-pending id reopened the editor with `Draft` reset to the provider name, discarding the user's text.
- Suggestion: Keep the auto-open marker until pending actually clears.
- Source: general
- Disposition notes: An auto-opened editor now stays open on Save (Save disabled by `IsBusy` in flight) and closes only when the pending id clears on success; a failure leaves the draft in place. Manually opened editors keep the close-on-Save behaviour. Design region updated.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-row-presenter-tests.cs:1
- Description: Task required an SPA test for the row content AND inline rename; only pure presenter string tests shipped.
- Suggestion: Component-level test for `CredentialList`.
- Source: general
- Disposition notes: New `credential-list-render-tests.cs` (HtmlRenderer over a minimal TimeWarp.State container + FluentUI + fakes) pins: row title/context/created/fingerprint + action markers, editor absent by default; auto-open editor prefilled with the provider name for the pending id only (one editor, other row keeps its title); pending id not in list opens nothing; empty message. 4/4 green; SPA suite 50/50. Interactive click paths split out as M7.

### M5 — Severity: suggestion — Status: fixed
- File: tests/common/timewarp-testing/principal-store-contract-tests.cs:1
- Description: The private-property EF binding for `Nickname` / `RegisteredAttachment` / `RegisteredBrowser` / `RegisteredOs` was checked only at model level; no backend round-trip through `UpdateCredentialAsync`.
- Suggestion: Contract case Create(nickname + RegisteredWith) → Add → Get → Rename → Update → Get.
- Source: general
- Disposition notes: `Nickname_and_registered_with_round_trip_through_update` added to the shared `Credentials` contract and re-exported on both fixtures (in-memory 228/228, EF/Postgres 57/57; the EF case verified not soft-skipped).

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/complete-agent-key-registration/complete-agent-key-registration-contracts.cs:39
- Description: `label` → `nickname` on the agent-key wire shapes is a silent break for already-built clients (unknown member ignored, name dropped) and was undocumented.
- Suggestion: Note it in the Design region, or ship a deprecated alias.
- Source: general
- Disposition notes: Documented as a deliberate pre-1.0 WIRE BREAK in both `CompleteAgentKeyRegistration` and `AddAgentKey` Design regions (no alias shim; in-repo CLI moved in the same commit; external clients rename the field).

### M7 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/identity/components/CredentialList.razor:96
- Description: (Split from M4 by the orchestrator.) Click paths — Save trimming + `OnRename`, Cancel raising `OnRenameCancel` only when auto-opened, two-step revoke raising `OnDelete` only on Confirm — and `RenameCredentialActionSet` clearing pending state / posting to `ToastNotificationState` are not exercised by an automated test.
- Suggestion: Requires an interactive component renderer (bUnit) or a Playwright flow.
- Source: orchestrator
- Disposition notes: wontfix on this task. The repo has no interactive Blazor component test harness (only `HtmlRenderer`), and adding bUnit is a dependency/architecture decision for the maintainer, not a fix-loop call. The logic is small and covered by the render test (state → markup) plus the server integration tests for the rename endpoint. Flagged in disposition and task Notes as a follow-up decision.

## Duplicates / conflicts

- Single reviewer; no duplicates. M7 is a deliberate split of M4 so the fixed part and the deferred part carry honest, separate statuses.
