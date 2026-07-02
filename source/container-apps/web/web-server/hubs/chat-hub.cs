#region Purpose
// SignalR hub for chat; routes client invocations into the mediator pipeline.
#endregion

#region Design
// Same thin-shim rule as HTTP endpoints: the hub holds no logic so validation and handling stay
// in the pipeline shared with REST.
// The OneOf result is flattened into SignalrResult because a discriminated union cannot be
// serialized over SignalR — IsSuccess plus two nullable slots is the wire-safe equivalent the
// Spa client unwraps.
// Mapped at ChatHubConstants.Route so the URL is owned by Web.Contracts.
#endregion

namespace TimeWarp.Architecture.Hubs;

public class ChatHub : Hub
{
  private readonly ISender Sender;

  public ChatHub(ISender sender)
  {
    Sender = sender;
  }

  public async Task<SignalrResult<Success, SharedProblemDetails>> SendMessage(SendMessage.Command sendMessageCommand)
  {
    OneOf<Success, SharedProblemDetails> result = await Sender.Send(sendMessageCommand);

    if (result.IsT0)
    {
      return new SignalrResult<Success, SharedProblemDetails> { IsSuccess = true, Success = result.AsT0 };
    }
    else
    {
      return new SignalrResult<Success, SharedProblemDetails> { IsSuccess = false, Failure = result.AsT1 };
    }
  }

  // Add more methods for handling other chat interactions as required

}
