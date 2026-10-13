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

- [x] Audit current panel vs reference
- [x] Design header, empty state, prompt cards, banner, input
- [x] Implement and screenshot before/after
- [x] Tests/visual checks still pass

## Related

- 347 Chats-are-recorded notice
- 348 Starter prompts for primary use cases
- 349 Polished chat panel redesign (Opus 5.5)

## Session

- Created: 1442907 (2026-10-13)
- 2026-10-13: implemented by ganda task work (Opus 5.5, headless implementer).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T16:18:15Z
- Review oracle: claude-opus-5-5 (headless), effort 3, roster general (subagent a5decffb365815db4); 2 rounds; disposition clean — 2026-10-13

## Results

### Audit (before → reference)

`assets/before-ask-empty-state-1280.png` vs `assets/cloudflare-chat-reference.png`:

- Header: a large "Ask" heading stacked over the conversation name, no divider. The reference has one compact row and a hairline under it.
- Copy, thumbs and Support showed in the **empty** state. MessageList renders ChildContent above the turns, so the answer bar was always there, at the top.
- Greeting: small and left-aligned, with no hero.
- Cards: centered, bold title and subtitle, the icon floating.
- Body capped at `max-height: min(28rem, 60dvh)`, so the composer floated mid-panel over blank space.
- Library tokens left the send button and focus ring Bootstrap blue (`#0d6efd`), off-theme.
- Edit-behavior footer was a heavy full-width segmented control.

### What changed

- **Header** (`AgentAsk.razor`): one row: brand gradient mark (filled Sparkle), "Ask" in 15px semibold, the conversation name muted and truncated, then subtle icon actions over a hairline. The expired-conversation notice is now a warning-bordered strip under the header.
- **Body**: fills the space between the header and the edit row and scrolls on its own. The 28rem cap is gone. Dotted canvas (`radial-gradient` of `--twe-rule` on `--twe-background`), echoing the reference.
- **Empty state**: three CSS gradient orbs in the brand purple, blue and green (the TimeWarp take on the reference cloud; no image asset). Greeting "What can I help with?" (`data-qa=AskGreeting`), muted tagline, then the cards.
- **Starter cards** (`AskStarterPrompts`): left-aligned, 36px soft icon tile, 14px medium title, muted 12px subtitle, `--twe-radius` card with `--twe-shadow-sm`, hover lift. Styled on the fluent-button host: Fluent v5 draws the border there, and the light-DOM text inherits from the host.
- **Notice** (`AskPrivacyBanner`): rounded raised card, text wraps, X stays top-right, underlined Privacy link.
- **Composer**: raised `--twe-radius` card, focus border in the primary color plus a deeper shadow, 32px send button in brand purple, Tag as a small outline pill. `--sc-ai-*` library tokens are mapped onto `--twe-*` on `.sc-ai-root`, so the transcript (user bubble, accent, borders, fonts) follows the template theme.
- **Answer bar** (`AskAnswerBar`): renders only once there is an answer, and moved into the footer directly under the latest answer. It subscribes to conversation status because the cascade is fixed. Support is right-aligned.
- **Not-ready states**: centered block with a section-size title, a helper line and a styled monospace command card.
- **Edit behavior**: a quiet row with a muted label and a small segmented control.
- Light/dark: every color is a `--twe-*` token. The template ships light-only today (`data-theme="light"`; nothing consumes ThemeState.Dark), so the panel follows whatever the tokens become.

### Boyscout

- `tests/.../aspire-spa-test-application.cs`: registers Blazored local storage. `CatalogAgent_Should.Dismiss_Privacy_Notice_Hides_It_Across_New_Conversations` (task 347) failed on master because DismissPrivacyNotice's handler could not resolve `ILocalStorageService`.

### Screenshots

`assets/`: `before-ask-empty-state-{1280,800}.png`, `after-ask-empty-state-{1280,800}.png`, `after-ask-answer-1280.png`, `after-ask-not-configured-1280.png`.

### Gates run

- `dev build`: success (0 warnings, 0 errors).
- `web-spa-playwright-tests` (all): 12/12 passed, including the new `AskPanelDesign_Given_Wasm`.
- `web-spa-integration-tests --filter-class Ask`: 38/38. `--filter-class CatalogAgent`: 40/40 after the boyscout fix (39/40 before).
- `ganda repo audit`: passes all checks.

### Review disposition

- Rounds: 2. Roster: general, effort 3 (by-diff budget, 867 lines).
- Final counts: bug 2 fixed, suggestion 2 fixed, nit 4 fixed. 0 open, 0 wontfix.
- Disposition: **clean**.
- Fixes on this task:
  - The Tag pill and edit-behavior segment styles now target the fluent-button host. Fluent v5 buttons have no `control` part.
  - `WaitForBoxAsync` now asserts that the box reached the expected size.
  - On short viewports the orbs shrink to half size (`zoom`), so all four cards fit at 1280x800 docked and at 800x700.
  - Small secondary text moved from `--twe-muted` (below AA) to `--twe-ink-2`.
  - The duplicate comment is removed, and the cards respect reduced motion.
  - `AskAnswerBar.LastAnswer()` returns `string?` instead of comparing against the fallback label.
  - The title uses the type token.
  - The expired notice sits in an always-present `role=status` live region.
- Re-run gates: `dev build` 0/0; Playwright 12/12; integration `Ask` 38/38, `CatalogAgent` 40/40.
- Artifacts: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`.
- The `after-ask-empty-state-{1280,800}.png` assets were re-taken after the fixes.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet build && dotnet run --no-build -- --filter-class AskPanelDesign
# then open bin/Debug/net11.0/playwright-screenshots/AskPanelDesign_Given_Wasm/ask-empty-state-1280.png
dotnet run --no-build -- --filter-class AskSurface
```

**Expect**

- Both runs report `failed: 0`.
- `ask-empty-state-1280.png` matches `assets/after-ask-empty-state-1280.png`: a compact header row with the gradient mark; dotted canvas; the brand orbs over "What can I help with?" and the tagline; left-aligned cards with icon tiles; a rounded "Chats are recorded. Privacy" card directly above the raised composer with a purple send button; the edit-behavior row at the bottom.
- No Copy or thumbs in the empty state. After an answer (`AskSurface_Given_Wasm/ctrl-k-ask-result.png`) they sit under the answer, above the composer.
