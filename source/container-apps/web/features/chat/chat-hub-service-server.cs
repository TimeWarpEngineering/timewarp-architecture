#region Purpose
// Server-initiated chat broadcast: pushes ReceiveMessage to all connected clients outside any hub invocation.
#endregion

#region Design
// Wraps IHubContext<ChatHub> behind IChatHubService (defined in Web.Contracts) so application
// handlers can push messages without referencing SignalR or the hub type.
// Reuses the ReceiveMessage.Command contract as the wire payload and nameof(ReceiveMessage) as
// the method name, keeping the server push and the Spa client handler bound to one shared shape.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

public sealed class ChatHubService : IChatHubService
{
  private readonly IHubContext<ChatHub> HubContext;

  public ChatHubService(IHubContext<ChatHub> hubContext)
  {
    HubContext = hubContext;
  }

  public async Task SendMessageToAll(string user, string message, CancellationToken cancellationToken)
  {
    var command = new ReceiveMessage.Command()
    {
      User = user,
      Message = message
    };

    await HubContext.Clients.All.SendAsync(nameof(ReceiveMessage), command, cancellationToken);
  }

  // Implement other methods as needed
}
