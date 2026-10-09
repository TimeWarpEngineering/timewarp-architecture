#region Purpose
// Returns whether xAI is configured. Does not call the model and does not read the key into the response.
#endregion

#region Design
// Status is unauthenticated-only plus the upstream predicate. It does not take an admission slot:
// the palette probes on every shell load, and that must not consume the completion budget.
// A missing principal is 401 even though the endpoint policy already requires one.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class GetAgentChatConfiguration
{
  /// <summary>Reads <see cref="IAgentChatUpstream.IsConfigured"/> for the current principal.</summary>
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IAgentChatUpstream Upstream;

    /// <summary>Creates the handler.</summary>
    public Handler(ICurrentPrincipalAccessor currentPrincipalAccessor, IAgentChatUpstream upstream)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Upstream = upstream;
    }

    /// <summary>Returns configured, the setup command, and the model id.</summary>
    public async Task<OneOf<Response, SharedProblemDetails>> Handle
    (
      Query request,
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

      bool configured = Upstream.IsConfigured;
      return new Response
      {
        Configured = configured,
        SetupCommand = XaiChatDefaults.SetupCommand,
        Model = configured ? Upstream.Model : null
      };
    }
  }
}
