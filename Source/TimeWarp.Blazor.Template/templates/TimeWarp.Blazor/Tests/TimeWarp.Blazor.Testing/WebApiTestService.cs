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

  [NotTest]
  public class WebApiTestService
  {
    private readonly HttpClient HttpClient;
    private readonly JsonSerializerOptions JsonSerializerOptions;

    public WebApiTestService(HttpClient aHttpClient, JsonSerializerOptions aJsonSerializerOptions)
    {
      HttpClient = aHttpClient;
      JsonSerializerOptions = aJsonSerializerOptions;
    }

    public async Task ConfirmEndpointValidationError<TResponse>
    (
      string aRoute,
      IRequest<TResponse> aRequest,
      string aAttributeName
    )
    {
      var apiRequest = aRequest as IApiRequest;
      HttpVerb httpverb = apiRequest.GetHttpVerb();
      HttpResponseMessage httpResponseMessage = null;

      switch (httpverb)
      {
        case HttpVerb.Get:
          httpResponseMessage = await HttpClient.GetAsync(apiRequest.GetRoute());
          break;
        case HttpVerb.Delete:
          httpResponseMessage = await HttpClient.DeleteAsync(apiRequest.GetRoute());
          break;
        case HttpVerb.Post:
        case HttpVerb.Put:
        case HttpVerb.Patch:
          httpResponseMessage =
            await GetHttpResponseMessageFromRequest(aRoute, aRequest).ConfigureAwait(false);
          break;
        case HttpVerb.Head:
        case HttpVerb.Options:
          throw new Exception("Update this if ever used!");
      }


      await ConfirmEndpointValidationError(httpResponseMessage, aAttributeName).ConfigureAwait(false);
    }

    internal static async Task ConfirmEndpointValidationError
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

    internal async Task<TResponse> DeleteJsonAsync<TResponse>(string aUri)
    {
      HttpResponseMessage httpResponseMessage = await HttpClient.DeleteAsync(aUri).ConfigureAwait(false);
      return await ReadFromJson<TResponse>(httpResponseMessage).ConfigureAwait(false);
    }


    internal async Task<HttpResponseMessage> GetHttpResponseMessageFromRequest<TResponse>(string aUri, IRequest<TResponse> aRequest)
    {
      string requestAsJson = JsonSerializer.Serialize(aRequest, aRequest.GetType());

      var httpContent =
        new StringContent
        (
          requestAsJson,
          Encoding.UTF8,
          MediaTypeNames.Application.Json
        );

      var apiRequest = aRequest as IApiRequest;
      HttpVerb httpverb = apiRequest.GetHttpVerb();

      return httpverb switch
      {
        HttpVerb.Post => await HttpClient.PostAsync(aUri, httpContent).ConfigureAwait(false),
        HttpVerb.Put => await HttpClient.PutAsync(aUri, httpContent).ConfigureAwait(false),
        HttpVerb.Patch => await HttpClient.PatchAsync(aUri, httpContent).ConfigureAwait(false),
        _ => null,
      };
    }

    public async Task<TResponse> GetJsonAsync<TResponse>(string aUri)
    {
      HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(aUri).ConfigureAwait(false);

      TResponse response = await ReadFromJson<TResponse>(httpResponseMessage).ConfigureAwait(false);

      return response;
    }

    internal async Task<TResponse> Post<TResponse>(string aUri, IRequest<TResponse> aRequest)
    {
      HttpResponseMessage httpResponseMessage =
        await GetHttpResponseMessageFromRequest(aUri, aRequest).ConfigureAwait(false);

      httpResponseMessage.EnsureSuccessStatusCode();

      string json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

      TResponse response = JsonSerializer.Deserialize<TResponse>(json);

      return response;
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
