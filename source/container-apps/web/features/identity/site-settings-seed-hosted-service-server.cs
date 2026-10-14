#region Purpose
// Boot-time site-settings seed, drift warnings, and Development-only ReseedSiteSettings.
#endregion

#region Design
// IHostedLifecycleService.StartingAsync runs before Kestrel StartAsync, so the empty-store seed
// completes before the server accepts requests. Creates a scope because EfSiteSettingsStore is
// scoped under postgres. Seeder is application-layer; this type is the server adapter. Since task
// 254 the store read seeds on empty too (SeedOnReadSiteSettingsStore), so this boot pass is no
// longer what makes Settings work — it keeps the first request fast, applies Development
// ReseedSiteSettings once per boot, and emits the drift warnings at startup.
// The AppHost has NO wait edge between web-server and web-migrations (AppHost program.cs Design
// region: WaitFor deadlocks dashboard restarts; WaitForCompletion breaks DCP endpoint wiring,
// re-confirmed on Aspire 13.6 in task 270), so this seed can run before web-migrations finishes —
// on an empty database (42P01) or after a new migration changed a table it reads (42703, the
// 2026-10-14 AssistantName incident). Task 363: each attempt asks IPendingMigrationsProbe and
// reads through EF only once the database has every migration this build was compiled with. A
// failing EF query logs Error inside EF before any catch here runs, and a clean boot must log none.
// Pending migrations are expected while web-migrations runs, so early attempts log Information
// (count and latest id); from attempt QuietAttempts + 1 the wait logs Warning, because migrations
// should have finished by then.
// EF Core 11 tools always run under Aspire's `--verbose`. That flag prints the full
// `dotnet build -getProperty` item graph (thousands of lines) before it opens a connection.
// On RC1 that stream took about 90s through the AppHost log pipe, so the old 30s budget
// expired, the host died on 42P01, and ingress never became healthy. 180s covers that
// startup plus applying the migrations; warnings begin only after two minutes.
// An exhausted budget fails the host with an InvalidOperationException naming the pending
// migrations, never a raw 42P01 / 42703 from a read against a stale schema. The 42P01 catch stays
// for the narrow race where the table disappears between probe and query (`dev db reset` during a
// restart); the next attempt probes again. With no probe registered (in-memory builds) the loop is
// a single seed pass before Kestrel starts.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Persistence;

public sealed class SiteSettingsSeedHostedService : IHostedLifecycleService
{
  private const int MaxAttempts = 180;
  private const int QuietAttempts = 120;
  private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

  private static readonly Action<ILogger, int, int, Exception?> LogUndefinedTableRetry =
    LoggerMessage.Define<int, int>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogUndefinedTableRetry)),
      "Site settings seed found identity.site_settings undefined (attempt {Attempt}/{MaxAttempts}); retrying — web-migrations has not applied yet."
    );

  private static readonly Action<ILogger, int, string, int, int, Exception?> LogMigrationsPending =
    LoggerMessage.Define<int, string, int, int>
    (
      LogLevel.Information,
      new EventId(2, nameof(LogMigrationsPending)),
      "Site settings seed is waiting for web-migrations: {PendingCount} pending migration(s) (latest {LatestMigration}) (attempt {Attempt}/{MaxAttempts})."
    );

  private static readonly Action<ILogger, int, string, int, int, Exception?> LogMigrationsStillPending =
    LoggerMessage.Define<int, string, int, int>
    (
      LogLevel.Warning,
      new EventId(3, nameof(LogMigrationsStillPending)),
      "Site settings seed is still waiting for {PendingCount} pending migration(s) (latest {LatestMigration}) (attempt {Attempt}/{MaxAttempts}); check that web-migrations ran."
    );

  private readonly IServiceScopeFactory ServiceScopeFactory;
  private readonly ILogger<SiteSettingsSeedHostedService> Logger;

  public SiteSettingsSeedHostedService(IServiceScopeFactory serviceScopeFactory, ILogger<SiteSettingsSeedHostedService> logger)
  {
    ServiceScopeFactory = serviceScopeFactory;
    Logger = logger;
  }

  public async Task StartingAsync(CancellationToken cancellationToken)
  {
    using IServiceScope scope = ServiceScopeFactory.CreateScope();
    SiteSettingsSeeder seeder = scope.ServiceProvider.GetRequiredService<SiteSettingsSeeder>();
    IHostEnvironment hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    bool isDevelopment = hostEnvironment.IsDevelopment();
    IPendingMigrationsProbe? migrationsProbe = scope.ServiceProvider.GetService<IPendingMigrationsProbe>();

    for (int attempt = 1; attempt <= MaxAttempts; attempt++)
    {
      if (migrationsProbe is not null)
      {
        IReadOnlyList<string> pending = await migrationsProbe.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        if (pending.Count > 0)
        {
          if (attempt == MaxAttempts)
          {
            throw new InvalidOperationException(
              $"Site settings seed gave up after {MaxAttempts} attempts: the database is missing {pending.Count} migration(s) " +
              $"this web-server was compiled with ({string.Join(", ", pending)}). Run web-migrations " +
              "(the ef-database-update command on the web-migrations resource) and restart web-server.");
          }

          if (attempt <= QuietAttempts)
          {
            LogMigrationsPending(Logger, pending.Count, pending[^1], attempt, MaxAttempts, null);
          }
          else
          {
            LogMigrationsStillPending(Logger, pending.Count, pending[^1], attempt, MaxAttempts, null);
          }

          await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
          continue;
        }
      }

      try
      {
        _ = await seeder.GetOrSeedAsync(isDevelopment, cancellationToken).ConfigureAwait(false);
        return;
      }
      catch (Exception exception) when (attempt < MaxAttempts && SiteSettingsSeeder.IsUndefinedTable(exception))
      {
        LogUndefinedTableRetry(Logger, attempt, MaxAttempts, exception);
        await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
      }
    }
  }

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
