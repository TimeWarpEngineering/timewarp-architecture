#region Purpose
// Asks xAI whether a key is accepted before that key is stored.
#endregion

#region Design
// GET {endpoint}/models with Authorization: Bearer. The key is not placed in the URL and is not
// logged. 2xx accepts the key. 401 and 403 return the provider body with the key removed, capped,
// and flattened. Any other failure, including a timeout, does not save and uses a fixed sentence.
// The caller's cancellation still cancels. A timeout that the caller did not ask for is unreachable.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.AgentChats.Application;

/// <summary>HTTP check against the xAI models list.</summary>
internal sealed class XaiApiKeyProbe : IXaiApiKeyProbe
{
  /// <summary>Named client. The key is a header, not a client default.</summary>
  public const string HttpClientName = "xai-api-key-probe";

  private static readonly TimeSpan Budget = TimeSpan.FromSeconds(10);

  private readonly IHttpClientFactory HttpClients;
  private readonly IOptionsMonitor<XaiChatOptions> Options;

  /// <summary>Creates the probe.</summary>
  public XaiApiKeyProbe(IHttpClientFactory httpClients, IOptionsMonitor<XaiChatOptions> options)
  {
    HttpClients = httpClients ?? throw new ArgumentNullException(nameof(httpClients));
    Options = options ?? throw new ArgumentNullException(nameof(options));
  }

  /// <inheritdoc />
  public async Task<OneOf<bool, SharedProblemDetails>> ValidateAsync(
    string apiKey,
    CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
    string endpoint = Options.CurrentValue.Endpoint;
    if (string.IsNullOrWhiteSpace(endpoint))
    {
      endpoint = XaiChatDefaults.DefaultEndpoint;
    }

    string url = endpoint.TrimEnd('/') + "/models";
    using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    budget.CancelAfter(Budget);
    try
    {
      HttpClient client = HttpClients.CreateClient(HttpClientName);
      using HttpRequestMessage request = new(HttpMethod.Get, url);
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
      using HttpResponseMessage response = await client
        .SendAsync(request, budget.Token)
        .ConfigureAwait(false);
      if (response.IsSuccessStatusCode)
      {
        return true;
      }

      int status = (int)response.StatusCode;
      if (status is 401 or 403)
      {
        string body = await response.Content.ReadAsStringAsync(budget.Token).ConfigureAwait(false);
        return UserXaiApiKeyProblems.ProviderRejected(Scrub(body, apiKey));
      }

      return UserXaiApiKeyProblems.ProviderUnreachable();
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (Exception exception) when (exception is OperationCanceledException or HttpRequestException)
    {
      return UserXaiApiKeyProblems.ProviderUnreachable();
    }
  }

  /// <summary>Removes the key, newlines, and anything past 200 characters.</summary>
  internal static string Scrub(string? body, string apiKey)
  {
    if (string.IsNullOrWhiteSpace(body))
    {
      return "xAI rejected the API key.";
    }

    string text = body.Replace(apiKey, "[redacted]", StringComparison.Ordinal);
    text = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
    if (text.Length > 200)
    {
      text = text[..200].Trim();
    }

    if (text.Length == 0 || text.Contains(apiKey, StringComparison.Ordinal))
    {
      return "xAI rejected the API key.";
    }

    return text;
  }
}
