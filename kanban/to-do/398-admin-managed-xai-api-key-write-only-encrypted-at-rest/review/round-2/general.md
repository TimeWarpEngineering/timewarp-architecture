# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** the fix delta `df2f61ddf` vs `9df34f363` (the round-1 review commit). That is 42 files, about 1050 changed lines. The 12 round-1 findings were re-checked against this delta, and the delta was scanned for new defects.

## Summary

The fix pass addresses all 12 round-1 findings. Key resolution now degrades to "not configured" on a store failure, and status, set, and clear return 503. The Admin sentence is a separate `AdminHint` field, sent only when the host can store a key. The copy block is back to the exact command. `NeedsReplacement` comes from a trial unprotect. A post-pipeline handler reloads Ask readiness. The store reports inserted or replaced and retries a unique violation as a replace. One completion turn reads the key once. The admin integration test now captures the key the chat client receives. The overall risk is low. One new nit: a setup-hint helper is left with no production caller.

## Verification run (this round, in the claim worktree)

- `web-jaribu-tests`: `SiteXaiApiKey_Given_` 6/6, `XaiApiKeyStatus_Given_` 2/2, `GetAgentChatConfigurationHint_Given_` 6/6, `CompleteAgentChatHandler_Given_` 5/5.
- `web-spa-integration-tests`: `AgentAskReadiness_Should_` 14/14.
- `web-server-integration-tests`: `XaiChatUpstream_Given_` 11/11, `XaiApiKeyAdmin_` 1/1 (real Postgres).
- `./bin/dev build`: 0 warnings, 0 errors.
- `./bin/dev lint-skills` (needed because `skills/tw-blazor/SKILL.md` changed) could not run here. The vally on PATH is 0.6.0, and the repo pins `@microsoft/vally-cli@0.19.0`. This is a local tool-version problem, not a finding. The PR gate or CI still has to run it.
- The Ask Playwright classes were not executed. The diff from `85ce8a719` to `df2f61ddf` on both Playwright files is empty, so the task 290 assertion (`ShouldBe(XaiChatDefaults.SetupCommand)`) is restored as it was.

## Round-1 re-verification

| ID | Verdict | Evidence |
|----|---------|----------|
| M1 | fixed | `SiteXaiApiKey.ResolveApiKeyAsync` catches storage failures and returns null. `GetStatusAsync`, `SetAsync`, and `ClearAsync` return `XaiApiKeyProblems.StorageFailed()` (503). Covered by `Storage_Failure_Should_Degrade_Ask_And_Return_503`. |
| M2 | fixed | `HostSetupText` has no Admin sentence. The configuration handler sets `AdminHint` only when `Keys.StorageAvailable`. Status, `StorageUnavailable`, and the 503 details use `HostSetupText`. The tests are flipped. |
| M3 | fixed | `AgentAsk.razor` renders `AdminHint` in its own `<p data-qa="AgentAskAdminHint">` after the `<pre>`. The Playwright assertions are restored. `Render_The_Admin_Sentence_Outside_The_Copy_Block` checks the order. |
| M4 | fixed | `GetStatusAsync` does a trial unprotect. An unreadable row is `Configured=false, NeedsReplacement=true`. `AiPage` says the key has to be replaced. Covered by `Unreadable_Stored_Key_Should_Need_Replacement`. |
| M5 | fixed | `AgentSurfaceState.XaiApiKeyRefreshHandler` (post-pipeline, skipped on `SaveFailed`). `ApplyStatus` resets `SaveFailed`. Covered by the reload and skip tests. |
| M6 | fixed | `EfProtectedSecretStore.SetAsync` returns `ProtectedSecretWrite`. A unique violation detaches the added row and replaces the winner. The audit label comes from the write result. The race is exercised in `XaiApiKeyAdmin_`, though not deterministically. |
| M7 | fixed | `BeginKeyResolution` opens an `AsyncLocal` slot on the handler's flow, `IsConfiguredAsync` fills it, and `GetClientAsync` uses it. `One_Turn_Unprotects_The_Stored_Key_Once` asserts one unprotect. |
| M8 | fixed | `XaiApiKeyAdmin_` swaps in a `CapturingChatClientFactory` and asserts `[Secret, Replacement]` after the HTTP set and replace. |
| M9 | fixed | The migration partials are `AddProtectedSecrets`. |
| M10 | fixed | `AutoComplete="new-password"`. |
| M11 | fixed | Results now attribute the audit lines to `SiteXaiApiKey_Given_`. |
| M12 | fixed | A key swap moves the old client to `RetiredClients`, which are disposed with the upstream. This is one of the two remedies M12 offered. |

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/agent-chat/xai-chat-contracts.cs:82
- Description: after the split into `HostSetupText` plus a separate `AdminHint`, `XaiChatDefaults.SetupHint(bool development, bool adminStorage = false)` has no production caller. The only callers are `get-agent-chat-configuration-tests.cs:48`, `:51`, `:58`, and `:64`. Those tests assert a "command + Admin sentence" string that nothing renders anymore, which brings back the M2/M3 shape as a helper that looks supported. This is a public member on a contracts type in the template, so every generated app ships it.
- Suggestion: delete `SetupHint`. Point those test lines at `HostSetupText` (for the no-Admin cases) and at the handler's `AdminHint` (already asserted in the same file), or drop the duplicate assertions.
- Status: open

## Considered and not raised

- `SiteXaiApiKey.IsStorageFailure` and `EfProtectedSecretStore.IsUniqueViolation` classify exceptions by type name and message text. This follows the repo precedent (`SiteSettingsSeeder.IsUndefinedTable` matches `42P01` in text), so it is not raised.
- `RetiredClients` keeps a replaced key's client alive until the process stops. Growth is bounded by admin key changes, and this was the remedy M12 offered.
- `XaiApiKeyAdmin_` does not deterministically hit the unique-violation retry path. That is acceptable for a suggestion-level fix.

## Notes for the operator (not findings)

- The round-1 misread still stands. The checklist item "Without `postgres`: decide with Steve" is ticked, and no Note records Steve's decision. Confirm it before merge.
