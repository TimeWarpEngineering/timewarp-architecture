#region Purpose
// Thin HTTP client for agent identity + metered capability endpoints; surfaces non-2xx as status + raw body.
#endregion
#region Design
// CLI-local paths (not generated RouteTemplates) so this tool has zero web-contracts coupling.
// On non-2xx: return status + raw body (problem details) — never hide errors. Empty-body POSTs
// send "{}" because some hosts reject a truly empty application/json body.
// Metered GET (104-014) returns headers (PAYMENT-REQUIRED / PAYMENT-RESPONSE) for the money path;
// x402 header names are protocol constants (PAYMENT-*) — hard-coded here, not a TimeWarp.X402 ref.
#endregion

namespace AgentIdentityCli.Services;

internal sealed class AgentHttpClient
{
  public const string RegisterOptionsPath = "/api/identity/agent/register/options";
  public const string RegisterPath = "/api/identity/agent/register";
  public const string TokenOptionsPath = "/api/identity/agent/token/options";
  public const string TokenPath = "/api/identity/agent/token";
  public const string MePath = "/api/identity/agent/me";
  public const string MeteredCapabilityPath = "/api/demo/metered-capability";

  /// <summary>x402 v2 challenge header (server → client). Matches TimeWarp.X402.PaymentHeaders.</summary>
  public const string PaymentRequiredHeader = "PAYMENT-REQUIRED";

  /// <summary>x402 v2 payment payload header (client → server).</summary>
  public const string PaymentSignatureHeader = "PAYMENT-SIGNATURE";

  /// <summary>x402 v2 settlement response header (server → client).</summary>
  public const string PaymentResponseHeader = "PAYMENT-RESPONSE";

  private readonly CliJson Json;

  public AgentHttpClient(CliJson json)
  {
    Json = json;
  }

  public Task<HttpResult<ChallengeResponse>> PostRegisterOptionsAsync(string server, CancellationToken ct)
    => PostAsync<ChallengeResponse>(server, RegisterOptionsPath, body: new { }, bearer: null, ct);

  public Task<HttpResult<RegisterResponse>> PostRegisterAsync(string server, RegisterRequest request, CancellationToken ct)
    => PostAsync<RegisterResponse>(server, RegisterPath, request, bearer: null, ct);

  public Task<HttpResult<ChallengeResponse>> PostTokenOptionsAsync(string server, CancellationToken ct)
    => PostAsync<ChallengeResponse>(server, TokenOptionsPath, body: new { }, bearer: null, ct);

  public Task<HttpResult<TokenResponse>> PostTokenAsync(string server, TokenRequest request, CancellationToken ct)
    => PostAsync<TokenResponse>(server, TokenPath, request, bearer: null, ct);

  public Task<HttpResult<WhoAmIResponse>> GetMeAsync(string server, string accessToken, CancellationToken ct)
    => GetAsync<WhoAmIResponse>(server, MePath, accessToken, ct);

  /// <summary>
  /// GET metered capability with optional PAYMENT-SIGNATURE. Always returns status + headers + body
  /// (including 402) so callers can narrate the money path without treating 402 as a hard fail.
  /// </summary>
  public async Task<MeteredHttpResult> GetMeteredAsync(
    string server,
    string accessToken,
    string? paymentSignature,
    CancellationToken ct)
  {
    using HttpClient client = CreateClient(server);
    using HttpRequestMessage request = new(HttpMethod.Get, MeteredCapabilityPath);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    if (!string.IsNullOrWhiteSpace(paymentSignature))
    {
      request.Headers.TryAddWithoutValidation(PaymentSignatureHeader, paymentSignature);
    }

    using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
    string raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    string? paymentRequired = ReadHeader(response, PaymentRequiredHeader);
    string? paymentResponse = ReadHeader(response, PaymentResponseHeader);
    MeteredCapabilityWireResponse? value = null;
    if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(raw))
    {
      value = Json.Deserialize<MeteredCapabilityWireResponse>(raw);
    }

    return new MeteredHttpResult(
      (int)response.StatusCode,
      response.IsSuccessStatusCode,
      value,
      raw,
      paymentRequired,
      paymentResponse);
  }

  private async Task<HttpResult<T>> PostAsync<T>(string server, string path, object? body, string? bearer, CancellationToken ct)
  {
    using HttpClient client = CreateClient(server);
    using HttpRequestMessage request = new(HttpMethod.Post, path);
    if (bearer is not null)
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
    }

    string json = body is null ? "{}" : Json.Serialize(body);
    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

    using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
    string raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    return ParseResult<T>(response, raw);
  }

  private async Task<HttpResult<T>> GetAsync<T>(string server, string path, string? bearer, CancellationToken ct)
  {
    using HttpClient client = CreateClient(server);
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    if (bearer is not null)
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
    }

    using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
    string raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    return ParseResult<T>(response, raw);
  }

  private HttpResult<T> ParseResult<T>(HttpResponseMessage response, string raw)
  {
    int status = (int)response.StatusCode;
    if (!response.IsSuccessStatusCode)
    {
      return HttpResult<T>.Fail(status, raw);
    }

    T? value = string.IsNullOrWhiteSpace(raw) ? default : Json.Deserialize<T>(raw);
    if (value is null)
    {
      return HttpResult<T>.Fail(status, string.IsNullOrWhiteSpace(raw) ? "(empty success body)" : raw);
    }

    return HttpResult<T>.Ok(status, value, raw);
  }

  private static string? ReadHeader(HttpResponseMessage response, string name) =>
    response.Headers.TryGetValues(name, out System.Collections.Generic.IEnumerable<string>? values)
      ? values.FirstOrDefault()
      : null;

  private static HttpClient CreateClient(string server)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(server);
    string baseUrl = server.TrimEnd('/') + "/";
    return new HttpClient
    {
      BaseAddress = new Uri(baseUrl, UriKind.Absolute),
      Timeout = TimeSpan.FromSeconds(60)
    };
  }
}

internal sealed class HttpResult<T>
{
  private HttpResult(bool success, int statusCode, T? value, string rawBody)
  {
    Success = success;
    StatusCode = statusCode;
    Value = value;
    RawBody = rawBody;
  }

  public bool Success { get; }
  public int StatusCode { get; }
  public T? Value { get; }
  public string RawBody { get; }

  public static HttpResult<T> Ok(int statusCode, T value, string rawBody) => new(true, statusCode, value, rawBody);
  public static HttpResult<T> Fail(int statusCode, string rawBody) => new(false, statusCode, default, rawBody);
}

/// <summary>Raw metered GET outcome including x402 headers (402 is a normal step on the money path).</summary>
internal sealed class MeteredHttpResult
{
  public MeteredHttpResult(
    int statusCode,
    bool success,
    MeteredCapabilityWireResponse? value,
    string rawBody,
    string? paymentRequiredHeader,
    string? paymentResponseHeader)
  {
    StatusCode = statusCode;
    Success = success;
    Value = value;
    RawBody = rawBody;
    PaymentRequiredHeader = paymentRequiredHeader;
    PaymentResponseHeader = paymentResponseHeader;
  }

  public int StatusCode { get; }
  public bool Success { get; }
  public MeteredCapabilityWireResponse? Value { get; }
  public string RawBody { get; }
  public string? PaymentRequiredHeader { get; }
  public string? PaymentResponseHeader { get; }
}
