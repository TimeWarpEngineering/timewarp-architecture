namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Chat.ChatState;

[Page("/Chat")]
public partial class ChatPage
{
  private string User { get; set; } = string.Empty;
  private string Message { get; set; } = string.Empty;
  private List<ChatMessage> ChatMessages => ChatState.ChatMessages;

  [Inject] private ChatHubConnection ChatHubConnection { get; set; }
  

  protected override async Task OnInitializedAsync()
  {
    await ChatHubConnection.ConnectAsync();
  }

  private async Task SendMessage()
  {
    if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Message) && ChatHubConnection.IsConnected)
    {
      var sendMessageAction =
        new ClientToServerMessageAction
        (
          new SendMessage.Command { User = User, Message = Message}
        );
      
      await Send(sendMessageAction);
      Message = string.Empty;
    }
  }

  private async Task HandleKeyDown(KeyboardEventArgs e)
  {
    if (e.Key == "Enter")
    {
      await SendMessage();
    }
  }

  public override void Dispose()
  {
    ChatHubConnection.Dispose();
  }
}
