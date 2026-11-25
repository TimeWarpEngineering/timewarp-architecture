#region Purpose
// Every web-server OpenAPI operation declares security, and every operation that documents 401 names a credential.
#endregion

#region Design
// Task 458-001: cookie routes answered 401 but published no security, so a generated client read
// them as anonymous. A tiny FastEndpoints host discovers every endpoint type in the web-server
// assembly and runs CommonServerModule.AddOpenApi with the same agent-bearer and session-cookie
// registrations web-server passes. It does not boot the mediator graph; OpenAPI reads Configure() only.
// Every operation must carry a security key (Redocly security-defined): a requirement list, or []
// on an anonymous verb. An operation that documents 401 must carry a non-empty list. The Entra
// challenge is the one allowlisted exception: it is anonymous at the gate (bootstrap has no
// session) and its handler still returns 401.
#endregion

namespace OpenApiOperationSecurity_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Document_Given_
{
  private static readonly string[] Methods = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

  private static readonly HashSet<string> AnonymousWith401 = new(StringComparer.OrdinalIgnoreCase)
  {
    "get /api/identity/entra/challenge",
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
    });
    CommonServerModule.AddOpenApi
    (
      builder.Services,
      "v1",
      "Test API",
      OpenApiAgentBearerRegistration.Create(),
      documentPublicWebRoutes: true,
      sessionCookie: OpenApiSessionCookieRegistration.Create()
    );
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

  public static async Task Every_Operation_Should_Declare_Security()
  {
    JsonElement document = await DocumentRootAsync();
    List<string> missing = [];
    int operationCount = 0;
    foreach ((string key, JsonElement operation) in Operations(document))
    {
      operationCount++;
      if (!operation.TryGetProperty("security", out _))
      {
        missing.Add(key);
      }
    }

    operationCount.ShouldBeGreaterThan(50);
    missing.ShouldBeEmpty("Operations without a security key: " + string.Join(", ", missing));
  }

  public static async Task Every_Operation_Documenting_401_Should_Require_A_Credential()
  {
    JsonElement document = await DocumentRootAsync();
    List<string> anonymous = [];
    int protectedCount = 0;
    foreach ((string key, JsonElement operation) in Operations(document))
    {
      if (!operation.TryGetProperty("responses", out JsonElement responses)
        || !responses.TryGetProperty("401", out _))
      {
        continue;
      }

      bool hasRequirement = operation.TryGetProperty("security", out JsonElement security)
        && security.GetArrayLength() > 0;
      if (hasRequirement)
      {
        protectedCount++;
      }
      else if (!AnonymousWith401.Contains(key))
      {
        anonymous.Add(key);
      }
    }

    protectedCount.ShouldBeGreaterThan(30);
    anonymous.ShouldBeEmpty("401 without a security requirement: " + string.Join(", ", anonymous));
  }

  public static async Task Allowlisted_Entra_Challenge_Should_Stay_Anonymous()
  {
    JsonElement document = await DocumentRootAsync();
    foreach (string key in AnonymousWith401)
    {
      string[] parts = key.Split(' ', 2);
      JsonElement operation = Operation(document, parts[1], parts[0]);
      operation.GetProperty("security").GetArrayLength().ShouldBe(0);
      operation.GetProperty("responses").TryGetProperty("401", out _).ShouldBeTrue();
    }
  }

  public static async Task Session_Scheme_Should_Be_The_Ceremony_Cookie()
  {
    JsonElement document = await DocumentRootAsync();
    JsonElement scheme = document
      .GetProperty("components")
      .GetProperty("securitySchemes")
      .GetProperty("IdentitySessionCookie");
    scheme.GetProperty("type").GetString().ShouldBe("apiKey");
    scheme.GetProperty("in").GetString().ShouldBe("cookie");
    scheme.GetProperty("name").GetString().ShouldBe(IdentitySessionDefaults.CookieName);
    string description = scheme.GetProperty("description").GetString()!;
    description.ShouldContain("passkey");
    description.ShouldContain("Entra");
    description.ShouldContain("cannot obtain");
  }

  public static async Task Cookie_Only_Route_Should_Require_The_Session()
  {
    JsonElement document = await DocumentRootAsync();
    JsonElement operation = Operation(document, "/api/admin/roles", "get");
    JsonElement security = operation.GetProperty("security");
    security.GetArrayLength().ShouldBe(1);
    SchemeNames(security[0]).ShouldBe(["IdentitySessionCookie"]);
    security[0].GetProperty("IdentitySessionCookie").GetArrayLength().ShouldBe(0);
    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain("Requires the browser session cookie (IdentitySessionCookie)");
    description.ShouldContain("not accepted");
  }

  public static async Task Dual_Route_Should_List_Session_And_Bearer_As_Alternatives()
  {
    JsonElement document = await DocumentRootAsync();
    JsonElement operation = Operation(document, "/api/feedback/attachments/{attachmentId}", "get");
    JsonElement security = operation.GetProperty("security");
    security.GetArrayLength().ShouldBe(2);
    SchemeNames(security[0]).ShouldBe(["JWTBearerAuth"]);
    SchemeNames(security[1]).ShouldBe(["IdentitySessionCookie"]);
    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain("Accepts the browser session cookie (IdentitySessionCookie) or an agent bearer");
    description.ShouldNotContain("Requires an agent bearer");
  }

  public static void Registration_Should_Match_The_Identity_Session_Scheme()
  {
    OpenApiSessionCookie session = OpenApiSessionCookieRegistration.Create();
    session.SchemeName.ShouldBe(AuthenticationSchemeNames.IdentitySession);
    session.SchemeName.ShouldBe(IdentitySessionDefaults.Scheme);
    session.CookieName.ShouldBe(IdentitySessionDefaults.CookieName);
    session.IsDefaultScheme.ShouldBeTrue();
  }

  private static List<string> SchemeNames(JsonElement requirement) =>
    [.. requirement.EnumerateObject().Select(static property => property.Name)];

  private static IEnumerable<(string Key, JsonElement Operation)> Operations(JsonElement document)
  {
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (string method in Methods)
      {
        if (path.Value.TryGetProperty(method, out JsonElement operation))
        {
          yield return ($"{method} {path.Name}", operation);
        }
      }
    }
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
