# Copy icon on every message bar

## Description

Steve, 2026-10-29: every message bar should have a copy icon that copies the bar's text to the clipboard.

Shell notification rows already do this (done task 313): `MessageBars.razor` puts an icon-only Copy button on each `NotificationState` row, and `CopyMessage` writes the text through `ClipboardJsModule`. This task gives that same control to every other rendered `FluentMessageBar`.

## Requirements

- Each rendered message bar has an icon-only Copy button with accessible name "Copy message".
- Copied text matches task 313: the title, or the title, a blank line, and the body when the bar has body text. Copy the message text. Leave out button labels. On "Name this passkey", copy the title and leave the nickname draft out.
- The button only dispatches a TimeWarp.State action. The handler owns clipboard interop and reuses `ClipboardJsModule`. TWA0026 stays clean: no `IJSRuntime` in a component and no `[DirectComponentSideEffect]` opt-out.
- A successful copy swaps the icon to a check labelled "Copied" for the same short interval task 313 uses. A clipboard failure reports a Warning through `NotificationState` ("Couldn't copy the message.") and does not throw.
- Bars that already have actions keep those actions. Copy sits with them. That covers both bars in `AddPasskeyPrompt.razor` (Save / Skip, and Add a passkey / Later).
- One shared control paints the button, and the shell host uses it too, so a new bar gets the icon from that control. A convention analyzer fails the build when a product `FluentMessageBar` is rendered outside that control and outside the shell host. Record the new diagnostic id in `AnalyzerReleases.Unshipped.md`. Analyzer test fixtures that stub `FluentMessageBar` stay exempt.
- Update the design-system "Message bars" usage snippet when the markup authors copy changes. That snippet is a `<pre>` on the single-part view (`DesignSystemParts`), not a second live bar.

### Bars still missing the icon

- `features/admin/site-settings/pages/AuthenticationPage.razor` — "Configuration drift"
- `features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor` — "Session expired"
- `features/identity/components/AddPasskeyPrompt.razor` — "Name this passkey" and "Add a passkey"
- `features/design-system/samples/MessageBarsSample.razor` — Success, Error, Warning, and Information samples

Leave these as page-local bars. TWA0025 already allows static Info and Warning guidance outside the shell host. Moving them into `NotificationState` is out of scope.

## Checklist

- [ ] Shared copy control used by the shell host and every other message bar
- [ ] Copy icon on the four call sites listed above, including both passkey bars
- [ ] Analyzer so a new product `FluentMessageBar` cannot skip the control
- [ ] Tests: copied text, failure Warning, and that each new button dispatches the action
- [ ] `dev build` 0 warnings / 0 errors and `ganda repo audit` pass

## Session

- Created: 01a18418-875e-7d10-9d0a-d9d22584627c (2026-10-28)
- Spec: 01a18418-875e-7d10-9d0a-d9d22584627c (2026-10-29)

## Notes

Task 313 format and confirmation live in `NotificationState.ClipboardText`, `CopiedConfirmationInterval`, and `MessageBars.razor`. Reuse them.

## How to validate

**Smoke**

1. `dev run`, then raise a shell notification, the Authentication "Configuration drift" bar, the Microsoft 365 "Session expired" bar, both passkey prompts, and Design System → Message bars.
2. On each bar, activate Copy.

**Expect**

- Clipboard text is the title, plus a blank line and the body when the bar has body text.
- The icon reads "Copied", then returns to "Copy message".
- Passkey Save, Skip, Add a passkey, and Later still work.
- Shell bars from task 313 still copy.

**Automated gate**

```bash
dev build
dev test --filter-class CopyMessage_Should_
```

Add the new runfile class name to that filter once it exists.
