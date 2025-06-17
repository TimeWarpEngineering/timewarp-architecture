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
