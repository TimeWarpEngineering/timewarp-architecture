# Round 2 — general
**Date:** 2026-09-17
**Scope reviewed:** re-verify M1–M3 and scan fix delta

## Summary

Uncommitted fix delta on `FormField`, `FormActions`, `AuthenticationPage`, and the FormField render test addresses M1–M3. Isolated FormField child-host width is gone; Exception B `<style>` targets v5 Fluent hosts from `.twe-form-field__control`; compiled `FormField.razor.rz.scp.css` has no `> *[b-…]` width rule. FormActions no longer stacks a second `--twe-space-6` on FormContainer. Authentication places FormActions after FormSection, keeps tenant/drift outside EditForm, and still emits `data-qa="AuthenticationSettings"` when `DraftLoaded` is false. No new defects in the delta.

## Prior IDs

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/forms/FormField.razor:42
- Description: Isolated `.twe-form-field__control > *` is gone from `FormField.razor.css` (wrapper `.twe-form-field` / `__control` stay `width: 100%` at FormField.razor.css:3–22). Exception B co-located `<style>` at FormField.razor:42–58 uses the verbatim `@(@"…")` pattern and lists `fluent-text-input`, `fluent-dropdown`, `fluent-select`, `fluent-combobox`, `fluent-textarea`, `fluent-text-area`, and `fluent-field` as **direct** children of `.twe-form-field__control`. Compiled `source/container-apps/web/projects/web-spa/obj/Release/net10.0/scopedcss/components/forms/FormField.razor.rz.scp.css` (and `Web.Spa.styles.css`) rewrites only FormField’s own elements with `[b-0phzzyrekv]`; there is no `> *[b-…]` child width rule. FluentUI v5 (`5.0.0-rc.5-26219.1`) select/combobox hosts are `fluent-dropdown` (not `fluent-select` / `fluent-combobox`); TextInput/Select/Combobox/TextArea/Checkbox all wrap in `FluentField` (`<fluent-field>`), so the selector that matches at runtime is `.twe-form-field__control > fluent-field`. Fluent’s own bundle already sets `fluent-text-input{width:100%}` and `fluent-dropdown{width:100%}`; `display:block; width:100%` on the field host is what breaks the shrink-to-content cycle. Avatar is `fluent-avatar` (not `fluent-field`) and is left intrinsic. The render test (`form-field-render-tests.cs:28–66`) asserts isolated CSS has no `> *`, Exception B source contains `fluent-text-input` / `fluent-dropdown` plus `width: 100%`, and the HtmlRenderer output includes those host names from the style tag — not source `width: 100%` alone.
- Suggestion: None. Extra unused tags (`fluent-select`, `fluent-combobox`, `fluent-text-area`) are harmless.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/forms/FormActions.razor.css:2
- Description: `margin-top: var(--twe-space-6)` is removed from `.twe-form-actions` (FormActions.razor.css:2–8; compiled `FormActions.razor.rz.scp.css` has no margin-top). FormContainer still owns `gap: var(--twe-space-6)` (FormContainer.razor.css:2–6). FormSection still owns `padding-bottom: var(--twe-space-12)` (FormSection.razor.css:2–6). Design comment at FormActions.razor:6–7 documents the split. TodoItemFormContainer therefore gets a single 24px flex gap above actions; Profile / RoleForm / Authentication / Style Guide keep the section hairline + 48px padding above FormActions.
- Suggestion: None.
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:106
- Description: Tenant line (`data-qa="ConfigurationTenant"`, AuthenticationPage.razor:91–94) and the drift banner (96–104) sit outside `EditForm`. Loaded path is `EditForm` → `FormSection` (fields + save-error bar, `data-qa="AuthenticationSettings"` at 110–112) → `FormActions` (143–150, still inside EditForm so submit works). `DraftLoaded` defaults false; the else branch at 153–157 still renders `FormSection` with `data-qa="AuthenticationSettings"` for prerender. Hairline / `padding-bottom: var(--twe-space-12)` now sit above Save, matching Profile / RoleForm / Style Guide.
- Suggestion: None.
- Status: fixed

## Issues
