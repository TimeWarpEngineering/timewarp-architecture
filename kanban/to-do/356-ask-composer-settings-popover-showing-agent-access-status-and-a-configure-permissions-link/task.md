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

- [ ] Settings icon + popover with real credential status
- [ ] Configure-permissions link to the existing management page (confirm target)
- [ ] Keyboard/ARIA; State actions
- [ ] Tests
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve's Cloudflare screenshot)
