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
// re-confirmed on Aspire 13.6 in task 270), so on a fresh Postgres volume this seed can run before
// web-migrations has created identity.site_settings. Bounded wait (1s backoff, up to MaxAttempts)
// rides out that first-boot race instead of crashing the host.
// Task 270: each attempt first asks ISiteSettingsTableProbe (a catalog lookup that cannot fail on
// a missing table) and only queries through EF once the table exists — a failing EF query logs
// two Error entries inside EF before any catch here runs, and a clean first run must log none.
// A missing table is expected on a first run, so early attempts log Information; from attempt
// QuietAttempts + 1 the wait logs Warning, because migrations should have finished by then.
// EF Core 11 tools always run under Aspire's `--verbose`. That flag prints the full
// `dotnet build -getProperty` item graph (thousands of lines) before it opens a connection.
// On RC1 that stream took about 90s through the AppHost log pipe, so the old 30s budget
// expired, the host died on 42P01, and ingress never became healthy. 180s covers that
// startup plus applying the migrations; warnings begin only after two minutes.
// The last attempt skips the probe and runs the seed anyway, so an exhausted budget still fails the
// host with the real 42P01 rather than a silent skip. The 42P01 catch stays for the narrow race
// where the table disappears between probe and query (`dev db reset` during a restart). With no
// probe registered (in-memory builds) the loop is a single seed pass before Kestrel starts.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Identity.Application;

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

  private static readonly Action<ILogger, int, int, Exception?> LogTableNotMigratedYet =
    LoggerMessage.Define<int, int>
    (
      LogLevel.Information,
      new EventId(2, nameof(LogTableNotMigratedYet)),
      "Site settings seed is waiting for web-migrations to create identity.site_settings (attempt {Attempt}/{MaxAttempts})."
    );

  private static readonly Action<ILogger, int, int, Exception?> LogTableStillNotMigrated =
    LoggerMessage.Define<int, int>
    (
      LogLevel.Warning,
      new EventId(3, nameof(LogTableStillNotMigrated)),
      "Site settings seed is still waiting for identity.site_settings (attempt {Attempt}/{MaxAttempts}); check that web-migrations ran."
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
    ISiteSettingsTableProbe? tableProbe = scope.ServiceProvider.GetService<ISiteSettingsTableProbe>();

    for (int attempt = 1; attempt <= MaxAttempts; attempt++)
    {
      if (tableProbe is not null
        && attempt < MaxAttempts
        && !await tableProbe.ExistsAsync(cancellationToken).ConfigureAwait(false))
      {
        if (attempt <= QuietAttempts)
        {
          LogTableNotMigratedYet(Logger, attempt, MaxAttempts, null);
        }
        else
        {
          LogTableStillNotMigrated(Logger, attempt, MaxAttempts, null);
        }

        await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
        continue;
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
