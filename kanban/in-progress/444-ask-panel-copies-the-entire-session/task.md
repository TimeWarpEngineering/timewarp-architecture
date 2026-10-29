# Ask panel copies the entire session

## Description

In the Ask panel the person can copy only the last answer (AskAnswerBar copies
LastAnswer). There is no way to copy the entire session. Add a conversation-level
copy that places the whole transcript on the clipboard.

Reported 2026-10-28: "In the Ask section the user should be able to copy the
entire session."

## Checklist

- [ ] Add a "Copy conversation" action (header or answer-bar) that serializes
  Conversation.Turns in order (user prompts + assistant replies as markdown)
  and writes it with AskAiJsModule.CopyTextAsync.
- [ ] Keep the existing per-answer Copy behavior unchanged.
- [ ] Cover with an integration or Playwright test (button present, clipboard
  text contains both sides of a two-turn thread).

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)

## Notes

Current copy path: AskAnswerBar.razor LastAnswer() walks Turns from the end and
returns the first non-empty RichContentBlock.RawText. The new action should walk
from the start instead and join turns (e.g. "You:" / "{AssistantName}:" lines).
Empty state still shows no copy actions. Name the data-qa AskCopyConversation.
