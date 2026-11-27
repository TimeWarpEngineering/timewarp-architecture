#region Purpose
// Endpoint-centric contract for a human to approve a pending agent link.
#endregion

#region Design
// Human session only. Handler requires the caller to be the link's HumanPrincipalId.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AgentLinkManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ApproveAgentHumanLink
{
  [ApiRoute("api/agent-links/{LinkId:guid}/approve", HttpVerb.Post)]
  [Operation("agent-human-links.approve", Description = "Approve a pending agent-to-human link.")]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    /// <summary>Id of the agent-human link.</summary>
    public Guid LinkId { get; }
    /// <summary>Link status.</summary>
    public string Status { get; }

    public Response(Guid linkId, string status)
    {
      LinkId = Guard.Against.NullOrEmpty(linkId);
      Status = Guard.Against.NullOrEmpty(status);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Approved");
  }
}
