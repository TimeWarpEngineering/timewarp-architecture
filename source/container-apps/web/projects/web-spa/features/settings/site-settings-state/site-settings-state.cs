#region Purpose
// SPA cache of the site-settings singleton for Admin Authentication, Link Microsoft 365, and Required passkey gate.
#endregion

#region Design
// TimeWarp.State: HTTP goes through ActionSets. Null snapshot until first fetch. Version is the
// concurrency token round-tripped on Update. PasskeyPromptMode drives AddPasskeyPrompt Later vs
// block. Slice Features.Settings; SettingsPage (Applications), AuthenticationPage (Admin.SiteSettings),
// and AuthenticationStateListener (Identity) take CrossSliceReference. Configuration* is the
// bound Entra section from GetSiteSettings, used for the Admin app-registration tenant line.
// Task 306: EntraTenantAcceptanceMode and EntraAcceptedTenants are the admin trust policy.
// Task 401: each entry is a tenant id plus an optional domain. DraftAcceptedTenants is the
// unsaved list the Authentication page edits. AcceptedTenantsMessage is the lookup or
// edit failure for that list. The saved snapshot stays on EntraAcceptedTenants.
// Microsoft365 list / AllowBootstrap drift is still on this snapshot for the seeder/admin copy.
// AssistantName is the Admin AI form's copy of the stored name. Labels read
// AgentSurfaceState.AssistantName from the public query. UpdateAssistantName writes the name;
// AssistantNameSaveFailed keeps that draft when the rename fails. UpdateXaiByokMode writes
// whose key Ask uses; XaiByokModeSaveFailed keeps that draft when the write fails. SaveFailed
// belongs to the authentication policy save.
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
  public XaiByokMode XaiByokMode { get; private set; } = XaiByokMode.Allow;
  public IReadOnlyList<EntraAcceptedTenant> EntraAcceptedTenants { get; private set; } = [];
  public IReadOnlyList<EntraAcceptedTenant> DraftAcceptedTenants { get; private set; } = [];
  public string? AcceptedTenantsMessage { get; private set; }
  public long Version { get; private set; }
  /// <summary>True when the last UpdateSiteSettings failed; the problem itself is on NotificationState.</summary>
  public bool SaveFailed { get; private set; }
  /// <summary>True when the last UpdateAssistantName failed; the problem itself is on NotificationState.</summary>
  public bool AssistantNameSaveFailed { get; private set; }
  /// <summary>True when the last UpdateXaiByokMode failed; the problem itself is on NotificationState.</summary>
  public bool XaiByokModeSaveFailed { get; private set; }
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
    XaiByokMode = XaiByokMode.Allow;
    EntraAcceptedTenants = [];
    DraftAcceptedTenants = [];
    AcceptedTenantsMessage = null;
    Version = 0;
    SaveFailed = false;
    AssistantNameSaveFailed = false;
    XaiByokModeSaveFailed = false;
    ConfigurationTenantId = null;
    ConfigurationTenantDisplayName = null;
    ConfigurationTenantDomain = null;
    ConfigurationEnabled = false;
    ConfigurationAllowBootstrap = false;
  }
}
