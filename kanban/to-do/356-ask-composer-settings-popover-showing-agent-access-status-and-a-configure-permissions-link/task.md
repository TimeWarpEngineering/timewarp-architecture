# Ask composer settings popover showing agent access status and a "Configure permissions" link

## Why (Steve, 2026-10-14)

Cloudflare's composer has a small **settings (sliders) icon** next to send. It pops up:
- ✓ **API token active** — "Cloudflare Agent can access your account"
- ⚙ **Configure permissions**

Steve: "seems reasonable". Our panel already has the matching concept: Ask runs under the user's
own principal through a **per-conversation, short-lived, scoped credential** (task 292;
`web-spa/features/application/agent/agent-conversation-credential.cs`, `AgentSurfaceState`).

## Requirements

- A sliders/settings icon button in the composer footer (right side, before send). Clicking it
  opens a FluentUI v5 popover:
  - **Status line:** whether the conversation's agent credential is active (✓ "Agent access
    active", with a subtitle such as "Ask can act on this app as you"), or expired/absent with a
    way to renew it if renewal exists. Show expiry if that's cheap. Reflect the real
    `AgentConversationCredential` state; never a hard-coded check.
  - **"Configure permissions"** navigates to the existing place where the user manages agent
    access, such as Agent links or Settings → agent permissions; confirm what exists. If no such
    page exists, say so in the PR and link to the closest one. Don't build a new permissions page
    here.
  - Optionally a link to the privacy notice (347) if it fits.
- Keyboard and ARIA like any menu or popover; focus returns to the icon. All interactions are
  State actions (TWA0026); navigation goes through an action.
- Tests: component (active, expired and absent states render correctly; the link target is right);
  Playwright smoke.

## Checklist

- [x] Settings icon + popover with real credential status
- [x] Configure-permissions link to the existing management page (confirm target)
- [x] Keyboard/ARIA; State actions
- [x] Tests
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve's Cloudflare screenshot)
- 2026-10-14 implement (ganda task work): AskSettingsMenu, component + Playwright tests, gates green.

## Results

**What shipped**

- `AskSettingsMenu.razor` (+ `.razor.cs`, `.razor.css`) in `web-spa/features/application/modals/agent-ask/`:
  a sliders icon button (`Options` icon, `aria-label="Ask settings"`, `data-qa=AskSettings`) on the
  composer footer row just left of send. It is a `FluentMenu` trigger, like the edit-mode pill
  (task 355). The fluent-menu web component owns `aria-haspopup`/`aria-expanded`, opening on
  click/Enter/Space, arrow keys, Esc, and focus return to the button.
- **Status item** (first, disabled so it reads in the menu but cannot be chosen) comes from
  `AgentSurfaceState.Conversation` + `AgentConversationAuthority.IsExpired`, the header's own test:
  - **Active**: check icon, "Agent access active", "Ask can act on this app as you", and
    "Expires at <local time>" in a `<time datetime="…Z">`.
  - **Expired**: warning icon, "Agent access expired", plus a **New conversation** item that
    dispatches `NewConversation`. That is the only renewal: the panel mints a fresh credential.
  - **Absent** (no credential: not minted yet, or the session has no principal id): "No agent
    credential", "Ask runs under your session without a conversation credential".
  - The button carries `data-status` = Active / Expired / Absent.
- **Configure permissions** closes the panel and dispatches `RouteState.ChangeRoute` to
  `AgentLinksPage.GetPageUrl()` (`/AgentLinks`), with the same shape as Ask's Sign in. The
  Applications → AgentLinks edge is declared with `[CrossSliceReference]` in `AskSettingsMenu.razor.cs`.

**Target confirmation (for the PR):** no page manages *Ask's* permissions. The conversation
credential's scopes are a copy of every permission claim the person holds, and an admin manages
those under Admin → Roles. The closest self-service page about agent access is **Agent links**
(`/AgentLinks`, policy `AgentLinkManageSelf`, seeded for every role), so the link goes there.
No new permissions page was built.

**Placement note:** `MessageInput.TrailingActions` *replaces* the send button (library doc:
"Replaces the default send button"). The menu therefore renders in `LeadingActions`, and the
composer grid (`AgentAsk.razor.css`, new `trail` area) places it left of send. Its tab stop
comes before the textarea.

**Not done / notes**
- The privacy link (347) was left out. The privacy banner already carries it, and an absolute
  `XAI:PrivacyUrl` would need external-navigation handling through RouteState.
- The status is computed on render. An expiry that passes while the panel is idle shows on the
  next re-render, the same as the existing header banner.
- Opened with the mouse, Esc does not close fluent-menu while focus stays on the page body
  (library behaviour, same as the edit-mode pill). Outside click closes it. Keyboard open + Esc
  works and is tested.

**Tests**
- `tests/container-apps/web/web-spa-integration-tests/features/application/ask-settings-menu-tests.cs`
  (5): absent, active with expiry `datetime`, expired with renew item (then NewConversation →
  absent), status item disabled, and the Configure permissions route is `AgentLinksPage.GetPageUrl()` = `/AgentLinks`.
- Playwright `AskSurface_Given_Wasm`: button sits left of send on its row, `aria-haspopup`, label,
  `data-status=Absent` (SPA signed out), Enter opens (`aria-expanded=true`), status text, screenshot
  `ask-settings-menu.png`, Esc closes and focus returns to `AskSettings`, then clicking Configure
  permissions detaches the panel and the URL contains `AgentLinks`. Not configured: no settings button.
- Playwright `AskSignIn_Given_Wasm`: signed in, the button reports `data-status=Active` and the
  menu shows "Agent access active" with the expiry.

**Gates (2026-10-14):** `dev build` 0 warnings / 0 errors. `dev test`: everything passed except
`web-jaribu-tests` and `web-server-integration-tests`, which failed only at SetupOnce because
port 36500 was in use (another process held the base). Both were rerun with
`TIMEWARP_TEST_PORT_BASE=41200`: 331/331 and 332/333 passed, 1 skipped, 0 failed.
`web-spa-integration-tests` and `web-spa-playwright-tests` passed inside `dev test`.
`dev template-smoke` passed. `ganda repo audit` passed.

### How to validate

**Smoke**

```bash
dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet build && bin/Debug/net11.0/web-spa-integration-tests --filter-class AskSettingsMenu_
cd ../web-spa-playwright-tests && dotnet build && bin/Debug/net11.0/web-spa-playwright-tests --filter-class AskSurface_Given_Wasm
bin/Debug/net11.0/web-spa-playwright-tests --filter-class AskSignIn_Given_Wasm
```

Manual: `dev run`, sign in, open Ask (Ctrl-K → Ask), and click the sliders icon left of send.

**Expect**

- Component suite: 5/5 passed. Playwright AskSurface: 1/1 passed. AskSignIn: 2/2 passed.
- `playwright-screenshots/AskSurface_Given_Wasm/ask-settings-menu.png` shows the menu over the
  composer: "No agent credential" status, then "Configure permissions".
- Manual, signed in: the menu shows ✓ "Agent access active", "Ask can act on this app as you",
  and "Expires at <time>". Esc closes it with focus back on the icon. Configure permissions closes
  the panel and lands on Agent links. The expiry is about 12 hours out (the default lifetime is
  720 minutes). The expired state ("Agent access expired" plus New conversation) is covered by the
  component test, because the lifetime is not configurable at runtime.
