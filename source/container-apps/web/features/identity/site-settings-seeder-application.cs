#region Purpose
// First-run seed of site settings from Authentication:Entra configuration, then one mismatch log.
#endregion

#region Design
// When the store is empty, copy Enabled / AllowBootstrap / TrustedTenants once so existing
// `dev entra setup` secrets keep working. After that those three are not consulted for policy —
// Configuration Enabled remains the scheme-registration gate only. PasskeyPromptMode is not in
// configuration; seed uses Soft. Concurrent first-boot Add races re-Get. Log once when
// options.Enabled != settings.EntraSignInEnabled so the two are never silently confused.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Identity;

public sealed class SiteSettingsSeeder
{
  private static readonly Action<ILogger, bool, bool, Exception?> LogConfiguredVsEnabled =
    LoggerMessage.Define<bool, bool>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogConfiguredVsEnabled)),
      "Authentication:Entra:Enabled is {ConfiguredEnabled} but site settings EntraSignInEnabled is {SettingsEnabled}. Configuration registers the named entra scheme; settings decide whether users may sign in."
    );

  private readonly ISiteSettingsStore SiteSettingsStore;
  private readonly IOptions<EntraAuthenticationOptions> Options;
  private readonly ILogger<SiteSettingsSeeder> Logger;

  public SiteSettingsSeeder(
    ISiteSettingsStore siteSettingsStore,
    IOptions<EntraAuthenticationOptions> options,
    ILogger<SiteSettingsSeeder> logger)
  {
    SiteSettingsStore = siteSettingsStore;
    Options = options;
    Logger = logger;
  }

  public async Task<SiteSettings> GetOrSeedAsync(CancellationToken cancellationToken = default)
  {
    SiteSettings? existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
    if (existing is null)
    {
      EntraAuthenticationOptions options = Options.Value;
      var created = SiteSettings.Create(
        entraSignInEnabled: options.Enabled,
        entraAllowBootstrap: options.AllowBootstrap,
        entraTrustedTenants: ParseTrustedTenants(options.TrustedTenants),
        passkeyPromptMode: PasskeyPromptMode.Soft);

      try
      {
        await SiteSettingsStore.AddAsync(created, cancellationToken).ConfigureAwait(false);
        existing = created;
      }
      catch (InvalidOperationException)
      {
        existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
          throw new InvalidOperationException("Site settings seed raced but re-get returned null.");
        }
      }
    }

    EntraAuthenticationOptions configured = Options.Value;
    if (existing.EntraSignInEnabled != configured.Enabled)
    {
      LogConfiguredVsEnabled(Logger, configured.Enabled, existing.EntraSignInEnabled, null);
    }

    return existing;
  }

  internal static List<Guid> ParseTrustedTenants(IEnumerable<string> entries)
  {
    List<Guid> tenants = [];
    foreach (string entry in entries)
    {
      if (Guid.TryParse(entry, out Guid tenantId) && tenantId != Guid.Empty)
      {
        tenants.Add(tenantId);
      }
    }

    return tenants;
  }
}
