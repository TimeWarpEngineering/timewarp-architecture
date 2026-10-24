# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:160
- Description: The inert-sample source scan checks only `@bind-Value` and the literal
  `ValueChanged`. Any other `@bind` form, any other `…Changed=` callback, or a confirm button that
  no longer calls `ConfirmSampleAsync` would still pass.
- Suggestion: Expect exactly 2 `@bind` occurrences of any kind, and ban `Changed="`. Tie
  `data-qa="ConfirmSample"` to `OnClick="ConfirmSampleAsync"`, and tie `ConfirmSampleAsync` to
  `NoSubStyleGuideState.ConfirmSample()`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:328
- Description: The static-render host is a verbatim copy of the one in `TimeWarpPage_Should_`
  (`CreateServices`, `HeadlessJsRuntime`, `FixedAuthenticationStateProvider`, `Title`,
  `CountOpen`). The Design note says it is "the same host", but nothing keeps the two copies in step.
- Suggestion: Extract a shared static-render host into `infrastructure/` and use it from both
  test classes.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:151
- Description: The role-card assertions do not tie the checked state and `tabindex="0"` to Owner.
  They also do not show that Admin and Staff are unchecked, which Results claims.
- Suggestion: Check each card's slice, or count exactly one `aria-checked="true"` and require it
  before "Admin".
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: kanban/to-do/379-017-extend-styleguidepage-contract-for-upstreamed-portal-components/task.md (Results → Expect)
- Description: Expect names the card "Trend / SLA / class pills". The rendered title is "Trend,
  SLA, and class pills".
- Suggestion: Use the rendered title in Expect.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
