#region Purpose
// Authenticated read of the site-settings singleton plus the bound Authentication:Entra snapshot.
#endregion

#region Design
// SettingsRead (every human role has it as self-service). Does not expose this payload
// anonymously — the login page uses GetEntraSignInOffered (boolean only). Version is the
// optimistic-concurrency token the editor must round-trip on UpdateSiteSettings.
// Slice: Features.Settings (own product slice). Store lives in TimeWarp.Identity so Identity
// policy can read it without TWA0009. SPA SettingsPage (Link Microsoft 365 / passkey prompt)
// and Admin/Authentication (policy editor + app-registration tenant line) both call this
// contract (other assembly, free). Configuration* fields are the bound Entra section, not
// persisted policy — they are not on ISiteSettingsDetails and are not sent on Update.
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
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response : ISiteSettingsDetails
  {
    public bool EntraSignInEnabled { get; set; }
    public bool EntraAllowBootstrap { get; set; }
    public PasskeyPromptMode PasskeyPromptMode { get; set; }
    public long Version { get; }
    public string? ConfigurationTenantId { get; }
    public string? ConfigurationTenantDisplayName { get; }
    public string? ConfigurationTenantDomain { get; }
    public bool ConfigurationEnabled { get; }
    public bool ConfigurationAllowBootstrap { get; }

    public Response(
      bool entraSignInEnabled,
      bool entraAllowBootstrap,
      PasskeyPromptMode passkeyPromptMode,
      long version,
      string? configurationTenantId = null,
      string? configurationTenantDisplayName = null,
      string? configurationTenantDomain = null,
      bool configurationEnabled = false,
      bool configurationAllowBootstrap = false)
    {
      EntraSignInEnabled = entraSignInEnabled;
      EntraAllowBootstrap = entraAllowBootstrap;
      PasskeyPromptMode = passkeyPromptMode;
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
      version: 0);
  }
}
