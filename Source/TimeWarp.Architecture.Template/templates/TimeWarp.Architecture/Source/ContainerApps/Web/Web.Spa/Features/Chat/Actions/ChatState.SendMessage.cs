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
    private TimeWarpHubConnection TimeWarpHubConnection { get; set; }

    public SendMessageHandler(IStore store, TimeWarpHubConnection timeWarpHubConnection) : base(store)
    {
      TimeWarpHubConnection = timeWarpHubConnection;
    }

    public override async Task Handle(SendMessageAction sendMessageAction, CancellationToken cancellationToken)
    {
      await TimeWarpHubConnection.SendMessageAsync(sendMessageAction.SendMessageCommand);
    }
  }
}
