# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** same as framework (branch vs merge base 762accd5e, product files)

## Summary

The change makes Role a hand-written aggregate in the Profile shape (`role-domain.cs`, `RoleId`,
value-record children over private backing lists, private `Invariants`), adds `IRoleStore` with an
in-memory snapshot store and an EF store, moves the seven roles handlers onto it, keeps
`IRolePermissionStore` / `IRoleAgentScopeStore` as read ports implemented in the Roles slice over the
aggregate's tables, and adds migration `AddRoles` (create, `InsertData`, stray parents, then FKs).
Risk is low. The design matches the task and the restatement. The aggregate, both stores, the EF
mapping, the migration ordering, and the catalog copies are each covered by tests. One acceptance
criterion, the 409 that `DeleteRole` returns for a product role, has no test at the handler or
endpoint level.

Checked and found sound (no finding):
- Domain: fail-closed `Create` overloads, full replace that keeps existing child instances,
  catalog checks in both the setters and `Invariants`, `EnsureRemovable` domain rule.
- `EfRoleStore.UpdateAsync` reloads tracked and replays the named mutations, with no comparison
  against the caller's `Version`. This is the same as `EfProfileStore.UpdateAsync`, the exemplar the
  task names, so it is not a regression.
- `AggregateDbContext` resolves owned-child changes to the root (`FindOwnership`), so a
  permissions-only change runs `Invariants` and bumps `Version`.
- Migration: seed, then stray-parent `INSERT ... SELECT ... UNION`, then FKs with cascade.
  `role-migration-seed-tests.cs` pins the order, and `ef-role-store-tests.cs` runs the stray-grant
  path on real Postgres.
- `principal_roles` gets no FK. That holds because `SetPrincipalRoles.Validator` only accepts
  `RoleIds.All` (`set-principal-roles-contracts.cs:43`) and product roles cannot be deleted.
- `SetRolePermissions` / `SetRoleAgentScopes`: the contract validator (catalog) and
  `AgentScopeOrder.OrderKnown` (filters to known scopes) run before the domain setter. The setter's
  `ArgumentException` therefore cannot surface as a 500 through the API.
- `InMemoryRoleStoresModule` registration and the `PostgresDbModule` replacement of all three
  ports. The `template.json` postgres-off exclusions name the four moved EF files.
- `in-memory-role-store-tests.cs` is picked up by the `web-jaribu-tests` glob
  (`features/**/*-tests.cs`), so it runs in `dev test --group fast`.
- Skill edits (`tw-aggregate-pattern`, `tw-feature-placement`) are accurate, present-tense, and
  name no clients.

Goal restatement vs task: consistent. The restatement adds a domain copy of `RoleIds.All`. The task
left the product-role guard placement to the implementer, and the domain-rule choice needs that
copy. Not a misread.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/features/admin/roles/delete-role/delete-role-handler-application.cs:40
- Description: The goal's acceptance line "`delete-role` returns 409 for them" has no test. The
  domain guard is tested (`role-tests.cs` `Product_roles_are_not_removable`), and so is the store
  refusal (in-memory and EF `RemoveAsync`). Nothing exercises `DeleteRole.Handler` with a product
  role id. If the `IsProductRole` branch were removed, `RoleStore.RemoveAsync` would throw
  `InvalidOperationException` and the endpoint would answer 500 instead of 409. Every current test
  would still pass.
- Suggestion: In `tests/container-apps/web/web-server-integration-tests/features/admin/roles/roles-endpoint-tests.cs`
  (next to `Create_Update_Get_Delete`), send `DeleteRole.Command` for `RoleIds.Member` (or each of
  `RoleIds.All`). Assert `IsT1` and `AsT1.Status == 409`, then assert that `GetRole` still resolves
  the role. A co-located handler runfile over `InMemoryRoleStore` would also work.
- Status: open
