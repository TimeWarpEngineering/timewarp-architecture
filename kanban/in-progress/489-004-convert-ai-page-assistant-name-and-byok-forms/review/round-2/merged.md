# Round 2 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs:235
- Description (round 1): no test ran the page's own save path, so deleting the forced accept would not fail any test.
- Disposition notes: fixed in 44a850b72. `Move_The_Assistant_Form_To_The_Saved_Response_And_Keep_A_Byok_Edit` and `Move_The_Byok_Form_To_The_Saved_Response_And_Keep_A_Name_Edit` invoke `SaveAssistantAsync` and `SaveByokAsync`. They assert the saved version, assert the old version is gone, and check that the other form keeps its unsaved edit. Removing either forced accept would leave the old version and fail the test.
- Source: general

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:264
- Description (round 1): `AssistantDraftLoaded` and `ByokDraftLoaded` repeated the source reference.
- Disposition notes: fixed in 44a850b72. Both flags are deleted, and the markup now tests `AssistantSource is not null` and `ByokSource is not null`.
- Source: general

## New findings

None.

## Duplicates / conflicts

- None. There was a single reviewer.
