#region Purpose
// CancelPick: leave pick mode without attaching anything (the panel's Cancel, or a closed panel).
#endregion

#region Design
// Cancels the pick in flight, armed or still arming: the browser is told its token, so a cancel
// that reaches ask-tag.ts before that pick's Start stops it from arming (task 351). Clearing
// PickToken first makes the pick's answer stale; IsPicking turns off only after the browser has
// left pick mode, so the "picking" line never disappears while the overlay is still up.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Services;

partial class AskTagState
{
  public static class CancelPickActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      IJSRuntime jsRuntime
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (AskTagState.PickToken is not { } token)
        {
          return;
        }

        AskTagState.PickToken = null;
        await AskTagJsModule.CancelPickAsync(jsRuntime, token);
        AskTagState.IsPicking = false;
      }
    }
  }
}
