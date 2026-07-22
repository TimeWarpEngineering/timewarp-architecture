#region Purpose
// Handles SendMessage.Command by broadcasting the message to all connected chat clients.
#endregion

#region Design
// Returns OneOf<Success, ...> with no payload: delivery happens out of band via the SignalR hub push, so the
// HTTP reply only acknowledges acceptance.
// Depends on IChatHubService (defined in Web.Contracts) rather than IHubContext so this handler stays testable
// and Web.Application carries no SignalR server dependency; the hub-backed implementation lives in Web.Server.
#endregion

namespace TimeWarp.Architecture.Features.Chat.Application;

public sealed class SendMessageHandler : IRequestHandler<SendMessage.Command, OneOf<Success, SharedProblemDetails>>
{
  private readonly IChatHubService ChatHubService;

  public SendMessageHandler(IChatHubService chatHubClients)
  {
    ChatHubService = chatHubClients;
  }

  public async Task<OneOf<Success, SharedProblemDetails>> Handle(SendMessage.Command request, CancellationToken cancellationToken)
  {
    try
    {
      await ChatHubService.SendMessageToAll(request.User, request.Message, cancellationToken: cancellationToken);
      return new Success();
    }
    catch (Exception exception)
    {
      return new SharedProblemDetails
      {
        Title = "Failed to send message",
        Detail = exception.Message,
        Status = 500
      };
    }
  }
}
