#region Purpose
// MarkSent: the chips went out with a message; move them to the conversation's sent tags.
#endregion

#region Design
// Dispatched by AskTagChatClient once the request that carried the chips succeeded, with the ids
// it sent: only those chips move, a chip attached meanwhile stays for the next message. Sent
// keeps the newest SentCap, so page_context.tags can still name an element tagged turns ago.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskTagState
{
  public static class MarkSentActionSet
  {
    public sealed class Action : IBaseAction
    {
      public IReadOnlyList<int> Ids { get; }

      public Action(IReadOnlyList<int> ids)
      {
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AskTagState.Rebase(AgentSurfaceState.ConversationGeneration);
        List<AskTag> moved = [.. AskTagState.Attached.Where(tag => action.Ids.Contains(tag.Id))];
        List<AskTag> sent = [.. AskTagState.Sent, .. moved];
        if (sent.Count > SentCap)
        {
          sent.RemoveRange(0, sent.Count - SentCap);
        }

        AskTagState.Sent = sent;
        AskTagState.Attached = [.. AskTagState.Attached.Where(tag => !action.Ids.Contains(tag.Id))];
        return ValueTask.CompletedTask;
      }
    }
  }
}
