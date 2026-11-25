#region Purpose
// The OpenAPI document has one problem details schema, and every problem+json response references it.
#endregion

#region Design
// Task 458-004. The host is the OpenAPI pipeline only: FastEndpoints discovers the web-server
// endpoints plus the probes in this file, with the agent bearer and session cookie registrations
// web-server passes, so bearer 401 and 403 are in the scan. No mediator graph.
// The SharedProblemDetails component must declare errors (map of string arrays) and allow
// extension members. Every problem+json response, on every operation, must be a $ref to it.
// A probe declares ProducesProblem, which registers MVC ProblemDetails; once the OneOf
// transformer rewrites that response, no other *ProblemDetails component may remain.
// A request type with a registered IValidator documents 400, as FluentValidationBehavior returns
// it; the host registers one probe validator, as web-server's AddValidatorsFromAssemblyContaining
// does for contract validators. A request type without one has no 400.
#endregion

namespace OpenApiProblemDetails_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneOf;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;
using TimeWarp.Foundation.Types;

[TestTag("OpenApi")]
public class Document_Given_
{
  private const string ProblemJson = "application/problem+json";
  private const string ProblemComponent = "TimeWarpFoundationTypesSharedProblemDetails";
  private const string ProblemSchema = "#/components/schemas/" + ProblemComponent;

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
      options.Assemblies = [typeof(Program).Assembly, typeof(ProblemProbeValidatedEndpoint).Assembly];
      options.Filter = type => type.Assembly == typeof(Program).Assembly
        || type.Namespace == typeof(ProblemProbeValidatedEndpoint).Namespace;
    });
    builder.Services.AddScoped<IValidator<ProblemProbeRequest>, ProblemProbeRequestValidator>();
    CommonServerModule.AddOpenApi
    (
      builder.Services,
      "v1",
      "Test API",
      OpenApiAgentBearerRegistration.Create(),
      sessionCookie: OpenApiSessionCookieRegistration.Create()
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

  public static async Task Shared_Component_Should_Declare_Errors_And_Allow_Extensions()
  {
    JsonElement problem = (await DocumentRootAsync()).GetProperty("components").GetProperty("schemas")
      .GetProperty(ProblemComponent);

    JsonElement properties = problem.GetProperty("properties");
    foreach (string member in new[] { "type", "title", "status", "detail", "instance", "errors" })
    {
      properties.TryGetProperty(member, out _).ShouldBeTrue(member);
    }

    JsonElement errors = properties.GetProperty("errors");
    errors.GetProperty("type").GetString().ShouldBe("object");
    JsonElement messages = errors.GetProperty("additionalProperties");
    messages.GetProperty("type").GetString().ShouldBe("array");
    messages.GetProperty("items").GetProperty("type").GetString().ShouldBe("string");
    problem.GetProperty("additionalProperties").ValueKind
      .ShouldBe(JsonValueKind.Object, "Extension members such as traceId must be allowed.");
  }

  public static async Task No_Other_Problem_Details_Component_Should_Remain()
  {
    List<string> names = (await DocumentRootAsync()).GetProperty("components").GetProperty("schemas")
      .EnumerateObject()
      .Select(static schema => schema.Name)
      .Where(static name => name.EndsWith("ProblemDetails", StringComparison.Ordinal))
      .ToList();

    names.ShouldBe([ProblemComponent]);
  }

  public static async Task Every_Problem_Json_Response_Should_Reference_The_Component()
  {
    JsonElement document = await DocumentRootAsync();
    HashSet<string> statuses = new(StringComparer.Ordinal);
    List<string> wrong = [];
    foreach (JsonProperty path in document.GetProperty("paths").EnumerateObject())
    {
      foreach (JsonProperty operation in path.Value.EnumerateObject())
      {
        if (!operation.Value.TryGetProperty("responses", out JsonElement responses))
        {
          continue;
        }

        foreach (JsonProperty response in responses.EnumerateObject())
        {
          if (!response.Value.TryGetProperty("content", out JsonElement content)
            || !content.TryGetProperty(ProblemJson, out JsonElement mediaType))
          {
            continue;
          }

          statuses.Add(response.Name);
          if (!mediaType.GetProperty("schema").TryGetProperty("$ref", out JsonElement reference)
            || reference.GetString() != ProblemSchema)
          {
            wrong.Add($"{operation.Name} {path.Name} {response.Name}");
          }
        }
      }
    }

    wrong.ShouldBeEmpty($"problem+json responses without $ref {ProblemSchema}: {string.Join(", ", wrong)}");
    foreach (string status in new[] { "default", "400", "401", "403", "422" })
    {
      statuses.ShouldContain(status, $"No problem+json {status} response was scanned.");
    }
  }

  public static async Task Validated_Request_Should_Publish_400_Problem()
  {
    JsonElement badRequest = (await ResponsesAsync(ProblemProbeValidatedEndpoint.Route, "post")).GetProperty("400");

    badRequest.GetProperty("content").GetProperty(ProblemJson).GetProperty("schema").GetProperty("$ref").GetString()
      .ShouldBe(ProblemSchema);
    badRequest.GetProperty("description").GetString()!.ShouldContain("errors");
  }

  public static async Task Request_Without_Validator_Should_Not_Publish_400()
  {
    JsonElement responses = await ResponsesAsync(ProblemProbeExplicitStatusEndpoint.Route, "get");

    responses.TryGetProperty("400", out _).ShouldBeFalse();
  }

  private static async Task<JsonElement> ResponsesAsync(string path, string method)
  {
    JsonElement paths = (await DocumentRootAsync()).GetProperty("paths");
    return paths.GetProperty(path).GetProperty(method).GetProperty("responses").Clone();
  }

  private static async Task<JsonElement> DocumentRootAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    string json = await response.Content.ReadAsStringAsync();
    // The document check throws; the developer exception page carries its message here.
    response.StatusCode.ShouldBe(HttpStatusCode.OK, json[..Math.Min(json.Length, 2000)]);
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }
}

public sealed class ProblemProbeRequest
{
  public string Name { get; set; } = null!;
}

public sealed class ProblemProbeResponse
{
  public string Message { get; set; } = string.Empty;
}

public sealed class ProblemProbeRequestValidator : AbstractValidator<ProblemProbeRequest>
{
  public ProblemProbeRequestValidator()
  {
    RuleFor(static request => request.Name).NotEmpty();
  }
}

public sealed class ProblemProbeValidatedEndpoint : Endpoint<ProblemProbeRequest, OneOf<ProblemProbeResponse, SharedProblemDetails>>
{
  internal const string Route = "/probe/problem-validated";

  public override void Configure()
  {
    Post(Route);
    AllowAnonymous();
  }

  public override Task HandleAsync(ProblemProbeRequest req, CancellationToken ct)
  {
    _ = req;
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class ProblemProbeExplicitStatusEndpoint : EndpointWithoutRequest<OneOf<ProblemProbeResponse, SharedProblemDetails>>
{
  internal const string Route = "/probe/problem-explicit-status";

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
