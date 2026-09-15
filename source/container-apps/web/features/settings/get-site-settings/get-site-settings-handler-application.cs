#region Purpose
// Server-side handler for GetSiteSettings: return the singleton snapshot, or 503 if not seeded.
#endregion

#region Design
// Application takes ISiteSettingsStore, not PostgresDbContext and not Identity.Application types
// (TWA0009). Does not insert when empty — only SiteSettingsSeeder writes the first row (from
// Authentication:Entra at boot via SiteSettingsSeedHostedService.StartingAsync). Empty store
// returns NotInitialized (503). Tenant ids serialize as D-format GUID strings.
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
      SiteSettings? settings = await SiteSettingsStore.GetAsync(cancellationToken)
        .ConfigureAwait(false);
      if (settings is null)
      {
        return SiteSettingsProblems.NotInitialized();
      }

      return ToResponse(settings);
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
