#region Purpose
// OpenAPI states when a GET is anonymous, and keeps 401/403 on the protected twins and the Entra challenge.
#endregion

#region Design
// Task 437: GET profile and GET assistant settings are AllowAnonymous on purpose, so FastEndpoints
// omits 401/403. The operation description must carry the contract reason or the spec looks unfinished.
// The same sentence covers the other public GETs in the audit. Entra challenge stays anonymous at the
// gate (bootstrap has no session) but the handler returns 401 and 403, so those statuses are listed.
// A tiny FastEndpoints host discovers the real web-server endpoint types. It does not boot the
// mediator graph; OpenAPI reads Configure() only.
#endregion

namespace OpenApiAnonymousIntent_;

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
  private static readonly HashSet<string> EndpointNames = new(StringComparer.Ordinal)
  {
    "GetProfileEndpoint",
    "UpdateProfileEndpoint",
    "GetAssistantIdentityEndpoint",
    "UpdateAssistantNameEndpoint",
    "HelloEndpoint",
    "GetEntraBootstrapChoiceEndpoint",
    "GetCurrentSessionEndpoint",
    "GetSignInMethodsEndpoint",
    "SubmitTipEndpoint",
    "ChallengeEntraEndpoint",
    "SignOutAntiforgeryTokenEndpoint",
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
        EnvironmentName = Environments.Development,
      }
    );
    builder.WebHost.UseTestServer();
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

  public static async Task Profile_Get_Should_State_Anonymous_Intent()
  {
    JsonElement operation = await OperationAsync("/api/Users/Current/Profile", "get");
    AssertAnonymous(operation, "dual-mode");
  }

  public static async Task Profile_Put_Should_Document_Unauthorized_And_Forbidden()
  {
    JsonElement operation = await OperationAsync("/api/Users/Current/Profile", "put");
    AssertProtected(operation);
  }

  public static async Task Assistant_Get_Should_State_Anonymous_Intent()
  {
    JsonElement operation = await OperationAsync("/api/settings/assistant", "get");
    AssertAnonymous(operation, "Ask button");
  }

  public static async Task Assistant_Put_Should_Document_Unauthorized_And_Forbidden()
  {
    JsonElement operation = await OperationAsync("/api/settings/assistant", "put");
    AssertProtected(operation);
  }

  public static async Task Audited_Public_Gets_Should_State_Anonymous_Intent()
  {
    JsonElement document = await DocumentRootAsync();
    AssertAnonymous(Operation(document, "/api/Hello", "get"), "no security surface");
    AssertAnonymous(Operation(document, "/api/identity/entra/choice", "get"), "no session yet");
    AssertAnonymous(Operation(document, "/api/identity/session", "get"), "IsAuthenticated=false");
    AssertAnonymous(Operation(document, "/api/identity/sign-in-methods", "get"), "sign-in methods");
    AssertAnonymous(Operation(document, "/api/tip", "get"), "tip jar");
    AssertAnonymous
    (
      Operation(document, "/identity/sign-out/antiforgery-token", "get"),
      "antiforgery token"
    );
  }

  public static async Task Entra_Challenge_Should_Document_Unauthorized_And_Forbidden()
  {
    JsonElement operation = await OperationAsync("/api/identity/entra/challenge", "get");
    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain("Intentionally anonymous.");
    description.ShouldContain("401");
    JsonElement responses = operation.GetProperty("responses");
    responses.TryGetProperty("401", out _).ShouldBeTrue();
    responses.TryGetProperty("403", out _).ShouldBeTrue();
    string unauthorized = responses.GetProperty("401").GetProperty("description").GetString()!;
    string forbidden = responses.GetProperty("403").GetProperty("description").GetString()!;
    unauthorized.ShouldContain("identity session");
    forbidden.ShouldContain("disabled");
  }

  private static void AssertAnonymous(JsonElement operation, string reasonFragment)
  {
    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain("Intentionally anonymous.");
    description.ShouldContain(reasonFragment);
    JsonElement responses = operation.GetProperty("responses");
    responses.TryGetProperty("401", out _).ShouldBeFalse();
    responses.TryGetProperty("403", out _).ShouldBeFalse();
  }

  private static void AssertProtected(JsonElement operation)
  {
    JsonElement responses = operation.GetProperty("responses");
    responses.TryGetProperty("401", out _).ShouldBeTrue();
    responses.TryGetProperty("403", out _).ShouldBeTrue();
    if (operation.TryGetProperty("description", out JsonElement description))
    {
      string text = description.GetString() ?? string.Empty;
      text.ShouldNotContain("Intentionally anonymous.");
    }
  }

  private static async Task<JsonElement> OperationAsync(string path, string method)
  {
    JsonElement document = await DocumentRootAsync();
    return Operation(document, path, method);
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
