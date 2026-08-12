#region Purpose
// HttpFacilitatorClient verify/settle/supported against a stub HttpMessageHandler (no live network).
#endregion

#region Design
// Proves the HTTP client maps facilitator JSON correctly and never requires a real facilitator URL
// to respond — CI-safe companion to the pure MockFacilitator used by PaymentGate tests.
#endregion

namespace HttpFacilitatorClient_;

using System.Net;
using System.Net.Http.Json;

public class VerifySettleSupported
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<VerifySettleSupported>();

  public static async Task Verify_deserializes_isValid_body()
  {
    RecordingHandler handler = new((request, _) =>
    {
      request.Method.ShouldBe(HttpMethod.Post);
      request.RequestUri!.AbsolutePath.ShouldEndWith("/verify");
      return Task.FromResult(JsonResponse(HttpStatusCode.OK, new { isValid = true }));
    });

    using HttpFacilitatorClient client = CreateClient(handler);
    FacilitatorVerifyResult result = await client.VerifyAsync(SampleRequest());

    result.IsValid.ShouldBeTrue();
    result.InvalidReason.ShouldBeNull();
  }

  public static async Task Verify_maps_http_error_when_body_empty()
  {
    RecordingHandler handler = new((_, _) =>
      Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)));

    using HttpFacilitatorClient client = CreateClient(handler);
    FacilitatorVerifyResult result = await client.VerifyAsync(SampleRequest());

    result.IsValid.ShouldBeFalse();
    result.InvalidReason.ShouldBe("facilitator_http_502");
  }

  public static async Task Settle_deserializes_success_body()
  {
    RecordingHandler handler = new((request, _) =>
    {
      request.Method.ShouldBe(HttpMethod.Post);
      request.RequestUri!.AbsolutePath.ShouldEndWith("/settle");
      return Task.FromResult(JsonResponse(HttpStatusCode.OK, new
      {
        success = true,
        transaction = "0xhttp-settle",
        network = "eip155:84532",
        payer = "0x000000000000000000000000000000000000dEaD",
      }));
    });

    using HttpFacilitatorClient client = CreateClient(handler);
    FacilitatorSettleResult result = await client.SettleAsync(SampleRequest());

    result.Success.ShouldBeTrue();
    result.Transaction.ShouldBe("0xhttp-settle");
    result.Network.ShouldBe("eip155:84532");
  }

  public static async Task Supported_returns_kinds()
  {
    RecordingHandler handler = new((request, _) =>
    {
      request.Method.ShouldBe(HttpMethod.Get);
      request.RequestUri!.AbsolutePath.ShouldEndWith("/supported");
      return Task.FromResult(JsonResponse(HttpStatusCode.OK, new
      {
        kinds = new[]
        {
          new { x402Version = 2, scheme = "exact", network = "eip155:84532" },
        },
      }));
    });

    using HttpFacilitatorClient client = CreateClient(handler);
    FacilitatorSupported supported = await client.GetSupportedAsync();

    supported.Kinds.Count.ShouldBe(1);
    supported.Kinds[0].Scheme.ShouldBe("exact");
    supported.Kinds[0].Network.ShouldBe("eip155:84532");
  }

  public static async Task Auth_header_factory_is_applied()
  {
    string? seenAuth = null;
    RecordingHandler handler = new((request, _) =>
    {
      if (request.Headers.TryGetValues("Authorization", out IEnumerable<string>? values))
      {
        seenAuth = values.Single();
      }

      return Task.FromResult(JsonResponse(HttpStatusCode.OK, new { isValid = true }));
    });

    using HttpClient http = new(handler) { BaseAddress = new Uri("https://facilitator.test/") };
    using HttpFacilitatorClient client = new(
      "https://facilitator.test",
      http,
      createAuthHeaders: (_, _) => Task.FromResult<IReadOnlyDictionary<string, string>>(
        new Dictionary<string, string> { ["Authorization"] = "Bearer test-token" }));

    await client.VerifyAsync(SampleRequest());
    seenAuth.ShouldBe("Bearer test-token");
  }

  private static HttpFacilitatorClient CreateClient(HttpMessageHandler handler)
  {
    HttpClient http = new(handler) { BaseAddress = new Uri("https://facilitator.test/") };
    return new HttpFacilitatorClient("https://facilitator.test", http);
  }

  private static FacilitatorPaymentRequest SampleRequest()
  {
    using JsonDocument payload = JsonDocument.Parse("""{"x402Version":2}""");
    using JsonDocument requirements = JsonDocument.Parse("""{"scheme":"exact"}""");
    return new FacilitatorPaymentRequest
    {
      X402Version = 2,
      PaymentPayload = payload.RootElement.Clone(),
      PaymentRequirements = requirements.RootElement.Clone(),
    };
  }

  private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body) =>
    new(status)
    {
      Content = JsonContent.Create(body),
    };

  private sealed class RecordingHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Responder;

    public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
      Responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken) =>
      Responder(request, cancellationToken);
  }
}
