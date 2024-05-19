namespace TimeWarp.Architecture.Features.Chat;

public sealed partial class ChatState
{
  public static class ServerToClientMessage
  {
    [TrackAction]
    public class Action(ReceiveMessage.Command Command) : IBaseAction
    {
      public ReceiveMessage.Command Command { get; set; } = Command;
    }

    [UsedImplicitly]
    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
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
