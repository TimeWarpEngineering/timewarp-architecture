namespace TrackEventHandler_;

using static TimeWarp.Architecture.Features.Analytics.Contracts.TrackEvent;

public class Handle_Returns
{
  private readonly Command Command;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Handle_Returns
  (
     WebTestServerApplication aWebTestServerApplication
  )
  {
    Command = new Command { EventName = "SomeEvent" };
    WebTestServerApplication = aWebTestServerApplication;
  }

  public async Task Ok_Given_Valid_Request()
  {
    OneOf<Response, SharedProblemDetails> result = await WebTestServerApplication.Send(Command);

    ValidateResult(result);
  }

  private void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch(
        response =>
        {
          response.Should().NotBeNull();
        },
        problemDetails =>
        {
          // This should not happen in a successful case
          Execute.Assertion.FailWith("The SignIn handler returned SharedProblemDetails instead of a successful response.");
        }
    );
  }
}
