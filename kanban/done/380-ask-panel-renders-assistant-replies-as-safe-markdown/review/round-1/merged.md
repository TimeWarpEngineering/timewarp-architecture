# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-playwright-tests/ask-markdown-playwright-tests.cs:100
- Description: The browser "no reload" assertion alone cannot tell a State-action route from
  Blazor's default link interception.
- Suggestion: Assert the State path in the browser.
- Source: general
- Disposition notes: wontfix (orchestrator). `Send_App_Link_Clicks_To_The_Callback` proves that
  the click is prevented and goes to the callback. `AgentAsk.OpenAppLinkAsync` is a one-line
  `NoSubRouteState.ChangeRoute` dispatch. The browser test proves the user-visible outcome. Adding
  a browser-side State probe would add test-only surface for no extra protection.

### M2 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/composites/safe-markdown.cs:256
- Description: Ctrl/Cmd+click on an in-app Ask link routes in place and does not open a new tab.
- Suggestion: Accept.
- Source: general
- Disposition notes: wontfix (orchestrator). A Blazor preventDefault is static per element. Making
  it modifier-aware needs JS interop, which the task did not ask for. Middle click still opens a
  new tab.

## Duplicates / conflicts

- None (single reviewer).
