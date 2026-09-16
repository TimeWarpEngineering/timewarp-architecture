# Round 2 — merged findings
**Date:** 2026-09-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/forms/FormField.razor:42
- Description: Isolated child-host width rule is gone. Exception B `<style>` stretches Fluent field hosts from `.twe-form-field__control`. Compiled scoped CSS has no `> *[b-…]` width rule. Render test asserts Exception B selectors and rendered style, not source `width: 100%` alone.
- Suggestion: None.
- Source: general
- Disposition notes: Re-verified in round 2. Runtime match is `.twe-form-field__control > fluent-field` (v5 TextInput/Select/Combobox wrap in FluentField). Extra host tags are unused but harmless.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/forms/FormActions.razor.css:2
- Description: `margin-top` removed from FormActions. FormContainer column gap and FormSection padding-bottom own the offset before actions.
- Suggestion: None.
- Source: general
- Disposition notes: Re-verified in round 2.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:106
- Description: Tenant/drift stay outside EditForm. FormActions follows FormSection. Prerender else branch still emits `data-qa="AuthenticationSettings"`.
- Suggestion: None.
- Source: general
- Disposition notes: Re-verified in round 2.

## Duplicates / conflicts

- None. No new findings on the fix delta. Prior M# IDs carried forward.
