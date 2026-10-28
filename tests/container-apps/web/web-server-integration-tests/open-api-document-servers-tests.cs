#region Purpose
// The OpenAPI document advertises the forwarded public origin, and Scalar does not pin a loopback server.
#endregion

#region Design
// A tiny host, not the full web graph: CommonServerModule is the only OpenAPI wiring, which is what
// serves /openapi/v1.json. Microsoft.AspNetCore.OpenApi fills servers from the request host; the
// public-origin transformer must replace that when X-Forwarded-Proto and X-Forwarded-Host are set,
// and must leave the listening address alone when they are absent. Scalar's page must not embed its
// own servers list — the UI base URL comes from the document.
#endregion

namespace OpenApiDocument_;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Servers_Given_
{
  private const string PublicOrigin = "https://arch.timewarp.work/";

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Servers_Given_>();

  public static async Task SetupOnce()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Production,
      }
    );
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    CommonServerModule.AddOpenApi(builder.Services, "v1", "Test API");
    App = builder.Build();
    CommonServerModule.UseScalarApiReference(App, "v1", "Test API");
    await App.StartAsync();

    IServer server = App.Services.GetRequiredService<IServer>();
    IServerAddressesFeature? addressesFeature = server.Features.Get<IServerAddressesFeature>();
    string? address = addressesFeature?.Addresses.SingleOrDefault(static candidate => candidate.StartsWith("http://", StringComparison.Ordinal));
    if (string.IsNullOrEmpty(address))
    {
      throw new InvalidOperationException("Kestrel did not report an http listen address.");
    }

    Client = new HttpClient { BaseAddress = new Uri(address) };
  }

  public static async Task CleanUpOnce()
  {
    Client?.Dispose();
    Client = null;
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  public static async Task ForwardedPublicOrigin_Should_ReplaceTheListenServer()
  {
    using HttpRequestMessage request = DocumentRequest();
    request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
    request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "arch.timewarp.work, internal.local");

    string serverUrl = await ReadServerUrlAsync(request);

    serverUrl.ShouldBe(PublicOrigin);
  }

  public static async Task NoForwardedHeaders_Should_KeepTheListenAddress()
  {
    string serverUrl = await ReadServerUrlAsync(DocumentRequest());

    serverUrl.ShouldStartWith("http://127.0.0.1:");
    serverUrl.ShouldNotContain("arch.timewarp.work");
  }

  public static async Task ScalarPage_Should_NotEmbedALoopbackServer()
  {
    using HttpRequestMessage request = new(HttpMethod.Get, "/scalar/v1");
    request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
    request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "arch.timewarp.work");

    using HttpResponseMessage response = await Client!.SendAsync(request);
    string html = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    // Scalar trims the leading slash; the page points at the document and does not override servers.
    html.ShouldContain("openapi/v1.json");
    html.ShouldNotContain("localhost:63621");
    html.ShouldNotContain("127.0.0.1");
    html.ShouldNotContain("\"servers\"");
  }

  private static HttpRequestMessage DocumentRequest() => new(HttpMethod.Get, "/openapi/v1.json");

  private static async Task<string> ReadServerUrlAsync(HttpRequestMessage request)
  {
    using HttpResponseMessage response = await Client!.SendAsync(request);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString()!;
  }
}
