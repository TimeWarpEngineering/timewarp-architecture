# Round 1 — general
**Date:** 2026-08-04
**Scope reviewed:** commit 7ed22978 (feat 147-006 EF principal role store)

## Summary

147-006 lands a clean dual-mode mirror of `EfPrincipalStore`: singleton `InMemoryPrincipalRoleStore` remains the zero-infra default; when `PostgresDbModule` sees a connection string it `RemoveAll`s and registers scoped `EfPrincipalRoleStore`. Captive-dependency risk is addressed by registering `IEffectiveRolesResolver` (and already-scoped `IClaimsTransformation`) as scoped so the resolver can resolve the scoped EF store. Entity/mapping put `PrincipalRoleAssignment` on `identity.principal_roles` with composite PK and `PrincipalId` Guid conversion, discovered via `DbSet` + `ApplyConfigurationsFromAssembly` (EnsureCreated path unchanged). Set is replace-set (load → RemoveRange → Distinct insert → single `SaveChanges`); Get is empty-on-miss with `AsNoTracking`. TWA0009 is respected (Features substrate for port/entity/resolver; literal `"identity"` schema in Admin.Principals configuration; no Identity.Infrastructure cross-slice reference). EF Jaribu suite covers cross-context durability, empty clear, missing principal, and dedupe. No product correctness bugs found in the store algorithm or DI gate; remaining items are upgrade/ops and comment/test hygiene.

## Issues

### Issue 1 — Severity: suggestion
- File: `source/container-apps/web/platform/postgres/postgres-db-context-startup-hosted-service-server.cs:45-51` (behavior); model addition in `postgres-db-context-infrastructure.cs` / `principal-role-assignment-entity-type-configuration-infrastructure.cs`
- Description: Materialization still uses `EnsureCreatedAsync`, which is a no-op when the database already has tables. Greenfield Aspire/Testcontainers DBs get `identity.principal_roles`; any dogfood or generated-app volume that already ran EnsureCreated for principals/credentials/profiles will **not** gain the new table on restart. Admin Set/Get then fails at runtime until the DB is dropped or the table is created out of band. Hosted-service Design already documents EnsureCreated vs migrations; this change is the first schema addition after identity principals for many long-lived volumes.
- Suggestion: Call out the upgrade step in the task Session/manual checklist (drop volume or `CREATE TABLE identity.principal_roles …`). Optional later: one-shot raw DDL, or migrate when the template adopts migrations. No change required for greenfield correctness.
- Status: open

### Issue 2 — Severity: suggestion
- File: `source/container-apps/web/features/admin/principals/in-memory-principal-role-store-application.cs:1-2`; `source/container-apps/web/features/identity/in-memory-identity-stores-module-infrastructure.cs:39`
- Description: Purpose still says “no EF yet”; the module line still says “in-memory until an EF backend lands.” Design regions above both already describe dual-mode / 147-006. Agent-context maintenance rule: Purpose/comments that deny EF are now wrong.
- Suggestion: Rephrase Purpose to “zero-infra / skip-mode backend” and the registration comment to “default until PostgresDbModule swaps to EfPrincipalRoleStore.”
- Status: open

### Issue 3 — Severity: suggestion
- File: `tests/container-apps/web/web-server-integration-tests/features/admin/principals/principals-authorization-tests.cs:75-76` (and roles-authorization-tests same pattern)
- Description: Suites resolve `IPrincipalRoleStore` from the root `WebApplicationHost.ServiceProvider`. That is valid only while the store stays singleton (skip-mode / no connection string — current in-proc host). Under the new dual-mode, a host with a postgres connection registers a **scoped** store; root resolution then throws or fails scope validation. Same pre-existing pattern exists for `IPrincipalStore` after 104-032; 147-006 extends the footgun to role grants.
- Suggestion: Resolve via `IServiceScopeFactory.CreateScope()` (or the existing `ScopedSender` helpers) if any integration path is expected to run with a real connection string. Not blocking for the default skip-mode suite.
- Status: open
