#region Purpose
// ChatState action that appends a hub-pushed message to the transcript.
#endregion

#region Design
// Dispatched by ChatHubConnection when the hub delivers a ReceiveMessage.Command.
// This is the single mutation path for ChatMessages — sent messages appear only
// after the server broadcasts them back, keeping every client consistent.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

using static ReceiveMessage;
partial class ChatState
{
  public static class ServerToClientMessage
  {
    [TrackAction]
    public class Action(Command Command) : IBaseAction
    {
      public Command Command { get; set; } = Command;
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) {}

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ChatState.ChatMessageList ??= [];
        ChatMessage chatMessage = new(action.Command.Message, action.Command.User);
        ChatState.ChatMessageList.Add(chatMessage);
        return Task.CompletedTask;
      }
    }
  }
}
