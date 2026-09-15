#region Purpose
// Boot-time site-settings seed and configured-vs-enabled mismatch log.
#endregion

#region Design
// IHostedService so the empty-store seed and the one-shot mismatch log run at host start, not on
// the first request. Creates a scope because EfSiteSettingsStore is scoped under postgres.
// Seeder is application-layer; this type is the server adapter.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.Identity.Application;

public sealed class SiteSettingsSeedHostedService : IHostedService
{
  private readonly IServiceScopeFactory ServiceScopeFactory;

  public SiteSettingsSeedHostedService(IServiceScopeFactory serviceScopeFactory)
  {
    ServiceScopeFactory = serviceScopeFactory;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    using IServiceScope scope = ServiceScopeFactory.CreateScope();
    SiteSettingsSeeder seeder = scope.ServiceProvider.GetRequiredService<SiteSettingsSeeder>();
    _ = await seeder.GetOrSeedAsync(cancellationToken).ConfigureAwait(false);
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
