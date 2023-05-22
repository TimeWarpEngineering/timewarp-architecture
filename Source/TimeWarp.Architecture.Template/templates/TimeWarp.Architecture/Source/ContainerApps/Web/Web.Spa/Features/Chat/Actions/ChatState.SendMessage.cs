namespace TimeWarp.Architecture.Features.Chat.Spa;

public sealed partial class ChatState
{
  [TrackProcessing]
  public record SendMessageAction : BaseAction
  {
    public Contracts.SendMessage.Command SendMessageCommand { get; set; } = new();

  }

  internal sealed class SendMessageHandler : BaseHandler<SendMessageAction>
  {
    private ChatHubConnection ChatHubConnection { get; set; }

    public SendMessageHandler(IStore store, ChatHubConnection chatHubConnection) : base(store)
    {
      ChatHubConnection = chatHubConnection;
    }

    public override async Task Handle(SendMessageAction sendMessageAction, CancellationToken cancellationToken)
    {
      await ChatHubConnection.SendMessageAsync(sendMessageAction.SendMessageCommand);
    }
  }
}
