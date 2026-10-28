# Round 1 — merged findings
**Date:** 2026-10-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:136
- Description: The rail gate is the repo's first custom-property container style query. In a browser without support, no rail rule applies. The 880–1279 default rail and the collapse toggle then have no visible effect, and the column stays 240px.
- Suggestion: Name the browser support this needs, and the fallback (the full column), in the tokens.css comment or the TimeWarpPage Design region.
- Source: general
- Disposition notes: wontfix (review oracle). Without support the page falls back to the usable 240px column, not a broken layout. Chromium is proved by the Playwright suite. The task explicitly asked for one copy of the rail rules, and a style query is the mechanism that gives that. A note-only fix does not justify a fix pass. The operator should know about it: if a supported browser lacks custom-property style queries, open a follow-up task for a fallback.

### M2 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.toggle-menu.cs:18
- Description: The `ToggleMenuActionSet` rename puts ToggleMenu on the derived action roster, so the Ctrl-K palette lists it. The Design region does not mention this.
- Suggestion: Record whether the palette entry is intended, or opt out with `[CommandPaletteOptOut]`.
- Source: general
- Disposition notes: wontfix (review oracle). The palette entry gives keyboard users a way to reach the toggle. It follows the repo's derive-don't-list convention, and no requirement forbids it. A palette run below 880px only stores a preference.

## Duplicates / conflicts

- None. One reviewer.
