# Wire Postgres into Aspire AppHost and remove SQL Server remnants

## Description

Mechanical track of [[113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation]]
— no RFC dependency; every candidate golden design needs a live database, and the Postgres-over-
SQL-Server decision is settled (Steve, 2026-07-21).

Today the `postgres` template flag ships server-side plumbing (`PostgresDbContext`,
`PostgresDbModule`, environment/health checks, schema-creation hosted service) but the AppHost
provisions no postgres resource — `dev run` has nothing to connect to. SQL Server exists only as
documented dead code.

## Checklist

- [ ] CPM: add `Aspire.Hosting.PostgreSQL` PackageVersion; reference it in aspire-app-host.csproj
      guarded consistently with the template's flag packaging (compare how yarp's package is
      handled).
- [ ] AppHost program.cs: `#if postgres` block — `AddPostgres` (+ `AddDatabase`), resource name
      from `ServiceNames` constants (TWA0007), `WithReference` + `WaitFor` into web-server (check
      whether api-server needs it too — identity handlers live in web-application today);
      data volume for dev-loop persistence across restarts (`WithDataVolume`).
- [ ] Connection flow: Aspire-injected connection string reaches `PostgresDbModule` in
      Development (align config key with what the module reads today); document the non-Aspire
      path (compose/K8s per 070) in the module's Design region.
- [ ] Remove SQL Server: delete `web-infrastructure/persistence/sql-db-context.cs`, drop
      `Microsoft.EntityFrameworkCore.SqlServer` from Directory.Packages.props, scrub the
      commented `AddDbContextCheck` line in web-server Program; reconcile Design regions that
      reference the SQL Server seam.
- [ ] Template both ways: `dev build` 0/0 and `dev test` green with the postgres flag on AND off;
      TWA0010 satisfied (directive names a template.json flag → DefineConstants).
- [ ] `dev run` comes up with a running postgres container, environment/health checks green in
      the dashboard.

## Notes

- Do NOT add entities or migrations here — the entity-free seam is deliberate; the golden model
  ships with the parent per RFC resolutions.
- pgAdmin/pgweb dashboard resource: optional, decide during implementation (dev-only nicety).

## Session

- Created: 2026-07-21 (split from 113 — mechanical track, RFC-independent)
