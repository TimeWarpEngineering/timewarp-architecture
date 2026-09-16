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

- [ ] Authentication page uses EditForm + FluentButton submit like RoleForm
- [ ] Tests updated; `dotnet test` for the SPA/prerender suites green
- [ ] tw-blazor rule added (or SSOT location recorded if the file is managed)
- [ ] `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate (screenshot-equivalent: Save renders as a primary button)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Reference: `RoleForm.razor` L36 (`EditForm … OnValidSubmit … FormName`), L50 (`FluentButton
  Type=ButtonType.Submit Appearance=ButtonAppearance.Primary data-qa="RoleSave"`).
- Current save control: `AuthenticationPage.razor` ~L128-133 (`<button … @onclick="SaveAuthenticationAsync">`).
- Prior: 225 (page created), 227 (trusted tenants removed from the page).
- Do not change the personal `/Settings` row actions in this task (229 owns that page).

## Results

_Pending._

### How to validate

_Pending._
