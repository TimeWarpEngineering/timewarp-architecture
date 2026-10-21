# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** commit `ee675a318` (the whole task diff: server, storage, SPA, tests, template.json, tw-deploy skill)

## Summary

The change adds Admin → AI at `/Admin/AI`, behind `settings.write`, to set, replace, and clear the site xAI key. The plaintext is protected with Data Protection under the purpose `TimeWarp.Architecture.XaiApiKey`, and the ciphertext is stored in `secrets.protected_secrets` through a named, reusable `IProtectedSecretStore`.

Several things are sound and were checked:

- The key never reaches the browser or the audit log.
- Host `XAI:ApiKey` wins, and writes return 409 when it is set.
- The singleton upstream opens a scope for each resolve, so it holds no captive `DbContext`. The client cache is keyed on the resolved key.
- Authorization excludes `settings.read`.
- The no-postgres variant excludes the EF store and keeps the unavailable store.
- The contracts carry `[Operation]` and use `IQuery` / `IIdempotentCommand`.
- Every new file has a Purpose region.

Two themes need fixing:

- Resolving the key now touches the database on every Ask configuration load and completion, with no failure handling.
- The appended "Or open Admin → AI." sentence is placed wherever the hint is shown, including where it is wrong.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:89 (also `get-agent-chat-configuration-handler-application.cs:65`, `complete-agent-chat-handler-application.cs:58`, `site-xai-api-key-application.cs:81`)
- Description: `IsConfiguredAsync` used to check options only. With no host key it now calls `ResolveApiKeyAsync` → `EfProtectedSecretStore.UnprotectAsync`, and nothing catches a database failure. Before web-migrations creates `secrets.protected_secrets` (42P01), or during any Postgres outage, the configuration query that every signed-in user's Ask makes returns 500 instead of "not configured". The same applies to the completion pre-check and to Admin → AI `GetStatusAsync`. Only `GetClientAsync` inside `CompleteAsync` is wrapped. Site settings already map 42P01 to 503 (`features/settings/get-site-settings/get-site-settings-handler-application.cs`, `site-settings-problems-application.cs`).
- Suggestion: catch storage exceptions in `SiteXaiApiKey.ResolveApiKeyAsync`, log the exception type (never the value), and return null, so Ask degrades to NotConfigured. Give `GetStatusAsync`, set, and clear a 503 storage-unavailable path that matches the site-settings precedent. Add a test with the table missing or the store throwing.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/features/agent-chat/xai-chat-contracts.cs:76 (`SetupHint`); shown by `projects/web-spa/features/admin/ai/pages/AiPage.razor:91-92`, `get-xai-api-key-status-handler-application.cs:63`, `xai-api-key-problems-application.cs:40`
- Description: `SetupHint(development)` always appends `AdminAiSentence` ("Or open Admin → AI."). With no postgres, or with no connection string, Admin → AI renders "Set the key on the host. Admin cannot store one here." and then a hint that ends "Or open Admin → AI." That points the admin at the page they are on, which cannot store a key. The Goal says that in this mode Admin → AI "only shows how to set the host value". The 503 `StorageUnavailable` detail and the Ask hint carry the same sentence in the no-storage variant. `site-xai-api-key-tests.cs:134` asserts `ShouldContain("Admin → AI")` on the storage-missing 503, so the test locks the defect in.
- Suggestion: make the hint depend on storage availability, for example `SetupHint(development, adminStorage)`, and add the Admin sentence only when the store can hold a key. Leave it out of the Admin → AI page and of the 503 detail entirely. Flip the line-134 assertion.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:665, with the text from `xai-chat-contracts.cs:76`
- Description: Ask renders the server `SetupCommand` (now `SetupHint(true)`) in `<pre data-qa="AgentAskSetupCommand">` under the lead-in "Set the xAI key this way, then reload:". The block is now `dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --id … Or open Admin → AI.`. Pasting it passes `Or open Admin → AI.` to `dotnet user-secrets` as extra arguments, which breaks the copy-paste command that task 290 fixed. The `SetupCommand` doc still says "Exact pwsh command". The Playwright assertions (`ask-sign-in-playwright-tests.cs:317`, `ask-surface-playwright-tests.cs:239`) were changed to expect the concatenated text.
- Suggestion: keep `SetupCommand` alone in the `<pre>`. Carry the Admin sentence as its own field or paragraph, gated per Issue 2, and put the Playwright assertions back to the exact command.
- Status: open

### Issue 4 — Severity: bug
- File: source/container-apps/web/features/agent-chat/site-xai-api-key-application.cs:90 (`Configured: host || row is not null`) vs `ef-protected-secret-store-infrastructure.cs:109-117`
- Description: if the stored ciphertext can't be unprotected (key ring lost or rotated out, or a purpose mismatch), `UnprotectAsync` logs a warning and returns null, so Ask reports not configured. `GetStatusAsync` still reports `Configured = true` and "ends in …xxxx" because it only checks that the row exists. That contradicts `IXaiApiKeyStatus.Configured` ("True when Ask has a key", `xai-api-key-status-contracts.cs:20`), and the admin gets no signal to re-enter the key.
- Suggestion: compute `Configured` from a trial resolve, or add an `Unreadable` / `NeedsReplacement` status that Admin → AI shows. Cover it with a test that stores a row under a different purpose or a different key ring.
- Status: open

### Issue 5 — Severity: suggestion
- File: projects/web-spa/features/admin/ai/xai-api-key-state/xai-api-key-state.set-xai-api-key.cs, `xai-api-key-state.clear-xai-api-key.cs`
- Description: after an admin saves or clears the key, an open tab's Ask still shows its earlier readiness until a reload. Chat configuration loads only at startup and on sign-in changes, and the NotConfigured branch has no Retry. Task 361 solved the same problem for the assistant name (`agent-surface-state.assistant-identity-refresh-handler.cs`).
- Suggestion: add a post-pipeline handler on the set and clear actions that dispatches `AgentSurfaceState.LoadChatConfiguration()` when `!SaveFailed`. Follow the 361 precedent and use `CrossSliceReference` as needed.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/features/agent-chat/site-xai-api-key-application.cs:138-148; `platform/postgres/ef-protected-secret-store-infrastructure.cs:76-87`
- Description: `SetAsync` reads with `FindAsync` to choose the audit label, then the store reads again and either adds or replaces. Two concurrent first sets both `Add` and hit the `Name` primary key, which is an unhandled `DbUpdateException` and a 500. The label comes from a separate read, so it can say "set" when the write was a replace.
- Suggestion: have `IProtectedSecretStore.SetAsync` return whether it inserted or replaced, and use that for the audit line. Catch the unique violation and retry as a replace, or upsert with `ON CONFLICT`.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-handler-application.cs:58 and `xai-chat-upstream-server.cs:194`
- Description: each Ask turn resolves the key twice: once in the `IsConfiguredAsync` pre-check and again in `GetClientAsync`. Each resolve opens a scope, makes a database round trip, and runs an unprotect.
- Suggestion: resolve once per turn. Either let `CompleteAsync` return NotConfigured and drop the pre-check, or pass the resolved key through.
- Status: open

### Issue 8 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/features/agent-chat/xai-api-key-admin-tests.cs:101-161
- Description: the checklist item "Tests: round trip set → status → Ask uses it" is proven in two halves that never join. `XaiApiKeyAdmin_` checks `ISiteXaiApiKey.ResolveApiKeyAsync` against real Postgres. `XaiChatUpstream_Given_` checks the upstream factory seam against an in-memory store. No test runs a completion through the real store and upstream after an HTTP PUT.
- Suggestion: in `XaiApiKeyAdmin_`, register a capturing `IXaiChatClientFactory` (registration uses `TryAdd`), run one completion after the PUT and one after the replace, and assert the captured key.
- Status: open

### Issue 9 — Severity: nit
- File: source/container-apps/web/platform/postgres/migrations/20261021214830_AddProtectedSecrets.cs:9 (and the Designer file at line 16)
- Description: the class is `_20261021214830_AddProtectedSecrets`. Every other migration class is the bare name (`AddSiteSettings`, `AddRolePermissions`, …).
- Suggestion: rename the class to `AddProtectedSecrets` in both partials. The `[Migration]` id stays the same.
- Status: open

### Issue 10 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:108
- Description: `AutoComplete="off"` on the password input. Chromium ignores `off` on password fields, so it may offer to save the key or autofill a login into the field.
- Suggestion: use `AutoComplete="new-password"`.
- Status: open

### Issue 11 — Severity: nit
- File: kanban/.../task.md `## Results` → How to validate → Expect
- Description: the Results list "The audit lines are `set`, `replace`, and `clear` plus the principal id" under the `XaiApiKeyAdmin_` expectations. That test has no audit assertions. The audit lines are asserted in `site-xai-api-key-tests.cs` (`SiteXaiApiKey_Given_`).
- Suggestion: attribute the audit expectation to `SiteXaiApiKey_Given_`, or add the assertion to the integration test.
- Status: open

### Issue 12 — Severity: nit
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:214
- Description: when the key changes, `RealClient?.Dispose()` runs inside the lock, but a concurrent request may still be in `GetResponseAsync` on the old client. This is harmless only while the adapter's `Dispose` does nothing.
- Suggestion: drop the dispose on swap, or dispose the old client only after in-flight calls finish.
- Status: open
