#region Purpose
// ApplicationState.Clone identity and value semantics under the SPA test host.
#endregion

namespace ApplicationState_;

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
