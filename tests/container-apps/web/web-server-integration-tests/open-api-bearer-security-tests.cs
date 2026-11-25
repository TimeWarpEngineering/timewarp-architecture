#region Purpose
// OpenAPI security for agent-token operations references JWTBearerAuth and names the scope.
#endregion

#region Design
// A tiny FastEndpoints host, not the full web graph. CommonServerModule.AddOpenApi is the
// document pipeline under test, with the same OpenApiAgentBearer web-server registers
// (seed inverse + AuthenticationSchemeNames.AgentToken). The seed walk reads every grant
// back from a probe operation, so a new permission with no probe, or the wrong scope,
// fails here. A domain 403
// description is kept and WWW-Authenticate stays optional. The host also passes the
// session-cookie registration: a cookie-only operation requires IdentitySessionCookie and no
// bearer, a dual operation lists the bearer then the session as alternatives, an operation
// with no AuthSchemes uses the session (the DefaultScheme), and an anonymous one publishes
// security: []. Scalar's page must name JWTBearerAuth so Authorize has a scheme to apply.
// The bearer 401 and 403 bodies reference the one SharedProblemDetails component (task 458-004).
#endregion

namespace OpenApiBearerSecurity_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Document_Given_
{
  private const string ProblemSchema = "#/components/schemas/TimeWarpFoundationTypesSharedProblemDetails";

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
      options.Assemblies = [typeof(AgentMeEndpoint).Assembly];
      options.Filter = type => type.Namespace == typeof(AgentMeEndpoint).Namespace
        && !type.IsAbstract
        && type.Name.EndsWith("Endpoint", StringComparison.Ordinal);
    });
    CommonServerModule.AddOpenApi
    (
      builder.Services,
      "v1",
      "Test API",
      OpenApiAgentBearerRegistration.Create(),
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

  public static async Task Scheme_Should_DescribeBearerFailures()
  {
    using JsonDocument document = await DocumentAsync();
    JsonElement scheme = document.RootElement
      .GetProperty("components")
      .GetProperty("securitySchemes")
      .GetProperty("JWTBearerAuth");

    scheme.GetProperty("type").GetString().ShouldBe("http");
    scheme.GetProperty("scheme").GetString().ShouldBe("bearer");
    string description = scheme.GetProperty("description").GetString()!;
    description.ShouldContain("identity:read");
    description.ShouldContain("credential:manage");
    description.ShouldContain("demo:invoke");
    description.ShouldContain("application/problem+json");
    description.ShouldContain("invalid_token");
    description.ShouldContain("insufficient_scope");
  }

  public static async Task Document_Should_NotRequireBearerGlobally()
  {
    using JsonDocument document = await DocumentAsync();
    document.RootElement.TryGetProperty("security", out _).ShouldBeFalse();
  }

  public static async Task AgentMe_Should_RequireIdentityRead()
  {
    JsonElement operation = await OperationAsync("/api/identity/agent/me", "get");
    AssertBearer(operation, "identity:read");
    operation.GetProperty("description").GetString()!.ShouldContain("identity:read");
  }

  public static void Registration_Should_MatchTheSeedAndTheAgentTokenScheme()
  {
    OpenApiAgentBearer bearer = OpenApiAgentBearerRegistration.Create();
    bearer.SchemeName.ShouldBe(AuthenticationSchemeNames.AgentToken);
    Type? agentTokenDefaults = typeof(OpenApiAgentBearerRegistration).Assembly
      .GetType("TimeWarp.Architecture.Configuration.AgentTokenDefaults");
    string? scheme = agentTokenDefaults?.GetField("Scheme")?.GetValue(null) as string;
    scheme.ShouldBe(AuthenticationSchemeNames.AgentToken);
    foreach ((string scope, IReadOnlyList<string> permissions) in AgentScopePermissionSeed.DefaultGrants)
    {
      foreach (string permission in permissions)
      {
        bearer.PolicyScopes[permission].ShouldBe(scope);
      }
    }
  }

  public static async Task SeedGrant_Should_DocumentTheGrantingScope()
  {
    using JsonDocument document = await DocumentAsync();
    JsonElement paths = document.RootElement.GetProperty("paths");
    foreach ((string scope, IReadOnlyList<string> permissions) in AgentScopePermissionSeed.DefaultGrants)
    {
      foreach (string permission in permissions)
      {
        ProbePaths.TryGetValue(permission, out string? path).ShouldBeTrue(
          $"Add an agent-token probe for permission {permission}.");
        paths.TryGetProperty(path!, out JsonElement pathItem).ShouldBeTrue(path);
        JsonElement scopes = pathItem.GetProperty("get").GetProperty("security")[0].GetProperty("JWTBearerAuth");
        scopes.GetArrayLength().ShouldBe(1);
        scopes[0].GetString().ShouldBe(scope);
      }
    }
  }

  public static async Task DomainForbidden_Should_KeepTheHandlerDetail()
  {
    JsonElement operation = await OperationAsync(DomainForbiddenEndpoint.Route, "get");
    AssertBearer(operation, "identity:read");
    string forbidden = operation.GetProperty("responses").GetProperty("403").GetProperty("description").GetString()!;
    forbidden.ShouldContain(DomainForbiddenEndpoint.DomainDetail);
    forbidden.ShouldContain("handler-level");
    JsonElement header = operation.GetProperty("responses").GetProperty("403").GetProperty("headers").GetProperty("WWW-Authenticate");
    AssertHeaderOptional(header);
    header.GetProperty("description").GetString()!.ShouldContain("only when");
  }

  public static async Task AgentLinks_Should_AcceptSessionOrIdentityRead()
  {
    JsonElement operation = await OperationAsync("/api/agent-links", "get");
    AssertBearer(operation, "identity:read");
    AssertSessionAlternative(operation, "identity:read");
  }

  public static async Task Credentials_Should_AcceptSessionOrCredentialManage()
  {
    JsonElement operation = await OperationAsync("/api/identity/credentials", "get");
    AssertBearer(operation, "credential:manage");
    AssertSessionAlternative(operation, "credential:manage");
  }

  public static async Task Metered_Should_RequireDemoInvoke()
  {
    JsonElement operation = await OperationAsync("/api/demo/metered-capability", "get");
    AssertBearer(operation, "demo:invoke");
  }

  public static async Task FeedbackRead_Should_AcceptSessionOrFeedbackRead()
  {
    JsonElement operation = await OperationAsync("/api/feedback", "get");
    AssertBearer(operation, "feedback:read");
    AssertSessionAlternative(operation, "feedback:read");
  }

  public static async Task AgentOnly_Should_ListOnlyTheBearer()
  {
    JsonElement operation = await OperationAsync("/api/identity/agent/me", "get");
    operation.GetProperty("security").GetArrayLength().ShouldBe(1);
    operation.GetProperty("description").GetString()!
      .ShouldContain("Requires an agent bearer with scope identity:read (JWTBearerAuth).");
  }

  public static async Task UnmappedAgentPolicy_Should_RequireBearerWithoutAScope()
  {
    JsonElement operation = await OperationAsync("/api/settings/site", "put");
    JsonElement scopes = operation.GetProperty("security")[0].GetProperty("JWTBearerAuth");
    scopes.GetArrayLength().ShouldBe(0);
    operation.GetProperty("description").GetString()!.ShouldContain("JWTBearerAuth");
    AssertProblemResponses(operation);
  }

  public static async Task Anonymous_Should_PublishEmptySecurity()
  {
    JsonElement operation = await OperationAsync("/api/hello", "get");
    operation.GetProperty("security").GetArrayLength().ShouldBe(0);
    operation.TryGetProperty("responses", out JsonElement responses).ShouldBeTrue();
    responses.TryGetProperty("401", out _).ShouldBeFalse();
  }

  public static async Task CookieOnly_Should_RequireTheSessionCookie()
  {
    JsonElement operation = await OperationAsync("/api/profile", "put");
    AssertSessionOnly(operation);
  }

  public static async Task NoAuthSchemes_Should_RequireTheDefaultSession()
  {
    JsonElement operation = await OperationAsync("/api/default-scheme", "get");
    AssertSessionOnly(operation);
  }

  public static async Task ScalarPage_Should_NameJwtBearerAuth()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/scalar/v1");
    string html = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    html.ShouldContain("JWTBearerAuth");
  }

  private static void AssertBearer(JsonElement operation, string scope)
  {
    JsonElement scopes = operation.GetProperty("security")[0].GetProperty("JWTBearerAuth");
    operation.GetProperty("security")[0].TryGetProperty("IdentitySessionCookie", out _).ShouldBeFalse();
    scopes.GetArrayLength().ShouldBe(1);
    scopes[0].GetString().ShouldBe(scope);
    AssertProblemResponses(operation);
  }

  private static void AssertSessionOnly(JsonElement operation)
  {
    JsonElement security = operation.GetProperty("security");
    security.GetArrayLength().ShouldBe(1);
    security[0].TryGetProperty("JWTBearerAuth", out _).ShouldBeFalse();
    security[0].GetProperty("IdentitySessionCookie").GetArrayLength().ShouldBe(0);
    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain("Requires the browser session cookie (IdentitySessionCookie).");
    description.ShouldNotContain("JWTBearerAuth");
  }

  private static void AssertSessionAlternative(JsonElement operation, string scope)
  {
    JsonElement security = operation.GetProperty("security");
    security.GetArrayLength().ShouldBe(2);
    security[1].TryGetProperty("JWTBearerAuth", out _).ShouldBeFalse();
    security[1].GetProperty("IdentitySessionCookie").GetArrayLength().ShouldBe(0);
    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain(
      $"Accepts the browser session cookie (IdentitySessionCookie) or an agent bearer with scope {scope} (JWTBearerAuth).");
    description.ShouldNotContain("Requires");
  }

  private static void AssertProblemResponses(JsonElement operation)
  {
    JsonElement unauthorized = operation.GetProperty("responses").GetProperty("401");
    ProblemRef(unauthorized).ShouldBe(ProblemSchema);
    string challenge = unauthorized.GetProperty("headers").GetProperty("WWW-Authenticate").GetProperty("description").GetString()!;
    challenge.ShouldContain("invalid_token");
    challenge.ShouldContain("Bearer");

    JsonElement forbidden = operation.GetProperty("responses").GetProperty("403");
    forbidden.GetProperty("description").GetString()!.ShouldContain("handler-level");
    ProblemRef(forbidden).ShouldBe(ProblemSchema);
    JsonElement forbiddenHeader = forbidden.GetProperty("headers").GetProperty("WWW-Authenticate");
    forbiddenHeader.GetProperty("description").GetString()!.ShouldContain("insufficient_scope");
    AssertHeaderOptional(forbiddenHeader);
    AssertHeaderOptional(unauthorized.GetProperty("headers").GetProperty("WWW-Authenticate"));
  }

  private static string? ProblemRef(JsonElement response) =>
    response.GetProperty("content").GetProperty("application/problem+json").GetProperty("schema")
      .GetProperty("$ref").GetString();

  private static void AssertHeaderOptional(JsonElement header)
  {
    if (header.TryGetProperty("required", out JsonElement required))
    {
      required.GetBoolean().ShouldBeFalse();
    }
  }

  private static async Task<JsonElement> OperationAsync(string path, string method)
  {
    using JsonDocument document = await DocumentAsync();
    return document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method).Clone();
  }

  private static async Task<JsonDocument> DocumentAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    return JsonDocument.Parse(json);
  }
}
