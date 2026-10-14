#region Purpose
// Regression: web-server booting against a database one migration behind waits, logs no Error, then serves once the migration lands.
#endregion

#region Design
// Task 363 (2026-10-14 incident: web-server crashed with 42703 on SiteSettings.AssistantName
// because the AppHost has no wait edge on web-migrations and task 270's probe only checked that
// identity.site_settings existed). The database is migrated to the second-to-last compiled
// migration, then a real Web.Server host boots against it on a background task. The boot must
// still be blocked a few seconds later (before task 363 it started at once, because the table
// existed), log the pending-migrations wait, and finish only after this test applies the latest
// migration. No Error-or-above entry may be logged by any category, EF included. The host is
// created and disposed inside the test (not SetupOnce) because its boot is what is under test.
// Template-excluded when postgres is off. Postgres comes from PostgresTestAvailability (env
// connection string or Testcontainers); a fresh database is minted per run.
#endregion

namespace SiteSettingsBootPendingMigrations_;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Concurrent;
using System.Net;
using TimeWarp.Architecture.Persistence;

[TestTag("Settings")]
public class Boot_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Boot_Given_>();

  public static async Task Database_One_Migration_Behind_Should_Wait_Then_Serve()
  {
    PostgresTestAvailability availability = await PostgresTestAvailability.ResolveAsync("timewarp_boot_pending_migrations_tests");
    if (availability.AdminConnectionString is not { } adminConnectionString)
    {
      if (PostgresTestAvailability.IsCiEnvironment())
      {
        throw new InvalidOperationException("Boot pending-migrations regression requires Postgres under CI. " + availability.SkipReason);
      }

      Console.WriteLine($"[SKIP] SiteSettingsBootPendingMigrations_: {availability.SkipReason ?? "no connection"}");
      return;
    }

    string connectionString = await CreateDatabaseAsync(adminConnectionString);
    string latestMigration;
    await using (PostgresDbContext db = CreateContext(connectionString))
    {
      string[] migrations = [.. db.Database.GetMigrations()];
      latestMigration = migrations[^1];
      await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
    }

    CapturingLoggerProvider logs = new();
    Dictionary<string, string?> configuration = new(WebTestServerApplication.HttpsServiceEndpoints)
    {
      ["PostgresDbOptions:ConnectionString"] = connectionString,
    };
    Task<HostGraph> boot = Task.Run(() => HostGraphFactory.CreateWebAsync(
      configureWeb: services => services.AddSingleton<ILoggerProvider>(logs),
      serviceEndpoints: configuration));

    await Task.Delay(TimeSpan.FromSeconds(3));
    if (boot.IsCompleted)
    {
      await using HostGraph early = await boot;
      throw new ShouldAssertException($"web-server started while {latestMigration} was still pending.");
    }

    logs.Messages.ShouldContain(message => message.Contains("pending migration", StringComparison.Ordinal)
      && message.Contains(latestMigration, StringComparison.Ordinal));

    await using (PostgresDbContext db = CreateContext(connectionString))
    {
      await db.Database.MigrateAsync();
    }

    await using HostGraph graph = await boot.WaitAsync(TimeSpan.FromSeconds(30));
    HttpResponseMessage response = await graph.Web!.HttpClient.GetAsync("api/settings/assistant");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    logs.ErrorsAndAbove.ShouldBeEmpty(string.Join(Environment.NewLine, logs.ErrorsAndAbove));
  }

  private static async Task<string> CreateDatabaseAsync(string adminConnectionString)
  {
    string databaseName = "boot_" + Guid.NewGuid().ToString("N");
    await using NpgsqlConnection connection = new(adminConnectionString);
    await connection.OpenAsync();
    await using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // databaseName is minted above from a Guid, never caller input.
    command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
    await command.ExecuteNonQueryAsync();
    return new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
  }

  private static PostgresDbContext CreateContext(string connectionString) =>
    new(new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(connectionString).Options);
}

file sealed class CapturingLoggerProvider : ILoggerProvider
{
  private readonly ConcurrentQueue<(LogLevel Level, string Category, string Message)> Entries = new();

  public IReadOnlyList<string> Messages => [.. Entries.Select(entry => entry.Message)];

  public IReadOnlyList<string> ErrorsAndAbove =>
    [.. Entries.Where(entry => entry.Level >= LogLevel.Error).Select(entry => $"{entry.Category}: {entry.Message}")];

  public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

  public void Dispose() { }

  private sealed class CapturingLogger(CapturingLoggerProvider provider, string category) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
      provider.Entries.Enqueue((logLevel, category, formatter(state, exception)));
  }
}
