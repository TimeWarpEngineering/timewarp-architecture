namespace TimeWarp.Architecture.Pages;

using static ChatState;


[Page("/Chat")]
partial class ChatPage
{
  private string User { get; set; } = string.Empty;
  private string Message { get; set; } = string.Empty;
  private IEnumerable<ChatMessage> ChatMessages => ChatState.ChatMessages ?? Enumerable.Empty<ChatMessage>();

  [Inject] private ChatHubConnection ChatHubConnection { get; set; } = default!;


  protected override async Task OnInitializedAsync()
  {
    await ChatHubConnection.ConnectAsync();
  }

  private async Task SendMessage()
  {
    if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Message) && ChatHubConnection.IsConnected)
    {
      await ChatState.SendMessageToServer
      (
        new SendMessage.Command
        {
          User = User,
          Message = Message
        }
      );

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
    GC.SuppressFinalize(this);
    ChatHubConnection.Dispose();
  }
}
