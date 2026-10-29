#region Purpose
// Every web-server OpenAPI operation carries a tag, so Scalar and client codegen can group it.
#endregion

#region Design
// Task 441: the generator emits Description.WithTags from the namespace leaf, but hand-written
// endpoints set their own tag, and five shipped without one. Scanning the whole document catches
// the next hand-written endpoint that forgets. A tiny FastEndpoints host discovers every endpoint
// type in the web-server assembly. It does not boot the mediator graph; OpenAPI reads Configure() only.
#endregion

namespace OpenApiOperationTags_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Document_Given_
{
  private static readonly string[] Methods = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

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
        EnvironmentName = Environments.Development,
      }
    );
    builder.WebHost.UseTestServer();
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(Program).Assembly];
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

  public static async Task Every_Operation_Should_Have_A_Tag()
  {
    JsonElement document = await DocumentRootAsync();
    List<string> untagged = [];
    int operationCount = 0;
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (string method in Methods)
      {
        if (!path.Value.TryGetProperty(method, out JsonElement operation))
        {
          continue;
        }

        operationCount++;
        if (Tags(operation).Count == 0)
        {
          untagged.Add($"{method.ToUpperInvariant()} {path.Name}");
        }
      }
    }

    operationCount.ShouldBeGreaterThan(0);
    untagged.ShouldBeEmpty($"Operations without OpenAPI tags: {string.Join(", ", untagged)}");
  }

  public static async Task Hand_Written_Endpoints_Should_Use_Their_Feature_Tag()
  {
    JsonElement document = await DocumentRootAsync();
    Tags(Operation(document, "/api/Feedback/attachments", "post")).ShouldBe(["Feedback"]);
    Tags(Operation(document, "/api/Feedback/attachments/{attachmentId}", "get")).ShouldBe(["Feedback"]);
    Tags(Operation(document, "/api/identity/entra/challenge", "get")).ShouldBe(["Identity"]);
    Tags(Operation(document, "/identity/sign-out", "post")).ShouldBe(["Identity"]);
    Tags(Operation(document, "/identity/sign-out/antiforgery-token", "get")).ShouldBe(["Identity"]);
  }

  private static List<string> Tags(JsonElement operation)
  {
    if (!operation.TryGetProperty("tags", out JsonElement tags))
    {
      return [];
    }

    return tags.EnumerateArray()
      .Select(static tag => tag.GetString() ?? string.Empty)
      .Where(static tag => tag.Length > 0)
      .ToList();
  }

  private static JsonElement Operation(JsonElement document, string path, string method)
  {
    JsonElement paths = document.GetProperty("paths");
    foreach (JsonProperty item in paths.EnumerateObject())
    {
      if (string.Equals(item.Name, path, StringComparison.OrdinalIgnoreCase)
        && item.Value.TryGetProperty(method, out JsonElement operation))
      {
        return operation;
      }
    }

    string known = string.Join(", ", paths.EnumerateObject().Select(static item => item.Name));
    throw new InvalidOperationException($"Missing {method} {path}. Paths: {known}");
  }

  private static async Task<JsonElement> DocumentRootAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }
}
