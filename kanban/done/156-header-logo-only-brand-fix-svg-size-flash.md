# Header: logo-only brand, fix SVG size flash

## Description

The appbar brand block showed the logo plus a two-line "TimeWarp / ARCHITECTURE" text stack.
User wants logo only. Additionally, `images/timewarp-logo.svg` has intrinsic dimensions of
1000x800, so on first paint the `<img>` rendered at full intrinsic size until the shell's
tier-2 `<style>` block applied `height: 34px` — a large, annoying layout flash.

## Checklist

- [x] Remove `twe-brand__text` / `__name` / `__sub` spans and their CSS from `TimeWarpPage.razor`
- [x] Add explicit `width="43" height="34"` attributes to the logo `<img>` so the browser
      reserves the correct box before any CSS applies (kills the flash)
- [x] Update alt text to "TimeWarp Architecture" (text no longer visible, alt carries the name)
- [x] `dev build` 0/0

## Notes

- File: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`
- 43x34 matches the SVG's 1000x800 (1.25) aspect ratio at the appbar's 34px logo height;
  CSS `height: 34px; width: auto` still governs the final rendered size.
- Root cause of the flash is intrinsic-size-before-style, not asset loading speed — width/height
  attributes are the standard fix (same mechanism as image CLS prevention).

## Results

- `TimeWarpPage.razor`: brand block is logo-only; `<img>` carries `width="43" height="34"`
  (matches the SVG's 1000x800 aspect at the 34px appbar logo height) so the browser reserves
  the box before styles apply — no more huge-then-shrink flash. Unused `twe-brand__text` /
  `__name` / `__sub` CSS removed; alt text now "TimeWarp Architecture".
- `dev build` 0/0. Shipped in commit `4d3519ee`.

**How to validate:** `dev run`, open the web app, hard-refresh (Ctrl-Shift-R): header shows only
the logo at 34px with no size flash during load.

## Session

- 2026-08-05 claude: implemented (text removed, size attrs added), dev build 0/0, committed
  `4d3519ee`, marked done.
