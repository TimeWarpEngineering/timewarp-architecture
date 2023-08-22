namespace TimeWarp.Architecture.Features.Chat;

public sealed partial class ChatState
{
  public static class ServerToClientMessage
  {
    [TrackProcessing]
    public record Action(ReceiveMessage.Command Command) : BaseAction { }

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ChatMessage chatMessage = new()
        {
          Message = action.Command.Message,
          User = action.Command.User
        };

        ChatState.ChatMessages.Add(chatMessage);
        return Task.CompletedTask;
      }
    }
  }
}
