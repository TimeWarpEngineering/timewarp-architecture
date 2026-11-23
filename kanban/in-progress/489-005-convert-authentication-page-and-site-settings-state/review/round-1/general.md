# Round 1 — general
**Date:** 2026-11-23
**Scope reviewed:** same as framework (`182192de9..HEAD`, all 20 files), plus call sites of the removed
`SiteSettingsState` members and of `ReviseSiteSettings`.

## Summary

`SiteSettingsState` now holds only `GetSiteSettings.Response? SiteSettings` plus the save flags and a
lookup result (`ResolvedTenant`, `AcceptedTenantsMessage`, `TenantLookupCount`). The flattened fields,
`DraftAcceptedTenants`, and the add/remove tenant actions are gone. No remaining reference to them in
`source/`, `tests/`, or `skills/` (`rg` for the removed members and action sets returns nothing outside
`kanban/`). The Authentication page copies `UpdateSiteSettings.Command.From(stored)` on load, Cancel,
and after its own successful save, reconciles later responses in `ShouldReRender` (the same pattern
AiPage took in 489-004), and applies each lookup result once by comparing `TenantLookupCount`. Readers
(`TimeWarpPage` passkey gate, `AddPasskeyPrompt`, AiPage host-managed line, drift banner, agent page
facts) read the response. Risk is low. The one theme is cosmetic shape in the agent facts.

Checked and found correct:

- Own save: during the action `ShouldReRender` sees a dirty command and changed policy fields and
  leaves it alone; `HandleValidSubmit` then forces `From(stored)`. A failed save (409) leaves
  `SiteSettings` unchanged, so the reconcile is a no-op and the edit stays, as before.
- Assistant-name / key-policy save during an edit: `ReviseSiteSettings` reuses the same tenant list
  and policy fields, so `SamePolicyFields` holds and only `Version` moves. Covered by
  `Keep_An_Unsaved_Edit_And_Take_The_New_Version`; the changed-policy case by
  `Keep_The_Old_Version_When_The_Stored_Policy_Changes`.
- `EntraAcceptedTenant` is a `sealed record`, so `SequenceEqual` in `SameTenants` is value equality.
- Add / Remove / lookup always assign a new list to `Command.EntraAcceptedTenants`; nothing mutates
  the stored response's list in place, so the store stays unchanged until Save (asserted in
  `Apply_A_Looked_Up_Tenant_To_The_Command` and `Add_And_Remove_Edit_The_Command`).
- Cancel and forced loads set `AppliedTenantLookupCount` to the current count, so a stale lookup is
  not re-applied after Cancel.
- `LookUpAsync` keeps the domain text on failure: `ApplyLookup` runs from `ShouldReRender` inside the
  dispatch, before the `await` returns. The page test that renders the looked-up row depends on that
  path, since the test helper's plain `StateHasChanged` never calls `ApplyLookup`.
- Add / Remove / Cancel as page methods: tw-blazor exempts editing and resetting a form draft, and
  the task asked for the tenant draft to leave the store.
- Tests: the two lookup tests that exercised the removed store merge now test the moved static
  helpers with the same inputs and expected messages, which is not a weakened check. The agent-facts
  assertion compares against the stored response in both the null and loaded branches.
- Recorded gates in `## Results` cover the change: build 0/0, each new or edited test class run by
  filter with counts, the edited Aspire-booting method run alone, and the audit. CI runs the full
  project and the default-variant template smoke.
- Goal restatement matches the task Description; no misread.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.agent-page-facts.cs:43
- Description: The no-snapshot facts are built in two steps. `entraSignInEnabled`, `entraAllowBootstrap`,
  and `version` start as `null` and are overwritten with `JsonValue.Create` when a response exists.
  `JsonNode` already has implicit conversions from `bool?` and `long?` (the removed code relied on the
  `bool?` one), so `settings?.EntraSignInEnabled` etc. would do it in one initializer.
  `entraAcceptedTenants` is `[]` rather than `null` with no snapshot, while the Design line says a
  missing response writes null facts.
- Suggestion: Use `settings?.X` for all scalar facts in the initializer. Either write `null` for the
  tenants when `settings` is null, or say in the Design line that the list is empty.
- Status: open
