#region Purpose
// ChatState action that pushes an outbound chat message to the server over the SignalR hub.
#endregion

#region Design
// Side-effect-only handler: it does not touch ChatMessages. The message enters the
// transcript only when the server broadcasts it back via ServerToClientMessageActionSet, so
// the sender and all other clients share one render path.
// Wraps the SendMessage.Command hub contract so the UI dispatches a state action
// instead of talking to ChatHubConnection directly.
#endregion

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
      private ChatHubConnection ChatHubConnection { get; }

      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        await ChatHubConnection.SendMessageAsync(action.SendMessageCommand);
      }
    }
  }
}
