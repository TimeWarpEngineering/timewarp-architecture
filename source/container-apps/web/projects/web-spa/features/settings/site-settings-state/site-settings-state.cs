#region Purpose
// SPA cache of the site-settings singleton for Admin Authentication, Link Microsoft 365, and Required passkey gate.
#endregion

#region Design
// TimeWarp.State: HTTP goes through ActionSets. Null snapshot until first fetch. Version is the
// concurrency token round-tripped on Update. PasskeyPromptMode drives AddPasskeyPrompt Later vs
// block. Slice Features.Settings; SettingsPage (Applications), AuthenticationPage (Admin.SiteSettings),
// and AuthenticationStateListener (Identity) take CrossSliceReference. Configuration* is the
// bound Entra section from GetSiteSettings, used for the Admin app-registration tenant line.
// Task 227 dropped persisted trusted tenants; Enabled/AllowBootstrap drift is still on this
// snapshot for the seeder/admin copy, not a tenant allowlist.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[StateAccess]
public sealed partial class SiteSettingsState : State<SiteSettingsState>
{
  public bool? EntraSignInEnabled { get; private set; }
  public bool? EntraAllowBootstrap { get; private set; }
  public PasskeyPromptMode PasskeyPromptMode { get; private set; } = PasskeyPromptMode.Soft;
  public long Version { get; private set; }
  public string? SaveError { get; private set; }
  public string? ConfigurationTenantId { get; private set; }
  public string? ConfigurationTenantDisplayName { get; private set; }
  public string? ConfigurationTenantDomain { get; private set; }
  public bool ConfigurationEnabled { get; private set; }
  public bool ConfigurationAllowBootstrap { get; private set; }

  public bool HasSnapshot => EntraSignInEnabled is not null;

  public bool ConfigurationEnabledDiffers =>
    HasSnapshot && ConfigurationEnabled != EntraSignInEnabled;

  public bool ConfigurationAllowBootstrapDiffers =>
    HasSnapshot && ConfigurationAllowBootstrap != EntraAllowBootstrap;

  public string ConfigurationTenantLabel =>
    SiteSettingsConfigurationDrift.FormatTenantLabel(
      ConfigurationTenantDisplayName,
      ConfigurationTenantDomain,
      ConfigurationTenantId);

  public override void Initialize()
  {
    EntraSignInEnabled = null;
    EntraAllowBootstrap = null;
    PasskeyPromptMode = PasskeyPromptMode.Soft;
    Version = 0;
    SaveError = null;
    ConfigurationTenantId = null;
    ConfigurationTenantDisplayName = null;
    ConfigurationTenantDomain = null;
    ConfigurationEnabled = false;
    ConfigurationAllowBootstrap = false;
  }
}
