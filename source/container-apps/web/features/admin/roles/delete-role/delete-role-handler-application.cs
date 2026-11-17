#region Purpose
// Server-side handler for the DeleteRole command.
#endregion

#region Design
// Task 473: a missing id reuses GetRole's 404-shaped problem details so all roles handlers report
// absence identically. A product role (RoleIds.All) answers 409: the rule is Role.IsProductRole in
// the domain, and IRoleStore.RemoveAsync enforces it again (Role.EnsureRemovable), so this check
// only turns the refusal into a problem value instead of an exception. Removing a custom role
// removes its permission and agent-scope rows with it (owned children).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using static TimeWarp.Architecture.Features.Admin.Roles.DeleteRole;

public sealed partial class DeleteRole
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleStore RoleStore;

    public Handler(IRoleStore roleStore)
    {
      RoleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
    }

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      Role? role = await RoleStore.FindAsync(RoleId.From(command.RoleId), cancellationToken).ConfigureAwait(false);
      if (role is null)
      {
        return GetRole.Handler.RoleNotFound(command.RoleId);
      }

      if (role.IsProductRole)
      {
        return ProductRoleNotDeletable(command.RoleId);
      }

      await RoleStore.RemoveAsync(role.Id, cancellationToken).ConfigureAwait(false);
      return new Response();
    }

    internal static SharedProblemDetails ProductRoleNotDeletable(Guid roleId) => new()
    {
      Title = "Product role cannot be deleted",
      Status = 409,
      Detail = $"Role '{roleId}' is a product role (Member, Operator, Administrator, Developer) and cannot be deleted."
    };
  }
}
