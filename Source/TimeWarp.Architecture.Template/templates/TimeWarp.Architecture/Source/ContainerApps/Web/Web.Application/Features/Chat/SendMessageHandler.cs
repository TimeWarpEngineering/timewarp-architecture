namespace TimeWarp.Architecture.Features.Chat.Application;

public sealed class SendMessageHandler : IRequestHandler<Contracts.SendMessage.Command, OneOf<Success, SharedProblemDetails>>
{
  private readonly IChatHubClients ChatHubClients;

  public SendMessageHandler(IChatHubClients chatHubClients)
  {
    ChatHubClients = chatHubClients;
  }

  public async Task<OneOf<Success, SharedProblemDetails>> Handle(Contracts.SendMessage.Command request, CancellationToken cancellationToken)
  {
    try
    {
      await ChatHubClients.All.SendAsync("ReceiveMessage", request.User, request.Message, cancellationToken: cancellationToken);
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
