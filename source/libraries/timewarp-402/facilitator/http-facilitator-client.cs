#region Purpose
// HTTP facilitator client for x402.org-shaped /verify, /settle, /supported endpoints.
#endregion

#region Design
// Wire body matches @x402/core HTTPFacilitatorClient: JSON with x402Version, paymentPayload,
// paymentRequirements. Auth is optional via createAuthHeaders — CDP JWT production stays in the
// host (or a future thin adapter); this type only applies whatever headers the factory returns.
// No merchant keys. No default network traffic in unit tests — inject HttpMessageHandler.
#endregion

namespace TimeWarp.X402;

using System.Net.Http.Json;
using System.Text.Json;
/// <summary>HTTP client for public or authenticated facilitator base URLs.</summary>
public sealed class HttpFacilitatorClient : IFacilitatorClient, IDisposable
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  private readonly HttpClient Http;
  private readonly bool OwnsHttp;
  private readonly Func<string, CancellationToken, Task<IReadOnlyDictionary<string, string>>>? CreateAuthHeaders;

  /// <param name="facilitatorBase">Facilitator root (trailing slash stripped). Wire string from config.</param>
  /// <param name="httpClient">Optional shared client; when null a private client is created.</param>
  /// <param name="createAuthHeaders">Optional auth header factory keyed by path segment (verify|settle|supported).</param>
  public HttpFacilitatorClient(
    string facilitatorBase,
    HttpClient? httpClient = null,
    Func<string, CancellationToken, Task<IReadOnlyDictionary<string, string>>>? createAuthHeaders = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(facilitatorBase);

    string trimmed = facilitatorBase.TrimEnd('/');
    CreateAuthHeaders = createAuthHeaders;

    if (httpClient is null)
    {
      Http = new HttpClient { BaseAddress = new Uri(trimmed + "/") };
      OwnsHttp = true;
    }
    else
    {
      Http = httpClient;
      if (Http.BaseAddress is null)
      {
        Http.BaseAddress = new Uri(trimmed + "/");
      }

      OwnsHttp = false;
    }
  }

  public async Task<FacilitatorSupported> GetSupportedAsync(CancellationToken cancellationToken = default)
  {
    using HttpRequestMessage request = new(HttpMethod.Get, "supported");
    await ApplyAuthAsync(request, "supported", cancellationToken).ConfigureAwait(false);

    using HttpResponseMessage response = await Http
      .SendAsync(request, cancellationToken)
      .ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    FacilitatorSupported? body = await response.Content
      .ReadFromJsonAsync<FacilitatorSupported>(JsonOptions, cancellationToken)
      .ConfigureAwait(false);

    return body ?? new FacilitatorSupported();
  }

  public async Task<FacilitatorVerifyResult> VerifyAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    using HttpRequestMessage httpRequest = new(HttpMethod.Post, "verify")
    {
      Content = JsonContent.Create(request, options: JsonOptions),
    };
    await ApplyAuthAsync(httpRequest, "verify", cancellationToken).ConfigureAwait(false);

    using HttpResponseMessage response = await Http
      .SendAsync(httpRequest, cancellationToken)
      .ConfigureAwait(false);

    FacilitatorVerifyResult? body = await response.Content
      .ReadFromJsonAsync<FacilitatorVerifyResult>(JsonOptions, cancellationToken)
      .ConfigureAwait(false);

    if (body is not null)
    {
      return body;
    }

    if (!response.IsSuccessStatusCode)
    {
      return new FacilitatorVerifyResult
      {
        IsValid = false,
        InvalidReason = $"facilitator_http_{(int)response.StatusCode}",
      };
    }

    return new FacilitatorVerifyResult { IsValid = false, InvalidReason = "empty_verify_response" };
  }

  public async Task<FacilitatorSettleResult> SettleAsync(
    FacilitatorPaymentRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    using HttpRequestMessage httpRequest = new(HttpMethod.Post, "settle")
    {
      Content = JsonContent.Create(request, options: JsonOptions),
    };
    await ApplyAuthAsync(httpRequest, "settle", cancellationToken).ConfigureAwait(false);

    using HttpResponseMessage response = await Http
      .SendAsync(httpRequest, cancellationToken)
      .ConfigureAwait(false);

    FacilitatorSettleResult? body = await response.Content
      .ReadFromJsonAsync<FacilitatorSettleResult>(JsonOptions, cancellationToken)
      .ConfigureAwait(false);

    if (body is not null)
    {
      return body;
    }

    if (!response.IsSuccessStatusCode)
    {
      return new FacilitatorSettleResult
      {
        Success = false,
        ErrorReason = $"facilitator_http_{(int)response.StatusCode}",
      };
    }

    return new FacilitatorSettleResult { Success = false, ErrorReason = "empty_settle_response" };
  }

  private async Task ApplyAuthAsync(
    HttpRequestMessage request,
    string path,
    CancellationToken cancellationToken)
  {
    if (CreateAuthHeaders is null)
    {
      return;
    }

    IReadOnlyDictionary<string, string> headers = await CreateAuthHeaders(path, cancellationToken)
      .ConfigureAwait(false);
    foreach ((string key, string value) in headers)
    {
      request.Headers.TryAddWithoutValidation(key, value);
    }
  }

  public void Dispose()
  {
    if (OwnsHttp)
    {
      Http.Dispose();
    }
  }
}
