#region Purpose
// Server-side handler for SetPrincipalAgentScopes: replace a principal's direct agent-scope grants.
#endregion

#region Design
// Task 466: 404 when the principal is missing. The response echoes the stored grants in
// AgentScopes.All order. Effective scopes (roles plus these grants plus identity:read) are
// read back through GetPrincipal, not this response.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Principals.SetPrincipalAgentScopes;

public sealed partial class SetPrincipalAgentScopes
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IPrincipalAgentScopeStore PrincipalAgentScopeStore;

    public Handler(IPrincipalStore principalStore, IPrincipalAgentScopeStore principalAgentScopeStore)
    {
      PrincipalStore = principalStore ?? throw new ArgumentNullException(nameof(principalStore));
      PrincipalAgentScopeStore = principalAgentScopeStore
        ?? throw new ArgumentNullException(nameof(principalAgentScopeStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      var principalId = PrincipalId.From(command.PrincipalId);
      Principal? principal = await PrincipalStore
        .GetPrincipalAsync(principalId, cancellationToken)
        .ConfigureAwait(false);
      if (principal is null)
      {
        return PrincipalProblems.NotFound(principalId);
      }

      List<string> ordered = OrderKnown(command.Scopes ?? []);
      await PrincipalAgentScopeStore
        .SetScopesAsync(principalId, ordered, cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyList<string> stored = await PrincipalAgentScopeStore
        .GetScopesAsync(principalId, cancellationToken)
        .ConfigureAwait(false);

      return new Response(OrderKnown(stored));
    }

    private static List<string> OrderKnown(IReadOnlyList<string> scopes)
    {
      List<string> ordered = [];
      foreach (string scope in AgentScopes.All)
      {
        if (scopes.Contains(scope, StringComparer.Ordinal))
        {
          ordered.Add(scope);
        }
      }

      return ordered;
    }
  }
}
