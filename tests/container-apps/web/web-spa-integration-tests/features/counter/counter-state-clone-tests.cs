#region Purpose
// CounterState.Clone identity and value semantics under the SPA test host.
#endregion

namespace CounterState_;

using global::Aspire.Hosting;

[TestTag("Integration")]
public class Clone_Should
{
  private static DistributedApplication? App;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Clone_Should>();

  public static async Task SetupOnce()
  {
    App = await SpaIntegrationHost.StartAsync();
    Spa = new AspireSpaTestApplication(App);
  }

  public static async Task CleanUpOnce()
  {
    await SpaIntegrationHost.StopAsync(App);
    App = null;
    Spa = null;
  }

  public static Task Clone()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    CounterState counterState = scope.Store.GetState<CounterState>();

    counterState.Initialize(count: 15);

    var clone = counterState.Clone() as CounterState;

    counterState.ShouldNotBeSameAs(clone);
    counterState.Count.ShouldBe(clone!.Count);
    counterState.Guid.ShouldNotBe(clone.Guid);
    return Task.CompletedTask;
  }
}
