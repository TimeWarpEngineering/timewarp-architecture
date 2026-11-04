#region Purpose
// Server-side handler for SetRoleAgentScopes: replace a role's agent-scope list.
#endregion

#region Design
// Task 466: 404 when the role does not exist, same as SetRolePermissions. No protected-core
// guard: agent scopes cannot carry admin.* permissions. Task 473: the write is
// Role.SetAgentScopes (full replace) saved through IRoleStore; the response goes through
// AgentScopeOrder so the client sees AgentScopes.All order.
// A scope stored on Member approves a roleless human, including one who holds an agent key.
// A self-registered agent has no effective role, so Member scopes do not approve it.
// This handler still stores the list.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.SetRoleAgentScopes;

public sealed partial class SetRoleAgentScopes
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleStore RoleStore;

    public Handler(IRoleStore roleStore)
    {
      RoleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      Role? role = await RoleStore.FindAsync(RoleId.From(command.RoleId), cancellationToken).ConfigureAwait(false);
      if (role is null)
      {
        return GetRole.Handler.RoleNotFound(command.RoleId);
      }

      role.SetAgentScopes(AgentScopeOrder.OrderKnown(command.Scopes ?? []));
      await RoleStore.UpdateAsync(role, cancellationToken).ConfigureAwait(false);

      return new Response(AgentScopeOrder.OrderKnown([.. role.AgentScopes.Select(scope => scope.Scope)]));
    }
  }
}
