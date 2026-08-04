# Replace EnsureCreated with EF migrations for web Postgres

## Parent

147

## Description

Make **EF Core migrations** the only schema materialization path for web
`PostgresDbContext`. Remove `EnsureCreated` and the interim
`PostgresModelSchemaBootstrap` “create missing tables” helper.

This is a **template** under active development — not production. Existing Aspire
volumes / dogfood DBs may be wiped; we do **not** design for zero-downtime
cutover of EnsureCreated databases.

Goal: grown-up schema evolution so new entities (e.g. `identity.principal_roles`)
land via committed migrations + `Database.MigrateAsync()` at startup. No hand
DDL, no “drop volume and hope EnsureCreated,” no startup inventing tables outside
migration history.

## Context

| Fact | Implication |
|------|-------------|
| ADR-0009 chose EnsureCreated for day-one zero-setup; Migrate for “grown apps” | Template **is** evolving; that deferral is now debt |
| 147-006 added durable `principal_roles` | EnsureCreated no-op on existing DBs broke dogfood |
| Interim bootstrap (`PostgresModelSchemaBootstrap`) | Create-missing-tables only; not renames/columns; **remove** when Migrate lands |
| No production tenants | Wipe dogfood volumes freely |

## Requirements

1. **Startup:** `await db.Database.MigrateAsync(ct)` only (when Postgres is registered).
2. **Remove:** `EnsureCreatedAsync` from `PostgresDbContextStartupHostedService`.
3. **Remove:** `PostgresModelSchemaBootstrap` (and any call sites).
4. **Initial migration:** full current model — `profiles`, `identity.principals`,
   `identity.credentials`, `identity.principal_roles`, and any other mapped sets.
5. **Tooling:** document exact `dotnet ef migrations add` command (project, startup
   project, output dir, context name) for agents and humans.
6. **Tests:** infrastructure / EF fixtures that today call `EnsureCreated` switch to
   `Migrate()` (or `EnsureCreated` only if intentionally isolated — prefer Migrate for
   parity with host).
7. **ADR-0009 / how-tos:** update golden-path text — template default is **Migrate**,
   not EnsureCreated-until-grown.
8. **Build 0/0**; no hand-wavy “ops will recreate.”

## Non-goals

- Preserving existing EnsureCreated dogfood data
- Dual-path EnsureCreated + Migrate forever
- Migrating api/grpc if they have no PostgresDbContext (web only unless already shared)
- Full multi-tenant / production rollout runbooks
- 147-005 chrome

## Decisions (locked for implementer)

| # | Decision |
|---|----------|
| D1 | Migrations are SSOT for schema; no EnsureCreated in host path |
| D2 | Existing DBs: **wipe acceptable** (Aspire volume / Testcontainers ephemeral) |
| D3 | Delete `PostgresModelSchemaBootstrap` entirely |
| D4 | One initial migration covering current model including 147-006 principal_roles |
| D5 | Folder task for multi-agent review (Claude + others) |

## Implementation plan

### Phase A — Tooling layout

1. Choose migration home (recommend):
   - Assembly: `web-infrastructure` (owns `PostgresDbContext` + configs), **or**
   - dedicated folder under `platform/postgres/migrations/` if project layout requires
   - Confirm `dotnet ef` can target: `--project web-infrastructure --startup-project web-server --context PostgresDbContext`
2. Add `Microsoft.EntityFrameworkCore.Design` (PrivateAssets) where needed for design-time.
3. Ensure design-time factory **or** startup project can resolve connection string for
   `dotnet ef` (empty/dummy connection is fine for scaffolding if factory supplies options).

### Phase B — Initial migration

1. `dotnet ef migrations add InitialPostgresModel` (name may vary; kebab/folder conventions
   per repo if any — usually EF default timestamps).
2. Review generated migration: schemas `identity`, `profiles`; tables principals,
   credentials, principal_roles, profiles; TypedId conversions; concurrency tokens.
3. Commit migration + snapshot.

### Phase C — Host startup

1. `PostgresDbContextStartupHostedService.StartAsync`:
   ```csharp
   await postgresDbContext.Database.MigrateAsync(cancellationToken);
   ```
2. Delete `postgres-model-schema-bootstrap-server.cs`.
3. Update Design/Purpose regions (hosted service, PostgresDbModule if it mentions EnsureCreated).

### Phase D — Tests

1. `ef-principal-store-contract-tests` / `ef-principal-role-store-tests`: replace
   `EnsureCreated` with `Migrate` (same ephemeral DB pattern).
2. Any Profile live Postgres tests same.
3. Run: `dev build`, targeted infrastructure tests, smoke host if practical.

### Phase E — Docs

1. ADR-0009: change EnsureCreated default language → **Migrate for template and apps**.
2. `how-to-add-your-aggregate.md` (or equivalent): “change model → add migration → run.”
3. Optional one-liner in developer how-to: wipe Aspire postgres volume if dogfood is wedged
   after switch (not a standing procedure).

### Phase F — Review / done

1. Implementation review under this folder task (`review/`).
2. Results + **How to validate** (commands below) before `ganda kanban done`.

## File / anchor checklist

| Action | Path |
|--------|------|
| Edit | `platform/postgres/postgres-db-context-startup-hosted-service-server.cs` |
| Delete | `platform/postgres/postgres-model-schema-bootstrap-server.cs` |
| Add | EF migration + `PostgresDbContextModelSnapshot` (location TBD in Phase A) |
| Edit | `tests/.../ef-principal-*-tests.cs` (Migrate not EnsureCreated) |
| Edit | ADR-0009, how-to-add-your-aggregate (and any EnsureCreated docs) |
| Related | `PostgresDbContext`, 147-006 `principal_roles` mapping |

## How to validate (draft — implementer finalizes in Results)

```bash
./bin/dev build
# expect: 0/0

# After host starts against empty postgres-db:
# DataGrip / SQL: expect __EFMigrationsHistory + identity.* + profiles.*

cd tests/container-apps/web/web-infrastructure-tests && \
  dotnet test -c Release -- --filter-class Principal
# expect: EF principal + role store suites green

# Schema change smoke (optional):
# 1. Add a throwaway entity, migrations add, build, run host → table appears
# 2. Revert throwaway before merge
```

**Manual:** `./bin/dev run` → web-server healthy → list tables includes
`identity.principal_roles` without hand DDL → assign roles → restart → grants persist.

## Risks

| Risk | Mitigation |
|------|------------|
| Design-time factory missing | Factory or documented startup project + connection |
| Snapshot out of sync with dual-mode configs | Single DbContext assembly discovery only |
| Testcontainers still EnsureCreated in a corner | Grep EnsureCreated under web/tests; kill remaining |
| Template consumers mid-cutover | Document wipe; no dual support |

## Out of band / already done

- 147-006: `IPrincipalRoleStore` + `EfPrincipalRoleStore` + `identity.principal_roles` mapping
- Interim bootstrap: **to be removed by this task**

## Session

- Created: 2026-08-04 — folder task for migrations; plan for multi-agent review
- Human: wipe dogfood DBs OK; want ideal Migrate path, no tech debt shortcuts
