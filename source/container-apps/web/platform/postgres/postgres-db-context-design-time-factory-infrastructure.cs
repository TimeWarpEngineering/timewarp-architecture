#region Purpose
// Design-time factory for PostgresDbContext so `dotnet ef` and Aspire RunDatabaseUpdateOnStart resolve a provider + connection.
#endregion

#region Design
// Contract (task 147-007): prefer a real connection string so `dotnet ef database update` (used by
// Aspire AddEFMigrations RunDatabaseUpdateOnStart) can reach Postgres. Resolution order:
//   1. ConnectionStrings:postgres-db — literal matches Constants.PostgresDatabaseResourceName /
//      AppHost database resource name (ConnectionStrings__postgres-db). Keep the literal here so
//      web-infrastructure does not reference AppHost constants.
//   2. PostgresDbOptions:ConnectionString — non-Aspire hand-wiring path (same key as PostgresDbModule).
//   3. Dummy Npgsql string — offline `migrations add` scaffolding only (no live DB required).
// Factory is discovered by EF design-time by type name convention; no DI registration.
#endregion

namespace TimeWarp.Architecture.Persistence;

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>Creates <see cref="PostgresDbContext"/> for EF tools and Aspire migration resources.</summary>
public sealed class PostgresDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
  // Keep in lockstep with Aspire Constants.PostgresDatabaseResourceName ("postgres-db").
  private const string PostgresDatabaseConnectionName = "postgres-db";

  private const string OfflineScaffoldConnectionString =
    "Host=127.0.0.1;Port=5432;Database=timewarp_design_time;Username=timewarp;Password=timewarp";

  public PostgresDbContext CreateDbContext(string[] args)
  {
    string connectionString = ResolveConnectionString() ?? OfflineScaffoldConnectionString;

    DbContextOptionsBuilder<PostgresDbContext> optionsBuilder = new();
    optionsBuilder.UseNpgsql(connectionString);
    return new PostgresDbContext(optionsBuilder.Options);
  }

  private static string? ResolveConnectionString()
  {
    IConfigurationRoot configuration = new ConfigurationBuilder()
      .AddEnvironmentVariables()
      .AddJsonFile("appsettings.json", optional: true)
      .AddJsonFile("appsettings.Development.json", optional: true)
      .Build();

    string? fromConnectionStrings = configuration.GetConnectionString(PostgresDatabaseConnectionName);
    if (!string.IsNullOrWhiteSpace(fromConnectionStrings))
    {
      return fromConnectionStrings;
    }

    string? fromOptions = configuration["PostgresDbOptions:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(fromOptions))
    {
      return fromOptions;
    }

    return null;
  }
}
