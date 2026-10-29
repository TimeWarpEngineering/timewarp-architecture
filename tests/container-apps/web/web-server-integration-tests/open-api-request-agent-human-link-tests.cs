#region Purpose
// POST /api/agent-links publishes humanPrincipalId as a required, described uuid with an example.
#endregion

#region Design
// The handler links the calling agent to the human the body names. There is no self-link
// default, so omitting the id must read as required in the document, not as an optional field.
// The host is the OpenAPI pipeline only: FastEndpoints discovers the one generated endpoint, and
// CommonServerModule.AddOpenApi runs the document transformers. No mediator graph.
#endregion

namespace OpenApiRequestAgentHumanLink_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class RequestAgentHumanLink_Given_
{
  private const string EndpointName = "RequestAgentHumanLinkEndpoint";

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RequestAgentHumanLink_Given_>();

  public static async Task SetupOnce()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Development,
      }
    );
    builder.WebHost.UseTestServer();
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(Program).Assembly];
      options.Filter = type => string.Equals(type.Name, EndpointName, StringComparison.Ordinal);
    });
    CommonServerModule.AddOpenApi(builder.Services, "v1", "Test API");
    App = builder.Build();
    App.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });
    CommonServerModule.UseScalarApiReference(App, "v1", "Test API");
    await App.StartAsync();
    Client = App.GetTestClient();
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

  public static async Task HumanPrincipalId_Should_Be_Required()
  {
    JsonElement schema = await RequestSchemaAsync();
    schema.TryGetProperty("required", out JsonElement required).ShouldBeTrue();
    required.EnumerateArray().Select(static item => item.GetString()).ShouldContain("humanPrincipalId");
  }

  public static async Task HumanPrincipalId_Should_Be_A_Described_Uuid_With_An_Example()
  {
    JsonElement schema = await RequestSchemaAsync();
    JsonElement property = schema.GetProperty("properties").GetProperty("humanPrincipalId");

    property.GetProperty("format").GetString().ShouldBe("uuid");
    string description = property.GetProperty("description").GetString() ?? string.Empty;
    description.ShouldContain("human");
    description.ShouldContain("account id");
    property.TryGetProperty("example", out JsonElement example).ShouldBeTrue();
    Guid.TryParse(example.GetString(), out Guid exampleId).ShouldBeTrue();
    exampleId.ShouldNotBe(Guid.Empty);
  }

  private static async Task<JsonElement> RequestSchemaAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    JsonElement operation = root
      .GetProperty("paths")
      .GetProperty("/" + RequestAgentHumanLink.Command.RouteTemplate)
      .GetProperty("post");
    JsonElement schema = operation
      .GetProperty("requestBody")
      .GetProperty("content")
      .GetProperty("application/json")
      .GetProperty("schema");

    if (schema.TryGetProperty("$ref", out JsonElement reference))
    {
      string name = reference.GetString()!.Split('/')[^1];
      schema = root.GetProperty("components").GetProperty("schemas").GetProperty(name);
    }

    return schema.Clone();
  }
}
