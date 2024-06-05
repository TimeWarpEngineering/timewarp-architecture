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
  protected HttpClient HttpClient { get; init; }
  private readonly JsonSerializerOptions JsonSerializerJsonSerializerOptions;

  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// </summary>
  /// <param name="httpClientFactory"></param>
  /// <param name="httpClientName"></param>
  /// <param name="jsonSerializerOptionsAccessor"></param>
  [ActivatorUtilitiesConstructor]
  protected BaseApiService
  (
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    IOptions<JsonSerializerOptions> jsonSerializerOptionsAccessor
  )
  {
    JsonSerializerJsonSerializerOptions = jsonSerializerOptionsAccessor.Value;
    HttpClient = httpClientFactory.CreateClient(httpClientName);
  }

  /// <summary>
  /// This is the Service that is used to interact with the API.Server
  /// This constructor is provided for testing purposes.
  /// </summary>
  /// <param name="httpClient"></param>
  /// <param name="jsonSerializerOptions"></param>
  protected BaseApiService(HttpClient httpClient, JsonSerializerOptions jsonSerializerOptions)
  {
    HttpClient = httpClient;
    JsonSerializerJsonSerializerOptions = jsonSerializerOptions;
  }

  /// <summary>
  /// Get the response for the given request
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public virtual async Task<OneOf<TResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    HttpResponseMessage httpResponseMessage =
      await GetHttpResponseMessageFromRequest(request).ConfigureAwait(false);

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
      return await ReadFromJson<TResponse>(httpResponseMessage).ConfigureAwait(false);

    return await ReadFromJson<SharedProblemDetails>(httpResponseMessage).ConfigureAwait(false);
  }

  private async Task<HttpResponseMessage> GetHttpResponseMessageFromRequest
  (
    IApiRequest apiRequest
  )
  {
    string route = PrepareRoute(apiRequest);
    StringContent? httpContent = PrepareContent(apiRequest);
    HttpVerb httpVerb = apiRequest.GetHttpVerb();
    return httpVerb switch
    {
      HttpVerb.Get => await HttpClient.GetAsync(route).ConfigureAwait(false),
      HttpVerb.Delete => await HttpClient.DeleteAsync(route).ConfigureAwait(false),
      HttpVerb.Post => await HttpClient.PostAsync(route, httpContent).ConfigureAwait(false),
      HttpVerb.Put => await HttpClient.PutAsync(route, httpContent).ConfigureAwait(false),
      HttpVerb.Patch => await HttpClient.PatchAsync(route, httpContent).ConfigureAwait(false),
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
        {
          string requestAsJson = JsonSerializer.Serialize(apiRequest, apiRequest.GetType());

          return
            new StringContent
            (
              requestAsJson,
              Encoding.UTF8,
              MediaTypeNames.Application.Json
            );
        }
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
  private async Task<TResponse> ReadFromJson<TResponse>(HttpResponseMessage httpResponseMessage)
  {
    //httpResponseMessage.EnsureSuccessStatusCode();

    string json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

    TResponse? response = JsonSerializer.Deserialize<TResponse>(json, JsonSerializerJsonSerializerOptions);
    if (response is null)
      throw new InvalidOperationException("The response is null.");

    return response;
  }
}
