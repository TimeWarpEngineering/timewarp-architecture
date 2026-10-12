# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** same as framework (MessageBars.razor, notification-state.cs,
notification-state.copy-message.cs, clipboard-js-module.cs, clipboard.ts, notification-copy-tests.cs)

## Summary

The change adds a Copy button that only dispatches `CopyMessage`. The handler does the
interop through a `[SideEffectService]` module, and the "Copied" icon swap reuses the host's
existing expiry timer through `NextExpiry`. This matches the task rules. Checked: the timer
reschedules when `NextExpiry` moves earlier (`ScheduleExpiry` compares against
`ScheduledExpiry`); dismiss, clear, and initialize reset the copied marker; TWA0026 stays
clean; the copied-text format matches the spec. Risk is low. One edge case breaks the
"never throws" rule.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.copy-message.cs:73
- Description: The handler catches only `JSException`. JS interop can also end in
  `OperationCanceledException`/`TaskCanceledException`, either from the Server JS-interop
  default timeout (for example a stalled permission prompt) or from the host's
  `CancellationToken` being cancelled when MessageBars is disposed. Either one escapes the
  handler. The pipeline then publishes an `ExceptionNotification`, which paints an Error bar
  reading "A task was canceled." The task rule says clipboard failure reports as a Warning and
  never throws.
- Suggestion: When the caller's token is not cancelled, treat `OperationCanceledException` as
  a failed copy (Warning bar). When the token is cancelled, swallow it because the host is
  gone. Add a test for the timeout path.
- Status: open
