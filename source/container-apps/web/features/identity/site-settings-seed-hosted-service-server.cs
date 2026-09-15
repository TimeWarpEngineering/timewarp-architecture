#region Purpose
// Boot-time site-settings seed and configured-vs-enabled mismatch log.
#endregion

#region Design
// IHostedLifecycleService.StartingAsync runs before Kestrel StartAsync, so the empty-store seed
// completes before the server accepts requests. Creates a scope because EfSiteSettingsStore is
// scoped under postgres. Seeder is application-layer; this type is the server adapter. Only
// SiteSettingsSeeder writes the empty-store row — Settings Get/Update return 503 until seeded.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.Identity.Application;

public sealed class SiteSettingsSeedHostedService : IHostedLifecycleService
{
  private readonly IServiceScopeFactory ServiceScopeFactory;

  public SiteSettingsSeedHostedService(IServiceScopeFactory serviceScopeFactory)
  {
    ServiceScopeFactory = serviceScopeFactory;
  }

  public async Task StartingAsync(CancellationToken cancellationToken)
  {
    using IServiceScope scope = ServiceScopeFactory.CreateScope();
    SiteSettingsSeeder seeder = scope.ServiceProvider.GetRequiredService<SiteSettingsSeeder>();
    _ = await seeder.GetOrSeedAsync(cancellationToken).ConfigureAwait(false);
  }

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
