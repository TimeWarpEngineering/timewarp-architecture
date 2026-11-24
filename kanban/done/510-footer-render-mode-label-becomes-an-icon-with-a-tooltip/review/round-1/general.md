# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/510-footer-render-mode-label-becomes-an-icon-with-a` vs `master`, the footer render-mode change in `TimeWarpPage` and `RenderModeBadge`, the tests that read the slot, and `evidence/`.

## Summary

The footer left slot now shows a FluentUI Regular Size16 icon for `RendererInfo.Name`: Document, Server, and DeveloperBoard, with the name as text when the name is anything else. The slot keeps `twe-footer__mode`, adds `role="img"`, an aria-label of the heading plus the sentence, and `data-qa="RenderMode"` / `data-mode`. An inline `FluentTooltip` carries "Rendering: &lt;name&gt;" and the sentence from the task. A name change replaces the slot (`@key`) and an interactive render fades it in over `--twe-duration-fast` (150ms), with the animation off under `prefers-reduced-motion` and off on the static first paint.

The restatement matches the task. Recorded gates cover the change: `./bin/dev build` 0/0, the badge and TimeWarpPage classes, the new footer Playwright class, the four Playwright classes whose footer waits moved to `data-mode`, and `ganda repo audit` (pass, one advisory outside this change). Evidence shots show the three icons, light and dark, version still at the right, and the Server tooltip copy. Icon fill in those PNGs is the muted token (`#767a86` light, `#adadad` dark). A pixel compare of the tooltip shot against the Server and Static shots (Python PNG decode of `evidence/*.png`, because the tooltip glyph is small) lines the open-tooltip glyph up with the Server icon.

No issues.

## Issues

None.
