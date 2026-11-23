# Round 1 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:236
- Description: No test covers the page's own-save path. `ShouldReRender` keeps the old `Version` on the saved form, because the form's own field changed. Only `AcceptAssistant(stored, force: true)` in `SaveAssistantAsync`, and `AcceptByok(stored, force: true)` in `SaveByokAsync`, then move it to the saved response. The integration tests dispatch the state actions directly and never run these handlers. If someone deleted the force call, every test would still pass, and the next save from that form would get a 409.
- Suggestion: Add `AiPage_Should_` cases that edit a field and run `SaveAssistantAsync` / `SaveByokAsync` on the dispatcher (by reflection, as the test already does for `AssistantCommand`). Assert that the saved form shows the saved name or mode and the saved version, and that an unsaved edit in the other form is kept.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:63
- Description: `AssistantDraftLoaded` and `ByokDraftLoaded` always equal `AssistantSource is not null` and `ByokSource is not null`. They are leftover shadow state, which the task set out to delete.
- Suggestion: Delete both fields and derive them from the source reference, either inline in the markup or as expression-bodied properties.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
