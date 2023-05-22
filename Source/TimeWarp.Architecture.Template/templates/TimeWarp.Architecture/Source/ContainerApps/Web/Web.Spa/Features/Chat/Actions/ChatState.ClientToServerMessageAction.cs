namespace TimeWarp.Architecture.Features.Chat.Spa;

public sealed partial class ChatState
{
  [TrackProcessing]
  public record ClientToServerMessageAction(SendMessage.Command SendMessageCommand) : BaseAction;

  internal sealed class SendMessageHandler : BaseHandler<ClientToServerMessageAction>
  {
    private ChatHubConnection ChatHubConnection { get; set; }

    public SendMessageHandler(IStore store, ChatHubConnection chatHubConnection) : base(store)
    {
      ChatHubConnection = chatHubConnection;
    }

    public override async Task Handle(ClientToServerMessageAction sendMessageAction, CancellationToken cancellationToken)
    {
      await ChatHubConnection.SendMessageAsync(sendMessageAction.SendMessageCommand);
    }
  }
}
