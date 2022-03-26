namespace TimeWarp.Architecture.Testing
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
  using TimeWarp.Architecture.Features;

  /// <summary>
  /// A class that contains a common set of methods used when testing Web APIs
  /// </summary>
  [NotTest]
  public class WebApiTestService : IWebApiTestService
  {
    private readonly WebApiService WebApiService;

    public WebApiTestService(WebApiService aWebApiService)
    {
      WebApiService = aWebApiService;
    }

    /// <inheritdoc/>
    public async Task ConfirmEndpointValidationError<TResponse>
    (
      IApiRequest aApiRequest,
      string aAttributeName
    )
    {
      HttpResponseMessage httpResponseMessage =
        await WebApiService.GetHttpResponseMessageFromRequest<TResponse>(aApiRequest).ConfigureAwait(false);

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

    public async Task<TResponse> GetResponse<TResponse>(IApiRequest aApiRequest) =>
      await WebApiService.GetResponse<TResponse>(aApiRequest);
  }
}
