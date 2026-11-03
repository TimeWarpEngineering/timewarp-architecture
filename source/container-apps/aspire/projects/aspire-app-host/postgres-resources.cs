#region Purpose
// Postgres for Web.Server, its only consumer: the database for each publish target (container,
// Flexible Server, or Neon connection strings) and the web-migrations EF resource.
#endregion

#region Design
// Postgres is not a project: a container resource in run mode, Compose and Kubernetes, an Azure Database for
// PostgreSQL Flexible Server for aca. Its connection string is injected into Web.Server keyed by the
// DATABASE resource name (constants.cs PostgresDatabaseResourceName), and PostgresDbModule reads it by that same key.
// Only Web.Server references Postgres; Api.Server intentionally does not — so Postgres is declared INSIDE the web
// preprocessor block (the postgres directive nested within the web one), not gated on postgres alone: with web
// excluded it would otherwise be an unreferenced orphan container in the postgres-without-web combination.
// The Postgres data volume uses Aspire 13.6 WithVolume(env:) (task 266): same volume name and mount as
// WithDataVolume, plus POSTGRES_DATA_VOLUME (the mount path) on the container. What env: buys is small but
// real: the mount path is declared once in the model, visible in the dashboard and in published manifests,
// and follows the same convention the AppHost uses for projects/executables. It deliberately does NOT
// override PGDATA (the image's PGDATA lives under the mount; rebinding it to the mount root would
// re-initdb and orphan existing data). aspire-tests' postgres-volume-model-tests guard name/target parity.
// Postgres carries WithRepl (Aspire 13.6, task 262) only when the AppHost environment is Development: the
// dashboard psql REPL runs with the server's credentials, so it must never appear on a shared dashboard.
// Schema evolution (task 147-007, amended task 155): committed EF migrations under
// platform/postgres/migrations/. Migrations are explicit/on-demand in every environment: production
// applies them from the published PublishAsMigrationScript/PublishAsMigrationBundle pipeline
// artifacts (never AppHost auto-run); local/Aspire dev gets RunDatabaseUpdateOnStart as an
// out-of-box convenience (idempotent — cheap no-op on an already-current schema) plus the
// ef-database-update dashboard command on the web-migrations resource for an explicit re-run.
// web-server does not Migrate/EnsureCreated at startup, and — as of task 155, re-confirmed on
// Aspire 13.6 by task 270 — has NO wait edge on web-migrations at all (no WaitFor, no
// WaitForCompletion). Both wait forms were tried and both broke: WaitFor deadlocks any dashboard
// restart/rebuild of web-server after the first run because run-mode DefaultWaitBehavior only
// continues past Running, a state the once-run migration resource never re-enters (Finished is
// terminal, not Running). WaitForCompletion breaks DCP endpoint wiring for web-server. On
// 2026-08-05 (Aspire.Hosting.EntityFrameworkCore 13.4.6-preview.1.26319.6) the error was "Could
// not create Endpoint object(s): information about the port to expose the service is missing;
// service-producer annotation is invalid". Re-tested 2026-10-02 on Aspire 13.6.0 with
// Aspire.Hosting.EntityFrameworkCore 13.6.0-preview.1.26479.8 (task 270): the wait itself works
// (web-server logs "Waiting for resource 'web-migrations' to complete", then starts after
// "Successfully executed command 'ef-database-update'"), but the dotnet-ef tool resource inherits
// web-server's environment and DCP rejects it: "Could not perform substitution for environment
// variable ASPNETCORE_URLS ... service '/web-server-https' referenced by Executable
// '/ef-tool-web-migrations-…' specification is not produced by this Executable". web-server
// then listens on the Kestrel default http://localhost:5000 instead of its allocated port, the
// ingress health check times out, and aspire-tests fails 6 of 11 (11 of 11 without the edge).
// So the migration window stays; web-server is what tolerates it. SiteSettingsSeedHostedService
// asks EfPendingMigrationsProbe whether any migration this build was compiled with is missing
// from the database and waits before Kestrel starts (task 363; task 270 only checked that the
// site-settings table existed, so a new column on an existing table still crashed boot with
// 42703). A first run against an empty database, or a boot racing a new migration, logs no Error
// (guarded by aspire-tests' FirstRunOnEmptyDatabase fact, which boots an ephemeral Postgres, and
// web-server-integration-tests' boot-waits-for-pending-migrations regression). Restart
// never deadlocks because there is no edge to wait on, and Aspire.Hosting.Testing suites stay
// green.
// program.cs calls AddTo inside the web block with the postgres flag nested in it, so this file is
// excluded from a generated app unless both flags are on (template.json).
#endregion

namespace TimeWarp.Architecture.Aspire;

/// <summary>Declares Postgres and the web-migrations resource, and references them from web-server.</summary>
internal static class PostgresResources
{
  /// <summary>
  /// Declares the database for <paramref name="publishTarget"/>, references it from
  /// <paramref name="webServer"/>, and adds the web-migrations resource.
  /// </summary>
  internal static void AddTo
  (
    IDistributedApplicationBuilder builder,
    PublishTarget publishTarget,
    IResourceBuilder<ProjectResource> webServer
  )
  {
    // Postgres is declared HERE, inside the web block, because Web.Server is its only consumer
    // (the api-server deliberately gets no reference). With web excluded there is nothing to
    // reference it, so it must not be declared at all — gating on the postgres flag alone would
    // boot an orphan container in the postgres-without-web template combination.
    // The data volume (dev-loop persistence) is config-gated: Postgres:UseDataVolume=false makes
    // the container ephemeral. Aspire test suites MUST pass that flag — the volume name is
    // deterministic per AppHost, so overlapping AppHost instances (dev run + test hosts, or
    // leaked test containers) sharing one volume corrupt postgres's WAL ("PANIC: could not
    // locate a valid checkpoint record"), after which every postgres start crash-loops and
    // WaitFor blocks forever. Found the hard way: PR 286 CI hang in web-spa-integration-tests.
    // The database resource name doubles as the ConnectionStrings key Aspire injects into
    // Web.Server (see constants.cs).
    (IResourceBuilder<IResourceWithConnectionString> postgresDb, IResourceBuilder<IResourceWithConnectionString>? neonDirect) =
      DeclareDatabase(builder, publishTarget);

    if (!publishTarget.IsContainerAppsNeon)
    {
      webServer.WithReference(postgresDb).WaitFor(postgresDb);
    }

    AddMigrations(builder, publishTarget, webServer, postgresDb, neonDirect);
  }

  private static (IResourceBuilder<IResourceWithConnectionString> Database, IResourceBuilder<IResourceWithConnectionString>? NeonDirect) DeclareDatabase
  (
    IDistributedApplicationBuilder builder,
    PublishTarget publishTarget
  )
  {
    if (publishTarget.IsContainerAppsNeon)
    {
      // Task 317: Neon is not provisioned by this model. The pooled string is the app's connection.
      // The direct string is a second resource so migrations can reference it under the postgres-db
      // name without pointing web-server at the pooler. The pooled WithReference is applied after
      // AddEFMigrations (see PublishTarget's Design region, publish-target.cs, aca-neon paragraph).
      // No WaitFor: a connection string has no health endpoint, and publish mode does not run wait
      // edges.
      // This is the only declaration of the two Neon parameters: web on, postgres on, Publish:Target=aca-neon.
      // Neither has a default. Publish still emits the pooled name as @secure(). Aspire also tries to copy
      // every parameter value into deployment state and logs MissingParameterValueException when the
      // operator has not set one. That warning does not fail publish. A default would ship the secret.
      IResourceBuilder<ParameterResource> pooled = builder.AddParameter(NeonPooledConnectionStringParameterName, secret: true);
      IResourceBuilder<ParameterResource> direct = builder.AddParameter(NeonDirectConnectionStringParameterName, secret: true);
      return
      (
        builder.AddConnectionString(PostgresDatabaseResourceName, ReferenceExpression.Create($"{pooled}")),
        builder.AddConnectionString(NeonDirectConnectionResourceName, ReferenceExpression.Create($"{direct}"))
      );
    }

    if (publishTarget.ContainerApps is not null)
    {
      // Task 070-007: Azure Database for PostgreSQL Flexible Server, not a postgres container on Azure
      // Files (no volume to lose, Azure-managed backups/patching). Password authentication with the
      // generated postgres-username / postgres-password parameters (the password @secure()). The
      // connection string lands in a Key Vault Aspire provisions beside the server, and web-server
      // reads it through a Key Vault-backed container-app secret; WithReference ALSO gives web-server
      // the password as container-app secrets postgres-db-password / postgres-db-uri (see
      // PublishTarget's Design region, publish-target.cs, aca Postgres bullet).
      // Firewall: AllowAllAzureIps (any Azure IP, any tenant). Publish-only, like every target switch:
      // run mode never reaches this branch and keeps the container (AddPostgresContainer).
      return
      (
        builder.AddAzurePostgresFlexibleServer(PostgresResourceName)
          .WithPasswordAuthentication()
          .AddDatabase(PostgresDatabaseResourceName),
        null
      );
    }

    return (AddPostgresContainer(builder, publishTarget), null);
  }

  private static IResourceBuilder<IResourceWithConnectionString> AddPostgresContainer
  (
    IDistributedApplicationBuilder builder,
    PublishTarget publishTarget
  )
  {
    bool usePostgresDataVolume = !string.Equals(
      builder.Configuration["Postgres:UseDataVolume"], "false", StringComparison.OrdinalIgnoreCase);

    IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(PostgresResourceName);

    // Task 266 (Aspire 13.6): WithVolume(env:) replaces WithDataVolume() with the SAME volume
    // identity — the name WithDataVolume generates ({app}-{apphost-path hash}-postgres-data) and the
    // mount it picks for the default postgres 18.x image — so an existing dev volume is reused.
    // The env var carries the mount path, NOT PGDATA: postgres 18 images keep PGDATA in a
    // version subdirectory of the mount (see the constants.cs Design note).
    // Published (task 070-003) the volume is always on and gets a FIXED name: the generated name
    // hashes the AppHost path, so every checkout would emit a different compose.yaml. Compose
    // prefixes it with the project name, so it never collides with the dev-loop volume.
    if (builder.ExecutionContext.IsPublishMode)
    {
      postgres = postgres.WithVolume(
        PostgresPublishedDataVolumeName,
        PostgresDataVolumeTarget,
        env: PostgresDataVolumeEnvironmentVariable);
    }
    else if (usePostgresDataVolume)
    {
      postgres = postgres.WithVolume(
        VolumeNameGenerator.Generate(postgres, "data"),
        PostgresDataVolumeTarget,
        env: PostgresDataVolumeEnvironmentVariable);
    }

    // Task 262 (Aspire 13.6): dashboard "REPL" command opens an authenticated psql shell in the
    // dashboard terminal dock. Anyone who can run dashboard commands gets the server credentials,
    // so it is Development-only (a tunnelled/shared dashboard in any other environment never
    // exposes it). WithRepl itself is run-mode only, so publish output is unaffected.
    if (builder.Environment.IsDevelopment())
    {
      postgres = postgres.WithRepl();
    }

    // Run mode creates the database through the AppHost; a published Compose stack has no AppHost,
    // so the postgres image's own POSTGRES_DB creates it on first initdb.
    if (builder.ExecutionContext.IsPublishMode)
    {
      postgres = postgres.WithEnvironment("POSTGRES_DB", PostgresDatabaseResourceName);
    }

    // Task 070-004: in the Helm chart the published postgres-data volume binds (by name) to a
    // PersistentVolumeClaim postgres-data, which renders postgres as a single-replica StatefulSet
    // mounting that claim.
    if (publishTarget.Kubernetes is not null)
    {
      postgres = postgres.WithPersistentVolume(
        publishTarget.Kubernetes.AddPersistentVolume(PostgresPublishedDataVolumeName)
          .WithCapacity(builder.AddParameter(PostgresStorageCapacityParameterName, "10Gi", publishValueAsDefault: true)));
    }

    return postgres.AddDatabase(PostgresDatabaseResourceName);
  }

  private static void AddMigrations
  (
    IDistributedApplicationBuilder builder,
    PublishTarget publishTarget,
    IResourceBuilder<ProjectResource> webServer,
    IResourceBuilder<IResourceWithConnectionString> postgresDb,
    IResourceBuilder<IResourceWithConnectionString>? neonDirect
  )
  {
    // Task 147-007: first-class EF migrations resource (aspire.dev AddEFMigrations).
    // Migrations live in web-infrastructure (not an AppHost project resource — path form; typed
    // Projects.* is only generated for Aspire project resources).
    string webInfrastructureProject = Path.GetFullPath(
      Path.Combine(builder.AppHostDirectory, "../../../web/projects/web-infrastructure/web-infrastructure.csproj"));
    IResourceBuilder<IResourceWithConnectionString> migrationConnection = neonDirect ?? postgresDb;
    IResourceBuilder<global::Aspire.Hosting.EntityFrameworkCore.EFMigrationResource> webMigrations = webServer
      .AddEFMigrations(WebMigrationsResourceName, "TimeWarp.Architecture.Persistence.PostgresDbContext")
      .WithMigrationsProject(webInfrastructureProject)
      .WithMigrationOutputDirectory("../../platform/postgres/migrations")
      .WithMigrationNamespace("TimeWarp.Architecture.Persistence.Migrations")
      .WithReference(migrationConnection, PostgresDatabaseResourceName)
      .RunDatabaseUpdateOnStart()
      .PublishAsMigrationScript();

    if (neonDirect is null)
    {
      webMigrations.WaitFor(postgresDb);
    }
    else
    {
      // Wake-up budget for Neon Free's suspend. The default retry (4 × 2s) is unchanged on every other target.
      // After AddEFMigrations so the snapshot on ef-tool-web-migrations does not also carry the pooled string.
      webServer
        .WithReference(postgresDb)
        .WithEnvironment("Postgres__RetryCount", AcaNeonCompute.RetryCount)
        .WithEnvironment("Postgres__RetryDelaySeconds", AcaNeonCompute.RetryDelaySeconds);
    }

    // Task 070-004: no bundle in the Helm chart. Publish output IS the chart directory, and Helm
    // refuses to load a chart holding the ~100 MB self-contained bundle (5 MiB per-file limit), so
    // helm lint and `aspire deploy` would both fail. Kubernetes migrates from the SQL script.
    if (publishTarget.Kubernetes is null)
    {
      webMigrations.PublishAsMigrationBundle();
    }
    // Task 155 / 270: no wait edge between web-server and web-migrations. Without a wait edge, a
    // dashboard restart/rebuild of web-server can never deadlock on the migration resource's
    // terminal Finished snapshot. WaitForCompletion still breaks DCP endpoint wiring on Aspire
    // 13.6 (see the Design region). web-server's boot seed waits until no migration is pending
    // (EfPendingMigrationsProbe, up to 180s — EF Core 11's verbose tool startup streams the
    // MSBuild item graph for about 90s before it migrates), so a first run or a new migration
    // logs no Error.
    // Re-run on demand via the ef-database-update dashboard command on the web-migrations resource.
  }
}
