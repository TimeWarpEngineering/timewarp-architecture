#region Purpose
// Authenticated read of the site-settings singleton for the Settings Authentication section.
#endregion

#region Design
// SettingsRead (every human role has it as self-service). Does not expose this payload
// anonymously — the login page uses GetEntraSignInOffered (boolean only). Version is the
// optimistic-concurrency token the editor must round-trip on UpdateSiteSettings.
// Slice: Features.Settings (own product slice). Store lives in TimeWarp.Identity so Identity
// policy can read it without TWA0009. SPA SettingsPage is Applications chrome and calls this
// contract (other assembly, free).
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
    public List<string> EntraTrustedTenants { get; set; }
    public PasskeyPromptMode PasskeyPromptMode { get; set; }
    public long Version { get; }

    public Response(
      bool entraSignInEnabled,
      bool entraAllowBootstrap,
      List<string> entraTrustedTenants,
      PasskeyPromptMode passkeyPromptMode,
      long version)
    {
      EntraSignInEnabled = entraSignInEnabled;
      EntraAllowBootstrap = entraAllowBootstrap;
      EntraTrustedTenants = entraTrustedTenants ?? [];
      PasskeyPromptMode = passkeyPromptMode;
      Version = version;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      entraSignInEnabled: false,
      entraAllowBootstrap: false,
      entraTrustedTenants: [],
      passkeyPromptMode: PasskeyPromptMode.Soft,
      version: 0);
  }
}
