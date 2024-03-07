namespace TimeWarp.Architecture.Features.Chat;

public sealed partial class ChatState
{
  public static class ServerToClientMessage
  {
    [TrackAction]
    public record Action(ReceiveMessage.Command Command) : BaseAction { }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ChatMessage chatMessage = new(action.Command.Message, action.Command.User);
        ChatState.ChatMessageList.Add(chatMessage);
        return Task.CompletedTask;
      }
    }
  }
}
