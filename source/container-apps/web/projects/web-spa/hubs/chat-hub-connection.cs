#region Purpose
// SignalR client wrapper for the chat hub: outbound sends and inbound message dispatch.
#endregion

#region Design
// Inbound hub messages never touch state directly; they are re-dispatched as ChatState actions
// so every mutation flows through the mediator pipeline (logging, DevTools, behaviors).
// Dispatch goes through the generated ChatState.ServerToClientMessage(command) ActionSet method
// (via IStore) rather than a raw ISender.Send — TWA0022 bans direct mediator Send in SPA client
// code, and the generated method wires the CancellationToken and is awaited, so an inbound
// message is no longer fire-and-forget.
// Hub method names bind via nameof to the shared SendMessage/ReceiveMessage contracts, so
// client and server cannot drift silently.
// The hub URL derives from NavigationManager.BaseUri, letting the same code work behind the
// YARP gateway or direct hosting without configuration.
#endregion

namespace TimeWarp.Architecture.Hubs;

public sealed class ChatHubConnection : IDisposable
{
  private readonly HubConnection HubConnection;
  private readonly IStore Store;
  public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

  public ChatHubConnection(NavigationManager navigationManager, IStore store)
  {
    Store = store;
    var chatHubUrl = new Uri(new Uri(navigationManager.BaseUri), ChatHubConstants.Route);
    HubConnection = new HubConnectionBuilder()
    .WithUrl(chatHubUrl)
    .Build();

    HubConnection.On<ReceiveMessage.Command>
    (
      nameof(ReceiveMessage),
      async (command) =>
        await Store.GetState<ChatState>().ServerToClientMessage(command)
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
    HubConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
  }

  public async Task SendMessageAsync(SendMessage.Command sendMessageCommand)
  {
    await HubConnection.InvokeAsync(nameof(SendMessage), sendMessageCommand);
  }
}
