# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** commit 9bdfe4cad (task/445 branch vs master), product + tests + skills

## Summary

The change adds one shared `MessageBar` control that paints the icon-only Copy button, routes the shell host and the four page-local call sites through it, and adds `NotificationState.CopyMessageBar` plus the TWA0037 analyzer so a raw `FluentMessageBar` cannot come back. Every requirement bullet is met in code, and I found no behavioral bug in the control, the state changes, or the call sites. The remaining findings are one enforcement hole in TWA0037 (and a Design comment that overstates it), missing test coverage for the changed `RemoveExpired` branch, and two nits.

## Issues

### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/direct-fluent-message-bar-analyzer.cs:17-22, 144-170; tests/analyzers/timewarp-architecture-analyzers-tests/direct-fluent-message-bar-analyzer-tests.cs:184-190
- Description: The host exemption is a suffix match (`EndsWithSegment(normalized, "components/MessageBar.razor", '/')` and the `_`-flattened form), so any `.../components/MessageBar.razor` or `.../components/MessageBars.razor` is exempt, not only the web-spa root `components/` folder. A feature folder such as `features/identity/components/MessageBar.razor` (the repo already has `features/identity/components/AddPasskeyPrompt.razor`) can render a raw `FluentMessageBar` with no TWA0037. The flattened hint `features_identity_components_MessageBar_razor.g.cs` also matches, because the `_` branch accepts `_` or `/` before the suffix. The Design comment says "a same-named file in another folder is still reported", which is only true when the other folder is not itself named `components`. The only "other folder" test uses `features/notes/` (test line 186), so the hole is untested. TWA0032 has the same suffix shape, so this is inherited rather than new.
- Suggestion: Either tighten the match (for example, also require the containing type's namespace to end in `.Components` and the type name to be `MessageBar` or `MessageBars`, instead of path alone), or narrow the Design comment and the test-file Design comment to say "same-named file outside a `components/` folder", and add a `features/x/components/MessageBar_razor.g.cs` case that records the intended behavior.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.cs:157-166; tests/container-apps/web/web-spa-integration-tests/features/notification/message-bar-copy-tests.cs:65-76
- Description: `RemoveExpired` was rewritten (`copiedRowExpired` computed before `RemoveAll`, then `copiedRowExpired || CopiedUntil <= now`). Walking the scenarios: shell row copied then dismissed is handled by `RemoveMessage` (line 148-155); a copied shell Success row that auto-expires clears through `copiedRowExpired`; a page-local bar copied while an unrelated Success row expires keeps its confirmation; and `CopiedUntil <= now` clears. The logic is correct, but no test reaches the `copiedRowExpired` branch or the "unrelated Success row expires" case. `Keep_Confirmation_Through_An_Earlier_Expiry_Pass` runs with an empty list, and the task 313 expiry test uses an Error row (notification-copy-tests.cs:62-72 area). Deleting the `copiedRowExpired` clause would still pass every existing test, so a regression in the changed code would go unnoticed.
- Suggestion: Add two state tests: (a) copy a shell Success row, then `ExpireMessagesActionSet.Action(row.AutoDismissAt)` before `CopiedUntil` and assert `CopiedMessageId` is null; (b) `CopyMessageBar`, add a Success message, expire at the Success `AutoDismissAt` (before `CopiedUntil`) and assert `CopiedMessageId` still names the bar.
- Status: open

### Issue 3 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md:5-6
- Description: The new reservation note says "TWA0036 is named by to-do task 452's spec (ISO date rendering); leave it for that rule." This file is under root `source/`, which the template packs into generated apps, where `kanban/` does not exist, and "to-do" is a status that goes stale when task 452 moves. The surrounding retired/reserved lines name the rule's subject, not a task state.
- Suggestion: Reword to the subject only, for example "TWA0036 is reserved (ISO date rendering rule)."
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/NotificationsSample.razor:13; source/container-apps/web/projects/web-spa/features/design-system/samples/MessageBarsSample.razor:4
- Description: Stale wording left after the conversion. NotificationsSample says ExceptionNotificationHandler "records a FluentMessageBar" (it records a `NotificationState` row that the shell paints as `MessageBar`). The sample card title "Message bars (FluentUI)" (also `design-system-parts.cs`, title argument) still labels the thing as FluentUI although the sample now shows the shared `MessageBar`.
- Suggestion: Say "records a message bar" in the NotificationsSample comment; optionally drop "(FluentUI)" from the title if the part-title string is not asserted by `DesignSystemPage_Should_`.
- Status: open

## Verified

- Requirements 1-8 (task.md lines 11-17) and the four call sites (task.md lines 21-24) are met: `MessageBar.razor:67-90` paints the icon-only Copy button labelled "Copy message" / "Copied"; `ChildContent` is rendered but never passed to the clipboard (`MessageBar.razor` `CopyAsync` sends only `Title` and `BodyText`); the nickname draft is `ChildContent` on "Name this passkey" (`AddPasskeyPrompt.razor:108-131`) and has no Body.
- `MessageBar.razor`: `LocalBarId`, `IsCopied` (`NotificationId ?? LocalBarId`), `BarLayout`, and `BarAttributes` (AskTag first, then `BaseComponent.Attributes` winning, so `data-qa` lands on the `fluent-message-bar`) are consistent. One `@code` block at top; no `IJSRuntime` (TWA0026); Copy only dispatches. `CssClass` is not forwarded, but no caller uses it.
- `ClipboardText(string, string?)` is the same format as before for the `NotificationMessage` overload; `WriteAndConfirmAsync` is a line-for-line extraction of the old inline try/catch (JSException and non-host-cancel `OperationCanceledException` become the Warning, host-cancel is swallowed).
- `RemoveMessage`, `Clear`, `Initialize`, `MarkCopied`, and `NextExpiry` (`MessageBarList.Min` over an empty list yields null, so a zero-row host still schedules for `CopiedUntil`) behave correctly with a page-local id in `CopiedMessageId`.
- Expiry timer reach: every converted site renders under a shell that hosts `MessageBars` unconditionally. `TimeWarpPage.razor:279` and `TimeWarpFocusedPage.razor:49` (not inside an `@if`); `AuthenticationPage` and `DesignSystemPage` use `TimeWarpPage`; `ChooseMicrosoft365Page` uses `TimeWarpFocusedPage`; `AddPasskeyPrompt` is rendered only from `TimeWarpPage.razor:282`. `MessageBars` reads `NotificationState` in markup and in `OnAfterRenderAsync`, so it is subscribed and re-renders and reschedules on a page-local copy even with zero rows.
- TWA0037 detection, WASM gate (stub fixtures exempt, `Given_Non_Wasm_Compilation_Does_Not_Flag`), razor-generated vs other `.g.cs` handling, and the mapped-parameter report location match TWA0032 (`direct-fluent-text-area-analyzer.cs`). The deliberate divergence is no opt-out attribute, which the Design region states. Exempting `MessageBars.razor` is what the spec asks for even though it no longer holds a raw bar.
- TWA0025: the `MessageBar` match (`Name == "MessageBar"`, namespace last segment `Components`, not the FluentUI namespace) is Warning-severity, WASM-only, and has a negative test. The control itself passes `Intent="@Intent"` (a property reference), which the silence bias leaves alone; the shell host passes `@message.Intent`. `MessageBarsSample` keeps its opt-out.
- `AnalyzerReleases.Unshipped.md` row format matches the table; TWA0037 is unused elsewhere (`git grep`).
- Tests prove the stated requirements: the format test, the write-and-confirm test, rejection and timeout Warning tests, the control Copy test (Title-only copy, the bar's own Save still works, "Copied" read from `UpdatedLabels` via `SetAttribute` edits with `ReferenceFrameIndex` and `Offset` used correctly), and all four design-system samples by index. The count assertion on `AllClickHandlers["MessageBarCopy"]` keeps the sample test from passing vacuously.
- Breakage search over tracked files (`git grep`, excluding `kanban/`): nothing selects `ConfigurationPolicyDrift` or `MessageBarBody` outside the new control and `AuthenticationPage.razor:168`; the only text expectations on the converted bars are in the new tests.
- Docs: `skills/tw-blazor/SKILL.md:264-300` and `components/overview.md:55` match the change; no other mention of rendering a raw `FluentMessageBar` as the thing to use exists in `skills/` (`tw-blazor-layout/SKILL.md:185-186` is about the host).
- Conventions: new `.cs` files are kebab-case (TW0001) with Purpose and Design regions (TWA0004); the new action carries `[CommandPaletteOptOut]` (the existing `CopyMessage` has none, which is allowed because the roster gives actions with required parameters no `Execute`).
