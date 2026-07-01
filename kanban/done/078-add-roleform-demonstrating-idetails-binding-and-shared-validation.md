# Add RoleForm demonstrating I*Details binding and shared validation

## Description

TWA currently has **no Blazor form that demonstrates the contract-binding + shared-validation
pattern** the `web-api-contracts` skill teaches (verified: repo-wide **0** `I*Details`-bound
components, **0** `FluentValidationMessage`/`FluentValidationValidator`; the only form —
`features/to-do/components/TodoItemForm.razor` — is an empty skeleton binding a concrete
`UpdateTodoItem.Command`, and the todo-items contract has no `ITodoItemDetails` interface). Meanwhile
the one **clean** contract that *does* model the pattern — `admin/roles` (`IRoleDetails` +
`RoleDetailsValidator`, with `CreateRole`/`UpdateRole`/`GetRole` commands) — has **no UI at all** in
web-spa.

Build an **Admin/Roles form slice** in `web-spa` that binds an `EditForm` directly to the
**`IRoleDetails`** interface and runs the **shared `RoleDetailsValidator`**, reused across
**New / Edit / View** — the exemplary counterpart to copic's `ModuleForm`. This gives the skill and
the template a living reference for "one shape, shared validation, no separate view model."

**Reference implementation (frozen, read-only):**
`copic/main/Source/ContainerApps/Web/Web.Spa/Features/Admin/Modules/` — `Pages/ModulePage/ModuleForm.razor`
+ `ModuleState` (`CreateModuleActionSet`/`UpdateModuleActionSet`). Mirror `IModuleDetails` → `IRoleDetails`.

**Contract already in place (use as-is — the clean example):**
`source/container-apps/web/web-contracts/features/admin/roles/` — `role-details.cs` (`IRoleDetails` +
`RoleDetailsValidator`), `commands/create-role.cs`, `commands/update-role.cs`, `queries/get-role.cs`
(has `GetMockResponseFactory()`), `queries/get-roles.cs`.

## What it must demonstrate (the whole point)
- `<EditForm Model=@RoleDetails>` where the model is the **`IRoleDetails` interface**, not a concrete
  Command/view-model — the `Create`/`Update` `Command` *is* the bound model.
- Per-field two-way binding (`@bind-Value=RoleDetails.Name`) + per-field validation message.
- The **shared `RoleDetailsValidator`** drives validation (no rules re-declared in the component).
- One form reused for **New** (bind `CreateRole.Command`), **Edit** (map `GetRole.Response` →
  `UpdateRole.Command`), **View** (read-only bind of the loaded state).

## Checklist
- [x] Validation library decision — **adopt Blazilla, remove Morris** (see Results).
- [x] `RoleState` + `CreateRoleActionSet` in `web-spa/features/admin/roles/role-state/` (mirrors copic
      `ModuleState` + TWA weather-forecast Fetch idiom via `DefaultApiHandler`).
- [x] `RoleForm.razor` bound to `IRoleDetails` with `@bind-Value` `Name`/`Description` + per-field
      `<ValidationMessage>`; Blazilla `<FluentValidator Validator="new RoleDetailsValidator()"/>`.
- [x] `RolePage.razor` (`/Admin/Roles/New`) + nav entry (new **Admin** `FluentNavCategory`).
- [x] Mock: added `CreateRole.GetMockResponseFactory()` + registered in `mock-web-api-service.cs`.
- [x] `dev build` green (0/0, warnings-as-errors).
- [x] **Manual/visual verification** — ran under Aspire: the form binds `IRoleDetails`, per-field
      validation fires, and Save dispatches a valid `CreateRole.Command`. **Binding + validation
      proven.** (See "Save round-trip" below for the expected 405.)
- [ ] **Edit/View modes** — deferred: `GetRole`/`UpdateRole` routes have a `RoleId` `int`-vs-`Guid`
      wart (`update-role.cs` uses `api/Role/{RoleId:int}`) that belongs to the contracts cleanup.
      New mode fully demonstrates the binding+validation pattern; Edit/View is follow-up.
- [ ] **Save round-trip** — deferred to
      [[079-implement-server-side-createrole-endpoint--backend-validation-roles-contract]]. On Save the
      valid `POST api/Roles` reaches the **real** web-server, which has no `CreateRole` endpoint yet →
      **405** → generic error toast (confirmed via Aspire console logs). Expected: this task owns the
      *client* half (binding + validation, done); 079 builds the *server* half + backend validation.
      The `MOCK_WEB_API` path works client-only, but mock mode is off by default and flipping it
      globally also mocks auth (`GetCurrentUser`), so we leave the real path 405-ing until 079.

## Results (build-green; pending manual run)
- **Validation library switched to Blazilla** (`loresoft/Blazilla` 2.4.0; net10.0; FluentValidation
  12.1.1). Removed `Morris.Blazor.FluentValidation` (pkg ref, `_Imports`, and the commented-out
  `AddFormValidation` TODO in `program.cs`). Kept `Morris.Blazor.ControlFlow` (unrelated). Retired the
  `<Validate/>` in `TodoItemForm.razor`.
  - **Why not Morris:** Morris resolves validators by the model's *runtime type* (exact-type dict) and
    can't target an interface, so binding `IRoleDetails` wouldn't pick `RoleDetailsValidator`. Blazilla's
    **explicit `Validator` parameter** lets us pass `new RoleDetailsValidator()` and validate the
    interface's shared rules — copic's pattern, without Blazored's deprecation.
  - **Skill finding:** "bind the interface → get the shared validator" is *library-dependent*. Capture
    in [[contract-conventions-rfc]].
- **FluentUI v5 rename:** the text input is `FluentTextInput`, not `FluentTextField` (v4 name).
- Files: `features/admin/roles/{role-state/role-state.cs, role-state/role-state.create-role.cs,
  components/RoleForm.razor, pages/RolePage.razor(.cs)}`; edits to `NavMenu.razor`,
  `mock-web-api-service.cs`, `create-role.cs`, `program.cs`, `_Imports.razor`, both `*.csproj`/props.

## Notes
- Depends conceptually on the clean `admin/roles` contract; **not** blocked by the contracts cleanup
  ([[077-contracts-compliance-01-nullability-validator-agreement]]) since roles are already compliant.
- Kebab-case paths, plural folders, per the RFC ([[contract-conventions-rfc]]).
- Headless Fixie can't render the FluentUI provider tree; visual verification is manual or an E2E
  click test (could sit alongside [[060-write-real-e2e-tests-for-sunny-day-money-paths-primary-use-cases--payment-flow]]).
- Option B (flesh out `TodoItemForm`) was rejected: it needs an `ITodoItemDetails` interface built
  first and demonstrates a rougher contract. This task uses the exemplary one.
