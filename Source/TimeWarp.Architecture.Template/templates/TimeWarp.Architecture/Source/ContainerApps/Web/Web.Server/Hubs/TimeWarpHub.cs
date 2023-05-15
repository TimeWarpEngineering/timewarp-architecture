namespace TimeWarp.Architecture.Hubs;

public class TimeWarpHub : Hub
{
  private readonly ISender Sender;

  public TimeWarpHub(ISender sender)
  {
    Sender = sender;
  }

  public async Task<SignalrResult<Success, SharedProblemDetails>> SendMessage(Features.Chat.Contracts.SendMessage.Command sendMessageCommand)
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

  // Add more methods for handling other interactions
  //public async Task<OneOf<SignIn.Response, SharedProblemDetails>> SignInCommand(SignIn.Command signInCommand)
  //{
  //  return await Sender.Send(signInCommand);
  //}
}
