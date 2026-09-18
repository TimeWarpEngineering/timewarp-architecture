#region Purpose
// ApplicationState action that wipes the entire store. Callers sequence any route change.
#endregion

#region Design
// Wipes the store only. The Counter page sequences ResetStore then ChangeRoute to home
// because the page being viewed may depend on state that was just re-initialized; landing
// on home guarantees a valid render after the wipe. Template demo of full-store reset.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class ResetStoreActionSet
  {
    public class Action : IBaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) {}
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        _ = action;
        _ = cancellationToken;
        Store.Reset();
        return default;
      }
    }
  }
}
