namespace TimeWarp.Architecture.Features.Chat.Spa;

public sealed partial class ChatState
{
  [TrackProcessing]
  public record ServerToClientMessageAction(ReceiveMessage.Command Command) : BaseAction { }

  internal sealed class ReceiveMessageHandler : BaseHandler<ServerToClientMessageAction>
  {
    public ReceiveMessageHandler(IStore store) : base(store) { }

    public override Task Handle(ServerToClientMessageAction receiveMessageAction, CancellationToken cancellationToken)
    {
      ChatMessage chatMessage = new()
      {
        Message = receiveMessageAction.Command.Message,
        User = receiveMessageAction.Command.User
      };

      ChatState.ChatMessages.Add(chatMessage);
      return Task.CompletedTask;
    }
  }
}
