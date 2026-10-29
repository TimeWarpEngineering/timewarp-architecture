# Ask panel copies the entire session

## Description

In the Ask panel the person can copy only the last answer (AskAnswerBar copies
LastAnswer). There is no way to copy the entire session. Add a conversation-level
copy that places the whole transcript on the clipboard.

Reported 2026-10-28: "In the Ask section the user should be able to copy the
entire session."

## Goal (implementer restatement)

Give the Ask panel a second copy action that puts the whole thread on the clipboard, not just
the last answer. It walks `Conversation.Turns` from the first turn, writes each user prompt as a
`You:` paragraph and each reply as an `{AssistantName}:` paragraph (raw markdown), and copies
the text with `AskAiJsModule.CopyTextAsync`.

Acceptance:

- A "Copy conversation" icon button (`data-qa=AskCopyConversation`) sits in the answer bar.
- The per-answer Copy (`AskCopy`) still copies only the last answer.
- The empty state shows neither copy action. The answer bar still renders only once there is an answer.
- The Playwright Ask surface test clicks the button on its two-turn thread and checks that the
  clipboard holds both prompts and both replies, in order.

## Checklist

- [x] Add a "Copy conversation" action (header or answer-bar) that serializes
  Conversation.Turns in order (user prompts + assistant replies as markdown)
  and writes it with AskAiJsModule.CopyTextAsync.
- [x] Keep the existing per-answer Copy behavior unchanged.
- [x] Cover with an integration or Playwright test (button present, clipboard
  text contains both sides of a two-turn thread).

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- Implemented: ganda task work implement oracle, Claude Opus 5.5 (2026-10-29)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 3e392671-ddab-4ea0-adaf-6a945a18238f, max-turns 500, Done — 2026-10-29T16:16:22Z

## Results

- `AskAnswerBar.razor` has a new icon button, "Copy conversation" (`DocumentCopy` icon,
  `data-qa=AskCopyConversation`), placed right after Copy. `Transcript()` walks
  `Conversation.Turns` from the first turn. For each turn it writes `You:` plus the request
  blocks' rich text, then `{AssistantName}:` plus the response blocks' rich text (raw markdown),
  and puts a blank line between paragraphs. A side with no text is skipped. The text goes
  through `AskAiJsModule.CopyTextAsync`.
- Copy, the thumbs, and `LastAnswer()` are unchanged. The bar still renders only when there is
  an answer, so the empty state has no copy actions. The Design comment and the side-effect
  attribute now mention the new action.
- `ask-surface-playwright-tests.cs` checks the button's icon, title, and aria-label. It clicks the
  button on the existing two-turn thread and asserts this clipboard text: `You:\none\ntwo\nthree`,
  then an `{name}:\npage_context:` reply, then `You:\nWhat is on this page?`, then a second reply.
  The assistant name is not pinned in the test, because another suite renames it.
- Gates: `dev build` passed (0 warnings). `dev test --group fast` passed (14/14).
  `dev test --group playwright` passed (web-spa-playwright-tests). `ganda repo audit` exits 0.
  Its one finding is a kanban-landed warning about 8 live claims held by other tasks
  (070-001, 104-024..026, 123, 141, 198, 244) that have no worktree. Those are other
  sessions' claims, so this task did not change them.

### How to validate

Smoke: `cd tests/container-apps/web/web-spa-playwright-tests && dotnet run -c Release -- --filter-class AskSurface_Given_Wasm`
(after `dev build`). By hand: `dev run`, open Ask, send two prompts, then click the
"Copy conversation" icon next to Copy.

Expect: the test passes (1/1). By hand, the clipboard holds both prompts under `You:` and both
replies under the assistant's name, in order. Copy still copies only the last answer.

## Notes

Current copy path: AskAnswerBar.razor LastAnswer() walks Turns from the end and
returns the first non-empty RichContentBlock.RawText. The new action should walk
from the start instead and join turns (e.g. "You:" / "{AssistantName}:" lines).
Empty state still shows no copy actions. Name the data-qa AskCopyConversation.
