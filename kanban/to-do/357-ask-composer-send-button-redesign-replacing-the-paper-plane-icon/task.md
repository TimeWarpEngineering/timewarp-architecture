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

- [ ] Round arrow-up send button with empty/in-flight/sendable states
- [ ] Stop state if cancel exists (else follow-up noted)
- [ ] Accessible name; keyboard behaviour verified
- [ ] Tests
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve's Cloudflare screenshot)
