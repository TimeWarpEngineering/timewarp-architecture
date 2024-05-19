namespace TimeWarp.Architecture.Hubs;

public sealed class ChatHubConnection : IDisposable
{
  private readonly HubConnection HubConnection;
  private readonly ISender Sender;
  public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

  public ChatHubConnection(NavigationManager navigationManager, ISender sender)
  {
    Sender = sender;
    var chatHubUrl = new Uri(new Uri(navigationManager.BaseUri), ChatHubConstants.Route);
    HubConnection = new HubConnectionBuilder()
    .WithUrl(chatHubUrl)
    .Build();

    HubConnection.On<ReceiveMessage.Command>
    (
      nameof(ReceiveMessage), (command) =>
      {
        Sender.Send(new ChatState.ServerToClientMessage.Action(command));
        return Task.CompletedTask;
      }
    );
  }

  public async Task ConnectAsync()
  {
    await HubConnection.StartAsync();
  }

  public async Task DisconnectAsync()
  {
    await HubConnection.StopAsync();
  }

  public void Dispose()
  {
    HubConnection.DisposeAsync();
  }

  public async Task SendMessageAsync(SendMessage.Command sendMessageCommand)
  {
    await HubConnection.InvokeAsync(nameof(SendMessage), sendMessageCommand);
  }
}

