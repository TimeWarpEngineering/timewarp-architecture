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
  [Operation("agent-human-links.list", Description = "List the caller's agent-to-human links.")]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class LinkSummary : ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

    /// <summary>Id of the agent-human link.</summary>
    public Guid LinkId { get; }
    /// <summary>Principal id of the agent.</summary>
    public Guid AgentPrincipalId { get; }
    /// <summary>Principal id of the human.</summary>
    public Guid HumanPrincipalId { get; }
    /// <summary>Link status.</summary>
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
    /// <summary>The links on this page.</summary>
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
