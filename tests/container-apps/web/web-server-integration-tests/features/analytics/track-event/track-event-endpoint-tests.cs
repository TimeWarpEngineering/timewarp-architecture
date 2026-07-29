namespace TrackEventEndpoint_;

using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public class Returns_
{
  private readonly Command Command;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_
  (
    WebTestServerApplication webTestServerApplication
  )
  {
    Command = new Command { EventName = "MyEvent" };
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Ok_Given_SomeEvent()
  {
    OneOf<Response, SharedProblemDetails> result = await WebTestServerApplication.Send(Command);

    ValidateResult(result);
  }

  public async Task ValidationError()
  {
    Command.EventName = "";

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>(Command, nameof(Command.EventName));
  }

  private void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch
    (
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
