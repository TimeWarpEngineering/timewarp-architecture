#region Purpose
// MarkSent: the chips went out with a message; move them to the conversation's sent tags.
#endregion

#region Design
// Dispatched by AskTagChatClient after it appended the chips to the outgoing user message. Sent
// keeps the newest SentCap, so page_context.tags can still name an element tagged turns ago.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskTagState
{
  public static class MarkSentActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AskTagState.Rebase(AgentSurfaceState.ConversationGeneration);
        List<AskTag> sent = [.. AskTagState.Sent, .. AskTagState.Attached];
        if (sent.Count > SentCap)
        {
          sent.RemoveRange(0, sent.Count - SentCap);
        }

        AskTagState.Sent = sent;
        AskTagState.Attached = [];
        return ValueTask.CompletedTask;
      }
    }
  }
}
