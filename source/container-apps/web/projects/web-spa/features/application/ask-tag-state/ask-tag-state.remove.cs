#region Purpose
// Remove: drop one composer chip before the message is sent.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskTagState
{
  public static class RemoveActionSet
  {
    public sealed class Action : IBaseAction
    {
      public int Id { get; }

      public Action(int id)
      {
        Id = id;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        List<AskTag> attached = [.. AskTagState.Attached];
        attached.RemoveAll(tag => tag.Id == action.Id);
        AskTagState.Attached = attached;
        return ValueTask.CompletedTask;
      }
    }
  }
}
