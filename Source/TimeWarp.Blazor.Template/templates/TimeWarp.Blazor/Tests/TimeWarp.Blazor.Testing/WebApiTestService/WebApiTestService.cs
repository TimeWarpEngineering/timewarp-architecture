namespace TimeWarp.Blazor.Testing
{
  using FluentAssertions;
  using MediatR;
  using System;
  using System.Net;
  using System.Net.Http;
  using System.Net.Mime;
  using System.Text;
  using System.Text.Json;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  /// <summary>
  /// A class that contains a common set of methods used when testing Web APIs
  /// </summary>
  [NotTest]
  public class WebApiTestService : IWebApiTestService
  {
    private readonly HttpClient HttpClient;
    private readonly JsonSerializerOptions JsonSerializerOptions;

    public WebApiTestService(HttpClient aHttpClient, JsonSerializerOptions aJsonSerializerOptions)
    {
      HttpClient = aHttpClient;
      JsonSerializerOptions = aJsonSerializerOptions;
    }

    public async Task<TResponse> GetResponse<TResponse>(IApiRequest aRequest)
    {

      HttpResponseMessage httpResponseMessage =
        await GetHttpResponseMessageFromRequest<TResponse>(aRequest).ConfigureAwait(false);
      return await ReadFromJson<TResponse>(httpResponseMessage).ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public async Task ConfirmEndpointValidationError<TResponse>
    (
      IApiRequest aApiRequest,
      string aAttributeName
    )
    {
      HttpVerb httpverb = aApiRequest.GetHttpVerb();
      HttpResponseMessage httpResponseMessage = null;

      switch (httpverb)
      {
        case HttpVerb.Get:
          httpResponseMessage = await HttpClient.GetAsync(aApiRequest.GetRoute());
          break;
        case HttpVerb.Delete:
          httpResponseMessage = await HttpClient.DeleteAsync(aApiRequest.GetRoute());
          break;
        case HttpVerb.Post:
        case HttpVerb.Put:
        case HttpVerb.Patch:
          httpResponseMessage =
            await GetHttpResponseMessageFromRequest<TResponse>(aApiRequest).ConfigureAwait(false);
          break;
        case HttpVerb.Head:
        case HttpVerb.Options:
          throw new Exception("Update this if ever used!");
      }


      await ConfirmEndpointValidationError(httpResponseMessage, aAttributeName).ConfigureAwait(false);
    }

    private static async Task ConfirmEndpointValidationError
    (
      HttpResponseMessage aHttpResponseMessage,
      string aAttributeName
    )
    {
      string json = await aHttpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

      aHttpResponseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
      json.Should().Contain("errors");
      json.Should().Contain(aAttributeName);
    }


    private async Task<HttpResponseMessage> GetHttpResponseMessageFromRequest<TResponse>
    (
      IApiRequest aApiRequest
    )
    {
      string requestAsJson = JsonSerializer.Serialize(aApiRequest, aApiRequest.GetType());

      var httpContent =
        new StringContent
        (
          requestAsJson,
          Encoding.UTF8,
          MediaTypeNames.Application.Json
        );

      HttpVerb httpverb = aApiRequest.GetHttpVerb();

      return httpverb switch
      {
        HttpVerb.Get => await HttpClient.GetAsync(aApiRequest.GetRoute()).ConfigureAwait(false),
        HttpVerb.Delete => await HttpClient.DeleteAsync(aApiRequest.GetRoute()).ConfigureAwait(false),
        HttpVerb.Post => await HttpClient.PostAsync(aApiRequest.GetRoute(), httpContent).ConfigureAwait(false),
        HttpVerb.Put => await HttpClient.PutAsync(aApiRequest.GetRoute(), httpContent).ConfigureAwait(false),
        HttpVerb.Patch => await HttpClient.PatchAsync(aApiRequest.GetRoute(), httpContent).ConfigureAwait(false),
        _ => null,
      };
    }


    private async Task<TResponse> ReadFromJson<TResponse>(HttpResponseMessage aHttpResponseMessage)
    {
      aHttpResponseMessage.EnsureSuccessStatusCode();

      string json = await aHttpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

      TResponse response = JsonSerializer.Deserialize<TResponse>(json, JsonSerializerOptions);

      return response;
    }
  }
}
