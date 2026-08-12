namespace TrackEventEndpoint_;

using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public class Returns_
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  private static Command CreateValidCommand() => new() { EventName = "MyEvent" };

  public static async Task Ok_Given_SomeEvent()
  {

    Command command = CreateValidCommand();

    OneOf<Response, SharedProblemDetails> result = await Web.Send(command);

    ValidateResult(result);
  }

  public static async Task ValidationError()
  {

    Command command = CreateValidCommand();

    command.EventName = "";

    await Web.ConfirmEndpointValidationError<Response>(command, nameof(command.EventName));
  }

  private static void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch
    (
      response => response.ShouldNotBeNull(),
      problemDetails =>
      {
        // This should not happen in a successful case
        problemDetails.ShouldBeNull("The SignIn handler returned SharedProblemDetails instead of a successful response.");
      }
    );
  }

}
