#region Purpose
// SetRolePermissions action: persists draft permission multi-select for one role.
#endregion

#region Design
// Task 182-004 / 206: mirrors PrincipalState.SetPrincipalRoles — draft toggles are pure state
// (RoleDetailPage grouped editor); Save posts SetRolePermissions. HandleSuccess does not
// fetch; LastSetRolePermissionsSucceeded is set false in GetRequest and true in HandleSuccess
// so RoleDetailPage sequences FetchRoles only after a successful write (failed 409 keeps drafts).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRolePermissions;

partial class RoleState
{
  public static class SetPermissionSelectedActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
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
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
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

        return default;
      }
    }
  }

  public static class SetRolePermissionsActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
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
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher)
        : base(store, webServerApiService, logger, publisher)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        RoleState.LastSetRolePermissionsSucceeded = false;
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
        RoleState.LastSetRolePermissionsSucceeded = true;
        return Task.CompletedTask;
      }
    }
  }
}
