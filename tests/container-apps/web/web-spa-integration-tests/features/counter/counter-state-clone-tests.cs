#region Purpose
// CounterState.Clone identity and value semantics under the SPA test host.
#endregion

namespace CounterState_;

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
    CounterState counterState = scope.Store.GetState<CounterState>();

    counterState.Initialize(count: 15);

    var clone = counterState.Clone() as CounterState;

    counterState.ShouldNotBeSameAs(clone);
    counterState.Count.ShouldBe(clone!.Count);
    counterState.Guid.ShouldNotBe(clone.Guid);
    return Task.CompletedTask;
  }
}
