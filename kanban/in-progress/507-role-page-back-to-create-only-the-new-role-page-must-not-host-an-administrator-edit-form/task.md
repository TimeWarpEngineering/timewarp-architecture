# RolePage back to create-only: the New Role page must not host an Administrator edit form

## Goal (implementer restatement)

`/Admin/Roles/New` creates a role and does nothing else. It shows one Card with `RoleForm` and
the subtitle "Create a custom role.". It does not fetch the Administrator role, has no edit
column, no `PageSplit`, and its agent actions are `CreateRole` only (the 489-003 `UpdateRole`
entry goes; the page never listed `GetRole`). Editing a role's name and description stays on
`/Admin/Roles/{RoleId}` through `RoleEditForm`, with the 489-003 working-copy behavior intact.

Acceptance:

- `RolePage.razor` / `.razor.cs` match the pre-`efb40c8e1` create-only page, with Design text
  that says why the edit column is gone.
- No test asserts the side-by-side layout; any that did now assert the detail page.
- The 489-003 kitchen strikes "side by side on New Role" in Acceptance and How to validate,
  with a one-line note pointing to 507; its validate steps run on `/Admin/Roles/{AdministratorId}`.
- Two screenshots from the running app (signed in as Administrator) in `evidence/`.
- `dev build` 0/0, role-related tests pass, `ganda repo audit` green.

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

- [x] `RolePage` create-only; `RolePage.razor` Design comment updated
- [x] Edit flow verified on `RoleDetailPage`; tests moved
- [x] 489-003 kitchen text corrected
- [x] Two screenshots in `evidence/`
- [x] `dev build` 0/0, web-spa integration tests, Playwright roles tests, `ganda repo audit` green

## Results

`RolePage` is create-only again. `RolePage.razor` is one `Card` with `RoleForm` and the subtitle
"Create a custom role.". It has no `OnInitializedAsync`, no `GetRole` call, no `EditRole`, and no
`PageSplit`. Its Purpose and Design comment say why. `RolePage.razor.cs` lists only
`CreateRoleActionSet.Action` in `[Page(AgentActions=…)]`. The 489-003 version listed
`UpdateRoleActionSet.Action`, not `GetRole`, and that entry is removed. `RoleDetailPage` already
lists `UpdateRole`, so the agent rename tool is still offered on the detail page. `RoleEditForm`
and `RoleDetailPage` behavior is unchanged. The `RoleEditForm` Design comment now says it is
hosted only by `RoleDetailPage`.

Tests: no existing test asserted the side-by-side layout. The 489-003 tests are store tests
(`RoleState_Contract_Actions_Should`). The catalog tests for `/Admin/Roles/New` compute their
expected tool names from the page's `AgentActions`, so they follow the change without edits.
New Playwright test `role-pages-playwright-tests.cs` (`RolePages_Given_Wasm`) signs in as the
first passkey account (Administrator). It checks that New Role shows the create form and no
`RoleEditForm`. It checks that `/Admin/Roles/{RoleIds.Administrator}` shows `RoleEditForm` with
the stored name and no create form, and that Cancel restores the name after an unsaved edit.

489-003 kitchen: "side by side on New Role" is struck in its restatement, Acceptance, Results,
and How to validate, with a note pointing here. The smoke step now opens
`/Admin/Roles/{AdministratorId}`.

Evidence: `evidence/admin-roles-new.png` and `evidence/admin-roles-administrator.png`. These
come from the Playwright test above, on the in-process Web.Server with the WASM SPA in headless
Chromium, signed in as Administrator. They are not from `dev run`. Steve's master AppHost was
running, and a task-worktree AppHost shares its `UserSecretsId`. A second AppHost has rewritten
the shared postgres password before (2026-09-23). The shots show the same SPA build in a real
browser.

### How to validate

Smoke: sign in as Administrator. Open `/Admin/Roles/New`, then `/Admin/Roles/834b9073-d5ff-40b3-938a-968c23fa76cc`.
On the detail page, change the name, click Cancel, then change it and click Save.

Expect: New Role shows only the "Role" card with Name, Description, and Save, under the subtitle
"Create a custom role.". It has no Edit column and does not load the Administrator role. The
Administrator page shows the edit form with the stored name and description, Cancel and Save
buttons, and the permission checkboxes below. Cancel restores the stored name. Save keeps the
new name. The Ask tool list on New Role offers `Role.CreateRole`, not `Role.UpdateRole`.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `tests/container-apps/web/web-spa-playwright-tests/role-pages-playwright-tests.cs` (new) — not a standalone runfile (no `#if !JARIBU_MULTI`). `dotnet run --project tests/container-apps/web/web-spa-playwright-tests -c Release --no-build -- --filter-class RolePages_Given_Wasm` — passed, 1 test.
- Covering tests for the `RolePage` route and agent actions (no test file edited). None is a standalone runfile, so each ran by class from the web-spa integration project:
  - `features/application/catalog-agent-tests.cs`, `catalog-agent-webmcp-tests.cs`, `catalog-agent-parity-tests.cs` — `--filter-class CatalogAgent_Should` — passed, 55 tests.
  - `features/application/action-catalog-tests.cs` — `--filter-class ActionCatalog_Should` — passed, 10 tests.
  - `features/admin/roles/role-state-contract-actions-tests.cs` — `--filter-class RoleState_Contract_Actions_Should` — passed, 8 tests.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing, not committable).

## Depends on

- 489-003

## Session

- Created: 2026-11-24 from Steve's review of the New Role page. The 489-003 spec wording was
  approved through this cockpit; the error is in the spec, not the implementer.
- Implementation: Claude Opus 5.5, 2026-11-24. RolePage back to create-only, Playwright proof of both role pages, 489-003 kitchen corrected, screenshots in evidence/.
