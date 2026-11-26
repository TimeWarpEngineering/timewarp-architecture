#region Purpose
// The OpenAPI document publishes [TypedId] structs as string uuid, and no component schema is untyped.
#endregion

#region Design
// Task 458-002. A [TypedId] struct is a Guid string on the wire, but the document published it as
// a description only, and Nullable<T> of one as {}, so Kiota and NSwag emitted object or any.
// The component scan covers every web-server component, so a later type whose converter hides its
// shape fails here: a component must carry type, oneOf, anyOf, allOf, $ref, or enum. The
// PrincipalId, CredentialId, and nullable PrincipalId components are pinned to the exact shape,
// and the session response's principalId must reach a string uuid. The host is the OpenAPI
// pipeline only: FastEndpoints discovers the web-server endpoints. No mediator graph.
#endregion

namespace OpenApiTypedId_;

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
  private const string ComponentPrefix = "#/components/schemas/";
  private const string PrincipalIdComponent = "TimeWarpIdentityPrincipalId";
  private const string CredentialIdComponent = "TimeWarpIdentityCredentialId";
  private const string NullablePrincipalIdComponent = "SystemNullableOfTimeWarpIdentityPrincipalId";

  private static readonly string[] ShapeKeywords = ["type", "oneOf", "anyOf", "allOf", "$ref", "enum"];

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
    CommonServerModule.AddOpenApi(builder.Services, "v1", "Test API", agentBearer: null);
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

  public static async Task No_Component_Schema_Should_Be_Untyped()
  {
    JsonElement schemas = await SchemasAsync();
    List<string> untyped = schemas.EnumerateObject()
      .Where(static schema => !ShapeKeywords.Any(keyword => schema.Value.TryGetProperty(keyword, out _)))
      .Select(static schema => $"{schema.Name}: {schema.Value.GetRawText()}")
      .ToList();

    schemas.EnumerateObject().Count().ShouldBeGreaterThan(0);
    untyped.ShouldBeEmpty($"Component schemas with no type, oneOf, anyOf, allOf, $ref, or enum: {string.Join("; ", untyped)}");
  }

  public static async Task Typed_Id_Should_Be_String_Uuid_With_Description()
  {
    JsonElement schemas = await SchemasAsync();
    foreach (string component in new[] { PrincipalIdComponent, CredentialIdComponent })
    {
      JsonElement schema = schemas.GetProperty(component);
      schema.GetProperty("type").GetString().ShouldBe("string", component);
      schema.GetProperty("format").GetString().ShouldBe("uuid", component);
      schema.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace(component);
    }
  }

  public static async Task Nullable_Typed_Id_Should_Be_Null_Or_String_Uuid()
  {
    JsonElement schema = (await SchemasAsync()).GetProperty(NullablePrincipalIdComponent);

    schema.GetProperty("type").EnumerateArray()
      .Select(static item => item.GetString())
      .Order(StringComparer.Ordinal)
      .ShouldBe(["null", "string"]);
    schema.GetProperty("format").GetString().ShouldBe("uuid");
  }

  public static async Task Every_Principal_Id_Property_Should_Reach_String_Uuid()
  {
    JsonElement schemas = await SchemasAsync();
    List<string> holders = [];
    List<string> untyped = [];
    foreach (JsonProperty component in schemas.EnumerateObject())
    {
      if (!component.Value.TryGetProperty("properties", out JsonElement properties)
        || !properties.TryGetProperty("principalId", out JsonElement principalId))
      {
        continue;
      }

      holders.Add(component.Name);
      if (!ReachesStringUuid(schemas, principalId))
      {
        untyped.Add($"{component.Name}.principalId: {principalId.GetRawText()}");
      }
    }

    holders.ShouldNotBeEmpty("No component has a principalId property.");
    untyped.ShouldBeEmpty($"principalId properties that are not string uuid: {string.Join("; ", untyped)}");
  }

  /// <summary>
  /// True when <paramref name="schema"/>, through $ref and its oneOf or anyOf arms, has a string
  /// uuid arm and no arm other than null and string uuid.
  /// </summary>
  private static bool ReachesStringUuid(JsonElement schemas, JsonElement schema)
  {
    if (schema.TryGetProperty("$ref", out JsonElement reference))
    {
      string name = reference.GetString()!;
      return name.StartsWith(ComponentPrefix, StringComparison.Ordinal)
        && ReachesStringUuid(schemas, schemas.GetProperty(name[ComponentPrefix.Length..]));
    }

    foreach (string union in new[] { "oneOf", "anyOf" })
    {
      if (schema.TryGetProperty(union, out JsonElement arms))
      {
        List<JsonElement> typed = arms.EnumerateArray().Where(static arm => !IsNullOnly(arm)).ToList();
        return typed.Count > 0 && typed.All(arm => ReachesStringUuid(schemas, arm));
      }
    }

    return schema.TryGetProperty("format", out JsonElement format)
      && format.GetString() == "uuid"
      && schema.TryGetProperty("type", out JsonElement type)
      && (type.ValueKind == JsonValueKind.String
        ? type.GetString() == "string"
        : type.EnumerateArray().Any(static item => item.GetString() == "string"));
  }

  private static bool IsNullOnly(JsonElement schema) =>
    schema.TryGetProperty("type", out JsonElement type)
    && type.ValueKind == JsonValueKind.String
    && type.GetString() == "null";

  private static async Task<JsonElement> SchemasAsync() =>
    (await DocumentRootAsync()).GetProperty("components").GetProperty("schemas");

  private static async Task<JsonElement> DocumentRootAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    string json = await response.Content.ReadAsStringAsync();
    // A transformer guard throws; the developer exception page carries its message here.
    response.StatusCode.ShouldBe(HttpStatusCode.OK, json[..Math.Min(json.Length, 2000)]);
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }
}
