namespace WebTestServerApplication_;

[TestTag("WebTestServerApplication")]
public class Should
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should>();

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

  /// <summary>
  /// This will test that the injected Web can be created and disposed.
  /// </summary>
  public static Task Start_Without_Exception()
  {
    true.ShouldBeTrue();
    return Task.CompletedTask;
  }

  [Skip("This test runs forever to allow me to manually test if servers are running properly.  Normally needs to be skipped as it will never completed")]
  public static async Task RunForever()
  {
    await Task.Delay(int.MaxValue);
    Console.WriteLine("Will never get here");
  }

}
