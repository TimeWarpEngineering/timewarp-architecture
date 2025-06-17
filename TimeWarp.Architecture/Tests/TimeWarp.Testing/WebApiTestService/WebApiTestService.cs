#nullable enable
namespace TimeWarp.Architecture.Testing;

/// <summary>
/// A class that contains a common set of methods used when testing Web APIs
/// </summary>
[NotTest]
public class WebApiTestService : IWebApiTestService
{
  private readonly IApiService ApiService;
  /// <summary>
  /// A class that contains a common set of methods used when testing Web APIs
  /// </summary>
  public WebApiTestService(IApiService apiService) {
    ApiService = apiService;
  }

  /// <inheritdoc/>
  public async Task ConfirmEndpointValidationError<TResponse>
  (
    IApiRequest apiRequest,
    string attributeName
  )
  {
    // Get the type of the current class
    Type type = typeof(BaseApiService);

    // Get the private method you want to call.
    MethodInfo method = type.GetMethod("GetHttpResponseMessageFromRequest") ?? throw new InvalidOperationException();

    // Call the method
    var httpResponseMessage = (HttpResponseMessage)await method.InvokeAsync(ApiService, [apiRequest]).ConfigureAwait(false);

    await ConfirmEndpointValidationError(httpResponseMessage, attributeName).ConfigureAwait(false);
  }

  public async Task<OneOf.OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest apiRequest,
      CancellationToken cancellationToken
    ) where TResponse : class =>
      await ApiService.GetResponse<TResponse>(apiRequest, cancellationToken);

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
}
