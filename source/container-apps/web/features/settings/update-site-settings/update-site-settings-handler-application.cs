#region Purpose
// Server-side handler for UpdateSiteSettings: optimistic concurrency then persist policy fields.
#endregion

#region Design
// Get-or-create factory defaults (same helper as GetSiteSettings), compare Command.Version to
// stored Version, 409 on mismatch. On match, ReplacePolicy then UpdateAsync. Concurrent Update
// throws ConcurrencyConflictException → same 409. Does not reference Identity.Application (TWA0009).
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Settings.UpdateSiteSettings;

public sealed class UpdateSiteSettings
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler(ISiteSettingsStore siteSettingsStore)
    {
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      SiteSettings settings = await GetSiteSettings.GetOrCreateDefaultsAsync(
        SiteSettingsStore,
        cancellationToken).ConfigureAwait(false);
      if (command.Version != settings.Version)
      {
        return SiteSettingsProblems.ConcurrencyConflict();
      }

      settings.ReplacePolicy(
        command.EntraSignInEnabled,
        command.EntraAllowBootstrap,
        ParseTenants(command.EntraTrustedTenants),
        command.PasskeyPromptMode);

      try
      {
        await SiteSettingsStore.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
      }
      catch (ConcurrencyConflictException)
      {
        return SiteSettingsProblems.ConcurrencyConflict();
      }

      SiteSettings? stored = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
      return ToResponse(stored ?? settings);
    }
  }

  internal static Response ToResponse(SiteSettings settings) =>
    new(
      entraSignInEnabled: settings.EntraSignInEnabled,
      entraAllowBootstrap: settings.EntraAllowBootstrap,
      entraTrustedTenants: [.. settings.EntraTrustedTenants.Select(static tenantId => tenantId.ToString("D"))],
      passkeyPromptMode: settings.PasskeyPromptMode,
      version: settings.Version);

  private static List<Guid> ParseTenants(IReadOnlyList<string> tenants)
  {
    List<Guid> parsed = [];
    foreach (string entry in tenants)
    {
      parsed.Add(Guid.Parse(entry));
    }

    return parsed;
  }
}
