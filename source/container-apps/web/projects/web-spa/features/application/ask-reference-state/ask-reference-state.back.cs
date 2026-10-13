#region Purpose
// Back: leave the chosen type's search and return to the type list (Backspace on an empty term).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class BackActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (AskReferenceState.IsOpen && AskReferenceState.SelectedType is not null)
        {
          AskReferenceState.SearchGeneration++;
          AskReferenceState.ShowTypes("");
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
