namespace Hello_Handler;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public class Handle_Returns
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Handle_Returns>();

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

  private static Query CreateQuery() => new() { Name = "SomeEvent" };

  public static async Task Ok_Given_Valid_Request()
  {
    OneOf<Response, SharedProblemDetails> result = await Web.Send(CreateQuery());

    ValidateResult(result);
  }

  private static void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch(
        response => response.ShouldNotBeNull(),
        problemDetails =>
        {
          // This should not happen in a successful case
          problemDetails.ShouldBeNull("The SignIn handler returned SharedProblemDetails instead of a successful response.");
        }
    );
  }

}
