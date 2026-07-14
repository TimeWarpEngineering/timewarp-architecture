#region Purpose
// Self-contained IApiService over HttpClient for integration tests — no dependency on any UI client stack.
#endregion

#region Design
// timewarp-testing must compile in every template flag combination, so the test client cannot
// borrow the SPA's BaseApiService (web-feature-owned). It mirrors that transport's semantics:
// verb and route come from the contract (GetHttpVerb/GetRoute), GET/DELETE carry data via query
// string (IQueryStringRouteProvider), POST/PUT/PATCH send a JSON body, non-success and 204 map to
// SharedProblemDetails, cancellation maps to 499. Request bodies serialize with the injected seam
// options (ContractSerializationDefaults) — the contract seam, not compiler defaults.
// The bearer token is a fixed placeholder (test hosts do not validate tokens); pass null for
// anonymous requests. GetHttpResponseMessage is public so WebApiTestService can assert on raw
// responses without reflection.
#endregion

namespace TimeWarp.Architecture.Testing;

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using OneOf;

[NotTest]
public sealed class TestApiService : IApiService
{
  private readonly HttpClient HttpClient;
  private readonly JsonSerializerOptions JsonSerializerOptions;

  public TestApiService
  (
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions,
    string? bearerToken = "dummy-token"
  )
  {
    HttpClient = httpClient;
    JsonSerializerOptions = jsonSerializerOptions;
    if (bearerToken is not null)
    {
      HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", bearerToken);
    }
  }

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

      return httpResponseMessage.IsSuccessStatusCode
        ? await HandleSuccessResponse<TResponse>(httpResponseMessage, cancellationToken).ConfigureAwait(false)
        : httpResponseMessage.StatusCode switch
        {
          HttpStatusCode.NoContent => new SharedProblemDetails
          {
            Title = "No Content",
            Status = (int)HttpStatusCode.NoContent,
            Detail = "The response content is empty."
          },
          _ => await HandleProblemResponse(httpResponseMessage, cancellationToken).ConfigureAwait(false)
        };
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

  /// <summary>Send the contract's request and return the raw response (for status/body assertions).</summary>
  public async Task<HttpResponseMessage> GetHttpResponseMessage(IApiRequest apiRequest, CancellationToken cancellationToken)
  {
    string route = PrepareRoute(apiRequest);
    using StringContent? httpContent = PrepareContent(apiRequest);
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
    catch (System.Exception exception) when (exception is JsonException or InvalidOperationException)
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

  private async Task<TResponse> ReadFromJson<TResponse>(HttpResponseMessage httpResponseMessage, CancellationToken cancellationToken)
  {
    string json = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    return JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions) ??
      throw new InvalidOperationException("The response is null.");
  }
}
