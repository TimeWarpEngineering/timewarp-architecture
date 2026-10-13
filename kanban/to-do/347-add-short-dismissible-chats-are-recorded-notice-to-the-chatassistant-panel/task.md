# Add short dismissible "Chats are recorded" notice to the chat/assistant panel

## Description

Cloudflare's dashboard AI chat shows a dismissible banner above the input: "Chats are recorded to improve the service and are processed in accordance with our Privacy Policy." Steven wants the same idea in Architecture's chat UI, but **no legalese**. Plain copy: "Chats are recorded." Optionally link to a privacy page if the app has one (keep that optional).

Reference: `assets/cloudflare-chat-reference.png` (Cloudflare dashboard AI chat, Steven's screenshot 2026-10-13).

Existing chat surfaces to start from:
- `source/container-apps/web/projects/web-spa/features/chat/pages/ChatPage.razor`
- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/` (`AgentAsk.razor`, `AskSuggestions.razor`, `AskAnswerBar.razor`, `AskStatusLine.razor`)

Keep it simple. No design work here (visual polish is 349).

## Requirements

- Small banner directly above the chat input
- Dismissible with an X; dismissal remembered per user/browser (e.g. local storage)
- Copy configurable per app (default "Chats are recorded."), optional privacy link URL
- Applies to whichever chat surfaces exist (ChatPage and/or Agent Ask)

## Checklist

- [ ] Locate chat input surface(s)
- [ ] Add notice component with configurable text and optional link
- [ ] Persist dismissal per browser
- [ ] Tests (renders, dismiss hides, stays dismissed on reload)

## Related

- 347 Chats-are-recorded notice
- 348 Starter prompts for primary use cases
- 349 Polished chat panel redesign (Opus 5.5)

## Session

- Created: 1440097 (2026-10-13)
