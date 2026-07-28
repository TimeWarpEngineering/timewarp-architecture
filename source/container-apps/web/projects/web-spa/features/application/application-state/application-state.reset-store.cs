#region Purpose
// ApplicationState action that wipes the entire store and returns to the home route.
#endregion

#region Design
// Redirects to "/" after Store.Reset because the page being viewed may depend on state that
// was just re-initialized; landing on home guarantees a valid render after the wipe.
// Template demo of full-store reset, wired to the Counter page reset button.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class ResetStoreActionSet
  {
    internal class Action : IBaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) {}
      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        Store.Reset();
        await RouteState.ChangeRoute(newRoute: "/", cancellationToken);
      }
    }
  }
}
