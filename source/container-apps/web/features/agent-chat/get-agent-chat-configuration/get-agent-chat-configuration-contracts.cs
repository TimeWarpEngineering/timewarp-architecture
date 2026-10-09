#region Purpose
// Reports whether the server can call xAI, plus the command that sets the key. Never returns the key.
#endregion

#region Design
// The WASM client cannot see user-secrets. This GET is the only signal Ask uses. Configured is
// true when a key is present, or when the Development/Testing fake upstream is explicitly on.
// SetupCommand is always the same pwsh line so a failed probe can fall back to the constant.
// Model is the configured id when Configured is true, and null otherwise.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = XaiChatDefaults.AuthenticatedPolicy,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetAgentChatConfiguration
{
  [ApiRoute("api/agent-chat/configuration", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Response : BaseResponse
  {
    public bool Configured { get; set; }

    public string SetupCommand { get; set; } = null!;

    public string? Model { get; set; }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response
    {
      Configured = false,
      SetupCommand = XaiChatDefaults.SetupCommand,
      Model = null
    };
}
