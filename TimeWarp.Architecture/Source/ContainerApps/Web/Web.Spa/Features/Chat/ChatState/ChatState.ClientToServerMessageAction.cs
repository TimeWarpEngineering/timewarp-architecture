namespace TimeWarp.Architecture.Features.Chat;

partial class ChatState
{
  public static class SendMessageToServerActionSet
  {
    [TrackAction]
    public class Action : IBaseAction
    {
      public SendMessage.Command SendMessageCommand { get; set; }
      public Action(SendMessage.Command SendMessageCommand)
      {
        this.SendMessageCommand = SendMessageCommand;
      }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler
      (
        IStore store,
        ChatHubConnection chatHubConnection
      ) : base(store)
      {
        ChatHubConnection = chatHubConnection;
      }
      private ChatHubConnection ChatHubConnection { get; set; }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        await ChatHubConnection.SendMessageAsync(action.SendMessageCommand);
      }
    }
  }
}
