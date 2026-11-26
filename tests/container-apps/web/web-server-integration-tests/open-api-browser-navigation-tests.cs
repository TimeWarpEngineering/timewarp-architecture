#region Purpose
// The OpenAPI operations for sign-out, its antiforgery token, and the Entra challenge state the wire contract.
#endregion

#region Design
// Task 458-007: these hand-written FastEndpoints are documented, not excluded. FastEndpoints'
// default 204 is gone; each operation carries the statuses, headers, parameters, and body the
// handler uses. A tiny FastEndpoints host discovers the three real endpoint types; OpenAPI reads
// Configure() only. The form field name comes from the host's AntiforgeryOptions, so the test
// reads the same options instead of spelling the default.
#endregion

namespace OpenApiBrowserNavigation_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Document_Given_
{
  private static readonly HashSet<string> EndpointNames = new(StringComparer.Ordinal)
  {
    nameof(SignOutBrowserSessionEndpoint),
    nameof(SignOutAntiforgeryTokenEndpoint),
    nameof(ChallengeEntraEndpoint),
  };

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Document_Given_>();

  public static async Task SetupOnce()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Production,
      }
    );
    builder.WebHost.UseTestServer();
    builder.Services.AddAntiforgery();
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(Program).Assembly];
      options.Filter = type => EndpointNames.Contains(type.Name);
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

  public static async Task SignOut_Should_Document_Redirect_BadRequest_And_Form_Field()
  {
    JsonElement root = await DocumentRootAsync();
    JsonElement operation = Operation(root, SignOutBrowserSession.Path, "post");
    JsonElement responses = operation.GetProperty("responses");

    responses.TryGetProperty("204", out _).ShouldBeFalse();
    responses.TryGetProperty("400", out _).ShouldBeTrue();
    AssertLocation(responses.GetProperty("303"), SignOutBrowserSession.RedirectPath);

    string fieldName = App!.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.FormFieldName;
    JsonElement body = operation.GetProperty("requestBody");
    body.GetProperty("required").GetBoolean().ShouldBeTrue();
    JsonElement content = body.GetProperty("content");
    content.EnumerateObject().Select(static item => item.Name).ShouldBe(["application/x-www-form-urlencoded"]);
    JsonElement schema = content.GetProperty("application/x-www-form-urlencoded").GetProperty("schema");
    schema.GetProperty("properties").TryGetProperty(fieldName, out _).ShouldBeTrue();
    schema.GetProperty("required").EnumerateArray().Select(static item => item.GetString()).ShouldContain(fieldName);
  }

  public static async Task AntiforgeryToken_Should_Document_The_Json_Token_Body()
  {
    JsonElement root = await DocumentRootAsync();
    JsonElement responses = Operation(root, SignOutBrowserSession.AntiforgeryTokenPath, "get").GetProperty("responses");

    responses.TryGetProperty("204", out _).ShouldBeFalse();
    JsonElement schema = responses.GetProperty("200").GetProperty("content")
      .GetProperty("application/json").GetProperty("schema");
    JsonElement properties = Resolve(root, schema).GetProperty("properties");
    properties.TryGetProperty("formFieldName", out _).ShouldBeTrue();
    properties.TryGetProperty("requestToken", out _).ShouldBeTrue();
  }

  public static async Task EntraChallenge_Should_Document_Mode_And_Redirect()
  {
    JsonElement root = await DocumentRootAsync();
    JsonElement operation = Operation(root, ChallengeEntra.Path, "get");
    JsonElement responses = operation.GetProperty("responses");

    responses.TryGetProperty("204", out _).ShouldBeFalse();
    AssertLocation(responses.GetProperty("302"), expectedExample: null);

    JsonElement mode = Parameter(operation, "mode");
    mode.GetProperty("in").GetString().ShouldBe("query");
    mode.GetProperty("required").GetBoolean().ShouldBeTrue();
    mode.GetProperty("schema").GetProperty("enum").EnumerateArray().Select(static item => item.GetString())
      .ShouldBe([EntraTicketProcessor.ModeLink, EntraTicketProcessor.ModeBootstrap]);

    JsonElement returnUrl = Parameter(operation, "returnUrl");
    returnUrl.GetProperty("in").GetString().ShouldBe("query");
    bool required = returnUrl.TryGetProperty("required", out JsonElement value) && value.GetBoolean();
    required.ShouldBeFalse();
  }

  private static void AssertLocation(JsonElement response, string? expectedExample)
  {
    JsonElement location = response.GetProperty("headers").GetProperty("Location");
    location.GetProperty("schema").GetProperty("type").GetString().ShouldBe("string");
    if (expectedExample is not null)
    {
      location.GetProperty("example").GetString().ShouldBe(expectedExample);
    }
  }

  private static JsonElement Parameter(JsonElement operation, string name)
  {
    return operation.GetProperty("parameters").EnumerateArray()
      .Single(parameter => parameter.GetProperty("name").GetString() == name);
  }

  private static JsonElement Resolve(JsonElement root, JsonElement schema)
  {
    if (!schema.TryGetProperty("$ref", out JsonElement reference))
    {
      return schema;
    }

    string id = reference.GetString()!["#/components/schemas/".Length..];
    return root.GetProperty("components").GetProperty("schemas").GetProperty(id);
  }

  private static JsonElement Operation(JsonElement root, string path, string method)
  {
    return root.GetProperty("paths").GetProperty(path).GetProperty(method);
  }

  private static async Task<JsonElement> DocumentRootAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    string json = await response.Content.ReadAsStringAsync();
    response.StatusCode.ShouldBe(HttpStatusCode.OK, json);
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }
}
