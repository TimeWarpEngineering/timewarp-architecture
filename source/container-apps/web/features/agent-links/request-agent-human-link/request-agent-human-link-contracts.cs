#region Purpose
// Endpoint-centric contract for an agent to request an optional link to a human principal.
#endregion

#region Design
// Agent-token only (AuthenticationSchemeNames.AgentToken). Policy AgentLinkManageSelf expands
// from identity:read — requesting a human link is the agent's identity graph, not credential
// material. Not required for paid service. Handler rejects non-Agent callers and missing humans.
// HumanPrincipalId is the only target: there is no self-link or default human, so an omitted id
// binds to Guid.Empty and the Validator returns 400. [Required] only marks the property required
// in the OpenAPI document; the Validator is the runtime rule. The required keyword would break
// the new() constraint that HttpMethod<TRequest> in AgentLinkOnboardingPrompt relies on.
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
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>
    /// Account id of the human to link this agent to. The Agent links page shows it to the
    /// signed-in human. There is no default: the server never links the calling agent to itself
    /// or guesses a human, so an omitted or empty id is a 400 validation problem.
    /// </summary>
    /// <example>3f2504e0-4f89-41d3-9a0c-0305e82c3301</example>
    [System.ComponentModel.DataAnnotations.Required]
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
