#region Purpose
// Server-side handler: build the portable humanUx document for an approved agent-owned link.
#endregion

#region Design
// Human chrome comes from TimeWarp.Identity.Principal.DisplayName (other assembly — free under
// TWA0009), not Features.Profiles — email/prefs stay on the profile slice. Missing display name
// yields Human = null. Paid handlers never call IAgentHumanLinkStore.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentLinks.GetHumanUx;

public sealed class GetHumanUx
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IPrincipalStore PrincipalStore;
    private readonly IAgentHumanLinkStore LinkStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IPrincipalStore principalStore,
      IAgentHumanLinkStore linkStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      PrincipalStore = principalStore;
      LinkStore = linkStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken).ConfigureAwait(false);
      if (callerId is null)
      {
        return AgentLinkProblems.Unauthenticated();
      }

      var linkId = AgentHumanLinkId.From(query.LinkId);
      AgentHumanLink? link = await LinkStore.FindAsync(linkId, cancellationToken).ConfigureAwait(false);
      if (link is null)
      {
        return AgentLinkProblems.NotFound();
      }

      if (link.AgentPrincipalId != callerId.Value.Value)
      {
        return AgentLinkProblems.Forbidden("This link belongs to a different agent.");
      }

      if (link.Status != AgentHumanLinkStatus.Approved)
      {
        return AgentLinkProblems.NotApproved();
      }

      HumanUxHuman? human = null;
      Principal? humanPrincipal = await PrincipalStore.GetPrincipalAsync(
        PrincipalId.From(link.HumanPrincipalId),
        cancellationToken).ConfigureAwait(false);
      if (humanPrincipal?.DisplayName is string displayName && displayName.Length > 0)
      {
        human = new HumanUxHuman(displayName, email: null);
      }

      return new Response(
        title: "Linked human",
        summary: "Present this to your operator. Paid service does not require a linked human — this payload is optional chrome for agents that have one.",
        link: new HumanUxLink(
          link.Id.Value,
          link.Status.ToString(),
          link.AgentPrincipalId,
          link.HumanPrincipalId),
        human: human,
        actions:
        [
          new HumanUxAction("open-profile", "Open profile", "/Profile"),
          new HumanUxAction("open-links", "Manage agent links", "/AgentLinks")
        ]);
    }
  }
}
