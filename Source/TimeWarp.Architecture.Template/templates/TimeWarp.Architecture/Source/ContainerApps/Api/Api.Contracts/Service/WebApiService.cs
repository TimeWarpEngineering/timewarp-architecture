namespace TimeWarp.Architecture;

using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features;

/// <summary>
/// Class that abstracts the WebAPI into a simple interface.
/// Given a Request return the Response.
/// </summary>
/// <remarks>
/// You don't care what http verb is used or even what protocol is used.
/// </remarks>
public class WebApiService
{
  private readonly HttpClient HttpClient;
  private readonly JsonSerializerOptions JsonSerializerOptions;

  public WebApiService(HttpClient aHttpClient, IOptions<JsonSerializerOptions> aJsonSerializerOptionsAccessor)
  {
    HttpClient = aHttpClient;
    JsonSerializerOptions = aJsonSerializerOptionsAccessor.Value;
  }

  /// <summary>
  /// Get the response for the given request
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="aRequest"></param>
  /// <returns></returns>
  public async Task<TResponse?> GetResponse<TResponse>(IApiRequest aRequest)
  {
    HttpResponseMessage httpResponseMessage =
      await GetHttpResponseMessageFromRequest<TResponse>(aRequest).ConfigureAwait(false);

    return await ReadFromJson<TResponse>(httpResponseMessage).ConfigureAwait(false);
  }

  public async Task<HttpResponseMessage> GetHttpResponseMessageFromRequest<TResponse>
  (
    IApiRequest aApiRequest
  )
  {
    HttpVerb httpverb = aApiRequest.GetHttpVerb();
    StringContent? httpContent = null;

    if (httpverb == HttpVerb.Post || httpverb == HttpVerb.Put || httpverb == HttpVerb.Patch)
    {

      string requestAsJson = JsonSerializer.Serialize(aApiRequest, aApiRequest.GetType());

      httpContent =
        new StringContent
        (
          requestAsJson,
          Encoding.UTF8,
          MediaTypeNames.Application.Json
        );
    }

    return httpverb switch
    {
      HttpVerb.Get => await HttpClient.GetAsync(aApiRequest.GetRoute()).ConfigureAwait(false),
      HttpVerb.Delete => await HttpClient.DeleteAsync(aApiRequest.GetRoute()).ConfigureAwait(false),
      HttpVerb.Post => await HttpClient.PostAsync(aApiRequest.GetRoute(), httpContent).ConfigureAwait(false),
      HttpVerb.Put => await HttpClient.PutAsync(aApiRequest.GetRoute(), httpContent).ConfigureAwait(false),
      HttpVerb.Patch => await HttpClient.PatchAsync(aApiRequest.GetRoute(), httpContent).ConfigureAwait(false),
      HttpVerb.Head => throw new NotImplementedException(),
      HttpVerb.Options => throw new NotImplementedException(),
      _ => throw new NotImplementedException()
    };
  }


  private async Task<TResponse?> ReadFromJson<TResponse>(HttpResponseMessage aHttpResponseMessage)
  {
    aHttpResponseMessage.EnsureSuccessStatusCode();

    string json = await aHttpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

    TResponse? response = JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions);
    Console.WriteLine(JsonSerializerOptions.ToString());

    return response;
  }
}
