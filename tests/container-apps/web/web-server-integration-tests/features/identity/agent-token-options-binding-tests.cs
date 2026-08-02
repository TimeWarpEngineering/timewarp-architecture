#region Purpose
// Regression pin (applying 104-003's round-1 finding M1 lesson from day one, not after a review
// catches it): AddFluentValidatedOptions binds configuration by the options TYPE NAME
// ("AgentTokenOptions") absent a [ConfigurationKey] attribute. This test fails if the appsettings
// section name ever drifts from that, because the test appsettings.json's
// AgentTokenOptions:TokenLifetimeMinutes value (20) differs from AgentTokenOptions's C#
// property-initializer default (15), so a defaults-mask-the-bug scenario cannot recur undetected.
#endregion

namespace AgentTokenOptionsBinding_;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;

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

  public static Task ConfiguredValue_Given_AppSettings_Overrides_The_CSharp_Default()

  {
    IOptions<AgentTokenOptions> options =
      Web.WebApplicationHost.ServiceProvider.GetRequiredService<IOptions<AgentTokenOptions>>();

    options.Value.TokenLifetimeMinutes.ShouldBe(20);

    return Task.CompletedTask;

  }

}
