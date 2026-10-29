#region Purpose
// The public web OpenAPI document names health, the tip alias, and the excluded discovery documents.
#endregion

#region Design
// A tiny host, not the full web graph. CommonServerModule is the document pipeline. The opt-in
// flag is what web-server passes. A second host without the flag is the api-server case: those
// routes must stay absent. llms.txt and the discovery skill must name the same excluded paths
// the document lists, because agents read both.
#endregion

namespace OpenApiDiscoveryCoverage_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Document_Given_
{
  private static readonly string[] ExcludedPaths =
  [
    "/.well-known/mcp/server-card.json",
    "/.well-known/mcp.json",
    "/.well-known/agent-skills/index.json",
    "/.well-known/agent-card.json",
    "/.well-known/agent.json",
    "/.well-known/webmcp.json",
  ];

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
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(TipGetEndpoint).Assembly];
      options.Filter = type => type.Namespace == typeof(TipGetEndpoint).Namespace
        && type.Name.EndsWith("Endpoint", StringComparison.Ordinal);
    });
    CommonServerModule.AddOpenApi
    (
      builder.Services,
      "v1",
      "Test API",
      agentBearer: null,
      documentPublicWebRoutes: true
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

  public static async Task Health_Should_Be_A_Plaintext_Operation()
  {
    JsonElement operation = await OperationAsync("/api/health", "get");

    string description = operation.GetProperty("description").GetString()!;
    description.ShouldContain("text/plain");
    description.ShouldContain("PostgresDbContext");
    description.ShouldContain("/web-server/alive");
    JsonElement responses = operation.GetProperty("responses");
    responses.GetProperty("200").GetProperty("content").TryGetProperty("text/plain", out _).ShouldBeTrue();
    responses.TryGetProperty("503", out _).ShouldBeTrue();
    AssertAnonymous(operation);
  }

  public static async Task TipAlias_Should_Point_At_The_Canonical_Operation()
  {
    JsonElement get = await OperationAsync("/api", "get");
    JsonElement post = await OperationAsync("/api", "post");
    JsonElement canonical = await OperationAsync("/api/tip", "get");

    get.GetProperty("description").GetString()!.ShouldContain("rewrite to /api/tip");
    post.GetProperty("description").GetString()!.ShouldContain("rewrite to /api/tip");
    canonical.GetProperty("description").GetString()!.ShouldContain("rewrite to /api/tip");
    AssertAnonymous(get);
    AssertAnonymous(post);
  }

  public static async Task WellKnown_Documents_Should_Be_Excluded_And_Listed()
  {
    using JsonDocument document = await DocumentAsync();
    JsonElement root = document.RootElement;
    JsonElement paths = root.GetProperty("paths");
    foreach (string path in ExcludedPaths)
    {
      paths.TryGetProperty(path, out _).ShouldBeFalse();
    }

    string description = root.GetProperty("info").GetProperty("description").GetString()!;
    description.ShouldContain("## Routes outside this document");
    description.ShouldContain("/llms.txt");
    description.ShouldContain("/scalar/v1");
    description.ShouldContain("x-timewarp-excluded-routes");

    JsonElement excluded = root.GetProperty("x-timewarp-excluded-routes");
    HashSet<string> listed = [];
    foreach (JsonElement entry in excluded.EnumerateArray())
    {
      entry.GetProperty("method").GetString().ShouldBe("GET");
      entry.GetProperty("see").GetString().ShouldBe("/llms.txt");
      listed.Add(entry.GetProperty("path").GetString()!);
    }

    listed.SetEquals(ExcludedPaths).ShouldBeTrue(string.Join(", ", listed));
  }

  public static async Task ScalarPage_Should_Load_The_Document()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/scalar/v1");
    string html = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    html.ShouldContain("openapi/v1.json");
  }

  public static Task Discovery_Index_Should_Name_The_Same_Exclusion()
  {
    string root = FindRepoRoot();
    string llms = File.ReadAllText
    (
      Path.Combine
      (
        root,
        "source",
        "container-apps",
        "web",
        "projects",
        "web-spa",
        "wwwroot",
        "llms.txt"
      )
    );
    string skill = File.ReadAllText
    (
      Path.Combine
      (
        root,
        "source",
        "container-apps",
        "web",
        "projects",
        "web-spa",
        "wwwroot",
        ".well-known",
        "agent-skills",
        "timewarp-architecture-discovery",
        "SKILL.md"
      )
    );

    foreach (string path in ExcludedPaths)
    {
      llms.ShouldContain(path);
      skill.ShouldContain(path);
    }

    llms.ShouldContain("x-timewarp-excluded-routes");
    llms.ShouldContain("/scalar/v1");
    llms.ShouldContain("GET|POST /api");
    skill.ShouldContain("x-timewarp-excluded-routes");
    skill.ShouldContain("/scalar/v1");
    return Task.CompletedTask;
  }

  public static async Task Without_The_Opt_In_Should_Omit_Web_Only_Routes()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Production,
      }
    );
    builder.WebHost.UseTestServer();
    CommonServerModule.AddOpenApi(builder.Services, "v1", "Test API");
    WebApplication app = builder.Build();
    CommonServerModule.UseScalarApiReference(app, "v1", "Test API");
    await app.StartAsync();
    try
    {
      using HttpClient client = app.GetTestClient();
      using HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
      JsonElement root = document.RootElement;
      bool hasHealth = root.TryGetProperty("paths", out JsonElement paths)
        && paths.TryGetProperty("/api/health", out _);
      hasHealth.ShouldBeFalse();
      root.TryGetProperty("x-timewarp-excluded-routes", out _).ShouldBeFalse();
    }
    finally
    {
      await app.DisposeAsync();
    }
  }

  private static void AssertAnonymous(JsonElement operation)
  {
    if (operation.TryGetProperty("security", out JsonElement security))
    {
      security.GetArrayLength().ShouldBe(0);
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

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }
}

public sealed class TipGetEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/tip");
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class TipPostEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Post("/api/tip");
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}
