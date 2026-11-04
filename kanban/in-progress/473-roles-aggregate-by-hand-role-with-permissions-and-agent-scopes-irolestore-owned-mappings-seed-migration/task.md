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

## Goal (implementer restatement)

Make Role a real hand-written aggregate that owns its permission ids and agent scopes, replacing
the in-memory `RoleStore` dictionary and the write side of the two grant stores.

Acceptance:
- `web-domain` has `RoleId` ([TypedId]) and `Role : Entity<RoleId>, IAggregateRoot` in the Profile
  shape: private ctor, two fail-closed `Create` overloads, `UpdateDetails`, full-replace
  `SetPermissions` / `SetAgentScopes`, value-record children (128 / 64 max length), private nested
  `Invariants` (NotEmpty on Name/Description, each child in its catalog).
- The domain carries its own copies of `PermissionIds.All`, `AgentScopes.All`, and `RoleIds.All`;
  a test under `tests/` fails when a copy drifts from its source.
- Product roles cannot be deleted (domain rule, recorded in Design); `delete-role` returns 409 for them.
- Protected core stays in `AdminLockoutGuards` (application, 409).
- `IRoleStore` (Find, FindMany, List, Add, Update, Remove; save inside each) with an in-memory
  snapshot store (seeded with the four product roles and `RolePermissionSeed` grants) and an EF store.
- The seven roles handlers use `IRoleStore`; entity-to-response mapping is hand-written per handler.
- `IRolePermissionStore` / `IRoleAgentScopeStore` keep only reads, implemented in the Roles slice
  over the aggregate (in-memory over `InMemoryRoleStore`, EF over the owned tables). `Set...`,
  `RolePermissionGrant`, `RoleAgentScopeGrant`, and their configurations are gone.
- EF mapping: `identity.roles`, `OwnsMany` onto the existing `identity.role_permissions` and
  `identity.role_agent_scopes` tables (same keys, columns, lengths), field access to backing lists.
- One migration: create `identity.roles`, `InsertData` the four product roles, insert parents for
  stray role ids already in the two grant tables, then add the FKs; the `principal_roles` FK
  decision is recorded. A test checks the seeded ids equal `RoleIds.All`.
- Gates: `dev build` 0 warnings, Jaribu tests (aggregate, stores in-memory + EF), existing admin
  roles flows unchanged, `ganda repo audit`, `dev template-smoke --matrix`, version + pins bumped.

## Checklist

- [x] Role aggregate, typed id, Invariants, catalog agreement test
- [x] IRoleStore with in-memory and EF implementations
- [x] Seven handlers on the aggregate; dictionary RoleStore retired
- [x] Grant stores: write side retired, read side over the aggregate's tables
- [x] Entity type configuration with both owned collections on the existing tables
- [x] Migration with InsertData seed, FK, principal_roles decision recorded
- [x] Product-role delete guard
- [x] Gates green, version bumped (no new bump: see Results, Version)

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)
- 2026-11-05 implementer (Claude Opus 5.5, ganda task work): implemented the aggregate, stores,
  handlers, mapping, migration, and tests; ran the gates listed under Results. Boy-scout:
  `ganda repo audit --fix` landed this kitchen and set the executable bit on the new runfile.

## Results

### What changed

- **Domain** (`features/admin/roles/`, compiled into `web-domain`): `RoleId` ([TypedId]);
  `Role : Entity<RoleId>, IAggregateRoot` with `Name`, `Description`, `Permissions`
  (`PermissionGrant`, max 128) and `AgentScopes` (`AgentScopeGrant`, max 64) over the private
  lists `PermissionList` / `AgentScopeList`; two fail-closed `Create` overloads, `UpdateDetails`,
  full-replace `SetPermissions` / `SetAgentScopes`, private nested `Invariants`.
  The child records are not named `RolePermission`, because CA1711 reserves the `Permission` suffix.
  `RoleCatalog` holds the domain copies of `PermissionIds.All`, `AgentScopes.All`, and `RoleIds.All`.
- **Product-role delete guard: domain rule.** `Role.IsProductRole` / `Role.EnsureRemovable`.
  Both stores call `EnsureRemovable` inside `RemoveAsync`; `DeleteRole` answers 409 first.
- **Protected core** stays `AdminLockoutGuards` in the `SetRolePermissions` handler (409).
- **Application:** `IRoleStore` (Find, FindMany, List, Add, Update, Remove; each write saves).
  `InMemoryRoleStore` is a snapshot store, seeded from the new `ProductRoleSeed` (names and
  descriptions) plus `RolePermissionSeed.DefaultGrants`. The seven handlers load, mutate, and save
  the aggregate; each maps entity to response by hand. The dictionary `RoleStore` is deleted.
- **Grant ports:** `IRolePermissionStore` / `IRoleAgentScopeStore` keep only their reads. The
  implementations moved into the Roles slice: `InMemoryRolePermissionStore` /
  `InMemoryRoleAgentScopeStore` read `InMemoryRoleStore`, and `EfRolePermissionStore` /
  `EfRoleAgentScopeStore` project the owned rows without loading aggregates. `RolePermissionGrant`,
  `RoleAgentScopeGrant`, their configurations, and the old platform stores are deleted.
  The new `InMemoryRoleStoresModule` (called from Program) registers them, and `PostgresDbModule`
  swaps all three ports together.
- **Infrastructure:** `RoleEntityTypeConfiguration`: `identity.roles` plus `OwnsMany` onto
  `identity.role_permissions (RoleId, PermissionId)` and `identity.role_agent_scopes (RoleId, Scope)`,
  same columns, lengths, and keys, field access, cascade FK. `EfRoleStore` uses `AsSplitQuery`, so
  the two collections do not multiply rows. `template.json` excludes the four EF files when
  postgres is off.
- **Migration `20261104193441_AddRoles`:** create `identity.roles`, `InsertData` the four product
  roles (Guid literals), then raw SQL inserts a parent for every distinct `RoleId` already in
  either grant table, then add the two FKs. No `HasData`. The EF tool wrote the model snapshot to
  `projects/web-infrastructure/TimeWarp/...`, and I moved it back over the committed snapshot.
- **principal_roles FK decision: no FK.** Its `RoleId` is a raw Guid owned by the principals slice,
  `SetPrincipalRoles` accepts only `RoleIds.All`, and product roles cannot be deleted, so a
  principal_roles row cannot point at a deleted role through the API. This is recorded in the
  `RoleEntityTypeConfiguration` and migration Design text.
- **Skills:** `tw-aggregate-pattern` names Role as the owned-collection exemplar.
  `tw-feature-placement` no longer cites the deleted `role-store-application.cs`.

### Tests

- `web-domain-tests/role-tests.cs`: Create, UpdateDetails, full replace (order, duplicates,
  instance reuse, catalog rejection), child length guards, product-role guard, and Invariants
  (an injected off-catalog child fails).
- `web-infrastructure-tests`: `role-catalog-agreement-tests.cs` (three copies equal their
  sources), `role-migration-seed-tests.cs` (seeded ids equal `RoleIds.All`, rows equal
  `ProductRoleSeed`, parents come before the FKs, read from `UpOperations`),
  `role-model-mapping-tests.cs`, and `ef-role-store-tests.cs`. The EF tests run on real Postgres:
  seed, round trip, update with a Version bump, cascade remove, product-role refusal, invariants
  on save, read ports, and the stray-parent migration path (migrate to AddAgentScopeGrants, insert
  stray grants, migrate to latest).
- `features/admin/roles/in-memory-role-store-tests.cs`: seed, snapshot reads, write errors,
  delete guard, read ports.
- Existing tests that wrote grants through the retired `Set...` methods now write through the
  aggregate (runfiles use the real `InMemoryRoleStore`; integration tests use a
  `RoleGrantTestExtensions` helper over `IRoleStore`).

### Gates run (2026-11-05)

- `dev build`: 0 warnings, 0 errors.
- `dev test --group fast`: 14/14 projects passed. `--group integration`: 8/8 passed
  (web-infrastructure-tests 100 tests on Testcontainers Postgres, none skipped). `--group playwright`:
  web-spa-playwright-tests passed, so the admin roles flows still work.
- `ganda repo audit`: passes. Two advisory warnings remain: other tasks' claims with no worktree,
  and EF-generated designer/snapshot files over the 500-line cap (generated, and TWA0038 exempts them).
- `dev lint-skills`: 9/9 passed.
- `dev template-smoke --matrix`: succeeded. All 7 variants generated and built (Contoso.Shop, SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp), and the generated Jaribu suites passed.
- **Version:** `dev check-version` reports source `2.0.0-beta.32` against latest NuGet
  `2.0.0-beta.31`. beta.32 is unreleased, so this change ships in it. The `<Version>` and platform
  pins are already beta.32. A second bump would skip a version, so I did not make one.

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev test --group fast
./bin/dev test --group integration
(cd tests/container-apps/web/web-infrastructure-tests && dotnet test)
dotnet run source/container-apps/web/features/admin/roles/in-memory-role-store-tests.cs
```

Expect: build 0 warnings. Every group passes. web-infrastructure-tests runs `Role_Catalog_Agreement_`,
`Role_Migration_Seed_`, `Role_Model_Mapping_`, and `RoleStore_.Ef` with no `[SKIP]` when Docker is
up. Manual check: `dev run`, sign in as an admin, open Admin → Roles. The four product roles are
listed. Create a role, edit its permissions and agent scopes, reload (the changes persist in
Postgres), and delete it. Deleting Member, Operator, Administrator, or Developer answers
409 "Product role cannot be deleted".

## Notes

Follows 461. Task 474 (analyzer) runs after this lands and uses Roles as its first target. The
rejected generator drafts under 461 `rfc/` show the intended shape of Role as code
(`option-2c-minimal.md` section 3 is a fair picture of the hand-written result).
