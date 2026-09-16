#region Purpose
// Boot-time site-settings seed, drift warnings, and Development-only ReseedSiteSettings.
#endregion

#region Design
// IHostedLifecycleService.StartingAsync runs before Kestrel StartAsync, so the empty-store seed
// completes before the server accepts requests. Creates a scope because EfSiteSettingsStore is
// scoped under postgres. Seeder is application-layer; this type is the server adapter. Only
// SiteSettingsSeeder writes the empty-store row — Settings Get/Update return 503 until seeded.
// CI fix (post-review, task 219-006): the AppHost has NO wait edge between web-server and
// web-migrations (postgres-db-module-server.cs Design region, task 155 — WaitFor deadlocked
// dashboard restarts, WaitForCompletion broke DCP under Aspire.Hosting.Testing), so on a fresh
// Postgres volume this seed can run before web-migrations has created identity.site_settings
// (Npgsql 42P01, "relation does not exist"). Bounded retry (1s backoff, up to MaxAttempts) rides
// out that same accepted first-boot race instead of crashing the host. In-memory builds never
// throw 42P01, so the loop is a harmless single pass there — the seed still completes
// synchronously before Kestrel starts, unchanged from before this fix.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Identity.Application;

public sealed class SiteSettingsSeedHostedService : IHostedLifecycleService
{
  private const int MaxAttempts = 30;
  private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

  private static readonly Action<ILogger, int, int, Exception?> LogUndefinedTableRetry =
    LoggerMessage.Define<int, int>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogUndefinedTableRetry)),
      "Site settings seed found identity.site_settings undefined (attempt {Attempt}/{MaxAttempts}); retrying — web-migrations has not applied yet."
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

    for (int attempt = 1; attempt <= MaxAttempts; attempt++)
    {
      try
      {
        _ = await seeder.GetOrSeedAsync(isDevelopment, cancellationToken).ConfigureAwait(false);
        return;
      }
      catch (Exception exception) when (attempt < MaxAttempts && IsUndefinedTable(exception))
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

  // String-based detection (same convention as EfSiteSettingsStore.IsUniqueViolation) so this
  // server-layer file does not need a direct Npgsql package reference just to catch one SQLSTATE.
  private static bool IsUndefinedTable(Exception exception)
  {
    Exception? current = exception;
    while (current is not null)
    {
      string text = current.GetType().FullName + " " + current.Message;
      if (text.Contains("42P01", StringComparison.Ordinal))
      {
        return true;
      }

      current = current.InnerException;
    }

    return false;
  }
}
