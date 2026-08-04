# EF principal role store behind postgres flag

## Parent

147

## Description

Make principal→role assignments **durable** under the same postgres path as
`EfPrincipalStore` (104-032). Admin Principals UI (147-004) already writes
`IPrincipalRoleStore`; today only `InMemoryPrincipalRoleStore` is registered, so
grants die on restart while `identity.principals` / `identity.credentials` survive.

Pattern: in-memory default; when Aspire/connection string is present, swap to
`EfPrincipalRoleStore` (mirror `PostgresDbModule` + `EfPrincipalStore`).

## Requirements

- Persist principalId → set of product `RoleIds` Guids in Postgres
- `IPrincipalRoleStore` contract unchanged (Get/Set replace-set semantics from 147-004)
- Empty store still means effective Member via `IEffectiveRolesResolver` (no change to algorithm)
- Bootstrap `Authentication:BootstrapAdministratorPrincipalIds` remains break-glass (not a substitute for store)
- No postgres / skip-mode: keep in-memory singleton
- With postgres: EnsureCreated (or equivalent current template approach) materializes tables
- Prefer schema `identity` (alongside principals/credentials) unless a clean reason for a sibling schema
- DI: register/swap only when connection string present (same gate as EfPrincipalStore)
- Tests: store contract (in-mem already covered if any) + EF fixture parity; optional integration that Set → restart-equivalent new scope → Get
- Build 0/0

## Non-goals

- 147-005 first-run chrome
- EF for RoleStore catalog (demo CRUD roles list)
- Marketplace Operator policies (118)
- Changing effective-role algorithm or SPA contracts
- Moving roles onto TimeWarp.Identity `Principal` entity (stay web-app `IPrincipalRoleStore`)

## Checklist

- [x] Entity + EF configuration (principal id + role id; replace-set semantics)
- [x] `EfPrincipalRoleStore` implementing `IPrincipalRoleStore`
- [x] DI swap in `PostgresDbModule` (or adjacent) when connection present
- [x] EnsureCreated / model discovery so tables appear next to identity principals
- [x] In-memory path unchanged for tests without postgres
- [x] Contract / integration tests for EF store
- [x] Manual: assign Admin roles in UI, restart web-server, grants still apply
- [x] Build 0/0; Design regions on new/touched files

## Notes

### Why now

147-004 shipped assignment UI against process memory. Principals already EF under
postgres; leaving roles in-mem is half-durable identity, not YAGNI. Sequence
before 147-005 chrome.

### Anchors

- `IPrincipalRoleStore` / `InMemoryPrincipalRoleStore` — `features/admin/principals/`
- `EfPrincipalStore` + `PrincipalEntityTypeConfiguration` — identity schema
- `PostgresDbModule` — connection gate + `RemoveAll`/`AddScoped` pattern
- 104-032 — EF principal store dual-mode precedent

### Suggested table shape (implementer may refine)

```
identity.principal_roles
  principal_id uuid  (FK or logical link to identity.principals)
  role_id      uuid
  PK (principal_id, role_id)
```

Set = delete-all for principal + insert set (or equivalent transactional replace).

## Session

- Created: 2026-08-04 — child of 147 after 147-004; before 147-005
- Orchestrate 147-006: 2026-08-04

## Notes (implementation plan)

### Locked

1. Entity `PrincipalRoleAssignment` (PrincipalId + RoleId), table `identity.principal_roles`, composite PK
2. `EfPrincipalRoleStore` : `IPrincipalRoleStore` — scoped, uses `PostgresDbContext`
3. `PostgresDbModule`: when connection present, `RemoveAll<IPrincipalRoleStore>()` + `AddScoped<…, EfPrincipalRoleStore>()`
4. **Lifetime fix:** `IEffectiveRolesResolver` must be **scoped** (not singleton) so it can resolve scoped EF store (captive dependency otherwise)
5. In-mem path: singleton `InMemoryPrincipalRoleStore` unchanged
6. No FK required (logical link); Set = delete rows for principal + insert set in one SaveChanges
7. Tests: EF store round-trip (Set/Get/empty clear) via ephemeral postgres pattern like EfPrincipalStore fixture
8. Reconcile Design on `IPrincipalRoleStore` (no longer "EF out of scope")


## Results

### Summary

Dual-mode `IPrincipalRoleStore`: in-memory singleton by default; when Postgres connection
string is present, `PostgresDbModule` registers scoped `EfPrincipalRoleStore` writing
`identity.principal_roles` (composite PK principal_id + role_id). Replace-set semantics
unchanged. `IEffectiveRolesResolver` is **scoped** so it can resolve the EF store without
captive dependency.

| Mode | Store | Lifetime |
|------|--------|----------|
| No connection | InMemoryPrincipalRoleStore | singleton |
| Postgres connected | EfPrincipalRoleStore | scoped |

### Commits

- `7ed22978` feat: durable EF principal role store
- (review fixes) scoped resolution in authz tests; comment cleanup

### Build / tests

| Gate | Result |
|------|--------|
| `./bin/dev build` | 0/0 |
| Principal_role_store (EF) | 4/4 |
| RolesAuthorization | 6/6 |
| PrincipalsAuthorization | 5/5 |

### Review

- Effort 1, general, 1 round
- Disposition: **accepted-exceptions** (EnsureCreated does not upgrade existing volumes)
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Automated**
```bash
./bin/dev build
# expect: 0/0

cd tests/container-apps/web/web-infrastructure-tests && \
  dotnet test -c Release -- --filter-class Principal_role_store
# expect: 4 passed (ephemeral Postgres / Testcontainers)

cd tests/container-apps/web/web-server-integration-tests && \
  dotnet test -c Release -- --filter-class PrincipalsAuthorization
# expect: all passed
```

**Manual (Aspire + postgres)**
1. `./bin/dev run` so web-server gets `ConnectionStrings__postgres-db`.
2. **Existing volume upgrade:** if `identity.principal_roles` is missing (DB created before 147-006),
   either drop the Aspire postgres data volume and restart, or run:
   ```sql
   CREATE TABLE IF NOT EXISTS identity.principal_roles (
     principal_id uuid NOT NULL,
     role_id uuid NOT NULL,
     PRIMARY KEY (principal_id, role_id)
   );
   ```
   Greenfield EnsureCreated creates the table automatically.
3. Bootstrap or assign Administrator via Admin → Principals; Save.
4. DataGrip: `SELECT * FROM identity.principal_roles;` — expect rows.
5. Restart web-server (keep volume) — **Expect:** Admin nav / role grants still present
   (not Member-only).

**Not in scope:** Role catalog EF; 147-005 chrome; migrations framework.
