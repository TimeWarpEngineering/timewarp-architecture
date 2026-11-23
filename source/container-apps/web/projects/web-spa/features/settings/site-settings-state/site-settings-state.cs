#region Purpose
// SPA cache of the site-settings singleton for Admin Authentication, Link Microsoft 365, and the required passkey gate.
#endregion

#region Design
// The store holds the fetched GetSiteSettings.Response, not a flattened copy of its fields.
// Fetch assigns it. UpdateSiteSettings, UpdateAssistantName, and UpdateXaiByokMode replace it
// with a new response, because Version is get-only and a page that already holds the previous
// instance must see the change. Configuration* stay on that response: they are the bound Entra
// section, not fields the authentication command edits. AssistantName and XaiByokMode stay on
// it for the same reason. The unsaved authentication form, including accepted tenants, lives on
// the page's UpdateSiteSettings.Command. ResolveEntraTenant records ResolvedTenant or
// AcceptedTenantsMessage; the page applies that result to its command. TenantLookupCount
// advances once per completed lookup so the page applies each result once. SaveFailed,
// AssistantNameSaveFailed, and XaiByokModeSaveFailed are the page-facing flags for those writes.
// Labels read AgentSurfaceState.AssistantName from the public query, not this snapshot.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[StateAccess]
public sealed partial class SiteSettingsState : State<SiteSettingsState>
{
  public GetSiteSettings.Response? SiteSettings { get; private set; }

  /// <summary>True when the last UpdateSiteSettings failed; the problem itself is on NotificationState.</summary>
  public bool SaveFailed { get; private set; }

  /// <summary>True when the last UpdateAssistantName failed; the problem itself is on NotificationState.</summary>
  public bool AssistantNameSaveFailed { get; private set; }

  /// <summary>True when the last UpdateXaiByokMode failed; the problem itself is on NotificationState.</summary>
  public bool XaiByokModeSaveFailed { get; private set; }

  /// <summary>The last successful domain lookup. The page merges it into its command.</summary>
  public EntraAcceptedTenant? ResolvedTenant { get; private set; }

  /// <summary>Lookup failure text. Null after a tenant is resolved.</summary>
  public string? AcceptedTenantsMessage { get; private set; }

  /// <summary>Completed lookup attempts. The page applies a result when this changes.</summary>
  public int TenantLookupCount { get; private set; }

  public bool HasSnapshot => SiteSettings is not null;

  public bool ConfigurationEnabledDiffers =>
    SiteSettings is { } settings && settings.ConfigurationEnabled != settings.EntraSignInEnabled;

  public bool ConfigurationAllowBootstrapDiffers =>
    SiteSettings is { } settings && settings.ConfigurationAllowBootstrap != settings.EntraAllowBootstrap;

  public string ConfigurationTenantLabel =>
    SiteSettings is { } settings
      ? SiteSettingsConfigurationDrift.FormatTenantLabel(
        settings.ConfigurationTenantDisplayName,
        settings.ConfigurationTenantDomain,
        settings.ConfigurationTenantId)
      : SiteSettingsConfigurationDrift.FormatTenantLabel(null, null, null);

  public override void Initialize()
  {
    SiteSettings = null;
    SaveFailed = false;
    AssistantNameSaveFailed = false;
    XaiByokModeSaveFailed = false;
    ResolvedTenant = null;
    AcceptedTenantsMessage = null;
    TenantLookupCount = 0;
  }

  private void ReviseSiteSettings
  (
    string? assistantName = null,
    long? version = null,
    XaiByokMode? xaiByokMode = null,
    bool? entraSignInEnabled = null,
    bool? entraAllowBootstrap = null,
    PasskeyPromptMode? passkeyPromptMode = null,
    EntraTenantAcceptanceMode? entraTenantAcceptanceMode = null,
    IReadOnlyList<EntraAcceptedTenant>? entraAcceptedTenants = null
  )
  {
    if (SiteSettings is not { } current)
    {
      return;
    }

    List<EntraAcceptedTenant> tenants = entraAcceptedTenants is null
      ? current.EntraAcceptedTenants
      : [.. entraAcceptedTenants];
    SiteSettings = new GetSiteSettings.Response
    (
      entraSignInEnabled ?? current.EntraSignInEnabled,
      entraAllowBootstrap ?? current.EntraAllowBootstrap,
      passkeyPromptMode ?? current.PasskeyPromptMode,
      assistantName ?? current.AssistantName,
      version ?? current.Version,
      current.ConfigurationTenantId,
      current.ConfigurationTenantDisplayName,
      current.ConfigurationTenantDomain,
      current.ConfigurationEnabled,
      current.ConfigurationAllowBootstrap,
      entraTenantAcceptanceMode ?? current.EntraTenantAcceptanceMode,
      tenants,
      xaiByokMode ?? current.XaiByokMode
    );
  }
}
