# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/xai-api-key-state/xai-api-key-state.cs:27
- Description: `ClearDeclined` has an internal setter (`{ get; set; }`). Any Web.Spa component
  or service could change `XaiApiKeyState` outside an action. The other flags on the state
  (`SaveFailed` and the rest) use `{ get; private set; }`.
- Suggestion: change it to `{ get; private set; }`. The only writer is the nested
  `ClearXaiApiKeyActionSet.Handler`, which can still set it.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai/clear-xai-api-key-confirmation-tests.cs:42
- Description: `standIn.CallCount.ShouldBe(1);` appears twice in the No test (lines 42 and 45).
- Suggestion: drop the duplicate.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
