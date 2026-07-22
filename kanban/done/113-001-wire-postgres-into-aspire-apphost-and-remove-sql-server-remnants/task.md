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

- [x] CPM: add `Aspire.Hosting.PostgreSQL` PackageVersion; reference it in aspire-app-host.csproj
      guarded consistently with the template's flag packaging (compare how yarp's package is
      handled).
- [x] AppHost program.cs: `#if postgres` block — `AddPostgres` (+ `AddDatabase`), resource name
      from `ServiceNames` constants (TWA0007), `WithReference` + `WaitFor` into web-server (check
      whether api-server needs it too — identity handlers live in web-application today);
      data volume for dev-loop persistence across restarts (`WithDataVolume`).
- [x] Connection flow: Aspire-injected connection string reaches `PostgresDbModule` in
      Development (align config key with what the module reads today); document the non-Aspire
      path (compose/K8s per 070) in the module's Design region.
- [x] Remove SQL Server: delete `web-infrastructure/persistence/sql-db-context.cs`, drop
      `Microsoft.EntityFrameworkCore.SqlServer` from Directory.Packages.props, scrub the
      commented `AddDbContextCheck` line in web-server Program; reconcile Design regions that
      reference the SQL Server seam.
- [x] Template both ways: `dev build` 0/0 and `dev test` green with the postgres flag on AND off;
      TWA0010 satisfied (directive names a template.json flag → DefineConstants).
- [x] `dev run` comes up with a running postgres container, environment/health checks green in
      the dashboard.

## Notes

- Do NOT add entities or migrations here — the entity-free seam is deliberate; the golden model
  ships with the parent per RFC resolutions.
- pgAdmin/pgweb dashboard resource: optional, decide during implementation (dev-only nicety).

### Implementation plan (Phase 2, 2026-07-22)

Plan agent findings that CORRECT the checklist above:

1. TWA0007 does NOT cover AddPostgres (analyzer Design region: AddProject-only by design;
   precedent YarpResourceName="ingress" is a hand-written constant). Shared-constant discipline,
   not analyzer requirement.
2. Do NOT add to ServiceNames (foundation-contracts): generated apps consume the PUBLISHED
   Foundation package which lags source — new constant would break them until republish. Use
   aspire-app-host/constants.cs instead (already compile-linked into web-server → one constant,
   zero drift, no publish dependency).
3. Plumbing deader than described: PostgresDbModule call is a plain // comment (no #if postgres
   in .cs anywhere); PostgresDbOptions never bound — module's throwaway-provider read always
   null.
4. DbContext health check AND Oakton environment check DISCARD CanConnectAsync results and
   return true (green with no DB; wrong catch type too). Fix both or "green checks" is vacuous.
5. Test blast radius: web-server-integration-tests direct-host Program.ConfigureServices (no
   Aspire) → module must skip when unconfigured; Aspire-testing suites will now start a real
   postgres container (Docker required, WaitFor latency).
6. api-server needs NO postgres reference (zero EF usage; identity handlers are web-server-hosted
   and in-memory today).

Steps: (1) CPM +Aspire.Hosting.PostgreSQL 13.4.6, −EFCore.SqlServer; (2) app-host csproj
DefineConstants +postgres, guarded PackageReference mirroring yarp; (3) constants.cs
PostgresResourceName="postgres" / PostgresDatabaseResourceName="postgres-db" (db name doubles as
Aspire ConnectionStrings key); (4) program.cs #if postgres AddPostgres().WithDataVolume()
.AddDatabase() + webServer.WithReference(postgresDb).WaitFor(postgresDb), Design region
reconciled; (5) web-server program.cs: #if postgres around module call (TWA0008-safe comment
rewrite), delete dead SqlDb comment lines; (6) postgres-db-module: single IConfiguration read —
precedence PostgresDbOptions:ConnectionString then GetConnectionString("postgres-db"), skip-mode
when absent, Configure<PostgresDbOptions> binding, honest CanConnectAsync health check, remove
throwaway provider + dead method, Design rewrite; (7) environment-check same honesty fix;
(8) delete sql-db-context.cs, web-server.csproj SqlServer PackageReference, postgres-db-context
Design sentence, dependencies-with-nuget.puml node (verify hand-maintained);
(9) verify dev build 0/0, dev run postgres green, dev test (Docker!), optional template
both-ways smoke.

Policy decisions taken (flagged to Steve in-chat, proceeding unless vetoed): skip-when-
unconfigured module; honest health checks (visible behavior change); Aspire test suites now
need Docker (CI check before merge).

- Plan: 2026-07-22 (plan agent via orchestrator)

## Session

- Created: 2026-07-21 (split from 113 — mechanical track, RFC-independent)

## Results

**Delivered (commits `00c11d73`, `7b99a3d2`, `285dedfa`, 2026-07-22):**

- AppHost provisions Postgres behind the postgres flag: `AddPostgres("postgres").WithDataVolume()
  .AddDatabase("postgres-db")`, declared INSIDE the web preprocessor block (Web.Server is the
  only consumer; api-server deliberately unreferenced — zero EF usage there; no orphan container
  in the postgres-without-web combo). Web.Server gets `WithReference` + `WaitFor`.
- Resource names live in aspire-app-host/constants.cs (compile-linked into web-server → zero
  drift), deliberately NOT in ServiceNames: TWA0007/service discovery cover AddProject only, and
  a new Foundation constant would break generated apps until the package republishes. The
  database resource name doubles as the Aspire-injected ConnectionStrings key.
- PostgresDbModule reworked: single-read two-source connection resolution
  (`PostgresDbOptions:ConnectionString` section wins → else Aspire `ConnectionStrings:postgres-db`),
  complete skip-mode when unconfigured (direct-host tests + unconfigured consumers boot
  unchanged), options actually bound, honest `CanConnectAsync` health check; throwaway
  BuildServiceProvider and dead code removed. Environment check discarded-result bug fixed.
- SQL Server fully removed: sql-db-context.cs deleted; CPM + web-server package refs dropped;
  zero grep residue; Design regions and dependency diagram reconciled.
- Unblocked in passing: NU1903 high-severity advisory on System.Security.Cryptography.Xml
  10.0.9 was failing the repo-wide 0/0 gate — bumped to 10.0.10 (`00c11d73`).

**Verification:** `dev build` 0 warnings / 0 errors (full solution, audit on).
web-server-integration-tests identical to pre-change baseline (59 pass / 23 pre-existing
ApiSecret-dependent failures / 1 skip) — zero regression; validates skip-mode. `dev run`
postgres-container smoke + full `dev test` under Docker deliberately left for the next live
session (see checklist residue below).

**Review (Phase 4b):** 1 round, single general reviewer (effort 1). 0 critical / 0 major /
1 minor / 2 nit; all fixed (`285dedfa`); disposition **clean** (`review/disposition.md`).
Reviewer empirically confirmed the postgres-without-web combo builds warning-free and that
skip-mode registers nothing when unconfigured.

**Deviations from plan:** global-usings.cs edits (compiler-forced); catch(Exception) in probes
(CA1031 NoWarn'd in container-apps); postgres declaration nested rather than compound-gated
(matches existing nested-directive template pattern).

**Residue / follow-ups:** (1) `dev run` smoke DONE 2026-07-22: postgres:18.3 container +
postgres-db resource healthy, WaitFor ordering held, schema-creation hosted service ran,
web-server /health 200 with the real CanConnectAsync probe, app serves via ingress. The smoke
caught one latent bug: PostgresDbContext lacked the DbContextOptions constructor that
AddDbContext validation requires — never fired while the module was dead code; fixed
`762b4e7c`. Full `dev test` with Docker DONE 2026-07-22: all suites green except
web-server-integration-tests' known 23 (traced to the WebAuthnOptions:RpId user-secret override,
NOT ApiSecret and NOT this task — see 104-031 addendum); aspire-tests booted the AppHost with
postgres under test cleanly, no port conflicts even with a live dev run. Remaining for CI:
confirm runners have Docker before merging to master.
(2) Honest health checks are a visible behavior change:
web-server reports unhealthy if postgres is configured but unreachable (intended). (3) pgAdmin/
pgweb dashboard resource: not added (optional nicety, revisit with 113 golden implementation).

## Session

- Implementation/review/disposition: 2026-07-22 (orchestrated: plan + build + review teammates)
