#region Purpose
// CommonServerModule maps no configuration dump, even when the host runs in Development.
#endregion

#region Design
// A tiny UseTestServer host, not the full web graph, so the test needs no Docker. The host runs
// in Development because that is where the removed route used to be mapped. The only shared
// call is CommonServerModule.ConfigureEndpoints, the hook api-server and web-server invoke.
#endregion

namespace CommonServerModuleEndpoints_;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Foundation;

[TestTag("Security")]
public class ConfigureEndpoints_Given_Development_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ConfigureEndpoints_Given_Development_>();

  public static async Task DebugConfig_Should_Return_NotFound()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Development,
      }
    );
    builder.WebHost.UseTestServer();
    WebApplication app = builder.Build();
    app.Environment.IsDevelopment().ShouldBeTrue();
    CommonServerModule.ConfigureEndpoints(app);
    await app.StartAsync();
    try
    {
      using HttpClient client = app.GetTestClient();
      using HttpResponseMessage response = await client.GetAsync("/api/debug-config");

      response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    finally
    {
      await app.DisposeAsync();
    }
  }
}
