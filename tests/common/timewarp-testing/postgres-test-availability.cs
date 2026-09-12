#region Purpose
// Shared Postgres availability for infrastructure tests: env connection string or Testcontainers.
#endregion

#region Design
// Three web-infrastructure suites previously copy-pasted env/Testcontainers/CI-skip logic with
// shape drift (ConnectionString vs AdminConnectionString; Container kept or dropped). One helper
// prefers PostgresDbOptions__ConnectionString / ConnectionStrings__postgres-db, else starts
// postgres:16-alpine. AdminConnectionString rewrites Database=postgres so callers can CREATE
// DATABASE per test. The Testcontainers instance is retained so Ryuk is not the only lifetime
// owner. CI fails closed via IsCiEnvironment; interactive hosts soft-skip.
#endregion

namespace TimeWarp.Architecture.Testing;

using System.Data.Common;
using Docker.DotNet;
using Testcontainers.PostgreSql;

/// <summary>
/// Resolves a process-scoped Postgres connection for infrastructure tests.
/// </summary>
public sealed class PostgresTestAvailability
{
  public string? ConnectionString { get; }
  public PostgreSqlContainer? Container { get; }
  public string? SkipReason { get; }
  public bool IsAvailable => ConnectionString is not null;

  public string? AdminConnectionString
  {
    get
    {
      if (ConnectionString is null)
      {
        return null;
      }

      DbConnectionStringBuilder builder = new() { ConnectionString = ConnectionString };
      builder["Database"] = "postgres";
      return builder.ConnectionString;
    }
  }

  private PostgresTestAvailability(
    string? connectionString,
    PostgreSqlContainer? container,
    string? skipReason)
  {
    ConnectionString = connectionString;
    Container = container;
    SkipReason = skipReason;
  }

  public static bool IsCiEnvironment() =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

  public static async Task<PostgresTestAvailability> ResolveAsync(string containerDatabaseName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(containerDatabaseName);

    string? fromEnv = Environment.GetEnvironmentVariable("PostgresDbOptions__ConnectionString")
      ?? Environment.GetEnvironmentVariable("ConnectionStrings__postgres-db");

    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
      return new PostgresTestAvailability(fromEnv, container: null, skipReason: null);
    }

    try
    {
      PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase(containerDatabaseName)
        .WithUsername("timewarp")
        .WithPassword("timewarp")
        .Build();

      await container.StartAsync();
      return new PostgresTestAvailability(container.GetConnectionString(), container, skipReason: null);
    }
    catch (Exception exception) when (
      exception is DockerApiException
        or DockerContainerNotFoundException
        or HttpRequestException
        or TimeoutException
        or IOException)
    {
      string skipReason =
        "No Postgres connection available (set PostgresDbOptions__ConnectionString or " +
        "ConnectionStrings__postgres-db, or enable Docker for Testcontainers). " +
        exception.Message;
      return new PostgresTestAvailability(null, null, skipReason);
    }
  }
}
