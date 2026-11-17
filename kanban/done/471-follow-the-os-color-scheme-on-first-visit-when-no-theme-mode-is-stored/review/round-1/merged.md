# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-blazor-css-strategy/SKILL.md:70 (rules 8 and 9)
- Description: The shipped skill still says App.razor renders `data-theme="light"` and that Fluent sets
  `body[data-theme="dark"]`. After task 471 the server renders `data-theme="system"`, and
  BrandThemeScript (first child of body) sets the initial mode from the stored mode, else the OS
  preference. An agent following the skill would bring back the light flash on a dark OS.
- Suggestion: Rewrite rules 8 and 9 to describe the new first-paint contract (no mode stored, script
  placement first in body), optionally point `tw-blazor-layout` line 34 at it, and run `dev lint-skills`.
- Source: general
- Disposition notes: Fixed in the round-2 fix pass (2026-11-04). Rule 8 now describes
  `data-theme="system"` on the server render, `BrandThemeScript` as the first child of `<body>`
  (stored mode, else `prefers-color-scheme`, never stores a mode), and why it must stay first.
  Rule 9 says the script sets `body[data-theme="dark"]` on first paint and Fluent changes it later.
  `tw-blazor-layout` step 1 points at the script. `dev lint-skills` passes (9/9).

## Duplicates / conflicts

- None (single reviewer).
