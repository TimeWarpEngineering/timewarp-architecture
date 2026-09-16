# Round 1 — merged findings
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
- File: source/container-apps/web/projects/web-spa/components/forms/FormField.razor.css:24
- Description: Isolated `.twe-form-field__control > * { width: 100% }` is rewritten to `.twe-form-field__control > *[b-0phzzyrekv]` and does not match Fluent light-DOM hosts (Wall A). Wrapper width on FormField’s own divs applies; the host stretch rule does not. `Width="100%"` on call sites was already reported as insufficient. The FormField render test asserts the source CSS substring, not a selector that can match a Fluent host.
- Suggestion: Exception B co-located `<style>` targeting Fluent hosts (`fluent-text-input`, `fluent-dropdown` / select / combobox, `fluent-textarea`) from `.twe-form-field__control`. Do not blanket `> *` (Avatar / Checkbox). Keep `Width="100%"` on controls. Assert the Exception B rule, not isolated source `width: 100%` alone.
- Source: general
- Disposition notes: Removed isolated `> *` rule. Exception B `<style>` on FormField.razor targets fluent-text-input / fluent-dropdown / fluent-select / fluent-combobox / fluent-textarea / fluent-field. Test asserts those selectors, rendered style tags, and that isolated CSS no longer contains `> *`. Scoped rewrite confirmed: no child `[b-xxxxx]` width rule.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/forms/FormContainer.razor.css:5
- Description: FormContainer column `gap: var(--twe-space-6)` plus FormActions `margin-top: var(--twe-space-6)` stacks to 48px above the action row. Flex gap does not collapse with the child’s margin. TodoItemFormContainer is the caller; previously `.twe-stack` was 16px with no extra action margin.
- Suggestion: Drop `margin-top` from FormActions. FormContainer gap and FormSection `padding-bottom: var(--twe-space-12)` already own the offset before actions.
- Source: general
- Disposition notes: Dropped `margin-top` from FormActions. FormContainer keeps column gap; FormSection padding-bottom still separates sections from actions.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:142
- Description: Authentication nests FormActions inside FormSection, so the section hairline and 48px padding sit *below* Save. Profile, RoleForm, and Style Guide place FormActions after FormSection.
- Suggestion: Keep tenant/drift outside EditForm; put FormSection around the fields; put FormActions after FormSection (still inside EditForm). Keep `data-qa="AuthenticationSettings"` on prerender (DraftLoaded is false on prerender).
- Source: general
- Disposition notes: Tenant/drift stay outside EditForm. Loaded path: EditForm → FormSection (fields) → FormActions. Prerender (DraftLoaded false) still renders FormSection with `data-qa="AuthenticationSettings"`.

## Duplicates / conflicts

- None. FormSection.razor.css `:33` has the same isolation rewrite on `__body > *`; general treated it as the same Wall A theme under M1, not a separate ID (FormGrid sets its own `width: 100%`).
