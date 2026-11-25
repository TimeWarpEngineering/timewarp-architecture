#region Purpose
// The OpenAPI document publishes no caller id and no OData paging member that a handler ignores.
#endregion

#region Design
// Task 458-008: every IAuthApiRequest contract published a required userId (query or body) that no
// hosted handler read, and GET /api/admin/roles published top, skip, filter, orderBy, and a
// required returnTotalCount that its handler ignored. A generated client had to invent a Guid.
// The fix removed IAuthApiRequest.UserId and the [AuthApiRequest] / [OpenDataQueryParameters]
// generator mixins. This scan covers every web-server operation, so a contract that brings back a
// caller id or an unread paging mixin fails here. Request bodies are resolved through $ref into
// components, one level of allOf/oneOf/anyOf included. A future list query that really pages declares
// its own paging members beside a handler that reads them; add a test that the page size changes
// the row count and narrow the paging names below to the operations that do not page.
// The host is the OpenAPI pipeline only: FastEndpoints discovers the web-server endpoints and
// CommonServerModule.AddOpenApi runs the document transformers. No mediator graph.
#endregion

namespace OpenApiUnreadRequestMembers_;

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
  private static readonly string[] PagingNames = ["top", "skip", "filter", "orderBy", "returnTotalCount"];

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

  public static async Task No_Operation_Should_Publish_A_UserId_Parameter_Or_Body_Property()
  {
    JsonElement document = await DocumentRootAsync();
    List<string> offenders = [];
    int operationCount = 0;
    foreach ((string label, JsonElement operation) in Operations(document))
    {
      operationCount++;
      foreach (string name in ParameterNames(operation))
      {
        if (string.Equals(name, "userId", StringComparison.OrdinalIgnoreCase))
        {
          offenders.Add($"{label} parameter {name}");
        }
      }

      foreach (string name in RequestBodyPropertyNames(document, operation))
      {
        if (string.Equals(name, "userId", StringComparison.OrdinalIgnoreCase))
        {
          offenders.Add($"{label} body property {name}");
        }
      }
    }

    operationCount.ShouldBeGreaterThan(0);
    offenders.ShouldBeEmpty($"Operations that publish a caller id: {string.Join(", ", offenders)}");
  }

  public static async Task No_Operation_Should_Publish_OData_Paging_Parameters()
  {
    JsonElement document = await DocumentRootAsync();
    List<string> offenders = [];
    foreach ((string label, JsonElement operation) in Operations(document))
    {
      foreach (string name in ParameterNames(operation))
      {
        if (PagingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
          offenders.Add($"{label} parameter {name}");
        }
      }
    }

    offenders.ShouldBeEmpty($"Operations that publish paging no handler reads: {string.Join(", ", offenders)}");
  }

  public static async Task GetRoles_Should_Publish_Only_Search()
  {
    JsonElement document = await DocumentRootAsync();
    JsonElement operation = Operations(document)
      .Single(static item => string.Equals(item.Label, "GET /api/admin/roles", StringComparison.OrdinalIgnoreCase))
      .Operation;

    ParameterNames(operation).ShouldBe(["search"]);
  }

  private static List<(string Label, JsonElement Operation)> Operations(JsonElement document)
  {
    List<(string Label, JsonElement Operation)> operations = [];
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (string method in Methods)
      {
        if (path.Value.TryGetProperty(method, out JsonElement operation))
        {
          operations.Add(($"{method.ToUpperInvariant()} {path.Name}", operation));
        }
      }
    }

    return operations;
  }

  private static List<string> ParameterNames(JsonElement operation)
  {
    if (!operation.TryGetProperty("parameters", out JsonElement parameters))
    {
      return [];
    }

    return
    [
      .. parameters.EnumerateArray()
        .Where(static parameter => parameter.TryGetProperty("name", out _))
        .Select(static parameter => parameter.GetProperty("name").GetString() ?? string.Empty)
    ];
  }

  private static List<string> RequestBodyPropertyNames(JsonElement document, JsonElement operation)
  {
    List<string> names = [];
    if (!operation.TryGetProperty("requestBody", out JsonElement requestBody)
      || !requestBody.TryGetProperty("content", out JsonElement content))
    {
      return names;
    }

    foreach (JsonProperty mediaType in content.EnumerateObject())
    {
      if (mediaType.Value.TryGetProperty("schema", out JsonElement schema))
      {
        AddPropertyNames(document, schema, names, depth: 0);
      }
    }

    return names;
  }

  private static void AddPropertyNames(JsonElement document, JsonElement schema, List<string> names, int depth)
  {
    if (depth > 4)
    {
      return;
    }

    JsonElement resolved = Resolve(document, schema);
    if (resolved.TryGetProperty("properties", out JsonElement properties))
    {
      names.AddRange(properties.EnumerateObject().Select(static property => property.Name));
    }

    foreach (string composite in (string[])["allOf", "oneOf", "anyOf"])
    {
      if (!resolved.TryGetProperty(composite, out JsonElement members))
      {
        continue;
      }

      foreach (JsonElement member in members.EnumerateArray())
      {
        AddPropertyNames(document, member, names, depth + 1);
      }
    }
  }

  private static JsonElement Resolve(JsonElement document, JsonElement schema)
  {
    if (!schema.TryGetProperty("$ref", out JsonElement reference))
    {
      return schema;
    }

    string path = reference.GetString() ?? string.Empty;
    const string prefix = "#/components/schemas/";
    if (!path.StartsWith(prefix, StringComparison.Ordinal))
    {
      return schema;
    }

    return document.GetProperty("components").GetProperty("schemas").GetProperty(path[prefix.Length..]);
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
