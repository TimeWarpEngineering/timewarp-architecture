#region Purpose
// The web-server OpenAPI document declares and describes every operation tag at the root, and publishes contact and license from repository metadata.
#endregion

#region Design
// Task 458-011. The first host registers every web-server endpoint type, the public web routes,
// and WebServerOpenApiTags.DocumentInfo(), as program.cs does, over the OpenAPI pipeline only (no
// mediator graph). Root tags must equal the tags operations use, each with a description, and
// every description in WebServerOpenApiTags must name a tag in use, so the map cannot go stale
// when a slice is removed. Contact and license are compared with the AssemblyMetadata
// msbuild/open-api-document-metadata.props writes, read here on their own. The second host gives
// the hello endpoint an empty description map, and the document check must fail the request
// naming the tag.
#endregion

namespace OpenApiRootTags_;

using System.Net;
using System.Reflection;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Features.Hellos;
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
    (App, Client) = await RootTagsHost.StartAsync
    (
      endpointName: null,
      documentPublicWebRoutes: true,
      WebServerOpenApiTags.DocumentInfo()
    );
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

  public static async Task Root_Tags_Should_Equal_The_Operation_Tags()
  {
    JsonElement document = await RootTagsHost.DocumentRootAsync(Client!);

    SortedSet<string> operationTags = RootTagsHost.OperationTags(document);
    SortedSet<string> rootTags = new(RootTagsHost.RootTags(document).Keys, StringComparer.Ordinal);

    operationTags.Count.ShouldBeGreaterThan(0);
    rootTags.ShouldBe(operationTags, $"Root tags: {string.Join(", ", rootTags)}. Operation tags: {string.Join(", ", operationTags)}");
  }

  public static async Task Every_Root_Tag_Should_Have_A_Description()
  {
    JsonElement document = await RootTagsHost.DocumentRootAsync(Client!);

    var undescribed = RootTagsHost.RootTags(document)
      .Where(static tag => string.IsNullOrWhiteSpace(tag.Value))
      .Select(static tag => tag.Key)
      .ToList();

    undescribed.ShouldBeEmpty($"Root tags without a description: {string.Join(", ", undescribed)}");
  }

  public static async Task Every_Description_Should_Name_A_Tag_In_Use()
  {
    JsonElement document = await RootTagsHost.DocumentRootAsync(Client!);
    SortedSet<string> operationTags = RootTagsHost.OperationTags(document);

    var unused = WebServerOpenApiTags.Descriptions.Keys
      .Where(name => !operationTags.Contains(name))
      .ToList();

    unused.ShouldBeEmpty($"WebServerOpenApiTags descriptions no operation uses: {string.Join(", ", unused)}");
  }

  public static async Task Info_Should_Publish_Contact_And_License_From_Repository_Metadata()
  {
    JsonElement document = await RootTagsHost.DocumentRootAsync(Client!);
    JsonElement info = document.GetProperty("info");
    Assembly webServer = typeof(WebServerOpenApiTags).Assembly;
    string contactUrl = Metadata(webServer, OpenApiDocumentInfo.ContactUrlMetadataKey);
    string license = Metadata(webServer, OpenApiDocumentInfo.LicenseMetadataKey);

    Uri.TryCreate(contactUrl, UriKind.Absolute, out Uri? contactUri).ShouldBeTrue();
    contactUri!.Scheme.ShouldBe(Uri.UriSchemeHttps);
    new Uri(info.GetProperty("contact").GetProperty("url").GetString()!).ShouldBe(contactUri);
    info.GetProperty("license").GetProperty("name").GetString().ShouldBe(license);
    info.GetProperty("license").GetProperty("identifier").GetString().ShouldBe(license);
  }

  private static string Metadata(Assembly assembly, string key) =>
    assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
      .Single(attribute => attribute.Key == key)
      .Value
      .ShouldNotBeNull();
}

[TestTag("OpenApi")]
public class UndescribedTag_Given_
{
  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<UndescribedTag_Given_>();

  public static async Task SetupOnce()
  {
    (App, Client) = await RootTagsHost.StartAsync
    (
      nameof(HelloEndpoint),
      documentPublicWebRoutes: false,
      new OpenApiDocumentInfo(new Uri("https://example.test/repository"), "MIT", new Dictionary<string, string>())
    );
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

  public static async Task Document_Check_Should_Fail_Naming_The_Tag()
  {
    using HttpResponseMessage response = await Client!.GetAsync(RootTagsHost.DocumentPath);
    string body = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    body.ShouldContain("OpenApiRootTags");
    body.ShouldContain("Hellos");
  }
}

internal static class RootTagsHost
{
  internal const string DocumentPath = "/openapi/v1.json";

  private static readonly string[] Methods = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

  /// <summary>
  /// Web-server endpoints (or the one named) over the OpenAPI pipeline with <paramref name="documentInfo"/>.
  /// </summary>
  internal static async Task<(WebApplication App, HttpClient Client)> StartAsync
  (
    string? endpointName,
    bool documentPublicWebRoutes,
    OpenApiDocumentInfo documentInfo
  )
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
      if (endpointName is not null)
      {
        options.Filter = type => string.Equals(type.Name, endpointName, StringComparison.Ordinal);
      }
    });
    CommonServerModule.AddOpenApi
    (
      builder.Services,
      "v1",
      "Test API",
      agentBearer: null,
      documentPublicWebRoutes: documentPublicWebRoutes,
      documentInfo: documentInfo
    );
    WebApplication app = builder.Build();
    app.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });
    CommonServerModule.UseScalarApiReference(app, "v1", "Test API");
    await app.StartAsync();
    return (app, app.GetTestClient());
  }

  internal static async Task<JsonElement> DocumentRootAsync(HttpClient client)
  {
    using HttpResponseMessage response = await client.GetAsync(DocumentPath);
    string json = await response.Content.ReadAsStringAsync();
    response.StatusCode.ShouldBe(HttpStatusCode.OK, json);
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }

  internal static SortedSet<string> OperationTags(JsonElement document)
  {
    SortedSet<string> tags = new(StringComparer.Ordinal);
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (string method in Methods)
      {
        if (path.Value.TryGetProperty(method, out JsonElement operation)
          && operation.TryGetProperty("tags", out JsonElement operationTags))
        {
          foreach (JsonElement tag in operationTags.EnumerateArray())
          {
            tags.Add(tag.GetString()!);
          }
        }
      }
    }

    return tags;
  }

  internal static Dictionary<string, string?> RootTags(JsonElement document)
  {
    Dictionary<string, string?> tags = new(StringComparer.Ordinal);
    if (!document.TryGetProperty("tags", out JsonElement rootTags))
    {
      return tags;
    }

    foreach (JsonElement tag in rootTags.EnumerateArray())
    {
      tags[tag.GetProperty("name").GetString()!] = tag.TryGetProperty("description", out JsonElement description)
        ? description.GetString()
        : null;
    }

    return tags;
  }
}
