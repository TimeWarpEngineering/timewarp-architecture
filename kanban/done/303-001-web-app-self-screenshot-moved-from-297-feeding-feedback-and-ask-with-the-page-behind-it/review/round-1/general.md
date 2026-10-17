# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** branch task/303-001-web-app-self-screenshot-moved-from-297-feeding-fee vs master (git diff master...HEAD)

## Summary

The implementation matches the spec refresh closely. `CaptureScreenshot` is the only caller of
`ScreenshotJsModule`. WebMCP and the in-app agent reach it through the catalog with approval.
`data-no-capture` and password inputs are masked. `page_context.screenshot` stays null until a
capture of that path exists and then holds metadata only. Feedback reuses `UploadOneAsync` →
`UploadFeedbackAttachment`. The tests prove the stated claims: real PNG IHDR size, the mask
checked by counting pixels, Ask remove and send checked against the recorded request, Feedback
attach, remove and file, and WebMCP parity through the real dispatcher and approval gate.
Two correctness bugs are in the new relay image path: a null `images` field crashes the server
mapping, and the client can build a request over the 2-image cap inside one Ask turn. One new
analyzer pragma was added in a test.

## Restatement vs task

- **"The page behind" Feedback is a reinterpretation.** Requirement 5 says that when Feedback is
  opened over a page, the person can attach a capture of the page behind it. Feedback is a route,
  not a dialog. So the restatement (and Notes) narrows this to "the latest capture taken on
  another route", offered as "Attach capture of {path}". If the person goes to Feedback without
  first capturing (with Ask's camera button or the Ctrl-K command), they cannot attach the page
  they came from. That page is no longer in the DOM. This is a defensible reading, but it is a
  product decision the spec did not make. Steve should confirm it, or ask for an entry point that
  captures before it navigates (for example a "Send feedback about this page" action on the
  shell).
- **Approval wording.** The restatement says agent captures need approval "in Ask before editing".
  In `AgentEditMode.AutomaticallyEdit`, `CatalogAgentApproval.RequiresApproval` returns false for
  every tool. An agent then captures with no prompt, and the only visible sign is the "Screenshot
  captured" notification. The spec's bar is "an agent tool call the user can see", so this
  probably passes. The task still does not say explicitly that the notification counts as consent
  when auto-edit is on.
- **ElementReference dropped.** The Requirements ask for "selector/ElementReference". Only a CSS
  selector is supported. That is fine for the agent and palette paths, which cannot pass an
  ElementReference. This is minor.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:441 (and complete-agent-chat-contracts.cs:147-160)
- Description: The server-side image validation does not handle nulls. The rule `item.Images is not { Count: > 0 } || ...` passes `"images": null`. The count rule uses `turn.Images?.Count ?? 0`. Then `XaiChatMapping.ToMessages` runs `foreach (TurnImage image in turn.Images)` and throws a NullReferenceException, which returns a 500. An element that is `null` (`"images":[null]`) also gets past `RuleForEach(...).ChildRules`, because FluentValidation skips null children. The mapping then dereferences `image.Data`. Any authenticated client can send either body. The existing `ToolCalls` rule rejects null (`calls?.Count <= Max` is false for null), but `Images` has no matching rule.
- Suggestion: Add `turn.RuleFor(item => item.Images).NotNull()` and `turn.RuleForEach(item => item.Images).NotNull()` (or handle `turn.Images ?? []` in the mapping). Add validator cases for them (see Issue 6).
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/relay-chat-client.cs:99-125
- Description: `ToTurns` collects every tool picture after the person's last message, and nothing limits how many. `AskScreenshotChatClient` sits outside `FunctionInvokingChatClient`, so the Ask attachment is sent again on every iteration of the tool loop. If a turn carries the Ask screenshot and the model calls `Screenshot.CaptureScreenshot` twice (once for the page, then for an element), the request has 3 images. A turn with no Ask screenshot and three capture calls also has 3. The server rejects it (`MaxImagesPerRequest = 2`, contract line 172), and the turn ends with "The model request failed." The cap is enforced only by the server, which is correct, but the client has no way to stay under it.
- Suggestion: In `ToTurns`, keep only the newest pictures that fit `MaxImagesPerRequest`, counting user-attached images first. Replace the dropped ones with a short text note such as "earlier picture omitted". Add a relay test with two tool pictures plus an Ask screenshot that asserts the command validates.
- Status: open

### Issue 3 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-screenshot-tests.cs:225-228
- Description: The new code adds `#pragma warning disable BL0016` to silence an analyzer in the `CaptureJsRuntime` test double. The review rule is no new suppressions. The same pragma already appears in 13 test files for IJSRuntime doubles, so this follows a repo pattern rather than starting one.
- Suggestion: Remove it, for example by implementing the two-argument overload without delegating, or by sharing one existing JS-runtime test double. Otherwise record the BL0016-in-test-doubles exception as an explicit reviewed decision so it stops coming up.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/screenshot-state/screenshot-state.capture-screenshot.cs:104
- Description: On refusal (`not-found`, `invalid-selector`, `too-large`, `tainted`, `unavailable`), `FailAsync` sets `LastError` and notifies, but it does not set `AgentCallOutcome`. The WebMCP dispatcher therefore returns `{"completed":true}` with no result, and the in-app model gets the same. An agent cannot tell that its selector matched nothing, and it may retry or claim it saw the page. The refusal test only checks that `"image"` is absent.
- Suggestion: Set an outcome on failure, for example `ScreenshotRefused(error, Describe(error))`, and assert it in `WebMcp_Capture_Screenshot_Refusal_Keeps_The_Last_Capture_Out`.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:459 ; source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:15,160
- Description: The click handlers coordinate several steps inside the component. `CaptureAsync` reads `Latest.Id`, dispatches `CaptureScreenshot`, compares ids, then dispatches `AttachToAsk`. Feedback keeps a page-local `HashSet<int> AttachedCaptures` that decides whether the page-behind button shows. The "all user interactions are State actions" rule prefers a handler to own this flow. The id comparison can also pick up a capture an agent took at the same moment.
- Suggestion: Add a `CaptureForAsk` action, a capture with an attach flag, or have the handler return the new id. Keep "already attached" in `FeedbackState` (for example a source capture id on `DraftAttachment`) rather than in page fields.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs:147-174
- Description: None of the new server-side image rules has a test: images on a non-user turn, more than 2 images, a media type outside the allowlist, data over `MaxImageDataLength`, data that is not base64, and null images (Issue 1). The only validator check is `Validate(...).IsValid.ShouldBeTrue()` on well-formed relay commands. The task asks for server-side enforcement, so the rejection paths should be proven.
- Suggestion: Add rejection cases next to the existing CompleteAgentChat contract and validator tests.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs:40-43
- Description: Images are outside `MaxRequestCharacters`, which the Design region calls the anti-proxy budget. Before this change a request topped out at about 256K characters. It can now carry about 14 MB of base64 (2 × 7,000,000), 30 times a minute per principal, so any authenticated client can relay arbitrary images to xAI. The server also does not check that the bytes match the declared `MediaType` (PNG or JPEG magic bytes).
- Suggestion: Check the magic bytes against `MediaType`. Consider a per-request byte budget for images, or a lower per-principal image rate, and record the decision in the Design region.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/screenshot.ts:56,99-100
- Description: `pending` is module-level, and the browser keeps the ES module cached across `import()` calls. The PNG Blob (up to 5 MiB) leaks for the session whenever `takeBytes` never runs, for example when the .NET side throws or is cancelled between `capture` and `takeBytes`, or a circuit drops.
- Suggestion: Clear `pending` at the start of each `capture` (only one capture is ever pending), or let entries expire.
- Status: open

### Issue 9 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/screenshot.ts:85-86,318-324
- Description: Width and height are clamped to `MaxSide` (4096), and `pinRoot` sets `overflow:hidden`. A page body taller than 4096 px is cut off without any sign. The notification, `ScreenshotCaptured` and `page_context.screenshot` do not say the picture is partial.
- Suggestion: Return a `cropped` flag and show it in the outcome and notification, or scale to fit instead of cropping.
- Status: open

### Issue 10 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/feedback/pages/FeedbackListPage.razor:174-178
- Description: `AttachedCaptures` gets the capture id on upload, and removing that draft attachment never takes the id out. If the person removes the page-behind attachment, the "Attach capture of {path}" button does not come back until they leave and reopen the page.
- Suggestion: Base the check on the current `DraftAttachments` (this ties in with Issue 5), or remove the id when the attachment is removed.
- Status: open

### Issue 11 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-screenshot-tests.cs:298
- Description: `CaptureSpa` registers `FakeItEasy.A.Fake<IWebServerApiService>()`, a fake of first-party code ("don't mock your friends"). One other test does the same. It appears to exist only to satisfy DI.
- Suggestion: Register the real service against a recording or unused HTTP handler, or leave it out if nothing resolves it.
- Status: open
