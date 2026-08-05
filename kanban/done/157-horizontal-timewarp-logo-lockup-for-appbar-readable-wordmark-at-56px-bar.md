# Horizontal TimeWarp logo lockup for appbar (readable wordmark at 56px bar)

## Description

Follow-up to task 156. With the brand text removed, the stacked `timewarp-logo.svg`
(icon over wordmark over tagline, generous padding) is unreadable at bar sizes: the
"TimeWarp" wordmark is only ~12% of the image height, i.e. ~6px at a 48px logo and
still only ~12px at 100px. Growing the appbar to ~112px was considered and rejected —
same artwork rearranged horizontally is more readable in the existing 56px bar.

## Checklist

- [x] Measure path bounds of `timewarp-logo.svg` (icon box 100.3,49.3 64x67 incl shadow;
      wordmark box 47.6,124.4 169.3x24.6, viewBox units)
- [x] Build `images/timewarp-horizontal.svg`: original defs + art wrapped in
      `<defs><g id="tw-art">`, windowed twice via nested `<svg viewBox>` + `<use>` —
      icon left at full height, wordmark right at 30/64 height, viewBox 0 0 280 64
- [x] Verify render on light and dark bar backgrounds (no opaque background in asset;
      `path939` full-canvas rect is fill:none)
- [x] Swap header `<img>` to the new asset, width=210 height=48 (keeps no-flash fix)
- [x] `dev build` 0/0

## Results

- New asset `source/container-apps/web/projects/web-spa/wwwroot/images/timewarp-horizontal.svg`:
  icon + "TimeWarp" wordmark side by side, built by windowing the original SVG's vectors
  (no redrawing; gradients preserved). Wordmark renders ~22px tall at the appbar's 48px
  logo height — more readable than the stacked logo would be at 100px.
- `TimeWarpPage.razor` brand img now uses the horizontal asset (210x48 attrs, alt
  "TimeWarp Architecture"). Appbar stays 56px.
- ENTERPRISES tagline is cropped out of the appbar windows but the paths remain in the
  asset's defs; original `timewarp-logo.svg` untouched for other uses.

**How to validate:** `dev run`, hard-refresh: header shows icon + readable "TimeWarp"
wordmark at 48px in the 56px bar, no size flash.

## Session

- 2026-08-05 claude: user rejected 34/48px stacked logo as unreadable, proposed 100px logo +
  taller bar; analysis showed stacked lockup geometry is the problem; user chose horizontal
  lockup option. Built asset, wired in, dev build 0/0.
