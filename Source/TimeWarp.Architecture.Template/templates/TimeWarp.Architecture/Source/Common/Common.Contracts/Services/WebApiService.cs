namespace TimeWarp.Architecture;

/// <summary>
/// Class that abstracts the WebAPI into a simple interface.
/// Given a Request return the Response.
/// </summary>
/// <remarks>
/// You don't care what http verb is used or even what protocol is used.
/// </remarks>
[UsedImplicitly]
public abstract class WebApiService
(
  IHttpClientFactory HttpClientFactory,
  string HttpClientName,
  IOptions<JsonSerializerOptions> JsonSerializerOptionsAccessor
) : IApiService
{
  private HttpClient HttpClient => HttpClientFactory.CreateClient(HttpClientName);
  private readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptionsAccessor.Value;

  /// <summary>
  /// Get the response for the given request
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<OneOf<TResponse, SharedProblemDetails>> GetResponse<TResponse>(IApiRequest request, CancellationToken cancellationToken) where TResponse : class
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

  [UsedImplicitly]// Used by the WebApiServiceTests
  public async Task<HttpResponseMessage> GetHttpResponseMessageFromRequest
  (
    IApiRequest apiRequest
  )
  {
    HttpVerb httpVerb = apiRequest.GetHttpVerb();
    StringContent? httpContent = null;

    if (httpVerb is HttpVerb.Post or HttpVerb.Put or HttpVerb.Patch)
    {

      string requestAsJson = JsonSerializer.Serialize(apiRequest, apiRequest.GetType());

      httpContent =
        new StringContent
        (
        requestAsJson,
        Encoding.UTF8,
        MediaTypeNames.Application.Json
        );
    }

    return httpVerb switch
    {
      HttpVerb.Get => await HttpClient.GetAsync(apiRequest.GetRoute()).ConfigureAwait(false),
      HttpVerb.Delete => await HttpClient.DeleteAsync(apiRequest.GetRoute()).ConfigureAwait(false),
      HttpVerb.Post => await HttpClient.PostAsync(apiRequest.GetRoute(), httpContent).ConfigureAwait(false),
      HttpVerb.Put => await HttpClient.PutAsync(apiRequest.GetRoute(), httpContent).ConfigureAwait(false),
      HttpVerb.Patch => await HttpClient.PatchAsync(apiRequest.GetRoute(), httpContent).ConfigureAwait(false),
      HttpVerb.Head => throw new NotImplementedException(),
      HttpVerb.Options => throw new NotImplementedException(),
      _ => throw new NotImplementedException()
    };
  }


  private async Task<TResponse> ReadFromJson<TResponse>(HttpResponseMessage httpResponseMessage)
  {
    httpResponseMessage.EnsureSuccessStatusCode();

    string json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

    TResponse? response = JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions);
    if (response is null)
      throw new InvalidOperationException("The response is null.");

    return response;
  }
}
