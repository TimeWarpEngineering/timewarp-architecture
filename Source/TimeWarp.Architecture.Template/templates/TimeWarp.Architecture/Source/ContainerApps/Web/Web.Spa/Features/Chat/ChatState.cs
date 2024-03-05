namespace TimeWarp.Architecture.Features.Chat;

[StateAccessMixin]
public sealed partial class ChatState : State<ChatState>
{
  private readonly List<ChatMessage> ChatMessageList = [];
  public IReadOnlyList<ChatMessage> ChatMessages => ChatMessageList.AsReadOnly();
  public override void Initialize() {}

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
