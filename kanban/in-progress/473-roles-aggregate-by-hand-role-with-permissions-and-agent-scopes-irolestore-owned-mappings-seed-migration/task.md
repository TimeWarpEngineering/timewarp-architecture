# Roles aggregate by hand: Role with permissions and agent scopes, IRoleStore, owned mappings, seed migration

## Description

Resolution of task 461 (`kanban/*/461-*/rfc/rfc.md`, Final resolution): the domain is written
domain-first, like the Profile exemplar; no generator. Roles today is an in-memory dictionary
(`features/admin/roles/role-store-application.cs`) called by seven handlers, with permissions and
agent scopes in two platform grant stores. This task makes Role a real aggregate that owns both.

## Requirements

Domain (`web-domain`, follow `tw-aggregate-pattern` and `profile-domain.cs` exactly):
- `role-id-domain.cs`: `[TypedId] RoleId`.
- `role-domain.cs`: `Role : Entity<RoleId>, IAggregateRoot`. Fields `Name`, `Description`
  (`private set`). Children `Permissions` (value record, max length 128) and `AgentScopes` (value
  record, max length 64) as `IReadOnlyList` over private backing lists. Private constructor,
  fail-closed `Create` (two overloads like Profile), `UpdateDetails`, `SetPermissions` and
  `SetAgentScopes` as full replace (the commands are `IIdempotentCommand`). Private nested
  `Invariants` with `NotEmpty` on both fields and `RuleForEach ... Must` against catalogs for both
  children. The catalogs (`PermissionIds.All` in contracts, `AgentScopes.All` in timewarp-identity)
  are not reachable from `web-domain`: copy them as domain constants with a test in
  `tests/` that the domain copy equals the source list (build check for agreement).
- Product roles (`RoleIds` in contracts) cannot be deleted: a domain rule or a store guard, your
  call, recorded in the Design region. Today `delete-role` has no guard.
- Protected core (`AdminLockoutGuards`) stays in the application layer and still returns 409. Not a
  domain hook.

Application (`web-application`):
- `i-role-store-application.cs`: `IRoleStore` in the `IProfileStore` shape (`FindAsync`,
  `FindManyAsync`, `ListAsync`, `AddAsync`, `UpdateAsync`, `RemoveAsync`; save inside each).
- `in-memory-role-store-application.cs` with snapshot semantics like `InMemoryProfileStore`.
- Rewrite the seven handlers (create, update, delete, get, get-roles, set-permissions,
  set-agent-scopes) to the aggregate. Retire `RoleStore` (dictionary). The DTO mapping
  (entity to response) is hand-written in each handler; that is deliberate (task 476 evaluates
  generating it).
- `IRolePermissionStore` and `IRoleAgentScopeStore` keep their **read** methods (the permission
  evaluator, lockout guards, agent-scope approval and seed, and the principals slice read them;
  TWA0009 forbids those callers depending on the Roles slice). Retire their `Set...` write
  methods, `RolePermissionGrant`, `RoleAgentScopeGrant`, and both entity type configurations. The
  read implementations query the aggregate's tables.

Infrastructure (`web-infrastructure`, follow `profile-entity-type-configuration-infrastructure.cs`):
- `role-entity-type-configuration-infrastructure.cs`: `identity.roles`; `OwnsMany` onto the
  existing `identity.role_permissions` `(RoleId, PermissionId)` and `identity.role_agent_scopes`
  `(RoleId, Scope)` with the current column names and lengths, no surrogate keys, field access to
  the backing lists. `Version` via `AggregateVersionConvention`.
- `ef-role-store-infrastructure.cs` like `EfProfileStore`.
- One migration: create `identity.roles`, `InsertData` the four `RoleIds` (Guid literals, the
  precedent is `20260812102254_AddRolePermissions`), insert a parent row for any distinct `RoleId`
  already present in either grant table, then add the foreign keys. Decide `identity.principal_roles`
  FK behavior and record it. No `HasData`.
- Test that the migration's seeded ids equal `RoleIds.All`.

Gates: `dev build` 0 warnings, Jaribu tests for the aggregate (Invariants, full replace, product
role delete guard), store tests in-memory and EF, Playwright admin roles flows unchanged,
`ganda repo audit`, `dev template-smoke --matrix` (postgres off must still build). Bump
`<Version>` and the platform pins (this touches `web-domain` content that ships in the template).

## Checklist

- [ ] Role aggregate, typed id, Invariants, catalog agreement test
- [ ] IRoleStore with in-memory and EF implementations
- [ ] Seven handlers on the aggregate; dictionary RoleStore retired
- [ ] Grant stores: write side retired, read side over the aggregate's tables
- [ ] Entity type configuration with both owned collections on the existing tables
- [ ] Migration with InsertData seed, FK, principal_roles decision recorded
- [ ] Product-role delete guard
- [ ] Gates green, version bumped

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Follows 461. Task 474 (analyzer) runs after this lands and uses Roles as its first target. The
rejected generator drafts under 461 `rfc/` show the intended shape of Role as code
(`option-2c-minimal.md` section 3 is a fair picture of the hand-written result).
