#region Purpose
// Shared HTTP transport core for IApiService: contract → request → OneOf outcome.
#endregion

#region Design
// Single home for the verb/route/body/problem mapping that SPA BaseApiService and test
// TestApiService previously mirrored. Both composers pass an HttpClient + seam
// JsonSerializerOptions; the SPA supplies a per-request bearer acquire delegate (MSAL
// IAccessTokenProvider adapter), tests pin Authorization on the client and pass null.
//
// Verbs: Get/Delete/Post/Put/Patch only — Head/Options throw NotSupportedException (no
// real client for them on either side). GET/DELETE take data via query string
// (IQueryStringRouteProvider); POST/PUT/PATCH send JSON bodies with the seam options.
// 204 is checked before IsSuccessStatusCode (it is 2xx but has no body to deserialize) and
// maps to SharedProblemDetails; other non-success map to problem; cancellation maps to 499.
// Stream TResponse becomes FileResponse without EnsureSuccessStatusCode (already on the
// success branch). Problem-body deserialization catches only JsonException/
// InvalidOperationException so unexpected failures surface rather than becoming a synthetic
// problem.
#endregion

namespace TimeWarp.Foundation;

using System.Net.Http.Headers;

/// <summary>
/// HTTP transport implementation of <see cref="IApiService"/>.
/// Compose from SPA and test clients rather than duplicating verb/route/problem logic.
/// </summary>
public sealed class HttpApiService : IApiService
{
  private readonly HttpClient HttpClient;
  private readonly JsonSerializerOptions JsonSerializerOptions;
  private readonly Func<CancellationToken, Task<string?>>? AcquireBearerTokenAsync;

  /// <param name="httpClient">Client used for all requests (caller owns lifetime).</param>
  /// <param name="jsonSerializerOptions">Contract-seam serializer options.</param>
  /// <param name="acquireBearerTokenAsync">
  /// Optional per-request bearer acquisition. When non-null, invoked before each send;
  /// a non-null returned string is set as the Bearer header on the client.
  /// </param>
  public HttpApiService
  (
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions,
    Func<CancellationToken, Task<string?>>? acquireBearerTokenAsync = null
  )
  {
    HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    JsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
    AcquireBearerTokenAsync = acquireBearerTokenAsync;
  }

  /// <inheritdoc />
  public async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    try
    {
      HttpResponseMessage httpResponseMessage =
        await GetHttpResponseMessage(request, cancellationToken).ConfigureAwait(false);

      // 204 is a 2xx success, so check it before IsSuccessStatusCode — empty body is not a DTO.
      if (httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
      {
        return new SharedProblemDetails
        {
          Title = "No Content",
          Status = (int)HttpStatusCode.NoContent,
          Detail = "The response content is empty."
        };
      }

      return httpResponseMessage.IsSuccessStatusCode
        ? await HandleSuccessResponse<TResponse>(httpResponseMessage, cancellationToken).ConfigureAwait(false)
        : await HandleProblemResponse(httpResponseMessage, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      return new SharedProblemDetails
      {
        Title = "Operation Cancelled",
        Status = 499, // "Client Closed Request"
        Detail = "The request was cancelled."
      };
    }
  }

  /// <summary>Send the contract's request and return the raw response (status/body assertions, SPA internals).</summary>
  public async Task<HttpResponseMessage> GetHttpResponseMessage
  (
    IApiRequest apiRequest,
    CancellationToken cancellationToken
  )
  {
    // Relative-or-absolute so contract routes ("/api/…") resolve against HttpClient.BaseAddress.
    Uri route = new(PrepareRoute(apiRequest), UriKind.RelativeOrAbsolute);
    using StringContent? httpContent = PrepareContent(apiRequest);
    await ApplyBearerTokenAsync(cancellationToken).ConfigureAwait(false);
    return apiRequest.GetHttpVerb() switch
    {
      HttpVerb.Get => await HttpClient.GetAsync(route, cancellationToken).ConfigureAwait(false),
      HttpVerb.Delete => await HttpClient.DeleteAsync(route, cancellationToken).ConfigureAwait(false),
      HttpVerb.Post => await HttpClient.PostAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Put => await HttpClient.PutAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Patch => await HttpClient.PatchAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      var verb => throw new NotSupportedException($"HttpVerb: {verb} is not supported.")
    };
  }

  private async Task ApplyBearerTokenAsync(CancellationToken cancellationToken)
  {
    if (AcquireBearerTokenAsync is null)
    {
      return;
    }

    string? token = await AcquireBearerTokenAsync(cancellationToken).ConfigureAwait(false);
    if (token is not null)
    {
      HttpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(scheme: "Bearer", token);
    }
  }

  private async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> HandleSuccessResponse<TResponse>
  (
    HttpResponseMessage httpResponseMessage,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    if (typeof(TResponse) == typeof(Stream))
    {
      Stream fileStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
      return new FileResponse(fileStream: fileStream)
      {
        FileName = httpResponseMessage.Content.Headers.ContentDisposition?.FileName,
        ContentType = httpResponseMessage.Content.Headers.ContentType?.MediaType
      };
    }

    return await ReadFromJson<TResponse>(httpResponseMessage, cancellationToken).ConfigureAwait(false);
  }

  private async Task<SharedProblemDetails> HandleProblemResponse
  (
    HttpResponseMessage httpResponseMessage,
    CancellationToken cancellationToken
  )
  {
    try
    {
      return await ReadFromJson<SharedProblemDetails>(httpResponseMessage, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is JsonException or InvalidOperationException)
    {
      // Body was not RFC 7807 JSON — synthesize a problem from the status code.
      return new SharedProblemDetails
      {
        Title = "Unhandled Error",
        Status = (int)httpResponseMessage.StatusCode,
        Detail = "An unhandled error occurred while processing the request."
      };
    }
  }

  private static string PrepareRoute(IApiRequest apiRequest) =>
    apiRequest.GetHttpVerb() switch
    {
      // GET and DELETE carry no body, so the query string is their only data channel
      // besides route parameters.
      HttpVerb.Get or HttpVerb.Delete =>
        (apiRequest as IQueryStringRouteProvider)?.GetRouteWithQueryString() ?? apiRequest.GetRoute(),
      _ => apiRequest.GetRoute()
    };

  private StringContent? PrepareContent(IApiRequest apiRequest) =>
    apiRequest.GetHttpVerb() switch
    {
      HttpVerb.Post or HttpVerb.Put or HttpVerb.Patch =>
        new StringContent
        (
          JsonSerializer.Serialize(apiRequest, apiRequest.GetType(), JsonSerializerOptions),
          Encoding.UTF8,
          MediaTypeNames.Application.Json
        ),
      _ => null
    };

  private async Task<TResponse> ReadFromJson<TResponse>
  (
    HttpResponseMessage httpResponseMessage,
    CancellationToken cancellationToken
  )
  {
    string json = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    return JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions) ??
      throw new InvalidOperationException("The response is null.");
  }
}
