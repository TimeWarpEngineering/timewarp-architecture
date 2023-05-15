namespace TimeWarp.Architecture.Hubs;

public sealed class TimeWarpHubConnection : IDisposable
{
  public const string Route = "/TimeWarpHub";
  private readonly HubConnection HubConnection;
  public event Func<string, string, Task> OnReceiveMessage;
  public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

  public TimeWarpHubConnection(string hubUrl)
  {
    HubConnection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .Build();

    HubConnection.On<string, string>("ReceiveMessage", (user, message) =>
    {
      OnReceiveMessage?.Invoke(user, message);
      return Task.CompletedTask;
    });
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

  // Add more methods for handling player interactions, such as sending and receiving messages

  public async Task SendMessageAsync(string user, string message)
  {
    var sendMessageCommand = new Features.Chat.Contracts.SendMessage.Command { User = user, Message = message };
    await HubConnection.InvokeAsync("SendMessage", sendMessageCommand);
  }

  public async Task SendMessageAsync(Features.Chat.Contracts.SendMessage.Command sendMessageCommand)
  {
    await HubConnection.InvokeAsync(nameof(Features.Chat.Contracts.SendMessage), sendMessageCommand);
  }

}

