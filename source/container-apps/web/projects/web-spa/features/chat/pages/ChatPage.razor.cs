#region Purpose
// Code-behind for the chat demo page: hub lifecycle, outbound dispatch, transcript binding.
#endregion

#region Design
// The page owns the SignalR connection lifecycle — connect in OnInitializedAsync,
// dispose on page teardown — so the socket exists only while chat is in use.
// Sends go through the ChatState action rather than ChatHubConnection directly, so
// the message renders only after the server broadcast; the page never mutates the
// transcript itself.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

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
    base.Dispose();
    GC.SuppressFinalize(this);
    ChatHubConnection.Dispose();
  }
}
