#region Purpose
// OpenAPI lists 402 and 503 on the tip jar and metered capability, and leaves free routes without 402.
#endregion

#region Design
// A tiny FastEndpoints host, not the full web graph. CommonServerModule.AddOpenApi is the
// document pipeline, with the same agent-bearer map web-server registers and
// OpenApiPaymentResponses.Register, which is the web host's extra callback.
// documentPublicWebRoutes is on, as on web-server, so the /api alias and /api/health are in
// the document. Tip GET and POST share /api/tip and are anonymous, so each publishes security: [].
// Metered GET requires demo:invoke. A free GET must not gain 402 or 503. The alias operations
// carry the same responses and parameters as the tip operations (task 458-009). Every paid
// operation lists PAYMENT-SIGNATURE (request) and PAYMENT-RESPONSE (200). The payment note is its
// own info.description paragraph, its 402 list is the operations with a 402, and the health 503
// is named as not a payment response. Document checks only: no paid route is called.
// Stub routes and asserted paths are the contract RouteTemplate values. The host sets
// TipOptions.Resource and MeteredCapabilityOptions.Resource to paths other than those routes,
// and the document must repeat the options values. Each 402 and 503 body is allOf over the one
// SharedProblemDetails component and the payment extension members (task 458-004).
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
using TimeWarp.X402;
using MeteredCapabilityOptions = TimeWarp.Architecture.Features.MeteredCapability.Application.MeteredCapabilityOptions;
using TipOptions = TimeWarp.Architecture.Features.Tip.Application.TipOptions;

[TestTag("OpenApi")]
public class Document_Given_
{
  private const string ProblemSchema = "#/components/schemas/TimeWarpFoundationTypesSharedProblemDetails";
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
      documentPublicWebRoutes: true,
      configureOpenApi: OpenApiPaymentResponses.Register
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
  }

  public static async Task Info_Should_PutThePaymentNoteInItsOwnParagraph()
  {
    using JsonDocument document = await DocumentAsync();
    string description = document.RootElement.GetProperty("info").GetProperty("description").GetString()!;
    description.ShouldContain("## Routes outside this document");
    int heading = description.IndexOf("\n\n## Payment\n\n", StringComparison.Ordinal);
    heading.ShouldBeGreaterThan(0);
    description.IndexOf("## Routes outside this document", StringComparison.Ordinal).ShouldBeLessThan(heading);
  }

  public static async Task Info_Should_ListExactlyTheOperationsThatDocument402()
  {
    using JsonDocument document = await DocumentAsync();
    JsonElement root = document.RootElement;
    string description = root.GetProperty("info").GetProperty("description").GetString()!;
    const string lead = "documented only on ";
    int start = description.IndexOf(lead, StringComparison.Ordinal) + lead.Length;
    string list = description[start..description.IndexOf(". ", start, StringComparison.Ordinal)];

    List<string> expected = [];
    foreach (JsonProperty path in root.GetProperty("paths").EnumerateObject())
    {
      foreach (JsonProperty operation in path.Value.EnumerateObject())
      {
        string name = "`" + operation.Name.ToUpperInvariant() + " " + path.Name + "`";
        if (operation.Value.GetProperty("responses").TryGetProperty("402", out _))
        {
          expected.Add(name);
          list.ShouldContain(name);
        }
        else
        {
          list.ShouldNotContain(name);
        }
      }
    }

    expected.ShouldBe
    (
      ["`GET /api`", "`POST /api`", "`GET " + TipPath + "`", "`POST " + TipPath + "`", "`GET " + MeteredPath + "`"],
      ignoreOrder: true
    );
  }

  public static async Task Info_Should_SayTheHealth503IsNotPayment()
  {
    using JsonDocument document = await DocumentAsync();
    JsonElement root = document.RootElement;
    root.GetProperty("paths").GetProperty("/api/health").GetProperty("get").GetProperty("responses")
      .TryGetProperty("503", out _).ShouldBeTrue();
    string description = root.GetProperty("info").GetProperty("description").GetString()!;
    description.ShouldContain("The 503 on `GET /api/health` is that route's own status");
    description.ShouldContain("not a payment response");
  }

  public static async Task TipAlias_Should_CopyTheTipResponsesAndParameters()
  {
    foreach (string method in new[] { "get", "post" })
    {
      JsonElement alias = await OperationAsync("/api", method);
      JsonElement tip = await OperationAsync(TipPath, method);
      alias.GetProperty("responses").GetRawText().ShouldBe(tip.GetProperty("responses").GetRawText());
      alias.GetProperty("parameters").GetRawText().ShouldBe(tip.GetProperty("parameters").GetRawText());
      alias.GetProperty("security").GetArrayLength().ShouldBe(0);
      AssertPaidResponses(alias, TipResource(), includeTip: true, checkDescription: false);
      AssertPaymentHeaders(alias);
    }
  }

  public static async Task PaidOperations_Should_DocumentSignatureAndSettlementHeaders()
  {
    AssertPaymentHeaders(await OperationAsync(TipPath, "get"));
    AssertPaymentHeaders(await OperationAsync(TipPostPath, "post"));
    AssertPaymentHeaders(await OperationAsync(MeteredPath, "get"));

    JsonElement free = await OperationAsync("/api/hello", "get");
    free.GetRawText().ShouldNotContain("PAYMENT-SIGNATURE");
    free.GetRawText().ShouldNotContain("PAYMENT-RESPONSE");
  }

  public static async Task TipGet_Should_Document402And503()
  {
    JsonElement operation = await OperationAsync(TipPath, "get");
    operation.GetProperty("security").GetArrayLength().ShouldBe(0);
    AssertPaidResponses(operation, TipResource(), includeTip: true);
  }

  public static async Task TipPost_Should_Document402And503()
  {
    TipPostPath.ShouldBe(TipPath);
    JsonElement operation = await OperationAsync(TipPostPath, "post");
    operation.GetProperty("security").GetArrayLength().ShouldBe(0);
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

  private static void AssertPaymentHeaders(JsonElement operation)
  {
    JsonElement signature = operation.GetProperty("parameters").EnumerateArray()
      .Single(parameter => parameter.GetProperty("name").GetString() == PaymentHeaders.PaymentSignature);
    signature.GetProperty("in").GetString().ShouldBe("header");
    if (signature.TryGetProperty("required", out JsonElement signatureRequired))
    {
      signatureRequired.GetBoolean().ShouldBeFalse();
    }

    signature.GetRawText().ShouldContain("base64");

    JsonElement settlement = operation.GetProperty("responses").GetProperty("200").GetProperty("headers")
      .GetProperty(PaymentHeaders.PaymentResponse);
    if (settlement.TryGetProperty("required", out JsonElement settlementRequired))
    {
      settlementRequired.GetBoolean().ShouldBeFalse();
    }

    settlement.GetRawText().ShouldContain("base64");
  }

  private static void AssertPaidResponses(JsonElement operation, string resource, bool includeTip, bool checkDescription = true)
  {
    if (checkDescription)
    {
      operation.GetProperty("description").GetString()!.ShouldContain("never return 402");
    }

    JsonElement responses = operation.GetProperty("responses");

    JsonElement paymentRequired = responses.GetProperty("402");
    string challenge = paymentRequired.GetProperty("description").GetString()!;
    challenge.ShouldContain("PAYMENT-REQUIRED");
    challenge.ShouldContain("x402Version");
    challenge.ShouldContain("payTo");
    challenge.ShouldContain(resource);
    JsonElement paymentMembers = ProblemMembers(paymentRequired);
    paymentMembers.TryGetProperty("payment", out _).ShouldBeTrue();
    paymentMembers.TryGetProperty("reason", out _).ShouldBeTrue();
    paymentMembers.TryGetProperty("tip", out _).ShouldBe(includeTip);

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

    JsonElement unavailableProperties = ProblemMembers(unavailable);
    unavailableProperties.TryGetProperty("error", out _).ShouldBeTrue();
    unavailableProperties.TryGetProperty("payment", out _).ShouldBeTrue();
    unavailableProperties.TryGetProperty("ok", out _).ShouldBeTrue();
    unavailableProperties.TryGetProperty("tip", out _).ShouldBe(includeTip);
  }

  /// <summary>
  /// The extension members of a payment problem body. The schema is allOf: the one
  /// SharedProblemDetails component first, then the members the payment gate adds.
  /// </summary>
  private static JsonElement ProblemMembers(JsonElement response)
  {
    JsonElement allOf = response.GetProperty("content").GetProperty("application/problem+json")
      .GetProperty("schema").GetProperty("allOf");
    allOf.GetArrayLength().ShouldBe(2);
    allOf[0].GetProperty("$ref").GetString().ShouldBe(ProblemSchema);
    return allOf[1].GetProperty("properties");
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
