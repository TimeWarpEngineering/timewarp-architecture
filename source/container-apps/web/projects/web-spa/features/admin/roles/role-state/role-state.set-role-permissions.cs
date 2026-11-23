#region Purpose
// SetRolePermissions: the local permission toggle and the hooks of the generated save action.
#endregion

#region Design
// Task 182-004 / 206: mirrors PrincipalState.SetPrincipalRoles. Draft toggles are pure state
// (RoleDetailPage grouped editor), so SetPermissionSelectedActionSet stays hand-written. The save,
// SetRolePermissionsActionSet and RoleState.SetRolePermissions(Command), is generated from
// [ContractState] (task 483), with SetRolePermissions in DraftRequests. So BuildSetRolePermissionsRequest
// is required: it fills PermissionIds from the draft for the RoleId the page set. It also resets
// LastSetRolePermissionsSucceeded, and OnSetRolePermissionsSucceeded sets it, so RoleDetailPage
// sequences GetRoles only after a successful write (a failed 409 keeps the drafts). Success does not fetch.
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
          set = [with(StringComparer.Ordinal)];
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

  private partial void BuildSetRolePermissionsRequest(Command request)
  {
    LastSetRolePermissionsSucceeded = false;
    request.PermissionIds = [.. GetDraftPermissionIds(request.RoleId)];
  }

  partial void OnSetRolePermissionsSucceeded(Response response) => LastSetRolePermissionsSucceeded = true;
}
