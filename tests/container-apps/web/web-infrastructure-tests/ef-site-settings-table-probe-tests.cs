#region Purpose
// EfSiteSettingsTableProbe against real Postgres: false on an unmigrated database without any EF Error, true once migrated.
#endregion

#region Design
// Task 270: the probe exists so a first run against an empty database logs no Error. The proof is
// the EF logger itself — every log entry the context emits is captured, and the unmigrated probe
// must produce none at Error or above (a plain EfSiteSettingsStore read in the same state produces
// two, which the control case pins so the capture cannot pass vacuously). Database names are
// minted per test (probe_<guid>) on the shared PostgresTestAvailability server.
#endregion

namespace SiteSettingsTableProbe_.Ef;

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
          "EfSiteSettingsTableProbe tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] SiteSettingsTableProbe_.Ef: {availability.SkipReason ?? "no connection"}");
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

public class Unmigrated_Database
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Unmigrated_Database>();

  public static async Task Probe_Should_Report_Missing_Without_Error_Then_Present_After_Migrate()
  {
    if (!ProbeDatabase.IsAvailable)
    {
      return;
    }

    CapturingLoggerProvider logs = new();
    await using PostgresDbContext db = ProbeDatabase.CreateEmpty(logs);
    EfSiteSettingsTableProbe probe = new(db);

    (await probe.ExistsAsync()).ShouldBeFalse();
    logs.ErrorsAndAbove.ShouldBeEmpty(string.Join(Environment.NewLine, logs.ErrorsAndAbove));

    await db.Database.MigrateAsync();

    (await probe.ExistsAsync()).ShouldBeTrue();
  }

  public static async Task Store_Read_Should_Log_Error_Control()
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
