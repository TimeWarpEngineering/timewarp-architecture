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
