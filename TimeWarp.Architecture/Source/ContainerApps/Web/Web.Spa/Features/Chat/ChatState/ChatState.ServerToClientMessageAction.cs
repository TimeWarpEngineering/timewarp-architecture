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
