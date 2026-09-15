#region Purpose
// Authenticated write of the site-settings singleton with optimistic concurrency.
#endregion

#region Design
// SettingsWrite — granted to the bootstrap Administrator role, not self-service. Version is the
// concurrency token from GetSiteSettings; mismatch is 409. Validator composes
// SiteSettingsDetailsValidator (GUID tenants). PUT of the same ISiteSettingsDetails shape the
// Get response implements so the Settings form binds once.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UpdateSiteSettings
{
  [ApiRoute("api/settings", HttpVerb.Put)]
  public sealed partial class Command : IApiRequest, ISiteSettingsDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public bool EntraSignInEnabled { get; set; }
    public bool EntraAllowBootstrap { get; set; }
    public List<string> EntraTrustedTenants { get; set; } = null!;
    public PasskeyPromptMode PasskeyPromptMode { get; set; }
    public long Version { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command).SetValidator(new SiteSettingsDetailsValidator());
      RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
  }

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
      version: 1);
  }
}
