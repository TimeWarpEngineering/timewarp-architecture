# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** commit 0fb7ae062 (task 381 product + tests) vs master; call sites in
`CommandPalette.razor`, `TimeWarpPage.razor`, `command-palette.ts`.

## Summary

The change raises a one-shot `ComposerFocusPending` on `OpenAskPanel`, consumes it in
`AgentAsk.OnAfterRenderAsync` once the built composer is drawn, adds soft refocus after send and
expand, restores the opener on close, and has Ask claim the palette's pending focus return. The
state shape follows the TWA0026 / State-action rules, the no-textarea and approval-pending
branches match the requirements, and Playwright covers header, Ctrl-K, reopen, expand, send, close
and approval. One defect: the claimed palette return target can be the palette trigger itself.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/source/features/command-palette.ts:68
  (with `ask-ai.ts` `RestoreOpener`)
- Description: `CommandPalette.RunAsync` closes the modal before running the row, so when Ask's
  first render runs `CaptureOpener` focus is on `body` and the palette hands over its
  `returnFocusTo`. When the palette was opened by clicking or focusing the appbar search field
  (the palette trigger), that target is the search field's input. On Ask close, `RestoreOpener`
  focuses it directly; the trigger's `focusin` listener only ignores focus while the palette's own
  `restoring` flag is set, so the focus re-opens the command palette. Closing Ask therefore pops
  the palette back up for anyone who reached Ask through the search field. The Ctrl-K test path
  starts with focus on `body`, so nothing hands over a target and the bug is not covered.
- Suggestion: hand the restore over with the target (a palette-owned restore callback that sets
  `restoring` while it focuses), so Ask's close restores through the same guard the palette uses.
  Add a Playwright step: open the palette from the search field, run the Ask row, close Ask, and
  assert the palette stays closed and the search field has focus.
- Status: open
