#region Purpose
// Server-side handler: human denies a pending agent-human link they own.
#endregion

#region Design
// Mirror of ApproveAgentHumanLink with Deny(). A later RequestAgentHumanLink for the same pair
// succeeds because FindOpen ignores Denied rows.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentLinks.DenyAgentHumanLink;

public sealed class DenyAgentHumanLink
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
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
      Command command,
      CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken).ConfigureAwait(false);
      if (callerId is null)
      {
        return AgentLinkProblems.Unauthenticated();
      }

      Principal? caller = await PrincipalStore.GetPrincipalAsync(callerId.Value, cancellationToken).ConfigureAwait(false);
      if (caller is null || caller.Kind != PrincipalKind.Human)
      {
        return AgentLinkProblems.Forbidden("Only a human principal can deny a link.");
      }

      var linkId = AgentHumanLinkId.From(command.LinkId);
      AgentHumanLink? link = await LinkStore.FindAsync(linkId, cancellationToken).ConfigureAwait(false);
      if (link is null)
      {
        return AgentLinkProblems.NotFound();
      }

      if (link.HumanPrincipalId != callerId.Value.Value)
      {
        return AgentLinkProblems.Forbidden("This link belongs to a different human.");
      }

      if (link.Status != AgentHumanLinkStatus.Pending)
      {
        return AgentLinkProblems.NotPending();
      }

      link.Deny();
      await LinkStore.UpdateAsync(link, cancellationToken).ConfigureAwait(false);
      return new Response(link.Id.Value, link.Status.ToString());
    }
  }
}
