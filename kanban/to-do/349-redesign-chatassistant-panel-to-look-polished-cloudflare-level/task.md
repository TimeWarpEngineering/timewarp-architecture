# Redesign chat/assistant panel to look polished (Cloudflare-level)

## Description

Steven: Cloudflare's chat looks good and ours looks bad. Visual redesign of the chat/assistant panel to a polished, Cloudflare-level look. **This is design work.**

**Model: Opus 5.5** - design work assigned to Opus 5.5. Harness note: launch with the profile that runs Opus 5.5 (ganda `task work` has no per-task model field; pass `--profile` accordingly).

Reference: `assets/cloudflare-chat-reference.png` (Cloudflare dashboard AI chat, Steven's screenshot 2026-10-13).

Existing chat surfaces to start from:
- `source/container-apps/web/projects/web-spa/features/chat/pages/ChatPage.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/` (`AgentAsk.razor`, `AskSuggestions.razor`, `AskAnswerBar.razor`, `AskStatusLine.razor`)

## Requirements

Redesign:
- Header (title, actions, close)
- Greeting / empty state
- Starter prompt cards (data from 348)
- Notice banner (component from 347)
- Input area (composer, send, attachments if any)

Responsive, light/dark consistent with the template theme.

## Checklist

- [ ] Audit current panel vs reference
- [ ] Design header, empty state, prompt cards, banner, input
- [ ] Implement and screenshot before/after
- [ ] Tests/visual checks still pass

## Related

- 347 Chats-are-recorded notice
- 348 Starter prompts for primary use cases
- 349 Polished chat panel redesign (Opus 5.5)

## Session

- Created: 1442907 (2026-10-13)
