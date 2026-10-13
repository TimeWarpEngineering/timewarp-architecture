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

- [x] Review existing starter prompts
- [x] Define prompt model (icon, title, subtitle, prompt) and config
- [x] Curate default 4-6 prompts for primary use cases
- [x] Render in empty state; click sends prompt
- [x] Tests

## Related

- 347 Chats-are-recorded notice
- 348 Starter prompts for primary use cases
- 349 Polished chat panel redesign (Opus 5.5)

## Session

- Created: 1441658 (2026-10-13)
- Implemented: 2026-10-13 (ganda task work, implement oracle)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T15:44:32Z
- Review oracle: claude-opus-5-5 orchestrator plus one general reviewer subagent (sonnet), effort 2, 2 rounds, disposition clean — 2026-10-13

## Results

Ask's three hardcoded suggestion buttons (`AgentAsk.razor` `Suggestions` array rendered by
`AskSuggestions.razor`) are replaced by one configurable starter-prompt list. There is no
second list. `ChatPage.razor` is the SignalR demo chat and has no starter prompts, so it is
unchanged.

- **Model** (`features/agent-chat/agent-chat-starter-prompt-contracts.cs`):
  `AgentChatStarterPrompt` is a value record with `Icon` (a name), `Title`, `Subtitle`, and
  `Prompt`. `AgentChatStarterPrompts.Defaults` holds the template's four defaults.
  `Normalize` drops entries with no title or no prompt, trims every field, keeps at most
  `MaxCount` (6), and falls back to the defaults when nothing usable is left.
- **Config**: `XaiChatOptions.StarterPrompts` binds from `XAI:StarterPrompts` (a get-only list,
  so binding fills it and does not append to the defaults). `GetAgentChatConfiguration.Response`
  carries the normalized list. `AgentSurfaceState.StarterPrompts` takes it on a server answer and
  resets to the defaults on any failure. This is the same path task 347 uses for the privacy
  notice.
- **Defaults** (Ask's primary uses in every generated app): Find a page (Navigation), Explain
  this page (Question), Run an action (Flash), Check approvals (ShieldCheckmark).
- **Icons**: `AskStarterPromptIcons` maps a case-insensitive name from a fixed set (Sparkle,
  Navigation, Question, Flash, ShieldCheckmark, Search, Lightbulb, Document, Edit, Info) to a 20px
  FluentUI icon. It does not use reflection, so trimming stays safe. A blank or unknown name gets
  Sparkle.
- **Rendering**: `AskSuggestions` is renamed to `AskStarterPrompts` (data-qa `AskStarterPrompt`).
  Each card shows the icon, a bold title, and a muted subtitle, and a click sends `Prompt` through
  the cascaded `AgentContext`. Final card styling is task 349.
- **Tests**: `ask-starter-prompts-tests.cs` (6 Unit tests) covers the shape of the defaults,
  binding from the XAI section, normalization (blanks, trimming, cap), icon mapping, rendering a
  server-configured list, and falling back to the defaults on an empty list or a failed probe.
  The Playwright Ask surface test selector is updated to `AskStarterPrompt`.

Gates: `dev build` has 0 warnings and 0 errors. `web-spa-integration-tests --filter-tag Unit`
passed 125/125. `dev template-smoke` succeeded. `ganda repo audit` passes. The Playwright lane
was not run here: it needs the Aspire host, and workers do not run an AppHost.

### How to validate

**Smoke:**

```bash
dev build
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class AskStarterPrompts_Should_
# Manual: `dev run`, sign in, open Ask AI on a page with an empty conversation.
# Optional override for web-server (env vars or appsettings):
#   XAI__StarterPrompts__0__Icon=Search
#   XAI__StarterPrompts__0__Title="Find an order"
#   XAI__StarterPrompts__0__Subtitle="Look one up by number"
#   XAI__StarterPrompts__0__Prompt="Help me find an order."
```

**Expect:**

- The test class reports 6/6 passed.
- With no override, Ask's empty state shows four cards, each with an icon, a title, and a subtitle:
  Find a page, Explain this page, Run an action, and Check approvals. Clicking one sends its
  prompt text as the first message.
- With the override, Ask shows only the "Find an order" card with a search icon. More than six
  configured entries show only the first six. Entries with no title or no prompt are skipped.

### Review disposition

- **Rounds:** 2 at effort 2, with one `general` reviewer. Framework: `review/review-framework.md`.
- **Final counts** (`review/round-2/merged.md`): bug 0; suggestion 1 fixed; nit 3 fixed; 0 open;
  0 wontfix.
- **Disposition:** `clean` (`review/disposition.md`).
- **Fixes from review:**
  - M1: the handler test `Send_Normalized_Configured_Prompts_From_The_Handler`. The class now has
    7 tests.
  - M2: the Design region wording on list replacement and merge by index.
  - M3: init-only properties on `AgentChatStarterPrompt`.
  - M4: a re-wrapped Design comment.
- **Gates after the fixes:** `dev build` had 0 warnings and 0 errors, and the web-spa Unit lane
  passed 126/126.
