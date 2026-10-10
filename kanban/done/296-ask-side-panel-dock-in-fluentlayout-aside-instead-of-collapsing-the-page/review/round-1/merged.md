# Round 1 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/css/app.css:28 (Ask header actions, AskAnswerBar)
- Description: Icon-only Ask buttons rendered their icons as specks; the global `svg { max-width: 100% }`
  reset resolved against the shrink-to-fit start slot. Tests only checked aria-label/title.
- Suggestion: Exempt the slotted svg in Ask CSS; assert rendered icon size.
- Source: general
- Disposition notes: Fixed. `AgentAsk.razor.css` and `AskAnswerBar.razor.css` add
  `::deep fluent-button > svg { max-width: none }` under their action containers.
  `AssertIconButtonAsync` now requires the icon box to be at least 16×16. Playwright class passed;
  refreshed `ask-docked-1280.png` shows full-size icons.

### M2 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:243
- Description: Screenshot path is hard-wired to the `296-*` task folder.
- Suggestion: Leave as is.
- Source: general
- Disposition notes: wontfix (orchestrator). Established per-task evidence pattern shared with
  `ask-sign-in` and `first-paint` tests; Requirement 5 asks for shots beside this task.

## Duplicates / conflicts

- None.
