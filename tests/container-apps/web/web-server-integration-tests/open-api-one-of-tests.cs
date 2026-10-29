#region Purpose
// The OpenAPI document publishes OneOf unions as their arms, never as the OneOf struct wrapper.
#endregion

#region Design
// Task 442: every contract endpoint declares OneOf<Response, SharedProblemDetails>, and the
// document carried a OneOfOneOfOf… component per endpoint with value, index, isT0…, and asT0…
// members. The scan covers every web-server endpoint, so the next union cannot bring one back.
// The default check starts from the endpoint metadata, not the document: every endpoint whose
// response union has a SharedProblemDetails arm must publish default, so an operation that
// lost it fails here. Probe endpoints in this file reach the branches the contracts do not: a
// response union with two success arms, a DTO property typed as a OneOf, an explicit 4xx
// problem response, and a union with no problem arm. The host is the OpenAPI pipeline only:
// FastEndpoints discovers the web-server endpoints plus the probes. No mediator graph.
#endregion

namespace OpenApiOneOf_;

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OneOf;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;
using TimeWarp.Foundation.Types;

[TestTag("OpenApi")]
public partial class Document_Given_
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

  public static async Task Every_Problem_Arm_Endpoint_Should_Publish_Default_Problem()
  {
    JsonElement paths = (await DocumentRootAsync()).GetProperty("paths");
    List<string> checkedOperations = [];
    List<string> missing = [];
    foreach (RouteEndpoint endpoint in ((IEndpointRouteBuilder)App!).DataSources
      .SelectMany(static source => source.Endpoints)
      .OfType<RouteEndpoint>())
    {
      EndpointDefinition? definition = endpoint.Metadata.GetMetadata<EndpointDefinition>();
      if (definition is null || !HasProblemArm(definition.ResDtoType))
      {
        continue;
      }

      string path = RouteConstraint().Replace("/" + endpoint.RoutePattern.RawText!.TrimStart('/'), "{$1}");
      foreach (string method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
      {
        string label = $"{method} {path}";
        checkedOperations.Add(label);
        if (!TryGetOperation(paths, path, method.ToLowerInvariant(), out JsonElement operation)
          || !operation.GetProperty("responses").TryGetProperty("default", out JsonElement problem)
          || !problem.TryGetProperty("content", out JsonElement content)
          || !content.TryGetProperty(ProblemJson, out JsonElement mediaType)
          || mediaType.GetProperty("schema").GetProperty("$ref").GetString() != ProblemSchema)
        {
          missing.Add(label);
        }
      }
    }

    checkedOperations.Count.ShouldBeGreaterThan(1, "No endpoint with a SharedProblemDetails arm was found.");
    missing.ShouldBeEmpty($"Operations without default -> {ProblemSchema}: {string.Join(", ", missing)}");
  }

  public static async Task Explicit_Problem_Status_Should_Publish_Shared_Problem_Details()
  {
    JsonElement responses = (await OperationAsync(OneOfProbeProblemStatusEndpoint.Route, "get")).GetProperty("responses");

    SchemaRef(responses.GetProperty("422"), ProblemJson).ShouldBe(ProblemSchema);
    SchemaRef(responses.GetProperty("200"), "application/json").ShouldEndWith(nameof(OneOfProbeCat));
    SchemaRef(responses.GetProperty("default"), ProblemJson).ShouldBe(ProblemSchema);
  }

  public static async Task Union_Without_Problem_Arm_Should_Stay_OneOf_At_200()
  {
    JsonElement responses = (await OperationAsync(OneOfProbeNoProblemEndpoint.Route, "get")).GetProperty("responses");

    responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
      .GetProperty("schema").GetProperty("oneOf").EnumerateArray()
      .Select(static arm => arm.GetProperty("$ref").GetString()!)
      .ToList()
      .ShouldSatisfyAllConditions
      (
        static refs => refs.Count.ShouldBe(2),
        static refs => refs[0].ShouldEndWith(nameof(OneOfProbeCat)),
        static refs => refs[1].ShouldEndWith(nameof(OneOfProbeDog))
      );
    responses.TryGetProperty("default", out _).ShouldBeFalse();
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

  /// <summary>
  /// True when <paramref name="type"/> is a OneOf union with a SharedProblemDetails arm.
  /// </summary>
  private static bool HasProblemArm(Type? type)
  {
    for (Type? current = type; current is not null; current = current.BaseType)
    {
      if (current.IsGenericType
        && string.Equals(current.Namespace, typeof(IOneOf).Namespace, StringComparison.Ordinal))
      {
        return current.GetGenericArguments().Contains(typeof(SharedProblemDetails));
      }
    }

    return false;
  }

  [GeneratedRegex(@"\{([^}:=?]+)[^}]*\}")]
  private static partial Regex RouteConstraint();

  private static string SchemaRef(JsonElement response, string mediaType) =>
    response.GetProperty("content").GetProperty(mediaType).GetProperty("schema").GetProperty("$ref").GetString()!;

  private static async Task<JsonElement> OperationAsync(string path, string method)
  {
    JsonElement paths = (await DocumentRootAsync()).GetProperty("paths");
    if (TryGetOperation(paths, path, method, out JsonElement operation))
    {
      return operation;
    }

    string known = string.Join(", ", paths.EnumerateObject().Select(static item => item.Name));
    throw new InvalidOperationException($"Missing {method} {path}. Paths: {known}");
  }

  private static bool TryGetOperation(JsonElement paths, string path, string method, out JsonElement operation)
  {
    foreach (JsonProperty item in paths.EnumerateObject())
    {
      if (string.Equals(item.Name, path, StringComparison.OrdinalIgnoreCase)
        && item.Value.TryGetProperty(method, out operation))
      {
        operation = operation.Clone();
        return true;
      }
    }

    operation = default;
    return false;
  }

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

public sealed class OneOfProbeProblemStatusEndpoint : EndpointWithoutRequest<OneOf<OneOfProbeCat, SharedProblemDetails>>
{
  internal const string Route = "/probe/one-of-problem-status";

  public override void Configure()
  {
    Get(Route);
    AllowAnonymous();
    Description(static builder => builder.ProducesProblem(StatusCodes.Status422UnprocessableEntity));
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class OneOfProbeNoProblemEndpoint : EndpointWithoutRequest<OneOf<OneOfProbeCat, OneOfProbeDog>>
{
  internal const string Route = "/probe/one-of-no-problem";

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
