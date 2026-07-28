#region Purpose
// State slice holding the chat transcript received over the SignalR hub.
#endregion

#region Design
// The list is private with a read-only projection so components can only mutate it
// through the actions in the sibling partial files.
// ChatMessage is nested here as a client-side view model, deliberately separate from
// the SendMessage/ReceiveMessage hub contracts.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

[StateAccess]
public sealed partial class ChatState : State<ChatState>
{
  private List<ChatMessage>? ChatMessageList { get; set; }
  public IReadOnlyList<ChatMessage>? ChatMessages => ChatMessageList?.AsReadOnly();
  public override void Initialize() => ChatMessageList = null;

  public sealed class ChatMessage
  (
    string message,
    string user
  )
  {
    public string Message { get; init; } = message;
    public string User { get; init; } = user;
  }
}
