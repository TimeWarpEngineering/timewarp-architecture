namespace TimeWarp.Architecture.Features.Chat;

[StateAccessMixin]
public sealed partial class ChatState : State<ChatState>
{
   public List<ChatMessage> ChatMessages { get; set; } = new();
   public override void Initialize() { }

   public sealed class ChatMessage
   {
     public string Message { get; set; }
     public string User { get; set; }
   }
}
