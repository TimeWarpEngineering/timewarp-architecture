#region Purpose
// Authenticated read of the site-settings singleton plus the bound Authentication:Entra snapshot.
#endregion

#region Design
// SettingsRead (every human role has it as self-service). Does not expose this payload
// anonymously — the login page uses GetSignInMethods (the method list). Version is the
// optimistic-concurrency token the editor must round-trip on UpdateSiteSettings.
// Slice: Features.Settings (own product slice). Store lives in TimeWarp.Identity so Identity
// policy can read it without TWA0009. SPA SettingsPage (Link Microsoft 365 / passkey prompt)
// and Admin/Authentication (policy editor + app-registration tenant line) both call this
// contract (other assembly, free). Configuration* fields are the bound Entra section, not
// persisted policy — they are not on ISiteSettingsDetails and are not sent on Update.
// Response also implements IAssistantNameDetails and IXaiByokModeDetails. Those commands'
// generated From copies the one field from this whole response, and CopyBeyondDetails copies
// Version. The name and the mode stay off ISiteSettingsDetails so an authentication save
// cannot round-trip them.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetSiteSettings
{
  [ApiRoute("api/settings", HttpVerb.Get)]
  [Operation("site-settings.get", Description = "Read the site settings and the bound Entra configuration.", Group = "settings")]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed partial class Response : ISiteSettingsDetails, IAssistantNameDetails, IXaiByokModeDetails
  {
    public bool EntraSignInEnabled { get; set; }
    public bool EntraAllowBootstrap { get; set; }
    public PasskeyPromptMode PasskeyPromptMode { get; set; }
    public string AssistantName { get; set; }
    public EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; set; }
    public List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; } = [];
    public XaiByokMode XaiByokMode { get; set; }
    /// <summary>Concurrency token; send back the value last read.</summary>
    public long Version { get; }
    /// <summary>Configured Entra tenant id, when set.</summary>
    public string? ConfigurationTenantId { get; }
    /// <summary>Display name of the configured Entra tenant, when known.</summary>
    public string? ConfigurationTenantDisplayName { get; }
    /// <summary>Primary domain of the configured Entra tenant, when known.</summary>
    public string? ConfigurationTenantDomain { get; }
    /// <summary>Whether configuration enables Entra sign-in.</summary>
    public bool ConfigurationEnabled { get; }
    /// <summary>Whether configuration allows Entra bootstrap sign-in.</summary>
    public bool ConfigurationAllowBootstrap { get; }

    public Response(
      bool entraSignInEnabled,
      bool entraAllowBootstrap,
      PasskeyPromptMode passkeyPromptMode,
      string assistantName,
      long version,
      string? configurationTenantId = null,
      string? configurationTenantDisplayName = null,
      string? configurationTenantDomain = null,
      bool configurationEnabled = false,
      bool configurationAllowBootstrap = false,
      EntraTenantAcceptanceMode entraTenantAcceptanceMode = EntraTenantAcceptanceMode.Restricted,
      List<EntraAcceptedTenant>? entraAcceptedTenants = null,
      XaiByokMode xaiByokMode = XaiByokMode.Allow)
    {
      EntraSignInEnabled = entraSignInEnabled;
      EntraAllowBootstrap = entraAllowBootstrap;
      PasskeyPromptMode = passkeyPromptMode;
      AssistantName = assistantName;
      EntraTenantAcceptanceMode = entraTenantAcceptanceMode;
      EntraAcceptedTenants = entraAcceptedTenants is null ? [] : [.. entraAcceptedTenants];
      XaiByokMode = xaiByokMode;
      Version = version;
      ConfigurationTenantId = configurationTenantId;
      ConfigurationTenantDisplayName = configurationTenantDisplayName;
      ConfigurationTenantDomain = configurationTenantDomain;
      ConfigurationEnabled = configurationEnabled;
      ConfigurationAllowBootstrap = configurationAllowBootstrap;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      entraSignInEnabled: false,
      entraAllowBootstrap: false,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: AssistantNameRules.Default,
      version: 0);
  }
}
