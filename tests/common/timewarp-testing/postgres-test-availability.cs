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
// Task 372: under parallel `dev test` on Docker Desktop, 3 of 21 local integration runs failed a
// container start here: once the first connection was refused (Testcontainers' postgres wait runs
// inside the container, so StartAsync can return before the host port forwards), twice Ryuk's
// initialization timed out. So StartAsync is followed by a host-side TCP probe of the mapped port
// (no Npgsql dependency: this file compiles with the postgres flag off too), and a start that fails
// either way is disposed and tried once more. Docker readiness is outside our control; the retry
// is bounded and a second failure still surfaces.
#endregion

namespace TimeWarp.Architecture.Testing;

using Docker.DotNet;
using DotNet.Testcontainers.Containers;
using System.Data.Common;
using System.Net.Sockets;
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
      PostgreSqlContainer container = await StartContainerAsync(containerDatabaseName);
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

  private static async Task<PostgreSqlContainer> StartContainerAsync(string containerDatabaseName)
  {
    const int attempts = 2;
    for (int attempt = 1; ; attempt++)
    {
      PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase(containerDatabaseName)
        .WithUsername("timewarp")
        .WithPassword("timewarp")
        .Build();
      try
      {
        await container.StartAsync();
        await WaitForHostPortAsync(container.Hostname, container.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort));
        return container;
      }
      catch (Exception exception) when (exception is ResourceReaperException or TimeoutException)
      {
        await container.DisposeAsync();
        if (attempt >= attempts) throw;
        Console.WriteLine($"[RETRY] Postgres container start (attempt {attempt} of {attempts}): {exception.Message}");
      }
    }
  }

  // Throws TimeoutException (a skip reason off CI) when the mapped port never accepts.
  private static async Task WaitForHostPortAsync(string host, int port)
  {
    DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
    while (true)
    {
      try
      {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        using TcpClient client = new();
        await client.ConnectAsync(host, port, timeout.Token);
        return;
      }
      catch (Exception exception) when ((exception is SocketException or OperationCanceledException) && DateTime.UtcNow < deadline)
      {
        await Task.Delay(TimeSpan.FromMilliseconds(200));
      }
      catch (Exception exception) when (exception is SocketException or OperationCanceledException)
      {
        throw new TimeoutException($"Postgres container port {host}:{port} did not accept connections within 30s.", exception);
      }
    }
  }
}
