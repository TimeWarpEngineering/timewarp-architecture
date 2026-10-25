#region Purpose
// SPA cache of the site-settings singleton for Admin Authentication, Link Microsoft 365, and Required passkey gate.
#endregion

#region Design
// TimeWarp.State: HTTP goes through ActionSets. Null snapshot until first fetch. Version is the
// concurrency token round-tripped on Update. PasskeyPromptMode drives AddPasskeyPrompt Later vs
// block. Slice Features.Settings; SettingsPage (Applications), AuthenticationPage (Admin.SiteSettings),
// and AuthenticationStateListener (Identity) take CrossSliceReference. Configuration* is the
// bound Entra section from GetSiteSettings, used for the Admin app-registration tenant line.
// Task 306: EntraTenantAcceptanceMode and EntraAcceptedTenantIds are the admin trust policy.
// Microsoft365 list / AllowBootstrap drift is still on this snapshot for the seeder/admin copy.
// AssistantName (task 361) is the admin form's copy; the labels read
// AgentSurfaceState.AssistantName from the public query.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[StateAccess]
public sealed partial class SiteSettingsState : State<SiteSettingsState>
{
  public bool? EntraSignInEnabled { get; private set; }
  public bool? EntraAllowBootstrap { get; private set; }
  public PasskeyPromptMode PasskeyPromptMode { get; private set; } = PasskeyPromptMode.Soft;
  public string AssistantName { get; private set; } = AssistantNameRules.Default;
  public EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; private set; }
  public IReadOnlyList<Guid> EntraAcceptedTenantIds { get; private set; } = [];
  public long Version { get; private set; }
  /// <summary>True when the last UpdateSiteSettings failed; the problem itself is on NotificationState.</summary>
  public bool SaveFailed { get; private set; }
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
    AssistantName = AssistantNameRules.Default;
    EntraTenantAcceptanceMode = EntraTenantAcceptanceMode.Restricted;
    EntraAcceptedTenantIds = [];
    Version = 0;
    SaveFailed = false;
    ConfigurationTenantId = null;
    ConfigurationTenantDisplayName = null;
    ConfigurationTenantDomain = null;
    ConfigurationEnabled = false;
    ConfigurationAllowBootstrap = false;
  }
}
