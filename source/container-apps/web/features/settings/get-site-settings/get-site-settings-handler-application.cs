#region Purpose
// Server-side handler for GetSiteSettings: return the singleton snapshot, or 503 if not seeded.
#endregion

#region Design
// Application takes ISiteSettingsStore, not PostgresDbContext. Does not insert when empty —
// only SiteSettingsSeeder writes the first row (from Authentication:Entra at boot via
// SiteSettingsSeedHostedService.StartingAsync). Empty store returns NotInitialized (503).
// Configuration* fields come from bound EntraAuthenticationOptions (Identity slice) so
// Admin/Authentication can show the app-registration tenant without a second endpoint —
// CrossSliceReference on Handler.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Settings.GetSiteSettings;

public sealed class GetSiteSettings
{
  [CrossSliceReference(
    typeof(EntraAuthenticationOptions),
    "Projects bound Authentication:Entra tenant and enablement onto GetSiteSettings so Admin/Authentication can show the app-registration tenant without a second endpoint.")]
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ISiteSettingsStore SiteSettingsStore;
    private readonly IOptions<EntraAuthenticationOptions> Options;

    public Handler(
      ISiteSettingsStore siteSettingsStore,
      IOptions<EntraAuthenticationOptions> options)
    {
      SiteSettingsStore = siteSettingsStore;
      Options = options;
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

      return ToResponse(settings, Options.Value);
    }

    private static Response ToResponse(SiteSettings settings, EntraAuthenticationOptions configured)
    {
      string? tenantId = string.IsNullOrWhiteSpace(configured.TenantId) ? null : configured.TenantId.Trim();
      string? displayName = string.IsNullOrWhiteSpace(configured.TenantDisplayName)
        ? null
        : configured.TenantDisplayName.Trim();
      string? domain = string.IsNullOrWhiteSpace(configured.TenantDomain) ? null : configured.TenantDomain.Trim();
      return new(
        entraSignInEnabled: settings.EntraSignInEnabled,
        entraAllowBootstrap: settings.EntraAllowBootstrap,
        passkeyPromptMode: settings.PasskeyPromptMode,
        version: settings.Version,
        configurationTenantId: tenantId,
        configurationTenantDisplayName: displayName,
        configurationTenantDomain: domain,
        configurationEnabled: configured.Enabled,
        configurationAllowBootstrap: configured.AllowBootstrap);
    }
  }
}
