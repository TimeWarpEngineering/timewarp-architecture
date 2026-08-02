#region Purpose
// EventStreamState.Clone identity and value semantics under the SPA test host.
#endregion

namespace EventStreamState_;

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
    EventStreamState eventStreamState = scope.Store.GetState<EventStreamState>();

    var events = new List<string> { "Event 1", "Event 2", "Event 3" };
    eventStreamState.Initialize(events);

    EventStreamState clone = eventStreamState.Clone();

    eventStreamState.Events.Count.ShouldBe(clone.Events.Count);
    eventStreamState.Guid.ShouldNotBe(clone.Guid);
    eventStreamState.Events[0].ShouldBe(clone.Events[0]);
    return Task.CompletedTask;
  }
}
