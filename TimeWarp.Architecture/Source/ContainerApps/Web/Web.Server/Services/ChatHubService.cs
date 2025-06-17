namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Features.Chat;

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
