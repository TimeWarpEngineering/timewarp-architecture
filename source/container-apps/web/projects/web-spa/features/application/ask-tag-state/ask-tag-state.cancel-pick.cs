#region Purpose
// CancelPick: leave pick mode without attaching anything (the panel's Cancel, or a closed panel).
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
        if (!AskTagState.IsPicking)
        {
          return;
        }

        AskTagState.PickGeneration++;
        AskTagState.IsPicking = false;
        await AskTagJsModule.CancelPickAsync(jsRuntime);
      }
    }
  }
}
