# Round 2 — general
**Date:** 2026-10-17
**Scope reviewed:** fix delta b9ebd97ec..HEAD plus re-verification of round-1 M1–M11

## Prior findings
| ID | Round-1 status | Verified | Notes |
|----|----------------|----------|-------|
| M1 | fixed | partial | The image half is fixed: `Images` null and null image elements are rejected (`complete-agent-chat-contracts.cs:157-162`), and there are tests for it (`complete-agent-chat-tests.cs` `Null_Images_Are_Rejected`, `Null_Image_Element_Is_Rejected`). The claim that "null turns are rejected" does not hold. The new `RuleForEach(Messages).NotNull()` (`:135-137`) adds an error, but the class-level `FitsBudget` rule (`:192`, `:234`) still runs and dereferences `turn.Role` (`:251`) on the null turn. Validation then throws a `NullReferenceException` before any message is returned. No test covers a null turn. See N1. |
| M2 | fixed | yes | `relay-chat-client.cs:137-150` caps non-picture (person) turns first, newest first, then the tool picture turns, newest first. `KeepNewest` (`:156-173`) removes the oldest images and adds `OmittedPictureText`. `Turn` is a class, so `pictureTurns.Contains` uses reference identity and is correct. Proven by `Relay_Keeps_The_Ask_Screenshot_And_The_Newest_Tool_Picture_Within_The_Cap`, which also runs the server `Validator`. |
| M3 | fixed | yes | The `#pragma warning disable BL0016` is gone. Both `InvokeAsync` overloads answer from the plain `CaptureExports` class (`catalog-agent-screenshot-tests.cs`). The delta adds no `#pragma` or `SuppressMessage`. |
| M4 | fixed | yes | `FailAsync` sets `AgentCallOutcome` to `ScreenshotRefused(error, description)` (`screenshot-state.capture-screenshot.cs:134-145`). The refusal test asserts `result.error == "not-found"` and that the description matches `Describe`. |
| M5 | fixed | partial | Ask is fixed. `AgentAsk.razor:464` dispatches the one action `CaptureForAsk`, and its handler attaches the capture it took itself (`screenshot-state.capture-for-ask.cs:38-47`). The HashSet is gone from Feedback. However, Feedback's "Capture this page" handler, `FeedbackListPage.razor:161-169`, was not touched. It still runs the multi-step flow: it dispatches `CaptureScreenshot`, compares `Latest.Id` with `before`, then uploads whatever `Latest` is. That is the racy pattern M5 named. See N2. |
| M6 | fixed | yes | 10 validator tests in `CompleteAgentChatValidator_Given_` (Jaribu `[TestTag("Unit")]` with Shouldly) cover media type, a non-user turn, the per-request and across-turns image cap, maximum length, base64, null `Images`, a null element, and a signature mismatch. |
| M7 | wontfix (budget) / fixed (magic bytes) | yes / reasonable | `MatchesSignature` (`:203-224`) decodes the first 12 base64 characters and checks the PNG or JPEG signature. It runs only after `IsBase64`, so the slice is safe. The budget wontfix is reasonable: each request is bounded at 2 images of 7,000,000 characters, the caller is authenticated, and there are per-principal concurrency and per-minute caps. A smaller cap is a cost and product decision. |
| M8 | fixed | yes | `screenshot.ts:68` runs `pending.clear()` at the start of `capture`. It adds a narrow failure when two captures overlap. See N4. |
| M9 | wontfix | reasonable | The 4096 px side limit is a documented limit in the `screenshot.ts` Design region. A selector lets an agent or a person capture part of a long page. Saying that a capture was cropped is a fair follow-up, not a defect. |
| M10 | fixed | yes | `PageBehind` is derived from `FeedbackState.DraftAttachments` by `FileName` (`FeedbackListPage.razor:30-36`). `FileName` now includes the capture id (`screenshot-capture.cs:62`). `FeedbackAttachmentNames.Normalize` keeps that name unchanged (no rejected characters, under the length cap), so the match holds after the server round trip. |
| M11 | fixed | yes | The `A.Fake<IWebServerApiService>()` registration is removed. The test file has no FakeItEasy left. |

Rule checks on the delta:
- **JS module has one caller:** yes. `ScreenshotState.CaptureAsync` (`screenshot-state.capture-screenshot.cs:97`) is the only reference to `ScreenshotJsModule.` in `source/`.
- **Razor handlers only dispatch State actions:** Ask's handler now only dispatches. Feedback's `CaptureThisPageAsync` still holds logic. See N2.
- **Purpose region on the new file:** present (`screenshot-state.capture-for-ask.cs`).
- **Kebab-case filename:** yes.
- **Test frameworks:** Jaribu and Shouldly only.
- **No new mocks of first-party code:** none added.

## Issues

### N1 — Severity: bug — Status: open
- File: source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs:234-251
- Description: A body of `{"messages":[null], ...}` still crashes validation. The new `RuleForEach(Messages).NotNull()` records an error, but FluentValidation's default class-level cascade keeps running rules. `FitsBudget` then runs `foreach (Turn turn in command.Messages) { size += turn.Role?.Length ...}` and throws a `NullReferenceException`, which surfaces as a 500. M1's disposition says null turns are rejected, and that is not yet true. The same pattern applies to null `ToolCalls` and `Tools` elements in `FitsBudget` (`:239`, `:260`). Those predate this task, but the same fix covers them.
- Suggestion: In `FitsBudget`, skip null entries (`if (turn is null) continue;`, and the same for tools and calls). Alternatively, gate the budget rule with `.When(command => command.Messages?.All(turn => turn is not null) ?? true)`. Add a `Null_Turn_Is_Rejected` validator test that asserts "A turn must not be null." and that no exception is thrown.

### N2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:161-169
- Description: `CaptureThisPageAsync` still runs the click flow that M5 removed from Ask: dispatch `CaptureScreenshot`, compare `Latest.Id` with the id taken before, then `AttachCaptureAsync(Latest)`. If an agent capture lands between the dispatch and the read, Feedback uploads the agent's capture as "this page". That capture could be one element or another route. The handler also decides and sequences two actions instead of dispatching one.
- Suggestion: Match Ask. Either add a `CaptureForFeedback` action that captures through `CaptureAsync` and records the capture it took (for example a `FeedbackCaptureId`, or a pending capture the page uploads), or have the handler read the id from an outcome the capture action sets, not from `Latest`. If TWA0022 makes the upload unable to live in that handler, record that in Feedback's Design region and make the race explicit there.

### N3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/screenshot-state/screenshot-state.capture-screenshot.cs:6-8
- Description: The Design region still says "Every capture (Ask's Screenshot button, Feedback's capture, the Ctrl-K command, the in-app model, a WebMCP agent) is this action." Ask's button is now `CaptureForAsk`, and the next lines contradict this sentence. The Purpose region of `screenshot.ts` (line 2) also still names `ScreenshotState.CaptureScreenshot` as its caller. The caller is now `CaptureAsync`, which serves both actions.
- Suggestion: Reword to "Every capture except Ask's camera button (`CaptureForAsk`) is this action; both go through `CaptureAsync`." In `screenshot.ts`, say "for ScreenshotState.CaptureAsync".

### N4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/screenshot.ts:68
- Description: `pending.clear()` at the start of `capture` assumes only one capture is in flight. The `CaptureForAsk` Design region itself says an agent capture can land during a click capture. Suppose capture A has returned its token but .NET has not yet called `takeBytes(A)`, and capture B starts in that gap. B clears A's blob, `takeBytes(A)` returns 0 bytes, and A fails as "unavailable". The window is small, and the failure is a refusal, not a wrong picture.
- Suggestion: Evict by age or count instead of clearing everything. For example, keep only the newest N tokens, or delete entries older than the previous token when a new one is set. That still bounds the leak M8 named without breaking a capture that is still waiting for `takeBytes`.
