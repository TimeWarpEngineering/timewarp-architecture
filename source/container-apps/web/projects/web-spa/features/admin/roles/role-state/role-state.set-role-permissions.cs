#region Purpose
// SetRolePermissions action: persists draft permission multi-select for one role.
#endregion

#region Design
// Task 182-004 / 206: mirrors PrincipalState.SetPrincipalRoles — draft toggles are pure state
// (RoleDetailPage grouped editor); Save posts SetRolePermissions. HandleSuccess does not
// fetch; RoleDetailPage sequences SetRolePermissions then FetchRoles so drafts re-seed from
// stored grants (and protected-core / validation errors surface via DefaultApiHandler
// problem handling).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRolePermissions;

partial class RoleState
{
  public static class SetPermissionSelectedActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }
      public string PermissionId { get; }
      public bool Selected { get; }

      public Action(Guid roleId, string permissionId, bool selected)
      {
        RoleId = roleId;
        PermissionId = permissionId;
        Selected = selected;
      }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        if (!RoleState.DraftPermissionIds.TryGetValue(action.RoleId, out HashSet<string>? set))
        {
          set = new HashSet<string>(StringComparer.Ordinal);
          RoleState.DraftPermissionIds[action.RoleId] = set;
        }

        if (action.Selected)
        {
          set.Add(action.PermissionId);
        }
        else
        {
          set.Remove(action.PermissionId);
        }

        return Task.CompletedTask;
      }
    }
  }

  public static class SetRolePermissionsActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }

      public Action(Guid roleId)
      {
        RoleId = roleId;
      }
    }

    internal class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger)
        : base(store, webServerApiService, logger)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        IReadOnlyCollection<string> draft = RoleState.GetDraftPermissionIds(action.RoleId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          RoleId = action.RoleId,
          PermissionIds = [.. draft]
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        return Task.CompletedTask;
      }
    }
  }
}
