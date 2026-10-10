# Web app can take a screenshot of itself (self-capture of the running UI)

## Description

The running web SPA should be able to capture an image of its own current UI (whole page or a chosen element) from inside the app: to attach to feedback, for agent/WebMCP use, and for bug reports. Capture is a normal TimeWarp.State action, and the same action is exposed as a WebMCP tool (one code path). In the Ctrl+K (command/Ask) dialog, a captured screenshot shows up next to the user's input, attached to the prompt.

Pairs with task 295 (feedback image paste and file upload): once 295 lands, captured screenshots should be attachable to feedback.

## Requirements

- Choose a capture approach and record the trade-offs: Screen Capture API (`getDisplayMedia`, needs user consent and a picker, pixel-exact) vs DOM-to-image rendering (e.g. `html-to-image`; no prompt, but limits on cross-origin images, canvas, iframes and fonts). Must work in Blazor WASM via JS interop.
- A reusable JS module plus a C# service wrapper. Captures the whole page or a chosen element (selector/ElementReference) and returns PNG bytes/data URL with dimensions.
- Taking a screenshot is a standard TimeWarp.State action with a normal handler (e.g. `CaptureScreenshot` action). The result goes into state. Nothing calls the capture service outside that action.
- The same State action is exposed as a regular WebMCP tool (e.g. `capture_screenshot`, optional element/selector argument) that dispatches the action. It is not a separate code path, so WebMCP parity follows.
- Ctrl+K (command/Ask) interface: the user can take a screenshot from the dialog. The captured image shows in the dialog alongside the user's input as an attachment to the prompt (thumbnail, remove button), and is sent with the prompt.
- Privacy/consent: capture only on explicit user action or an agent tool call the user can see. Support masking of sensitive elements (e.g. `data-no-capture`). Never upload automatically. Show a clear indicator when a capture is taken.
- Once 295 lands: an "attach screenshot" path on the feedback form that uses the same action.

## Checklist

- [ ] Spike both approaches; write the decision and trade-offs in Notes
- [ ] JS capture module + C# service (whole page and element)
- [ ] TimeWarp.State `CaptureScreenshot` action + handler; result in state
- [ ] WebMCP tool dispatching the same action
- [ ] Ctrl+K dialog: capture button, thumbnail next to input, attached to prompt, removable
- [ ] Consent/indicator and sensitive-element masking
- [ ] Tests: handler unit test, WebMCP tool test, Ctrl+K attachment UI/e2e test
- [ ] Hook into feedback attachments after 295 merges (or file follow-up)

## Acceptance criteria

- Dispatching the State action captures the page or an element and stores the image in state.
- Calling the WebMCP tool runs that same action and returns the image.
- In Ctrl+K, a screenshot appears next to the input and goes with the prompt.
- Masked elements are not visible in captures. Nothing is uploaded without a user action.

## Notes

- 2026-10-11: superseded. Moved under parent 303 as child task **303-001** (Steven split the
  WebMCP overhaul into parent + child). Work happens on 303-001; do not launch 297.

- Filed from Steven's voice call, 2026-10-10. Not launched.
- Related: 295 (feedback image paste/file upload), 271/292 (Cloudflare-style Ask AI, WebMCP parity).

## Session

- Created: 3467057 (2026-10-10)
