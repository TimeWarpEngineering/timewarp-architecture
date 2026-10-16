# Round 2 — merged findings
**Date:** 2026-10-17
**Sources:** general (re-review of the fix delta b9ebd97ec..ee91a4014 and of round-1 M1–M11)

## Counts (final, after the round-2 fix loop)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 4 | 1 |
| nit | 0 | 5 | 1 |

These counts cover round 1 (M1–M11) and round 2 (N1–N4). M1 and N1 are counted once each, M5 and N2 are counted once each, and M7 is counted as one wontfix suggestion.

## Prior findings (carried)

- M1 was partly fixed. Its null-turn crash is tracked as N1, now fixed.
- M5 was partly fixed. Feedback's half is tracked as N2, now fixed.
- M2, M3, M4, M6, M8, M10 and M11 were verified fixed. The magic-byte part of M7 was verified fixed.
- The reviewer agreed with the M7 budget wontfix and the M9 crop wontfix.

## Issues

### N1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs
- Description: `FitsBudget` read `turn.Role` on a null turn and threw, which returned a 500.
- Source: general
- Disposition notes: The budget rule now skips null tools, turns and tool calls, and each of those gets its own not-null error. New tests `Null_Turn_Is_Rejected`, `Null_Tool_Is_Rejected` and `Null_Tool_Call_Is_Rejected` call the reject helper inside `Should.NotThrow`. Fixed in e03d334c9.

### N2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor
- Description: "Capture this page" read `Latest`, so an agent capture taken during the click could be uploaded as "this page".
- Source: general
- Disposition notes: The button now dispatches `CaptureForFeedback`. Only that action sets `ForFeedback`, and the page uploads `ForFeedback` through the existing `UploadOneAsync` → `UploadFeedbackAttachment` path, so the race is gone. The click still sends two actions in a row, capture and then upload. The platform Applications tier cannot reach Feedback's upload, and a handler never sends another action. This is recorded in the FeedbackListPage Design region. Fixed in aae4b0de7.

### N3 — Severity: nit — Status: fixed
- File: screenshot-state.capture-screenshot.cs, screenshot.ts, and others
- Description: The Design text was stale after CaptureForAsk was added.
- Disposition notes: The text now names CaptureForAsk, CaptureForFeedback and the shared CaptureAsync. Fixed in aae4b0de7.

### N4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/screenshot.ts
- Description: The blanket `pending.clear()` broke a capture that overlapped another.
- Disposition notes: The module now keeps the newest `MaxPending = 3` blobs and drops older ones. Fixed in aae4b0de7.

## Duplicates / conflicts

- N1 finishes M1, and N2 finishes M5.
