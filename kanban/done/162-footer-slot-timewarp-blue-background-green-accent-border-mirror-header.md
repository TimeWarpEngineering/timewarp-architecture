# Footer slot TimeWarp blue background + green accent border (mirror header)

## Description

Mirror the header chrome treatment on the footer:

- **Header** (existing): FluentLayout header slot uses brand purple (`--colorBrandBackground`
  from `MainLayout` theme); white `.twe-appbar` sits inside with a **green accent border on
  the bottom** (`3px solid var(--twe-green)`). Purple peeks as a frame via Fluent's 8px slot
  padding.
- **Footer** (this task): FluentLayout footer slot currently uses neutral
  `--colorNeutralBackgroundDisabled` and `.twe-footer` has a 1px neutral top rule. Change to
  **TimeWarp blue slot background** + **green accent border on top** (same 3px green), so
  header bottom / footer top accents bookend the page.

## Requirements

1. Override footer layout item background to `var(--twe-blue)` under `.twe-shell`.
2. Replace footer `border-top: 1px solid var(--twe-rule)` with `3px solid var(--twe-green)`.
3. Leave header as-is (green border-bottom already correct).
4. Keep tokens-only colors (no hard-coded hex in shell styles).

## Checklist

- [x] Create task and move to in-progress
- [x] Override Fluent footer slot background to `--twe-blue` in `TimeWarpPage.razor`
- [x] Footer green accent `border-top` (3px, mirror header)
- [x] Document header/footer chrome pairing in shell CSS comments
- [x] Commit task + CSS

## Notes

- Source map (read-only investigation): Fluent CSS sets
  `.fluent-layout-item[area=header] { background-color: var(--colorBrandBackground) }` and
  `.fluent-layout-item[area=footer] { background-color: var(--colorNeutralBackgroundDisabled) }`.
  Our shell styles live in the tier-2 `<style>` block of
  `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`.
- Blue token: `--twe-blue: #0085b2` in `wwwroot/css/tokens.css`.
- Header green bottom border was already correct; left unchanged.

## Results

Footer chrome now mirrors the header treatment using logo colors: blue frame + green
top accent (header remains purple frame + green bottom accent).

### Changed

- `TimeWarpPage.razor` shell styles:
  - `.twe-shell .fluent-layout-item[area=footer] { background-color: var(--twe-blue); }`
  - `.twe-footer` `border-top`: `1px solid var(--twe-rule)` → `3px solid var(--twe-green)`
  - Comments documenting the purple/blue frame + green accent pairing

### How to validate

**Smoke**

1. `dev run` (or existing Aspire host) → open any page with the app shell.
2. **Header:** purple frame around white appbar; **green** line along the **bottom** of the bar.
3. **Footer:** **blue** frame around white footer bar; **green** line along the **top** of the bar.
4. Footer content (render mode, spinner, version) still readable on white paper.

**Automated**

- No automated chrome assertion; CSS-only. Optional: `dev build` if other work is co-committed.

## Session

- Investigation + implement: grok (2026-08-05)
