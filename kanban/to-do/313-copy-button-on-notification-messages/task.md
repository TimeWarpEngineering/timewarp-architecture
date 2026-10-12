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

- [x] `CopyMessage` action + handler + clipboard JsModule
- [x] Copy icon button in `MessageBars.razor` with accessible label and copied feedback
- [x] Tests (format, failure path, dispatch)
- [x] Gates: `dev build` 0/0, `ganda repo audit` pass; `dev test` has one failure that also
  fails on master (see Results)

## Session

- Created: 2026-10-12 (cockpit, per Steve)
- 2026-10-12: implemented under `ganda task work` (implement oracle, Claude).

## Results

- **Action:** `NotificationState.CopyMessageActionSet` (`notification-state.copy-message.cs`).
  The handler looks up the row by id, writes `ClipboardText(message)` through the new
  `[SideEffectService] ClipboardJsModule` (`services/clipboard-js-module.cs` imports
  `source/features/clipboard.ts`, export `WriteText`, using `navigator.clipboard.writeText`). An
  unknown id does nothing. A `JSException` (permission denied or insecure context) adds a
  Warning bar ("Couldn't copy the message."). Nothing throws. The module swallows a dropped
  Server circuit.
- **Format:** `Title`, or `Title` + `\n\n` + `Body` when there is a body
  (`NotificationState.ClipboardText`).
- **Button:** each `MessageBars` row has an icon-only subtle `FluentButton` before Dismiss. It
  uses the Copy icon with `aria-label`/`Title` "Copy message" and `data-qa="MessageBarCopy"`, and
  it only dispatches `NotificationState.CopyMessage(id)`. There is no `IJSRuntime` in the
  component and no `[DirectComponentSideEffect]`, so TWA0026 stays clean.
- **Confirmation (choice recorded):** the icon swaps to a Checkmark labelled "Copied" for
  `CopiedConfirmationInterval` (2s). A Success bar was rejected because it could push the error
  being copied behind "+N more". `MarkCopied` stamps `CopiedUntil`. `NextExpiry` includes it,
  so the host's existing `ExpireMessages` timer ends the swap and no second timer is needed.
  Dismiss, navigation clear, and `Initialize` also reset it.
- **CSS:** no change to `MessageBars.razor.css`. The `fluent-message-bar` actions slot already
  spaces Copy and Dismiss.
- **Tests:** `tests/container-apps/web/web-spa-integration-tests/features/notification/notification-copy-tests.cs`
  has 6 tests, all passing. They cover title-only format, title+body format, a write that
  confirms and then expires, the Warning on a clipboard `JSException`, an unknown id, and the
  Copy button dispatching `CopyMessage`. The dispatch test uses a minimal `Renderer` that fires
  the button's onclick handler and checks that the fake clipboard received the text. A fresh
  render then shows the "Copied" label.
- **Gates:** `dev build` 0 warnings / 0 errors. `ganda repo audit` passes. `dev test`: every
  project passes except one aspire-tests case,
  `IngressSmoke_Given_.RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
  (401 instead of 403). It also fails on a clean detached checkout of origin/master `4f4e59766`,
  so this task did not cause it. It depends on the mock-auth host config
  (`Authentication:UseMock`) and is unrelated to the SPA notification region.
- `dev test` rewrites Playwright screenshots under `kanban/done/*`. They were restored and not
  committed.

### How to validate

- **Smoke:** `./tests/container-apps/web/web-spa-integration-tests/bin/Release/net11.0/web-spa-integration-tests --filter-class CopyMessage_Should_`
  (after `dev build`). Manually, in `dev run`, cause any notification (for example a failed
  action), then click the copy icon on the bar and paste.
- **Expect:** 6 passed, 0 failed. In the browser the icon becomes a checkmark labelled "Copied"
  for about 2s, and the pasted text is the bar title, a blank line, then the body. With
  clipboard permission blocked, a Warning bar "Couldn't copy the message." appears and nothing
  throws.
