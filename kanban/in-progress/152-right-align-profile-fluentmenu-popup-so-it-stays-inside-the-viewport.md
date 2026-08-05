# Right-align profile FluentMenu popup so it stays inside the viewport

## Description

Follow-up defect from task 149: the profile menu popup still renders off the right viewport
edge (observed 2026-08-05, signed-out home page — the "Sign-in" item is clipped). 149's fix
added `padding-inline-end` to `.twe-appbar__actions`, but the actual cause is in the FluentUI
v5 `fluent-menu` web component's shadow stylesheet: the slotted popup is positioned

```css
::slotted([popover]) {
  inset-block-start: anchor(end);
  inset-inline-start: anchor(start);   /* left edge = trigger's LEFT edge, grows rightward */
  position-try-fallbacks: flip-block;  /* vertical flip only — never flips inline */
}
```

so a trigger at the far right of the appbar always overflows; the component only right-aligns
in `split` mode (`:host([split]) ::slotted([popover]) { inset-inline-end: anchor(end); }`).

## Requirements

- Popup opens fully inside the viewport, right-aligned to the trigger (standard end-aligned
  profile-menu behavior), in both signed-in and signed-out states.
- Fix is CSS-only, scoped to the profile menu (document-level style overriding the shadow
  `::slotted()` default — light-DOM `fluent-menu-list` is targetable; document styles beat
  `::slotted()` at the cascade). Follow `tw-blazor-css-strategy` tier-2 light-DOM exception.
- No component forks, no `split` attribute misuse (it changes display and adds
  primary-action affordances).

## Checklist

- [x] Scope class on the profile `FluentMenu` + document-level override
      `inset-inline-start: auto; inset-inline-end: anchor(end);` on its `fluent-menu-list`
- [x] `dev build` 0/0
- [x] Visual verification: signed-out avatar click shows the full Sign-in item inside the
      viewport (screenshot)
- [x] Results with How to validate

## Notes

- Component API check: Blazor `FluentMenu` (5.0.0-rc.4-26180.1) exposes no positioning
  parameter; underlying web component honors `positioning` only for tooltips, not menus.
- `anchor()` CSS requires Chromium 125+; non-supporting browsers ignore the override and get
  the component's own fallback behavior — same degradation path as the component default.
- Related: task 149 (first attempt), review disposition was clean because visual placement
  was never re-verified in a live browser — checklist here requires a screenshot.

## Results

**What changed (commit `70397b3b`):**
`source/container-apps/web/projects/web-spa/features/profiles/components/Profile.razor` —
`FluentMenu` gets `Class="{Id}-menu"`, and the component's inline style block adds a
document-level rule on the light-DOM popup:

```css
fluent-menu.{Id}-menu > fluent-menu-list {
  inset-inline-start: auto;
  inset-inline-end: anchor(end);
}
```

This mirrors the web component's own `[split]`-mode right-alignment and outranks its shadow
`::slotted([popover])` default (document styles beat `::slotted()` at the cascade). Works for
both signed-in and signed-out menus (same popup element). RTL-safe via logical properties.

**Root cause:** FluentUI v5 `fluent-menu` anchors the slotted popup at
`inset-inline-start: anchor(start)` with only `flip-block` fallbacks — no inline flip — so a
trigger at the far right of the appbar always pushed the popup past the viewport edge. Task
149's `padding-inline-end: 8px` on `.twe-appbar__actions` could never fix this.

**Gates:**
- `dev build` → Build completed successfully (warnings-as-errors ⇒ 0/0).
- Live visual check (Aspire `dev run`, Chromium via playwright-cli, 1480×900, signed-out):
  avatar click opens the menu fully inside the viewport, right-aligned under the trigger,
  "Sign-in" item fully visible. Screenshot captured 2026-08-05 (playwright session artifact).

### How to validate

**Smoke (UI):**
1. `dev run`, open the web app (web-server URL from the Aspire dashboard).
2. Signed out, click the avatar at the top-right.
3. Expect: the menu (Sign-in) opens fully visible, its right edge aligned under the avatar —
   nothing clipped by the viewport.
4. Sign in with a passkey and repeat: Profile / Settings / Sign out menu also fully visible.

**Automated gate:**
```bash
dev build   # expect 0 warnings / 0 errors
```

**Not in scope:** browsers without CSS anchor positioning (pre-Chromium-125) keep the
component's own fallback behavior — the override is simply ignored there, same as the
component default.

## Session

- Created: Claude (2026-08-05)
- Implementation + visual verification: Claude (2026-08-05), commit `70397b3b`
