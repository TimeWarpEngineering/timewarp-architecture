#region Purpose
// Endpoint-centric contract for a human to deny a pending agent link.
#endregion

#region Design
// Same auth posture as ApproveAgentHumanLink. Denied pairs may be requested again.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AgentLinkManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class DenyAgentHumanLink
{
  [ApiRoute("api/agent-links/{LinkId:guid}/deny", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    public Guid LinkId { get; }
    public string Status { get; }

    public Response(Guid linkId, string status)
    {
      LinkId = Guard.Against.NullOrEmpty(linkId);
      Status = Guard.Against.NullOrEmpty(status);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Denied");
  }
}
