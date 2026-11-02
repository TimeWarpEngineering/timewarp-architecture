#region Purpose
// Server-side handler for SetRoleAgentScopes: replace a role's agent-scope list.
#endregion

#region Design
// Task 466: 404 when RoleStore has no role, same as SetRolePermissions. No protected-core
// guard: agent scopes cannot carry admin.* permissions. The response re-reads the store in
// AgentScopes.All order so the client sees the canonical list.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Roles.SetRoleAgentScopes;

public sealed partial class SetRoleAgentScopes
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleAgentScopeStore RoleAgentScopeStore;

    public Handler(IRoleAgentScopeStore roleAgentScopeStore)
    {
      RoleAgentScopeStore = roleAgentScopeStore
        ?? throw new ArgumentNullException(nameof(roleAgentScopeStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      if (!RoleStore.Roles.ContainsKey(command.RoleId))
      {
        return GetRole.Handler.RoleNotFound(command.RoleId);
      }

      List<string> ordered = OrderKnown(command.Scopes ?? []);
      await RoleAgentScopeStore
        .SetScopesForRoleAsync(command.RoleId, ordered, cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyList<string> stored = await RoleAgentScopeStore
        .GetScopesForRoleAsync(command.RoleId, cancellationToken)
        .ConfigureAwait(false);

      return new Response(OrderKnown(stored));
    }

    internal static List<string> OrderKnown(IReadOnlyList<string> scopes)
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
