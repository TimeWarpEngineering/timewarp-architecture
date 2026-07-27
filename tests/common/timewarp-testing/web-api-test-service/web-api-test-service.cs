#region Purpose
// Shared assertion helpers over TestApiService: typed responses and endpoint-validation checks.
#endregion

#region Design
// Takes the concrete TestApiService (not IApiService): ConfirmEndpointValidationError needs the
// raw HttpResponseMessage, which TestApiService exposes directly. The previous implementation
// reflected into the SPA's BaseApiService private transport — a hidden compile-time dependency on
// the web feature and a refactoring trap; owning the client removes both.
#endregion

namespace TimeWarp.Architecture.Testing;

/// <summary>
/// A class that contains a common set of methods used when testing Web APIs
/// </summary>
[NotTest]
public class WebApiTestService : IWebApiTestService
{
  private readonly TestApiService ApiService;

  /// <summary>
  /// A class that contains a common set of methods used when testing Web APIs
  /// </summary>
  public WebApiTestService(TestApiService apiService)
  {
    ApiService = apiService;
  }

  /// <inheritdoc/>
  public async Task ConfirmEndpointValidationError<TResponse>
  (
    IApiRequest apiRequest,
    string attributeName
  )
  {
    HttpResponseMessage httpResponseMessage =
      await ApiService.GetHttpResponseMessage(apiRequest, CancellationToken.None).ConfigureAwait(false);

    string json = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

    httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    json.ShouldContain("errors");
    json.ShouldContain(attributeName);
  }

  public async Task<OneOf.OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest apiRequest,
      CancellationToken cancellationToken
    ) where TResponse : class =>
      await ApiService.GetResponse<TResponse>(apiRequest, cancellationToken);
}
