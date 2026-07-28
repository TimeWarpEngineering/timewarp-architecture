#region Purpose
// Bundles every PostgreSQL registration (DbContext, health check, environment check, schema-creation service) behind one opt-in module.
#endregion

#region Design
// IModule keeps Program.ConfigureServices a single gated call site (the postgres template feature
// flag compiles the call in or out); all Postgres coupling lives here.
// Connection string resolution has two sources, checked in order:
//   1. The PostgresDbOptions:ConnectionString configuration section — the non-Aspire hosting path
//      (a consumer wiring Postgres by hand via appsettings, environment, or user secrets).
//   2. The Aspire-injected ConnectionStrings key, whose name IS the database resource name
//      (constants.cs PostgresDatabaseResourceName); Aspire sets it on every run via WithReference.
// If neither yields a value the module registers NOTHING and returns. Rationale: the Web.Server
// integration tests direct-host the app WITHOUT Aspire (no injected string), and a template consumer
// who has the postgres flag but no configured database must still get a running app rather than a
// startup failure. Under Aspire the string is always present, so the real registrations always run.
// Skip-mode also keeps InMemoryIdentityStoresModule's singleton InMemoryPrincipalStore — only when a
// connection is present do we Replace the IPrincipalStore registration with scoped EfPrincipalStore
// (task 104-032). Challenge/token stores stay in-memory either way.
// The connection string is read once and reused for both Configure<PostgresDbOptions> (the
// environment check consumes IOptions<PostgresDbOptions>) and AddDbContext, so the two cannot drift.
// Health check (liveness) and environment check (startup gate) intentionally share the same
// CanConnectAsync probe.
#endregion

namespace TimeWarp.Architecture.Modules;

using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeWarp.Identity;

public sealed partial class PostgresDbModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    string? connectionString =
      configuration["PostgresDbOptions:ConnectionString"]
      ?? configuration.GetConnectionString(PostgresDatabaseResourceName);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
      // Unconfigured (no Aspire injection, no explicit config): leave Postgres entirely
      // unregistered so direct-host integration tests and unconfigured consumers still boot.
      // IPrincipalStore stays the InMemoryIdentityStoresModule in-memory singleton.
      return;
    }

    serviceCollection.Configure<PostgresDbOptions>(options => options.ConnectionString = connectionString);

    _ = serviceCollection.AddDbContext<PostgresDbContext>
    (
      dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql(connectionString)
    );

    // Durable principal store only when EF is actually registered (skip-mode keeps in-memory).
    serviceCollection.RemoveAll<IPrincipalStore>();
    serviceCollection.AddScoped<IPrincipalStore, EfPrincipalStore>();

    IHealthChecksBuilder healthChecksBuilder = serviceCollection.AddHealthChecks();
    healthChecksBuilder.AddDbContextCheck<PostgresDbContext>
    (
        name: nameof(PostgresDbContext),
        HealthStatus.Unhealthy,
        null,
        PerformPostgresHealthCheck()
    );

    ConfigureEnvironmentChecks(serviceCollection);
    serviceCollection.AddHostedService<PostgresDbContextStartupHostedService>();
  }

  private static void ConfigureEnvironmentChecks(IServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<PostgresDbEnvironmentCheck>();
    serviceCollection.CheckEnvironment<PostgresDbEnvironmentCheck>
    (
        PostgresDbEnvironmentCheck.Description,
        async (postgresDbEnvironmentCheck) => await postgresDbEnvironmentCheck.CheckAsync()
    );
  }

  private static Func<PostgresDbContext, CancellationToken, Task<bool>> PerformPostgresHealthCheck() =>
      async (postgresDbContext, cancellationToken) =>
      {
        try
        {
          return await postgresDbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception)
        {
          // Any failure reaching the database (network, auth, provider) means unhealthy.
          return false;
        }
      };
}
