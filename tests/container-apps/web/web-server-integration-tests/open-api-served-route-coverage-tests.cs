#region Purpose
// Every route the real web-server host serves is an OpenAPI operation or an x-timewarp-excluded-routes entry.
#endregion

#region Design
// Task 458-007. WebServer_Given_ boots the real web-server host and walks its endpoint data
// source: a served route that is not an operation must be in x-timewarp-excluded-routes, unless
// it is in a family the document description names as unlisted (OpenApiDiscoveryCoverage
// UnlistedFamilies: Razor pages, static assets, Blazor framework routes, the document and Scalar,
// Development-only routes). The match is per method: an endpoint that declares methods needs each
// one listed (the dictation path is GET and CONNECT); one with no method metadata (SignalR
// connections) needs its path listed. The test host does not run Program.Main, so the walk also
// maps MapDefaultEndpoints, the call Program.Main makes, on a Production host and checks those
// routes (/alive) against the real document. Its own file because it boots its own host graph.
#endregion

namespace OpenApiDiscoveryCoverage_;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeWarp.Foundation.Configuration;

[TestTag("OpenApi")]
public class WebServer_Given_
{
  private static readonly string[] UnlistedPrefixes =
  [
    "/_blazor",
    "/_framework",
    "/openapi/",
    "/scalar",
  ];

  private static readonly string[] DevelopmentOnlyPaths =
  [
    HealthEndpointPaths.Health,
    TimeWarp.State.DevTools.Export.TimeWarpStateDevToolsClientConfigEndpoint.Path,
  ];

  private static readonly string[] OperationKeys =
  [
    "get",
    "put",
    "post",
    "delete",
    "options",
    "head",
    "patch",
    "trace",
    "query",
  ];

  private static HostGraph? Graph;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<WebServer_Given_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Every_Served_Route_Should_Be_An_Operation_Or_Listed()
  {
    WebTestServerApplication web = Graph!.Web!;
    Coverage coverage = await CoverageAsync(web);
    EndpointDataSource dataSource = web.WebApplicationHost.ServiceProvider.GetRequiredService<EndpointDataSource>();

    List<string> unlisted = Unlisted(dataSource.Endpoints, coverage, out int checkedRoutes);

    checkedRoutes.ShouldBeGreaterThan(0);
    unlisted.ShouldBeEmpty
    (
      "Served but neither an operation nor in x-timewarp-excluded-routes: " + string.Join("; ", unlisted)
    );
  }

  public static async Task Default_Endpoints_Should_Be_Listed()
  {
    Coverage coverage = await CoverageAsync(Graph!.Web!);
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Production,
      }
    );
    builder.Services.AddHealthChecks();
    await using WebApplication app = builder.Build();
    app.MapDefaultEndpoints();
    Microsoft.AspNetCore.Http.Endpoint[] endpoints = [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints)];

    List<string> unlisted = Unlisted(endpoints, coverage, out int checkedRoutes);

    checkedRoutes.ShouldBeGreaterThan(0);
    coverage.Methods.ShouldContain("GET " + HealthEndpointPaths.Alive);
    unlisted.ShouldBeEmpty
    (
      "MapDefaultEndpoints serves but the document does not list: " + string.Join("; ", unlisted)
    );
  }

  public static async Task Dictation_Should_List_Every_Mapped_Method()
  {
    Coverage coverage = await CoverageAsync(Graph!.Web!);
    const string path = TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio.StreamPath;

    coverage.Methods.ShouldContain("GET " + path);
    coverage.Methods.ShouldContain("CONNECT " + path);
  }

  private static List<string> Unlisted(IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints, Coverage coverage, out int checkedRoutes)
  {
    List<string> unlisted = [];
    checkedRoutes = 0;
    foreach (RouteEndpoint endpoint in endpoints.OfType<RouteEndpoint>())
    {
      string path = Normalize(endpoint.RoutePattern.RawText ?? string.Empty);
      if (IsUnlistedFamily(endpoint, path))
      {
        continue;
      }

      checkedRoutes++;
      IReadOnlyList<string> methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
      if (methods.Count == 0)
      {
        if (!coverage.Paths.Contains(path))
        {
          unlisted.Add(path + " (" + endpoint.DisplayName + ")");
        }

        continue;
      }

      foreach (string method in methods)
      {
        if (!coverage.Methods.Contains(method + " " + path))
        {
          unlisted.Add(method + " " + path + " (" + endpoint.DisplayName + ")");
        }
      }
    }

    return unlisted;
  }

  private static async Task<Coverage> CoverageAsync(WebTestServerApplication web)
  {
    using HttpResponseMessage response = await web.HttpClient.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    JsonElement root = document.RootElement;

    // The document camel-cases route parameter names ({linkId}); the route keeps the DTO's ({LinkId}).
    Coverage coverage = new
    (
      new HashSet<string>(StringComparer.OrdinalIgnoreCase),
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    );
    foreach (JsonProperty path in root.GetProperty("paths").EnumerateObject())
    {
      coverage.Paths.Add(path.Name);
      foreach (JsonProperty operation in path.Value.EnumerateObject())
      {
        if (OperationKeys.Contains(operation.Name, StringComparer.Ordinal))
        {
          coverage.Methods.Add(operation.Name + " " + path.Name);
        }
      }
    }

    foreach (JsonElement entry in root.GetProperty("x-timewarp-excluded-routes").EnumerateArray())
    {
      string path = entry.GetProperty("path").GetString()!;
      coverage.Paths.Add(path);
      coverage.Methods.Add(entry.GetProperty("method").GetString() + " " + path);
    }

    return coverage;
  }

  private static bool IsUnlistedFamily(RouteEndpoint endpoint, string path)
  {
    return endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Components.Endpoints.ComponentTypeMetadata>() is not null
      || endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.StaticAssets.StaticAssetDescriptor>() is not null
      || endpoint.RoutePattern.Parameters.Any(IsFileParameter)
      || UnlistedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      || DevelopmentOnlyPaths.Contains(path, StringComparer.OrdinalIgnoreCase);
  }

  // The static-file fallback ({**path:file}) serves files the asset manifest does not list.
  private static bool IsFileParameter(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart parameter)
  {
    return parameter.ParameterPolicies.Any(static policy => string.Equals(policy.Content, "file", StringComparison.Ordinal));
  }

  private static string Normalize(string rawText)
  {
    // The document spells a route parameter without its constraint or optional marker.
    string[] segments = rawText.TrimStart('/').Split('/');
    for (int index = 0; index < segments.Length; index++)
    {
      string segment = segments[index];
      if (segment.StartsWith('{', StringComparison.Ordinal) && segment.EndsWith('}', StringComparison.Ordinal))
      {
        string name = segment[1..^1].TrimStart('*');
        int end = name.IndexOfAny([':', '?', '=']);
        segments[index] = "{" + (end < 0 ? name : name[..end]) + "}";
      }
    }

    return "/" + string.Join('/', segments);
  }

  private sealed record Coverage(HashSet<string> Paths, HashSet<string> Methods);
}
