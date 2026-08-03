#region Purpose
// ApplicationState.Clone identity and value semantics under the SPA test host.
#endregion

namespace ApplicationState_;

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
    ApplicationState applicationState = scope.Store.GetState<ApplicationState>();

    applicationState.Initialize(name: "TestName", logo: "SomeUrl", isMenuExpanded: false);

    ApplicationState clone = applicationState.Clone();

    applicationState.ShouldNotBeSameAs(clone);
    applicationState.Name.ShouldBe(clone.Name);
    applicationState.Logo.ShouldBe(clone.Logo);
    applicationState.IsMenuExpanded.ShouldBe(clone.IsMenuExpanded);
    applicationState.Guid.ShouldNotBe(clone.Guid);
    return Task.CompletedTask;
  }
}
