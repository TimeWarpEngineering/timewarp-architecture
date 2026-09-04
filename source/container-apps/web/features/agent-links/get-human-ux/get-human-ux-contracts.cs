#region Purpose
// Endpoint-centric contract for an agent to fetch the portable humanUx handoff document.
#endregion

#region Design
// Agent-token only. Response IS the humanUx document (spec timewarp.humanUx/v1) — see
// human-ux-contracts.cs Design region and human-ux.sample.json. Handler requires an Approved
// link owned by the calling agent. Not required for paid service.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AgentLinkManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.AgentToken
)]
public static partial class GetHumanUx
{
  [ApiRoute("api/agent-links/{LinkId:guid}/human-ux", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public const string SpecId = "timewarp.humanUx/v1";
    public const string KindId = "handoff";

    public string Spec { get; }
    public string Kind { get; }
    public string Title { get; }
    public string Summary { get; }
    public HumanUxLink Link { get; }
    public HumanUxHuman? Human { get; }
    public IReadOnlyList<HumanUxAction> Actions { get; }

    public Response(
      string title,
      string summary,
      HumanUxLink link,
      HumanUxHuman? human,
      IReadOnlyList<HumanUxAction> actions)
    {
      Spec = SpecId;
      Kind = KindId;
      Title = Guard.Against.NullOrEmpty(title);
      Summary = Guard.Against.NullOrEmpty(summary);
      Link = Guard.Against.Null(link);
      Human = human;
      Actions = Guard.Against.Null(actions);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      title: "Linked human",
      summary: "Present this to your operator. Paid service does not require a linked human — this payload is optional chrome for agents that have one.",
      link: new HumanUxLink(
        Guid.Parse("11111111-2222-3333-4444-555555555555"),
        "Approved",
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff")),
      human: new HumanUxHuman("Ada", "ada@example.com"),
      actions:
      [
        new HumanUxAction("open-profile", "Open profile", "/Profile"),
        new HumanUxAction("open-links", "Manage agent links", "/AgentLinks")
      ]);
  }
}
