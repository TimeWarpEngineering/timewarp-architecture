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
// Teardown semantics: the generated method reads the state's own CancellationToken on every call,
// where the raw send passed CancellationToken.None. State<TState>.Dispose cancels AND THEN disposes
// the CancellationTokenSource, so after disposal the token cannot be read at all — reading
// CancellationTokenSource.Token throws ObjectDisposedException. A message arriving after teardown
// therefore faults inside SignalR's handler instead of being silently dropped; that is left
// unguarded deliberately (unlike the event-stream trace), because a dropped chat message is real
// data loss and should surface in the hub's own logging.
// The handler is typed as an explicit Func<..., Task> local: HubConnection.On has both
// Func<T, Task> and Action<T> overloads, and letting overload resolution pick is how an inbound
// dispatch silently reverts to async-void.
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

    Func<ReceiveMessage.Command, Task> onReceiveMessage =
      async (command) =>
        await Store.GetState<ChatState>().ServerToClientMessage(command);

    HubConnection.On(nameof(ReceiveMessage), onReceiveMessage);
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
