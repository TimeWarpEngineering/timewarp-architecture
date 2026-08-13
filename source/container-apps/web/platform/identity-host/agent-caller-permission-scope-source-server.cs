#region Purpose
// Host adapter: ambient IAgentCallerContext → IAgentPermissionScopeSource for PermissionEvaluator.
#endregion

#region Design
// Task 182-006: keeps PermissionEvaluator free of IAgentCallerContext so Features tests avoid
// CS0433 under web-jaribu multi-mode (api + web both define IAgentCallerContext). Scoped;
// same request as AgentCallerContext / PermissionEvaluator.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Identity;

/// <summary>Maps <see cref="IAgentCallerContext"/> to <see cref="IAgentPermissionScopeSource"/>.</summary>
public sealed class AgentCallerPermissionScopeSource : IAgentPermissionScopeSource
{
  private readonly IAgentCallerContext AgentCallerContext;

  public AgentCallerPermissionScopeSource(IAgentCallerContext agentCallerContext)
  {
    AgentCallerContext = agentCallerContext
      ?? throw new ArgumentNullException(nameof(agentCallerContext));
  }

  public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId)
  {
    AgentCaller? caller = AgentCallerContext.GetCurrentCaller();
    if (caller is null || caller.PrincipalId != principalId)
    {
      return null;
    }

    return caller.Scopes;
  }
}
