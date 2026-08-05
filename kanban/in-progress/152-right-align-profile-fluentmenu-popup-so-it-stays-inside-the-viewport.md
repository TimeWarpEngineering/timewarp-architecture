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

- [ ] Scope class on the profile `FluentMenu` + document-level override
      `inset-inline-start: auto; inset-inline-end: anchor(end);` on its `fluent-menu-list`
- [ ] `dev build` 0/0
- [ ] Visual verification: signed-out avatar click shows the full Sign-in item inside the
      viewport (screenshot)
- [ ] Results with How to validate

## Notes

- Component API check: Blazor `FluentMenu` (5.0.0-rc.4-26180.1) exposes no positioning
  parameter; underlying web component honors `positioning` only for tooltips, not menus.
- `anchor()` CSS requires Chromium 125+; non-supporting browsers ignore the override and get
  the component's own fallback behavior — same degradation path as the component default.
- Related: task 149 (first attempt), review disposition was clean because visual placement
  was never re-verified in a live browser — checklist here requires a screenshot.

## Session

- Created: Claude (2026-08-05)
