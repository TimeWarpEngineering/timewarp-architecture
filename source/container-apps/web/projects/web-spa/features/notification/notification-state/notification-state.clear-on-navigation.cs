#region Purpose
// ClearOnNavigation action: drops every shell message bar when the route changes.
#endregion

#region Design
// Rule 4 of the single-region design: a failure raised on one page must not follow the user
// to the next. NavigationListener dispatches this from NavigationManager.LocationChanged —
// the same event RouteState.ChangeRoute drives — so both state-driven and NavLink navigation
// clear the region. Success bars are cleared too; they are transient by definition.
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  public static class ClearOnNavigationActionSet
  {
    public sealed class Action : IBaseAction;

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        _ = action;
        _ = cancellationToken;
        NotificationState.Clear();
        return ValueTask.CompletedTask;
      }
    }
  }
}
