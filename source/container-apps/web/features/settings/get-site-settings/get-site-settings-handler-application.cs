#region Purpose
// Server-side handler for GetSiteSettings: return the singleton snapshot, creating factory defaults if empty.
#endregion

#region Design
// Application takes ISiteSettingsStore, not PostgresDbContext and not Identity.Application types
// (TWA0009). First-run copy from Authentication:Entra lives in SiteSettingsSeedHostedService.
// If this handler races the seed, factory defaults (all false, Soft) are inserted; the seeder
// Add then no-ops on the existing row. Tenant ids serialize as D-format GUID strings.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Settings.GetSiteSettings;

public sealed class GetSiteSettings
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler(ISiteSettingsStore siteSettingsStore)
    {
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      _ = request;
      SiteSettings settings = await GetOrCreateDefaultsAsync(SiteSettingsStore, cancellationToken)
        .ConfigureAwait(false);
      return ToResponse(settings);
    }
  }

  internal static async Task<SiteSettings> GetOrCreateDefaultsAsync(
    ISiteSettingsStore store,
    CancellationToken cancellationToken)
  {
    SiteSettings? settings = await store.GetAsync(cancellationToken).ConfigureAwait(false);
    if (settings is not null)
    {
      return settings;
    }

    var created = SiteSettings.Create();
    try
    {
      await store.AddAsync(created, cancellationToken).ConfigureAwait(false);
      return created;
    }
    catch (InvalidOperationException)
    {
      SiteSettings? winner = await store.GetAsync(cancellationToken).ConfigureAwait(false);
      if (winner is null)
      {
        throw new InvalidOperationException("Site settings create raced but re-get returned null.");
      }

      return winner;
    }
  }

  internal static Response ToResponse(SiteSettings settings) =>
    new(
      entraSignInEnabled: settings.EntraSignInEnabled,
      entraAllowBootstrap: settings.EntraAllowBootstrap,
      entraTrustedTenants: [.. settings.EntraTrustedTenants.Select(static tenantId => tenantId.ToString("D"))],
      passkeyPromptMode: settings.PasskeyPromptMode,
      version: settings.Version);
}
