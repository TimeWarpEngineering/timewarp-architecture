# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 3 | 1 |
| nit | 0 | 3 | 1 |

M7 is split: the magic-byte part is fixed, and the image byte and rate budget is wontfix. It is counted once, as a wontfix suggestion.

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs
- Description: `Images: null` or a null image element got past validation, and the server mapping then crashed with a 500.
- Source: general (Issue 1)
- Disposition notes: The validator now rejects null turns, null `Images` and null elements. Fixed in c1ac91125, with tests in `CompleteAgentChatValidator_Given_`.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs
- Description: The client could build a request over the server's 2-image cap.
- Source: general (Issue 2)
- Disposition notes: `KeepNewest` counts the person's images first, then the newest tool pictures that still fit. Each dropped picture becomes the omitted-picture text. Fixed in a277ed678, test in ee91a4014.

### M3 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-screenshot-tests.cs
- Description: A new `#pragma warning disable BL0016` silenced an analyzer.
- Source: general (Issue 3)
- Disposition notes: The pragma is removed. The fake browser's answers moved to a plain nested class that does not implement IJSRuntime. Fixed in ee91a4014.

### M4 — Severity: suggestion — Status: fixed
- File: .../screenshot-state/screenshot-state.capture-screenshot.cs
- Description: When a capture failed, the agent got "completed" with no result.
- Source: general (Issue 4)
- Disposition notes: A failed capture now returns the `ScreenshotRefused(Error, Description)` outcome, and the refusal test asserts it. Fixed in f7e622e1b and ee91a4014.

### M5 — Severity: suggestion — Status: fixed
- File: AgentAsk.razor, FeedbackListPage.razor
- Description: The click handlers ran a multi-step flow themselves, used a racy `Latest.Id` comparison, and kept a page-local HashSet.
- Source: general (Issue 5)
- Disposition notes: One `CaptureForAsk` action now captures and attaches the capture it took. The capture code is shared through `ScreenshotState.CaptureAsync`, which is still the only caller of the JS module. Sending another action from a handler is banned (TWA0022). Feedback decides whether to offer the capture from `DraftAttachments`. Fixed in f7e622e1b.

### M6 — Severity: suggestion — Status: fixed
- File: complete-agent-chat-contracts.cs
- Description: The server-side image rules had no rejection tests.
- Source: general (Issue 6)
- Disposition notes: There are now 10 validator tests in `complete-agent-chat-tests.cs`. Fixed in c1ac91125.

### M7 — Severity: suggestion — Status: wontfix (budget) / fixed (magic bytes)
- File: complete-agent-chat-contracts.cs
- Description: Images sit outside the character budget, so a request can carry about 14 MB. The bytes were not checked against `MediaType`.
- Source: general (Issue 7)
- Disposition notes: Magic bytes: fixed in c1ac91125 (`MatchesSignature`). Budget: wontfix (decided by the review orchestrator). The relay is authenticated, rate-limited per principal, and capped at 2 images × 7,000,000 base64 characters, so each request is bounded. A smaller image budget is a product or cost decision for Steve, not a defect in this change.

### M8 — Severity: nit — Status: fixed
- File: screenshot.ts
- Description: A pending PNG blob could leak for the rest of the session.
- Source: general (Issue 8)
- Disposition notes: Each capture now starts with `pending.clear()`. Fixed in f7e622e1b.

### M9 — Severity: nit — Status: wontfix
- File: screenshot.ts
- Description: A page taller than 4096 px is cropped and nothing says so.
- Source: general (Issue 9)
- Disposition notes: Wontfix (decided by the review orchestrator). The 4096 px side limit is one of the capture limits the spike decision accepted. To capture part of a long page, an agent or a person can pass a selector. Telling the person a capture was cropped can be a follow-up if Steve wants it.

### M10 — Severity: nit — Status: fixed
- File: FeedbackListPage.razor
- Description: After the page-behind attachment was removed, its button did not come back.
- Source: general (Issue 10)
- Disposition notes: The offer is now derived from the draft attachment file names, which include the capture id. Fixed in f7e622e1b.

### M11 — Severity: nit — Status: fixed
- File: catalog-agent-screenshot-tests.cs
- Description: The test registered a FakeItEasy fake of first-party `IWebServerApiService`.
- Source: general (Issue 11)
- Disposition notes: The fake is removed because nothing resolved it. Fixed in ee91a4014.

## Duplicates / conflicts

- None. Issue 10 was fixed together with Issue 5.
