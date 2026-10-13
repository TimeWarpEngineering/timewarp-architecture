# Ask composer edit-mode pill with a two-option popover, replacing the "Edit behavior" row

## Why (Steve, 2026-10-14)

Cloudflare's composer has a small **pill button** at the bottom-left. Clicking it pops up the two
edit modes, each with an icon and a one-line description:
- **Ask before editing** — "Review and approve each change"
- **Automatically edit** — "Always allow edits for this conversation"

That is cleaner than our full-width "Edit behavior: Ask before editing | Automatically edit"
segmented row under the composer (`source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor` ~596, `data-qa=AskEditMode`).

Cloudflare labels the pill "**Ask**", which reads badly here. Steve suggested "Mode" or
"Edit mode". The pill should show the **current mode** with its icon (for example ✎ "Ask first" /
⏩ "Auto-edit"), with an accessible name like "Edit mode: Ask before editing". Pick the final
label and record it.

## Requirements

- Replace the segmented row with a pill in the composer's footer row, next to the Tag button.
  Clicking it opens a FluentUI v5 popover or menu with the two options. Each option has an icon,
  a title and a subtitle, and the active one is marked with a check or highlight.
- Keyboard: open with Enter/Space, move with ↑↓, choose with Enter, close with Esc; focus returns to
  the pill. ARIA: `aria-haspopup`, `aria-expanded`, a menu with radio items.
- Choosing a mode dispatches the existing `AgentSurfaceState.SetEditMode` action (no behaviour
  change); everything stays a State action (TWA0026).
- Keep `data-qa` hooks for tests: `AskEditMode` on the pill, with option hooks.
- Styling per `tw-blazor-css-strategy`, matching 349's redesign tokens.
- Tests: state/component (open, choose, current mode reflected); Playwright (switch modes with
  mouse and keyboard; the old row is gone).

## Checklist

- [ ] Pill + popover replacing the segmented row; label decision recorded
- [ ] Keyboard + ARIA; existing action reused
- [ ] Tests (component + Playwright)
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve's Cloudflare screenshot)
