# Round 1 — general
**Date:** 2026-09-17
**Scope reviewed:** branch task/234 vs origin/master (product form primitives + Profile, RoleForm, Authentication, StyleGuide, tokens, tw-blazor, FormField test)

## Summary

Task 234 adds `FormSection` / `FormGrid` / `FormField` / `FormActions`, folds `FormContainer` onto those primitives, adds `--twe-space-2/3/6/12`, and applies the stacked full-width rhythm to Profile, RoleForm, Authentication, and the Style Guide Forms card. TWA0022, `data-qa` hooks, the 233 FluentButton/EditForm rule, and “no Tailwind / no page-local form CSS” hold. The dominant risk is CSS isolation Wall A: the child-host full-width rule is rewritten so it cannot match Fluent light-DOM hosts, and `FormContainer` + `FormActions` stack two `--twe-space-6` spacers on the existing Todo caller.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/forms/FormField.razor.css:24
- Description: Isolated `.twe-form-field__control > * { display: block; width: 100%; … }` does not apply to Fluent hosts at runtime. The Release scoped rewrite (also in `Web.Spa.bundle.scp.css`) is `.twe-form-field__control > *[b-0phzzyrekv]`. Blazor stamps `[b-0phzzyrekv]` only on native elements FormField authors (`div.twe-form-field`, `div.twe-form-field__label`, `div.twe-form-field__control`, `p.twe-form-field__hint`). Child `FluentTextInput` / `FluentSelect` / `FluentCombobox` roots never receive that attribute (tw-blazor-css-strategy Wall A). A native ancestor is present, but without `::deep` the `*` still requires the parent scope id, so the rule is inert. The file comment at FormField.razor.css:1–2 (“Native ancestor reaches Fluent light-DOM hosts”) is therefore false. Wrapper `.twe-form-field` / `__control` `width: 100%` *does* apply to FormField’s own divs; that is necessary so a percentage width has a definite containing block, but it does not set `display: block` or `width: 100%` on the Fluent host. Call sites pass `Width="100%"`, which the task notes already said still rendered narrow. The HtmlRenderer test reads source CSS from disk (`css.ShouldContain("width: 100%")`) and renders an unscoped `<input>` — it cannot see the rewritten `*[b-0phzzyrekv]` selector and would pass even though Fluent hosts do not match. Same rewrite on FormSection.razor.css:33 → `.twe-form-section__body > *[b-01d1w0l2vu]` (less load-bearing because FormGrid sets its own `width: 100%`).
- Suggestion: Exception B, not `::deep`. Co-located `<style>` scoped to the namespaced wrapper, targeting the Fluent light-DOM hosts (e.g. `.twe-form-field__control fluent-text-input`, `fluent-select`, `fluent-combobox`, `fluent-text-area`) with `display: block; width: 100%; max-width: 100%; box-sizing: border-box`. Do not use a blanket `> *` — that would stretch `FluentAvatar` / `FluentCheckbox`. Keep `Width="100%"` on the controls. Assert the *rewritten* selector (or a browser-level host width), not the source `width: 100%` substring.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/forms/FormContainer.razor.css:5
- Description: `FormContainer` is `display: flex; flex-direction: column; gap: var(--twe-space-6)` (24px). It renders `ActionContent` inside `FormActions` (FormContainer.razor:24), and `FormActions` also has `margin-top: var(--twe-space-6)` (FormActions.razor.css:8). Flex gap does not collapse with the child’s margin, so the space above the action row is 48px. The only caller is `TodoItemFormContainer`. Previously the shell was `.twe-stack` (16px gap) and the action row had no extra margin — this is a double `--twe-space-6` regression on that form.
- Suggestion: Keep spacing in one place. Either drop `margin-top` from `FormActions` and let `FormContainer` gap / `FormSection` padding-bottom provide the rhythm, or drop the container gap between main and actions (e.g. no gap on the column, header/main spaced separately). Profile / RoleForm / StyleGuide already sit `FormActions` *after* `FormSection` (hairline + `padding-bottom: var(--twe-space-12)`); they should not pick up 48px on top of that if both spacers remain.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor:142
- Description: Profile, RoleForm, and the Style Guide Forms card place `FormActions` as a sibling *after* `FormSection`, so the section hairline (`border-bottom: 1px solid var(--twe-rule)` plus `padding-bottom: var(--twe-space-12)`) sits above the action row. Authentication wraps tenant line, drift banner, `EditForm`, grid, save-error bar, and `FormActions` all inside one `FormSection`. Tenant/drift stay outside `EditForm` as required, and the native `<select>` / `twe-settings__*` page-local CSS are gone, but the hairline falls *below* Save with 48px padding after the buttons. `:last-of-type` only zeroes `margin-bottom`, not the padding or border.
- Suggestion: Match Profile/RoleForm: `FormSection` around the fields (tenant/drift can remain above the `EditForm`, either inside the section body or just above it); put `FormActions` after the `FormSection` (still inside `EditForm` so submit works).
- Status: open
