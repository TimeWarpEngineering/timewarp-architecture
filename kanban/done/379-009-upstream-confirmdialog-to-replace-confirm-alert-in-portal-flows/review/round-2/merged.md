# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/ai/xai-api-key-state/xai-api-key-state.cs:27
- Description: `ClearDeclined` had an internal setter.
- Suggestion: `{ get; private set; }`.
- Source: general (round 1)
- Disposition notes: Fixed in 812f263a6. The only writers are `Initialize()` and the nested clear handler.

### M2 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai/clear-xai-api-key-confirmation-tests.cs:42
- Description: The No test asserted `standIn.CallCount.ShouldBe(1)` twice.
- Suggestion: Drop the duplicate.
- Source: general (round 1)
- Disposition notes: Fixed in 812f263a6. The test asserts the count once and passes. The class ran 2/2.

## Duplicates / conflicts

- None. This round raised no new findings.
