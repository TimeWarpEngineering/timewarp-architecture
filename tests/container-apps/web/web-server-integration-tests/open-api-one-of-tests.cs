#region Purpose
// The OpenAPI document publishes OneOf unions as their arms, never as the OneOf struct wrapper.
#endregion

#region Design
// Task 442: every contract endpoint declares OneOf<Response, SharedProblemDetails>, and the
// document carried a OneOfOneOfOf… component per endpoint with value, index, isT0…, and asT0…
// members. The scan covers every web-server endpoint, so the next union cannot bring one back.
// Two probe endpoints in this file reach the branches the contracts do not: a response union
// with two success arms, and a DTO property typed as a OneOf. The host is the OpenAPI pipeline
// only: FastEndpoints discovers the web-server endpoints plus the probes. No mediator graph.
#endregion

namespace OpenApiOneOf_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OneOf;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;
using TimeWarp.Foundation.Types;

[TestTag("OpenApi")]
public class Document_Given_
{
  private const string ProblemJson = "application/problem+json";
  private const string ProblemSchema = "#/components/schemas/TimeWarpFoundationTypesSharedProblemDetails";

  private static readonly string[] WrapperMembers = ["isT0", "asT0", "index"];

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
      options.Assemblies = [typeof(Program).Assembly, typeof(OneOfProbeUnionEndpoint).Assembly];
      options.Filter = type => type.Assembly == typeof(Program).Assembly
        || type.Namespace == typeof(OneOfProbeUnionEndpoint).Namespace;
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

  public static async Task No_Schema_Should_Be_A_OneOf_Wrapper()
  {
    JsonElement document = await DocumentRootAsync();
    List<string> wrapperComponents = document.GetProperty("components").GetProperty("schemas")
      .EnumerateObject()
      .Select(static schema => schema.Name)
      .Where(static name => name.StartsWith("OneOf", StringComparison.Ordinal))
      .ToList();
    wrapperComponents.ShouldBeEmpty($"OneOf wrapper components: {string.Join(", ", wrapperComponents)}");

    List<string> wrapperShapes = [];
    CollectWrapperShapes(document, "$", wrapperShapes);
    wrapperShapes.ShouldBeEmpty($"Schemas with OneOf wrapper members: {string.Join(", ", wrapperShapes)}");
  }

  public static async Task Contract_Operation_Should_Publish_Response_And_Problem_Json()
  {
    JsonElement responses = (await OperationAsync("/Analytics/TrackEvent", "post")).GetProperty("responses");

    SchemaRef(responses.GetProperty("200"), "application/json")
      .ShouldEndWith("TrackEvent_Response");
    SchemaRef(responses.GetProperty("default"), ProblemJson).ShouldBe(ProblemSchema);
  }

  public static async Task Every_Default_Response_Should_Be_Shared_Problem_Details()
  {
    JsonElement document = await DocumentRootAsync();
    int defaults = 0;
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (JsonProperty operation in path.Value.EnumerateObject())
      {
        if (!operation.Value.TryGetProperty("responses", out JsonElement responses)
          || !responses.TryGetProperty("default", out JsonElement problem))
        {
          continue;
        }

        defaults++;
        SchemaRef(problem, ProblemJson).ShouldBe(ProblemSchema, $"{operation.Name} {path.Name}");
      }
    }

    defaults.ShouldBeGreaterThan(0);
  }

  public static async Task Two_Success_Arms_Should_Publish_OneOf_At_200()
  {
    JsonElement responses = (await OperationAsync(OneOfProbeUnionEndpoint.Route, "get")).GetProperty("responses");

    JsonElement oneOf = responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
      .GetProperty("schema").GetProperty("oneOf");
    oneOf.EnumerateArray()
      .Select(static arm => arm.GetProperty("$ref").GetString()!)
      .ToList()
      .ShouldSatisfyAllConditions
      (
        static refs => refs.Count.ShouldBe(2),
        static refs => refs[0].ShouldEndWith(nameof(OneOfProbeCat)),
        static refs => refs[1].ShouldEndWith(nameof(OneOfProbeDog))
      );
    SchemaRef(responses.GetProperty("default"), ProblemJson).ShouldBe(ProblemSchema);
  }

  public static async Task OneOf_Property_Should_Publish_OneOf_Over_Its_Arms()
  {
    JsonElement document = await DocumentRootAsync();
    JsonElement holder = document.GetProperty("components").GetProperty("schemas").EnumerateObject()
      .Single(static schema => schema.Name.EndsWith(nameof(OneOfProbeHolder), StringComparison.Ordinal))
      .Value;

    JsonElement pet = holder.GetProperty("properties").GetProperty("pet");
    pet.TryGetProperty("properties", out _).ShouldBeFalse();
    pet.GetProperty("oneOf").EnumerateArray()
      .Select(static arm => arm.GetProperty("$ref").GetString()!)
      .ToList()
      .ShouldSatisfyAllConditions
      (
        static refs => refs.Count.ShouldBe(2),
        static refs => refs[0].ShouldEndWith(nameof(OneOfProbeCat)),
        static refs => refs[1].ShouldEndWith(nameof(OneOfProbeDog))
      );
  }

  public static async Task Scalar_Should_Still_Serve()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/scalar/v1");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  private static void CollectWrapperShapes(JsonElement element, string path, List<string> hits)
  {
    if (element.ValueKind == JsonValueKind.Array)
    {
      int index = 0;
      foreach (JsonElement item in element.EnumerateArray())
      {
        CollectWrapperShapes(item, $"{path}[{index++}]", hits);
      }

      return;
    }

    if (element.ValueKind != JsonValueKind.Object)
    {
      return;
    }

    if (element.TryGetProperty("properties", out JsonElement properties)
      && properties.ValueKind == JsonValueKind.Object
      && WrapperMembers.All(member => properties.TryGetProperty(member, out _)))
    {
      hits.Add(path);
    }

    foreach (JsonProperty property in element.EnumerateObject())
    {
      CollectWrapperShapes(property.Value, $"{path}.{property.Name}", hits);
    }
  }

  private static string SchemaRef(JsonElement response, string mediaType) =>
    response.GetProperty("content").GetProperty(mediaType).GetProperty("schema").GetProperty("$ref").GetString()!;

  private static async Task<JsonElement> OperationAsync(string path, string method)
  {
    JsonElement paths = (await DocumentRootAsync()).GetProperty("paths");
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

  private static async Task<JsonElement> DocumentRootAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }
}

public sealed class OneOfProbeCat
{
  public string Name { get; set; } = string.Empty;
}

public sealed class OneOfProbeDog
{
  public int Barks { get; set; }
}

public sealed class OneOfProbeHolder
{
  public OneOf<OneOfProbeCat, OneOfProbeDog> Pet { get; set; }
}

public sealed class OneOfProbeUnionEndpoint : EndpointWithoutRequest<OneOf<OneOfProbeCat, OneOfProbeDog, SharedProblemDetails>>
{
  internal const string Route = "/probe/one-of-union";

  public override void Configure()
  {
    Get(Route);
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class OneOfProbeHolderEndpoint : EndpointWithoutRequest<OneOfProbeHolder>
{
  internal const string Route = "/probe/one-of-holder";

  public override void Configure()
  {
    Get(Route);
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}
