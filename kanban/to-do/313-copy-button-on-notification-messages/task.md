# Copy button on notification messages

## Description

Steve, 2026-10-12: "a copy icon on each message would be nice so message could be easily copied to
the clipboard". Each row in the shell's notification region
(`web-spa/components/MessageBars.razor`, the single `FluentMessageBar` host enforced by TWA0025)
gets a small copy-to-clipboard button. It copies the message's Title plus Body as plain text.

## Rules

- **Every user interaction is a TimeWarp.State action.** The button only dispatches, for example a
  `NotificationState.CopyMessage(id)` action set. The handler does the clipboard JS interop. TWA0026
  forbids direct `IJSRuntime` calls in components; never add a
  `[DirectComponentSideEffect]` opt-out for this.
- JS interop lives in the existing typed `*JsModule` pattern (`[SideEffectService]`). The
  `window.Spa` namespace must be plain objects (see the existing modules). Use
  `navigator.clipboard.writeText`.
- FluentUI v5 icon button (copy icon) with an accessible label ("Copy message") and a brief "Copied"
  confirmation. A tooltip, an icon swap, or a short-lived Success notification are all fine; pick
  one and record it. Styling goes in `MessageBars.razor.css` with tokens, no `<style>` element
  (`tw-blazor-css-strategy`).
- Clipboard failure (permissions, insecure context) reports through `NotificationState` as a
  warning, never throws.

## Requirements

- Copy button on every row (Error, Warning, Info, Success), next to Dismiss.
- Copied text: `Title`, plus a blank line and `Body` when there is a body.
- Tests:
  - handler and state tests: the copied text format, and a failure report on a clipboard
    exception, using a fake clipboard module (an externality, so mocking it is fine);
  - a component test that the button dispatches the action. TWA0026 stays clean.

## Checklist

- [ ] `CopyMessage` action + handler + clipboard JsModule
- [ ] Copy icon button in `MessageBars.razor` with accessible label and copied feedback
- [ ] Tests (format, failure path, dispatch)
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve)
