# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general (with a read-only runfile-equivalence helper)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-tests.cs:13
- Description: The Purpose region claims "an expiry that survives a blob delete that throws". That
  test (`Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted`) is in
  `feedback-attachment-submit-tests.cs:116`, and that file's Purpose does not mention it.
- Suggestion: Move the test back to the upload and expiry runfile (preferred), or fix both Purpose
  regions.
- Source: general
- Disposition notes: Moved the test back to `feedback-attachment-tests.cs`, after the expiry test. Both Purpose regions now match their tests (fix pass, 2026-11-04).

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-triage-world-tests.cs:90
- Description: `TriageWorld.AddAsync` inlines `FeedbackItem.File(..., "Export failed", "Nothing
  came back.", ...)`, which copies the `Filed` helper at `feedback-triage-tests.cs:216`. That is a
  second copy of a shared helper, and the task says helpers are written once.
- Suggestion: Move `Filed` into the world support file and call it from both places.
- Source: general
- Disposition notes: `TriageWorld.Filed(owner, filedAt, kind)` is the one helper. `AddAsync` and the triage tests call it (fix pass, 2026-11-04).

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/agent-chat/user-xai-api-key-harness-tests.cs:50
- Description: The new `SiteSetter` GUID is the same value as `UserA` (`user-xai-api-key-tests.cs:38`),
  which `StoreSiteAsync` used before the split. The value is now in two places.
- Suggestion: Keep one constant in the harness and use it from the test file.
- Source: general
- Disposition notes: `XaiKeyTestHarness.UserA` holds the GUID and sets the site key. The test file's `UserA` reads it (fix pass, 2026-11-04).

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/credential-rules-application.cs:6
- Description: Design notes still say `EntraTicketProcessor` enforces `HoldsMicrosoft365` and does
  the link and attach stamping. After the split, that code is in `EntraAccountLinker` and
  `EntraSignInStamp`. The same drift is in `get-credentials/get-credentials-contracts.cs:47`,
  `get-credentials/get-credentials-handler-application.cs:29`, and
  `source/libraries/timewarp-identity/credentials/credential-usage-recorder.cs:9`.
- Suggestion: In each note, name the type that now holds the rule.
- Source: general
- Disposition notes: The four notes now name `EntraAccountLinker` (HoldsMicrosoft365) and `EntraSignInStamp` (the stamp) (fix pass, 2026-11-04).

## Duplicates / conflicts

- None. The helper's three items are M1 to M3, and M4 came from the reviewer's own cross-reference
  grep.

## Verified (no finding)

- `XaiChatClientCache`, `XaiFakeChatClient`, and `XaiChatMapping` are moved verbatim. The lock
  scope, eviction order, deferred disposal, and the fake release that does nothing are unchanged.
  The `MaxCachedClients` forwarder keeps `xai-chat-upstream-tests.cs:383,415` compiling.
- `EntraTicketProcessor`'s public surface, its constructor, and `AddScoped<EntraTicketProcessor>`
  did not change. The link, merge, and attach branches and every stamp point are preserved. The
  Design-region content was split across the three files with nothing lost.
- Runfiles: every test and attribute is kept. Each new runfile keeps its original's `#:`
  directives and the `JARIBU_MULTI` guard. Support files have no entry point and are compiled once
  by the aggregator. No shared state moved, so the C-create rule is kept. The convention note in
  `co-located-jaribu-runfiles.md` matches what was done.
- Gates the reviewer ran again: `dev build` 0/0, web-jaribu-tests 458/458, web-server integration
  435 passed / 1 skipped / 0 failed.
