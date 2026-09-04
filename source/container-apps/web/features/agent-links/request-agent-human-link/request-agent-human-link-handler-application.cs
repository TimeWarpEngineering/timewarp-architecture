#region Purpose
// Server-side handler: agent requests a pending link to an existing human principal.
#endregion

#region Design
// Caller must be PrincipalKind.Agent; target must exist and be Human. An open (Pending/Approved)
// pair is 409 so a denied link can be requested again. IPrincipalStore is identity kernel (other
// assembly — free under TWA0009).
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentLinks.RequestAgentHumanLink;

public sealed class RequestAgentHumanLink
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
      if (caller is null || caller.Kind != PrincipalKind.Agent)
      {
        return AgentLinkProblems.Forbidden("Only an agent principal can request a human link.");
      }

      var humanId = PrincipalId.From(command.HumanPrincipalId);
      Principal? human = await PrincipalStore.GetPrincipalAsync(humanId, cancellationToken).ConfigureAwait(false);
      if (human is null || human.Kind != PrincipalKind.Human)
      {
        return AgentLinkProblems.HumanNotFound();
      }

      AgentHumanLink? open = await LinkStore.FindOpenAsync(callerId.Value, humanId, cancellationToken).ConfigureAwait(false);
      if (open is not null)
      {
        return AgentLinkProblems.AlreadyLinked();
      }

      var link = AgentHumanLink.Create(callerId.Value.Value, humanId.Value);
      await LinkStore.AddAsync(link, cancellationToken).ConfigureAwait(false);
      return new Response(link.Id.Value, link.Status.ToString());
    }
  }
}
