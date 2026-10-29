# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** branch vs merge-base 7181632c9, product paths: `AskAnswerBar.razor`, `ask-surface-playwright-tests.cs`

## Summary

The change adds a second icon button, "Copy conversation" (`data-qa=AskCopyConversation`), to the
Ask answer bar. `Transcript()` walks `Conversation.Turns` from the first turn and emits a `You:`
paragraph from the request blocks and an `{AssistantName}:` paragraph from the response blocks,
using the raw markdown of each `RichContentBlock`, skipping a side with no text, and joining
paragraphs with a blank line. The text goes through the same `AskAiJsModule.CopyTextAsync` path as
Copy. `CopyAsync`, `LastAnswer()`, the thumbs, and the render guard are untouched, so the empty
state still shows no copy action. The Playwright Ask surface test asserts the button's icon, title,
and aria-label, clicks it on the existing two-turn thread, and checks both prompts and both replies
appear in order. Risk is low: the logic is a pure read of the cascaded conversation, and the one
side effect is already declared on the component.

Goal check: the implementer restatement matches the task Description, Checklist, and Notes
(walk from the first turn, `You:` / `{AssistantName}:` lines, `CopyTextAsync`, data-qa name,
per-answer Copy unchanged, Playwright coverage of a two-turn thread). No misread.

Gates re-run by the reviewer in this worktree:

- `dev build`: 0 warnings, 0 errors.
- `dotnet run -c Release --no-build -- --filter-class AskSurface_Given_Wasm` in
  `tests/container-apps/web/web-spa-playwright-tests`: first run failed, second run passed.
  The first failure was a 30 s `WaitForURLAsync` timeout at the final Configure-permissions
  navigation (`AssertSettingsMenuAsync`, test line 599) on a cold WASM start (43 s). That step
  runs after the new `AskCopyConversation` assertions, which had already passed, and the code it
  exercises is not in this diff. The pass took 14 s. Pre-existing cold-start flake, not a finding
  against this change.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskAnswerBar.razor:13
- Description: The new Design sentence was spliced into the existing wrapped paragraph, leaving a
  170-character line that runs the "A turn side with no text is skipped." sentence straight into
  the old "Thumbs are a human SubmitFeedback" sentence. The rest of the region wraps near 100.
- Suggestion: Rewrap the Design paragraph so the task 444 sentences end on their own line and the
  Thumbs sentence starts a fresh line.
- Status: open
