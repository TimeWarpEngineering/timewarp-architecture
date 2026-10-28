#region Purpose
// Authenticated write of whose xAI key Ask uses, with the same optimistic concurrency as site settings.
#endregion

#region Design
// XaiByokMode stays a SiteSettings column, but it is an AI concern. A dedicated command keeps the
// AI page from round-tripping Entra policy, the passkey prompt, and the accepted-tenant list, and
// keeps an authentication save from overwriting the mode. SettingsWrite, the same policy as
// UpdateSiteSettings. Version is the concurrency token from GetSiteSettings; mismatch is 409.
// PUT api/settings/xai-byok sits beside the assistant rename. ICommand, not IIdempotentCommand:
// a successful write advances Version, so a retry of the same body conflicts. [Operation]
// publishes the mode to CLI and MCP. The host XAI:ApiKey still wins at resolve time; this
// command only stores the policy.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UpdateXaiByokMode
{
  [ApiRoute("api/settings/xai-byok", HttpVerb.Put)]
  [Operation
  (
    "xai-byok-mode.update",
    Description = "Choose whose xAI API key Ask uses when the host has not set one.",
    Group = "admin.ai",
    Title = "Update xAI key policy"
  )]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public XaiByokMode XaiByokMode { get; set; }
    public long Version { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.XaiByokMode).IsInEnum();
      RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
  }

  public sealed class Response
  {
    public XaiByokMode XaiByokMode { get; }
    public long Version { get; }

    public Response(XaiByokMode xaiByokMode, long version)
    {
      XaiByokMode = xaiByokMode;
      Version = version;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response(XaiByokMode.Allow, 1);
}
