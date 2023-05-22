namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Chat.Spa.ChatState;

[Page("/Chat")]
public partial class ChatPage
{
  private string User { get; set; } = string.Empty;
  private string Message { get; set; } = string.Empty;
  private List<string> ChatMessages { get; set; } = new List<string>();

  [Inject] private ChatHubConnection ChatHubConnection { get; set; }
  

  protected override async Task OnInitializedAsync()
  {
    ChatHubConnection.OnReceiveMessage += ReceiveMessage;
    await ChatHubConnection.ConnectAsync();
  }

  private async Task SendMessage()
  {
    if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Message) && ChatHubConnection.IsConnected)
    {
      var sendMessageAction = new SendMessageAction();
      sendMessageAction.SendMessageCommand.User = User;
      sendMessageAction.SendMessageCommand.Message = Message;
      await Send(sendMessageAction);
      Message = string.Empty;
    }
  }

  private Task ReceiveMessage(string user, string message)
  {
    ChatMessages.Add($"{user}: {message}");
    StateHasChanged();
    return Task.CompletedTask;
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
    ChatHubConnection.OnReceiveMessage -= ReceiveMessage;
    ChatHubConnection.Dispose();
  }
}
