#region Purpose
// First-run seed of site settings from Authentication:Entra, then drift warnings or Development reseed.
#endregion

#region Design
// When the store is empty, copy Enabled / AllowBootstrap / TrustedTenants once so existing
// `dev entra setup` secrets keep working. After that those three are not consulted for policy —
// Configuration Enabled remains the scheme-registration gate only. PasskeyPromptMode is not in
// configuration; seed uses Soft. Concurrent first-boot Add races re-Get.
// Task 225: each later boot compares persisted policy with configuration. LoggerMessage.Define
// Warning when TenantId (GUID) is missing from EntraTrustedTenants, when AllowBootstrap differs,
// or when Enabled differs — each line names /Admin/Authentication and `dev entra reseed`.
// ReseedSiteSettings overwrites those three fields from configuration only when isDevelopment
// is true (hosted service passes IHostEnvironment.IsDevelopment()). PasskeyPromptMode is kept.
// Non-GUID TenantId (appsettings "organizations") is not tenant drift.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Identity;

public sealed class SiteSettingsSeeder
{
  private static readonly Action<ILogger, bool, bool, Exception?> LogConfiguredVsEnabled =
    LoggerMessage.Define<bool, bool>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogConfiguredVsEnabled)),
      "Authentication:Entra:Enabled is {ConfiguredEnabled} but site settings EntraSignInEnabled is {SettingsEnabled}. Configuration registers the named entra scheme; settings decide whether users may sign in. Edit on /Admin/Authentication or run `dev entra reseed`."
    );

  private static readonly Action<ILogger, string, Exception?> LogConfiguredTenantUntrusted =
    LoggerMessage.Define<string>
    (
      LogLevel.Warning,
      new EventId(2, nameof(LogConfiguredTenantUntrusted)),
      "Authentication:Entra:TenantId {ConfiguredTenantId} is not in the persisted EntraTrustedTenants list. Bootstrap with this tenant will be refused. Edit on /Admin/Authentication or run `dev entra reseed`."
    );

  private static readonly Action<ILogger, bool, bool, Exception?> LogConfiguredVsAllowBootstrap =
    LoggerMessage.Define<bool, bool>
    (
      LogLevel.Warning,
      new EventId(3, nameof(LogConfiguredVsAllowBootstrap)),
      "Authentication:Entra:AllowBootstrap is {ConfiguredAllowBootstrap} but site settings EntraAllowBootstrap is {SettingsAllowBootstrap}. Edit on /Admin/Authentication or run `dev entra reseed`."
    );

  private static readonly Action<ILogger, Exception?> LogReseedApplied =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(4, nameof(LogReseedApplied)),
      "Overwrote EntraSignInEnabled, EntraAllowBootstrap, and EntraTrustedTenants from Authentication:Entra because ReseedSiteSettings is true (Development only). Run `dev entra reseed --clear` so later boots keep admin edits."
    );

  private static readonly Action<ILogger, Exception?> LogReseedIgnored =
    LoggerMessage.Define
    (
      LogLevel.Warning,
      new EventId(5, nameof(LogReseedIgnored)),
      "Authentication:Entra:ReseedSiteSettings is true but isDevelopment is false; the flag is honoured only in Development."
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

  public Task<SiteSettings> GetOrSeedAsync(CancellationToken cancellationToken = default) =>
    GetOrSeedAsync(isDevelopment: false, cancellationToken);

  public async Task<SiteSettings> GetOrSeedAsync(bool isDevelopment, CancellationToken cancellationToken = default)
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
    if (configured.ReseedSiteSettings)
    {
      if (isDevelopment)
      {
        existing.ReplacePolicy(
          configured.Enabled,
          configured.AllowBootstrap,
          ParseTrustedTenants(configured.TrustedTenants),
          existing.PasskeyPromptMode);
        await SiteSettingsStore.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false) ?? existing;
        LogReseedApplied(Logger, null);
      }
      else
      {
        LogReseedIgnored(Logger, null);
      }
    }

    LogDrift(configured, existing);
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

  private void LogDrift(EntraAuthenticationOptions configured, SiteSettings existing)
  {
    if (existing.EntraSignInEnabled != configured.Enabled)
    {
      LogConfiguredVsEnabled(Logger, configured.Enabled, existing.EntraSignInEnabled, null);
    }

    if (existing.EntraAllowBootstrap != configured.AllowBootstrap)
    {
      LogConfiguredVsAllowBootstrap(Logger, configured.AllowBootstrap, existing.EntraAllowBootstrap, null);
    }

    if (SiteSettingsConfigurationDrift.TryParseTenantId(configured.TenantId, out Guid configuredTenant)
      && !existing.IsTrustedTenant(configuredTenant))
    {
      LogConfiguredTenantUntrusted(Logger, configuredTenant.ToString("D"), null);
    }
  }
}
