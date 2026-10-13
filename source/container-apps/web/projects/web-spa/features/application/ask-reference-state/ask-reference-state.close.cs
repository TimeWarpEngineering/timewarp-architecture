#region Purpose
// Close: hide the @ picker (Esc, Close, or a dismissed panel). Chosen references stay.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class CloseActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AskReferenceState.IsOpen = false;
        AskReferenceState.SearchGeneration++;
        AskReferenceState.ResetPicker();
        return ValueTask.CompletedTask;
      }
    }
  }
}
