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

- [x] Pill + popover replacing the segmented row; label decision recorded
- [x] Keyboard + ARIA; existing action reused
- [x] Tests (component + Playwright)
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve's Cloudflare screenshot)
- 2026-10-14 implementer (ganda task work): pill + menu built, tests and gates green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T02:06:29Z
- 2026-10-14 review oracle (claude-opus-5-5, tw-implementation-review effort 2): general reviewer subagent afd88d563babaf134 (Sonnet); 1 round; fix applied on this id.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-14T02:30:15Z

## Results

**Label decision:** the pill shows the current mode, not a fixed word: ✎ **Ask first** (Edit icon)
or ⏩ **Auto-edit** (FastForward icon). Its accessible name and title are "Edit mode: Ask before
editing" / "Edit mode: Automatically edit". "Ask" was rejected because it reads as the panel's name.
Recorded in the Design region of `AskEditModePill.razor`.

- New `AskEditModePill.razor` (+ scoped `.razor.css`) in the composer lead slot beside Tag. It is a
  FluentUI v5 `FluentMenu` with the pill as the trigger slot and two `MenuItemRole.Radio` items
  (icon, title, subtitle; the current mode is checked). The fluent-menu web component supplies
  `aria-haspopup` / `aria-expanded`, Enter/Space to open, the arrow keys to move, Enter to choose,
  and Esc to close with focus back on the pill.
- Choosing a mode dispatches the existing `AgentSurfaceState.SetEditMode` (radio items raise
  `CheckedChanged`, not `OnClick`). No behaviour change.
- Focus after a choice: a mode change rebuilds the agent, and the composer (keyed on the
  conversation) mounts a new pill. The pill raises `Chose`, and AgentAsk passes `RestoreFocus` to
  the next pill, which focuses itself once with `ElementReference.FocusAsync` (not flagged by TWA0026).
- The old "Edit behavior" footer row and its CSS are gone. The pill exists only when the composer
  does, so the not-configured / sign-in / error states no longer show an edit-mode control (they
  have no agent to edit with).
- `data-qa`: `AskEditMode` on the pill (with `data-mode` = current mode); options `AskBeforeEditing`
  and `AskAutomaticallyEdit`.
- Tests: `ask-edit-mode-pill-tests.cs` (HtmlRenderer: default Ask first + both options; after
  SetEditMode the label, aria-label, data-mode and checked item follow). `AskSurface_Given_Wasm`
  now covers mouse switching (menu screenshot `ask-edit-mode-menu.png`), keyboard Enter/Space/arrows/
  Enter/Esc with focus returning to the pill, the pill sitting on Tag's row, and the old row gone.
- Boyscout: `AskSignIn_Given_Wasm` pressed Esc as soon as the "Picking" status rendered, before
  `ask-tag.ts` attached its key listener. The extra menu render widened that race, so the test
  failed with the pill and passed without it. It now waits for the pick outline first.
- Gates: `dev build` 0 warnings / 0 errors; `dev test` all projects passed; `ganda repo audit` passed.

### Review disposition

- Rounds: 1; effort 2; roster: general (Sonnet subagent) + review-oracle verification.
- Final counts: bug 1 fixed; suggestion 1 wontfix; nit 1 fixed, 1 wontfix; 0 open.
- Disposition: **accepted-exceptions**.
  - M1 (bug, fixed): a mode change mid-turn defers the rebuild, which left the focus-restore flag set
    so a later pill stole focus. AgentAsk now asks for focus only when no turn defers the rebuild,
    and New conversation clears the flag.
  - M2 (nit, fixed): Design-region wording.
  - M3 (wontfix): a deferred-turn test needs a streaming stub the WASM lane lacks.
  - M4 (wontfix): the `aria-expanded` risk was disproved by a green Playwright run.
- After fix: `dev build` 0/0; `AskEditModePill_Should_` 2/2; `AskSurface_Given_Wasm` 1/1.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate

**Smoke:**

```bash
dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -- --filter-class AskEditModePill_Should_
cd ../web-spa-playwright-tests && dotnet test -- --filter-class AskSurface_Given_Wasm
dotnet test -- --filter-class AskSignIn_Given_Wasm
```

Manual: `dev run`, open Ask (Ctrl-K → Ask), click the "Ask first" pill next to Tag, choose
"Automatically edit"; then Tab to the pill, Enter, ↓, Enter; Esc closes it.

**Expect:** build 0/0; `AskEditModePill_Should_` 2/2; `AskSurface_Given_Wasm` 1/1; `AskSignIn_Given_Wasm`
2/2. In the app, no "Edit behavior" row under the composer; the pill opens a menu with two options
(icon, title, subtitle, a check on the current one); choosing switches the pill to "Auto-edit"
(the panel root's `data-built-mode` becomes `AutomaticallyEdit`) and focus is back on the pill.
