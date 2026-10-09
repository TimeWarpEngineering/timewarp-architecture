#region Purpose
// Admits one completion for the current principal and forwards it. Does not execute tools.
#endregion

#region Design
// The endpoint policy already requires a principal. The handler checks again so a miswired
// endpoint is not an open proxy to the key. A missing key returns 503 with the setup command
// and does not call the provider. Admission covers the upstream call only. The lease is
// disposed on every path, including cancellation.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class CompleteAgentChat
{
  /// <summary>Forwards an admitted turn to <see cref="IAgentChatUpstream"/>.</summary>
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IAgentChatUpstream Upstream;
    private readonly AgentChatAdmission Admission;

    /// <summary>Creates the handler.</summary>
    public Handler
    (
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IAgentChatUpstream upstream,
      AgentChatAdmission admission
    )
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Upstream = upstream;
      Admission = admission;
    }

    /// <summary>Returns the model text and function calls, or a problem.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle
    (
      Command request,
      CancellationToken cancellationToken
    )
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return AgentChatProblems.Unauthenticated();
      }

      if (!Upstream.IsConfigured)
      {
        return AgentChatProblems.NotConfigured();
      }

      using AgentChatAdmission.AdmissionLease lease = await Admission
        .TryAdmitAsync(principalId.Value.Value.ToString("D"), cancellationToken)
        .ConfigureAwait(false);
      if (!lease.Admitted)
      {
        return AgentChatProblems.TooManyRequests();
      }

      return await Upstream.CompleteAsync(request, cancellationToken).ConfigureAwait(false);
    }
  }
}
