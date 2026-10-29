#region Purpose
// DELETE /api/Roles/{roleId} is published without a required request body.
#endregion

#region Design
// The role id is the path parameter. UserId is the mock-mode caller id and travels on the query
// string, matching GetRole and HttpApiService. A bodyless DELETE (clear xAI key) stays bodyless.
// Two probe DELETE endpoints in this file cover the other transformer branches: nullable leftover
// scalars become optional query parameters, and a leftover collection keeps the body with
// required false. The host is the OpenAPI pipeline only: FastEndpoints discovers the generated
// endpoints plus the probes, and CommonServerModule.AddOpenApi runs the document transformers.
// No mediator graph.
#endregion

namespace OpenApiDeleteRequestBody_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class DeleteRoles_Given_
{
  private static readonly HashSet<string> EndpointNames = new(StringComparer.Ordinal)
  {
    "DeleteRoleEndpoint",
    "GetRoleEndpoint",
    "ClearUserXaiApiKeyEndpoint",
    nameof(DeleteProbeOptionalEndpoint),
    nameof(DeleteProbePayloadEndpoint),
  };

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DeleteRoles_Given_>();

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
      options.Assemblies = [typeof(Program).Assembly, typeof(DeleteProbeOptionalEndpoint).Assembly];
      options.Filter = type => EndpointNames.Contains(type.Name)
        && (type.Assembly == typeof(Program).Assembly
          || type.Namespace == typeof(DeleteProbeOptionalEndpoint).Namespace);
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

  public static async Task DeleteRole_Should_Omit_RequestBody_And_Publish_UserId_Query()
  {
    JsonElement operation = await OperationAsync("/api/Roles/{roleId}", "delete");
    operation.TryGetProperty("requestBody", out _).ShouldBeFalse();

    JsonElement roleId = Parameter(operation, "roleId", "path");
    roleId.TryGetProperty("required", out JsonElement roleRequired).ShouldBeTrue();
    roleRequired.GetBoolean().ShouldBeTrue();

    JsonElement userId = Parameter(operation, "userId", "query");
    userId.TryGetProperty("required", out JsonElement userRequired).ShouldBeTrue();
    userRequired.GetBoolean().ShouldBeTrue();
    string format = userId.GetProperty("schema").GetProperty("format").GetString()!;
    format.ShouldBe("uuid");
  }

  public static async Task GetRole_Should_Keep_UserId_On_The_Query_String()
  {
    JsonElement operation = await OperationAsync("/api/Roles/{roleId}", "get");
    operation.TryGetProperty("requestBody", out _).ShouldBeFalse();
    Parameter(operation, "userId", "query");
    Parameter(operation, "roleId", "path");
  }

  public static async Task ClearUserXaiApiKey_Should_Stay_Without_A_Body()
  {
    JsonElement operation = await OperationAsync("/api/Users/Current/xai-api-key", "delete");
    operation.TryGetProperty("requestBody", out _).ShouldBeFalse();
  }

  public static async Task Optional_Scalars_Should_Publish_Optional_Query_Parameters()
  {
    JsonElement operation = await OperationAsync(DeleteProbeOptionalEndpoint.Route, "delete");
    operation.TryGetProperty("requestBody", out _).ShouldBeFalse();

    IsRequired(Parameter(operation, "id", "path")).ShouldBeTrue();
    IsRequired(Parameter(operation, "limit", "query")).ShouldBeFalse();
    IsRequired(Parameter(operation, "note", "query")).ShouldBeFalse();
  }

  public static async Task Collection_Property_Should_Keep_An_Optional_RequestBody()
  {
    JsonElement operation = await OperationAsync(DeleteProbePayloadEndpoint.Route, "delete");
    operation.TryGetProperty("requestBody", out JsonElement requestBody).ShouldBeTrue();
    IsRequired(requestBody).ShouldBeFalse();
    Parameter(operation, "id", "path");
  }

  // The OpenAPI writer omits required when it is false.
  private static bool IsRequired(JsonElement element) =>
    element.TryGetProperty("required", out JsonElement required) && required.GetBoolean();

  private static JsonElement Parameter(JsonElement operation, string name, string location)
  {
    JsonElement parameters = operation.GetProperty("parameters");
    foreach (JsonElement parameter in parameters.EnumerateArray())
    {
      string parameterName = parameter.GetProperty("name").GetString() ?? string.Empty;
      string parameterIn = parameter.GetProperty("in").GetString() ?? string.Empty;
      if (string.Equals(parameterName, name, StringComparison.OrdinalIgnoreCase)
        && string.Equals(parameterIn, location, StringComparison.OrdinalIgnoreCase))
      {
        return parameter;
      }
    }

    string known = string.Join
    (
      ", ",
      parameters.EnumerateArray().Select(static item =>
        $"{item.GetProperty("in").GetString()}:{item.GetProperty("name").GetString()}")
    );
    throw new InvalidOperationException($"Missing {location} parameter {name}. Parameters: {known}");
  }

  private static async Task<JsonElement> OperationAsync(string path, string method)
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement paths = document.RootElement.GetProperty("paths");
    foreach (JsonProperty item in paths.EnumerateObject())
    {
      if (string.Equals(item.Name, path, StringComparison.OrdinalIgnoreCase)
        && item.Value.TryGetProperty(method, out JsonElement operation))
      {
        return operation.Clone();
      }
    }

    string known = string.Join(", ", paths.EnumerateObject().Select(static item => item.Name));
    throw new InvalidOperationException($"Missing {method} {path}. Paths: {known}");
  }
}

public sealed class DeleteProbeOptionalRequest
{
  public Guid Id { get; set; }
  public int? Limit { get; set; }
  public string? Note { get; set; }
}

public sealed class DeleteProbeOptionalEndpoint : Endpoint<DeleteProbeOptionalRequest>
{
  internal const string Route = "/probe/delete-optional/{id}";

  public override void Configure()
  {
    Delete(Route);
    AllowAnonymous();
  }

  public override Task HandleAsync(DeleteProbeOptionalRequest req, CancellationToken ct)
  {
    _ = req;
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class DeleteProbePayloadRequest
{
  public Guid Id { get; set; }
  public IReadOnlyList<Guid> Items { get; set; } = [];
}

public sealed class DeleteProbePayloadEndpoint : Endpoint<DeleteProbePayloadRequest>
{
  internal const string Route = "/probe/delete-payload/{id}";

  public override void Configure()
  {
    Delete(Route);
    AllowAnonymous();
  }

  public override Task HandleAsync(DeleteProbePayloadRequest req, CancellationToken ct)
  {
    _ = req;
    _ = ct;
    return Task.CompletedTask;
  }
}
