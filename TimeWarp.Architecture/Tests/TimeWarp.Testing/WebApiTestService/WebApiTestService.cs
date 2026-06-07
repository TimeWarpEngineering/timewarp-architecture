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
    System.Reflection.MethodInfo method = type.GetMethod
    (
      name: "GetHttpResponseMessageFromRequest",
      bindingAttr: System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
      binder: null,
      types: new[] { typeof(IApiRequest), typeof(CancellationToken) },
      modifiers: null
    ) ?? throw new InvalidOperationException();

    // Call the method and provide a deterministic cancellation token
    var httpResponseMessage = (HttpResponseMessage)await method
      .InvokeAsync(ApiService, [apiRequest, CancellationToken.None])
      .ConfigureAwait(false);

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
    HttpResponseMessage httpResponseMessage,
    string attributeName
  )
  {
    string json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

    httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    json.ShouldContain("errors");
    json.ShouldContain(attributeName);
  }
}
