#region Purpose
// Endpoint-centric contract for an agent to request an optional link to a human principal.
#endregion

#region Design
// Agent-token only (AuthenticationSchemeNames.AgentToken). Policy AgentLinkManageSelf expands
// from identity:read — requesting a human link is the agent's identity graph, not credential
// material. Not required for paid service. Handler rejects non-Agent callers and missing humans.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AgentLinkManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.AgentToken
)]
public static partial class RequestAgentHumanLink
{
  [ApiRoute("api/agent-links", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid HumanPrincipalId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.HumanPrincipalId).NotEmpty();
    }
  }

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
}
