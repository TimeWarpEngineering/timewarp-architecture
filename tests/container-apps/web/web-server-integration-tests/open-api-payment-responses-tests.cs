#region Purpose
// OpenAPI lists 402 and 503 on the tip jar and metered capability, and leaves free routes without 402.
#endregion

#region Design
// A tiny FastEndpoints host, not the full web graph. CommonServerModule.AddOpenApi is the
// document pipeline, with the same agent-bearer map web-server registers and
// OpenApiPaymentResponses.Register, which is the web host's extra callback.
// Tip GET and POST share /api/tip. Metered GET requires demo:invoke. A free GET must not
// gain 402 or 503. The bare /api alias is not its own operation. The document description
// keeps the free-routes-never-402 statement.
// Stub routes and asserted paths are the contract RouteTemplate values. The host sets
// TipOptions.Resource and MeteredCapabilityOptions.Resource to paths other than those routes,
// and the document must repeat the options values.
#endregion

namespace OpenApiPaymentResponses_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.MeteredCapability;
using TimeWarp.Architecture.Features.Tip;
using TimeWarp.Architecture.Payment;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;
using MeteredCapabilityOptions = TimeWarp.Architecture.Features.MeteredCapability.Application.MeteredCapabilityOptions;
using TipOptions = TimeWarp.Architecture.Features.Tip.Application.TipOptions;

[TestTag("OpenApi")]
public class Document_Given_
{
  private const string ConfiguredTipResource = "/pay/configured-tip";
  private const string ConfiguredMeteredResource = "/pay/configured-meter";
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
      options.Assemblies = [typeof(TipGetEndpoint).Assembly];
      options.Filter = type => type.Namespace == typeof(TipGetEndpoint).Namespace
        && !type.IsAbstract
        && type.Name.EndsWith("Endpoint", StringComparison.Ordinal);
    });
    CommonServerModule.AddOpenApi
    (
      builder.Services,
      "v1",
      "Test API",
      OpenApiAgentBearerRegistration.Create(),
      OpenApiPaymentResponses.Register
    );
    builder.Services.Configure<TipOptions>(static options => options.Resource = ConfiguredTipResource);
    builder.Services.Configure<MeteredCapabilityOptions>(static options => options.Resource = ConfiguredMeteredResource);
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

  public static async Task Info_Should_SayFreeRoutesNeverReturn402()
  {
    using JsonDocument document = await DocumentAsync();
    string description = document.RootElement.GetProperty("info").GetProperty("description").GetString()!;
    description.ShouldContain("never return HTTP 402");
    description.ShouldContain(TipPath);
    description.ShouldContain(MeteredPath);
    description.ShouldContain("bare /api");
    description.ShouldContain("discovery alias");
    document.RootElement.GetProperty("paths").TryGetProperty("/api", out _).ShouldBeFalse();
  }

  public static async Task TipGet_Should_Document402And503()
  {
    JsonElement operation = await OperationAsync(TipPath, "get");
    operation.TryGetProperty("security", out _).ShouldBeFalse();
    AssertPaidResponses(operation, TipResource(), includeTip: true);
  }

  public static async Task TipPost_Should_Document402And503()
  {
    TipPostPath.ShouldBe(TipPath);
    JsonElement operation = await OperationAsync(TipPostPath, "post");
    operation.TryGetProperty("security", out _).ShouldBeFalse();
    AssertPaidResponses(operation, TipResource(), includeTip: true);
  }

  public static async Task Metered_Should_Document402503AndDemoInvoke()
  {
    JsonElement operation = await OperationAsync(MeteredPath, "get");
    JsonElement scopes = operation.GetProperty("security")[0].GetProperty("JWTBearerAuth");
    scopes.GetArrayLength().ShouldBe(1);
    scopes[0].GetString().ShouldBe("demo:invoke");
    operation.GetProperty("description").GetString()!.ShouldContain("demo:invoke");
    operation.GetProperty("responses").GetProperty("200").GetProperty("description").GetString()!.ShouldContain("balanceAfter");
    AssertPaidResponses(operation, MeteredResource(), includeTip: false);
  }

  public static async Task ConfiguredResource_Should_DocumentTheOptionsValue()
  {
    string tipResource = TipResource();
    string meteredResource = MeteredResource();
    tipResource.ShouldBe(ConfiguredTipResource);
    meteredResource.ShouldBe(ConfiguredMeteredResource);
    tipResource.ShouldNotBe(TipPath);
    meteredResource.ShouldNotBe(MeteredPath);

    JsonElement tip = await OperationAsync(TipPath, "get");
    string tipChallenge = tip.GetProperty("responses").GetProperty("402").GetProperty("description").GetString()!;
    tip.GetProperty("description").GetString()!.ShouldContain(tipResource);
    tipChallenge.ShouldContain(tipResource);
    tipChallenge.ShouldNotContain(TipPath);

    JsonElement metered = await OperationAsync(MeteredPath, "get");
    string meteredChallenge = metered.GetProperty("responses").GetProperty("402").GetProperty("description").GetString()!;
    meteredChallenge.ShouldContain(meteredResource);
    meteredChallenge.ShouldNotContain(MeteredPath);
  }

  public static async Task FreeRoute_Should_Omit402And503()
  {
    JsonElement operation = await OperationAsync("/api/hello", "get");
    JsonElement responses = operation.GetProperty("responses");
    responses.TryGetProperty("402", out _).ShouldBeFalse();
    responses.TryGetProperty("503", out _).ShouldBeFalse();
  }

  private static void AssertPaidResponses(JsonElement operation, string resource, bool includeTip)
  {
    operation.GetProperty("description").GetString()!.ShouldContain("never return 402");
    JsonElement responses = operation.GetProperty("responses");

    JsonElement paymentRequired = responses.GetProperty("402");
    string challenge = paymentRequired.GetProperty("description").GetString()!;
    challenge.ShouldContain("PAYMENT-REQUIRED");
    challenge.ShouldContain("x402Version");
    challenge.ShouldContain("payTo");
    challenge.ShouldContain(resource);
    paymentRequired.GetProperty("content").GetProperty("application/problem+json").GetProperty("schema")
      .GetProperty("properties").TryGetProperty("payment", out _).ShouldBeTrue();
    paymentRequired.GetProperty("content").GetProperty("application/problem+json").GetProperty("schema")
      .GetProperty("properties").TryGetProperty("tip", out _).ShouldBe(includeTip);

    JsonElement header = paymentRequired.GetProperty("headers").GetProperty("PAYMENT-REQUIRED");
    header.GetProperty("required").GetBoolean().ShouldBeTrue();
    string headerText = header.GetRawText();
    headerText.ShouldContain("x402Version");
    headerText.ShouldContain("accepts");
    headerText.ShouldContain("payTo");
    headerText.ShouldContain("base64");

    JsonElement unavailable = responses.GetProperty("503");
    string unavailableText = unavailable.GetProperty("description").GetString()!;
    unavailableText.ShouldContain("disabled");
    unavailableText.ShouldContain("402");
    unavailableText.ShouldContain("never return 402");
    if (unavailable.TryGetProperty("headers", out JsonElement headers))
    {
      headers.TryGetProperty("PAYMENT-REQUIRED", out _).ShouldBeFalse();
    }

    JsonElement unavailableProperties = unavailable.GetProperty("content").GetProperty("application/problem+json")
      .GetProperty("schema").GetProperty("properties");
    unavailableProperties.TryGetProperty("error", out _).ShouldBeTrue();
    unavailableProperties.TryGetProperty("payment", out _).ShouldBeTrue();
    unavailableProperties.TryGetProperty("ok", out _).ShouldBeTrue();
    unavailableProperties.TryGetProperty("tip", out _).ShouldBe(includeTip);
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

  private static string OpenApiPath(string routeTemplate) =>
    routeTemplate.StartsWith('/', StringComparison.Ordinal) ? routeTemplate : "/" + routeTemplate;

  private static string TipPath => OpenApiPath(SubmitTip.Query.RouteTemplate);

  private static string TipPostPath => OpenApiPath(SubmitTipPost.Command.RouteTemplate);

  private static string MeteredPath => OpenApiPath(InvokeMeteredCapability.Query.RouteTemplate);

  private static string TipResource() =>
    App!.Services.GetRequiredService<IOptions<TipOptions>>().Value.Resource;

  private static string MeteredResource() =>
    App!.Services.GetRequiredService<IOptions<MeteredCapabilityOptions>>().Value.Resource;
}

public sealed class TipGetEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get(SubmitTip.Query.RouteTemplate);
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
    Post(SubmitTipPost.Command.RouteTemplate);
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class MeteredCapabilityEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get(InvokeMeteredCapability.Query.RouteTemplate);
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.DemoInvoke);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class HelloEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/hello");
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}
