#nullable enable
namespace TimeWarp.Architecture.Testing;

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
    IApiRequest apiRequest,
    string attributeName
  )
  {
    HttpResponseMessage httpResponseMessage =
      await WebApiService.GetHttpResponseMessageFromRequest(apiRequest).ConfigureAwait(false);

    await ConfirmEndpointValidationError(httpResponseMessage, attributeName).ConfigureAwait(false);
  }

  private static async Task ConfirmEndpointValidationError
  (
    HttpResponseMessage aHttpResponseMessage,
    string attributeName
  )
  {
    string json = await aHttpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

    aHttpResponseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    json.Should().Contain("errors");
    json.Should().Contain(attributeName);
  }

  public async Task<TResponse?> GetResponse<TResponse>(IApiRequest apiRequest) where TResponse : class =>
    await WebApiService.GetResponse<TResponse>(apiRequest);
}
