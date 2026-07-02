#region Purpose
// Bundles every PostgreSQL registration (DbContext, health check, environment check, schema-creation service) behind one opt-in module.
#endregion

#region Design
// IModule keeps Program.ConfigureServices a single call site to comment in/out with the
// `postgres` feature flag — all Postgres coupling lives here.
// ConfigurePostgresDb builds a throwaway provider to read PostgresDbOptions because the
// connection string is needed while the collection is still being composed; accepted smell,
// scoped to this method.
// Health check (liveness) and environment check (startup gate) intentionally share the same
// CanConnectAsync probe.
#endregion

namespace TimeWarp.Architecture.Modules;

public sealed partial class PostgresDbModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    ConfigureInfrastructure(serviceCollection);
  }

  private static void ConfigureInfrastructure(IServiceCollection serviceCollection)
  {
    IHealthChecksBuilder healthChecksBuilder = serviceCollection.AddHealthChecks();
    healthChecksBuilder.AddDbContextCheck<PostgresDbContext>
    (
        name: nameof(PostgresDbContext),
        HealthStatus.Unhealthy,
        null,
        PerformPostgresHealthCheck()
    );
    ConfigureEnvironmentChecks(serviceCollection);
    ConfigurePostgresDb(serviceCollection);
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

  private static void ConfigurePostgresDb(IServiceCollection serviceCollection)
  {
    using IServiceScope scope = serviceCollection.BuildServiceProvider().CreateScope();
    {
      PostgresDbOptions postgresDbOptions = scope.ServiceProvider.GetRequiredService<IOptions<PostgresDbOptions>>().Value;

      _ = serviceCollection.AddDbContext<PostgresDbContext>
      (
          dbContextOptionsBuilder =>
              dbContextOptionsBuilder
              .UseNpgsql
              (
                  connectionString: postgresDbOptions.ConnectionString
              )
      );
    }
  }

  private static Action<NpgsqlDbContextOptionsBuilder> NpgsqlOptionsAction(string databaseName)
  {
    return builder => builder.UseAdminDatabase(databaseName);
  }

  private static Func<PostgresDbContext, CancellationToken, Task<bool>> PerformPostgresHealthCheck() =>
      async (postgresDbContext, _) =>
      {
        try
        {
          await postgresDbContext.Database.CanConnectAsync().ConfigureAwait(true);
        }
        catch (HttpRequestException)
        {
          return false;
        }

        return true;
      };
}
