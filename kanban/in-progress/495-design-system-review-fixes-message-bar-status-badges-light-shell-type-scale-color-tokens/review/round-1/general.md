# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** `master...task/495-design-system-review-fixes-message-bar-status` (product diff under `source/` and `tests/`, task.md Results and How to validate, after screenshots in `assets/08-after-*.png`)

## Summary

The change covers all seven approved items. Message bars get a `display: contents` anchor that
rebinds Fluent's intent tokens to the `--twe-*` status tints and default to SingleLine. The Lost
sample becomes Churned, with snippet and tests updated. The app bar and Profile label take
`--twe-ink`, and the nav rest fill is transparent in both themes. Shared `twe-type-*` role
classes in `tokens.css` now drive both the real components and the Type Scale rows, with the KPI
eyebrow chosen as the single eyebrow. Color chips get a stronger ring, a computed value line, and
three status swatches. A small on-demand JS module resets the shell scroller on part change and
reads computed token colors. The catalog height is the viewport space left at page top.

The goal restatement matches the task. The recorded gates (build 0/0, StatusBadge 3/3,
DesignSystemPage 6/6, audit exit 0, browser pass in both themes) cover the change. One
regression: the new `--twe-danger-tint` token is declared only on `:root`, so in dark mode it
keeps the light-theme value. The implementer's own dark screenshot shows it.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:62 (also the dark block at 220–258, StatusBadge.razor.css:407, MessageBar.razor.css:107–112)
- Description: `--twe-danger-tint: color-mix(in srgb, var(--twe-danger) 12%, var(--twe-paper))`
  is declared on `:root` only. A custom property substitutes its `var()` references on the
  element that declares it, so `<html>` computes it from the light `--twe-danger` (#c2342a) and
  light `--twe-paper` (#ffffff). `body[data-theme="dark"]` overrides `--twe-danger` and
  `--twe-paper` but not the tint, so body descendants inherit the light pink fill. The dark
  block already re-declares the positive, warning, and info tints for this reason, and the
  comment at tokens.css:219 says the same about `--twe-scrim`. Result in dark: the error
  message bar and every `twe-badge--error` badge are a near-white pink fill under light-pink
  `--twe-danger` (#ef9a9a) text, which is unreadable. Before this change the badge rule did the
  `color-mix` inline, so it resolved per element and dark was correct; this is a regression for
  the badge, not only a new-bar defect. `assets/08-after-message-bars-dark.png` shows the Error
  bar as a light pink box with faded pink text and a faded Copy icon.
- Suggestion: re-declare `--twe-danger-tint: color-mix(in srgb, var(--twe-danger) 12%, var(--twe-paper));`
  in the `body[data-theme="dark"]` block, next to the other three tints (12% keeps the
  "clears 4.5:1 on a 12% tint" note at tokens.css:218 true). Update the comment at
  tokens.css:87–88 so it says the dark block re-declares it. Recapture
  `08-after-message-bars-dark.png` and check a dark error badge (Status Badges part) reads.
- Status: open

## Checked and not raised

- MessageBar wrapper div: no other stylesheet or test selects `fluent-message-bar` as a direct
  child of `.twe-message-bars`; `display: contents` keeps the flex column intact.
- `BaseComponent` does not override `OnAfterRenderAsync`, so the new override on
  `DesignSystemPage` drops nothing.
- Type roles: the isolated rules now set only margins, so `tokens.css` role classes are not
  fought by scoped CSS. `.twe-page__description` keeps its muted color, and `twe-type-body`
  sets no color by design.
- `DesignSystemPageJsModule` placement follows the existing `services/*-js-module.cs` +
  `source/features/*.ts` pattern; both callers carry `[DirectComponentSideEffect]`.
- Catalog `max-height` is a hand sum of the header stack. It is commented and unset below the
  879px breakpoint; not raised.
