#region Purpose
// OpenAPI security for agent-token operations references JWTBearerAuth and names the scope.
#endregion

#region Design
// A tiny FastEndpoints host, not the full web graph. CommonServerModule.AddOpenApi is the
// document pipeline under test, with the same OpenApiAgentBearer web-server registers
// (seed inverse + AuthenticationSchemeNames.AgentToken). The seed walk reads every grant
// back from a probe operation, so a new permission with no probe, or the wrong scope,
// fails here. A domain 403
// description is kept and WWW-Authenticate stays optional. Cookie-only and anonymous
// operations must stay without a bearer requirement. Scalar's page must name JWTBearerAuth
// so Authorize has a scheme to apply.
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
      OpenApiAgentBearerRegistration.Create()
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

  public static async Task AgentLinks_Should_RequireIdentityRead()
  {
    JsonElement operation = await OperationAsync("/api/agent-links", "get");
    AssertBearer(operation, "identity:read");
  }

  public static async Task Credentials_Should_RequireCredentialManage()
  {
    JsonElement operation = await OperationAsync("/api/identity/credentials", "get");
    AssertBearer(operation, "credential:manage");
  }

  public static async Task Metered_Should_RequireDemoInvoke()
  {
    JsonElement operation = await OperationAsync("/api/demo/metered-capability", "get");
    AssertBearer(operation, "demo:invoke");
  }

  public static async Task FeedbackRead_Should_RequireFeedbackRead()
  {
    JsonElement operation = await OperationAsync("/api/feedback", "get");
    AssertBearer(operation, "feedback:read");
  }

  public static async Task UnmappedAgentPolicy_Should_RequireBearerWithoutAScope()
  {
    JsonElement operation = await OperationAsync("/api/settings/site", "put");
    JsonElement scopes = operation.GetProperty("security")[0].GetProperty("JWTBearerAuth");
    scopes.GetArrayLength().ShouldBe(0);
    operation.GetProperty("description").GetString()!.ShouldContain("JWTBearerAuth");
    AssertProblemResponses(operation);
  }

  public static async Task Anonymous_Should_OmitBearerSecurity()
  {
    JsonElement operation = await OperationAsync("/api/hello", "get");
    operation.TryGetProperty("security", out _).ShouldBeFalse();
    operation.TryGetProperty("responses", out JsonElement responses).ShouldBeTrue();
    responses.TryGetProperty("401", out _).ShouldBeFalse();
  }

  public static async Task CookieOnly_Should_OmitBearerSecurity()
  {
    JsonElement operation = await OperationAsync("/api/profile", "put");
    operation.TryGetProperty("security", out _).ShouldBeFalse();
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
    scopes.GetArrayLength().ShouldBe(1);
    scopes[0].GetString().ShouldBe(scope);
    AssertProblemResponses(operation);
  }

  private static void AssertProblemResponses(JsonElement operation)
  {
    JsonElement unauthorized = operation.GetProperty("responses").GetProperty("401");
    unauthorized.GetProperty("content").TryGetProperty("application/problem+json", out _).ShouldBeTrue();
    string challenge = unauthorized.GetProperty("headers").GetProperty("WWW-Authenticate").GetProperty("description").GetString()!;
    challenge.ShouldContain("invalid_token");
    challenge.ShouldContain("Bearer");

    JsonElement forbidden = operation.GetProperty("responses").GetProperty("403");
    forbidden.GetProperty("description").GetString()!.ShouldContain("handler-level");
    forbidden.GetProperty("content").TryGetProperty("application/problem+json", out _).ShouldBeTrue();
    JsonElement forbiddenHeader = forbidden.GetProperty("headers").GetProperty("WWW-Authenticate");
    forbiddenHeader.GetProperty("description").GetString()!.ShouldContain("insufficient_scope");
    AssertHeaderOptional(forbiddenHeader);
    AssertHeaderOptional(unauthorized.GetProperty("headers").GetProperty("WWW-Authenticate"));
  }

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

public sealed class AgentMeEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/identity/agent/me");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.IdentityRead);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class DomainForbiddenEndpoint : EndpointWithoutRequest
{
  internal const string Route = "/api/agent-links/human-ux";
  internal const string DomainDetail = "This link belongs to a different agent.";

  public override void Configure()
  {
    Get(Route);
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.AgentLinkManageSelf);
    Summary(summary => summary.Responses[403] = DomainDetail);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

internal static class ProbePaths
{
  private static readonly Dictionary<string, string> Paths = new(StringComparer.Ordinal)
  {
    [PermissionIds.IdentityRead] = "/api/identity/agent/me",
    [PermissionIds.AgentLinkManageSelf] = "/api/agent-links",
    [PermissionIds.CredentialManageSelf] = "/api/identity/credentials",
    [PermissionIds.DemoInvoke] = "/api/demo/metered-capability",
    [PermissionIds.FeedbackRead] = "/api/feedback",
    [PermissionIds.FeedbackNote] = "/api/feedback/notes",
    [PermissionIds.FeedbackSeverity] = "/api/feedback/severity",
    [PermissionIds.FeedbackStatus] = "/api/feedback/status",
  };

  internal static bool TryGetValue(string permission, out string? path) =>
    Paths.TryGetValue(permission, out path);
}

public sealed class AgentLinksEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/agent-links");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.AgentLinkManageSelf);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class CredentialsEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/identity/credentials");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.CredentialManageSelf);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class MeteredEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/demo/metered-capability");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.DemoInvoke);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackReadEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackRead);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackNoteEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback/notes");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackNote);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackSeverityEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback/severity");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackSeverity);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackStatusEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback/status");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackStatus);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class UnmappedAgentEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Put("/api/settings/site");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.SettingsWrite);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class PublicHelloEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/hello");
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class CookieProfileEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Put("/api/profile");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession);
    Policies(PermissionIds.ProfileWrite);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}
