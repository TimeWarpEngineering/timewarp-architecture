#region Purpose
// EventStreamState.Clone identity and value semantics under the SPA test host.
#endregion

namespace EventStreamState_;

[TestTag("Integration")]
public class Clone_Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Clone_Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    // Session-owned: the Jaribu session hook disposes SpaSessionFixture; do not dispose here.
    Session = null;
    Spa = null;
    return Task.CompletedTask;
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
