#region Purpose
// OpenNewRole: opens the New Role form (RolePage, /Admin/Roles/New).
#endregion

#region Design
// Task 506: the Roles list's New Role button was a plain FluentAnchorButton Href, so neither
// Ctrl-K nor Ask AI could find a way to start a role. The button now dispatches this action.
// It is catalogued Both, with RolePage's own policy (admin.roles.manage), so the palette lists
// "Role: Open new role" and the agent gets a tool on every page for a principal who may create
// roles. CreateRole stays the agent tool on the New Role page itself. Handlers do not dispatch
// other actions (TWS0002), so the handler navigates with NavigationManager, as OpenFeedback and
// RedirectToLogin do, and skips the move when the browser is already on the form.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

partial class RoleState
{
  public static class OpenNewRoleActionSet
  {
    [CatalogAction
    (
      Description = "Open the New Role form to create an authorization role.",
      Permissions = [PermissionIds.AdminRolesManage],
      Visibility = ActionVisibility.Both
    )]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, NavigationManager navigationManager) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        string target = navigationManager.ToAbsoluteUri(RolePage.GetPageUrl()).ToString();
        if (!string.Equals(navigationManager.Uri, target, StringComparison.OrdinalIgnoreCase))
        {
          navigationManager.NavigateTo(target);
        }

        return default;
      }
    }
  }
}
