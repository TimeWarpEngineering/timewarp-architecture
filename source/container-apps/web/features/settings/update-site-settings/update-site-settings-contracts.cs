#region Purpose
// Authenticated write of the site-settings singleton with optimistic concurrency.
#endregion

#region Design
// SettingsWrite — granted to the bootstrap Administrator role, not self-service. Version is the
// concurrency token from GetSiteSettings; mismatch is 409. Validator composes
// SiteSettingsDetailsValidator. PUT of the same ISiteSettingsDetails shape the Get response
// implements so the Authentication form binds once. Task 306: EntraTenantAcceptanceMode and
// EntraAcceptedTenants are the trust policy on this command (id plus optional domain).
// The assistant name is UpdateAssistantName. The xAI key mode is UpdateXaiByokMode. This command
// accepts neither. The response still echoes the stored name and mode so a policy save refreshes
// the SPA copy.
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
  public sealed partial class Command : IApiRequest, ISiteSettingsDetails, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public bool EntraSignInEnabled { get; set; }
    public bool EntraAllowBootstrap { get; set; }
    public PasskeyPromptMode PasskeyPromptMode { get; set; }
    public EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; set; }
    public List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; } = [];
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
    public PasskeyPromptMode PasskeyPromptMode { get; set; }
    public string AssistantName { get; set; }
    public EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; set; }
    public List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; } = [];
    public XaiByokMode XaiByokMode { get; set; }
    public long Version { get; }

    public Response(
      bool entraSignInEnabled,
      bool entraAllowBootstrap,
      PasskeyPromptMode passkeyPromptMode,
      string assistantName,
      long version,
      EntraTenantAcceptanceMode entraTenantAcceptanceMode = EntraTenantAcceptanceMode.Restricted,
      List<EntraAcceptedTenant>? entraAcceptedTenants = null,
      XaiByokMode xaiByokMode = XaiByokMode.Allow)
    {
      EntraSignInEnabled = entraSignInEnabled;
      EntraAllowBootstrap = entraAllowBootstrap;
      PasskeyPromptMode = passkeyPromptMode;
      AssistantName = assistantName;
      Version = version;
      EntraTenantAcceptanceMode = entraTenantAcceptanceMode;
      EntraAcceptedTenants = entraAcceptedTenants is null ? [] : [.. entraAcceptedTenants];
      XaiByokMode = xaiByokMode;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      entraSignInEnabled: false,
      entraAllowBootstrap: false,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: AssistantNameRules.Default,
      version: 1);
  }
}
