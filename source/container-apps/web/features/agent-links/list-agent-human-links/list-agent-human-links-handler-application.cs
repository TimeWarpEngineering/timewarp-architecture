#region Purpose
// Server-side handler: list agent-human links for the authenticated caller.
#endregion

#region Design
// IDOR rule: principal id comes only from ICurrentPrincipalAccessor. Both agent and human
// callers see rows where they are a party.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentLinks.ListAgentHumanLinks;

public sealed class ListAgentHumanLinks
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IAgentHumanLinkStore LinkStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IAgentHumanLinkStore linkStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      LinkStore = linkStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      _ = query;
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken).ConfigureAwait(false);
      if (callerId is null)
      {
        return AgentLinkProblems.Unauthenticated();
      }

      IReadOnlyList<AgentHumanLink> links =
        await LinkStore.ListByPrincipalAsync(callerId.Value, cancellationToken).ConfigureAwait(false);

      List<LinkSummary> items = [];
      foreach (AgentHumanLink link in links)
      {
        items.Add(new LinkSummary(
          link.Id.Value,
          link.AgentPrincipalId,
          link.HumanPrincipalId,
          link.Status.ToString()));
      }

      return new Response(items);
    }
  }
}
