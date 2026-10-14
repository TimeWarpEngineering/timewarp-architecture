#region Purpose
// EfPendingMigrationsProbe against real Postgres: fresh, partially migrated, and fully migrated databases, with no EF Error.
#endregion

#region Design
// Task 363 (was task 270's table probe): the probe exists so a boot racing web-migrations logs no
// Error. The proof is the EF logger itself — every log entry the context emits is captured, and
// the probe must produce none at Error or above on a fresh database (no __EFMigrationsHistory). A
// plain EfSiteSettingsStore read on a fresh database produces Error entries, which the control
// case pins so the capture cannot pass vacuously. Partial migration uses IMigrator with the
// second-to-last compiled migration as target. Database names are minted per test
// (probe_<guid>) on the shared PostgresTestAvailability server.
#endregion

namespace PendingMigrationsProbe_.Ef;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Concurrent;
using TimeWarp.Architecture.Features.Settings.Infrastructure;
using TimeWarp.Architecture.Testing;

file sealed class ProbeDatabase
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_site_settings_probe_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

  public static bool IsAvailable
  {
    get
    {
      PostgresTestAvailability availability = Availability.Value.GetAwaiter().GetResult();
      if (availability.AdminConnectionString is not null)
      {
        return true;
      }

      if (PostgresTestAvailability.IsCiEnvironment())
      {
        throw new InvalidOperationException(
          "EfPendingMigrationsProbe tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] PendingMigrationsProbe_.Ef: {availability.SkipReason ?? "no connection"}");
      return false;
    }
  }

  /// <summary>A new, empty (unmigrated) database whose context logs into <paramref name="logs"/>.</summary>
  public static PostgresDbContext CreateEmpty(CapturingLoggerProvider logs)
  {
    PostgresTestAvailability availability = Availability.Value.GetAwaiter().GetResult();
    string adminConnectionString = availability.AdminConnectionString
      ?? throw new InvalidOperationException(availability.SkipReason ?? "Postgres is not available.");

    string databaseName = "probe_" + Guid.NewGuid().ToString("N");
    using (NpgsqlConnection connection = new(adminConnectionString))
    {
      connection.Open();
      using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // databaseName is minted above from a Guid, never caller input.
      command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
      command.ExecuteNonQuery();
    }

    string connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString, PostgresRetryPolicy.Configure)
      .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Trace)))
      .Options;
    return new PostgresDbContext(options);
  }
}

file sealed class CapturingLoggerProvider : ILoggerProvider
{
  public ConcurrentQueue<(LogLevel Level, string Category, string Message)> Entries { get; } = new();

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

public class Probe_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Probe_Given_>();

  public static async Task Fresh_Database_Should_Report_Every_Migration_Without_Error()
  {
    if (!ProbeDatabase.IsAvailable)
    {
      return;
    }

    CapturingLoggerProvider logs = new();
    await using PostgresDbContext db = ProbeDatabase.CreateEmpty(logs);
    EfPendingMigrationsProbe probe = new(db);

    IReadOnlyList<string> pending = await probe.GetPendingMigrationsAsync();

    pending.ShouldBe([.. db.Database.GetMigrations()]);
    pending.ShouldNotBeEmpty();
    logs.ErrorsAndAbove.ShouldBeEmpty(string.Join(Environment.NewLine, logs.ErrorsAndAbove));
  }

  public static async Task Partially_Migrated_Database_Should_Report_Only_The_Latest_Migration()
  {
    if (!ProbeDatabase.IsAvailable)
    {
      return;
    }

    CapturingLoggerProvider logs = new();
    await using PostgresDbContext db = ProbeDatabase.CreateEmpty(logs);
    string[] migrations = [.. db.Database.GetMigrations()];
    await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
    logs.Entries.Clear(); // MigrateAsync's own history read on the fresh database is not the probe's.
    EfPendingMigrationsProbe probe = new(db);

    IReadOnlyList<string> pending = await probe.GetPendingMigrationsAsync();

    pending.ShouldBe([migrations[^1]]);
    logs.ErrorsAndAbove.ShouldBeEmpty(string.Join(Environment.NewLine, logs.ErrorsAndAbove));
  }

  public static async Task Fully_Migrated_Database_Should_Report_None()
  {
    if (!ProbeDatabase.IsAvailable)
    {
      return;
    }

    CapturingLoggerProvider logs = new();
    await using PostgresDbContext db = ProbeDatabase.CreateEmpty(logs);
    await db.Database.MigrateAsync();
    logs.Entries.Clear(); // MigrateAsync's own history read on the fresh database is not the probe's.
    EfPendingMigrationsProbe probe = new(db);

    (await probe.GetPendingMigrationsAsync()).ShouldBeEmpty();
    logs.ErrorsAndAbove.ShouldBeEmpty(string.Join(Environment.NewLine, logs.ErrorsAndAbove));
  }
}

public class Store_Read_Control
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Store_Read_Control>();

  public static async Task Fresh_Database_Read_Should_Log_Error()
  {
    if (!ProbeDatabase.IsAvailable)
    {
      return;
    }

    // Control: the read the probe guards really does log Error on an unmigrated database — the
    // first-run symptom (EF "Failed executing DbCommand" for 42P01). Proves the capture works.
    CapturingLoggerProvider logs = new();
    await using PostgresDbContext db = ProbeDatabase.CreateEmpty(logs);
    EfSiteSettingsStore store = new(db);

    await Should.ThrowAsync<Exception>(() => store.GetAsync());

    logs.ErrorsAndAbove.ShouldNotBeEmpty();
  }
}
