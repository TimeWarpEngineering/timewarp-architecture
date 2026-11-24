# RolePage back to create-only: the New Role page must not host an Administrator edit form

## Description

`/Admin/Roles/New` (`RolePage.razor`, title "New Role") renders two columns: a Create form and
an Edit form bound to the Administrator role fetched with `GetRole`. Task 489-003 (commit
`efb40c8e1`) added the Edit column because its spec said "Create and edit sit side by side on New
Role". That spec sentence was wrong: it described a demo arrangement, not the product page. A
page titled New Role that edits the Administrator role is incorrect. Three review rounds passed
it with no browser opened and no screenshot (489-003 Results: "The SPA was not exercised in a
browser in this walk").

The role edit form already lives where it belongs: `RoleDetailPage.razor` line 160 renders
`RoleEditForm` for callers who can manage the role.

## Requirements

- `RolePage` returns to create-only, as before `efb40c8e1`: one `Card` with `RoleForm`, the
  subtitle "Create a custom role.", no `GetRole` call in `OnInitializedAsync`, no `EditRole`
  property, no `PageSplit`. Remove `typeof(RoleState.GetRoleActionSet.Action)` from its
  `[Page(AgentActions=…)]` if nothing else on the page needs it; keep `CreateRole`.
- `RoleEditForm` stays on `RoleDetailPage`. Keep the 489-003 working-copy behavior there (Cancel
  resets to the fetched snapshot; agent rename of the open role rebinds).
- Move or rewrite any 489-003 tests that assert the side-by-side layout so they assert the detail
  page instead. The 489-003 "How to validate" steps that open `/Admin/Roles/New` to change the
  Administrator name now run on `/Admin/Roles/{AdministratorId}`.
- Correct the 489-003 kitchen text: strike "side by side on New Role" in its acceptance and
  validate sections with a one-line note pointing here.
- Evidence: screenshots of `/Admin/Roles/New` and `/Admin/Roles/{AdministratorId}` after the
  change in `evidence/`, taken from the running app (`dev run`), signed in as Administrator.

## Checklist

- [ ] `RolePage` create-only; `RolePage.razor` Design comment updated
- [ ] Edit flow verified on `RoleDetailPage`; tests moved
- [ ] 489-003 kitchen text corrected
- [ ] Two screenshots in `evidence/`
- [ ] `dev build` 0/0, web-spa integration tests, Playwright roles tests, `ganda repo audit` green

## Depends on

- 489-003

## Session

- Created: 2026-11-24 from Steve's review of the New Role page. The 489-003 spec wording was
  approved through this cockpit; the error is in the spec, not the implementer.
