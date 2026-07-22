#region Purpose
// Shared SignalR hub route and typed hub-service contract for the chat feature.
#endregion

#region Design
// The route lives in contracts so the client HubConnection URL and the server MapHub registration
// cannot drift apart.
// IChatHubService types the hub's method surface, keeping string-based hub invocations out of feature code.
#endregion

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
