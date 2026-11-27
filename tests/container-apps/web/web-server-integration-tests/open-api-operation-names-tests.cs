#region Purpose
// The web-server OpenAPI document names and summarizes every operation from its contract's [Operation], describes its parameters, and quotes only real operation ids.
#endregion

#region Design
// Task 458-005. The first host registers every web-server endpoint type and the public web
// routes over the OpenAPI pipeline only, as the root-tags test does. Contract expectations are
// read from the web-contracts assembly by reflection: every hosted [ApiEndpoint] request's
// generated OperationName must be an operationId in the document with OperationDescription as
// its summary, so a new contract is covered without editing this file. Quoted ids are found in
// every string of the document (not only the places OpenApiOperationNames' check reads), so a
// description the check does not visit still cannot quote a stale id. The summary and
// <resource>.<action> rules live here, not in the runtime check, because other test hosts
// publish probe endpoints that have no contract. The second host adds a
// description that quotes a missing id, and the document check must fail the request naming it.
#endregion

namespace OpenApiOperationNames_;

using System.Net;
using System.Reflection;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Features.Admin.Roles;
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
    (App, Client) = await OperationNamesHost.StartAsync(endpointName: null, documentPublicWebRoutes: true, configureOpenApi: null);
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

  public static async Task Every_Operation_Should_Have_A_Summary()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);

    var unsummarized = OperationNamesHost.Operations(document)
      .Where(static operation => string.IsNullOrWhiteSpace(OperationNamesHost.Text(operation.Operation, "summary")))
      .Select(static operation => operation.Where)
      .ToList();

    unsummarized.ShouldBeEmpty($"Operations without a summary: {string.Join(", ", unsummarized)}");
  }

  public static async Task Every_OperationId_Should_Be_Resource_Dot_Action_And_Unique()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    List<string> ids = [.. OperationNamesHost.Operations(document).Select(static operation => OperationNamesHost.Text(operation.Operation, "operationId") ?? "<none> at " + operation.Where)];

    var malformed = ids.Where(static id => !OperationNamesHost.IsOperationId(id)).ToList();
    malformed.ShouldBeEmpty($"operationIds that are not <resource>.<action>: {string.Join(", ", malformed)}");
    ids.Distinct(StringComparer.Ordinal).Count().ShouldBe(ids.Count, $"Repeated operationIds: {string.Join(", ", ids.GroupBy(static id => id).Where(static g => g.Count() > 1).Select(static g => g.Key))}");
  }

  public static async Task Every_Contract_Operation_Should_Use_Its_Operation_Name_And_Description()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    Dictionary<string, JsonElement> byId = OperationNamesHost.ById(document);
    List<(string Name, string Description)> contracts = OperationNamesHost.HostedContractOperations(typeof(GetRoles).Assembly);

    contracts.Count.ShouldBeGreaterThan(0);
    foreach ((string name, string description) in contracts)
    {
      byId.TryGetValue(name, out JsonElement operation).ShouldBeTrue($"No operation has operationId {name}");
      OperationNamesHost.Text(operation, "summary").ShouldBe(description, name);
    }
  }

  public static async Task Description_Should_Not_Repeat_The_Summary()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);

    var repeated = OperationNamesHost.Operations(document)
      .Where(static operation =>
        OperationNamesHost.Text(operation.Operation, "summary") is { Length: > 0 } summary
        && OperationNamesHost.Text(operation.Operation, "description") is { } description
        && description.StartsWith(summary.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
      .Select(static operation => operation.Where)
      .ToList();

    repeated.ShouldBeEmpty($"Descriptions that start with the summary: {string.Join(", ", repeated)}");
  }

  public static async Task Quoted_Operation_Ids_Should_Resolve()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    HashSet<string> ids = [.. OperationNamesHost.ById(document).Keys];
    List<string> quoted = [];
    OperationNamesHost.CollectQuotedIds(document, quoted);

    var stale = quoted.Where(id => !ids.Contains(id)).Distinct(StringComparer.Ordinal).ToList();
    stale.ShouldBeEmpty($"Quoted operation ids with no operation: {string.Join(", ", stale)}");
  }

  public static async Task Hand_Added_Operations_Should_Use_Resource_Dot_Action()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    Dictionary<string, JsonElement> byId = OperationNamesHost.ById(document);

    byId.ShouldContainKey("health.get");
    byId.ShouldContainKey("tip.get-alias");
    byId.ShouldContainKey("tip.submit-alias");
  }

  public static async Task Every_Parameter_Should_Have_A_Description()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    List<string> undescribed = [];
    foreach ((string where, JsonElement operation) in OperationNamesHost.Operations(document))
    {
      if (!operation.TryGetProperty("parameters", out JsonElement parameters))
      {
        continue;
      }

      foreach (JsonElement parameter in parameters.EnumerateArray())
      {
        if (string.IsNullOrWhiteSpace(OperationNamesHost.Text(parameter, "description")))
        {
          undescribed.Add(where + " " + OperationNamesHost.Text(parameter, "name"));
        }
      }
    }

    undescribed.ShouldBeEmpty($"Parameters without a description: {string.Join(", ", undescribed)}");
  }

  public static async Task Route_Parameter_Should_Carry_The_Generated_Route_Summary()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    JsonElement operation = OperationNamesHost.ById(document)[GetRole.Query.OperationName];

    JsonElement roleId = operation.GetProperty("parameters").EnumerateArray()
      .Single(static parameter => string.Equals(OperationNamesHost.Text(parameter, "name"), nameof(GetRole.Query.RoleId), StringComparison.OrdinalIgnoreCase));
    OperationNamesHost.Text(roleId, "description").ShouldBe("The RoleId value in the route " + GetRole.Query.RouteTemplate + ".");
  }

  public static async Task Interface_Member_Summary_Should_Describe_The_Schema_Property()
  {
    JsonElement document = await OperationNamesHost.DocumentRootAsync(Client!);
    JsonElement operation = OperationNamesHost.ById(document)[CreateRole.Command.OperationName];
    JsonElement schema = operation.GetProperty("requestBody").GetProperty("content").EnumerateObject().First().Value.GetProperty("schema");
    if (OperationNamesHost.Text(schema, "$ref") is { } reference)
    {
      schema = document.GetProperty("components").GetProperty("schemas").GetProperty(reference[(reference.AsSpan().LastIndexOf('/') + 1)..]);
    }

    JsonElement name = schema.GetProperty("properties").GetProperty("name");
    OperationNamesHost.Text(name, "description").ShouldBe("The role's display name.");
  }
}

[TestTag("OpenApi")]
public class StaleQuote_Given_
{
  private const string MissingId = "widgets.missing";

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StaleQuote_Given_>();

  public static async Task SetupOnce()
  {
    (App, Client) = await OperationNamesHost.StartAsync
    (
      nameof(HelloEndpoint),
      documentPublicWebRoutes: false,
      configureOpenApi: options => options.AddDocumentTransformer
      (
        (document, context, cancellationToken) =>
        {
          _ = context;
          _ = cancellationToken;
          document.Info.Description = "Call `" + MissingId + "` first.";
          return Task.CompletedTask;
        }
      )
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

  public static async Task Document_Check_Should_Fail_Naming_The_Quoted_Id()
  {
    using HttpResponseMessage response = await Client!.GetAsync(OperationNamesHost.DocumentPath);
    string body = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    body.ShouldContain("OpenApiOperationNames");
    body.ShouldContain(MissingId);
  }
}

internal static class OperationNamesHost
{
  internal const string DocumentPath = "/openapi/v1.json";

  private static readonly string[] Methods = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

  /// <summary>Web-server endpoints (or the one named) over the OpenAPI pipeline.</summary>
  internal static async Task<(WebApplication App, HttpClient Client)> StartAsync
  (
    string? endpointName,
    bool documentPublicWebRoutes,
    Action<Microsoft.AspNetCore.OpenApi.OpenApiOptions>? configureOpenApi
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
      configureOpenApi: configureOpenApi,
      documentInfo: WebServerOpenApiTags.DocumentInfo()
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

  internal static IEnumerable<(string Where, JsonElement Operation)> Operations(JsonElement document)
  {
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (string method in Methods)
      {
        if (path.Value.TryGetProperty(method, out JsonElement operation))
        {
          yield return (method.ToUpperInvariant() + " " + path.Name, operation);
        }
      }
    }
  }

  internal static Dictionary<string, JsonElement> ById(JsonElement document)
  {
    Dictionary<string, JsonElement> byId = new(StringComparer.Ordinal);
    foreach ((_, JsonElement operation) in Operations(document))
    {
      if (Text(operation, "operationId") is { } id)
      {
        byId[id] = operation;
      }
    }

    return byId;
  }

  internal static string? Text(JsonElement element, string property) =>
    element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
      ? value.GetString()
      : null;

  /// <summary>Every backtick-quoted operation id in any string of the document.</summary>
  internal static void CollectQuotedIds(JsonElement element, List<string> quoted)
  {
    switch (element.ValueKind)
    {
      case JsonValueKind.Object:
        foreach (JsonProperty property in element.EnumerateObject())
        {
          CollectQuotedIds(property.Value, quoted);
        }

        break;
      case JsonValueKind.Array:
        foreach (JsonElement item in element.EnumerateArray())
        {
          CollectQuotedIds(item, quoted);
        }

        break;
      case JsonValueKind.String:
        string[] pieces = element.GetString()!.Split('`');
        // Odd pieces sit between a pair of backticks.
        for (int index = 1; index < pieces.Length - 1; index += 2)
        {
          if (IsOperationId(pieces[index]))
          {
            quoted.Add(pieces[index]);
          }
        }

        break;
    }
  }

  /// <summary>
  /// OperationName and OperationDescription of every hosted contract request: a nested type of an
  /// [ApiEndpoint] class, without [ClientOnlyContract], that carries the generated constants.
  /// </summary>
  internal static List<(string Name, string Description)> HostedContractOperations(Assembly contracts)
  {
    List<(string Name, string Description)> operations = [];
    foreach (Type outer in contracts.GetTypes().Where(static type => HasAttribute(type, "ApiEndpointAttribute")))
    {
      foreach (Type request in outer.GetNestedTypes().Where(static type => type.Name is "Query" or "Command"))
      {
        string? name = Constant(request, "OperationName");
        string? description = Constant(request, "OperationDescription");
        if (name is not null && description is not null && !HasAttribute(request, "ClientOnlyContractAttribute"))
        {
          operations.Add((name, description));
        }
      }
    }

    return operations;
  }

  /// <summary>
  /// Lower-case kebab segments joined by dots, at least two segments, each starting with a letter
  /// (<c>roles.list</c>, <c>agent-key-registration.start</c>).
  /// </summary>
  internal static bool IsOperationId(string value)
  {
    string[] segments = value.Split('.');
    return segments.Length >= 2 && segments.All(static segment =>
      segment.Length > 0
      && segment[0] is >= 'a' and <= 'z'
      && segment[^1] != '-'
      && !segment.Contains("--", StringComparison.Ordinal)
      && segment.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'));
  }

  private static bool HasAttribute(Type type, string name) =>
    type.GetCustomAttributesData().Any(attribute => attribute.AttributeType.Name == name);

  private static string? Constant(Type type, string name) =>
    type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue() as string;
}
