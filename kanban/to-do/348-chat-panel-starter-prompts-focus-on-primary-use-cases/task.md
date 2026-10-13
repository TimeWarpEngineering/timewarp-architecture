# Chat panel starter prompts: focus on primary use cases

## Description

Cloudflare's chat empty state shows a short list of top-level ready-made prompts (icon + title + one-line subtitle, e.g. "Transfer a domain / Walk me through the process") tied to its primary use cases. We already have some starter prompts (see `AskSuggestions.razor`). Rework them into a short curated list (about 4-6) for Architecture apps' primary use cases, configurable per app.

Reference: `assets/cloudflare-chat-reference.png` (Cloudflare dashboard AI chat, Steven's screenshot 2026-10-13).

Existing chat surfaces to start from:
- `source/container-apps/web/projects/web-spa/features/chat/pages/ChatPage.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/` (`AgentAsk.razor`, `AskSuggestions.razor`, `AskAnswerBar.razor`, `AskStatusLine.razor`)

## Requirements

- 4-6 curated prompts, each with icon, title, one-line subtitle, and the prompt text sent on click
- Defined via per-app configuration (template ships sensible defaults)
- Replace/trim the existing suggestion list rather than adding a second one
- Visual styling of the cards is finalized in 349; this task delivers data model + basic rendering

## Checklist

- [ ] Review existing starter prompts
- [ ] Define prompt model (icon, title, subtitle, prompt) and config
- [ ] Curate default 4-6 prompts for primary use cases
- [ ] Render in empty state; click sends prompt
- [ ] Tests

## Related

- 347 Chats-are-recorded notice
- 348 Starter prompts for primary use cases
- 349 Polished chat panel redesign (Opus 5.5)

## Session

- Created: 1441658 (2026-10-13)
