#region Purpose
// Endpoint-centric contract for listing the caller's agent-human links.
#endregion

#region Design
// Dual-scheme: a human session lists links they must approve; an agent token lists links it
// requested. Handler scopes by ICurrentPrincipalAccessor — never a client-supplied id.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AgentLinkManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class ListAgentHumanLinks
{
  [ApiRoute("api/agent-links", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class LinkSummary
  {
    public Guid LinkId { get; }
    public Guid AgentPrincipalId { get; }
    public Guid HumanPrincipalId { get; }
    public string Status { get; }

    public LinkSummary(Guid linkId, Guid agentPrincipalId, Guid humanPrincipalId, string status)
    {
      LinkId = Guard.Against.NullOrEmpty(linkId);
      AgentPrincipalId = Guard.Against.NullOrEmpty(agentPrincipalId);
      HumanPrincipalId = Guard.Against.NullOrEmpty(humanPrincipalId);
      Status = Guard.Against.NullOrEmpty(status);
    }
  }

  public sealed class Response
  {
    public IReadOnlyList<LinkSummary> Items { get; }

    public Response(IReadOnlyList<LinkSummary> items)
    {
      Items = Guard.Against.Null(items);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
    [
      new LinkSummary(
        Guid.Parse("11111111-2222-3333-4444-555555555555"),
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
        "Pending")
    ]);
  }
}
