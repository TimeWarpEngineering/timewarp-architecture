# Round 2 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-attachment-tests.cs:272
- Description: The Purpose region named a test that lived in the other attachment runfile.
- Disposition notes: Re-verified in round 2. The test is back in `feedback-attachment-tests.cs`,
  and both Purpose regions match their tests. Counts are 14 + 8 = 22.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-triage-world-tests.cs:95
- Description: `TriageWorld.AddAsync` had its own copy of the `Filed` helper.
- Disposition notes: Re-verified in round 2. `TriageWorld.Filed` is the one helper, and
  `AddAsync` and the triage tests call it. The old helper and its unused alias are gone.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/agent-chat/user-xai-api-key-harness-tests.cs:51
- Description: The `SiteSetter` GUID had the same value as the test file's `UserA`.
- Disposition notes: Re-verified in round 2. `XaiKeyTestHarness.UserA` holds the GUID, and the
  test file reads it.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/identity/credential-rules-application.cs:7
- Description: Design notes in four files still named `EntraTicketProcessor` for rules that moved.
- Disposition notes: Re-verified in round 2. The notes name `EntraAccountLinker`
  (HoldsMicrosoft365) and `EntraSignInStamp` (the stamp). The stamp-caller list in
  `credential-usage-recorder.cs` matches the call sites in the processor and the linker.

## New findings

None. The fix delta changes only test code and comments.

## Duplicates / conflicts

- None.
