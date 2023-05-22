namespace TimeWarp.Architecture.Services;

public sealed class ChatHubService : IChatHubService
{
  private readonly IHubContext<ChatHub> HubContext;

  public ChatHubService(IHubContext<ChatHub> hubContext)
  {
      HubContext = hubContext;
  }

  public async Task SendMessageToAll(string user, string message, CancellationToken cancellationToken)
  {
      
      await HubContext.Clients.All.SendAsync("ReceiveMessage", user, message, cancellationToken);
  }

  // Implement other methods as needed
}
