# Web app self-screenshot (moved from 297) feeding Feedback and Ask with the page behind it

## Parent

303

## Goal (implementer restatement)

The SPA can take a PNG picture of its own page, on purpose, through one TimeWarp.State action.

- One action, `ScreenshotState.CaptureScreenshot(selector?)`, is the only caller of the capture
  JS module. It captures the page body (`.twe-page__body`, so the docked Ask panel and app chrome
  are not in it) or the element a selector names, masks every `data-no-capture` element, stores
  the PNG and its size in state, and shows a notification that a capture was taken.
- The action is a catalog action with Visibility Both, so it is a Ctrl-K command and an agent
  tool (WebMCP and in-app Ask) with no extra code path. It needs approval in Ask before editing.
  The WebMCP result returns the image as a data URL; the in-app model gets the image as image
  content, never as tool-result text.
- Ask panel: a Screenshot button captures the page behind the panel and shows a removable
  thumbnail above the composer. The next message carries it as image content (relay contract
  gains user-turn images with their own caps), then the thumbnail clears.
- Feedback form: "Capture this page" and, when a capture was taken on another page before
  opening Feedback, "Attach capture of {path}" (the page behind it). Both go through the existing
  upload action, limits and validation, and show the usual removable thumbnail.
- `page_context.screenshot` stays null until a capture of that path exists; then it holds the
  capture's metadata only (size, bytes, masked count, time, whether Ask sent it). Text first.
- Nothing is captured or uploaded without a person's click or a visible (approved) tool call.

Acceptance: Playwright proves a real non-empty PNG with width/height, that a `data-no-capture`
element is masked, and that Ask and Feedback show a removable thumbnail and send it; an
integration test proves the WebMCP tool runs the same action and returns the image. Gates
(check-version, build 0/0, test, template-smoke, repo audit) pass.

## Spec refresh 2026-10-16 (Steve approved)

This section overrides any older text below (Requirements, Checklist, Acceptance, Notes).

- **Attaching a capture to feedback is REQUIRED** (not "after 295" or "file a follow-up"). Task 295
  is done and task 352 made attachment bytes durable. The capture goes through the existing
  attachment pipeline: no parallel path, same limits, validation and storage. Pipeline today:
  - Draft attachments in state: `source/container-apps/web/projects/web-spa/features/feedback/feedback-state.cs` (`DraftAttachments`),
    `feedback-state.upload-feedback-attachment.cs`, `feedback-state.remove-feedback-attachment.cs`,
    `feedback-state.submit-feedback.cs`.
  - Upload contract and endpoint: `source/container-apps/web/features/feedback/upload-feedback-attachment/`
    (`-contracts.cs`, `-endpoint-server.cs`, `-binder-server.cs`, `-handler-application.cs`).
  - Limits and validation: `source/container-apps/web/features/feedback/feedback-attachment-rules-contracts.cs`
    (`FeedbackAttachmentRules`: MaxBytes 5 MiB, MaxPerItem 8, allowed content types incl. PNG).
  - Storage: `feedback-attachment-bytes-infrastructure.cs` (Postgres, durable per 352),
    `ef-feedback-attachment-blob-store-infrastructure.cs`, `azure-feedback-attachment-blob-store-infrastructure.cs`.
  - Existing client paste path (reference): `source/container-apps/web/projects/web-spa/source/features/feedback-paste.ts`,
    `source/container-apps/web/projects/web-spa/services/feedback-paste-js-module.cs`.
- **Ask is its own panel** (tasks 345-357 in kanban/done). Panel: `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/`
  (`AgentAsk.razor`, `AgentAsk.razor.cs`, plus `Ask*.razor` parts such as `AskTagChips.razor`,
  `AskSendButton.razor`). Prompts are sent through `source/container-apps/web/projects/web-spa/features/application/agent/ask-tag-chat-client.cs`
  (task 346 note there: the library MessageInput has no attachment hook, so attachments must be
  carried by that client/payload, see `ask-tag-payload.cs`). State: `source/container-apps/web/projects/web-spa/features/application/ask-tag-state/`.
  App-wide Ask context is task 300 (not 362; 362 is the .NET RC2 bump).
- **Ctrl-K is only the command palette** (`source/container-apps/web/projects/web-spa/features/application/command-palette/`). Wherever this task
  says "Ctrl+K (command/Ask) dialog", read instead: the capture control lives in the **Ask panel and
  the Feedback form**. The palette may offer a "Capture screenshot" command only through the normal
  action catalog (because capture is a catalog action). No command-palette UI changes.
- **`page_context.screenshot`**: parent 303 reserved it as JSON null
  (`source/container-apps/web/projects/web-spa/components/page-agent-context.cs`, `source/container-apps/web/projects/web-spa/source/features/page-surface.ts`). Fill it only on
  explicit opt-in (user action or a visible agent tool call); otherwise it stays null. Text
  `page_context` is always present and first.
- **Capture approach spike**: decide between `getDisplayMedia` and DOM-to-image and record the choice
  in Notes. web-spa ships JS as TypeScript compiled to `wwwroot/js` with no npm/Tailwind toolchain;
  prefer an npm-free, pinned approach. A third-party DOM-to-image library would have to be vendored
  (pinned file in the repo, license noted).
- **Tests (don't mock your friends)**: no handler unit test. Use integration/browser coverage in
  `tests/container-apps/web/web-spa-playwright-tests/`: (a) dispatching the capture yields a real
  non-empty PNG with width/height; (b) an element marked `data-no-capture` is masked in the capture;
  (c) the WebMCP tool invokes the same action (follow the existing `CatalogAgent_Should` pattern in
  `tests/container-apps/web/web-spa-integration-tests`); (d) the Ask panel and the Feedback form each
  show a removable thumbnail and send the attachment. Workers must NOT run `dev run` (worktree
  AppHosts share user secrets); use the test lanes.
- **Keep**: the State action is the only caller of the capture service; the WebMCP tool dispatches the
  same action; consent/indicator; `data-no-capture` masking; never auto-upload; text-first rule.
- **Gates**: `dev check-version` (bump if it says so), `dev build` 0 warnings/0 errors, `dev test`,
  `dev template-smoke` if template output changes, `ganda repo audit`.

## Description

The running web SPA should be able to capture an image of its own current UI (whole page or a chosen element) from inside the app: to attach to feedback, for agent/WebMCP use, and for bug reports. Capture is a normal TimeWarp.State action, and the same action is exposed as a WebMCP tool (one code path). In the Ask panel and the Feedback form, a captured screenshot shows up next to the user's input as a removable attachment.

Builds on task 295 (done) and 352: captures are attached to feedback through the existing attachment pipeline.

## Requirements

- Choose a capture approach and record the trade-offs: Screen Capture API (`getDisplayMedia`, needs user consent and a picker, pixel-exact) vs DOM-to-image rendering (e.g. `html-to-image`; no prompt, but limits on cross-origin images, canvas, iframes and fonts). Must work in Blazor WASM via JS interop.
- A reusable JS module plus a C# service wrapper. Captures the whole page or a chosen element (selector/ElementReference) and returns PNG bytes/data URL with dimensions.
- Taking a screenshot is a standard TimeWarp.State action with a normal handler (e.g. `CaptureScreenshot` action). The result goes into state. Nothing calls the capture service outside that action.
- The same State action is exposed as a regular WebMCP tool (e.g. `capture_screenshot`, optional element/selector argument) that dispatches the action. It is not a separate code path, so WebMCP parity follows.
- Ask panel and Feedback form: the user can take a screenshot from either. The captured image shows alongside the user's input as an attachment (thumbnail, remove button) and is sent with the prompt or feedback.
- Privacy/consent: capture only on explicit user action or an agent tool call the user can see. Support masking of sensitive elements (e.g. `data-no-capture`). Never upload automatically. Show a clear indicator when a capture is taken.
- Feedback form: an "attach screenshot" control that uses the same action and the existing feedback attachment pipeline (required).

- **Requirement 5 (Steven, 2026-10-11 01:22 ICT): the Feedback form and the Ask panel consume
  this capture to show the page behind them.** When Feedback (or Ask) is opened over a page, the
  user can attach a capture of the page behind the dialog/panel (not the dialog itself), shown as
  a removable thumbnail and sent with the feedback item / prompt. Capture stays explicit (user
  action or a visible agent tool call), with masking (`data-no-capture`).
- **Text first (parent 303 rule).** A screenshot is only an optional supplement to the text
  `page_context` that parent task 303 builds; it never replaces it. Where 303 reserved an opt-in
  image field/flag on `page_context`, wire this capture into it (opt-in only).

## Checklist

- [ ] Spike both approaches; write the decision and trade-offs in Notes
- [ ] JS capture module + C# service (whole page and element)
- [ ] TimeWarp.State `CaptureScreenshot` action + handler; result in state
- [ ] WebMCP tool dispatching the same action
- [ ] Ask panel: capture button, removable thumbnail next to input, attachment sent with the prompt
- [ ] Optional "Capture screenshot" palette command via the normal action catalog only (no palette UI changes)
- [ ] Consent/indicator and sensitive-element masking
- [ ] Playwright: dispatch capture returns a real non-empty PNG with dimensions
- [ ] Playwright: `data-no-capture` element is masked
- [ ] WebMCP tool invokes the same action (CatalogAgent pattern)
- [ ] Playwright: Ask panel and Feedback form show a removable thumbnail and send the attachment
- [ ] Feedback form and Ask panel: attach a capture of the page behind them (requirement 5)
- [ ] Fill `page_context.screenshot` only on explicit opt-in; else null; text first
- [ ] REQUIRED: attach a capture to feedback through the existing attachment pipeline (same limits, validation, storage; no parallel path)
- [ ] Gates: dev check-version, dev build 0/0, dev test, dev template-smoke if template output changes, ganda repo audit

## Acceptance criteria

- Dispatching the State action captures the page or an element and stores the image in state.
- Calling the WebMCP tool runs that same action and returns the image.
- In the Ask panel and Feedback form, a screenshot appears as a removable thumbnail and is sent with the prompt/feedback.
- Masked elements are not visible in captures. Nothing is uploaded without a user action.

## Notes

- Filed from Steven's voice call, 2026-10-10 as task 297. Moved under parent 303 as 303-001 on
  2026-10-11 (Steven: split the WebMCP overhaul into parent 303 + this child). 297 is archived
  with a pointer here.
- Parent 303's implementation merged in PR #461 (page_context shape, WebMCP tool list); the explicit Depends-on line was removed 2026-10-16 because ganda claim did not recognise that merge.
- Related: 295 (done), 352 (done), 271/292 (Cloudflare-style Ask AI, WebMCP parity).
- **Capture approach (spike decision, 2026-10-16): DOM-to-image, written in the repo's own
  TypeScript (`source/features/screenshot.ts`), no vendored library.**
  - `getDisplayMedia` rejected: a browser picker on every capture, the person must pick the tab,
    the picture includes the Ask panel or dialog the capture is meant to leave out, element
    capture needs cropping by guessed offsets, and headless tests need Chrome flags to auto-pick.
    It is pixel-exact, which is its only win here.
  - Vendoring `html-to-image` rejected: a pinned third-party file and its license for something
    the app can do in ~300 lines, and FluentUI v5 web components need open-shadow-root flattening
    the module has to get right for this app anyway.
  - How it works: clone the target, flatten open shadow roots (slots take their assigned nodes),
    inline each element's computed style, copy form values, replace `data-no-capture` elements
    with a solid box of the same size, turn same-origin images and canvases into data URLs, wrap
    the result in an SVG `foreignObject`, draw it on a canvas, and export PNG. It scales down
    until the PNG fits `FeedbackAttachmentRules.MaxBytes`.
  - Known limits (accepted): web fonts fall back to system fonts, cross-origin images and CSS
    `url()` backgrounds render empty, `::before`/`::after` content and closed shadow roots are not
    drawn, and iframes are blank. Safari taints SVG-foreignObject canvases, so capture reports an
    error there instead of a picture.
  - The bytes reach .NET as a JS stream reference (same reason as feedback paste: InteractiveServer's
    SignalR message cap).
- **Feedback is a route, not a dialog.** "The page behind it" is the latest capture taken on the
  page the person came from (Ask's Screenshot button or the Ctrl-K "Capture screenshot" command),
  offered as "Attach capture of {path}". "Capture this page" captures the Feedback page itself.

## Session

- Created: 269751 (2026-10-11 ICT), carried over from 297 (created 3467057, 2026-10-10)
