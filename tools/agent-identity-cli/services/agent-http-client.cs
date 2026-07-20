#region Purpose
// Thin HTTP client for agent identity ceremony endpoints; surfaces non-2xx as status + raw body.
#endregion
#region Design
// CLI-local paths (not generated RouteTemplates) so this tool has zero web-contracts coupling.
// On non-2xx: return status + raw body (problem details) — never hide errors. Empty-body POSTs
// send "{}" because some hosts reject a truly empty application/json body.
#endregion

namespace AgentIdentityCli.Services;

internal sealed class AgentHttpClient
{
  public const string RegisterOptionsPath = "/api/identity/agent/register/options";
  public const string RegisterPath = "/api/identity/agent/register";
  public const string TokenOptionsPath = "/api/identity/agent/token/options";
  public const string TokenPath = "/api/identity/agent/token";
  public const string MePath = "/api/identity/agent/me";

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
