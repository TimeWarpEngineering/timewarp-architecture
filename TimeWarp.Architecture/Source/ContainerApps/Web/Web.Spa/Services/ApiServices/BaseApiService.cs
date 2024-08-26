namespace TimeWarp.Architecture;

/// <summary>
/// Class that abstracts the WebAPI into a simple interface.
/// Given a Request return the Response.
/// </summary>
/// <remarks>
/// You don't care what http verb is used or even what protocol is used.
/// </remarks>
[UsedImplicitly]
public abstract class BaseApiService : IApiService
{
  protected readonly HttpClient HttpClient;
  private readonly JsonSerializerOptions JsonSerializerOptions;

  private readonly IAccessTokenProvider AccessTokenProvider;
  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// Given a Request return the Response.
  /// </summary>
  /// <param name="httpClientFactory"></param>
  /// <param name="httpClientName"></param>
  /// <param name="accessTokenProvider"></param>
  /// <param name="jsonSerializerOptionsAccessor"></param>
  protected BaseApiService
  (
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    IAccessTokenProvider accessTokenProvider,
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccessor
  )
  {
    HttpClient = httpClientFactory.CreateClient(httpClientName);
    AccessTokenProvider = accessTokenProvider;
    JsonSerializerOptions = jsonSerializerOptionsAccessor.Value;
  }

  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// This constructor is provided for testing purposes only.
  /// </summary>
  /// <param name="httpClient"></param>
  /// <param name="accessTokenProvider"></param>
  /// <param name="jsonSerializerOptions"></param>
  protected BaseApiService
  (
    HttpClient httpClient,
    IAccessTokenProvider accessTokenProvider,
    JsonSerializerOptions jsonSerializerOptions
  )
  {
    HttpClient = httpClient;
    AccessTokenProvider = accessTokenProvider;
    JsonSerializerOptions = jsonSerializerOptions;
  }

  /// <summary>
  /// Get the response for the given request
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public virtual async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    try
    {
      HttpResponseMessage httpResponseMessage =
        await GetHttpResponseMessageFromRequest(request, cancellationToken).ConfigureAwait(false);

      if (httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
      {
        return new SharedProblemDetails
        {
          Title = "No Content",
          Status = (int)HttpStatusCode.NoContent,
          Detail = "The response content is empty."
        };
      }

      if (httpResponseMessage.IsSuccessStatusCode)
      {
        if (typeof(TResponse) == typeof(Stream))
        {
          Stream fileStream = await ReadFileStream(httpResponseMessage, cancellationToken).ConfigureAwait(false);
          var fileResponse = new FileResponse(fileStream: fileStream)
          {
            FileName = httpResponseMessage.Content.Headers.ContentDisposition?.FileName,
            ContentType = httpResponseMessage.Content.Headers.ContentType?.MediaType
          };
          return fileResponse;
        }
        return await ReadFromJson<TResponse>(httpResponseMessage, cancellationToken).ConfigureAwait(false);
      }

      return await ReadFromJson<SharedProblemDetails>(httpResponseMessage, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      return new SharedProblemDetails
      {
        Title = "Operation Cancelled",
        Status = 499, // 499 is the code for "Client Closed Request"
        Detail = "The request was cancelled."
      };
    }
  }

  private async Task<HttpResponseMessage> GetHttpResponseMessageFromRequest(IApiRequest apiRequest, CancellationToken cancellationToken)
  {
    string route = PrepareRoute(apiRequest);
    StringContent? httpContent = PrepareContent(apiRequest);
    HttpVerb httpVerb = apiRequest.GetHttpVerb();
    await SetBearerTokenAsync();
    return httpVerb switch
    {
      HttpVerb.Get => await HttpClient.GetAsync(route, cancellationToken).ConfigureAwait(false),
      HttpVerb.Delete => await HttpClient.DeleteAsync(route, cancellationToken).ConfigureAwait(false),
      HttpVerb.Post => await HttpClient.PostAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Put => await HttpClient.PutAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Patch => await HttpClient.PatchAsync(route, httpContent, cancellationToken).ConfigureAwait(false),
      HttpVerb.Head => throw new NotImplementedException(),
      HttpVerb.Options => throw new NotImplementedException(),
      _ => throw new NotImplementedException()
    };
  }

  private static StringContent? PrepareContent(IApiRequest apiRequest)
  {
    HttpVerb httpVerb = apiRequest.GetHttpVerb();
    switch (httpVerb)
    {
      case HttpVerb.Post:
      case HttpVerb.Put:
      case HttpVerb.Patch:
        string requestAsJson = JsonSerializer.Serialize(apiRequest, apiRequest.GetType());
        return new StringContent(requestAsJson, Encoding.UTF8, MediaTypeNames.Application.Json);
      case HttpVerb.Get:
      case HttpVerb.Delete:
      case HttpVerb.Head:
      case HttpVerb.Options:
        return null;
      default:
        throw new ArgumentOutOfRangeException($"HttpVerb: {httpVerb} is not supported.");
    }
  }
  private static string PrepareRoute(IApiRequest apiRequest)
  {
    switch (apiRequest.GetHttpVerb())
    {
      case HttpVerb.Get:
        return (apiRequest as IQueryStringRouteProvider)?.GetRouteWithQueryString() ?? apiRequest.GetRoute();
      case HttpVerb.Post:
      case HttpVerb.Delete:
      case HttpVerb.Put:
      case HttpVerb.Patch:
      case HttpVerb.Head:
      case HttpVerb.Options:
      default:
        return apiRequest.GetRoute();
    }
  }
  private async Task<TResponse> ReadFromJson<TResponse>(HttpResponseMessage httpResponseMessage, CancellationToken cancellationToken)
  {
    httpResponseMessage.EnsureSuccessStatusCode();

    string json = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    TResponse? response = JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions);
    if (response is null)
      throw new InvalidOperationException("The response is null.");

    return response;
  }
  private static async Task<Stream> ReadFileStream(HttpResponseMessage httpResponseMessage, CancellationToken cancellationToken)
  {
    httpResponseMessage.EnsureSuccessStatusCode();
    return await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task SetBearerTokenAsync()
  {
    AccessTokenResult tokenResult = await AccessTokenProvider.RequestAccessToken();
    if (tokenResult.TryGetToken(out AccessToken? token))
    {
      HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
    }
  }
}
