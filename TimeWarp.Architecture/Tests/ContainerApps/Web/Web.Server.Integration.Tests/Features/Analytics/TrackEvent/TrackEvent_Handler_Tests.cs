namespace TrackEventHandler_;

using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public class Handle_Returns
{
  private readonly Command Command;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Handle_Returns
  (
     WebTestServerApplication webTestServerApplication
  )
  {
    Command = new Command { EventName = "SomeEvent" };
    WebTestServerApplication = webTestServerApplication;
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
          response.ShouldNotBeNull();
        },
        problemDetails =>
        {
          // This should not happen in a successful case
          problemDetails.ShouldBeNull("The SignIn handler returned SharedProblemDetails instead of a successful response.");
        }
    );
  }
}
