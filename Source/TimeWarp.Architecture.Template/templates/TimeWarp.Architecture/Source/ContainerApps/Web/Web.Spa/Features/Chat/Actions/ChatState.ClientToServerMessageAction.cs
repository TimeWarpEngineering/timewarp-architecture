namespace TimeWarp.Architecture.Features.Chat;

public sealed partial class ChatState
{
  public static class ClientToServerMessage
  {
    [TrackAction]
    public record Action(SendMessage.Command SendMessageCommand) : BaseAction;

    internal sealed class Handler : BaseHandler<Action>
    {
      private ChatHubConnection ChatHubConnection { get; set; }

      public Handler(IStore store, ChatHubConnection chatHubConnection) : base(store)
      {
        ChatHubConnection = chatHubConnection;
      }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        await ChatHubConnection.SendMessageAsync(action.SendMessageCommand);
      }
    }
  }
}
