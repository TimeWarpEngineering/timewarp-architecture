# Admin Authentication page: use the EditForm and FluentButton form pattern; document form vs inline-action rule in tw-blazor

## Description

Live review 2026-09-16 (Steve): on `/Admin/Authentication` the "Save authentication settings"
action is a plain `<button>` styled as a link. The repo already has a form pattern — the admin
roles form (`web-spa/features/admin/roles/components/RoleForm.razor`): `EditForm` bound to the
contract model, `OnValidSubmit`, and `FluentButton Type=ButtonType.Submit
Appearance=ButtonAppearance.Primary` for Save with a `data-qa` hook. The Authentication page
(225) did not follow it. Personal `/Settings` uses link-styled buttons for inline row actions
("Delete", "Unlink", "Create a passkey", "Link Microsoft 365"); that is a different case
(row actions, not a form submit) and stays.

## Requirements

- `web-spa/features/admin/site-settings/pages/AuthenticationPage.razor` (+ `.razor.cs`):
  wrap the policy fields in an `EditForm` bound to a small edit model (offer sign-in, allow
  bootstrap, passkey prompt mode, version), `OnValidSubmit` → existing `SiteSettingsState`
  update action; Save is `FluentButton Type=ButtonType.Submit Appearance=ButtonAppearance.Primary`
  with `data-qa="AuthenticationSave"`, disabled while busy; keep the read-only "App registration
  tenant" line and the Enabled/AllowBootstrap drift banner outside the form. Match RoleForm's
  layout/spacing classes; keep CSS placement per `tw-blazor-css-strategy`.
- Add a "Form vs inline action" rule to the repo Blazor skill
  (`skills/tw-blazor/SKILL.md`): a page or component that edits a model and persists it uses
  `EditForm` + `FluentButton` submit (Primary for the main action, Neutral for Cancel); link-styled
  buttons are for inline row actions only (delete/unlink/add on a list item). Point at
  `RoleForm.razor` as the reference. If that skill file is a managed copy synced from
  timewarp-flow (check for a `*.ganda-source` marker or sync note), do **not** hand-edit it —
  record in Results that the rule must be added at the SSOT in timewarp-flow instead.
- Update the SPA/prerender tests from 225 that click the save control (selector by `data-qa`).

## Checklist

- [x] Authentication page uses EditForm + FluentButton submit like RoleForm
- [x] Tests updated; `dotnet test` for the SPA/prerender suites green
- [x] tw-blazor rule added (or SSOT location recorded if the file is managed)
- [x] `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate (screenshot-equivalent: Save renders as a primary button)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok session (2026-09-16)

## Notes

- Reference: `RoleForm.razor` L36 (`EditForm … OnValidSubmit … FormName`), L50 (`FluentButton
  Type=ButtonType.Submit Appearance=ButtonAppearance.Primary data-qa="RoleSave"`).
- Current save control: `AuthenticationPage.razor` ~L128-133 (`<button … @onclick="SaveAuthenticationAsync">`).
- Prior: 225 (page created), 227 (trusted tenants removed from the page).
- Do not change the personal `/Settings` row actions in this task (229 owns that page).

## Results

Admin `/Admin/Authentication` now follows the RoleForm persist pattern. Policy fields bind
`ISiteSettingsDetails` (`UpdateSiteSettings.Command`, including concurrency `Version`) through
`EditForm` + `OnValidSubmit`. Save is `FluentButton Type=ButtonType.Submit
Appearance=ButtonAppearance.Primary data-qa="AuthenticationSave"`, disabled while
`ActionTrackingState.IsActive`. The app-registration tenant line and Enabled/AllowBootstrap
drift banner stay outside the form. Layout uses `FluentStack Orientation=Vertical VerticalGap="16"`
like RoleForm. Personal `/Settings` row actions were not changed.

`skills/tw-blazor/SKILL.md` is a first-class file in this repo (no `*.ganda-source` marker).
Added a **Form vs inline action** rule there: persist-a-model → EditForm + FluentButton submit
(Primary main, Default for Cancel — FluentUI v5 name for v4 Neutral); link-styled buttons are
inline row actions only. Reference: `RoleForm.razor`.

### Files changed

- `source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor` (+ `.razor.cs`)
- `skills/tw-blazor/SKILL.md`
- `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs` — save hook `SaveAuthenticationSettings` → `AuthenticationSave`

### Test outcomes

- `dotnet run tools/dev-cli/dev.cs -- build` — 0 Warning / 0 Error
- `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class AuthenticationPage_Should_` — 2 passed
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Admin_Authentication_Html` — 2 passed
- `cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Ok_Page_Given_Passkey_Member_Settings_Html` — 1 passed
- `ganda repo audit` — blocking checks pass (advisory: memsearch-scaffold, vscode-window-icon; pre-existing)

### How to validate

**Smoke**

1. From the repo root: `dotnet run tools/dev-cli/dev.cs -- run` (or `dev run` if `bin/dev` is on PATH).
2. Sign in as an Administrator (needs `settings.write`).
3. Open `/Admin/Authentication`.
4. After the site-settings snapshot loads, the offer-sign-in / allow-bootstrap / passkey-prompt
   controls sit in a form. **Save authentication settings** is a filled primary Fluent button,
   not a blue link. In DevTools the control has `data-qa="AuthenticationSave"`.
5. The "App registration tenant" line and any "Configuration drift" banner sit **above** the
   form, not inside it.

**Expect**

- Save is `fluent-button` with primary appearance; it disables while a save is in flight.
- Submitting persists via the existing `UpdateSiteSettings` action (same fields as before).
- `/Settings` still uses link-styled **row** actions (Delete / Unlink / Create a passkey /
  Link Microsoft 365). Those are out of scope.

**Automated**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class AuthenticationPage_Should_
# expect: 2 passed

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Admin_Authentication_Html
# expect: 2 passed (Member 403; Administrator 200 with AuthenticationSettings + ConfigurationTenant)

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Ok_Page_Given_Passkey_Member_Settings_Html
# expect: 1 passed; Settings HTML does not contain data-qa="AuthenticationSave"
```

**Not in scope:** live WebAuthn / Entra click-through of Save (prerender HTML does not include
the form until the interactive snapshot loads; proof of the primary button is the Smoke steps).
