#region Purpose
// Regression (task 312): Web.Server's server-side named HttpClients resolve through Aspire service
// discovery when the deployment publishes http only — the shape Compose, Kubernetes and ACA emit.
#endregion

#region Design
// The host boots with HttpOnlyServiceEndpoints (Services:web-server:http:0 only, no https entry),
// exactly what a published target injects as services__web-server__http__0. Before task 312 the
// named client read services__web-server__https__0 by hand, so BaseAddress was null and every
// InteractiveServer call failed with "An invalid request URI was provided" — this class failed then.
// The call goes through the production client registration (https+http://web-server plus
// IdentitySessionCookieForwardingHandler), resolved from the host's own IHttpClientFactory, the
// same path a component takes during interactive-server render.
#endregion

namespace WebServerNamedClientServiceDiscovery_;

using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TimeWarp.Foundation.Configuration;

public class Resolves_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Resolves_>();

  public static async Task SetupOnce()
  {
    Graph = await HostGraphFactory.CreateWebAsync(serviceEndpoints: WebTestServerApplication.HttpOnlyServiceEndpoints);
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Web_Server_Client_Over_Http_When_Only_Http_Is_Published()
  {
    IHttpClientFactory httpClientFactory = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    using HttpClient webServerClient = httpClientFactory.CreateClient(ServiceNames.WebServiceName);

    using HttpResponseMessage response = await webServerClient.GetAsync("api/health");

    // The only configured endpoint is http, so a 200 means discovery resolved it. (The discovery
    // handler restores the logical https+http://web-server URI on RequestMessage afterwards.)
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }
}
