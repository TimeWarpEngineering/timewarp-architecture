namespace TimeWarp.Architecture.Features.Chat;

public static class ChatHubConstants
{
  public const string Route = "/chat-hub";
}

public interface IChatHubService
{
  //Task<SignalrResult<Success, SharedProblemDetails>> SendMessage(SendMessage.Command sendMessageCommand);
  Task SendMessageToAll(string user, string message, CancellationToken cancellationToken);
}
