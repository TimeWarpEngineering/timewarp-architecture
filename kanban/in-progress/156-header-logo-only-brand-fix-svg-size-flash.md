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
- [ ] `dev build` 0/0

## Notes

- File: `source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor`
- 43x34 matches the SVG's 1000x800 (1.25) aspect ratio at the appbar's 34px logo height;
  CSS `height: 34px; width: auto` still governs the final rendered size.
- Root cause of the flash is intrinsic-size-before-style, not asset loading speed — width/height
  attributes are the standard fix (same mechanism as image CLS prevention).

## Session

- 2026-08-05 claude: implemented (text removed, size attrs added), build gate pending.
