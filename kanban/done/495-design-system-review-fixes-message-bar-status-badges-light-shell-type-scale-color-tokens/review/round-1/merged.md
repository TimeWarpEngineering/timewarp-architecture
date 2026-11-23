# Round 1 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:62
- Description: `--twe-danger-tint` is declared only on `:root`, so it is computed from the
  light `--twe-danger` and `--twe-paper` and is not re-declared in `body[data-theme="dark"]`.
  In dark mode the error message bar and every `twe-badge--error` badge get a near-white pink
  fill under light-pink `#ef9a9a` text. The badge was correct in dark before this change (the
  mix was inline in the badge rule). Visible in `assets/08-after-message-bars-dark.png`.
- Suggestion: add `--twe-danger-tint: color-mix(in srgb, var(--twe-danger) 12%, var(--twe-paper));`
  to the dark block beside the positive, warning, and info tints; update the tokens.css
  comment; recapture the dark message-bar shot and check a dark error badge.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
