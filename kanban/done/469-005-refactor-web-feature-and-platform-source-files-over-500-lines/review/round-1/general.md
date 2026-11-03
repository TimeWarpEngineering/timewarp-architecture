# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** branch vs `ebac73588` (head `a91c4721f`), the 20 product files, excluding `kanban/`.

## Summary

The change moves code without changing behavior. `XaiChatClientCache`, `XaiFakeChatClient`, and
`XaiChatMapping` are faithful moves out of `xai-chat-upstream-server.cs`. The lock scope, eviction,
deferred disposal, and the release that skips the fake all match the old code. The tests still read
`XaiChatUpstream.MaxCachedClients` through a forwarding constant. `EntraAccountLinker` and
`EntraSignInStamp` are faithful moves too. The public surface, the constructor, and the DI
registration of `EntraTicketProcessor` did not change, and the old Design region was split across
the three files with nothing lost. A helper compared every runfile member before and after: no test
is missing or duplicated, every attribute is kept, and the only body changes are helper renames,
one inlined helper (M2), and the CA1859 return-type fix. The risk is low. The findings are only
about written context that no longer matches the code, plus two small duplications.

Gates the reviewer ran again in the claim worktree (not taken from the report):
- `./bin/dev build`: 0 warnings, 0 errors.
- `dotnet test` in `tests/container-apps/web/web-jaribu-tests`: 458 of 458 passed.
- `dotnet test` in `tests/container-apps/web/web-server-integration-tests`: 435 passed, 1 skipped, 0 failed.
- The helper ran `feedback-attachment-submit-tests.cs` standalone: 9 of 9 passed.
- No non-migration `source/container-apps/web/**/*.cs` file is over 500 lines. Every new file
  starts with a Purpose region and has a kebab name. The new runfiles are 100755, and the support
  files are 100644 and have no entry point.

Goal restatement vs task: they match. The restatement only adds acceptance detail the task
implies (no `partial`, one type per file, shared helpers written once). No misread.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/feedback/feedback-attachment-tests.cs:13
- Description: The Purpose region says this runfile covers "an expiry that survives a blob delete
  that throws". That test (`Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted`) is in
  `feedback-attachment-submit-tests.cs:116`, and that file's Purpose (lines 13-17) does not mention
  it. Purpose regions are agent context (TWA0004), so a region that names the wrong tests sends a
  reader to the wrong file.
- Suggestion: Move the test back to `feedback-attachment-tests.cs`, where the upload and expiry
  scenarios live. Or keep it where it is and fix both Purpose regions. Moving it fits the
  by-scenario split better.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/feedback/feedback-triage-world-tests.cs:90
- Description: `TriageWorld.AddAsync` used to call `FeedbackItemTriage_Given_.Filed(...)`. It now
  calls `FeedbackItem.File(owner, kind, "Export failed", "Nothing came back.", Day(day))` itself,
  which copies the literals in `Filed` at `feedback-triage-tests.cs:216`. It behaves the same. But
  the task says shared helpers are written once, and this is a second copy of the item-builder
  helper.
- Suggestion: Move `Filed` (and the `At` default it uses, if needed) into the world support file,
  and have both the triage class and `TriageWorld.AddAsync` call it.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/agent-chat/user-xai-api-key-harness-tests.cs:50
- Description: `SiteSetter` is a new constant with the same GUID as `UserA` at
  `user-xai-api-key-tests.cs:38`. Before the split, `StoreSiteAsync` used `UserA` itself. It behaves
  the same, but the value is now in two places, and a later edit to one would quietly change which
  user sets the site key.
- Suggestion: Keep one constant in the harness file and have the test file use it (the test file
  includes the harness), or note in the harness that it must equal `UserA`.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/features/identity/credential-rules-application.cs:6
- Description: The Design region says "EntraTicketProcessor enforces HoldsMicrosoft365". After this
  change the check is in `EntraAccountLinker.RefuseIfCallerAlreadyHasEntraAsync`. Other notes that
  point at the processor for the link rules have the same drift:
  `get-credentials/get-credentials-contracts.cs:47`,
  `get-credentials/get-credentials-handler-application.cs:29`, and
  `source/libraries/timewarp-identity/credentials/credential-usage-recorder.cs:9`. A reader
  following them lands in the processor and finds only a delegation.
- Suggestion: Name `EntraAccountLinker` (reached through `EntraTicketProcessor`) in
  `credential-rules-application.cs`, and check the other three notes the same way. In each one,
  name the type that now holds the rule.
- Status: open
