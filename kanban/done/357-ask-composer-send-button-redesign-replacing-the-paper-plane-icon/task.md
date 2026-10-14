# Ask composer send button redesign, replacing the paper-plane icon

## Why (Steve, 2026-10-14)

The composer's send button uses a paper-plane icon that looks like Telegram's. Steve: "I don't
really like" it. Cloudflare uses a **round button with an upward arrow** (↑), muted/disabled while
the input is empty and solid when there is text to send.

## Requirements

- Replace the paper-plane with an **arrow-up** glyph (FluentUI icon, for example `ArrowUp`) in a
  round button, using 349's brand tokens.
- States: disabled or muted when the input is empty or a send is in flight; solid when sendable.
  While an answer streams, it may become a **stop** button (■) if a cancel action exists; if not,
  record that as a follow-up instead of building cancel here.
- Accessible name "Send message" (and "Stop" when applicable). Keyboard: Enter sends and Shift+Enter
  makes a newline (keep the current behaviour; verify).
- Keep the existing send action and `data-qa`. All interactions stay State actions.
- Tests: component state for enabled/disabled; Playwright smoke (type → button enabled → send).

## Checklist

- [x] Round arrow-up send button with empty/in-flight/sendable states
- [x] Stop state if cancel exists (else follow-up noted)
- [x] Accessible name; keyboard behaviour verified
- [x] Tests
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve's Cloudflare screenshot)
- 2026-10-14: implemented by ganda task work (implement oracle)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T03:24:34Z
- Review oracle: tw-implementation-review, effort 2, roster general (claude-opus-5-5, session not reported) — 2026-10-14
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-14T03:26:05Z

## Results

- **Send stays the library button.** `MessageInput` owns the typed text and its send click, and
  `TrailingActions` would replace the button, so the send action, the "Send message" name and the
  Enter / Shift+Enter keys are unchanged. There was never a `data-qa` on send; tests and code use
  `.sc-ai-input__send`, which is kept.
- **New `AskSendButton.razor`** (a leading action of the composer, so its elements land in the
  composer grid's send cell):
  - Not streaming: the FluentUI `ArrowUp` (Regular 20) glyph, `aria-hidden` and click-through,
    drawn over the library button (`data-qa="AskSendGlyph"`).
  - Streaming: a circular `FluentButton` Stop (`Filled.Size16.Stop`, aria-label/title "Stop",
    `data-qa="AskStop"`) that calls `AgentContext.CancelAsync()` on the cascaded conversation (the
    same library-owned conversation the starter prompts call). The library then clears the partial
    answer and goes back to Idle. AwaitingInput (an approval card) shows the muted send instead.
  - It subscribes to the conversation's status changes itself, because AgentAsk re-renders only
    when a turn ends.
- **`AgentAsk.razor.css`** hides the library paper plane. The button is muted (`--twe-paper-2`,
  muted glyph) while the textarea shows its placeholder (nothing typed) or the library disables it
  (turn in progress), and solid `--twe-primary` with the `--twe-on-primary` glyph when there is
  text. Stop hides send while it shows.
- No follow-up for cancel: `AgentContext.CancelAsync` exists, so Stop is built.
- Known limit: whitespace-only text shows the solid state. The library ignores that send, so a
  click does nothing.
- Tests: `ask-send-button-tests.cs` (web-spa-integration, 2 tests) renders the real
  MessageInput + AskSendButton over a real UIAgent with a chat client that keeps the stream open:
  idle = glyph and enabled send; streaming = Stop and disabled send; cancel = back to glyph and
  enabled send. Playwright `AskSurface_Given_Wasm` now checks the "Send message" name, the hidden
  paper plane, the glyph over send, muted when empty, solid primary after typing, then the click
  send (screenshots `ask-send-muted.png`, `ask-send-solid.png`). The existing Shift+Enter newline and
  Enter send checks still pass.
- Gates: `dev build` 0 warnings 0 errors; `dev test` passed; `ganda repo audit` passes;
  `dev check-version` passes (source already ahead of the latest NuGet).

### Review disposition

- **Rounds:** 1 · **Effort:** 2 · **Roster:** general
- **Final counts:** bug 0 · suggestion 0 · nit 1 (wontfix) · open 0
- **Disposition:** `accepted-exceptions`. M1 (nit): keyboard Stop unmounts itself so focus drops
  to `body`; wontfix because the composer textarea/send are library-private and restoring focus
  needs JS interop into library DOM, outside this task's scope.
- **Artifacts:** `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke:**

```bash
dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet run -c Release -- --filter-class AskSendButton
cd ../web-spa-playwright-tests && dotnet run -c Release -- --filter-class AskSurface
```

Then, by hand, in `dev run` with an xAI key: open Ask (Ctrl-K → Ask).

**Expect:**

- Integration: 2/2 passed. Playwright: 1/1 passed, with
  `bin/Release/net11.0/playwright-screenshots/AskSurface_Given_Wasm/ask-send-muted.png` (grey
  round button, grey up arrow) and `ask-send-solid.png` (purple round button, white up arrow).
- By hand: the empty composer shows a muted round ↑ button; typing makes it solid purple; Enter
  sends and Shift+Enter adds a newline; while the answer streams a dark round ■ "Stop" button
  replaces send, and clicking it stops the answer and brings back the ↑ button.
