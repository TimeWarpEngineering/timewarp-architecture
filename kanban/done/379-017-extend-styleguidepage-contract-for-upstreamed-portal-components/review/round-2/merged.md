# Round 2 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:160
- Description: The inert-sample source scan covered only `@bind-Value` and `ValueChanged`.
- Source: general (round 1)
- Disposition notes: Fixed in e9374c26d. The test counts every `@bind` (2), bans `Changed="`,
  ties `data-qa="ConfirmSample"` to `OnClick="ConfirmSampleAsync"`, and checks that the handler
  calls `NoSubStyleGuideState.ConfirmSample()`. Re-verified against `StyleGuidePage.razor:76,378`.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/infrastructure/static-render-host.cs
- Description: The static-render host was copied verbatim between two test classes.
- Source: general (round 1)
- Disposition notes: Fixed in e9374c26d. `StaticRenderHost` is the only copy. Both page
  contracts call it, and both classes pass (2/2 and 14/14).

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:143
- Description: The role-card checks did not tie the checked state to Owner.
- Source: general (round 1)
- Disposition notes: Fixed in e9374c26d. Each card is sliced. Exactly one card is checked, and it
  is Owner with `tabindex="0"`. Admin and Staff are unchecked with `tabindex="-1"`.

### M4 — Severity: nit — Status: fixed
- File: kanban/to-do/379-017-extend-styleguidepage-contract-for-upstreamed-portal-components/task.md (Results → Expect)
- Description: Expect used the wrong pill-card title.
- Source: general (round 1)
- Disposition notes: Fixed in fd31d8a59. Expect reads "Trend, SLA, and class pills".

## New findings

None on the fix delta.

## Duplicates / conflicts

- None. Single reviewer.
