namespace TimeWarp.Architecture.Features.Chat;

partial class ChatState
{
  public static class ClientToServerMessage
  {
    [TrackAction]
    public class Action(SendMessage.Command SendMessageCommand) : IBaseAction
    {
      public SendMessage.Command SendMessageCommand { get; set; } = SendMessageCommand;
    }

    [UsedImplicitly]
    internal sealed class Handler
    (
      IStore store,
      ChatHubConnection chatHubConnection
    ) : BaseHandler<Action>(store)
    {
      private ChatHubConnection ChatHubConnection { get; set; } = chatHubConnection;

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        await ChatHubConnection.SendMessageAsync(action.SendMessageCommand);
      }
    }
  }
}
